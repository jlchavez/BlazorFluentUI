﻿using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace BlazorFluentUI
{
    public partial class Panel : FluentUIComponentBase, IAsyncDisposable
    {
        [Inject]
        private IJSRuntime? JSRuntime { get; set; }
        private const string BasePath = "./_content/BlazorFluentUI.CoreComponents/baseComponent.js";
        private IJSObjectReference? baseModule;

        private const string ScriptPath = "./_content/BlazorFluentUI.CoreComponents/panel.js";
        private IJSObjectReference? scriptModule;


        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        [Parameter]
        public string? CloseButtonAriaLabel { get; set; }

        [Parameter]
        public string? CustomWidth { get; set; }

        [Parameter]
        public ElementReference ElementToFocusOnDismiss { get; set; }

        [Parameter]
        public string? FirstFocusableSelector { get; set; }

        [Parameter]
        public RenderFragment? FooterTemplate { get; set; }

        [Parameter]
        public bool ForceFocusInsideTrap { get; set; }

        [Parameter]
        public bool HasCloseButton { get; set; } = true;

        [Parameter]
        public string? HeaderClassName { get; set; }

        [Parameter]
        public RenderFragment? HeaderTemplate { get; set; }

        [Parameter]
        public string? HeaderText { get; set; }

        [Parameter]
        public string? HostId { get; set; }

        [Parameter]
        public bool IsBlocking { get; set; } = true;

        [Parameter]
        public bool IsFooterAtBottom { get; set; } = false;

        [Parameter]
        public bool IsHiddenOnDismiss { get; set; } = false;

        [Parameter]
        public bool IsLightDismiss { get; set; } = false;

        [Parameter]
        public bool IsOpen { get; set; }

        [Parameter]
        public RenderFragment? NavigationTemplate { get; set; }

        [Parameter]
        public RenderFragment? NavigationContentTemplate { get; set; }

        [Parameter]
        public EventCallback OnDismiss { get; set; }

        [Parameter]
        public EventCallback OnDismissed { get; set; }

        [Parameter]
        public EventCallback OnLightDismissClick { get; set; }

        [Parameter]
        public EventCallback OnOpen { get; set; }

        [Parameter]
        public EventCallback OnOpened { get; set; }

        [Parameter]
        public EventCallback OnOuterClick { get; set; }

        [Parameter]
        public PanelType Type { get; set; } = PanelType.SmallFixedFar;

        private bool isAnimating = false;
        private bool animationRenderStart = false;

        private EventCallback ThrowawayCallback { get; set; }

        private PanelVisibilityState previousVisibility = PanelVisibilityState.Closed;
        private PanelVisibilityState currentVisibility = PanelVisibilityState.Closed;
        private bool isFooterSticky = false;

        private Action? onPanelClick;
        private Action? _dismiss;
        private List<int> _scrollerEventId = new();
        private int _resizeId = -1;
        private int _mouseDownId = -1;

        private Timer? _animationTimer;
        private Action? _clearExistingAnimationTimer;
        private Action<PanelVisibilityState>? _animateTo;
        private Action? _onTransitionComplete;

        private ElementReference panelElement;
        private ElementReference scrollableContent;
        private bool _scrollerRegistered;

        private ElapsedEventHandler? _handler = null;

        private bool _jsAvailable = false;

        private static string _headerId = "";
        private DotNetObjectReference<Panel>? selfReference;

        public Panel()
        {
            HeaderTemplate = builder =>
            {
                if (HeaderText != null)
                {
                    if (string.IsNullOrEmpty(_headerId))
                        _headerId = "".GetRandomHashCodeString();
                    builder.OpenElement(0, "div");
                    {
                        builder.AddAttribute(1, "class", "ms-Panel-header");
                        builder.OpenElement(2, "p");
                        {
                            builder.AddAttribute(3, "class", "xlargeFont ms-Panel-headerText");
                            builder.AddAttribute(4, "id", _headerId);
                            builder.AddAttribute(5, "role", "heading");
                            builder.AddAttribute(6, "aria-level", "2");
                            builder.AddContent(7, HeaderText);
                        }
                        builder.CloseElement();
                    }
                    builder.CloseElement();
                }
            };

            onPanelClick = () =>
            {
                _dismiss!();
            };

            _dismiss = () =>
            {
                OnDismiss.InvokeAsync(null);
                //normally, would check react synth events to see if event was interrupted from the OnDismiss callback before calling the following...
                // To Do
                Close();
            };



        }

        //Task OnPanelClick()
        //{
        //    this.dismiss();
        //    return Task.CompletedTask;
        //}

        [JSInvokable]
        public async Task UpdateFooterPositionAsync()
        {
            //Debug.WriteLine("Calling UpdateFooterPositionAsync");
            double clientHeight = await baseModule!.InvokeAsync<double>("getClientHeight", scrollableContent);
            double scrollHeight = await baseModule!.InvokeAsync<double>("getScrollHeight", scrollableContent);

            if (clientHeight < scrollHeight)
                isFooterSticky = true;
            else
                isFooterSticky = false;
        }

        [JSInvokable]
        public async Task DismissOnOuterClick(bool contains)
        {
            if (IsActive())
            {
                //Debug.WriteLine("Calling DismissOnOuterClick");
                if (!contains)
                {
                    //Debug.WriteLine("Contains is false!");
                    if (OnOuterClick.HasDelegate)
                    {
                        await OnOuterClick.InvokeAsync(null);
                        //need to prevent default for bubbling maybe.  Test with lightdismiss ...
                    }
                    else
                    {
                        _dismiss!();
                    }
                }
            }
        }

        public static void Open()
        {
            //ignore these calls if we have isOpen set... isOpen need to be nullable in this case...
            // To Do

        }

        public static void Close()
        {

            //ignore these calls if we have isOpen set... isOpen need to be nullable in this case...
            // To Do
        }

        private string GetTypeCss()
        {
            return Type switch
            {
                PanelType.SmallFixedNear => " ms-Panel--smLeft",
                PanelType.SmallFixedFar => " ms-Panel--sm",
                PanelType.SmallFluid => " ms-Panel--smFluid",
                PanelType.Medium => " ms-Panel--md",
                PanelType.Large => " ms-Panel--lg",
                PanelType.LargeFixed => " ms-Panel--fixed",
                PanelType.ExtraLarge => " ms-Panel--xl",
                PanelType.Custom => " ms-Panel--custom",
                PanelType.CustomNear => " ms-Panel--customLeft",
                _ => "",
            };
        }

        protected string GetMainAnimation()
        {
            bool isOnRightSide = true;
            switch (Type)
            {
                // this changes in RTL env, To Do
                case PanelType.SmallFixedNear:
                case PanelType.CustomNear:
                    isOnRightSide = false;
                    break;
            }
            if (IsOpen && isAnimating && !isOnRightSide)
                return " slideRightIn40";
            if (IsOpen && isAnimating && isOnRightSide)
                return " slideLeftIn40";
            if (!IsOpen && isAnimating && !isOnRightSide)
                return " slideLeftOut40";
            if (!IsOpen && isAnimating && isOnRightSide)
                return " slideRightOut40";
            return "";
        }

        private async Task SetRegistrationsAsync()
        {
            //if (ShouldListenForOuterClick())
            //{
            //    _mouseDownId = await baseModule!.InvokeAsync<int>("registerMouseDownHandler", panelElement, DotNetObjectReference.Create(this));
            //}

            selfReference = DotNetObjectReference.Create(this);
            if (ShouldListenForOuterClick() && _mouseDownId == -1)
            {
                _mouseDownId = -2;
                _mouseDownId = await scriptModule!.InvokeAsync<int>("registerMouseDownHandler", panelElement, selfReference);
            }
            else if (!ShouldListenForOuterClick() && _mouseDownId > -1)
            {
                await scriptModule!.InvokeVoidAsync("unregisterHandler", _mouseDownId);
            }

            if (IsOpen && _resizeId == -1)
            {
                _resizeId = -2;
                _resizeId = await scriptModule!.InvokeAsync<int>("registerSizeHandler", selfReference);
                //listen for lightdismiss
            }
            else if (!IsOpen && _resizeId > -1)
            {
                await scriptModule!.InvokeVoidAsync("unregisterHandler", _resizeId);
            }

            //if (IsOpen && !_scrollerRegistered)
            //{
            //    _scrollerRegistered = true;
            //    _scrollerEventId = await baseModule!.InvokeAsync<List<int>>("makeElementScrollAllower", scrollableContent);
            //}

            if (!IsOpen && _scrollerRegistered)
            {
                List<int>? copied = _scrollerEventId.ToList();
                _scrollerEventId.Clear();
                _scrollerRegistered = false;

                foreach (int id in copied)
                {
                    await scriptModule!.InvokeVoidAsync("unregisterHandler", id);
                }
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (baseModule == null)
                baseModule = await JSRuntime!.InvokeAsync<IJSObjectReference>("import", BasePath);
            if (scriptModule == null)
                scriptModule = await JSRuntime!.InvokeAsync<IJSObjectReference>("import",  ScriptPath);

            if (firstRender)
            {
                _jsAvailable = true;
                // register timers here so that pre-rendered Panels don't have timers running.
                _animationTimer = new Timer();

                _clearExistingAnimationTimer = () =>
                {
                    if (_animationTimer.Enabled)
                    {
                        _animationTimer.Stop();
                        _animationTimer.Elapsed -= _handler;
                    }
                };

                _animateTo = (animationState) =>
                {
                    Debug.WriteLine($"Animating to {animationState}");
                    _animationTimer.Interval = 200;
                    _handler = null;
                    _handler = (s, e) =>
                    {
                        _animationTimer.Stop();
                        _animationTimer.Elapsed -= _handler;
                        Debug.WriteLine($"Inside invokeAsync from animateTo timer elapsed.");
                        //InvokeAsync(() =>
                        //{
                        previousVisibility = currentVisibility;
                        currentVisibility = animationState;
                        _onTransitionComplete!();
                        //});
                    };
                    _animationTimer.Elapsed += _handler;
                    _animationTimer.Start();
                };

                _onTransitionComplete = async () =>
                {
                    isAnimating = false;
                    //StateHasChanged();
                    await UpdateFooterPositionAsync();

                    if (currentVisibility == PanelVisibilityState.Open)
                    {
                        await OnOpened.InvokeAsync(null);
                    }
                    if (currentVisibility == PanelVisibilityState.Closed)
                    {
                        await OnDismissed.InvokeAsync(null);
                    }
                    await InvokeAsync(StateHasChanged);
                };
            }
            await SetRegistrationsAsync();

            await base.OnAfterRenderAsync(firstRender);
        }


        protected override async Task OnParametersSetAsync()
        {
            previousVisibility = currentVisibility;

            if (!OnLightDismissClick.HasDelegate)
            {
                OnLightDismissClick = OnDismiss; //OnLightDismissClick. //= EventCallback.Factory.Create(this, onPanelClick);
            }

            if (IsOpen && (currentVisibility == PanelVisibilityState.Closed || currentVisibility == PanelVisibilityState.AnimatingClosed))
            {
                currentVisibility = PanelVisibilityState.AnimatingOpen;
            }
            if (!IsOpen && (currentVisibility == PanelVisibilityState.Open || currentVisibility == PanelVisibilityState.AnimatingOpen))
            {
                currentVisibility = PanelVisibilityState.AnimatingClosed;
                // This StateHasChanged call was added because using a custom close button in NavigationTemplate did not cause a state change to occur.
                // The result was that the animation class would not get added and the close transition would not show.  This is a hack to make it work.
                StateHasChanged();
            }

            //Debug.WriteLine($"Was: {previousVisibility}  Current:{currentVisibility}");

            if (_jsAvailable)
            {
                if (currentVisibility != previousVisibility)
                {
                    Debug.WriteLine("Clearing animation timer");
                    _clearExistingAnimationTimer!();
                    if (currentVisibility == PanelVisibilityState.AnimatingOpen)
                    {
                        isAnimating = true;
                        animationRenderStart = true;
                        _animateTo!(PanelVisibilityState.Open);
                    }
                    else if (currentVisibility == PanelVisibilityState.AnimatingClosed)
                    {
                        isAnimating = true;
                        //animationRenderStart = true;
                        _animateTo!(PanelVisibilityState.Closed);
                    }
                }

                //await SetRegistrationsAsync();
            }


            await base.OnParametersSetAsync();
        }

        protected override bool ShouldRender()
        {
            if (isAnimating && !animationRenderStart)
                return false;
            else
            {
                animationRenderStart = false;
                return true;
            }
            //return base.ShouldRender();
        }

        private bool IsActive()
        {
            return currentVisibility == PanelVisibilityState.Open || currentVisibility == PanelVisibilityState.AnimatingOpen;
        }

        private bool ShouldListenForOuterClick()
        {
            return IsBlocking && IsOpen;
        }

        public override async ValueTask DisposeAsync()
        {
            _clearExistingAnimationTimer?.Invoke();

            try
            {
                if (scriptModule != null && baseModule != null)
                {
                    if (_scrollerEventId != null)
                    {
                        foreach (int id in _scrollerEventId)
                        {
                            await scriptModule.InvokeVoidAsync("unregisterHandler", id);
                        }
                        _scrollerEventId.Clear();
                    }

                    if (_resizeId != -1)
                    {
                        await scriptModule.InvokeVoidAsync("unregisterHandler", _resizeId);
                    }
                    if (_mouseDownId != -1)
                    {
                        await scriptModule.InvokeVoidAsync("unregisterHandler", _mouseDownId);
                    }

                    await scriptModule.SafeDisposeAsync();
                    await baseModule.SafeDisposeAsync();
                }
                selfReference?.Dispose();

                await base.DisposeAsync();
            }
            catch (TaskCanceledException)
            {
            }
        }
    }

    internal class PanelWidth
    {
        public static string Full => "100%";
        public static string Auto => "auto";
        public static int Xs => 272;
        public static int Sm => 340;
        public static int Md1 => 592;
        public static int Md2 => 644;
        public static int Lg => 940;
    }

    internal class PanelMargin
    {
        public static string Auto => "auto";
        public static int None => 0;
        public static int Md => 48;
        public static int Lg => 428;
        public static int Xl => 176;
    }
}
