using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Fusion.Addons.KCC
{
	/// <summary>
	///     Defines stats recorder type.
	///     <list type="bullet">
	///         <item>
	///             <description>None           - Default.</description>
	///         </item>
	///         <item>
	///             <description>Frame          - Recorded graph is driven by <c>Time.frameCount</c> - file ends with (Frame).</description>
	///         </item>
	///         <item>
	///             <description>
	///                 EngineTime     - Recorded graph is driven by <c>Time.unscaledTime</c> - file ends with
	///                 (EngineTime).
	///             </description>
	///         </item>
	///         <item>
	///             <description>
	///                 MonitorTime    - Recorded graph is driven by simulated monitor time - file ends with
	///                 (MonitorTime).
	///             </description>
	///         </item>
	///         <item>
	///             <description>
	///                 SimulationTick - Recorded graph is driven by <c>Runner.Tick</c> - file ends with
	///                 (SimulationTick).
	///             </description>
	///         </item>
	///     </list>
	/// </summary>
	public enum ERecorderType
    {
        None = 0,
        Frame = 1,
        EngineTime = 2,
        MonitorTime = 3,
        SimulationTick = 4
    }

	/// <summary>
	///     Base class for recorders.
	/// </summary>
	[DefaultExecutionOrder(31500)]
    public abstract class StatsRecorder : NetworkBehaviour, IBeforeAllTicks, IAfterTick
    {
        // PRIVATE MEMBERS

        [SerializeField] [DisabledInPlayMode] private ERecorderType _recorderType = ERecorderType.EngineTime;

        [SerializeField] [DisabledInPlayMode] private int _referenceRefreshRate;

        private ERecorderType _activeRecorderType;
        private int _lastForwardTicks;
        private float _lastLocalRenderTime;
        private float _lastRemoteRenderTime;
        private int _lastResimulationTicks;
        private double _lastRoundTripTime;
        private int _lastSimulationTick;
        private float _lastSimulationTime;
        private float _monitorRefreshAlpha;
        private int _monitorRefreshCounter;
        private double _monitorRefreshDeltaTime;
        private double _monitorTime;
        private double _pendingMonitorDeltaTime;

        private StatsWriter _statsWriter;
        // PUBLIC MEMBERS

        public bool IsActive => _activeRecorderType != ERecorderType.None;

        // MonoBehaviour INTERFACE

        private void LateUpdate()
        {
            if (_activeRecorderType == ERecorderType.Frame || _activeRecorderType == ERecorderType.EngineTime)
            {
                WriteValues();
            }
            else if (_activeRecorderType == ERecorderType.MonitorTime)
            {
                _monitorRefreshCounter = 0;
                _pendingMonitorDeltaTime += Time.unscaledDeltaTime;

                while (TryRefreshMonitor())
                    if (_statsWriter.HasValues)
                    {
                        _statsWriter.Override(0, $"{_monitorTime:F6}");
                        _statsWriter.Override(1, $"{_monitorRefreshCounter - 1}");
                        _statsWriter.Override(2, $"{_monitorRefreshAlpha:F3}");
                        _statsWriter.Write(false);
                    }

                AddValues();

                _monitorRefreshAlpha = (float)(_pendingMonitorDeltaTime / _monitorRefreshDeltaTime);
            }

            _lastForwardTicks = default;
            _lastResimulationTicks = default;
        }

        private void OnDestroy()
        {
            Deinitialize();
        }

        // IAfterTick INTERFACE

        void IAfterTick.AfterTick()
        {
            if (_activeRecorderType == ERecorderType.SimulationTick && Runner.IsForward) WriteValues();
        }

        // IBeforeAllTicks INTERFACE

        void IBeforeAllTicks.BeforeAllTicks(bool resimulation, int frameCount)
        {
            if (resimulation)
                _lastResimulationTicks = frameCount;
            else
                _lastForwardTicks = frameCount;
        }

        // StatsRecorder INTERFACE

        public virtual bool IsSupported(ERecorderType recorderType)
        {
            return true;
        }

        protected abstract void GetHeaders(ERecorderType recorderType, List<string> headers);
        protected abstract bool AddValues(ERecorderType recorderType, StatsWriter writer);

        // PUBLIC METHODS

        public void SetActive(bool isActive)
        {
            if (isActive)
                Initialize(_recorderType);
            else
                Deinitialize();
        }

        public void SetActive(ERecorderType recorderType)
        {
            Initialize(recorderType);
        }

        public void SetReferenceRefreshRate(int referenceRefreshRate)
        {
            if (IsActive)
                throw new InvalidOperationException("Changing reference refresh rate is not allowed during recording!");

            _referenceRefreshRate = referenceRefreshRate;
        }

        // NetworkBehaviour INTERFACE

        public override void Spawned()
        {
            _lastForwardTicks = default;
            _lastResimulationTicks = default;
            _lastSimulationTick = Runner.Tick.Raw;
            _lastSimulationTime = Runner.SimulationTime;
            _lastLocalRenderTime = Runner.LocalRenderTime;
            _lastRemoteRenderTime = Runner.RemoteRenderTime;
            _lastRoundTripTime = Runner.GetPlayerRtt(PlayerRef.None);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            Deinitialize();
        }

        public override void FixedUpdateNetwork()
        {
            _lastSimulationTick = Runner.Tick.Raw;
            _lastSimulationTime = Runner.SimulationTime;
            _lastRoundTripTime = Runner.GetPlayerRtt(PlayerRef.None);
        }

        public override void Render()
        {
            _lastSimulationTick = Runner.Tick.Raw;
            _lastSimulationTime = Runner.SimulationTime;
            _lastLocalRenderTime = Runner.LocalRenderTime;
            _lastRemoteRenderTime = Runner.RemoteRenderTime;
            _lastRoundTripTime = Runner.GetPlayerRtt(PlayerRef.None);
        }

        // PRIVATE METHODS

        private bool TryRefreshMonitor()
        {
            if (_pendingMonitorDeltaTime < _monitorRefreshDeltaTime)
                return false;

            ++_monitorRefreshCounter;
            _pendingMonitorDeltaTime -= _monitorRefreshDeltaTime;
            _monitorTime += _monitorRefreshDeltaTime;

            return true;
        }

        private void Initialize(ERecorderType recorderType)
        {
            if (_activeRecorderType == recorderType && _statsWriter != null)
                return;
            if (IsSupported(recorderType) == false)
                throw new NotSupportedException($"{recorderType}");

            Deinitialize();

            _activeRecorderType = recorderType;
            if (_activeRecorderType == ERecorderType.None)
                return;

            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            var monitorRefreshRate = _referenceRefreshRate;
            if (monitorRefreshRate <= 0)
            {
                monitorRefreshRate = Application.targetFrameRate;
                if (monitorRefreshRate <= 0) monitorRefreshRate = 60;
            }

            var fileID = $"{DateTime.Now:yyyy-MM-dd-HH-mm-ss}";
            var fileSuffix = _recorderType == ERecorderType.MonitorTime ? $"_{monitorRefreshRate}Hz" : default;
            var statsFileName = $"Rec_{fileID}_{name}_{GetType().Name}_{_activeRecorderType}{fileSuffix}.log";

            var headers = new List<string>();

            switch (_activeRecorderType)
            {
                case ERecorderType.Frame:
                {
                    headers.Add("Engine Frame");
                    headers.Add("Engine Time [s]");
                    headers.Add("Engine Delta Time [ms]");
                    headers.Add("Engine Render Speed [FPS]");
                    headers.Add("Round Trip Time [ms]");
                    headers.Add("Simulation Tick");
                    headers.Add("Resimulation Ticks");
                    headers.Add("Forward Ticks");
                    break;
                }
                case ERecorderType.EngineTime:
                {
                    headers.Add("Engine Time [s]");
                    headers.Add("Engine Frame");
                    headers.Add("Engine Delta Time [ms]");
                    headers.Add("Engine Render Speed [FPS]");
                    headers.Add("Round Trip Time [ms]");
                    headers.Add("Simulation Tick");
                    headers.Add("Resimulation Ticks");
                    headers.Add("Forward Ticks");
                    headers.Add("Local Render Time [s]");
                    headers.Add("Remote Render Time [s]");
                    break;
                }
                case ERecorderType.MonitorTime:
                {
                    headers.Add("Monitor Time [s]");
                    headers.Add("Repeat Counter");
                    headers.Add("Refresh Alpha");
                    headers.Add("Engine Frame");
                    break;
                }
                case ERecorderType.SimulationTick:
                {
                    headers.Add("Simulation Tick");
                    headers.Add("Simulation Time [s]");
                    headers.Add("Engine Frame");
                    break;
                }
                default:
                {
                    throw new NotImplementedException(_activeRecorderType.ToString());
                }
            }

            var customHeaders = new List<string>();
            GetHeaders(_activeRecorderType, customHeaders);
            headers.AddRange(customHeaders);

            _statsWriter = new StatsWriter();
            _statsWriter.Initialize(statsFileName, default, fileID, headers.ToArray());

            _monitorRefreshDeltaTime = 1.0 / monitorRefreshRate;
        }

        private void Deinitialize()
        {
            if (_statsWriter != null)
            {
                _statsWriter.Deinitialize();
                _statsWriter = null;
            }

            _activeRecorderType = ERecorderType.None;
        }

        private bool WriteValues()
        {
            if (AddValues())
            {
                _statsWriter.Write();
                return true;
            }

            return false;
        }

        private bool AddValues()
        {
            _statsWriter.Clear();

            switch (_activeRecorderType)
            {
                case ERecorderType.Frame:
                {
                    _statsWriter.Add($"{Time.frameCount}");
                    _statsWriter.Add($"{Time.unscaledTime:F6}");
                    _statsWriter.Add($"{Time.unscaledDeltaTime * 1000.0f:F3}");
                    _statsWriter.Add($"{(int)(1.0 / Time.unscaledDeltaTime + 0.5f)}");
                    _statsWriter.Add($"{_lastRoundTripTime * 1000.0f:F3}");
                    _statsWriter.Add($"{_lastSimulationTick}");
                    _statsWriter.Add($"{_lastResimulationTicks}");
                    _statsWriter.Add($"{_lastForwardTicks}");
                    break;
                }
                case ERecorderType.EngineTime:
                {
                    _statsWriter.Add($"{Time.unscaledTime:F6}");
                    _statsWriter.Add($"{Time.frameCount}");
                    _statsWriter.Add($"{Time.unscaledDeltaTime * 1000.0f:F3}");
                    _statsWriter.Add($"{(int)(1.0 / Time.unscaledDeltaTime + 0.5f)}");
                    _statsWriter.Add($"{_lastRoundTripTime * 1000.0f:F3}");
                    _statsWriter.Add($"{_lastSimulationTick}");
                    _statsWriter.Add($"{_lastResimulationTicks}");
                    _statsWriter.Add($"{_lastForwardTicks}");
                    _statsWriter.Add($"{_lastLocalRenderTime:F6}");
                    _statsWriter.Add($"{_lastRemoteRenderTime:F6}");
                    break;
                }
                case ERecorderType.MonitorTime:
                {
                    _statsWriter.Add("");
                    _statsWriter.Add("");
                    _statsWriter.Add("");
                    _statsWriter.Add($"{Time.frameCount}");
                    break;
                }
                case ERecorderType.SimulationTick:
                {
                    _statsWriter.Add($"{_lastSimulationTick}");
                    _statsWriter.Add($"{_lastSimulationTime:F6}");
                    _statsWriter.Add($"{Time.frameCount}");
                    break;
                }
                default:
                {
                    throw new NotImplementedException(_activeRecorderType.ToString());
                }
            }

            if (AddValues(_activeRecorderType, _statsWriter) == false)
            {
                _statsWriter.Clear();
                return false;
            }

            return true;
        }
    }
}