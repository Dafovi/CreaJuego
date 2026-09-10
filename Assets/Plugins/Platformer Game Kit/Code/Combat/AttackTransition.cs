// Platformer Game Kit // https://kybernetik.com.au/platformer // Copyright 2021-2025 Kybernetik //

using Animancer;
using System;
using UnityEngine;

namespace PlatformerGameKit
{
    /// <summary>A <see cref="ClipTransition"/> with <see cref="HitData"/>.</summary>
    /// <remarks>
    /// <strong>Documentation:</strong>
    /// <see href="https://kybernetik.com.au/platformer/docs/combat/melee">
    /// Melee Attacks</see>
    /// </remarks>
    /// https://kybernetik.com.au/platformer/api/PlatformerGameKit/AttackTransition
    /// 
    [Serializable]
    public partial class AttackTransition : ClipTransition, ICopyable<AttackTransition>
    {
        /************************************************************************************************************************/

        [SerializeField]
        private HitData[] _Hits;

        /// <summary>The details of the hit boxes involved in this attack.</summary>
        public HitData[] Hits
        {
            get => _Hits;
            set
            {
                if (_HasInitializedEvents)
                    throw new InvalidOperationException(
                        $"Modifying the {nameof(AttackTransition)}.{nameof(Hits)} after the transition has already been used" +
                        $" is not supported because its initialisation modifies the underlying" +
                        $" {nameof(AnimancerEvent)}.{nameof(AnimancerEvent.Sequence)} in a way that can't be easily undone.");

                _Hits = value;
            }
        }

        private bool _HasInitializedEvents;

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override AnimancerEvent.Sequence Events
        {
            get
            {
                var events = base.Events;

                if (!_HasInitializedEvents)
                {
                    _HasInitializedEvents = true;
                    HitData.InitializeEvents(Hits, events, Clip.length);
                }

                return events;
            }
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override void CopyFrom(ClipTransition copyFrom, CloneContext context)
            => this.CopyFromBase(copyFrom, context);

        /// <inheritdoc/>
        public virtual void CopyFrom(AttackTransition copyFrom, CloneContext context)
        {
            base.CopyFrom(copyFrom, context);

            AnimancerUtilities.CopyExactArray(copyFrom._Hits, ref _Hits);
        }

        /************************************************************************************************************************/
    }
}
