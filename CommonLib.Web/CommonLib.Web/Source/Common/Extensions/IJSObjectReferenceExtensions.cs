using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace CommonLib.Web.Source.Common.Extensions
{
    public static class IJSObjectReferenceExtensions
    {
        public static async ValueTask InvokeVoidAndCatchCancellationAsync(this IJSObjectReference jsObjectReference, string identifier, params object[] args)
        {
            try
            {
                if (jsObjectReference is not null)
                    await jsObjectReference.InvokeVoidAsync(identifier, args);
            }
            catch (Exception ex) when (ex is TaskCanceledException or JSDisconnectedException) { }
        }

        public static ValueTask<TValue> InvokeAndCatchCancellationAsync<TValue>(this IJSObjectReference jsObjectReference, string identifier, params object[] args)
        {
            try
            {
                return jsObjectReference.InvokeAsync<TValue>(identifier, args);
            }
            catch (Exception ex) when (ex is TaskCanceledException or JSDisconnectedException)
            {
                return new ValueTask<TValue>();
            }
        }
        
        public static ValueTask<TValue> InvokeAndCatchCancellationAsync<TValue>(this IJSObjectReference jsObjectReference, string identifier, CancellationToken cancellationToken, params object[] args)
        {
            try
            {
                return jsObjectReference.InvokeAsync<TValue>(identifier, cancellationToken, args);
            }
            catch (Exception ex) when (ex is TaskCanceledException or JSDisconnectedException)
            {
                return new ValueTask<TValue>();
            }
        }
        
        public static ValueTask InvokeVoidAndCatchCancellationAsync(this IJSObjectReference jsObjectReference, string identifier, CancellationToken cancellationToken, params object[] args)
        {
            try
            {
                return jsObjectReference.InvokeVoidAsync(identifier, cancellationToken, args);
            }
            catch (Exception ex) when (ex is TaskCanceledException or JSDisconnectedException)
            {
                return new ValueTask();
            }
        }
        
        public static ValueTask<TValue> InvokeAndCatchCancellationAsync<TValue>(this IJSObjectReference jsObjectReference, string identifier, TimeSpan timeout, params object[] args)
        {
            try
            {
                return jsObjectReference.InvokeAsync<TValue>(identifier, timeout, args);
            }
            catch (Exception ex) when (ex is TaskCanceledException or JSDisconnectedException)
            {
                return new ValueTask<TValue>();
            }
        }
        
        public static ValueTask InvokeVoidAndCatchCancellationAsync(this IJSObjectReference jsObjectReference, string identifier, TimeSpan timeout, params object[] args)
        {
            try
            {
                return jsObjectReference.InvokeVoidAndCatchCancellationAsync(identifier, timeout, args);
            }
            catch (Exception ex) when (ex is TaskCanceledException or JSDisconnectedException)
            {
                return new ValueTask();
            }
        }
    }
}
