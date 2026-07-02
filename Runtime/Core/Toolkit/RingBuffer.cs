using System;

namespace GestureInput.Core
{
    /// <summary>
    /// Fixed-capacity sliding window of the most recent samples. Adding beyond
    /// capacity silently evicts the oldest element. Index 0 is the oldest kept
    /// sample; <see cref="Latest"/> is the newest. Not thread-safe.
    /// </summary>
    public sealed class RingBuffer<T>
    {
        private readonly T[] _items;
        private int _start;
        private int _count;

        public RingBuffer(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Capacity must be positive.");
            _items = new T[capacity];
        }

        public int Capacity => _items.Length;
        public int Count => _count;
        public bool IsFull => _count == _items.Length;

        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= _count)
                    throw new ArgumentOutOfRangeException(nameof(index), index, $"Buffer holds {_count} item(s).");
                return _items[(_start + index) % _items.Length];
            }
        }

        public T Latest
        {
            get
            {
                if (_count == 0) throw new InvalidOperationException("RingBuffer is empty.");
                return this[_count - 1];
            }
        }

        public void Add(T item)
        {
            if (IsFull)
            {
                _items[_start] = item;
                _start = (_start + 1) % _items.Length;
            }
            else
            {
                _items[(_start + _count) % _items.Length] = item;
                _count++;
            }
        }

        public void Clear()
        {
            Array.Clear(_items, 0, _items.Length);
            _start = 0;
            _count = 0;
        }
    }
}
