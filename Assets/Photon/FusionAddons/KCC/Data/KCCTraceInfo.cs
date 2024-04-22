using System;

namespace Fusion.Addons.KCC
{
    public enum EKCCTrace
    {
        None = 0,
        Stage = 1,
        Processor = 2
    }

    /// <summary>
    ///     Helper class for tracing stage and processor execution.
    /// </summary>
    public sealed class KCCTraceInfo
    {
        public bool IsVisible;
        public int Level;
        public string Name;

        public IKCCProcessor Processor;
        // PUBLIC MEMBERS

        public EKCCTrace Trace;
        public Type Type;

        public bool IsValid => Trace != EKCCTrace.None;
        public bool IsStage => Trace == EKCCTrace.Stage;
        public bool IsProcessor => Trace == EKCCTrace.Processor;

        // PUBLIC METHODS

        public void Set(EKCCTrace trace, Type type, string name, int level, IKCCProcessor processor)
        {
            if (Trace == trace && Name == name && Type == type && Level == level &&
                ReferenceEquals(Processor, processor))
                return;

            Trace = trace;
            Type = type;
            Name = name;
            Level = level;
            Processor = processor;
            IsVisible = level == default;
        }
    }
}