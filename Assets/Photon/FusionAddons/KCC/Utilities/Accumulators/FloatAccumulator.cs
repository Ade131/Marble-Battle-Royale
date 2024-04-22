using UnityEngine;

namespace Fusion.Addons.KCC
{
    public sealed class FloatAccumulator
    {
        private int _lastAccumulateFrame;
        private int _lastConsumeFrame;

        // PRIVATE MEMBERS

        private readonly SmoothFloat _smoothValues = new(256);
        private float _unprocessedDeltaTime;

        private float _unprocessedValue;
        // PUBLIC MEMBERS

        public float SmoothingWindow;
        public bool UseDirectionFilter;

        // CONSTRUCTORS

        public FloatAccumulator() : this(default, default)
        {
        }

        public FloatAccumulator(float smoothingWindow) : this(smoothingWindow, default)
        {
        }

        public FloatAccumulator(float smoothingWindow, bool useDirectionFilter)
        {
            SmoothingWindow = smoothingWindow;
            UseDirectionFilter = useDirectionFilter;
        }

        public float AccumulatedValue { get; private set; }

        // PUBLIC METHODS

        public void Accumulate(float value)
        {
            var currentFrame = Time.frameCount;
            if (currentFrame == _lastAccumulateFrame)
                return;

            var unscaledDeltaTime = Time.unscaledDeltaTime;

            // Calculate average value if the smoothing window is valid.
            if (SmoothingWindow > 0.0f)
            {
                // Clear all values in opposite direction for instant flip.
                if (UseDirectionFilter) _smoothValues.FilterValues(value < 0.0f, value > 0.0f);

                // Add or update value for current frame.
                _smoothValues.AddValue(currentFrame, unscaledDeltaTime, value);

                // Calculate smooth value.
                value = _smoothValues.CalculateSmoothValue(SmoothingWindow, unscaledDeltaTime);
            }

            AccumulatedValue += value;

            _unprocessedValue = value;
            _unprocessedDeltaTime = unscaledDeltaTime;
            _lastAccumulateFrame = currentFrame;
        }

        public float Consume()
        {
            var consumeValue = AccumulatedValue;

            AccumulatedValue = default;
            _unprocessedValue = default;
            _unprocessedDeltaTime = default;
            _lastConsumeFrame = Time.frameCount;

            return consumeValue;
        }

        public float ConsumeTickAligned(NetworkRunner runner)
        {
            var currentFrame = Time.frameCount;

            // Revert accumulated value to state before latest accumulation.
            var consumeValue = AccumulatedValue - _unprocessedValue;

            // In the first call (within single Unity frame) we want to accumulate only "missing" part since last Render to align timing with fixed tick (last Runner.LocalAlpha => 1.0).
            // All subsequent calls return remaining value which is not yet consumed, but again within alignment limits of fixed ticks (0.0 => 1.0 = current => next).
            var baseAlpha = _lastConsumeFrame != currentFrame ? runner.LocalAlpha : 0.0f;

            // Here we calculate delta time between last Render time (or last tick aligned time) and time of the pending simulation tick.
            var tickAlignedDeltaTime = (1.0f - baseAlpha) * runner.DeltaTime;

            // The unprocessed value is usually not aligned with ticks, we need to remove delta which is ahead of fixed tick time.
            var tickAlignedValue = _unprocessedValue * Mathf.Clamp01(tickAlignedDeltaTime / _unprocessedDeltaTime);

            // Accumulate consume value up to next aligned tick time.
            consumeValue += tickAlignedValue;

            // Decrease remaining unprocessed value by the partial value consumed by accumulation.
            _unprocessedValue -= tickAlignedValue;

            // Decrease remaining unprocessed delta time by the partial delta time consumed by accumulation.
            _unprocessedDeltaTime -= tickAlignedDeltaTime;

            // Removed the calculated consume value from total accumulated value. This is a remaining value that will be polled with for next tick.
            AccumulatedValue -= consumeValue;

            _lastConsumeFrame = currentFrame;

            return consumeValue;
        }

        public void Clear()
        {
            _smoothValues.ClearValues();

            AccumulatedValue = default;
            _unprocessedValue = default;
            _unprocessedDeltaTime = default;
        }
    }
}