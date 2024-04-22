namespace Fusion.Addons.KCC
{
    public sealed class SmoothFloat : SmoothValue<float>
    {
        // CONSTRUCTORS

        public SmoothFloat(int records) : base(records)
        {
        }

        // PUBLIC METHODS

        public void FilterValues(bool positive, bool negative)
        {
            var items = Items;
            SmoothItem<float> item;

            if (positive)
                for (int i = 0, count = items.Length; i < count; ++i)
                {
                    item = items[i];
                    if (item.Value > 0.0f) item.Value = 0.0f;
                }

            if (negative)
                for (int i = 0, count = items.Length; i < count; ++i)
                {
                    item = items[i];
                    if (item.Value < 0.0f) item.Value = 0.0f;
                }
        }

        // SmoothValue INTERFACE

        protected override float GetDefaultValue()
        {
            return 0.0f;
        }

        protected override float AccumulateValue(float accumulatedValue, float value, double scale)
        {
            return (float)(accumulatedValue + value * scale);
        }

        protected override float GetSmoothValue(float accumulatedValue, double scale)
        {
            return (float)(accumulatedValue * scale);
        }
    }
}