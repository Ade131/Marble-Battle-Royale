using Fusion;
using Fusion.Menu;
using UnityEngine;

namespace MultiClimb.Menu
{
    public class MenuConnectionBehaviour : FusionMenuConnectionBehaviour
    {
        [SerializeField] private FusionMenuConfig config;

        [Space]
        [Header(
            "Provide a NetworkRunner prefab to be instantiated.\nIf no prefab is provided, a simple one will be created.")]
        [SerializeField]
        private NetworkRunner networkRunnerPrefab;

        private void Awake()
        {
            if (!config)
                Log.Error("Fusion menu configuration file not provided.");
        }

        public override IFusionMenuConnection Create()
        {
            return new MenuConnection(config, networkRunnerPrefab);
        }
    }
}