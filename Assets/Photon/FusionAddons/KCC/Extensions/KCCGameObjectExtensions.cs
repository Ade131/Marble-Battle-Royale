using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Fusion.Addons.KCC
{
    public static class KCCGameObjectExtensions
    {
        // PUBLIC METHODS

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetComponentNoAlloc<T>(this GameObject gameObject) where T : class
        {
#if UNITY_EDITOR
            return GameObjectExtensions<T>.GetComponentNoAlloc(gameObject);
#else
			return gameObject.GetComponent<T>();
#endif
        }
    }

    public static class GameObjectExtensions<T> where T : class
    {
        // PRIVATE MEMBERS

        private static readonly List<T> _components = new();

        // PUBLIC METHODS

        public static T GetComponentNoAlloc(GameObject gameObject)
        {
            _components.Clear();

            gameObject.GetComponents(_components);

            if (_components.Count > 0)
            {
                var component = _components[0];

                _components.Clear();

                return component;
            }

            return null;
        }
    }
}