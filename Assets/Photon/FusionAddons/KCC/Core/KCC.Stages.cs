using System;
using System.Collections.Generic;

namespace Fusion.Addons.KCC
{
    using PostProcess = Action<KCC, KCCData>;

    // This file contains implementation related to stage execution.
    public partial class KCC
    {
        // PUBLIC METHODS

        /// <summary>
        ///     Executes stage of type <c>TStage</c> on all processors.
        ///     The stage object passed to processors is null.
        /// </summary>
        public void ExecuteStage<TStage>() where TStage : IKCCStage<TStage>
        {
            ExecuteStageInternal<TStage, TStage>(default);
        }

        /// <summary>
        ///     Executes stage of type <c>IKCCStage&lt;TStageObject&gt;</c> with the stage object of type <c>TStageObject</c> on
        ///     all processors.
        /// </summary>
        public void ExecuteStage<TStageObject>(TStageObject stage) where TStageObject : IKCCStage<TStageObject>
        {
            ExecuteStageInternal<IKCCStage<TStageObject>, TStageObject>(stage);
        }

        /// <summary>
        ///     Executes stage of type <c>TStage</c> with the stage object of type <c>TStageObject</c> on all processors.
        /// </summary>
        public void ExecuteStage<TStage, TStageObject>(TStageObject stage) where TStage : IKCCStage<TStageObject>
            where TStageObject : IKCCStage<TStageObject>
        {
            ExecuteStageInternal<TStage, TStageObject>(stage);
        }

        /// <summary>
        ///     Check if the processor is pending execution in current stage.
        ///     The query does NOT include the currently executed processor.
        /// </summary>
        public bool HasPendingProcessor(IKCCProcessor processor)
        {
            if (_activeStages.Count == 0)
                throw new InvalidOperationException(
                    "Querying processor execution is allowed only during stage execution!");

            return _activeStages[_activeStages.Count - 1].HasPendingProcessor(processor);
        }

        /// <summary>
        ///     Check if the processor is pending execution in any active stage of type <c>TStage</c>.
        ///     The query does NOT include the currently executed processor.
        /// </summary>
        public bool HasPendingProcessor<TStage>(IKCCProcessor processor) where TStage : IKCCStage
        {
            if (_activeStages.Count == 0)
                throw new InvalidOperationException(
                    "Querying processor pending execution is allowed only during stage execution!");

            for (var i = _activeStages.Count - 1; i >= 0; --i)
            {
                var activeStageInfo = _activeStages[i];
                if (activeStageInfo.IsAssignable<TStage>())
                    if (activeStageInfo.HasPendingProcessor(processor))
                        return true;
            }

            return false;
        }

        /// <summary>
        ///     Check if any processor of type <c>TProcessor</c> is pending execution in current stage.
        ///     The query does NOT include the currently executed processor.
        /// </summary>
        public bool HasPendingProcessor<TProcessor>() where TProcessor : class
        {
            if (_activeStages.Count == 0)
                throw new InvalidOperationException(
                    "Querying processor pending execution is allowed only during stage execution!");

            return _activeStages[_activeStages.Count - 1].HasPendingProcessor<TProcessor>();
        }

        /// <summary>
        ///     Check if any processor of type <c>TProcessor</c> is pending execution in any active stage of type <c>TStage</c>.
        ///     The query does NOT include the currently executed processor.
        /// </summary>
        public bool HasPendingProcessor<TProcessor, TStage>() where TProcessor : class where TStage : IKCCStage
        {
            if (_activeStages.Count == 0)
                throw new InvalidOperationException(
                    "Querying processor pending execution is allowed only during stage execution!");

            for (var i = _activeStages.Count - 1; i >= 0; --i)
            {
                var activeStageInfo = _activeStages[i];
                if (activeStageInfo.IsAssignable<TStage>())
                    if (activeStageInfo.HasPendingProcessor<TProcessor>())
                        return true;
            }

            return false;
        }

