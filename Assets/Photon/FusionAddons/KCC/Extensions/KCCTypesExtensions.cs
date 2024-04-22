using System.Runtime.CompilerServices;

namespace Fusion.Addons.KCC
{
    public static class KCCTypesExtensions
    {
        // PUBLIC METHODS

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Has(this EKCCFeatures features, EKCCFeature feature)
        {
            return ((int)features & (1 << (int)feature)) != 0;
        }
    }
}