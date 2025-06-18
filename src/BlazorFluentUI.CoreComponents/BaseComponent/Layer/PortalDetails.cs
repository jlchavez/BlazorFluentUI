using Microsoft.AspNetCore.Components;

namespace BlazorFluentUI
{
    public class PortalDetails
    {
        public string? Id { get; set; }
        public RenderFragment? Fragment { get; set; }
        //public string? Style { get; set; }
        public ElementReference Parent { get; set; }
    }
}