        /// <summary>
        ///     Get first processor of type <c>TProcessor</c> pending execution in current stage.
        ///     The query does NOT include the currently executed processor.
        /// </summary>
        public TProcessor GetPendingProcessor<TProcessor>() where TProcessor : class
        {
            if (_activeStages.Count == 0)
                throw new InvalidOperationException(
                    "Querying processor pending execution is allowed only during stage execution!");

            if (_activeStages[_activeStages.Count - 1].GetPendingProcessor(out TProcessor processor))
                return processor;

            return default;
        }

        /// <summary>
        ///     Get first processor of type <c>TProcessor</c> pending execution in any active stage of type <c>TStage</c>.
        ///     The query does NOT include the currently executed processor.
        /// </summary>
        public TProcessor GetPendingProcessor<TProcessor, TStage>() where TProcessor : class where TStage : IKCCStage
        {
            if (_activeStages.Count == 0)
                throw new InvalidOperationException(
                    "Querying processor pending execution is allowed only during stage execution!");

            for (var i = _activeStages.Count - 1; i >= 0; --i)
            {
                var activeStageInfo = _activeStages[i];
                if (activeStageInfo.IsAssignable<TStage>())
                    if (activeStageInfo.GetPendingProcessor(out TProcessor processor))
                        return processor;
            }

            return default;
        }

        /// <summary>
        ///     Get all processors of type <c>TProcessor</c> pending execution in current stage.
        ///     The query does NOT include the currently executed processor.
        /// </summary>
        public void GetPendingProcessors<TProcessor>(List<TProcessor> processors) where TProcessor : class
        {
            if (_activeStages.Count == 0)
                throw new InvalidOperationException(
                    "Querying processors pending execution is allowed only during stage execution!");

            _activeStages[_activeStages.Count - 1].GetPendingProcessors(processors, true);
        }

        /// <summary>
        ///     Get all processors of type <c>TProcessor</c> pending execution in all active stages of type <c>TStage</c>.
        ///     The query does NOT include the currently executed processor.
        /// </summary>
        public void GetPendingProcessors<TProcessor, TStage>(List<TProcessor> processors)
            where TProcessor : class where TStage : IKCCStage
        {
            if (_activeStages.Count == 0)
                throw new InvalidOperationException(
                    "Querying processors pending execution is allowed only during stage execution!");

            processors.Clear();

            for (var i = _activeStages.Count - 1; i >= 0; --i)
            {
                var activeStageInfo = _activeStages[i];
                if (activeStageInfo.IsAssignable<TStage>()) activeStageInfo.GetPendingProcessors(processors, false);
            }
        }

        /// <summary>
        ///     Check if the processor has executed in current stage.
        ///     The query does NOT include the currently executed processor.
        /// </summary>
        public bool HasExecutedProcessor(IKCCProcessor processor)
        {
            if (_activeStages.Count == 0)
                throw new InvalidOperationException(
                    "Querying executed processor is allowed only during stage execution!");

            return _activeStages[_activeStages.Count - 1].HasExecutedProcessor(processor);
        }

        /// <summary>
        ///     Check if the processor has executed in any active stage of type <c>TStage</c>.
        ///     The query does NOT include the currently executed processor.
        /// </summary>
        public bool HasExecutedProcessor<TStage>(IKCCProcessor processor) where TStage : IKCCStage
        {
            if (_activeStages.Count == 0)
                throw new InvalidOperationException(
                    "Querying executed processor is allowed only during stage execution!");

            for (var i = _activeStages.Count - 1; i >= 0; --i)
            {
                var activeStageInfo = _activeStages[i];
                if (activeStageInfo.IsAssignable<TStage>())
                    if (activeStageInfo.HasExecutedProcessor(processor))
                        return true;
            }

            return false;
        }

        /// <summary>
        ///     Check if any processor of type <c>TProcessor</c> has executed in current stage.
        ///     The query does NOT include the currently executed processor.
        /// </summary>
        public bool HasExecutedProcessor<TProcessor>() where TProcessor : class
        {
            if (_activeStages.Count == 0)
                throw new InvalidOperationException(
                    "Querying executed processor is allowed only during stage execution!");

            return _activeStages[_activeStages.Count - 1].HasExecutedProcessor<TProcessor>();
        }

