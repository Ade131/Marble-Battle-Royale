using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Fusion.Addons.KCC
{
    public static class KCCUtility
    {
        // PRIVATE MEMBERS

        private static readonly float[] _sortPriorities = new float[KCC.CACHE_SIZE];

        // PUBLIC METHODS

        public static void ClampLookRotationAngles(ref float pitch, ref float yaw)
        {
            pitch = Mathf.Clamp(pitch, -90.0f, 90.0f);

            while (yaw > 180.0f) yaw -= 360.0f;
            while (yaw < -180.0f) yaw += 360.0f;
        }

        public static void ClampLookRotationAngles(ref float pitch, ref float yaw, float minPitch, float maxPitch)
        {
            if (minPitch < -90.0f) minPitch = -90.0f;
            if (maxPitch > 90.0f) maxPitch = 90.0f;

            if (maxPitch < minPitch) maxPitch = minPitch;

            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            while (yaw > 180.0f) yaw -= 360.0f;
            while (yaw < -180.0f) yaw += 360.0f;
        }

        public static Vector2 ClampLookRotationAngles(Vector2 lookRotation)
        {
            return ClampLookRotationAngles(lookRotation, -90.0f, 90.0f);
        }

        public static Vector2 ClampLookRotationAngles(Vector2 lookRotation, float minPitch, float maxPitch)
        {
            if (minPitch < -90.0f) minPitch = -90.0f;
            if (maxPitch > 90.0f) maxPitch = 90.0f;

            if (maxPitch < minPitch) maxPitch = minPitch;

            lookRotation.x = Mathf.Clamp(lookRotation.x, minPitch, maxPitch);

            while (lookRotation.y > 180.0f) lookRotation.y -= 360.0f;
            while (lookRotation.y < -180.0f) lookRotation.y += 360.0f;

            return lookRotation;
        }

        public static void GetClampedLookRotationAngles(Quaternion lookRotation, out float pitch, out float yaw)
        {
            var eulerAngles = lookRotation.eulerAngles;

            if (eulerAngles.x > 180.0f) eulerAngles.x -= 360.0f;
            if (eulerAngles.y > 180.0f) eulerAngles.y -= 360.0f;

            pitch = Mathf.Clamp(eulerAngles.x, -90.0f, 90.0f);
            yaw = Mathf.Clamp(eulerAngles.y, -180.0f, 180.0f);
        }

        public static Vector2 GetClampedEulerLookRotation(Quaternion lookRotation)
        {
            Vector2 eulerAngles = lookRotation.eulerAngles;

            if (eulerAngles.x > 180.0f) eulerAngles.x -= 360.0f;
            if (eulerAngles.y > 180.0f) eulerAngles.y -= 360.0f;

            eulerAngles.x = Mathf.Clamp(eulerAngles.x, -90.0f, 90.0f);
            eulerAngles.y = Mathf.Clamp(eulerAngles.y, -180.0f, 180.0f);

            return eulerAngles;
        }

        public static Vector2 GetClampedEulerLookRotation(Vector3 direction)
        {
            return GetClampedEulerLookRotation(Quaternion.LookRotation(direction));
        }

        public static Vector2 GetClampedEulerLookRotation(Vector2 lookRotation, Vector2 lookRotationDelta,
            float minPitch, float maxPitch)
        {
            return ClampLookRotationAngles(lookRotation + lookRotationDelta, minPitch, maxPitch);
        }

        public static Vector2 GetClampedEulerLookRotationDelta(Vector2 lookRotation, Vector2 lookRotationDelta,
            float minPitch, float maxPitch)
        {
            var clampedlookRotationDelta = lookRotationDelta;
            lookRotationDelta.x =
                Mathf.Clamp(lookRotation.x + lookRotationDelta.x, minPitch, maxPitch) - lookRotation.x;
            return lookRotationDelta;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float InterpolateRange(float from, float to, float min, float max, float alpha)
        {
            var range = max - min;
            if (range <= 0.0f)
                throw new ArgumentException($"{nameof(max)} must be greater than {nameof(min)}!");

            if (from < min)
                from = min;
            else if (from > max) from = max;
            if (to < min)
                to = min;
            else if (to > max) to = max;

            if (from == to)
                return from;

            var halfRange = range * 0.5f;

            float interpolatedValue;

            if (from < to)
            {
                var distance = to - from;
                if (distance <= halfRange)
                {
                    interpolatedValue = Mathf.Lerp(from, to, alpha);
                }
                else
                {
                    interpolatedValue = Mathf.Lerp(from + range, to, alpha);
                    if (interpolatedValue > max) interpolatedValue -= range;
                }
            }
            else
            {
                var distance = from - to;
                if (distance <= halfRange)
                {
                    interpolatedValue = Mathf.Lerp(from, to, alpha);
                }
                else
                {
                    interpolatedValue = Mathf.Lerp(from - range, to, alpha);
                    if (interpolatedValue <= min) interpolatedValue += range;
                }
            }

            return interpolatedValue;
        }

        public static void DumpProcessors(KCC kcc, IKCCProcessor[] processors, int count)
        {
            if (count <= 0)
                return;

            kcc.Log($"Processors ({count})");

            for (var i = 0; i < count; ++i)
            {
                var processor = processors[i];

                if (processor is Component processorComponent)
                    kcc.Log(processorComponent.GetType().Name, processorComponent.name);
                else if (processor != null)
                    kcc.Log(processor.GetType().Name);
                else
                    kcc.Log("NULL");
            }
        }

        public static void SortProcessors(KCC kcc, IKCCProcessor[] processors, int count)
        {
            if (count <= 1)
                return;

            var isSorted = false;
            var priorities = _sortPriorities;
            int leftIndex;
            int rightIndex;
            float leftPriority;
            float rightPriority;
            IKCCProcessor leftProcessor;
            IKCCProcessor rightProcessor;

            for (var i = 0; i < count; ++i) priorities[i] = processors[i].GetPriority(kcc);

            while (isSorted == false)
            {
                isSorted = true;

                leftIndex = 0;
                rightIndex = 1;
                leftPriority = priorities[leftIndex];

                while (rightIndex < count)
                {
                    rightPriority = priorities[rightIndex];

                    if (leftPriority >= rightPriority)
                    {
                        leftPriority = rightPriority;
                    }
                    else
                    {
                        priorities[leftIndex] = rightPriority;
                        priorities[rightIndex] = leftPriority;

                        leftProcessor = processors[leftIndex];
                        rightProcessor = processors[rightIndex];

                        processors[leftIndex] = rightProcessor;
                        processors[rightIndex] = leftProcessor;

                        isSorted = false;
                    }

                    ++leftIndex;
                    ++rightIndex;
                }
            }
        }

        public static void SortProcessors<T>(KCC kcc, IList<T> processors) where T : class
        {
            var count = processors.Count;
            if (count <= 1)
                return;

            var isSorted = false;
            var priorities = _sortPriorities;
            int leftIndex;
            int rightIndex;
            float leftPriority;
            float rightPriority;
            T leftProcessor;
            T rightProcessor;

            for (var i = 0; i < count; ++i) priorities[i] = ((IKCCProcessor)processors[i]).GetPriority(kcc);

            while (isSorted == false)
            {
                isSorted = true;

                leftIndex = 0;
                rightIndex = 1;
                leftPriority = priorities[leftIndex];

                while (rightIndex < count)
                {
                    rightPriority = priorities[rightIndex];

                    if (leftPriority >= rightPriority)
                    {
                        leftPriority = rightPriority;
                    }
                    else
                    {
                        priorities[leftIndex] = rightPriority;
                        priorities[rightIndex] = leftPriority;

                        leftProcessor = processors[leftIndex];
                        rightProcessor = processors[rightIndex];

                        processors[leftIndex] = rightProcessor;
                        processors[rightIndex] = leftProcessor;

                        isSorted = false;
                    }

                    ++leftIndex;
                    ++rightIndex;
                }
            }
        }

        public static void SortProcessors<TStageObject>(KCC kcc, IKCCProcessor[] processors, int count)
            where TStageObject : IKCCStage<TStageObject>
        {
            if (count <= 1)
                return;

            var isSorted = false;
            var priorities = _sortPriorities;
            int leftIndex;
            int rightIndex;
            float leftPriority;
            float rightPriority;
            IKCCProcessor leftProcessor;
            IKCCProcessor rightProcessor;

            for (var i = 0; i < count; ++i) priorities[i] = ((IKCCStage<TStageObject>)processors[i]).GetPriority(kcc);

            while (isSorted == false)
            {
                isSorted = true;

                leftIndex = 0;
                rightIndex = 1;
                leftPriority = priorities[leftIndex];

                while (rightIndex < count)
                {
                    rightPriority = priorities[rightIndex];

                    if (leftPriority >= rightPriority)
                    {
                        leftPriority = rightPriority;
                    }
                    else
                    {
                        priorities[leftIndex] = rightPriority;
                        priorities[rightIndex] = leftPriority;

                        leftProcessor = processors[leftIndex];
                        rightProcessor = processors[rightIndex];

                        processors[leftIndex] = rightProcessor;
                        processors[rightIndex] = leftProcessor;

                        isSorted = false;
                    }

                    ++leftIndex;
                    ++rightIndex;
                }
            }
        }

        public static void SortProcessors<TStage, TStageObject>(KCC kcc, IKCCProcessor[] processors, int count)
            where TStage : IKCCStage<TStageObject> where TStageObject : IKCCStage<TStageObject>
        {
            if (count <= 1)
                return;

            var isSorted = false;
            var priorities = _sortPriorities;
            int leftIndex;
            int rightIndex;
            float leftPriority;
            float rightPriority;
            IKCCProcessor leftProcessor;
            IKCCProcessor rightProcessor;

            for (var i = 0; i < count; ++i) priorities[i] = ((TStage)processors[i]).GetPriority(kcc);

            while (isSorted == false)
            {
                isSorted = true;

                leftIndex = 0;
                rightIndex = 1;
                leftPriority = priorities[leftIndex];

                while (rightIndex < count)
                {
                    rightPriority = priorities[rightIndex];

                    if (leftPriority >= rightPriority)
                    {
                        leftPriority = rightPriority;
                    }
                    else
                    {
                        priorities[leftIndex] = rightPriority;
                        priorities[rightIndex] = leftPriority;

                        leftProcessor = processors[leftIndex];
                        rightProcessor = processors[rightIndex];

                        processors[leftIndex] = rightProcessor;
                        processors[rightIndex] = leftProcessor;

                        isSorted = false;
                    }

                    ++leftIndex;
                    ++rightIndex;
                }
            }
        }

        public static void SortStages<TStageObject>(KCC kcc, IList<TStageObject> stages)
            where TStageObject : IKCCStage<TStageObject>
        {
            var count = stages.Count;
            if (count <= 1)
                return;

            var isSorted = false;
            var priorities = _sortPriorities;
            int leftIndex;
            int rightIndex;
            float leftPriority;
            float rightPriority;
            TStageObject leftStage;
            TStageObject rightStage;

            for (var i = 0; i < count; ++i) priorities[i] = stages[i].GetPriority(kcc);

            while (isSorted == false)
            {
                isSorted = true;

                leftIndex = 0;
                rightIndex = 1;
                leftPriority = priorities[leftIndex];

                while (rightIndex < count)
                {
                    rightPriority = priorities[rightIndex];

                    if (leftPriority >= rightPriority)
                    {
                        leftPriority = rightPriority;
                    }
                    else
                    {
                        priorities[leftIndex] = rightPriority;
                        priorities[rightIndex] = leftPriority;

                        leftStage = stages[leftIndex];
                        rightStage = stages[rightIndex];

                        stages[leftIndex] = rightStage;
                        stages[rightIndex] = leftStage;

                        isSorted = false;
                    }

                    ++leftIndex;
                    ++rightIndex;
                }
            }
        }

        public static void SortStages<TStage, TStageObject>(KCC kcc, IList<TStage> stages)
            where TStage : IKCCStage<TStageObject> where TStageObject : IKCCStage<TStageObject>
        {
            var count = stages.Count;
            if (count <= 1)
                return;

            var isSorted = false;
            var priorities = _sortPriorities;
            int leftIndex;
            int rightIndex;
            float leftPriority;
            float rightPriority;
            TStage leftStage;
            TStage rightStage;

            for (var i = 0; i < count; ++i) priorities[i] = stages[i].GetPriority(kcc);

            while (isSorted == false)
            {
                isSorted = true;

                leftIndex = 0;
                rightIndex = 1;
                leftPriority = priorities[leftIndex];

                while (rightIndex < count)
                {
                    rightPriority = priorities[rightIndex];

                    if (leftPriority >= rightPriority)
                    {
                        leftPriority = rightPriority;
                    }
                    else
                    {
                        priorities[leftIndex] = rightPriority;
                        priorities[rightIndex] = leftPriority;

                        leftStage = stages[leftIndex];
                        rightStage = stages[rightIndex];

                        stages[leftIndex] = rightStage;
                        stages[rightIndex] = leftStage;

                        isSorted = false;
                    }

                    ++leftIndex;
                    ++rightIndex;
                }
            }
        }

        public static bool AddUniqueProcessor(KCC kcc, IKCCProcessor processor, IKCCProcessor[] processors,
            ref int processorCount)
        {
            if (processorCount >= processors.Length)
                return false;
            if (processor == null)
                return false;
            if (processor.IsActive(kcc) == false)
                return false;

            for (var i = 0; i < processorCount; ++i)
                if (ReferenceEquals(processors[i], processor))
                    return false;

            processors[processorCount] = processor;
            ++processorCount;

            return true;
        }

        public static bool ResolveProcessor(Object unityObject, out IKCCProcessor processor)
        {
            processor = unityObject as IKCCProcessor;
            if (ReferenceEquals(processor, null) == false)
                return true;

            var gameObject = unityObject as GameObject;
            if (ReferenceEquals(gameObject, null) == false)
            {
                processor = gameObject.GetComponent<IKCCProcessor>();
                if (ReferenceEquals(processor, null) == false)
                    return true;
            }

            return false;
        }

        public static bool ResolveProcessor(Object unityObject, out IKCCProcessor processor, out GameObject gameObject,
            out Component component, out ScriptableObject scriptableObject)
        {
            processor = unityObject as IKCCProcessor;
            if (ReferenceEquals(processor, null) == false)
            {
                component = processor as Component;
                if (ReferenceEquals(component, null) == false)
                {
                    gameObject = component.gameObject;
                    scriptableObject = null;
                }
                else
                {
                    gameObject = null;
                    scriptableObject = processor as ScriptableObject;
                }

                return true;
            }

            gameObject = unityObject as GameObject;
            if (ReferenceEquals(gameObject, null) == false)
            {
                processor = gameObject.GetComponent<IKCCProcessor>();
                if (ReferenceEquals(processor, null) == false)
                {
                    component = processor as Component;
                    if (ReferenceEquals(component, null) == false)
                        scriptableObject = null;
                    else
                        scriptableObject = processor as ScriptableObject;

                    return true;
                }
            }

            component = null;
            scriptableObject = null;

            return false;
        }

        [HideInCallstack]
        public static void LogInfo(SimulationBehaviour behaviour, params object[] messages)
        {
            Log(behaviour, behaviour, default, EKCCLogType.Info, messages);
        }

        [HideInCallstack]
        public static void LogWarning(SimulationBehaviour behaviour, params object[] messages)
        {
            Log(behaviour, behaviour, default, EKCCLogType.Warning, messages);
        }

        [HideInCallstack]
        public static void LogError(SimulationBehaviour behaviour, params object[] messages)
        {
            Log(behaviour, behaviour, default, EKCCLogType.Error, messages);
        }

        [HideInCallstack]
        public static void Log(SimulationBehaviour behaviour, string logGroup, EKCCLogType logType,
            params object[] messages)
        {
            Log(behaviour, behaviour, logGroup, logType, messages);
        }

        [HideInCallstack]
        public static void Log(SimulationBehaviour behaviour, Object context, string logGroup, EKCCLogType logType,
            params object[] messages)
        {
            var stringBuilder = new StringBuilder();

            var runner = behaviour.Runner;

#if UNITY_EDITOR
            if (Time.frameCount % 2 == 0)
                stringBuilder.Append($"<color=#19A7CE>[{Time.frameCount}]</color>");
            else
                stringBuilder.Append($"<color=#FC3C3C>[{Time.frameCount}]</color>");

            if (runner != null)
            {
                var isInFixedUpdate = runner.Stage != default;
                var isInForwardTick = runner.IsForward;

                if (isInFixedUpdate)
                {
                    if (isInForwardTick)
                        stringBuilder.Append($"<color=#FFFF00>[{runner.Tick.Raw}]</color>");
                    else
                        stringBuilder.Append($"<color=#FF0000>[{runner.Tick.Raw}]</color>");
                }
                else
                {
                    stringBuilder.Append($"<color=#00FF00>[{runner.Tick.Raw}]</color>");
                }
            }
            else
            {
                stringBuilder.Append("[--]");
            }
#else
			stringBuilder.Append($"[{Time.frameCount}]");

			if (runner != null)
			{
				bool isInFixedUpdate = runner.Stage != default;
				bool isInForwardTick = runner.IsForward == true;

				if (isInFixedUpdate == true)
				{
					if (isInForwardTick == true)
					{
						stringBuilder.Append($"[FF]");
					}
					else
					{
						stringBuilder.Append($"[FR]");
					}
				}
				else
				{
					stringBuilder.Append($"[RF]");
				}

				stringBuilder.Append($"[{runner.Tick.Raw}]");
			}
			else
			{
				stringBuilder.Append($"[--]");
				stringBuilder.Append($"[--]");
			}
#endif

            if (string.IsNullOrEmpty(logGroup) == false) stringBuilder.Append($"[{logGroup}]");

            stringBuilder.Append($"[{behaviour.name}]");

            for (var i = 0; i < messages.Length; ++i)
            {
                var message = messages[i];
                if (message != null)
                {
                    stringBuilder.Append(" ");
                    stringBuilder.Append(message);
                }
            }

            switch (logType)
            {
                case EKCCLogType.Info:
                    Debug.Log(stringBuilder.ToString(), context);
                    break;
                case EKCCLogType.Warning:
                    Debug.LogWarning(stringBuilder.ToString(), context);
                    break;
                case EKCCLogType.Error:
                    Debug.LogError(stringBuilder.ToString(), context);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(logType), logType, null);
            }
        }

        [HideInCallstack]
        [Conditional(KCC.TRACING_SCRIPT_DEFINE)]
        public static void Trace(SimulationBehaviour behaviour, string logGroup, EKCCLogType logType,
            params object[] messages)
        {
            Log(behaviour, logGroup, logType, messages);
        }

        [HideInCallstack]
        [Conditional(KCC.TRACING_SCRIPT_DEFINE)]
        public static void Trace<T>(SimulationBehaviour behaviour, params object[] messages)
        {
            Log(behaviour, typeof(T).Name, EKCCLogType.Info, messages);
        }
    }
}