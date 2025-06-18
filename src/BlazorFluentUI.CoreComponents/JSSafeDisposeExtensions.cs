using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace BlazorFluentUI
{
    public static class JSSafeDisposeExtensions
    {
        public static async Task SafeDisposeAsync(this IJSObjectReference? jsObjectReference)
        {
            if (jsObjectReference == null)
                return;
            try
            {
                await jsObjectReference.DisposeAsync();
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public static async Task SafeInvokeVoidAsync(this IJSObjectReference jsRuntime, string identifier, params object[] args)
        {
            try
            {
                await jsRuntime.InvokeVoidAsync(identifier, args);
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}