        /// <summary>
        ///     Check if any processor of type <c>TProcessor</c> has executed in any active stage of type <c>TStage</c>.
        ///     The query does NOT include the currently executed processor.
        /// </summary>
        public bool HasExecutedProcessor<TProcessor, TStage>() where TProcessor : class where TStage : IKCCStage
        {
            if (_activeStages.Count == 0)
                throw new InvalidOperationException(
                    "Querying executed processor is allowed only during stage execution!");

            for (var i = _activeStages.Count - 1; i >= 0; --i)
            {
                var activeStageInfo = _activeStages[i];
                if (activeStageInfo.IsAssignable<TStage>())
                    if (activeStageInfo.HasExecutedProcessor<TProcessor>())
                        return true;
            }

            return false;
        }

        /// <summary>
        ///     Get last processor of type <c>TProcessor</c> executed in current stage.
        ///     The query does NOT include the currently executed processor.
        /// </summary>
        public TProcessor GetExecutedProcessor<TProcessor>() where TProcessor : class
        {
            if (_activeStages.Count == 0)
                throw new InvalidOperationException(
                    "Querying executed processor is allowed only during stage execution!");

            if (_activeStages[_activeStages.Count - 1].GetExecutedProcessor(out TProcessor processor))
                return processor;

            return default;
        }

        /// <summary>
        ///     Get last processor of type <c>TProcessor</c> executed in any active stage of type <c>TStage</c>.
        ///     The query does NOT include the currently executed processor.
        /// </summary>
        public TProcessor GetExecutedProcessor<TProcessor, TStage>() where TProcessor : class where TStage : IKCCStage
        {
            if (_activeStages.Count == 0)
                throw new InvalidOperationException(
                    "Querying executed processor is allowed only during stage execution!");

            for (var i = _activeStages.Count - 1; i >= 0; --i)
            {
                var activeStageInfo = _activeStages[i];
                if (activeStageInfo.IsAssignable<TStage>())
                    if (activeStageInfo.GetExecutedProcessor(out TProcessor processor))
                        return processor;
            }

            return default;
        }

        /// <summary>
        ///     Get all processors of type <c>TProcessor</c> executed in current stage.
        ///     The query does NOT include the currently executed processor.
        ///     Processors in the list are sorted by priority.
        /// </summary>
        public void GetExecutedProcessors<TProcessor>(List<TProcessor> processors) where TProcessor : class
        {
            if (_activeStages.Count == 0)
                throw new InvalidOperationException(
                    "Querying executed processors is allowed only during stage execution!");

            _activeStages[_activeStages.Count - 1].GetExecutedProcessors(processors, true);
        }

        /// <summary>
        ///     Get all processors of type <c>T</c> executed in all active stages of type <c>TStage</c>.
        ///     The query does NOT include the currently executed processor.
        ///     Processors in the list are sorted by priority.
        /// </summary>
        public void GetExecutedProcessors<TProcessor, TStage>(List<TProcessor> processors)
            where TProcessor : class where TStage : IKCCStage
        {
            if (_activeStages.Count == 0)
                throw new InvalidOperationException(
                    "Querying executed processors is allowed only during stage execution!");

            processors.Clear();

            for (int i = 0, count = _activeStages.Count; i < count; ++i)
            {
                var activeStageInfo = _activeStages[i];
                if (activeStageInfo.IsAssignable<TStage>()) activeStageInfo.GetExecutedProcessors(processors, false);
            }
        }

