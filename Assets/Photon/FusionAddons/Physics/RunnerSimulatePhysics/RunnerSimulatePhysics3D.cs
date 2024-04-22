using Fusion.Analyzer;
using UnityEngine;

namespace Fusion.Addons.Physics
{
  /// <summary>
  ///     Fusion component for handling Physics.Simulate(). When added to a <see cref="NetworkRunner" /> GameObject, this
  ///     will automatically disable
  /// </summary>
  [DisallowMultipleComponent]
    public class RunnerSimulatePhysics3D : RunnerSimulatePhysicsBase<PhysicsScene>
    {
        // Unity 2022.3 made changes to PhysX (Physics3D) which made it similar to Box2d (Physics2D)
        // This changed from a basic Physics.AutoSimulate to Simulations options for FixedUpdate, Update or Script(the equivalent of auto-simulate disabled).
#if !UNITY_2022_3_OR_NEWER
    /// <inheritdoc/>
    protected override PhysicsTimings UnityPhysicsMode => Physics.autoSimulation ? PhysicsTimings.FixedUpdate : PhysicsTimings.Script;
    
    /// <inheritdoc/>
    protected override void OverrideAutoSimulate(bool set) {
      
      if (set && _physicsTiming == PhysicsTimings.Update) {
        Debug.LogWarning($"{GetType().Name}.{nameof(_physicsTiming)} set to {PhysicsTimings.Update}, which is not valid in Unity versions below 2022.3. Changing {_physicsAuthority} to {PhysicsAuthorities.Fusion}");
        _physicsAuthority = PhysicsAuthorities.Fusion;
        set = false;
      }
      _physicsAutoSimRestore = Physics.autoSimulation ? PhysicsTimings.FixedUpdate : PhysicsTimings.Script;
      Physics.autoSimulation = set;
    }
    
    /// <inheritdoc/>
    protected override void RestoreAutoSimulate() {
      Physics.autoSimulation = _physicsAutoSimRestore == PhysicsTimings.FixedUpdate ? true : false;
    }
#else
      /// <inheritdoc />
      protected override PhysicsTimings UnityPhysicsMode => (PhysicsTimings)UnityEngine.Physics.simulationMode;

      /// <inheritdoc />
      protected override void OverrideAutoSimulate(bool set)
        {
            _physicsAutoSimRestore = (PhysicsTimings)UnityEngine.Physics.simulationMode;
            if (set)
                UnityEngine.Physics.simulationMode = (SimulationMode)_physicsTiming;
            else
                UnityEngine.Physics.simulationMode = SimulationMode.Script;
        }

      /// <inheritdoc />
      protected override void RestoreAutoSimulate()
        {
            UnityEngine.Physics.simulationMode = (SimulationMode)_physicsAutoSimRestore;
        }
#endif

        [StaticField(StaticFieldResetMode.None)]
        private static bool? _physicsAutoSyncRestore;

        /// <inheritdoc />
        protected override bool AutoSyncTransforms
        {
            get => UnityEngine.Physics.autoSyncTransforms;
            set => UnityEngine.Physics.autoSyncTransforms = value;
        }

        /// <inheritdoc />
        protected override void SimulatePrimaryScene(float deltaTime)
        {
            if (Runner.SceneManager.TryGetPhysicsScene3D(out var physicsScene))
            {
                if (physicsScene.IsValid())
                    physicsScene.Simulate(deltaTime);
                else
                    UnityEngine.Physics.Simulate(deltaTime);
            }
        }

        /// <inheritdoc />
        protected override void SimulateAdditionalScenes(float deltaTime, bool isForward)
        {
            if (_additionalScenes == null || _additionalScenes.Count == 0) return;
            var defaultPhysicsScene = UnityEngine.Physics.defaultPhysicsScene;
            foreach (var scene in _additionalScenes)
                if (!scene.ForwardOnly || isForward)
                {
#if UNITY_2022_3_OR_NEWER
                    if (scene.PhysicsScene != defaultPhysicsScene ||
                        UnityEngine.Physics.simulationMode == SimulationMode.Script)
                    {
#else
          if (scene.PhysicsScene != defaultPhysicsScene || Physics.autoSimulation == false) {
#endif
                        scene.PhysicsScene.Simulate(deltaTime);
                    }
                }
        }
    }
}