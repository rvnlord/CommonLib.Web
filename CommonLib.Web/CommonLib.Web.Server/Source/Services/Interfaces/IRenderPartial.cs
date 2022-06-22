namespace CommonLib.Web.Server.Source.Services.Interfaces
{
    public interface IRenderPartial
    {
        Task<string> RenderPartialAsync(string partialName);
        Task<string> RenderPartialAsync<TModel>(string partialName, TModel model);
        Task<string> RenderPageAsync(string pageName);
        Task<string> RenderPageAsync<TModel>(string pageName, TModel model);
    }
}