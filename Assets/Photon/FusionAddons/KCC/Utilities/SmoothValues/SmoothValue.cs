using System;

namespace Fusion.Addons.KCC
{
    public sealed class SmoothItem<T>
    {
        public int Frame;
        public double Size;
        public T Value;
    }

    public abstract class SmoothValue<T>
    {
        private int _index;

        // PRIVATE MEMBERS

        // CONSTRUCTORS

        public SmoothValue(int capacity)
        {
            Items = new SmoothItem<T>[capacity];

            for (var i = 0; i < capacity; ++i) Items[i] = new SmoothItem<T>();
        }
        // PUBLIC MEMBERS

        public SmoothItem<T>[] Items { get; }

        // PUBLIC METHODS

        public void AddValue(int frame, double size, T value)
        {
            if (size <= 0.0)
                throw new ArgumentException(nameof(size));

            var item = Items[_index];
            if (item.Frame == frame)
            {
                item.Size = size;
                item.Value = value;
                return;
            }

            _index = (_index + 1) % Items.Length;

            item = Items[_index];
            item.Frame = frame;
            item.Size = size;
            item.Value = value;
        }

        public void ClearValues()
        {
            SmoothItem<T> smoothItem;

            for (int i = 0, count = Items.Length; i < count; ++i)
            {
                smoothItem = Items[i];
                smoothItem.Frame = default;
                smoothItem.Size = default;
                smoothItem.Value = GetDefaultValue();
            }

            _index = default;
        }

        public T CalculateSmoothValue(double window, double size)
        {
            return CalculateSmoothValue(window, size, out var frames);
        }

        public T CalculateSmoothValue(double window, double size, out int frames)
        {
            SmoothItem<T> item;

            frames = default;

            if (window <= 0.0f)
            {
                item = Items[_index];
                if (item.Frame == default)
                    return default;

                frames = 1;
                return item.Value;
            }

            var remainingWindow = window;
            var accumulatedValue = GetDefaultValue();

            for (var i = _index; i >= 0; --i)
            {
                item = Items[i];
                if (item.Frame == default)
                    continue;

                if (remainingWindow <= item.Size)
                {
                    var scale = remainingWindow / item.Size;

                    accumulatedValue = AccumulateValue(accumulatedValue, item.Value, scale);

                    ++frames;

                    return GetSmoothValue(accumulatedValue, size / window);
                }

                remainingWindow -= item.Size;
                accumulatedValue = AccumulateValue(accumulatedValue, item.Value, 1.0);

                ++frames;
            }

            for (var i = Items.Length - 1; i > _index; --i)
            {
                item = Items[i];
                if (item.Frame == default)
                    continue;

                if (remainingWindow < item.Size)
                {
                    var scale = remainingWindow / item.Size;

                    accumulatedValue = AccumulateValue(accumulatedValue, item.Value, scale);

                    ++frames;

                    return GetSmoothValue(accumulatedValue, size / window);
                }

                remainingWindow -= item.Size;
                accumulatedValue = AccumulateValue(accumulatedValue, item.Value, 1.0);

                ++frames;
            }

            if (remainingWindow >= window)
                return default;

            var accumulatedSize = window - remainingWindow;

            return GetSmoothValue(accumulatedValue, size / accumulatedSize);
        }

        // SmoothValue INTERFACE

        protected abstract T GetDefaultValue();
        protected abstract T AccumulateValue(T accumulatedValue, T value, double scale);
        protected abstract T GetSmoothValue(T accumulatedValue, double scale);
    }
}