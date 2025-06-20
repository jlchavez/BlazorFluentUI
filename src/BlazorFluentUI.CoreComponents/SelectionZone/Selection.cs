﻿using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Collections.Specialized;
using System.Diagnostics;

namespace BlazorFluentUI
{
    public class Selection<TItem>
    {
        private IList<TItem>? _items;

        public IList<TItem> SelectedItems
        {
            get => GetSelection();
            set => SetItemsSelected(value);
        }

        private void SetItemsSelected(IList<TItem> items)
        {
            if (GetKey == null)
                throw new Exception("GetKey must not be null.");
            SetChangeEvents(false);
            SetAllSelected(false);
            foreach (TItem? item in items)
            {
                object? key = GetKey(item);
                if (key != null)
                    SetKeySelected(key, true, false);
            }
            SetChangeEvents(true);
        }

        private SelectionMode _selectionMode = SelectionMode.Single;
        public SelectionMode SelectionMode { get => _selectionMode;  set { if (_selectionMode != value) { _selectionMode = value; AdjustSelectionMode(); } } }

        public Func<TItem, object?>? GetKey { get; set; }

        public Func<TItem, int?, bool> CanSelectItem { get; set; } = (item, index) => !index.HasValue || index.Value >= 0;

        public int? Count { get; set; }

        private Subject<Unit> selectionChanged = new();
        public IObservable<Unit> SelectionChanged => selectionChanged.AsObservable();
        public event EventHandler? OnSelectionChanged;

        private int _unselectableCount;
        private bool _isAllSelected;
        private int _exemptedCount;
        private Dictionary<object, int> _keyToIndexMap = new();
        private Dictionary<int, object> _exemptedIndices = new();
        private Dictionary<int, TItem> _unselectableIndices = new();
        private List<int>? _selectedIndices = new();

        private HashSet<object> _exemptedKeys = new();
        //private HashSet<object> _unselectableKeys = new();
        //private List<object> _selectedKeys = new();

        private int _changeEventSuppressionCount;
        private List<TItem>? _selectedItems = new();


        private bool _hasChanged;
        private bool _isModal;
        private int _anchoredIndex;

        public Selection()
        {
            selectionChanged.Subscribe(x => OnSelectionChanged?.Invoke(this, EventArgs.Empty));
        }

        public void AdjustSelectionMode()
        {
            switch (_selectionMode)
            {
                case SelectionMode.Multiple:
                    //do nothing!
                    break;
                case SelectionMode.Single:
                    if (_exemptedIndices.Count> 1)
                    {
                        //clear all
                        SetAllSelected(false);
                    }
                    break;
                case SelectionMode.None:
                    if (_exemptedIndices.Count > 0)
                    {
                        //clear all
                        SetAllSelected(false);
                    }
                    break;
            }
        }

        public IList<TItem> GetItems()
        {

            return _items ?? new List<TItem>();
        }

