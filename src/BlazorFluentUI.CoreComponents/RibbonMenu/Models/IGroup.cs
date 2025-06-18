using System.Collections.Generic;

namespace BlazorFluentUI.Models
{
    public interface IGroup
    {
        ICollection<IRibbonItem> ItemsSource { get; set; }

    }
}
