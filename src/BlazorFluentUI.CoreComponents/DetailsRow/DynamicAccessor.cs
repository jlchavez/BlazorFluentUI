using System;
using System.Linq.Expressions;

namespace BlazorFluentUI
{
    public class DynamicAccessor<TItem>
    {
        public object Value
        {
            get => _getter(_item);
            set => _setter(_item, value);
        }

        private Func<TItem, object> _getter;
        private Action<TItem, object> _setter;
        private TItem _item;

        public DynamicAccessor(TItem item, Expression<Func<TItem,object>> getter)
        {
            _item = item;
            _getter = getter.Compile();
            _setter = DetailsRowUtils.GetSetter(getter);
        }
    }
}
