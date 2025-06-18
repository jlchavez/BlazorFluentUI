using Microsoft.AspNetCore.Components;

namespace BlazorFluentUI
{
    public partial class KeytipData : FluentUIComponentBase
    {
        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        [Parameter]
        public bool Disabled { get; set; }
    }
}
