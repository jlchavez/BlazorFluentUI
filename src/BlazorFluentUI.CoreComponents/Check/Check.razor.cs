﻿using Microsoft.AspNetCore.Components;

namespace BlazorFluentUI
{
    public partial class Check : FluentUIComponentBase
    {
        [Parameter]
        public bool Checked { get; set; }

        [Parameter]
        public bool UseFastIcons { get; set; }

    }
}
