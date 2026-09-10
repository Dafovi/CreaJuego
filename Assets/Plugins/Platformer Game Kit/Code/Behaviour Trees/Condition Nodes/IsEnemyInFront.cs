// Platformer Game Kit // https://kybernetik.com.au/platformer // Copyright 2021-2025 Kybernetik //

using Animancer;
using Animancer.Units;
using PlatformerGameKit.Characters;
using System;
using UnityEngine;
using static Animancer.Validate;

namespace PlatformerGameKit.BehaviourTrees
{
    /// <summary>A <see cref="ConditionNode"/> which checks if an enemy is in front of the character.</summary>
    /// <remarks>
    /// <strong>Documentation:</strong>
    /// <see href="https://kybernetik.com.au/platformer/docs/characters/brains/behaviour/specific#conditions">
    /// Behaviour Tree Brains - Conditions</see>
    /// </remarks>
    /// https://kybernetik.com.au/platformer/api/PlatformerGameKit.BehaviourTrees/IsEnemyInFront
    /// 
    [Serializable]
    public class IsEnemyInFront : ConditionNode
    {
        /************************************************************************************************************************/

        [SerializeField]
        [Meters(Rule = Value.IsNotNegative)]
        [Tooltip("The maximum distance within which to check (in meters)")]
        private float _Range = 1;

        /// <summary>The maximum distance within which to check (in meters).</summary>
        public ref float Range
            => ref _Range;

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override bool Condition
        {
            get
            {
                var character = Context<Character>.Current;
                var rigidbody = character.Body.Rigidbody;

                var bounds = character.Body.Collider.bounds;
                var center = (Vector2)bounds.center + character.MovementDirection * _Range;

                var filter = new ContactFilter2D
                {
                    layerMask = HitTrigger.HitLayers,
                    useLayerMask = true,
                };

                var colliders = ListPool.Acquire<Collider2D>();
                Physics2D.OverlapBox(center, bounds.size, rigidbody.rotation, filter, colliders);
                for (int i = 0; i < colliders.Count; i++)
                {
                    var hit = new Hit(character.transform, character.Health.Team, 0, 0, default);
                    if (hit.CanHit(colliders[i]))
                    {
                        ListPool.Release(colliders);
                        return true;
                    }
                }
                ListPool.Release(colliders);
                return false;
            }
        }

        /************************************************************************************************************************/
    }
}
