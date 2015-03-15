
using System;

namespace KittyHawk.MqttLib.Collections
{
    /// <summary>
    /// Due to the lack of a collection type that is common across all platforms, I've had to create a simple
    /// auto-expanding array type for simple collection types to use.
    /// </summary>
    internal class AutoExpandingArray
    {
        private const int StartingSize = 4;
        private int _arraySize;
        private int _nextInsertIndex;
        private object[] _list;

        internal AutoExpandingArray()
        {
            Clear();
        }

        public int Add(object item)
        {
            if (_nextInsertIndex == _list.Length)
            {
                GrowArray();
            }

            int index = _nextInsertIndex;
            _list[index] = item;
            _nextInsertIndex++;
            return index;
        }

        /// <summary>
        /// Calls IDispose.Dispose on each item IF they are derived from IDisposable, then clears the array.
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < _nextInsertIndex; i++)
            {
                var disposable = _list[i] as IDisposable;
                if (disposable != null)
                {
                    disposable.Dispose();
                }
            }

            _nextInsertIndex = 0;
            _arraySize = StartingSize;
            _list = new object[_arraySize];
        }

        public bool Contains(object item)
        {
            for (int i = 0; i < _nextInsertIndex; i++)
            {
                if (item.Equals(_list[i]))
                {
                    return true;
                }
            }
            return false;
        }

        public int IndexOf(object item)
        {
            for (int i = 0; i < _list.Length; i++)
            {
                if (item.Equals(_list[i]))
                {
                    return i;
                }
            }
            return -1;
        }

        public object GetAt(int index)
        {
            if (index >= _nextInsertIndex)
            {
                throw new ArgumentException("Index out of range.");
            }

            return _list[index];
        }

        public void Remove(object item)
        {
            for (int i = 0; i < _list.Length; i++)
            {
                if (item.Equals(_list[i]))
                {
                    ShiftDown(i);
                    return;
                }
            }
        }

        public void RemoveAt(int index)
        {
            if (index >= _nextInsertIndex)
            {
                throw new ArgumentException("Index out of range.");
            }
            var disposable = _list[index] as IDisposable;
            if (disposable != null)
            {
                disposable.Dispose();
            }
            ShiftDown(index);
        }

        public int Count
        {
            get { return _nextInsertIndex; }
        }

        private void GrowArray()
        {
            _arraySize *= 2;
            var newList = new object[_arraySize];
            Array.Copy(_list, newList, _list.Length);
            _list = newList;
        }

        private void ShiftDown(int indexToRemove)
        {
            for (int i = indexToRemove; i < _nextInsertIndex - 1; i++)
            {
                _list[i] = _list[i + 1];
            }
            _nextInsertIndex--;
        }
    }
}
