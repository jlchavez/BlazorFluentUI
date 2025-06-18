using Microsoft.AspNetCore.Components;

namespace BlazorFluentUI
{
    public partial class ResponsiveWrapper : ResponsiveComponentBase
    {
        [Parameter]
        public RenderFragment<ResponsiveMode>? ChildContent { get; set; }


    }
}
