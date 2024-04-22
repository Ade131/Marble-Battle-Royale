using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.Profiling;
using UnityEngine;

namespace Fusion.Addons.KCC
{
    using ReadOnlyProcessors = ReadOnlyCollection<IKCCProcessor>;

    /// <summary>
    ///     Fusion kinematic character controller component.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed partial class KCC : NetworkTRSP, IAfterSpawned, IAfterClientPredictionReset, IBeforeTick, IAfterTick
    {
        // CONSTANTS

        public const int CACHE_SIZE = 64;
        public const int HISTORY_SIZE = 60;
        public const string TRACING_SCRIPT_DEFINE = "KCC_TRACE";

        private static ProfilerMarker _fixedUpdateMarker = new("KCC.FixedUpdate");
        private static ProfilerMarker _renderUpdateMarker = new("KCC.RenderUpdate");
        private static ProfilerMarker _restoreStateMarker = new("KCC.RestoreState");
        private static ProfilerMarker _afterTickMarker = new("KCC.AfterTick");
        private static ProfilerMarker _simulatedMoveMarker = new("Simulated Move");
        private static ProfilerMarker _interpolationMarker = new("Interpolation");

        // PRIVATE MEMBERS

        [SerializeField] private KCCSettings _settings = new();

        private readonly List<KCCStageInfo> _activeStages = new();
        private readonly Collider[] _addColliders = new Collider[CACHE_SIZE];
        private readonly AfterMoveStep _afterMoveStep = new();
        private readonly BeginMove _beginMove = new();
        private int _cachedProcessorCount;
        private readonly IKCCProcessor[] _cachedProcessors = new IKCCProcessor[CACHE_SIZE * 2];
        private readonly List<Collider> _childColliders = new();
        private readonly KCCCollider _collider = new();
        private readonly KCCSettings _defaultSettings = new();
        private readonly EndMove _endMove = new();
        private readonly KCCOverlapInfo _extendedOverlapInfo = new(CACHE_SIZE);
        private readonly KCCData[] _historyData = new KCCData[HISTORY_SIZE];
        private readonly Collider[] _hitColliders = new Collider[CACHE_SIZE];

        private bool _isInFixedUpdate;
        private Vector3 _lastAntiJitterPosition;
        private Collider _lastNonNetworkedCollider;
        private int _lastPredictedLookRotationFrame;
        private int _lastPredictedRenderFrame;
        private Vector3 _lastRenderPosition;
        private float _lastRenderTime;
        private readonly List<IKCCProcessor> _localProcessors = new();
        private Vector3 _predictionError;
        private readonly PrepareData _prepareData = new();
        private readonly RaycastHit[] _raycastHits = new RaycastHit[CACHE_SIZE];
        private readonly Collider[] _removeColliders = new Collider[CACHE_SIZE];
        private readonly KCCCollision[] _removeCollisions = new KCCCollision[CACHE_SIZE];
        private readonly KCCResolver _resolver = new(CACHE_SIZE);
        private int _stageProcessorCount;
        private readonly IKCCProcessor[] _stageProcessors = new IKCCProcessor[CACHE_SIZE * 2];
        private readonly KCCOverlapInfo _trackOverlapInfo = new(CACHE_SIZE);

        // NetworkBehaviour INTERFACE

        public override int? DynamicWordCount => GetNetworkDataWordCount();

        // MonoBehaviour INTERFACE

        private void Awake()
        {
            Transform = transform;

            Rigidbody = GetComponent<Rigidbody>();
            Rigidbody.isKinematic = true;

            LocalProcessors = new ReadOnlyProcessors(_localProcessors);

            RefreshCollider();
        }

        private void OnDestroy()
        {
            SetDefaults(true);
        }

        private void OnDrawGizmosSelected()
        {
            if (_settings == null)
                return;

            var radius = Mathf.Max(0.01f, _settings.Radius);
            var height = Mathf.Max(radius * 2.0f, _settings.Height);

            var basePosition = transform.position;

            var gizmosColor = Gizmos.color;

            var baseLow = basePosition + Vector3.up * radius;
            var baseHigh = basePosition + Vector3.up * (height - radius);
            var offsetFront = Vector3.forward * radius;
            var offsetBack = Vector3.back * radius;
            var offsetLeft = Vector3.left * radius;
            var offsetRight = Vector3.right * radius;

            Gizmos.color = Color.green;

            Gizmos.DrawWireSphere(baseLow, radius);
            Gizmos.DrawWireSphere(baseHigh, radius);

            Gizmos.DrawLine(baseLow + offsetFront, baseHigh + offsetFront);
            Gizmos.DrawLine(baseLow + offsetBack, baseHigh + offsetBack);
            Gizmos.DrawLine(baseLow + offsetLeft, baseHigh + offsetLeft);
            Gizmos.DrawLine(baseLow + offsetRight, baseHigh + offsetRight);

            if (_settings.Extent > 0.0f)
            {
                var extendedRadius = radius + _settings.Extent;

                Gizmos.color = Color.yellow;

                Gizmos.DrawWireSphere(baseLow, extendedRadius);
                Gizmos.DrawWireSphere(baseHigh, extendedRadius);
            }

            Gizmos.color = gizmosColor;
        }

        // IAfterClientPredictionReset INTERFACE

        void IAfterClientPredictionReset.AfterClientPredictionReset()
        {
            int latestServerTick = Runner.LatestServerTick;

            Trace(nameof(IAfterClientPredictionReset.AfterClientPredictionReset), $"Tick:{latestServerTick}");

            _restoreStateMarker.Begin();

            var historyData = _historyData[latestServerTick % HISTORY_SIZE];
            if (historyData != null && historyData.Tick == latestServerTick)
            {
                FixedData.CopyFromOther(historyData);
                FixedData.Frame = Time.frameCount;
            }

            ReadNetworkData();

            if (historyData != default) RestoreHistoryData(historyData);

            RefreshCollider();

            if (FixedData.IsActive) SynchronizeTransform(FixedData, true, true, false);

            LastPredictedFixedTick = latestServerTick;

            _restoreStateMarker.End();
        }

        // IAfterSpawned INTERFACE

        void IAfterSpawned.AfterSpawned()
        {
            RenderData.CopyFromOther(FixedData);

            if (Object.IsInSimulation) WriteNetworkData();

            _isInFixedUpdate = false;
        }

        // IAfterTick INTERFACE

        void IAfterTick.AfterTick()
        {
            Trace(nameof(IAfterTick.AfterTick));

            _afterTickMarker.Begin();

            PublishFixedData(true, true);
            WriteNetworkData();

            _isInFixedUpdate = false;

            _afterTickMarker.End();
        }

        // IBeforeTick INTERFACE

        void IBeforeTick.BeforeTick()
        {
            _isInFixedUpdate = true;

            FixedData.Frame = Time.frameCount;
            FixedData.Tick = Runner.Tick.Raw;
            FixedData.Alpha = 0.0f;
            FixedData.Time = Runner.SimulationTime;
            FixedData.DeltaTime = Runner.DeltaTime;
            FixedData.UpdateDeltaTime = FixedData.DeltaTime;

            Trace(nameof(IBeforeTick.BeforeTick), "[Fixed Update Initialization]", $"Time:{FixedData.Time:F6}",
                $"DeltaTime:{FixedData.DeltaTime:F6}", $"Alpha:{FixedData.Alpha:F4}",
                $"HasInputAuthority:{Object.HasInputAuthority}", $"HasStateAuthority:{Object.HasStateAuthority}");
        }

        private event Action<KCC> _onSpawn;

        // PUBLIC METHODS

        /// <summary>
        ///     Immediately synchronize Transform and Rigidbody based on current state.
        /// </summary>
        public void SynchronizeTransform(bool synchronizePosition, bool synchronizeRotation,
            bool allowAntiJitter = true)
        {
            if (IsInFixedUpdate) allowAntiJitter = false;

            SynchronizeTransform(Data, synchronizePosition, synchronizeRotation, allowAntiJitter);
        }

        /// <summary>
        ///     Refresh child colliders list, used for collision filtering.
        ///     Child colliders are ignored completely, triggers are treated as valid collision.
        /// </summary>
        public void RefreshChildColliders()
        {
            _childColliders.Clear();

            GetComponentsInChildren(true, _childColliders);

            var currentIndex = 0;
            var lastIndex = _childColliders.Count - 1;

            while (currentIndex <= lastIndex)
            {
                var childCollider = _childColliders[currentIndex];
                if (childCollider.isTrigger || childCollider == _collider.Collider)
                {
                    _childColliders[currentIndex] = _childColliders[lastIndex];
                    _childColliders.RemoveAt(lastIndex);

                    --lastIndex;
                }
                else
                {
                    ++currentIndex;
                }
            }
        }

        /// <summary>
        ///     Returns fixed data for specific tick in history. Default history size is 60 ticks.
        /// </summary>
        public KCCData GetHistoryData(int tick)
        {
            if (tick < 0)
                return null;

            var data = _historyData[tick % HISTORY_SIZE];
            if (data != null && data.Tick == tick)
                return data;

            return null;
        }

        /// <summary>
        ///     Controls whether update methods are driven by default Fusion methods or called manually using
        ///     <c>ManualFixedUpdate()</c> and <c>ManualRenderUpdate()</c>.
        /// </summary>
        public void SetManualUpdate(bool hasManualUpdate)
        {
            HasManualUpdate = hasManualUpdate;
        }

        /// <summary>
        ///     Invokes callback when the KCC is spawned.
        ///     If the KCC is already spawned, the callback is invoked immediately.
        /// </summary>
        public void InvokeOnSpawn(Action<KCC> callback)
        {
            if (IsSpawned)
            {
                callback(this);
            }
            else
            {
                _onSpawn -= callback;
                _onSpawn += callback;
            }
        }

        /// <summary>
        ///     Manual fixed update execution, <c>SetManualUpdate(true)</c> must be called prior usage.
        /// </summary>
        public void ManualFixedUpdate()
        {
            if (IsSpawned == false)
                return;
            if (HasManualUpdate == false)
                throw new InvalidOperationException($"[{name}] Manual update is not set!");

            _fixedUpdateMarker.Begin();
            OnFixedUpdateInternal();
            _fixedUpdateMarker.End();
        }

        /// <summary>
        ///     Manual render update execution, <c>SetManualUpdate(true)</c> must be called prior usage.
        /// </summary>
        public void ManualRenderUpdate()
        {
            if (IsSpawned == false)
                return;
            if (HasManualUpdate == false)
                throw new InvalidOperationException($"[{name}] Manual update is not set!");

            _renderUpdateMarker.Begin();
            OnRenderUpdateInternal();
            _renderUpdateMarker.End();
        }

        /// <summary>
        ///     Explicit interpolation on demand. Implicit interpolation in render update is not skipped!
        /// </summary>
        /// <param name="alpha">
        ///     Custom interpolation alpha. Valid range is 0.0 - 1.0, otherwise default value from
        ///     <c>TryGetSnapshotsBuffers()</c> is used.
        /// </param>
        public void Interpolate(float alpha = -1.0f)
        {
            if (IsSpawned == false)
                return;

            var renderSource = RenderSource.Interpolated;
            var renderTimeframe = GetInterpolationTimeframe();

            Interpolate(renderSource, renderTimeframe, alpha);
        }

        /// <summary>
        ///     Explicit interpolation on demand. Implicit interpolation in render update is not skipped!
        /// </summary>
        /// <param name="renderSource">Custom render source.</param>
        /// <param name="renderTimeframe">Custom render timeframe.</param>
        /// <param name="alpha">
        ///     Custom interpolation alpha. Valid range is 0.0 - 1.0, otherwise default value from
        ///     <c>TryGetSnapshotsBuffers()</c> is used.
        /// </param>
        public void Interpolate(RenderSource renderSource, RenderTimeframe renderTimeframe, float alpha = -1.0f)
        {
            if (IsSpawned == false)
                return;

            InterpolateNetworkData(renderSource, renderTimeframe, alpha);

            if (RenderData.IsActive)
            {
                CacheProcessors(RenderData);
                InvokeOnInterpolate(RenderData);
                SynchronizeTransform(RenderData, true, true, false);
            }
        }

        public override void Spawned()
        {
            Trace(nameof(Spawned));

            if (IsSpawned)
                throw new InvalidOperationException($"[{name}] KCC is already spawned!");

            _defaultSettings.CopyFromOther(_settings);

            SetDefaults(false);

            IsSpawned = true;
            _isInFixedUpdate = true;

            KCCUtility.GetClampedLookRotationAngles(Transform.rotation, out var lookPitch, out var lookYaw);

            FixedData = new KCCData();
            FixedData.Frame = Time.frameCount;
            FixedData.Tick = Runner.Tick.Raw;
            FixedData.Time = Runner.SimulationTime;
            FixedData.DeltaTime = Runner.DeltaTime;
            FixedData.UpdateDeltaTime = FixedData.DeltaTime;
            FixedData.Gravity = UnityEngine.Physics.gravity;
            FixedData.MaxGroundAngle = 60.0f;
            FixedData.MaxWallAngle = 5.0f;
            FixedData.MaxHangAngle = 30.0f;
            FixedData.BasePosition = Transform.position;
            FixedData.DesiredPosition = Transform.position;
            FixedData.TargetPosition = Transform.position;
            FixedData.LookPitch = lookPitch;
            FixedData.LookYaw = lookYaw;

            LastPredictedFixedTick = FixedData.Tick;
            _lastPredictedRenderFrame = FixedData.Frame;
            _lastPredictedLookRotationFrame = FixedData.Frame;

            if (Object.HasStateAuthority == false)
            {
                ReadNetworkData();
                SynchronizeTransform(FixedData, true, true, false);
            }

            RenderData = new KCCData();
            RenderData.CopyFromOther(FixedData);

            _lastRenderPosition = RenderData.TargetPosition;
            _lastAntiJitterPosition = RenderData.TargetPosition;

            RefreshCollider();
            RefreshChildColliders();

            var processors = _settings.Processors;
            if (processors != null)
                for (int i = 0, count = processors.Length; i < count; ++i)
                {
                    var processorObject = processors[i];
                    if (processorObject == null)
                    {
                        KCCUtility.Log(this, this, default, EKCCLogType.Warning,
                            $"Missing processor object - {nameof(KCCSettings)}.{nameof(KCCSettings.Processors)} at index {i}");
                        continue;
                    }

                    if (KCCUtility.ResolveProcessor(processorObject, out var processor, out var gameObject,
                            out var component, out var scriptableObject) == false)
                    {
                        KCCUtility.Log(this, processorObject, default, EKCCLogType.Error,
                            $"Failed to resolve {nameof(IKCCProcessor)} in {processorObject.name} ({processorObject.GetType().FullName}) - {nameof(KCCSettings)}.{nameof(KCCSettings.Processors)} at index {i}");
                        continue;
                    }

                    AddLocalProcessor(processor);
                }

            if (_onSpawn != null)
            {
                try
                {
                    _onSpawn(this);
                }
                catch (Exception exception)
                {
                    UnityEngine.Debug.LogException(exception);
                }

                _onSpawn = null;
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (IsSpawned == false)
                return;

            Trace(nameof(Despawned));

            ForceRemoveAllCollisions(FixedData);
            ForceRemoveAllModifiers(FixedData);

            while (_localProcessors.Count > 0) RemoveLocalProcessor(_localProcessors[_localProcessors.Count - 1]);

            SetDefaults(true);
        }

        public override void FixedUpdateNetwork()
        {
            if (HasManualUpdate)
                return;

            _fixedUpdateMarker.Begin();
            OnFixedUpdateInternal();
            _fixedUpdateMarker.End();
        }

        public override void Render()
        {
            if (HasManualUpdate)
                return;

            _renderUpdateMarker.Begin();
            OnRenderUpdateInternal();
            _renderUpdateMarker.End();
        }

        // PRIVATE METHODS

        private void OnFixedUpdateInternal()
        {
            if (IsInFixedUpdate == false)
                throw new InvalidOperationException(
                    $"[{name}] KCC fixed update called from render update! This is not allowed.");

            Trace(nameof(OnFixedUpdateInternal));

            Debug.BeforePredictedFixedMove(this);

            RefreshCollider();

            _simulatedMoveMarker.Begin();

            MovePredicted(FixedData);

            if (FixedData.IsActive) SynchronizeTransform(FixedData, true, true, false);

            PublishFixedData(false, true);

            LastPredictedFixedTick = FixedData.Tick;

            _simulatedMoveMarker.End();

            Debug.AfterPredictedFixedMove(this);
        }

        private void OnRenderUpdateInternal()
        {
            if (IsInFixedUpdate)
                throw new InvalidOperationException(
                    $"[{name}] KCC render update called from fixed update! This is not allowed.");

            Trace(nameof(OnRenderUpdateInternal));

            var frame = Time.frameCount;
            var deltaTime = Runner.DeltaTime;

            RenderData.Frame = frame;

            if (Object.IsInSimulation)
            {
                _simulatedMoveMarker.Begin();

                RenderData.Tick = Runner.Tick;
                RenderData.Alpha = Runner.LocalAlpha;

                var previousTime = RenderData.Time;

                RenderData.Time = Runner.SimulationTime + RenderData.Alpha * deltaTime;

                if (IsInterpolatingInRenderUpdate)
                {
                    RenderData.Tick -= 1;
                    RenderData.Time -= deltaTime;

                    if (RenderData.Frame == FixedData.Frame) previousTime -= deltaTime;
                }

                RenderData.DeltaTime = RenderData.Time - previousTime;
                RenderData.UpdateDeltaTime = RenderData.DeltaTime;

                UpdatePredictionError();
#if UNITY_EDITOR
                if (Debug.ShowPath)
                {
                    if (RenderData.Frame == FixedData.Frame)
                        UnityEngine.Debug.DrawLine(FixedData.TargetPosition, RenderData.TargetPosition,
                            KCCDebug.FixedToRenderPathColor, Debug.DisplayTime);
                    else
                        UnityEngine.Debug.DrawLine(_lastRenderPosition, RenderData.TargetPosition,
                            KCCDebug.PredictionCorrectionColor, Debug.DisplayTime);
                }
#endif
                if (IsPredictingInRenderUpdate)
                {
                    MovePredicted(RenderData);

                    _lastPredictedRenderFrame = frame;
                    _lastPredictedLookRotationFrame = frame;
                }
                else
                {
                    MoveInterpolated(RenderData);

                    if (_settings.ForcePredictedLookRotation) _lastPredictedLookRotationFrame = frame;
                }

                if (RenderData.IsActive) SynchronizeTransform(RenderData, true, true, true);

                _simulatedMoveMarker.End();
            }
            else
            {
                _interpolationMarker.Begin();

                if (_settings.ProxyInterpolationMode == EKCCInterpolationMode.Transform)
                {
                    InterpolateNetworkTransform();
                }
                else
                {
                    FixedData.Frame = frame;
                    FixedData.Tick = Object.LastReceiveTick;
                    FixedData.Alpha = 1.0f;
                    FixedData.Time = FixedData.Tick * deltaTime;
                    FixedData.DeltaTime = deltaTime;
                    FixedData.UpdateDeltaTime = deltaTime;

                    ReadNetworkData();

                    InterpolateNetworkData(RenderSource.Interpolated, RenderTimeframe.Remote);

                    if (RenderData.IsActive)
                    {
                        RefreshCollider();
                        CacheProcessors(RenderData);
                        InvokeOnInterpolate(RenderData);
                        SynchronizeTransform(RenderData, true, true, false);
                    }
                }

                _interpolationMarker.End();
            }

            _lastRenderPosition = RenderData.TargetPosition;
            _lastRenderTime = RenderData.Time;

            Debug.AfterRenderUpdate(this);
        }

        private void UpdatePredictionError()
        {
            if (ActiveFeatures.Has(EKCCFeature.PredictionCorrection) && RenderData.Frame == FixedData.Frame)
            {
                var current = GetHistoryData(RenderData.Tick);
                if (current != null && _lastRenderTime <= current.Time)
                    for (var i = 0; i < 5; ++i)
                    {
                        var previous = GetHistoryData(current.Tick - 1);
                        if (previous == null)
                            break;

                        if (_lastRenderTime >= previous.Time)
                        {
                            if (current.HasTeleported || previous.HasTeleported)
                            {
                                _predictionError = default;
                                return;
                            }

                            var deltaTime = current.Time - previous.Time;
                            if (deltaTime <= 0.000001f)
                            {
                                _predictionError = default;
                                return;
                            }

                            var lastRenderAlpha = (_lastRenderTime - previous.Time) / deltaTime;
                            var expectedRenderPosition = Vector3.Lerp(previous.TargetPosition, current.TargetPosition,
                                lastRenderAlpha);
#if UNITY_EDITOR
                            if (Debug.ShowPath)
                                UnityEngine.Debug.DrawLine(expectedRenderPosition, _lastRenderPosition,
                                    KCCDebug.PredictionErrorColor, Debug.DisplayTime);
#endif
                            _predictionError = _lastRenderPosition - expectedRenderPosition;
                            if (_predictionError.sqrMagnitude >=
                                _settings.TeleportThreshold * _settings.TeleportThreshold)
                            {
                                _predictionError = default;
                                return;
                            }

                            _predictionError = Vector3.Lerp(_predictionError, Vector3.zero,
                                _settings.PredictionCorrectionSpeed * Time.deltaTime);

                            RenderData.BasePosition += _predictionError;
                            RenderData.DesiredPosition += _predictionError;
                            RenderData.TargetPosition += _predictionError;

                            return;
                        }

                        current = previous;
                    }
            }

            if (_predictionError.IsAlmostZero(0.000001f))
            {
                _predictionError = default;
            }
            else
            {
                RenderData.BasePosition -= _predictionError;
                RenderData.DesiredPosition -= _predictionError;
                RenderData.TargetPosition -= _predictionError;

                _predictionError = Vector3.Lerp(_predictionError, Vector3.zero,
                    _settings.PredictionCorrectionSpeed * Time.deltaTime);

                RenderData.BasePosition += _predictionError;
                RenderData.DesiredPosition += _predictionError;
                RenderData.TargetPosition += _predictionError;
            }
        }

        private void MovePredicted(KCCData data)
        {
            ActiveFeatures = _settings.Features;

            var baseTime = data.Time;
            var baseDeltaTime = data.DeltaTime;
            var basePosition = data.TargetPosition;
            var desiredPosition = data.TargetPosition;
            var wasGrounded = data.IsGrounded;
            var wasSteppingUp = data.IsSteppingUp;
            var wasSnappingToGround = data.IsSnappingToGround;

            data.DeltaTime = baseDeltaTime;
            data.BasePosition = basePosition;
            data.DesiredPosition = desiredPosition;

            if (data.IsActive == false)
            {
                data.ClearTransientProperties();
                ForceRemoveAllCollisions(data);
                ForceRemoveAllHits(data);
                return;
            }

            CacheProcessors(data);
            SetBaseProperties(data);

            ExecuteStageInternal<IBeginMove, BeginMove>(_beginMove, data);

            if (data.IsActive == false)
            {
                data.ClearTransientProperties();
                ForceRemoveAllCollisions(data);
                ForceRemoveAllHits(data);

                ExecuteStageInternal<IEndMove, EndMove>(_endMove, data);

                return;
            }

            baseDeltaTime = data.DeltaTime;
            basePosition = data.BasePosition;

            if (baseDeltaTime < KCCSettings.ExtrapolationDeltaTimeThreshold)
            {
                var extrapolationVelocity = data.DesiredVelocity;
                if (data.RealVelocity.sqrMagnitude <= extrapolationVelocity.sqrMagnitude)
                    extrapolationVelocity = data.RealVelocity;

                desiredPosition = basePosition + extrapolationVelocity * baseDeltaTime;

                data.BasePosition = basePosition;
                data.DesiredPosition = desiredPosition;
                data.TargetPosition = desiredPosition;

                ExecuteStageInternal<IEndMove, EndMove>(_endMove, data);

                InvokeOnStay(data);

                return;
            }

            ExecuteStageInternal<IPrepareData, PrepareData>(_prepareData, data);

            ForceRemoveAllHits(data);

            var pendingDeltaTime = Mathf.Clamp01(baseDeltaTime);
            var pendingDeltaPosition = data.DesiredVelocity * pendingDeltaTime + data.ExternalDelta;

            desiredPosition = data.BasePosition + pendingDeltaPosition;

            data.DesiredPosition = desiredPosition;
            data.TargetPosition = data.BasePosition;
            data.ExternalDelta = default;

            var hasFinished = false;
            var radiusMultiplier = Mathf.Clamp(_settings.CCDRadiusMultiplier, 0.25f, 0.75f);
            var maxDeltaMagnitude = _settings.Radius * (radiusMultiplier + 0.1f);
            var optimalDeltaMagnitude = _settings.Radius * radiusMultiplier;
            var nonTeleportedPosition = data.TargetPosition;

            while (hasFinished == false && data.HasTeleported == false)
            {
                data.BasePosition = data.TargetPosition;

                var consumeDeltaTime = pendingDeltaTime;
                var consumeDeltaPosition = pendingDeltaPosition;

                if (ActiveFeatures.Has(EKCCFeature.CCD))
                {
                    var consumeDeltaPositionMagnitude = consumeDeltaPosition.magnitude;
                    if (consumeDeltaPositionMagnitude > maxDeltaMagnitude)
                    {
                        var deltaRatio = optimalDeltaMagnitude / consumeDeltaPositionMagnitude;

                        consumeDeltaTime *= deltaRatio;
                        consumeDeltaPosition *= deltaRatio;
                    }
                    else
                    {
                        hasFinished = true;
                    }
                }
                else
                {
                    hasFinished = true;
                }

                pendingDeltaTime -= consumeDeltaTime;
                pendingDeltaPosition -= consumeDeltaPosition;

                if (pendingDeltaTime <= 0.0f) pendingDeltaTime = 0.0f;

                data.Time = baseTime - pendingDeltaTime;
                data.DeltaTime = consumeDeltaTime;
                data.DesiredPosition = data.BasePosition + consumeDeltaPosition;
                data.TargetPosition = data.DesiredPosition;
                data.WasGrounded = data.IsGrounded;
                data.WasSteppingUp = data.IsSteppingUp;
                data.WasSnappingToGround = data.IsSnappingToGround;

                ProcessMoveStep(data);

                if (data.HasTeleported == false) nonTeleportedPosition = data.TargetPosition;

                UpdateCollisions(data, _trackOverlapInfo);

                if (data.HasTeleported)
                {
                    UpdateHits(data, null, EKCCHitsOverlapQuery.New);
                    UpdateCollisions(data, _trackOverlapInfo);
                }

                if (hasFinished && data.ExternalDelta.IsZero() == false)
                {
                    pendingDeltaPosition += data.ExternalDelta;
                    data.ExternalDelta = default;
                    hasFinished = false;
                }
            }

            data.Time = baseTime;
            data.DeltaTime = baseDeltaTime;
            data.BasePosition = basePosition;
            data.DesiredPosition = desiredPosition;
            data.WasGrounded = wasGrounded;
            data.WasSteppingUp = wasSteppingUp;
            data.WasSnappingToGround = wasSnappingToGround;
            data.RealVelocity = (nonTeleportedPosition - data.BasePosition) / data.DeltaTime;
            data.RealSpeed = data.RealVelocity.magnitude;

            var targetPosition = data.TargetPosition;

            ExecuteStageInternal<IEndMove, EndMove>(_endMove, data);

            if (data.TargetPosition.IsEqual(targetPosition) == false)
            {
                UpdateHits(data, null, EKCCHitsOverlapQuery.New);
                UpdateCollisions(data, _trackOverlapInfo);
            }

            targetPosition = data.TargetPosition;

            InvokeOnStay(data);

            if (data.TargetPosition.IsEqual(targetPosition) == false)
            {
                UpdateHits(data, null, EKCCHitsOverlapQuery.New);
                UpdateCollisions(data, _trackOverlapInfo);
            }
        }

        private void MoveInterpolated(KCCData data)
        {
            if (data.IsActive == false)
                return;

            var currentFixedData = FixedData;
            if (currentFixedData.HasTeleported == false)
            {
                var previousFixedData = GetHistoryData(currentFixedData.Tick - 1);
                if (previousFixedData != null)
                {
                    var alpha = data.Alpha;

                    data.BasePosition =
                        Vector3.Lerp(previousFixedData.BasePosition, currentFixedData.BasePosition, alpha) +
                        _predictionError;
                    data.DesiredPosition =
                        Vector3.Lerp(previousFixedData.DesiredPosition, currentFixedData.DesiredPosition, alpha) +
                        _predictionError;
                    data.TargetPosition =
                        Vector3.Lerp(previousFixedData.TargetPosition, currentFixedData.TargetPosition, alpha) +
                        _predictionError;
                    data.RealVelocity = Vector3.Lerp(previousFixedData.RealVelocity, currentFixedData.RealVelocity,
                        alpha);
                    data.RealSpeed = Mathf.Lerp(previousFixedData.RealSpeed, currentFixedData.RealSpeed, alpha);

                    if (_settings.ForcePredictedLookRotation == false)
                    {
                        data.LookPitch = Mathf.Lerp(previousFixedData.LookPitch, currentFixedData.LookPitch, alpha);
                        data.LookYaw = KCCUtility.InterpolateRange(previousFixedData.LookYaw, currentFixedData.LookYaw,
                            -180.0f, 180.0f, alpha);
                    }
                }
            }

            CacheProcessors(data);
            InvokeOnInterpolate(data);
        }

        private void SetBaseProperties(KCCData data)
        {
            data.HasTeleported = default;
            data.MaxPenetrationSteps = _settings.MaxPenetrationSteps;

            if (data.Frame == FixedData.Frame) data.JumpFrames = default;
        }

        private void ProcessMoveStep(KCCData data)
        {
            data.IsGrounded = default;
            data.IsSteppingUp = default;
            data.IsSnappingToGround = default;
            data.GroundNormal = default;
            data.GroundTangent = default;
            data.GroundPosition = default;
            data.GroundDistance = default;
            data.GroundAngle = default;

            ForceRemoveAllHits(data);

            var hasJumped = data.JumpFrames > 0;

            if (_settings.CollisionLayerMask != 0 && _collider.IsSpawned)
            {
                var baseOverlapQueryExtent = _settings.Radius;
                var baseHitsOverlapQuery = EKCCHitsOverlapQuery.Default;

                if (_settings.ForceSingleOverlapQuery)
                {
                    baseOverlapQueryExtent = _settings.Extent;
                    baseHitsOverlapQuery = EKCCHitsOverlapQuery.Reuse;
                }

                CapsuleOverlap(_extendedOverlapInfo, data, data.TargetPosition, _settings.Radius, _settings.Height,
                    baseOverlapQueryExtent, _settings.CollisionLayerMask, QueryTriggerInteraction.Collide);

                data.TargetPosition = ResolvePenetration(_extendedOverlapInfo, data, data.BasePosition,
                    data.TargetPosition, hasJumped == false, data.MaxPenetrationSteps, 3, true);

                UpdateHits(data, _extendedOverlapInfo, baseHitsOverlapQuery);
            }

            if (hasJumped) data.IsGrounded = false;

            _afterMoveStep.OverlapInfo.CopyFromOther(_extendedOverlapInfo);

            ExecuteStageInternal<IAfterMoveStep, AfterMoveStep>(_afterMoveStep, data);
        }

        private void SynchronizeTransform(KCCData data, bool synchronizePosition, bool synchronizeRotation,
            bool allowAntiJitter)
        {
            if (synchronizePosition)
            {
                var targetPosition = data.TargetPosition;

                Rigidbody.position = targetPosition;

                if (allowAntiJitter && ActiveFeatures.Has(EKCCFeature.AntiJitter) &&
                    _settings.AntiJitterDistance.IsZero() == false)
                {
                    var targetDelta = targetPosition - _lastAntiJitterPosition;
                    if (targetDelta.sqrMagnitude < _settings.TeleportThreshold * _settings.TeleportThreshold)
                    {
                        targetPosition = _lastAntiJitterPosition;

                        var distanceY = Mathf.Abs(targetDelta.y);
                        if (distanceY > 0.000001f && distanceY > _settings.AntiJitterDistance.y)
                            targetPosition.y += targetDelta.y *
                                                Mathf.Clamp01((distanceY - _settings.AntiJitterDistance.y) / distanceY);

                        var targetDeltaXZ = targetDelta.OnlyXZ();

                        var distanceXZ = Vector3.Magnitude(targetDeltaXZ);
                        if (distanceXZ > 0.000001f && distanceXZ > _settings.AntiJitterDistance.x)
                            targetPosition += targetDeltaXZ *
                                              Mathf.Clamp01((distanceXZ - _settings.AntiJitterDistance.x) / distanceXZ);
                    }

                    _lastAntiJitterPosition = targetPosition;
                }

                if (synchronizeRotation)
                    Transform.SetPositionAndRotation(targetPosition, data.TransformRotation);
                else
                    Transform.position = targetPosition;
            }
            else
            {
                if (synchronizeRotation) Transform.rotation = data.TransformRotation;
            }
        }

        private void RefreshCollider()
        {
            if (_settings.Shape == EKCCShape.None)
            {
                _collider.Destroy();
                return;
            }

            _settings.Radius = Mathf.Max(0.01f, _settings.Radius);
            _settings.Height = Mathf.Max(_settings.Radius * 2.0f, _settings.Height);
            _settings.Extent = Mathf.Max(0.0f, _settings.Extent);

            _collider.Update(this);
        }

        private void SetDefaults(bool cleanup)
        {
            Debug.SetDefaults();

            FixedData.Clear();
            RenderData.Clear();
            _historyData.Clear();
            _afterMoveStep.OverlapInfo.Reset(true);
            _extendedOverlapInfo.Reset(true);
            _trackOverlapInfo.Reset(true);
            _childColliders.Clear();
            _raycastHits.Clear();
            _hitColliders.Clear();
            _addColliders.Clear();
            _removeColliders.Clear();
            _removeCollisions.Clear();
            _activeStages.Clear();
            _cachedProcessors.Clear();
            _stageProcessors.Clear();
            _localProcessors.Clear();

            _cachedProcessorCount = default;
            _stageProcessorCount = default;

            Rigidbody.isKinematic = true;

            ActiveFeatures = default;
            LastPredictedFixedTick = default;
            _lastPredictedRenderFrame = default;
            _lastPredictedLookRotationFrame = default;
            _lastRenderTime = default;
            _lastRenderPosition = default;
            _lastAntiJitterPosition = default;
            _lastNonNetworkedCollider = default;
            _predictionError = default;

            if (cleanup)
            {
                IsSpawned = default;
                _isInFixedUpdate = default;
                HasManualUpdate = default;

                _settings.CopyFromOther(_defaultSettings);

                _collider.Destroy();

                _onSpawn = default;

                ResolveCollision = default;
                OnCollisionEnter = default;
                OnCollisionExit = default;
                GetExternalProcessors = default;
            }
        }

        private void PublishFixedData(bool render, bool history)
        {
            if (render) RenderData.CopyFromOther(FixedData);

            if (history)
            {
                var historyData = _historyData[FixedData.Tick % HISTORY_SIZE];
                if (historyData == null)
                {
                    historyData = new KCCData();
                    _historyData[FixedData.Tick % HISTORY_SIZE] = historyData;
                }

                historyData.CopyFromOther(FixedData);
            }
        }
    }
}