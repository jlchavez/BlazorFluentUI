﻿using System;

namespace BlazorFluentUI
{
    public class DropdownOption : IDropdownOption
    {
        public bool Disabled { get; set; }
        public bool Hidden { get; set; }
        public SelectableOptionMenuItemType ItemType { get; set; } = SelectableOptionMenuItemType.Normal;

        string? key;
        public string? Key
        {
            get
            {
                if(key == null)
                {
                    key = $"id_{Guid.NewGuid().ToString().Replace("-", "")}";
                }
                return key;
            }
            set
            {
                key = value;
            }
        }
        public string? Text { get; set; }
    }
}
