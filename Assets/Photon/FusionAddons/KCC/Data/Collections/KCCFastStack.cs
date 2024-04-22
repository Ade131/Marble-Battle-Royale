using System;

namespace Fusion.Addons.KCC
{
	/// <summary>
	///     Custom stack implementation for fast push/pop and pre-allocation.
	///     Only for internal KCC purposes.
	/// </summary>
	public sealed class KCCFastStack<T> where T : class, new()
    {
        private int _count;
        private T[] _items;

        public KCCFastStack(int capacity, bool createInstances)
        {
            _items = new T[capacity];
            _count = default;

            if (createInstances)
            {
                _count = capacity;

                for (var i = 0; i < capacity; ++i) _items[i] = new T();
            }
        }

        public T PopOrCreate()
        {
            if (_count > 0)
            {
                --_count;
                return _items[_count];
            }

            return new T();
        }

        public void Push(T item)
        {
            if (_count == _items.Length) Array.Resize(ref _items, _items.Length * 2);

            _items[_count] = item;
            ++_count;
        }
    }
}