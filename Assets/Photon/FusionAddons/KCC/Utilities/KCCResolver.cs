using System;
using UnityEngine;

namespace Fusion.Addons.KCC
{
	/// <summary>
	///     Utility for calculation single depenetration vector based on multiple unrelated vectors.
	///     Starting point is sum of min and max components of these vectors, putting the target vector close to correct
	///     position and minimising number of iterations.
	///     This method uses gradient descent algorithm with sum of absolute errors as function to find the local minimum.
	///     This approach has good results on depenetration vectors with various normals, but fails on corrections with same
	///     direction but different correction distance.
	///     For best results use at least 2 compute penetration passes and apply target correction fully in last pass only.
	/// </summary>
	public sealed class KCCResolver
    {
        private readonly Correction[] _corrections;
        private Vector3 _maxCorrection;
        private Vector3 _minCorrection;

        // PRIVATE MEMBERS

        // CONSTRUCTORS

        public KCCResolver(int maxSize)
        {
            _corrections = new Correction[maxSize];
            for (var i = 0; i < maxSize; ++i) _corrections[i] = new Correction();
        }
        // PUBLIC MEMBERS

        /// <summary>Count of input corrections.</summary>
        public int Size { get; private set; }

        /// <summary>Number of iterations in last calculation.</summary>
        public int Iterations { get; private set; }

        /// <summary>Correction calculated from input corrections.</summary>
        public Vector3 TargetCorrection { get; private set; }

        // PUBLIC METHODS

        /// <summary>
        ///     Resets resolver. Call this before adding corrections.
        /// </summary>
        public void Reset()
        {
            Size = default;
            Iterations = default;
            _minCorrection = default;
            _maxCorrection = default;
            TargetCorrection = default;
        }

        /// <summary>
        ///     Adds single correction vector.
        /// </summary>
        public void AddCorrection(Vector3 direction, float distance)
        {
            var correction = _corrections[Size];

            correction.Amount = direction * distance;
            correction.Direction = direction;
            correction.Distance = distance;

            _minCorrection = Vector3.Min(_minCorrection, correction.Amount);
            _maxCorrection = Vector3.Max(_maxCorrection, correction.Amount);

            ++Size;
        }

        /// <summary>
        ///     Returns correction at specific index.
        /// </summary>
        public Vector3 GetCorrection(int index)
        {
            return _corrections[index].Amount;
        }

        /// <summary>
        ///     Returns correction amount and direction at specific index.
        /// </summary>
        public Vector3 GetCorrection(int index, out Vector3 direction)
        {
            var correction = _corrections[index];
            direction = correction.Direction;
            return correction.Amount;
        }

        /// <summary>
        ///     Returns correction amount, direction and distance at specific index.
        /// </summary>
        public Vector3 GetCorrection(int index, out Vector3 direction, out float distance)
        {
            var correction = _corrections[index];
            direction = correction.Direction;
            distance = correction.Distance;
            return correction.Amount;
        }

        /// <summary>
        ///     Calculates target correction vector based on added corrections.
        /// </summary>
        public Vector3 CalculateMinMax()
        {
            Iterations = default;
            TargetCorrection = _minCorrection + _maxCorrection;

            return TargetCorrection;
        }

        /// <summary>
        ///     Calculates target correction vector based on added corrections.
        /// </summary>
        public Vector3 CalculateSum()
        {
            Iterations = default;
            TargetCorrection = default;

            for (int i = 0, count = Size; i < count; ++i) TargetCorrection += _corrections[i].Amount;

            return TargetCorrection;
        }

        /// <summary>
        ///     Calculates target correction vector based on added corrections.
        /// </summary>
        public Vector3 CalculateBinary()
        {
            if (Size != 2)
                throw new InvalidOperationException("Size must be 2!");

            Iterations = default;
            TargetCorrection = _minCorrection + _maxCorrection;

            var correction0 = _corrections[0];
            var correction1 = _corrections[1];

            var correctionDot = Vector3.Dot(correction0.Direction, correction1.Direction);
            if (correctionDot > 0.999f || correctionDot < -0.999f)
                return TargetCorrection;

            var deltaCorrectionDirection = Vector3
                .Cross(Vector3.Cross(correction0.Direction, correction1.Direction), correction0.Direction).normalized;
            var deltaCorrectionDistance = (correction1.Distance - correction0.Distance * correctionDot) /
                                          Mathf.Sqrt(1.0f - correctionDot * correctionDot);

            TargetCorrection = correction0.Amount + deltaCorrectionDirection * deltaCorrectionDistance;

            return TargetCorrection;
        }

        /// <summary>
        ///     Calculates target correction vector based on added corrections.
        /// </summary>
        public Vector3 CalculateGradientDescent(int maxIterations, float maxError)
        {
            Iterations = default;
            TargetCorrection = _minCorrection + _maxCorrection;

            if (Size <= 1)
                return TargetCorrection;

            Vector3 error;
            float errorDot;
            float errorCorrection;
            float errorCorrectionSize;
            var desiredCorrection = TargetCorrection;
            var corrections = _corrections;
            Correction correction;

            while (Iterations < maxIterations)
            {
                error = default;
                errorCorrection = default;
                errorCorrectionSize = default;

                for (int i = 0, count = Size; i < count; ++i)
                {
                    correction = corrections[i];

                    // Calculate error of desired correction relative to single correction.
                    correction.Error = correction.Direction.x * desiredCorrection.x +
                                       correction.Direction.y * desiredCorrection.y +
                                       correction.Direction.z * desiredCorrection.z -
                                       correction.Distance;

                    // Accumulate error of all corrections.
                    error += correction.Direction * correction.Error;
                }

                // The accumulated error is almost zero which means we hit a local minimum.
                if (error.IsAlmostZero(maxError))
                    break;

                // Normalize the error => now we know what is the wrong direction => desired correction needs to move in opposite direction to lower the error.
                error.Normalize();

                for (int i = 0, count = Size; i < count; ++i)
                {
                    correction = corrections[i];

                    // Compare single correction direction with the accumulated error direction.
                    errorDot = correction.Direction.x * error.x + correction.Direction.y * error.y +
                               correction.Direction.z * error.z;

                    // Accumulate error correction based on relative correction errors.
                    // Corrections with direction aligned to accumulated error have more impact.
                    errorCorrection += correction.Error * errorDot;

                    if (errorDot >= 0.0f)
                        errorCorrectionSize += errorDot;
                    else
                        errorCorrectionSize -= errorDot;
                }

                if (errorCorrectionSize < 0.000001f)
                    break;

                // The error correction is almost zero and desired correction won't change.
                errorCorrection /= errorCorrectionSize;
                if (errorCorrection.IsAlmostZero(maxError))
                    break;

                // Move desired correction in opposite way of the accumulated error.
                desiredCorrection -= error * errorCorrection;

                ++Iterations;
            }

            TargetCorrection = desiredCorrection;

            return desiredCorrection;
        }

        // DATA STRUCTURES

        private sealed class Correction
        {
            public Vector3 Amount;
            public Vector3 Direction;
            public float Distance;
            public float Error;
        }
    }
}