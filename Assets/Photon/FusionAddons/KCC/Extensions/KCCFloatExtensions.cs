using System.Runtime.CompilerServices;

namespace Fusion.Addons.KCC
{
    public static class KCCFloatExtensions
    {
        // PUBLIC METHODS

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNaN(this float value)
        {
            return float.IsNaN(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAlmostZero(this float value, float tolerance = 0.01f)
        {
            return value < tolerance && value > -tolerance;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AlmostEquals(this float valueA, float valueB, float tolerance = 0.01f)
        {
            return IsAlmostZero(valueA - valueB, tolerance);
        }
    }
}