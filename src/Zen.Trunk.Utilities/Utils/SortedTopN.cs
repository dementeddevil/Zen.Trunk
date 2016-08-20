﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace System.Linq
{
    internal class SortedTopN<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        private int _n;
        private List<TKey> _topNKeys;
        private List<TValue> _topNValues;
        private IComparer<TKey> _comparer;

        public SortedTopN(int count, IComparer<TKey> comparer)
        {
            if (count < 1) throw new ArgumentOutOfRangeException("count");
            if (comparer == null) throw new ArgumentNullException("comparer");
            _n = count;
            _topNKeys = new List<TKey>(count);
            _topNValues = new List<TValue>(count);
            _comparer = comparer;
        }

        public bool Add(KeyValuePair<TKey,TValue> item)
        {
            return Add(item.Key, item.Value);
        }

        public bool Add(TKey key, TValue value)
        {
            int position = _topNKeys.BinarySearch(key, _comparer);
            if (position < 0) position = ~position;
            if (_topNKeys.Count < _n || position != 0)
            {
                // Empty out an item if we're already full and we need to
                // add another
                if (_topNKeys.Count == _n)
                {
                    _topNKeys.RemoveAt(0);
                    _topNValues.RemoveAt(0);
                    position--;
                }

                // Insert or add based on where we're adding
                if (position < _n)
                {
                    _topNKeys.Insert(position, key);
                    _topNValues.Insert(position, value);
                }
                else
                {
                    _topNKeys.Add(key);
                    _topNValues.Add(value);
                }
                return true;
            }

            // No room for this item
            return false;
        }

        public IEnumerable<TValue> Values
        {
            get
            {
                for (int i = _topNKeys.Count - 1; i >= 0; i--)
                {
                    yield return _topNValues[i];
                }
            }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            for (int i = _topNKeys.Count - 1; i>=0; i--)
            {
                yield return new KeyValuePair<TKey, TValue>(_topNKeys[i], _topNValues[i]);
            }
        }
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
    }
}