        /// <summary>
        ///     Suppress execution of a pending processor in current stage.
        /// </summary>
        /// <param name="processor">Processor instance to be suppressed.</param>
        /// <param name="suppressInFutureStages">If true, processors will be suppressed also in all future stages.</param>
        public bool SuppressProcessor(IKCCProcessor processor, bool suppressInFutureStages = false)
        {
            if (_activeStages.Count == 0)
                throw new InvalidOperationException(
                    "Suppressing processor execution is allowed only during stage execution!");

            var result = _activeStages[_activeStages.Count - 1].SuppressProcessor(processor);

            if (suppressInFutureStages)
                for (int i = 0, count = _stageProcessorCount; i < count; ++i)
                    if (ReferenceEquals(_stageProcessors[i], processor))
                    {
                        _stageProcessors[i] = null;
                        result = true;
                        break;
                    }

            return result;
        }

        /// <summary>
        ///     Suppress execution of a pending processor in all active stages of type <c>TStage</c>.
        /// </summary>
        public bool SuppressProcessor<TStage>(IKCCProcessor processor) where TStage : IKCCStage
        {
            if (_activeStages.Count == 0)
                throw new InvalidOperationException(
                    "Suppressing processor execution is allowed only during stage execution!");

            var result = false;

            for (int i = 0, count = _activeStages.Count; i < count; ++i)
            {
                var activeStageInfo = _activeStages[i];
                if (activeStageInfo.IsAssignable<TStage>()) result |= activeStageInfo.SuppressProcessor(processor);
            }

            return result;
        }

        /// <summary>
        ///     Suppress execution of pending processors of type <c>TProcessor</c> in current stage.
        /// </summary>
        /// <param name="suppressInFutureStages">If true, processors will be suppressed also in all future stages.</param>
        public bool SuppressProcessors<TProcessor>(bool suppressInFutureStages = false) where TProcessor : class
        {
            if (_activeStages.Count == 0)
                throw new InvalidOperationException(
                    "Suppressing processor execution is allowed only during stage execution!");

            var result = _activeStages[_activeStages.Count - 1].SuppressProcessors<TProcessor>();

            if (suppressInFutureStages)
                for (int i = 0, count = _stageProcessorCount; i < count; ++i)
                    if (_stageProcessors[i] is TProcessor)
                    {
                        _stageProcessors[i] = null;
                        result = true;
                    }

            return result;
        }

        /// <summary>
        ///     Suppress execution of pending processors of type <c>TProcessor</c> in all active stages of type <c>TStage</c>.
        /// </summary>
        public bool SuppressProcessors<TProcessor, TStage>() where TProcessor : class where TStage : IKCCStage
        {
            if (_activeStages.Count == 0)
                throw new InvalidOperationException(
                    "Suppressing processor execution is allowed only during stage execution!");

            var result = false;

            for (int i = 0, count = _activeStages.Count; i < count; ++i)
            {
                var activeStageInfo = _activeStages[i];
                if (activeStageInfo.IsAssignable<TStage>()) result |= activeStageInfo.SuppressProcessors<TProcessor>();
            }

            return result;
        }

        /// <summary>
        ///     Suppress execution of all pending processors except one in current stage.
        /// </summary>
        /// <param name="processor">Processor instance not to be suppressed.</param>
        /// <param name="suppressInFutureStages">If true, processors will be suppressed also in all future stages.</param>
        public void SuppressProcessorsExcept(IKCCProcessor processor, bool suppressInFutureStages = false)
        {
            if (_activeStages.Count == 0)
                throw new InvalidOperationException(
                    "Suppressing processor execution is allowed only during stage execution!");

            _activeStages[_activeStages.Count - 1].SuppressProcessorsExcept(processor);

            if (suppressInFutureStages)
                for (int i = 0, count = _stageProcessorCount; i < count; ++i)
                    if (ReferenceEquals(_stageProcessors[i], processor) == false)
                        _stageProcessors[i] = null;
        }

