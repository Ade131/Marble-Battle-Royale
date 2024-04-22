using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fusion.Addons.KCC
{
    // This file contains definition of KCC events.
    public partial class KCC
    {
        // PUBLIC MEMBERS

        /// <summary>
        ///     Custom collision resolver callback. Use this to apply extra filtering.
        /// </summary>
        public Func<KCC, Collider, bool> ResolveCollision;

        /// <summary>
        ///     Callback to provide an external list of processors. For example items in inventory.
        ///     These processors are NOT tracked by KCC and never get the OnEnter/OnExit callbacks.
        ///     Called typically once per frame/tick.
        /// </summary>
        public Func<IList<IKCCProcessor>> GetExternalProcessors;

        /// <summary>
        ///     Called when a collision with networked object starts. This callback is invoked in both fixed and render update.
        /// </summary>
        public event Action<KCC, KCCCollision> OnCollisionEnter;

        /// <summary>
        ///     Called when a collision with networked object ends. This callback is invoked in both fixed and render update.
        /// </summary>
        public event Action<KCC, KCCCollision> OnCollisionExit;
    }
}