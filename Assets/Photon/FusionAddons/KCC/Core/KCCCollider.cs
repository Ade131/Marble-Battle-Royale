using UnityEngine;

namespace Fusion.Addons.KCC
{
	/// <summary>
	///     Custom wrapper/cache for fast property checks and synchronization.
	///     Direct changes on the Collider component or the Game Object won't propagate here.
	/// </summary>
	public sealed partial class KCCCollider
    {
        public CapsuleCollider Collider;
        // PUBLIC MEMBERS

        public GameObject GameObject;
        public float Height;
        public bool IsSpawned;
        public bool IsTrigger;
        public int Layer;
        public float Radius;
        public Transform Transform;

        // PUBLIC METHODS

        public void Update(KCC kcc)
        {
            var settings = kcc.Settings;

            if (IsSpawned == false)
            {
                IsTrigger = settings.IsTrigger;
                Radius = settings.Radius;
                Height = settings.Height;
                Layer = settings.ColliderLayer;

                GameObject = new GameObject("KCCCollider");
                GameObject.layer = settings.ColliderLayer;

                Transform = GameObject.transform;
                Transform.SetParent(kcc.Transform, false);
                Transform.localPosition = Vector3.zero;
                Transform.localRotation = Quaternion.identity;
                Transform.localScale = Vector3.one;

                Collider = GameObject.AddComponent<CapsuleCollider>();
                Collider.direction = 1;
                Collider.isTrigger = settings.IsTrigger;
                Collider.radius = settings.Radius;
                Collider.height = settings.Height;
                Collider.center = new Vector3(0.0f, settings.Height * 0.5f, 0.0f);

                IsSpawned = true;
            }

            if (IsTrigger != settings.IsTrigger)
            {
                IsTrigger = settings.IsTrigger;
                Collider.isTrigger = settings.IsTrigger;
            }

            if (Radius != settings.Radius)
            {
                Radius = settings.Radius;
                Collider.radius = settings.Radius;
            }

            if (Height != settings.Height)
            {
                Height = settings.Height;
                Collider.height = settings.Height;
                Collider.center = new Vector3(0.0f, settings.Height * 0.5f, 0.0f);
            }

            if (Layer != settings.ColliderLayer)
            {
                Layer = settings.ColliderLayer;
                GameObject.layer = settings.ColliderLayer;
            }

            OnUpdate(kcc);
        }

        public void Destroy()
        {
            if (IsSpawned == false)
                return;

            if (Collider != null) Collider.enabled = false;

            Object.Destroy(GameObject);

            GameObject = default;
            Transform = default;
            Collider = default;
            IsSpawned = default;
            IsTrigger = default;
            Radius = default;
            Height = default;
            Layer = default;

            OnDestroy();
        }

        // PARTIAL METHODS

        partial void OnUpdate(KCC kcc);
        partial void OnDestroy();
    }
}