        /// <summary>
        ///     Suppress execution of all pending processors except processors of type <c>TProcessor</c> in current stage.
        /// </summary>
        /// <param name="suppressInFutureStages">If true, processors will be suppressed also in all future stages.</param>
        public void SuppressProcessorsExcept<TProcessor>(bool suppressInFutureStages = false) where TProcessor : class
        {
            if (_activeStages.Count == 0)
                throw new InvalidOperationException(
                    "Suppressing processor execution is allowed only during stage execution!");

            _activeStages[_activeStages.Count - 1].SuppressProcessorsExcept<TProcessor>();

            if (suppressInFutureStages)
                for (int i = 0, count = _stageProcessorCount; i < count; ++i)
                    if (_stageProcessors[i] is not TProcessor)
                        _stageProcessors[i] = null;
        }

        /// <summary>
        ///     Suppress execution of all pending processors except processors of type <c>TProcessor</c> in all active stages of
        ///     type <c>TStage</c>.
        /// </summary>
        public void SuppressProcessorsExcept<TProcessor, TStage>() where TProcessor : class where TStage : IKCCStage
        {
            if (_activeStages.Count == 0)
                throw new InvalidOperationException(
                    "Suppressing processor execution is allowed only during stage execution!");

            for (int i = 0, count = _activeStages.Count; i < count; ++i)
            {
                var activeStageInfo = _activeStages[i];
                if (activeStageInfo.IsAssignable<TStage>()) activeStageInfo.SuppressProcessorsExcept<TProcessor>();
            }
        }

