using Microsoft.AspNetCore.Components;

namespace BlazorFluentUI
{
    public partial class ResponsiveCascader : ResponsiveComponentBase
    {
        [Parameter]
        public RenderFragment? ChildContent { get; set; }
    }
}
