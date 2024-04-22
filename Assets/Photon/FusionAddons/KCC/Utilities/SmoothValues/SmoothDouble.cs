namespace Fusion.Addons.KCC
{
    public sealed class SmoothDouble : SmoothValue<double>
    {
        // CONSTRUCTORS

        public SmoothDouble(int records) : base(records)
        {
        }

        // PUBLIC METHODS

        public void FilterValues(bool positive, bool negative)
        {
            var items = Items;
            SmoothItem<double> item;

            if (positive)
                for (int i = 0, count = items.Length; i < count; ++i)
                {
                    item = items[i];
                    if (item.Value > 0.0) item.Value = 0.0;
                }

            if (negative)
                for (int i = 0, count = items.Length; i < count; ++i)
                {
                    item = items[i];
                    if (item.Value < 0.0) item.Value = 0.0;
                }
        }

        // SmoothValue INTERFACE

        protected override double GetDefaultValue()
        {
            return 0.0;
        }

        protected override double AccumulateValue(double accumulatedValue, double value, double scale)
        {
            return accumulatedValue + value * scale;
        }

        protected override double GetSmoothValue(double accumulatedValue, double scale)
        {
            return accumulatedValue * scale;
        }
    }
}