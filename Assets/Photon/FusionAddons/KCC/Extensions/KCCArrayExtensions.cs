using System;
using System.Runtime.CompilerServices;

namespace Fusion.Addons.KCC
{
    public static class KCCArrayExtensions
    {
        // PUBLIC METHODS

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Clear(this Array array)
        {
            Array.Clear(array, 0, array.Length);
        }
    }
}