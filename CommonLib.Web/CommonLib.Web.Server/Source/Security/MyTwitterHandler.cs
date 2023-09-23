using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using AspNet.Security.OAuth.Twitter;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Utils;
using CommonLib.Source.Common.Utils.UtilClasses.Exceptions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace CommonLib.Web.Server.Source.Security
{
    public class MyTwitterHandler : TwitterAuthenticationHandler
    {
        public MyTwitterHandler(IOptionsMonitor<TwitterAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder, clock) { }

        protected override async Task<HandleRequestResult> HandleRemoteAuthenticateAsync()
        {
            var query = Request.Query;
            var stateHash = query["state"].ToString();
            var state = Response.HttpContext.Session.Get("state")?.ToUTF8String().JsonDeserialize().To<Dictionary<string, string>>();
            var properties = state is null ? null : new AuthenticationProperties(state!);
            if (properties is null || !stateHash.EqualsInvariant(state.JsonSerialize().TrimMultiline().Keccak256().HexToBase58()))
                return HandleRequestResult.Fail("The oauth state was missing or invalid.");
            if (!ValidateCorrelationId(properties))
                return HandleRequestResult.Fail("Correlation failed.", properties);

            var error = query["error"];
            if (!StringValues.IsNullOrEmpty(error))
            {
                var errorDescription = query["error_description"];
                var errorUri = query["error_uri"];
                if (StringValues.Equals(error, "access_denied"))
                {
                    var result = await HandleAccessDeniedErrorAsync(properties);
                    if (!result.None)
                        return result;

                    var deniedEx = new AuthenticationFailureException("Access was denied by the resource owner or by the remote server.")
                    {
                        Data =
                        {
                            ["error"] = error.ToString(),
                            ["error_description"] = errorDescription.ToString(),
                            ["error_uri"] = errorUri.ToString()
                        }
                    };

                    return HandleRequestResult.Fail(deniedEx, properties);
                }

                var failureMessage = new StringBuilder();
                failureMessage.Append(error);
                if (!StringValues.IsNullOrEmpty(errorDescription))
                    failureMessage.Append(";Description=").Append(errorDescription);
                if (!StringValues.IsNullOrEmpty(errorUri))
                    failureMessage.Append(";Uri=").Append(errorUri);

                var ex = new AuthenticationFailureException(failureMessage.ToString())
                {
                    Data =
                    {
                        ["error"] = error.ToString(),
                        ["error_description"] = errorDescription.ToString(),
                        ["error_uri"] = errorUri.ToString()
                    }
                };

                return HandleRequestResult.Fail(ex, properties);
            }

            var code = query["code"];
            if (StringValues.IsNullOrEmpty(code))
                return HandleRequestResult.Fail("Code was not found.", properties);

            var codeExchangeContext = new OAuthCodeExchangeContext(properties, code.ToString(), BuildRedirectUri(Options.CallbackPath));
            using var tokens = await ExchangeCodeAsync(codeExchangeContext);

            if (tokens.Error is not null)
                return HandleRequestResult.Fail(tokens.Error, properties);
            if (string.IsNullOrEmpty(tokens.AccessToken))
                return HandleRequestResult.Fail("Failed to retrieve access token.", properties);

            var identity = new ClaimsIdentity(ClaimsIssuer);
            if (Options.SaveTokens)
            {
                var authTokens = new List<AuthenticationToken> { new() { Name = "access_token", Value = tokens.AccessToken } };
                if (tokens.RefreshToken is not null && !tokens.RefreshToken.IsNullOrWhiteSpace())
                    authTokens.Add(new AuthenticationToken { Name = "refresh_token", Value = tokens.RefreshToken });
                if (tokens.TokenType is not null && !tokens.TokenType.IsNullOrWhiteSpace())
                    authTokens.Add(new AuthenticationToken { Name = "token_type", Value = tokens.TokenType });

                var expiresIn = tokens.ExpiresIn?.ToIntN();
                if (expiresIn is not null)
                {
                    var expiresAt = DateTime.UtcNow + TimeSpan.FromSeconds(expiresIn.ToInt());
                    authTokens.Add(new AuthenticationToken { Name = "expires_at", Value = expiresAt.ToString("o", CultureInfo.InvariantCulture) });
                }

                properties.StoreTokens(authTokens);
            }

            var ticket = await CreateTicketAsync(identity, properties, tokens);
            return HandleRequestResult.Success(ticket);
        }

        protected override async Task<OAuthTokenResponse> ExchangeCodeAsync([NotNull] OAuthCodeExchangeContext context)
        {
            var tokenRequestParameters = new Dictionary<string, string>
            {
                ["client_id"] = Options.ClientId,
                ["redirect_uri"] = context.RedirectUri,
                ["client_secret"] = Options.ClientSecret,
                ["code"] = context.Code,
                ["grant_type"] = "authorization_code",
            };

            // PKCE https://tools.ietf.org/html/rfc7636#section-4.5
            if (context.Properties.Items.TryGetValue(OAuthConstants.CodeVerifierKey, out var codeVerifier))
            {
                tokenRequestParameters.Add(OAuthConstants.CodeVerifierKey, codeVerifier!);
                context.Properties.Items.Remove(OAuthConstants.CodeVerifierKey);
            }

            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(
                string.Concat(
                    Uri.EscapeDataString(Options.ClientId),
                    ":",
                    Uri.EscapeDataString(Options.ClientSecret))));

            using var requestContent = new FormUrlEncodedContent(tokenRequestParameters!);
            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, Options.TokenEndpoint);

            requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            requestMessage.Content = requestContent;
            requestMessage.Version = Backchannel.DefaultRequestVersion;

            using var response = await Backchannel.SendAsync(requestMessage, Context.RequestAborted);
            var body = await response.Content.ReadAsStringAsync();

            return response.IsSuccessStatusCode switch
            {
                true => OAuthTokenResponse.Success(JsonDocument.Parse(body)),
                false => PrepareFailedOAuthTokenReponse(response, body)
            };

            static OAuthTokenResponse PrepareFailedOAuthTokenReponse(HttpResponseMessage response, string body)
            {
                var exception = GetStandardErrorException(JsonDocument.Parse(body));
                if (exception is not null) 
                    return OAuthTokenResponse.Failed(exception);

                var errorMessage = $"OAuth token endpoint failure: Status: {response.StatusCode};Headers: {response.Headers};Body: {body};";
                return OAuthTokenResponse.Failed(new Exception(errorMessage));

                static Exception? GetStandardErrorException(JsonDocument response)
                {
                    var root = response.RootElement;
                    var error = root.GetString("error");

                    if (error is null)
                        return null;

                    var result = new StringBuilder("OAuth token endpoint failure: ");
                    result.Append(error);

                    if (root.TryGetProperty("error_description", out var errorDescription))
                    {
                        result.Append(";Description=");
                        result.Append(errorDescription);
                    }

                    if (root.TryGetProperty("error_uri", out var errorUri))
                    {
                        result.Append(";Uri=");
                        result.Append(errorUri);
                    }

                    var exception = new Exception(result.ToString())
                    {
                        Data =
                        {
                            ["error"] = error.ToString(),
                            ["error_description"] = errorDescription.ToString(),
                            ["error_uri"] = errorUri.ToString()
                        }
                    };

                    return exception;
                }
            }
        }

        protected override string BuildChallengeUrl(AuthenticationProperties properties, string redirectUri)
        {
            var scopeParameter = properties.GetParameter<ICollection<string>>(OAuthChallengeProperties.ScopeKey);
            var scope = scopeParameter != null ? FormatScope(scopeParameter) : FormatScope();
            var parameters = new Dictionary<string, string>
            {
                ["client_id"] = Options.ClientId,
                ["scope"] = scope,
                ["response_type"] = "code",
                ["redirect_uri"] = redirectUri
            };

            if (Options.UsePkce)
            {
                var codeVerifier = RandomUtils.RandomBytes(32).ToBase64SafeUrlString();
                properties.Items.Add(OAuthConstants.CodeVerifierKey, codeVerifier);
                var codeChallenge = codeVerifier.UTF8ToByteArray().Sha256().ToBase64SafeUrlString();

                parameters[OAuthConstants.CodeChallengeKey] = codeChallenge;
                parameters[OAuthConstants.CodeChallengeMethodKey] = OAuthConstants.CodeChallengeMethodS256;
            }
        
            var strJsonState = properties.Items.JsonSerialize().TrimMultiline();
            Response.HttpContext.Session.Set("state", strJsonState.UTF8ToByteArray());
            parameters["state"] = properties.Items.JsonSerialize().TrimMultiline().Keccak256().HexToBase58(); //Options.StateDataFormat.Protect(properties);

            return QueryHelpers.AddQueryString(Options.AuthorizationEndpoint, parameters!);
        }
    }
}
