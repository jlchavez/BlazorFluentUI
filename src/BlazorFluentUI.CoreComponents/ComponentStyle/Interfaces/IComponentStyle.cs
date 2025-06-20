﻿using System.Collections.Generic;

namespace BlazorFluentUI
{
    public interface IComponentStyle
    {
        bool IsClient { get; }

        //GlobalRules GlobalRules { get; set; }

        ICollection<ILocalCSSheet> LocalCSSheets { get; set; }

        //ObservableCollection<IGlobalCSSheet> GlobalCSSheets { get; set; }

        //ICollection<string> GlobalCSRules { get; set; }

        //void RulesChanged(IGlobalCSSheet globalCSSheet);

        string PrintRule(IRule rule);

        void SetDisposedAction();
    }
}
