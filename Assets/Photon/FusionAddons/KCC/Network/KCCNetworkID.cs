using System.Runtime.InteropServices;

namespace Fusion.Addons.KCC
{
    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct KCCNetworkID
    {
        // CONSTANTS

        public const int WORD_COUNT = 2;

        // PUBLIC MEMBERS

        [FieldOffset(0)] public uint Value0;

        [FieldOffset(4)] public uint Value1;

        public bool IsValid => Value1 != default;

        // PUBLIC METHODS

        public bool Equals(KCCNetworkID other)
        {
            return Value0 == other.Value0 && Value1 == other.Value1;
        }

        public static KCCNetworkID GetNetworkID(NetworkObject networkObject)
        {
            if (networkObject == null)
                return default;

            var networkID = new KCCNetworkID();

            if (networkObject.Id.IsValid)
            {
                networkID.Value0 = networkObject.Id.Raw;
                networkID.Value1 = 1U;
            }
            else
            {
                var networkTypeId = networkObject.NetworkTypeId;
                var networkTypeIdAsKCCNetworkID = *(KCCNetworkID*)&networkTypeId;
                networkID.Value0 = networkTypeIdAsKCCNetworkID.Value0;
                networkID.Value1 = 2U | (networkTypeIdAsKCCNetworkID.Value1 << 2);
            }

            return networkID;
        }

        public static NetworkObject GetNetworkObject(NetworkRunner runner, KCCNetworkID networkID)
        {
            var type = networkID.Value1 & 3U;
            if (type == 1U)
            {
                var networkId = new NetworkId();
                networkId.Raw = networkID.Value0;
                return runner.FindObject(networkId);
            }

            if (type == 2U)
            {
                var networkIDAsNetworkTypeId = new KCCNetworkID();
                networkIDAsNetworkTypeId.Value0 = networkID.Value0;
                networkIDAsNetworkTypeId.Value1 = networkID.Value1 >> 2;

                var networkObjectTypeId = *(NetworkObjectTypeId*)&networkIDAsNetworkTypeId;
                if (networkObjectTypeId.IsPrefab)
                    return runner.Config.PrefabTable.Load(networkObjectTypeId.AsPrefabId, true);
            }

            return default;
        }
    }
}