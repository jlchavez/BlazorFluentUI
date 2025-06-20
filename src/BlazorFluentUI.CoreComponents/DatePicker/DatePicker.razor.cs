﻿using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace BlazorFluentUI
{
    public partial class DatePicker : FluentUIComponentBase
    {
        [Parameter] public bool AllFocusable { get; set; } = false;
        [Parameter] public bool AllowTextInput { get; set; } = false;
        [Parameter] public bool Borderless { get; set; } = false;
        [Parameter] public DateTimeFormatter DateTimeFormatter { get; set; } = new DateTimeFormatter();
        [Parameter] public bool DisableAutoFocus { get; set; } = false;
        [Parameter] public bool Disabled { get; set; } = false;
        [Parameter] public DayOfWeek FirstDayOfWeek { get; set; } = DayOfWeek.Sunday;
        [Parameter] public FirstWeekOfYear FirstWeekOfYear { get; set; } = FirstWeekOfYear.FirstFullWeek;
        [Parameter] public Func<DateTime, string> FormatDate { get; set; } = (date) => date.ToString("D");
        [Parameter] public bool HighlightCurrentMonth { get; set; } = false;
        [Parameter] public bool HighlightSelectedMonth { get; set; } = false;
        [Parameter] public DateTime InitialPickerDate { get; set; } = DateTime.MinValue;
        [Parameter] public bool IsMonthPickerVisible { get; set; } = true;
        [Parameter] public bool IsTimePickerVisible { get; set; }
        [Parameter] public string IsOutOfBoundsErrorMessage { get; set; } = "Date must be between {0}-{1}";
        [Parameter] public string InvalidInputErrorMessage { get; set; } = "Invalid date format.";
        [Parameter] public bool IsRequired { get; set; } = false;
        [Parameter] public string IsRequiredErrorMessage { get; set; } = "Field is required.";
        [Parameter] public string? Label { get; set; }
        [Parameter] public DateTime MaxDate { get; set; } = DateTime.MaxValue;
        [Parameter] public DateTime MinDate { get; set; } = DateTime.MinValue;
        [Parameter] public EventCallback OnAfterMenuDismiss { get; set; }
        [Parameter] public EventCallback<DateTime?> OnSelectDate { get; set; }
        [Parameter]
        public Func<string, DateTime> ParseDateFromString { get; set; } = text =>
        {
            DateTime date = DateTime.MinValue;
            bool success = DateTime.TryParse(text, out date);
            return date;
        };
        [Parameter] public string? PickerAriaLabel { get; set; }
        [Parameter] public string? Placeholder { get; set; }
        [Parameter] public bool ShowCloseButton { get; set; }
        [Parameter] public bool ShowGoToToday { get; set; }
        [Parameter] public bool ShowMonthPickerAsOverlay { get; set; }
        [Parameter] public bool ShowWeekNumbers { get; set; }
        [Parameter] public int TabIndex { get; set; }
        [Parameter] public DateTime Today { get; set; } = DateTimeOffset.Now.Date;
        [Parameter] public bool Underlined { get; set; } = false;
        [Parameter] public DateTime? Value { get; set; } //= DateTime.MinValue;
        [Parameter] public EventCallback<DateTime?> ValueChanged { get; set; }


        // State
        protected string? ErrorMessage = null;
        [Parameter] public bool IsDatePickerShown { get; set; } = false;
        protected string FormattedDate = "";
        protected DateTime? SelectedDate; //= DateTime.MinValue;


        protected string? calloutId;
        protected ElementReference datePickerDiv;
        protected TextField? textFieldComponent;
        //protected string id = $"id_{Guid.NewGuid().ToString().Replace("-","")}";

        private bool _preventFocusOpeningPicker = false;
        private bool _oldIsDatePickerShown;

        [CascadingParameter] EditContext CascadedEditContext { get; set; } = default!;

        private FieldIdentifier FieldIdentifier;

        [Parameter]
        public Expression<Func<DateTime?>>? ValueExpression { get; set; }

        protected override Task OnInitializedAsync()
        {
            ErrorMessage = IsRequired && (Value == DateTime.MinValue) ? IsRequiredErrorMessage : null;
            return base.OnInitializedAsync();
        }

        public override Task SetParametersAsync(ParameterView parameters)
        {
            _oldIsDatePickerShown = IsDatePickerShown;

            if (!parameters.TryGetValue("MinDate", out DateTime nextMinDate))
                nextMinDate = MinDate;
            if (!parameters.TryGetValue("MaxDate", out DateTime nextMaxDate))
                nextMaxDate = MaxDate;
            if (!parameters.TryGetValue("Value", out DateTime? nextValue))
                nextValue = Value;
            if (!parameters.TryGetValue("InitialPickerDate", out DateTime nextInitialPickerDate))
                nextInitialPickerDate = InitialPickerDate;
            if (!parameters.TryGetValue("IsRequired", out bool nextIsRequired))
                nextIsRequired = IsRequired;
            if (!parameters.TryGetValue("FormatDate", out Func<DateTime, string>? nextFormatDate))
                nextFormatDate = FormatDate;

            if (DateTime.Compare(MinDate, nextMinDate) == 0 &&
                DateTime.Compare(MaxDate, nextMaxDate) == 0 &&
                IsRequired == nextIsRequired &&
                DateTimeCompareNullable(SelectedDate, nextValue) == 0 &&
                FormatDate != null &&
                (FormatDate.Equals(nextFormatDate) || nextFormatDate == null))  //since FormatDate may not be set as a parameter, it's ok for nextFormatDate to be null
            {
                return base.SetParametersAsync(parameters);
            }

            SetErrorMessage(true, nextIsRequired, nextValue, nextMinDate, nextMaxDate, nextInitialPickerDate);

            DateTime? oldValue = SelectedDate;
            if (DateTimeCompareNullable(oldValue, nextValue) != 0
                || (FormatDate != null
                && nextFormatDate != null
                && ((nextValue != null ? FormatDate((DateTime)nextValue) : null) != (nextValue != null ?nextFormatDate((DateTime)nextValue) : null))))
            {
                SelectedDate = nextValue;
                FormattedDate = FormatDateInternal(nextValue);
            }

            return base.SetParametersAsync(parameters);
        }

        static int DateTimeCompareNullable(DateTime? val1, DateTime? val2)
        {
            if(val1 == null && val2 == null)
            {
                return 0;
            }
            else if(val1 != null && val2 != null)
            {
                return DateTime.Compare((DateTime)val1,(DateTime)val2);
            }
            else
            {
                return 1; // One value is null and the other not, therefore return 1 in order to indicate that they are unequal
            }

        }

        protected override Task OnParametersSetAsync()
        {
            FormattedDate = FormatDateInternal(SelectedDate);

            if (CascadedEditContext != null && ValueExpression != null)
            {

                FieldIdentifier = FieldIdentifier.Create<DateTime?>(ValueExpression);

                //CascadedEditContext.NotifyFieldChanged(FieldIdentifier);

                CascadedEditContext.OnValidationStateChanged += CascadedEditContext_OnValidationStateChanged;
            }
            return base.OnParametersSetAsync();
        }

        private void CascadedEditContext_OnValidationStateChanged(object? sender, ValidationStateChangedEventArgs e)
        {
            InvokeAsync(() => StateHasChanged());  //invokeasync required for serverside
        }

        private string FormatDateInternal(DateTime? dateTime)
        {
            if (dateTime != null)
            {
                if (FormatDate != null)
                {
                    return FormatDate((DateTime)dateTime);
                }
            }
            return "";
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (_oldIsDatePickerShown && !IsDatePickerShown)
            {
                _ = OnAfterMenuDismiss.InvokeAsync(null);
            }
            await base.OnAfterRenderAsync(firstRender);
        }

        protected string? GetErrorMessage()
        {
            if (IsDatePickerShown)
                return null;

            return ErrorMessage;
        }

        protected void ShowDatePickerPopup()
        {
            if (!IsDatePickerShown)
            {
                _preventFocusOpeningPicker = true;
                IsDatePickerShown = true;
                StateHasChanged();
            }
        }

        protected void DismissDatePickerPopup()
        {
            if (IsDatePickerShown)
            {
                IsDatePickerShown = false;
                if (Value != null)
                    ValidateTextInput();
            }
            if (AllowTextInput)
            {
                _preventFocusOpeningPicker = true;
                DisableAutoFocus = !DisableAutoFocus;
            }
        }

        protected void CalendarDismissed()
        {
            _preventFocusOpeningPicker = true;
            DismissDatePickerPopup();
        }

        protected void OnSelectedDate(SelectedDateResult selectedDateResult)
        {
            //skip calendar props OnSelectedDate callback, not implemented through DatePicker

                SelectedDate = selectedDateResult.Date;
                FormattedDate = FormatDateInternal(selectedDateResult.Date);
                ErrorMessage = "";

                CascadedEditContext?.NotifyFieldChanged(FieldIdentifier);
                OnSelectDate.InvokeAsync(selectedDateResult.Date);
                ValueChanged.InvokeAsync(selectedDateResult.Date);
                if (!selectedDateResult.ShouldLetOpenDatePicker)
                {
                    CalendarDismissed();
                }

        }

        protected void OnTextFieldFocus()
        {
            if (DisableAutoFocus)
                return;
            if (!AllowTextInput)
            {
                if (!_preventFocusOpeningPicker)
                {
                    ShowDatePickerPopup();
                }
                else
                {
                    _preventFocusOpeningPicker = false;
                }
            }
        }

        protected void OnTextFieldChange(string text)
        {
            if (AllowTextInput)
            {
                if (IsDatePickerShown)
                    DismissDatePickerPopup();
            }

            if (FormattedDate != text)
            {
                ErrorMessage = IsRequired && (Value == DateTime.MinValue) ? IsRequiredErrorMessage : null;
                FormattedDate = text;
            }
            // skip TextField OnChange callback ... not implemented through DatePicker
        }

        protected void OnTextFieldKeyDown(KeyboardEventArgs args)
        {
            switch (args.Key)
            {
                case "Enter":
                    //preventDefault
                    //stopPropagation
                    if (!IsDatePickerShown)
                    {
                        ValidateTextInput();
                        ShowDatePickerPopup();
                    }
                    else
                    {
                        if (AllowTextInput)
                            DismissDatePickerPopup();
                    }
                    break;
                case "Escape":
                    HandleEscKey(args);
                    break;
                default:
                    break;
            }
        }

        protected void OnTextFieldBlur()
        {
            ValidateTextInput();
        }

        private void ValidateTextInput()
        {
            if (IsDatePickerShown)
                return;

            string? inputValue = FormattedDate;

            if (AllowTextInput)
            {

                if (!string.IsNullOrWhiteSpace(inputValue))
                {
                    DateTime? date = DateTime.MinValue;
                    if (SelectedDate != DateTime.MinValue && FormatDate != null && FormatDateInternal(SelectedDate) == inputValue)
                    {
                        return;
                    }
                    else
                    {
                        date = ParseDateFromString(inputValue);
                        // Check if date is minvalue, or date is Invalid Date
                        if (date == DateTime.MinValue)
                        {
                            // Reset invalid input field, if formatting is available
                            if (FormatDate != null)
                            {
                                date = SelectedDate;
                                FormattedDate = FormatDateInternal(date);
                            }
                            ErrorMessage = InvalidInputErrorMessage;
                        }
                        else
                        {
                            // Check against optional date boundaries
                            if (IsDateOutOfBounds(date, MinDate, MaxDate))
                            {
                                ErrorMessage = IsOutOfBoundsErrorMessage;
                            }
                            else
                            {
                                SelectedDate = date;
                                ErrorMessage = "";

                                // When formatting is available. If formatted date is valid, but is different from input, update with formatted date
                                // This occurs when an invalid date is entered twice
                                if (FormatDate != null && FormatDateInternal(date) != inputValue)
                                {
                                    FormattedDate = FormatDateInternal(date);
                                }
                            }
                        }
                    }
                    OnSelectDate.InvokeAsync(date);
                    ValueChanged.InvokeAsync(date);
                }
                else
                {
                    ErrorMessage = IsRequired ? IsRequiredErrorMessage : "";
                }

                CascadedEditContext?.NotifyFieldChanged(FieldIdentifier);


            }
            else if (IsRequired && string.IsNullOrWhiteSpace(inputValue))
            {
                ErrorMessage = IsRequiredErrorMessage;
            }

        }

        private void HandleEscKey(KeyboardEventArgs args)
        {
            if (IsDatePickerShown)
            {
                //stopPropagation
                CalendarDismissed();
            }
        }

        private string? SetErrorMessage(bool setState, bool isRequired, DateTime? value, DateTime minDate, DateTime maxDate, DateTime initialPickerDate)
        {
            string? errorMessge = (initialPickerDate == DateTime.MinValue) && isRequired && (value == DateTime.MinValue) ? IsRequiredErrorMessage : null;

            if (errorMessge == null && value != null)
                errorMessge = IsDateOutOfBounds(value, minDate, maxDate) ? string.Format(IsOutOfBoundsErrorMessage, minDate.Date.ToShortDateString(), maxDate.Date.ToShortDateString()) : null;

            if (setState)
            {
                ErrorMessage = errorMessge;
            }
            return errorMessge;
        }

        private static bool IsDateOutOfBounds(DateTime? date, DateTime minDate, DateTime maxDate)
        {
            return DateTimeCompareNullable(minDate, date) > 0 || DateTimeCompareNullable(maxDate, date) < 0;
        }

    }
}