        /// <summary>
        ///     Enqueues post-processing callback which is executed at the end of the current stage.
        ///     This can be used as a lightweight alternative to a processor with higher priority.
        /// </summary>
        public void EnqueuePostProcess(Action<KCC, KCCData> callback)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));
            if (_activeStages.Count == 0)
                throw new InvalidOperationException("Post-processing is allowed only during stage execution!");

            var activeStageInfo = _activeStages[_activeStages.Count - 1];
            activeStageInfo.PostProcesses.Add(callback);
        }

        /// <summary>
        ///     Enqueues post-processing callback which is executed at the end of the latest active stage of type <c>TStage</c>.
        /// </summary>
        public bool EnqueuePostProcess<TStage>(Action<KCC, KCCData> callback) where TStage : IKCCStage
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));
            if (_activeStages.Count == 0)
                throw new InvalidOperationException("Post-processing is allowed only during stage execution!");

            for (var i = _activeStages.Count - 1; i >= 0; --i)
            {
                var activeStageInfo = _activeStages[i];
                if (activeStageInfo.IsAssignable<TStage>())
                {
                    activeStageInfo.PostProcesses.Add(callback);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     Returns true if a specific feature is currently active.
        /// </summary>
        public bool HasActiveFeature(EKCCFeature feature)
        {
            return ActiveFeatures.Has(feature);
        }

        /// <summary>
        ///     Enforcing execution of a specific KCC feature. The call is valid only with BeginMove being the first stage in
        ///     hierarchy.
        ///     For persistent change please set KCC.Settings.Features directly.
        /// </summary>
        public void EnforceFeature(EKCCFeature feature)
        {
            if (_activeStages.Count == 0 || _activeStages[0].StageObjectType != KCCTypes.BeginMove)
                throw new InvalidOperationException(
                    $"Enforcing features is allowed only during {nameof(BeginMove)} stage execution!");

            ActiveFeatures = (EKCCFeatures)((int)ActiveFeatures | (1 << (int)feature));
        }

        /// <summary>
        ///     Suppressing execution of a specific KCC feature. The call is valid only with BeginMove being the first stage in
        ///     hierarchy.
        ///     For persistent change please set KCC.Settings.Features directly.
        /// </summary>
        public void SuppressFeature(EKCCFeature feature)
        {
            if (_activeStages.Count == 0 || _activeStages[0].StageObjectType != KCCTypes.BeginMove)
                throw new InvalidOperationException(
                    $"Suppressing features is allowed only during {nameof(BeginMove)} stage execution!");

            ActiveFeatures = (EKCCFeatures)((int)ActiveFeatures & ~(1 << (int)feature));
        }

        // PRIVATE METHODS

        private void ExecuteStageInternal<TStage, TStageObject>(TStageObject stage)
            where TStage : IKCCStage<TStageObject> where TStageObject : IKCCStage<TStageObject>
        {
            if (IsSpawned == false)
                return;

            var type = typeof(TStageObject);
            if (type == KCCTypes.IBeginMove || type == KCCTypes.BeginMove)
                throw new InvalidOperationException(
                    $"Explicit execution of {nameof(IBeginMove)} stage is not allowed!");
            if (type == KCCTypes.IPrepareData || type == KCCTypes.PrepareData)
                throw new InvalidOperationException(
                    $"Explicit execution of {nameof(IPrepareData)} stage is not allowed!");
            if (type == KCCTypes.IAfterMoveStep || type == KCCTypes.AfterMoveStep)
                throw new InvalidOperationException(
                    $"Explicit execution of {nameof(IAfterMoveStep)} stage is not allowed!");
            if (type == KCCTypes.IEndMove || type == KCCTypes.EndMove)
                throw new InvalidOperationException($"Explicit execution of {nameof(IEndMove)} stage is not allowed!");

            var data = Data;

            if (_activeStages.Count == 0) CacheProcessors(data);

            ExecuteStageInternal<TStage, TStageObject>(stage, data);
        }

        private void ExecuteStageInternal<TStage, TStageObject>(TStageObject stage, KCCData data)
            where TStage : IKCCStage<TStageObject> where TStageObject : IKCCStage<TStageObject>
        {
            var stageInfo = KCCStageInfo.Allocate();
            stageInfo.Type = typeof(TStage);
            stageInfo.Level = _activeStages.Count > 0 ? _activeStages[_activeStages.Count - 1].Level + 1 : 0;
            stageInfo.StageObject = stage;
            stageInfo.StageObjectType = typeof(TStageObject);

            if (stageInfo.Level >= KCCSettings.MaxNestedStages)
            {
                LogError($"Nested stages overflow! Maximum allowed: {KCCSettings.MaxNestedStages}");
                KCCStageInfo.Release(stageInfo);
                return;
            }

            var stageProcessors = stageInfo.Processors;
            var stagePostProcesses = stageInfo.PostProcesses;

            for (var i = 0; i < _stageProcessorCount; ++i)
            {
                var stageProcessor = _stageProcessors[i];
                if (stageProcessor is TStage)
                {
                    stageProcessors[stageInfo.ProcessorCount] = stageProcessor;
                    ++stageInfo.ProcessorCount;
                }
            }

            KCCUtility.SortProcessors<TStage, TStageObject>(this, stageProcessors, stageInfo.ProcessorCount);

            _activeStages.Add(stageInfo);

            var traceProcessors = Debug.TraceStage(this, stageInfo.StageObjectType, stageInfo.Level);

            if (stage is IBeforeStage beforeStage)
                try
                {
                    beforeStage.BeforeStage(this, data);
                }
                catch (Exception exception)
                {
                    UnityEngine.Debug.LogException(exception);
                }

            for (stageInfo.ProcessorIndex = 0;
                 stageInfo.ProcessorIndex < stageInfo.ProcessorCount;
                 ++stageInfo.ProcessorIndex)
            {
                var processor = stageProcessors[stageInfo.ProcessorIndex];
                if (ReferenceEquals(processor, null))
                    continue;

                if (traceProcessors) Debug.TraceProcessor(processor, stageInfo.Level);

                try
                {
                    ((TStage)processor).Execute(stage, this, data);
                }
                catch (Exception exception)
                {
                    UnityEngine.Debug.LogException(exception);
                }
            }

            if (stage != null)
                try
                {
                    stage.Execute(stage, this, data);
                }
                catch (Exception exception)
                {
                    UnityEngine.Debug.LogException(exception);
                }

            for (var i = 0; i < stagePostProcesses.Count; ++i)
                try
                {
                    stagePostProcesses[i](this, data);
                }
                catch (Exception exception)
                {
                    UnityEngine.Debug.LogException(exception);
                }

            stagePostProcesses.Clear();

            if (stage is IAfterStage afterStage)
                try
                {
                    afterStage.AfterStage(this, data);
                }
                catch (Exception exception)
                {
                    UnityEngine.Debug.LogException(exception);
                }

            for (var i = 0; i < stagePostProcesses.Count; ++i)
                try
                {
                    stagePostProcesses[i](this, data);
                }
                catch (Exception exception)
                {
                    UnityEngine.Debug.LogException(exception);
                }

            var lastStageIndex = _activeStages.Count - 1;
            if (_activeStages[lastStageIndex] == stageInfo)
            {
                _activeStages.RemoveAt(lastStageIndex);
            }
            else
            {
                LogError(
                    $"Active stage of type {stageInfo.Type.FullName} is at unexpected position ({lastStageIndex}) in execution stack!");
                _activeStages.Remove(stageInfo);
            }

            KCCStageInfo.Release(stageInfo);
        }

        private void InvokeOnStay(KCCData data)
        {
            var stageInfo = KCCStageInfo.Allocate();
            stageInfo.Type = typeof(IKCCProcessor);
            stageInfo.StageObjectType = typeof(IKCCProcessor);

            _activeStages.Add(stageInfo);

            var traceProcessors = Debug.TraceStage(this, default, nameof(IKCCProcessor.OnStay), stageInfo.Level);

            for (var i = 0; i < _cachedProcessorCount; ++i)
            {
                var processor = _cachedProcessors[i];
                if (ReferenceEquals(processor, null))
                    continue;

                if (traceProcessors) Debug.TraceProcessor(processor, stageInfo.Level);

                try
                {
                    processor.OnStay(this, data);
                }
                catch (Exception exception)
                {
                    UnityEngine.Debug.LogException(exception);
                }
            }

            var stagePostProcesses = stageInfo.PostProcesses;
            for (var i = 0; i < stagePostProcesses.Count; ++i)
                try
                {
                    stagePostProcesses[i](this, data);
                }
                catch (Exception exception)
                {
                    UnityEngine.Debug.LogException(exception);
                }

            if (_activeStages[_activeStages.Count - 1] == stageInfo)
            {
                _activeStages.RemoveAt(_activeStages.Count - 1);
            }
            else
            {
                LogError(
                    $"Active stage of type {stageInfo.Type.FullName} is at unexpected position in execution stack!");
                _activeStages.Remove(stageInfo);
            }

            KCCStageInfo.Release(stageInfo);
        }

        private void InvokeOnInterpolate(KCCData data)
        {
            var stageInfo = KCCStageInfo.Allocate();
            stageInfo.Type = typeof(IKCCProcessor);
            stageInfo.StageObjectType = typeof(IKCCProcessor);

            _activeStages.Add(stageInfo);

            var traceProcessors = Debug.TraceStage(this, default, nameof(IKCCProcessor.OnInterpolate), stageInfo.Level);

            for (var i = 0; i < _cachedProcessorCount; ++i)
            {
                var processor = _cachedProcessors[i];
                if (ReferenceEquals(processor, null))
                    continue;

                if (traceProcessors) Debug.TraceProcessor(processor, stageInfo.Level);

                try
                {
                    processor.OnInterpolate(this, data);
                }
                catch (Exception exception)
                {
                    UnityEngine.Debug.LogException(exception);
                }
            }

            var stagePostProcesses = stageInfo.PostProcesses;
            for (var i = 0; i < stagePostProcesses.Count; ++i)
                try
                {
                    stagePostProcesses[i](this, data);
                }
                catch (Exception exception)
                {
                    UnityEngine.Debug.LogException(exception);
                }

            if (_activeStages[_activeStages.Count - 1] == stageInfo)
            {
                _activeStages.RemoveAt(_activeStages.Count - 1);
            }
            else
            {
                LogError(
                    $"Active stage of type {stageInfo.Type.FullName} is at unexpected position in execution stack!");
                _activeStages.Remove(stageInfo);
            }

            KCCStageInfo.Release(stageInfo);
        }
    }
}