        public void SetItems(IList<TItem> items, bool shouldClear = true)
        {
            Dictionary<object, int> newKeyToIndexMap = new();
            Dictionary<int, TItem> newUnselectableIndices = new();
            HashSet<object> newUnselectableKeys = new();

            bool hasSelectionChanged = false;

            SetChangeEvents(false);

            if (items != _items && _items != null && _items is INotifyCollectionChanged)
            {
                //unsubscribe
                (_items as INotifyCollectionChanged)!.CollectionChanged += Selection_CollectionChanged;
            }
            if (items != _items && items != null && items is INotifyCollectionChanged)
            {
                //subscribe to observable list change (like sort order!)
                (items as INotifyCollectionChanged)!.CollectionChanged += Selection_CollectionChanged;
            }

            _unselectableCount = 0;
            int index = 0;
            foreach (TItem? item in items!)
            {
                object? key = GetKey!(item);
                if (key != null)
                {
                    newKeyToIndexMap.Add(key, index);


                    if (!CanSelectItem(item, null))
                    {
                        newUnselectableIndices[index] = item;
                        newUnselectableKeys.Add(key);
                        _unselectableCount++;
                    }
                }

                index++;
            }

            if (shouldClear || items.Count == 0)
            {
                SetAllSelected(false, true);
            }

            Dictionary<int, object> newExemptedIndices = new();
            HashSet<object> newExemptedKeys = new();
            int newExemptedCount = 0;

            foreach (KeyValuePair<int, object> i in _exemptedIndices)
            {
                //skipping hasOwnProperty check
                index = i.Key;
                TItem? item = (index < _items!.Count && index >= 0) ? _items[index] : default;
                object? exemptKey = item != null ? GetKey!(item) : null;
                int newIndex = exemptKey != null ? (newKeyToIndexMap.ContainsKey(exemptKey) ? newKeyToIndexMap[exemptKey] : -1) : index;

                if (newIndex == -1)
                {
                    hasSelectionChanged = true;
                }
                else
                {
                    if (exemptKey != null)
                    {
                        newExemptedIndices[newIndex] = exemptKey;
                        newExemptedKeys.Add(exemptKey);
                        newExemptedCount++;
                        hasSelectionChanged = hasSelectionChanged || newIndex != index;
                    }
                }
            }

            if (_items != null && _exemptedCount == 0 && items.Count != _items.Count && _isAllSelected)
            {
                hasSelectionChanged = true;
            }

            _exemptedIndices = newExemptedIndices;
            _exemptedKeys = newExemptedKeys;
            _exemptedCount = newExemptedCount;
            _keyToIndexMap = newKeyToIndexMap;
            _unselectableIndices = newUnselectableIndices;
            //_unselectableKeys = newUnselectableKeys;
            _items = items;
            _selectedItems = null;

            if (hasSelectionChanged)
            {
                UpdateCount();
                Change();
            }

            SetChangeEvents(true);

        }

        private void Selection_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // This is fairly brute force, but I haven't found a situation in which something other than a full reset is required.
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:

                case NotifyCollectionChangedAction.Remove:

                case NotifyCollectionChangedAction.Reset:

                case NotifyCollectionChangedAction.Move:
                    //check all, match by key instead of index as index might have changed from a sort or filter operation 
                    Dictionary<object, int> newKeyToIndexMap = new();
                    Dictionary<int, TItem> newUnselectableIndices = new();
                    Dictionary<int, object> newExemptedIndices = new();

                    _unselectableCount = 0;
                    int index = 0;
                    foreach (TItem item in _items!)
                    {
                        if (item != null)
                        {
                            object? key = GetKey!(item);
                            if (key != null)
                            {
                                newKeyToIndexMap.Add(key, index);
                            }

                            if (!CanSelectItem(item, null))
                            {
                                newUnselectableIndices[index] = item;
                                _unselectableCount++;
                            }

                            index++;
                        }
                    }

                    int newExemptedCount = 0;
                    foreach (object? key in _exemptedKeys)
                    {

                        if (newKeyToIndexMap.ContainsKey(key))  //the item might no longer exist (it was filtered out)
                                                                // the index won't exist, but they key still does... 
                        {
                            index = newKeyToIndexMap[key];
                            newExemptedIndices.Add(index, key);
                        }

                        newExemptedCount++;
                    }

                    _exemptedIndices = newExemptedIndices;  //just update indices, not keys
                    _exemptedCount = newExemptedCount;  //this only counts indices
                    _keyToIndexMap = newKeyToIndexMap;
                    _unselectableIndices = newUnselectableIndices;

                    _selectedItems = null;

