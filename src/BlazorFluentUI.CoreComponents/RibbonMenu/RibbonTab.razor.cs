﻿using BlazorFluentUI.Models;
using BlazorFluentUI.Resize;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;


namespace BlazorFluentUI
{
    public partial class RibbonTab : ResizeComponentBase
    {
        [Parameter] public string? HeaderText { get; set; }
        [Parameter] public RenderFragment? ChildContent { get; set; }
        [Parameter] public IEnumerable<IGroup>? ItemsSource { get; set; }
        [Parameter] public RenderFragment<ResizeGroupData>? ItemTemplate { get; set; }

        Collection<ResizeGroupData> ResizableGroups = new();


        public RibbonTab()
        {
            
           OnGrowData = () =>
            {

                if (ResizableGroups.Count > 0)
                {

                    ResizeGroupData? groupToGrow = ResizableGroups[0];
                    foreach (ResizeGroupData? resizableGroup in ResizableGroups)
                    {
                        if(groupToGrow.HighestPriorityInOverflowItems() <= resizableGroup.HighestPriorityInOverflowItems())
                        {
                            groupToGrow = resizableGroup;
                        }
      
                    }
                    if (groupToGrow.Grow())
                    {
                        return true;
                    }
                }

                return false;
            };

            OnReduceData = () =>
            {
                if (ResizableGroups.Count > 0)
                {
                    ResizeGroupData? groupToShrink = ResizableGroups[0];
                    foreach (ResizeGroupData? resizableGroup in ResizableGroups)
                    {
                        if (groupToShrink.LowestPriorityInItems() >= resizableGroup.LowestPriorityInItems())
                        {
                            groupToShrink = resizableGroup;
                        }

                    }
                    if (groupToShrink.Shrink())
                    {
                        return true;
                    }

          
                }
                
                return false;
            };
        }

        protected override Task OnInitializedAsync()
        {
  
            if (ItemsSource != null)
            {
                foreach (IGroup? group in ItemsSource)
                {
                    ResizableGroups.Add(new ResizeGroupData(group.ItemsSource, group == ItemsSource.Last()));
                }
            }
            return base.OnInitializedAsync();
        }

    }
}
