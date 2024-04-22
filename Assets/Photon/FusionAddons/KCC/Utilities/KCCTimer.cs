using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Debug = UnityEngine.Debug;

namespace Fusion.Addons.KCC
{
    public sealed class KCCTimer
    {
        public enum EState
        {
            Stopped = 0,
            Running = 1,
            Paused = 2
        }

        // PUBLIC MEMBERS

        public readonly int ID;
        public readonly string Name;
        private long _baseTicks;
        private long _lastTicks;
        private long _peakTicks;
        private long _recentTicks;

        // PRIVATE MEMBERS

        private long _totalTicks;

        // CONSTRUCTORS

        public KCCTimer() : this(-1, null, false)
        {
        }

        public KCCTimer(string name) : this(-1, name, false)
        {
        }

        public KCCTimer(bool start) : this(-1, null, start)
        {
        }

        public KCCTimer(string name, bool start) : this(-1, name, start)
        {
        }

        public KCCTimer(int id, string name) : this(id, name, false)
        {
        }

        public KCCTimer(int id, string name, bool start)
        {
            ID = id;
            Name = name;

            if (start) Start();
        }

        public EState State { get; private set; }

        public int Counter { get; private set; }

        public TimeSpan TotalTime
        {
            get
            {
                if (State == EState.Running) Update();
                return new TimeSpan(_totalTicks);
            }
        }

        public TimeSpan RecentTime
        {
            get
            {
                if (State == EState.Running) Update();
                return new TimeSpan(_recentTicks);
            }
        }

        public TimeSpan PeakTime
        {
            get
            {
                if (State == EState.Running) Update();
                return new TimeSpan(_peakTicks);
            }
        }

        public TimeSpan LastTime
        {
            get
            {
                if (State == EState.Running) Update();
                return new TimeSpan(_lastTicks);
            }
        }

        // PUBLIC METHODS

        public void Start()
        {
            if (State == EState.Running)
                return;

            if (State != EState.Paused)
            {
                if (_recentTicks != 0)
                {
                    _lastTicks = _recentTicks;
                    _recentTicks = 0;
                }

                ++Counter;
            }

            _baseTicks = Stopwatch.GetTimestamp();
            State = EState.Running;
        }

        public void Pause()
        {
            if (State != EState.Running)
                return;

            Update();

            State = EState.Paused;
        }

        public void Stop()
        {
            if (State == EState.Running) Update();

            State = EState.Stopped;
        }

        public void Restart()
        {
            if (_recentTicks != 0) _lastTicks = _recentTicks;

            State = EState.Running;
            Counter = 1;
            _baseTicks = Stopwatch.GetTimestamp();
            _recentTicks = 0;
            _totalTicks = 0;
            _peakTicks = 0;
        }

        public void Reset()
        {
            if (_recentTicks != 0) _lastTicks = _recentTicks;

            State = EState.Stopped;
            Counter = 0;
            _baseTicks = 0;
            _recentTicks = 0;
            _totalTicks = 0;
            _peakTicks = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetTotalSeconds()
        {
            return (float)TotalTime.TotalSeconds;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetTotalMilliseconds()
        {
            return (float)TotalTime.TotalMilliseconds;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetRecentSeconds()
        {
            return (float)RecentTime.TotalSeconds;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetRecentMilliseconds()
        {
            return (float)RecentTime.TotalMilliseconds;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetPeakSeconds()
        {
            return (float)PeakTime.TotalSeconds;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetPeakMilliseconds()
        {
            return (float)PeakTime.TotalMilliseconds;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetLastSeconds()
        {
            return (float)LastTime.TotalSeconds;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetLastMilliseconds()
        {
            return (float)LastTime.TotalMilliseconds;
        }

        public void LogSeconds(string prefix = null)
        {
            Debug.Log($"{prefix}{TotalTime.TotalSeconds:F3}s");
        }

        public void LogMilliseconds(string prefix = null)
        {
            Debug.Log($"{prefix}{TotalTime.TotalMilliseconds:F3}ms");
        }

        // PRIVATE METHODS

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Update()
        {
            var ticks = Stopwatch.GetTimestamp();

            _totalTicks += ticks - _baseTicks;
            _recentTicks += ticks - _baseTicks;

            _baseTicks = ticks;

            if (_recentTicks > _peakTicks) _peakTicks = _recentTicks;

            if (_totalTicks < 0L) _totalTicks = 0L;
        }
    }
}