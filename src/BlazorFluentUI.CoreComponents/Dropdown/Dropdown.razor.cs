﻿using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace BlazorFluentUI
{
    public partial class Dropdown : ResponsiveComponentBase
    {
        [Parameter] public RenderFragment? ChildContent { get; set; }
        [Parameter] public IEnumerable<string>? DefaultSelectedKeys { get; set; }
        [Parameter] public IEnumerable<IDropdownOption>? DefaultSelectedOptions { get; set; }
        [Parameter] public bool Disabled { get; set; }
        [Parameter] public int DropdownWidth { get; set; } = 0;
        [Parameter] public int DropdownHeight { get; set; } = 0;
        [Parameter] public string? ErrorMessage { get; set; }
        [Parameter] public IEnumerable<IDropdownOption>? ItemsSource { get; set; }
        [Parameter] public RenderFragment<IDropdownOption>? ItemTemplate { get; set; }
        [Parameter] public string? Label { get; set; }
        [Parameter] public bool MultiSelect { get; set; }
        [Parameter] public EventCallback<DropdownChangeArgs> OnChange { get; set; }
        [Parameter] public string? Placeholder { get; set; }
        [Parameter] public bool Required { get; set; }
        [Parameter] public ResponsiveMode ResponsiveMode { get; set; }
        //[Parameter] [Obsolete] public string? SelectedKey { get; set; }
        [Parameter] public IDropdownOption? SelectedOption { get; set; }
        [Parameter] public EventCallback<IDropdownOption?> SelectedOptionChanged { get; set; }
        //[Parameter] [Obsolete] public EventCallback<string> SelectedKeyChanged { get; set; }
        //[Parameter] [Obsolete] public List<string> SelectedKeys { get; set; } = new List<string>();
        [Parameter] public IEnumerable<IDropdownOption> SelectedOptions { get; set; } = new List<IDropdownOption>();
        //[Parameter] [Obsolete] public EventCallback<List<string>> SelectedKeysChanged { get; set; }
        [Parameter] public EventCallback<IEnumerable<IDropdownOption>> SelectedOptionsChanged { get; set; }
        [Parameter] public DirectionalHint? DirectionalHint { set; get; }

        [Inject]
        private IJSRuntime? JSRuntime { get; set; }
        private const string BasePath = "./_content/BlazorFluentUI.CoreComponents/baseComponent.js";
        private IJSObjectReference? baseModule;


        [CascadingParameter] EditContext CascadedEditContext { get; set; } = default!;

        private FieldIdentifier FieldIdentifier;

        [Parameter]
        public Expression<Func<IDropdownOption>>? SelectedOptionExpression { get; set; }
        [Parameter]
        public Expression<Func<IEnumerable<IDropdownOption>>>? SelectedOptionsExpression { get; set; }

        protected bool IsOpen { get; set; }

        protected string id = $"id_{Guid.NewGuid().ToString().Replace("-", "")}";
        protected bool isSmall = false;
        protected Rectangle dropDownBounds = new();

        private ElementReference calloutReference;
        private ElementReference panelReference;
        private ElementReference _chosenReference;
        private string? _registrationGuid;

        private FocusZone? calloutFocusZone;
        private CalloutPositionedInfo? _calloutPositionedInfo;

        //private bool firstRender = true;

        #region Style
        private ICollection<IRule> DropdownLocalRules { get; set; } = new List<IRule>();

        private Rule DropdownTitleOpenRule = new();
        private Rule DropdownCalloutRule = new();
        private Rule DropdownCalloutMainRule = new();
        #endregion

        protected override Task OnInitializedAsync()
        {
            CreateLocalCss();
            SetStyle();
            return base.OnInitializedAsync();
        }


        protected override void OnThemeChanged()
        {
            SetStyle();
        }

        public void ResetSelection()
        {
            //SelectedKeys.Clear();
            SelectedOptions = Enumerable.Empty<IDropdownOption>();
            //SelectedKey = null;

            if (MultiSelect)
            {
                if (SelectedOptionsChanged.HasDelegate)
                    SelectedOptionsChanged.InvokeAsync(SelectedOptions);
            }
            else
            {
                if (SelectedOptionChanged.HasDelegate)
                    SelectedOptionChanged.InvokeAsync(SelectedOption);
            }

            if (FieldIdentifier.FieldName != null)
                CascadedEditContext?.NotifyFieldChanged(FieldIdentifier);
            StateHasChanged();
        }


        public void AddSelection(string key)
        {
            IDropdownOption? option = ItemsSource!.FirstOrDefault(x => x.Key == key);
            if (option == null)
                return;

            if (MultiSelect)
            {
                if (SelectedOptions.Contains(option))
                    throw new Exception("This option was already selected somehow.");

                if (OnChange.HasDelegate)
                    OnChange.InvokeAsync(new DropdownChangeArgs(option, true));

                SelectedOptions = SelectedOptions.Append(option).ToList();

                if (SelectedOptionsChanged.HasDelegate)
                    SelectedOptionsChanged.InvokeAsync(SelectedOptions);

            }
            else
            {
                if (SelectedOption != option)
                {
                    SelectedOption = option;
                    if (OnChange.HasDelegate)
                        OnChange.InvokeAsync(new DropdownChangeArgs(option, true));
                    if (SelectedOptionChanged.HasDelegate)
                        SelectedOptionChanged.InvokeAsync(SelectedOption);
                }
                IsOpen = false;
            }

            if (FieldIdentifier.FieldName != null)
                CascadedEditContext?.NotifyFieldChanged(FieldIdentifier);
            StateHasChanged();
        }

        public void RemoveSelection(string key)
        {
            IDropdownOption? option = ItemsSource!.FirstOrDefault(x => x.Key == key);
            if (option == null)
                return;

            if (MultiSelect)
            {
                Console.WriteLine($"Active Count: {SelectedOptions.Count()}");
                if (!SelectedOptions.Contains(option))
                    throw new Exception("This option was not already selected.");

                if (OnChange.HasDelegate)
                    OnChange.InvokeAsync(new DropdownChangeArgs(option, false));

                SelectedOptions = SelectedOptions.Where(x => x != option).ToList();

                if (SelectedOptionsChanged.HasDelegate)
                    SelectedOptionsChanged.InvokeAsync(SelectedOptions);

            }
            else
            {
                if (SelectedOption != null)
                {
                    SelectedOption = null;
                    if (OnChange.HasDelegate)
                        OnChange.InvokeAsync(new DropdownChangeArgs(option, false));

                    if (SelectedOptionChanged.HasDelegate)
                        SelectedOptionChanged.InvokeAsync(SelectedOption);
                }

            }

            if (FieldIdentifier.FieldName != null)
                CascadedEditContext?.NotifyFieldChanged(FieldIdentifier);
            StateHasChanged();
        }



        [JSInvokable]
        public override async Task OnResizedAsync(double windowWidth, double windowHeight)
        {
            Rectangle? oldBounds = dropDownBounds;
            dropDownBounds = await GetBoundsAsync();
            if (oldBounds.Width != dropDownBounds.Width)
            {
                StateHasChanged();
            }
            await base.OnResizedAsync(windowWidth, windowHeight);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (baseModule == null)
                baseModule = await JSRuntime!.InvokeAsync<IJSObjectReference>("import", BasePath);

            if (firstRender)
            {
                GetDropdownBounds();

            }
            if (IsOpen && _registrationGuid == null)
                await RegisterListFocusAsync();

            if (!IsOpen && _registrationGuid != null)
                await DeregisterListFocusAsync();

            await base.OnAfterRenderAsync(firstRender);
        }

        private async void GetDropdownBounds()
        {
            dropDownBounds = await GetBoundsAsync();
        }

        protected override async Task OnParametersSetAsync()
        {
            await base.OnParametersSetAsync();
            if (DefaultSelectedKeys != null)
            {
                foreach (string? key in DefaultSelectedKeys)
                    AddSelection(key);
            }
            if (DefaultSelectedOptions != null)
            {
                foreach (IDropdownOption? option in DefaultSelectedOptions)
                    AddSelection(option.Key!);
            }
            if (ItemTemplate == null)
            {
                ItemTemplate = (item) => (builder) =>
                {
                    builder.OpenComponent<DropdownItem>(0);
                    builder.AddAttribute(1, "Text", item.Text);
                    builder.AddAttribute(2, "Key", item.Key);
                    builder.AddAttribute(3, "ItemType", item.ItemType);
                    builder.AddAttribute(4, "Disabled", item.Disabled);
                    builder.AddAttribute(5, "Hidden", item.Hidden);
                    builder.CloseComponent();
                };
            }

            if (CascadedEditContext != null && (SelectedOptionExpression != null || SelectedOptionsExpression != null))
            {
                if (SelectedOptionExpression != null)
                    FieldIdentifier = FieldIdentifier.Create<IDropdownOption>(SelectedOptionExpression);
                else
                    FieldIdentifier = FieldIdentifier.Create<IEnumerable<IDropdownOption>>(SelectedOptionsExpression!);

                //CascadedEditContext?.NotifyFieldChanged(FieldIdentifier);
                CascadedEditContext!.OnValidationStateChanged += CascadedEditContext_OnValidationStateChanged;
            }
        }

        private void CascadedEditContext_OnValidationStateChanged(object? sender, ValidationStateChangedEventArgs e)
        {
            InvokeAsync(() => StateHasChanged());  //invokeasync required for serverside
        }

        private async Task RegisterListFocusAsync()
        {
            if (_registrationGuid != null)
            {
                await DeregisterListFocusAsync();
            }
            if ((int)CurrentMode <= (int)ResponsiveMode.Medium)
                _chosenReference = panelReference;
            else
                _chosenReference = calloutReference;

            _registrationGuid = $"id_{Guid.NewGuid().ToString().Replace("-", "")}";
            await baseModule!.InvokeVoidAsync("registerKeyEventsForList", _chosenReference, _registrationGuid);
        }

        private async Task DeregisterListFocusAsync()
        {
            if (_registrationGuid != null)
            {
                await baseModule!.InvokeVoidAsync("deregisterKeyEventsForList", _registrationGuid);
            }
        }

        private void OnPositioned(CalloutPositionedInfo calloutPositionedInfo)
        {
            _calloutPositionedInfo = calloutPositionedInfo;
            SetStyle();
            calloutFocusZone!.FocusFirstElement();
        }

        private Task KeydownHandler(KeyboardEventArgs args)
        {
            bool containsExpandCollapseModifier = args.AltKey || args.MetaKey;
            switch (args.Key)
            {
                case "Enter":
                case " ":
                    IsOpen = !IsOpen;
                    break;
                case "Escape":
                    IsOpen = false;
                    break;
                case "ArrowDown":
                    if (containsExpandCollapseModifier)
                    {
                        IsOpen = true;
                    }
                    break;
            }
            return Task.CompletedTask;
        }

        protected Task ClickHandler(MouseEventArgs args)
        {
            if (!Disabled)
                IsOpen = !IsOpen;  //There is a problem here.  Clicking when open causes automatic dismissal (light dismiss) so this just opens it again.
            return Task.CompletedTask;
        }
        protected static Task FocusHandler(FocusEventArgs args)
        {
            // Could write logic to open on focus automatically...
            //isOpen = true;
            return Task.CompletedTask;
        }

        protected void DismissHandler()
        {
            IsOpen = false;
        }

        private void CreateLocalCss()
        {
            DropdownTitleOpenRule.Selector = new CssStringSelector() { SelectorName = ".ms-Dropdown.is-open .ms-Dropdown-title" };
            DropdownCalloutRule.Selector = new ClassSelector() { SelectorName = "ms-Dropdown-callout" };
            DropdownCalloutMainRule.Selector = new CssStringSelector() { SelectorName = ".ms-Dropdown-callout .ms-Callout-main" };

            DropdownLocalRules.Add(DropdownTitleOpenRule);
            DropdownLocalRules.Add(DropdownCalloutRule);
            DropdownLocalRules.Add(DropdownCalloutMainRule);
        }

        private void SetStyle()
        {
            DropdownTitleOpenRule.Properties = new CssString()
            {
                Css = $"border-radius:{(_calloutPositionedInfo?.TargetEdge == RectangleEdge.Bottom ? $"{Theme?.Effects.RoundedCorner2} {Theme?.Effects.RoundedCorner2} 0 0" : $"0 0 {Theme?.Effects.RoundedCorner2} {Theme?.Effects.RoundedCorner2}")};"
            };
            DropdownCalloutRule.Properties = new CssString()
            {
                Css = $"border-radius:{(_calloutPositionedInfo?.TargetEdge == RectangleEdge.Bottom ? $"0 0 {Theme?.Effects.RoundedCorner2} {Theme?.Effects.RoundedCorner2}" : $"{Theme?.Effects.RoundedCorner2} {Theme?.Effects.RoundedCorner2} 0 0")};"
            };
            DropdownCalloutMainRule.Properties = new CssString()
            {
                Css = $"border-radius:{(_calloutPositionedInfo?.TargetEdge == RectangleEdge.Bottom ? $"0 0 {Theme?.Effects.RoundedCorner2} {Theme?.Effects.RoundedCorner2}" : $"{Theme?.Effects.RoundedCorner2} {Theme?.Effects.RoundedCorner2} 0 0")};"
            };


        }



    }
}