                    break;
            }

        }

        public void SetAllSelected(bool isAllSelected, bool preserveModalState = false)
        {
            if (isAllSelected && SelectionMode != SelectionMode.Multiple)
                return;

            int selectableCount = _items != null ? _items.Count - _unselectableCount : 0;

            SetChangeEvents(false);

            if (selectableCount > 0 && (_exemptedCount > 0 || isAllSelected != _isAllSelected))
            {
                _exemptedIndices = new Dictionary<int, object>();
                _exemptedKeys = new HashSet<object>();

                if (isAllSelected != _isAllSelected || _exemptedCount > 0)
                {
                    _exemptedCount = 0;
                    _isAllSelected = isAllSelected;
                    Change();
                }

                UpdateCount();
            }
            SetChangeEvents(true);

        }

        private void UpdateCount(bool preserveModalState = false)
        {
            int? count = GetSelectedCount();

            if (count != Count)
            {
                Count = count;
                Change();
            }

            if (Count.HasValue && !preserveModalState)
            {
                SetModal(false);
            }
        }

        public bool IsModal()
        {
            return _isModal;
        }

        public void SetModal(bool isModal)
        {
            if (_isModal != isModal)
            {
                SetChangeEvents(false);

                _isModal = isModal;

                if (!isModal)
                {
                    SetAllSelected(false);
                }

                Change();

                SetChangeEvents(true);
            }
        }


        public IList<TItem> GetSelection()
        {
            if (_selectedItems == null)
            {
                _selectedItems= new List<TItem>();

                IList<TItem>? items = _items;

                if (items != null)
                {
                    for (int i = 0; i < items.Count; i++)
                    {
                        if (IsIndexSelected(i))
                        {
                            _selectedItems.Add(items[i]);
                        }
                    }
                }
            }

            return _selectedItems;
        }

        public int? GetSelectedCount()
        {
            if (_items == null)
                throw new Exception("_items was null");

            return _isAllSelected
                ? _items.Count - _exemptedCount - _unselectableCount
                : _exemptedCount;
        }

        public IList<int> GetSelectedIndices()
        {
            if (_selectedIndices == null)
            {
                _selectedIndices = new List<int>();

                IList<TItem>? items = _items;

                if (items != null)
                {
                    for (int i = 0; i < items.Count; i++)
                    {
                        if (IsIndexSelected(i))
                        {
                            _selectedIndices.Add(i);
                        }
                    }
                }
            }

            return _selectedIndices;
        }

        public void SetChangeEvents(bool isEnabled, bool suppressChange = false)
        {
            _changeEventSuppressionCount += isEnabled ? -1 : 1;

            if (_changeEventSuppressionCount == 0 && _hasChanged)
            {
                _hasChanged = false;

                if (!suppressChange)
                {
                    Change();
                }
            }
        }

        private void Change()
        {
            if (_changeEventSuppressionCount == 0)
            {
                _selectedItems = null;
                _selectedIndices = null;

                selectionChanged.OnNext(Unit.Default);
            }
            else
            {
                _hasChanged = true;
            }
        }

        public void SetKeySelected(object key, bool isSelected, bool shouldAnchor)
        {
            int index = _keyToIndexMap[key];

            if (index >= 0)
            {
                SetIndexSelected(index, isSelected, shouldAnchor);
            }
        }

        public void SetIndexSelected(int index, bool isSelected, bool shouldAnchor)
        {
            if (SelectionMode == SelectionMode.None)
            {
                return;
            }

            // Clamp the index.
            index = Math.Min(Math.Max(0, index), _items!.Count - 1);

            // No-op on out of bounds selections.
            if (index < 0 || index >= _items.Count)
            {
                return;
            }

            SetChangeEvents(false);

            bool isExempt = _exemptedIndices.ContainsKey(index);
            bool canSelect = !_unselectableIndices.ContainsKey(index);

            if (canSelect)
            {
                if (isSelected && SelectionMode == SelectionMode.Single)
                {
                    // If this is single-select, the previous selection should be removed.
                    SetAllSelected(false, true);
                }

                // Determine if we need to remove the exemption.
                if (isExempt && ((isSelected && _isAllSelected) || (!isSelected && !_isAllSelected)))
                {
                    _exemptedKeys.Remove(_exemptedIndices[index]);
                    _exemptedIndices.Remove(index);

                    _exemptedCount--;
                }

                // Determine if we need to add the exemption.
                if (!isExempt && ((isSelected && !_isAllSelected) || (!isSelected && _isAllSelected)))
                {
                    object? key = GetKey!(_items[index]);
                    if (key != null)
                    {
                        _exemptedKeys.Add(key);
                        _exemptedIndices[index] = key;
                        _exemptedCount++;
                    }
                }

                if (shouldAnchor)
                {
                    _anchoredIndex = index;
                }
            }

            UpdateCount();

            SetChangeEvents(true);
        }

        public void SelectToKey(object key, bool clearSelection = false)
        {
            SelectToIndex(_keyToIndexMap[key], clearSelection);
        }

        public void SelectToIndex(int index, bool clearSelection = false)
        {
            if (SelectionMode == SelectionMode.None)
            {
                return;
            }

            if (SelectionMode == SelectionMode.Single)
            {
                SetIndexSelected(index, true, true);
                return;
            }

            int anchorIndex = _anchoredIndex;
            int startIndex = Math.Min(index, anchorIndex);
            int endIndex = Math.Max(index, anchorIndex);

            SetChangeEvents(false);

            if (clearSelection)
            {
                SetAllSelected(false, true);
            }

            for (; startIndex <= endIndex; startIndex++)
            {
                SetIndexSelected(startIndex, true, false);
            }

            SetChangeEvents(true);
        }

        public void ToggleRangeSelected(int fromIndex, int count)
        {
            if (SelectionMode == SelectionMode.None)
            {
                return;
            }

            bool isRangeSelected = IsRangeSelected(fromIndex, count);
            int endIndex = fromIndex + count;

            if (SelectionMode == SelectionMode.Single && count > 1)
            {
                return;
            }

            SetChangeEvents(false);
            for (int i = fromIndex; i < endIndex; i++)
            {
                SetIndexSelected(i, !isRangeSelected, false);
            }
            SetChangeEvents(true);
        }

        public void ToggleAllSelected()
        {
            SetAllSelected(!IsAllSelected());
        }

        public void ToggleKeySelected(object key)
        {
            SetKeySelected(key, !IsKeySelected(key), true);
        }

        public void ToggleIndexSelected(int index)
        {
            SetIndexSelected(index, !IsIndexSelected(index), true);
        }


        public bool IsRangeSelected(int fromIndex, int count)
        {
            if (count == 0)
            {
                return false;
            }

            int endIndex = fromIndex + count;

            for (int i = fromIndex; i < endIndex; i++)
            {
                if (!IsIndexSelected(i))
                {
                    return false;
                }
            }

            return true;
        }

        public bool IsAllSelected()
        {
            int selectableCount = _items!.Count - _unselectableCount;

            // In single mode, we can only have a max of 1 item.
            if (SelectionMode == SelectionMode.Single)
            {
                selectableCount = Math.Min(selectableCount, 1);
            }

            return (
              (Count > 0 && _isAllSelected && _exemptedCount == 0) ||
              (!_isAllSelected && _exemptedCount == selectableCount && selectableCount > 0)
            );
        }

        public bool IsKeySelected(object key, bool ignoreIndex=false)
        {
            if (ignoreIndex)
            {
                return _exemptedKeys.Contains(key);
            }
            else
            {
                if (!_keyToIndexMap.ContainsKey(key))
                {
                    Debug.WriteLine("In the middle of updating from a filter. Subgroups haven't redrawn even though selection indices have been mapped (incorrectly)");
                    return false;
                }
                int index = _keyToIndexMap[key];

                return IsIndexSelected(index);
            }
        }

        public bool IsIndexSelected(int index)
        {
            return (
              (Count > 0 && _isAllSelected && !_exemptedIndices.ContainsKey(index) && !_unselectableIndices.ContainsKey(index)) ||
              (!_isAllSelected && _exemptedIndices.ContainsKey(index))
            );
        }

        public void ClearSelection()
        {
            _items = new List<TItem>();
        }


    }
}
