﻿using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace BlazorFluentUI
{
    // Blazor doesn't have a React equivalent to "createPortal" so to use a Layer, you need to place
    // a LayerHost manually near the root of the app.  This will allow you to use a CascadeParameter
    // to send the LayerHost to anywhere in the app and render items to it.

    public class Layer : FluentUIComponentBase, IAsyncDisposable
    {
        [Inject] private IJSRuntime? JSRuntime { get; set; }
        private const string BasePath = "./_content/BlazorFluentUI.CoreComponents/baseComponent.js";
        private IJSObjectReference? baseModule;

        //private const string PanelPath = "./_content/BlazorFluentUI.CoreComponents/panel.js";
        //private IJSObjectReference? panelModule;


        [Inject] private LayerHostService? LayerHostService { get; set; }

        [Parameter] public RenderFragment? ChildContent { get; set; }
        [Parameter] public string? HostId { get; set; }

        //[CascadingParameter(Name = "HostedContent")] protected LayerHost? LayerHost { get; set; }
        private LayerHost? LayerHost { get; set; }

        private bool addedToHost = false;

        public string id = $"id_{Guid.NewGuid().ToString().Replace("-","")}";
        private ElementReference _element;

        //private bool isFirstRendered = false;

        protected override async Task OnParametersSetAsync()
        {
            if (!addedToHost)
            {
                if (HostId == null)
                {
                    LayerHost = LayerHostService?.GetDefaultHost();
                }
                else
                {
                    LayerHost = LayerHostService?.GetHost(HostId);
                }

                if (LayerHost != null)
                {
                    await LayerHost.AddOrUpdateHostedContentAsync(id, ChildContent)!;
                    addedToHost = true;
                }
            }
            await base.OnParametersSetAsync();
        }

        protected override bool ShouldRender()
        {
            if (LayerHost != null)
            {
                LayerHost.AddOrUpdateHostedContentAsync(id, ChildContent);
                addedToHost = true;
            }
            return true;
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "span");
            builder.AddAttribute(1, "class", "ms-layer");
            builder.AddAttribute(2, "style", Style);
            builder.AddAttribute(3, "data-layer-id", id);
            builder.AddElementReferenceCapture(4, element => _element = element);
            builder.CloseElement();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (baseModule == null)
                baseModule = await JSRuntime!.InvokeAsync<IJSObjectReference>("import", cancellationTokenSource.Token, BasePath);

            if (firstRender)
            {
                //isFirstRendered = true;
                if (!addedToHost)
                {
                    if (HostId == null)
                    {
                        LayerHost = LayerHostService?.GetDefaultHost();
                    }
                    else
                    {
                        LayerHost = LayerHostService?.GetHost(HostId);
                    }

                    if (LayerHost != null)
                    {
                        await LayerHost.AddOrUpdateHostedContentAsync(id, ChildContent)!;
                        addedToHost = true;
                        StateHasChanged();
                    }
                }

                await baseModule!.InvokeVoidAsync("addOrUpdateVirtualParent", cancellationTokenSource.Token, _element);
            }
            await base.OnAfterRenderAsync(firstRender);
        }

        public override async ValueTask DisposeAsync()
        {
            try
            {
                if (LayerHost != null)
                    await LayerHost.RemoveHostedContentAsync(id)!;
                addedToHost = false;
                if (baseModule != null && !cancellationTokenSource.IsCancellationRequested)
                    await baseModule.DisposeAsync();
            }
            catch (TaskCanceledException)
            {
            }
        }
    }
}
