using System.Runtime.CompilerServices;
using UnityEngine;

namespace Fusion.Addons.KCC
{
    public static unsafe class KCCNetworkUtility
    {
        // CONSTANTS

        public const int WORD_COUNT_BOOL = 1;
        public const int WORD_COUNT_INT = 1;
        public const int WORD_COUNT_FLOAT = 1;
        public const int WORD_COUNT_VECTOR3 = 3;

        // PUBLIC METHODS

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ReadBool(int* ptr)
        {
            return *ptr != 0 ? true : false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteBool(int* ptr, bool value)
        {
            *ptr = value ? 1 : 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadInt(int* ptr)
        {
            return *ptr;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt(int* ptr, int value)
        {
            *ptr = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ReadFloat(int* ptr)
        {
            return *(float*)ptr;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteFloat(int* ptr, float value)
        {
            *(float*)ptr = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ReadVector3(int* ptr)
        {
            Vector3 value;
            value.x = *(float*)ptr;
            value.y = *(float*)(ptr + 1);
            value.z = *(float*)(ptr + 2);
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteVector3(int* ptr, Vector3 value)
        {
            *(float*)ptr = value.x;
            *(float*)(ptr + 1) = value.y;
            *(float*)(ptr + 2) = value.z;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static KCCNetworkID ReadNetworkID(int* ptr)
        {
            var uintPtr = (uint*)ptr;
            var networkID = new KCCNetworkID();
            networkID.Value0 = *(uintPtr + 0);
            networkID.Value1 = *(uintPtr + 1);
            return networkID;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static KCCNetworkID ReadNetworkID(NetworkBehaviourBuffer buffer, int offset)
        {
            var networkID = new KCCNetworkID();
            networkID.Value0 = buffer.ReinterpretState<uint>(offset + 0);
            networkID.Value1 = buffer.ReinterpretState<uint>(offset + 1);
            return networkID;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteNetworkID(int* ptr, KCCNetworkID networkID)
        {
            var uintPtr = (uint*)ptr;
            *(uintPtr + 0) = networkID.Value0;
            *(uintPtr + 1) = networkID.Value1;
        }
    }
}