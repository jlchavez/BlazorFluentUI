﻿using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorFluentUI
{
    public partial class CalendarDay : FluentUIComponentBase
    {
        [Parameter] public bool AllFocusable { get; set; }
        [Parameter] public bool AutoNavigateOnSelection { get; set; }
        [Parameter] public DateRangeType DateRangeType { get; set; }
        [Parameter] public DateTimeFormatter DateTimeFormatter { get; set; } = new DateTimeFormatter();
        [Parameter] public DayOfWeek FirstDayOfWeek { get; set; }
        [Parameter] public FirstWeekOfYear FirstWeekOfYear { get; set; }
        [Parameter] public DateTime MaxDate { get; set; }
        [Parameter] public DateTime MinDate { get; set; }
        [Parameter] public DateTime NavigatedDate { get; set; }
        [Parameter] public EventCallback<bool> OnHeaderSelect { get; set; }
        [Parameter] public EventCallback OnDismiss { get; set; }
        [Parameter] public EventCallback<NavigatedDateResult> OnNavigateDate { get; set; }
        [Parameter] public EventCallback<SelectedDateResult> OnSelectDate { get; set; }
        [Parameter] public List<DateTime>? RestrictedDates { get; set; }
        [Parameter] public DateTime? SelectedDate { get; set; }
        [Parameter] public bool ShowCloseButton { get; set; }
        [Parameter] public bool ShowSixWeeksByDefault { get; set; }
        [Parameter] public bool ShowWeekNumbers { get; set; }
        [Parameter] public DateTime? Today { get; set; }
        [Parameter] public List<DayOfWeek>? WorkWeekDays { get; set; }


        protected string DayPickerId = $"d_{Guid.NewGuid().ToString().Replace("-", "")}";
        protected string MonthAndYearId = $"my_{Guid.NewGuid().ToString().Replace("-", "")}";

        protected string PreviousMonthAriaLabel = "Previous month"; //needs localization!
        protected string NextMonthAriaLabel = "Next month"; //needs localization!
        protected string CloseButtonAriaLabel = "Close"; //needs localization!
        protected string WeekNumberFormatString = "Week number {0}"; //needs localization!

        protected bool PrevMonthInBounds;
        protected bool NextMonthInBounds;

        protected List<List<DayInfo>>? Weeks;
        protected List<int>? WeekNumbers;
        protected Dictionary<string, string>? WeekCornersStyled;

        protected int HoverWeek = -1;
        protected int HoverMonth = -1;
        protected int PressWeek = -1;
        protected int PressMonth = -1;

        TimeSpan timeOfDay;
        bool onFirstRender = true;
        protected override Task OnParametersSetAsync()
        {
            if(onFirstRender)
            {
                timeOfDay = NavigatedDate.TimeOfDay;
                onFirstRender = false;
            }
            GenerateWeeks();

            if (ShowWeekNumbers)
            {
                WeekNumbers = DateUtilities.GetWeekNumbersInMonth(Weeks!.Count, FirstDayOfWeek, FirstWeekOfYear, NavigatedDate);
            }
            else
                WeekNumbers = null;

            WeekCornersStyled = CreateWeekCornerStyles();

            // determine if previous/next months are in bounds

            DateTime firstDayOfMonth = new(NavigatedDate.Year, NavigatedDate.Month, 1);
            PrevMonthInBounds = DateTime.Compare(MinDate, firstDayOfMonth) < 0;
            NextMonthInBounds = DateTime.Compare(firstDayOfMonth.AddMonths(1).AddDays(-1), MaxDate) < 0;

            #region time of day setting
            if (timeOfDay != NavigatedDate.TimeOfDay)
            {
                timeOfDay = NavigatedDate.TimeOfDay;
                OnSelectDateInternal(NavigatedDate.Date,true);
            }
            #endregion

            return base.OnParametersSetAsync();
        }


        protected Task OnHeaderSelectInternal(MouseEventArgs mouseEventArgs)
        {
            OnHeaderSelect.InvokeAsync(true);
            return Task.CompletedTask;
        }

        protected Task OnHeaderKeyDownInternal(KeyboardEventArgs keyboardEventArgs)
        {
            if (keyboardEventArgs.Key == "Enter" || keyboardEventArgs.Key == " ")
            {
                OnHeaderSelect.InvokeAsync(true);
            }
            return Task.CompletedTask;
        }

        protected Task OnSelectPrevMonth()
        {
            return OnNavigateDate.InvokeAsync(new NavigatedDateResult() { Date = NavigatedDate.AddMonths(-1), FocusOnNavigatedDay = false });
        }

        protected Task OnSelectNextMonth()
        {
            return OnNavigateDate.InvokeAsync(new NavigatedDateResult() { Date = NavigatedDate.AddMonths(1), FocusOnNavigatedDay = false });
        }


        protected Task OnClose(MouseEventArgs mouseEventArgs)
        {
            return OnDismiss.InvokeAsync(null);
        }

        //protected static void OnTableMouseLeave(System.EventArgs eventArgs)
        //{
        //}
        //protected static void OnTableMouseUp(MouseEventArgs mouseEventArgs)
        //{
        //}
        //protected Task OnDayMouseOver(MouseEventArgs eventArgs)
        //{
        //    return Task.CompletedTask;
        //}
        //protected Task OnDayMouseLeave(EventArgs eventArgs)
        //{
        //    return Task.CompletedTask;
        //}
        //protected Task OnDayMouseDown(MouseEventArgs eventArgs)
        //{
        //    return Task.CompletedTask;
        //}
        //protected Task OnDayMouseUp(MouseEventArgs eventArgs)
        //{
        //    return Task.CompletedTask;
        //}

        protected string GetDayClasses(DayInfo day)
        {
            string classNames = "";
            classNames += "dayCell";
            if (WeekCornersStyled !=null && WeekCornersStyled.ContainsKey(day.WeekIndex + "_" + (day.OriginalDate.Day - 1)))
            {
                classNames += WeekCornersStyled[day.WeekIndex + "_" + (day.OriginalDate.Day - 1)];
            }
            if (day.IsSelected && (DateRangeType == DateRangeType.Week || DateRangeType == DateRangeType.WorkWeek))
                classNames += " ms-Calendar-weekBackground";
            if (day.IsSelected && DateRangeType == DateRangeType.Day)
                classNames += " ms-Calendar-dayBackground";
            if (day.IsSelected && DateRangeType == DateRangeType.Day)
                classNames += " ms-Calendar-dayIsHighlighted";
            if (day.IsInBounds && day.IsInMonth && DateRangeType == DateRangeType.Day)
                classNames += " ms-Calendar-dayIsFocused";
            if (day.IsInBounds && !day.IsInMonth && DateRangeType == DateRangeType.Day)
                classNames += " ms-Calendar-dayUnFocused";
            if (!day.IsInBounds && day.IsInMonth)
                    classNames += " ms-Calendar-dayOutsideBounds";
            switch (DateRangeType)
            {
                case DateRangeType.Day:
                    classNames += " ms-Calendar-daySelection";
                    break;
                case DateRangeType.Week:
                    //classNames += " ms-Calendar-weekSelection";
                    if (HoverWeek == day.WeekIndex)
                        classNames += " ms-Calendar-dayHover";
                    if (PressWeek == day.WeekIndex)
                        classNames += " ms-Calendar-dayPress";
                    break;
                case DateRangeType.WorkWeek:
                    //if (day.IsHighlightedOnHover)
                    //    classNames += " ms-Calendar-dayIsFocused";
                    if (HoverWeek == day.WeekIndex && day.IsHighlightedOnHover)
                        classNames += " ms-Calendar-dayHover";
                    if (PressWeek == day.WeekIndex)
                        classNames += " ms-Calendar-dayPress";
                    break;
                case DateRangeType.Month:
                    classNames += " ms-Calendar-monthSelection";
                    if (HoverMonth == day.OriginalDate.Month)
                        classNames += " ms-Calendar-dayHover";
                    if (PressMonth == day.OriginalDate.Month)
                        classNames += " ms-Calendar-dayPress";
                    break;
            }
            return classNames;
        }

        private Dictionary<string,string> CreateWeekCornerStyles()
        {
            Dictionary<string, string>? weekCornersStyled = new();

            switch (DateRangeType)
            {
                case DateRangeType.Month:
                    for (int weekIndex = 0; weekIndex < Weeks!.Count; weekIndex++)
                    {
                        List<DayInfo>? week = Weeks[weekIndex];
                        for (int dayIndex =0; dayIndex < week.Count; dayIndex++)
                        {
                            bool above = false, below = false, left = false, right = false;
                            DayInfo? day = week[dayIndex];
                            if (weekIndex - 1 >= 0 && dayIndex < Weeks[weekIndex - 1].Count)
                            {
                                above = Weeks[weekIndex - 1][dayIndex].OriginalDate.Month == Weeks[weekIndex][dayIndex].OriginalDate.Month;
                            }
                            if (weekIndex + 1 < Weeks.Count && dayIndex < Weeks[weekIndex + 1].Count)
                            {
                                below = Weeks[weekIndex + 1][dayIndex].OriginalDate.Month == Weeks[weekIndex][dayIndex].OriginalDate.Month;
                            }
                            if (dayIndex - 1 >= 0)
                            {
                                left = Weeks[weekIndex][dayIndex - 1].OriginalDate.Month == Weeks[weekIndex][dayIndex].OriginalDate.Month;
                            }
                            if (dayIndex + 1 < Weeks[weekIndex].Count)
                            {
                                right = Weeks[weekIndex][dayIndex + 1].OriginalDate.Month == Weeks[weekIndex][dayIndex].OriginalDate.Month;
                            }

                            bool roundedTopLeft = !above && !left;
                            bool roundedTopRight = !above && !right;
                            bool roundedBottomLeft = !below && !left;
                            bool roundedBottomRight = !below && !right;

                            string classNames = "";
                            if (roundedTopLeft)
                                classNames += " ms-Calendar-topLeftCornerDate";
                            if (roundedTopRight)
                                classNames += " ms-Calendar-topRightCornerDate";
                            if (roundedBottomLeft)
                                classNames += " ms-Calendar-bottomLeftCornerDate";
                            if (roundedBottomRight)
                                classNames += " ms-Calendar-bottomRightCornerDate";

                            if (!above)
                                classNames += " ms-Calendar-topDate";
                            if (!below)
                                classNames += " ms-Calendar-bottomDate";
                            if (!right)
                                classNames += " ms-Calendar-rightDate";
                            if (!left)
                                classNames += " ms-Calendar-leftDate";

                            weekCornersStyled.Add(weekIndex + "_" + dayIndex, classNames);
                        }
                    }
                    break;
                case DateRangeType.Week:
                case DateRangeType.WorkWeek:
                    for (int weekIndex =0; weekIndex < Weeks!.Count; weekIndex++)
                    {
                        int minIndex = Weeks[weekIndex].IndexOf(Weeks[weekIndex].First(x => x.IsInBounds));
                        int maxIndex = Weeks[weekIndex].IndexOf(Weeks[weekIndex].Last(x => x.IsInBounds));

                        string? leftStyle = " ms-Calendar-topLeftCornerDate ms-Calendar-bottomLeftCornerDate";
                        string? rightStyle = " ms-Calendar-topRightCornerDate ms-Calendar-bottomRightCornerDate";
                        weekCornersStyled.Add(weekIndex + "_" + minIndex, leftStyle);
                        weekCornersStyled.Add(weekIndex + "_" + maxIndex, rightStyle);
                    }
                    break;
            }

            return weekCornersStyled;
        }

        private void OnSelectDateInternal(DateTime selectedDate, bool shouldLetOpenDatePicker)
        {
            // stop propagation - needed?
            DateTime selectedDateWithTime = selectedDate.Add(timeOfDay);

            List<DateTime>? dateRange = DateUtilities.GetDateRangeArray(selectedDate, DateRangeType, FirstDayOfWeek, WorkWeekDays);
            if (DateRangeType != DateRangeType.Day)
                dateRange = GetBoundedDateRange(dateRange, MinDate, MaxDate);
            dateRange = dateRange.Where(d => !GetIsRestrictedDate(d)).ToList();

            OnSelectDate.InvokeAsync(new SelectedDateResult() { Date = selectedDateWithTime, SelectedDateRange = dateRange, ShouldLetOpenDatePicker= shouldLetOpenDatePicker });

            // Navigate to next or previous month if needed
            if (AutoNavigateOnSelection && selectedDateWithTime.Month != NavigatedDate.Month)
            {
                int compareResult = DateTime.Compare(selectedDateWithTime.Date, NavigatedDate.Date);
                if (compareResult < 0)
                    OnSelectPrevMonth();
                else if (compareResult > 0)
                    OnSelectNextMonth();

            }
        }


        private void GenerateWeeks()
        {
            DateTime date = new(NavigatedDate.Year, NavigatedDate.Month, 1);
            DateTime todaysDate = DateTime.Now;
            Weeks = new List<List<DayInfo>>();

            // cycle backwards to get first day of week
            while (date.DayOfWeek != FirstDayOfWeek)
                date -= TimeSpan.FromDays(1);

            // a flag to indicate whether all days of the week are in the month
            bool isAllDaysOfWeekOutOfMonth = false;

            // in work week view we want to select the whole week
            //DateRangeType selecteDateRangeType = DateRangeType == DateRangeType.WorkWeek ? DateRangeType.Week : DateRangeType;
            DateRangeType selecteDateRangeType = DateRangeType;
            List<DateTime>? selectedDates = SelectedDate == null ? new List<DateTime>() : DateUtilities.GetDateRangeArray((DateTime)SelectedDate, selecteDateRangeType, FirstDayOfWeek, WorkWeekDays);
            if (DateRangeType != DateRangeType.Day)
            {
                selectedDates = GetBoundedDateRange(selectedDates, MinDate, MaxDate);
            }

            bool shouldGetWeeks = true;
            for (int weekIndex = 0; shouldGetWeeks; weekIndex++)
            {
                List<DayInfo> week = new();
                isAllDaysOfWeekOutOfMonth = true;

                for (int dayIndex = 0; dayIndex < 7; dayIndex++)
                {
                    DateTime originalDate = new(date.Year, date.Month, date.Day);
                    DayInfo? dayInfo = new()
                    {
                        Key = date.ToString(),
                        Date = date.Date.ToString("D"),
                        OriginalDate = originalDate,
                        WeekIndex = weekIndex,
                        IsInMonth = date.Month == NavigatedDate.Month,
                        IsToday = DateTime.Compare(DateTime.Now.Date, originalDate) == 0,
                        IsSelected = IsInDateRangeArray(date, selectedDates),
                        IsHighlightedOnHover = IsInWorkweekRange(date),
                        OnSelected = () => OnSelectDateInternal(originalDate, false),
                        IsInBounds =
                            (DateTime.Compare(MinDate, date) < 1 ) &&
                            (DateTime.Compare(date, MaxDate) < 1 ) &&
                            !GetIsRestrictedDate(date)
                    };

                    week.Add(dayInfo);
                    if (dayInfo.IsInMonth)
                        isAllDaysOfWeekOutOfMonth = false;

                    date = date.AddDays(1);
                }

                // We append the condition of the loop depending upon the ShowSixWeeksByDefault parameter.
                shouldGetWeeks = ShowSixWeeksByDefault ? !isAllDaysOfWeekOutOfMonth || weekIndex <= 5 : !isAllDaysOfWeekOutOfMonth;
                if (shouldGetWeeks)
                    Weeks.Add(week);
            }
        }

        private bool GetIsRestrictedDate(DateTime date)
        {
            if (RestrictedDates == null)
                return false;

            if (RestrictedDates.Select(x=>x.Date).Contains(date))
            {
                return true;
            }
            return false;
        }

        private static bool IsInDateRangeArray(DateTime date, List<DateTime> dateRange)
        {
            foreach (DateTime dateInRange in dateRange) {
                if (DateTime.Compare(date, dateInRange) == 0) {
                    return true;
                }
            }
            return false;
        }

        private bool IsInWorkweekRange(DateTime date)
        {
            if (WorkWeekDays != null)
            {
                foreach (DayOfWeek workday in WorkWeekDays)
                {
                    if (date.DayOfWeek == workday)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static List<DateTime> GetBoundedDateRange(List<DateTime> dateRange, DateTime? minDate = null, DateTime? maxDate = null)
        {
            List<DateTime>? boundedDateRange = dateRange;
            if (minDate.HasValue) {
                boundedDateRange = boundedDateRange.Where(date => DateTime.Compare(date.Date, minDate.Value) >= 0).ToList();
            }
            if (maxDate.HasValue) {
                boundedDateRange = boundedDateRange.Where(date => DateTime.Compare(date.Date, maxDate.Value) <= 0).ToList();
            }
            return boundedDateRange;
        }


}
}
