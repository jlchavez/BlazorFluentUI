﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace BlazorFluentUI
{

    public partial class FocusZone : FluentUIComponentBase, IAsyncDisposable
    {
        [Inject] private IJSRuntime? JSRuntime { get; set; }
        private const string ScriptPath = "./_content/BlazorFluentUI.CoreComponents/focusZone.js";
        private IJSObjectReference? scriptModule;
        private const string BasePath = "./_content/BlazorFluentUI.CoreComponents/baseComponent.js";
        private IJSObjectReference? baseModule;

        [Parameter] public bool AllowFocusRoot { get => allowFocusRoot; set { if (value != allowFocusRoot) { updateFocusZone = true; allowFocusRoot = value; } } }
        //[Parameter] public ComponentBase As { get; set; }
        [Parameter] public bool CheckForNoWrap { get => checkForNoWrap; set { if (value != checkForNoWrap) { updateFocusZone = true; checkForNoWrap = value; } } }
        [Parameter] public RenderFragment? ChildContent { get; set; }
        [Parameter] public string? DefaultActiveElement { get => defaultActiveElement; set { if (value != defaultActiveElement) { updateFocusZone = true; defaultActiveElement = value; } } }
        [Parameter] public FocusZoneDirection Direction { get => direction; set { if (value != direction) { updateFocusZone = true; direction = value; } } }
        [Parameter] public bool Disabled { get => disabled; set { if (value != disabled) { updateFocusZone = true; disabled = value; } } }
        [Parameter] public bool DoNotAllowFocusEventToPropagate { get => doNotAllowFocusEventToPropagate; set { if (value != doNotAllowFocusEventToPropagate) { updateFocusZone = true; doNotAllowFocusEventToPropagate = value; } } }
        [Parameter] public FocusZoneTabbableElements HandleTabKey { get => handleTabKey; set { if (value != handleTabKey) { updateFocusZone = true; handleTabKey = value; } } }
        [Parameter] public bool IsCircularNavigation { get => isCircularNavigation; set { if (value != isCircularNavigation) { updateFocusZone = true; isCircularNavigation = value; } } }
        [Parameter] public List<ConsoleKey>? InnerZoneKeystrokeTriggers { get => innerZoneKeystrokeTriggers; set { if (value != innerZoneKeystrokeTriggers) { updateFocusZone = true; innerZoneKeystrokeTriggers = value; } } }
        [Parameter] public EventCallback OnActiveElementChanged { get; set; }
        [Parameter] public Func<bool>? OnBeforeFocus { get => onBeforeFocus; set { if (value != onBeforeFocus) { updateFocusZone = true; onBeforeFocus = value; } } }   // This is likely not having an effect because of asynchronous code allowing the event to propagate.

        [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }
        [Parameter] public EventCallback OnFocusNotification { get; set; }

        [Parameter] public string Role { get; set; } = "presentation";
        [Parameter] public Func<bool>? ShouldInputLoseFocusOnArrowKey { get => shouldInputLoseFocusOnArrowKey; set { if (value != shouldInputLoseFocusOnArrowKey) { updateFocusZone = true; shouldInputLoseFocusOnArrowKey = value; } } } // This is likely not having an effect because of asynchronous code allowing the event to propagate.
        [Parameter] public bool IsFocusable { get; set; }

        [Parameter(CaptureUnmatchedValues = true)] public Dictionary<string, object>? UnknownAttributes { get; set; }


        bool allowFocusRoot;
        bool checkForNoWrap;
        string? defaultActiveElement;
        FocusZoneDirection direction;
        bool disabled;
        bool doNotAllowFocusEventToPropagate;
        FocusZoneTabbableElements handleTabKey;
        bool isCircularNavigation;
        List<ConsoleKey>? innerZoneKeystrokeTriggers;
        Func<bool>? onBeforeFocus;
        Func<bool>? shouldInputLoseFocusOnArrowKey;

        bool updateFocusZone = false;

        public async Task Focus()
        {
            await RootElementReference.FocusAsync();
        }

        public async void FocusFirstElement()
        {
            if (baseModule != null)
                await baseModule.InvokeVoidAsync("focusFirstElementChild", RootElementReference);
        }

        protected string Id = $"id_{Guid.NewGuid().ToString().Replace("-", "")}";
        private DotNetObjectReference<FocusZone>? selfReference;

        //private int[] _lastIndexPath;
        //private bool _jsAvailable;
        //private int _registrationId = -1;

        //private readonly Task<int>? _registrationTask = null;

        //public event PropertyChangedEventHandler? PropertyChanged;
        //private bool parametersUpdated = false;

        protected override Task OnInitializedAsync()
        {
            Direction = FocusZoneDirection.Bidirectional;
            return base.OnInitializedAsync();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (baseModule == null)
                baseModule = await JSRuntime!.InvokeAsync<IJSObjectReference>("import", BasePath);
            if (scriptModule == null)
                scriptModule = await JSRuntime!.InvokeAsync<IJSObjectReference>("import", ScriptPath);

            selfReference = DotNetObjectReference.Create(this);

            if (firstRender)
            {
                FocusZoneProps? props = FocusZoneProps.GenerateProps(this, Id); //, RootElementReference);
                await scriptModule.InvokeVoidAsync("registerFocusZone", selfReference, RootElementReference, props);

                updateFocusZone = false;
            }
            else
            {
                if ( updateFocusZone)
                {
                    updateFocusZone = false;
                    await UpdateFocusZoneAsync();
                }
            }
            await base.OnAfterRenderAsync(firstRender);
        }


        protected override async Task OnParametersSetAsync()
        {

            await base.OnParametersSetAsync();
        }

        private async Task UpdateFocusZoneAsync()
        {
            try
            {
                FocusZoneProps? props = FocusZoneProps.GenerateProps(this, Id);
                await scriptModule!.InvokeVoidAsync("updateFocusZoneProps", selfReference, props);
            }
            catch (TaskCanceledException)
            {

            }
        }




        private async Task UnregisterFocusZoneAsync()
        {
            try
            {
                await scriptModule!.InvokeVoidAsync("unregisterFocusZone", selfReference);
            }
            catch (TaskCanceledException)
            {
            }
        }

        [JSInvokable]
        public bool JSOnBeforeFocus()
        {
            return OnBeforeFocus!();
        }

        [JSInvokable]
        public bool JSShouldInputLoseFocusOnArrowKey()
        {
            return ShouldInputLoseFocusOnArrowKey!();
        }

        [JSInvokable]
        public void JSOnFocusNotification()
        {
            OnFocusNotification.InvokeAsync(null);
        }

        [JSInvokable]
        public void JSOnActiveElementChanged()
        {
            OnActiveElementChanged.InvokeAsync(null);
        }


        //public async void Dispose()
        //{
        //    if (_registrationId != -1)
        //    {
        //        //Debug.WriteLine("Trying to unregister focuszone");
        //        UnregisterFocusZoneAsync();

        //    }
        //}

        public override async ValueTask DisposeAsync()
        {
            try
            {
                if (scriptModule != null)
                {
                    await scriptModule!.SafeInvokeVoidAsync("unregisterFocusZone", selfReference);
                    await scriptModule.SafeDisposeAsync();
                }
                if (baseModule != null)
                    await baseModule.SafeDisposeAsync();

                selfReference?.Dispose();

                await base.DisposeAsync();
            }
            catch (TaskCanceledException)
            {
            }

        }
    }
}
