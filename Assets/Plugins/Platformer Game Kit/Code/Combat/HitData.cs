// Platformer Game Kit // https://kybernetik.com.au/platformer // Copyright 2021-2025 Kybernetik //

#if ! UNITY_EDITOR
#pragma warning disable CS0618 // Type or member is obsolete (for Layers in Animancer Lite).
#endif

using Animancer;
using Animancer.Units;
using PlatformerGameKit.Characters;
using System;
using UnityEngine;

namespace PlatformerGameKit
{
    /// <summary>Information about when and how to activate a <see cref="HitTrigger"/> during an attack animation.</summary>
    /// <remarks>
    /// <strong>Documentation:</strong>
    /// <see href="https://kybernetik.com.au/platformer/docs/combat/melee">
    /// Melee Attacks</see>
    /// </remarks>
    /// https://kybernetik.com.au/platformer/api/PlatformerGameKit/HitData
    /// 
    [Serializable]
    public sealed partial class HitData
    {
        /************************************************************************************************************************/

        [SerializeField]
        [Tooltip("The time when this hit becomes active.\n• " + AnimationTimeAttribute.Tooltip)]
        [AnimationTime(AnimationTimeAttribute.Units.Seconds)]
        private float _StartTime;

        /// <summary>[<see cref="SerializeField"/>]
        /// The <see cref="AnimancerState.Time"/> when this hit becomes active.
        /// </summary>
        public ref float StartTime
            => ref _StartTime;

        /************************************************************************************************************************/

        [SerializeField]
        [Tooltip("The time when this hit becomes inactive.\n• " + AnimationTimeAttribute.Tooltip)]
        [AnimationTime(AnimationTimeAttribute.Units.Seconds)]
        private float _EndTime;

        /// <summary>[<see cref="SerializeField"/>]
        /// The <see cref="AnimancerState.Time"/> when this hit becomes inactive.
        /// </summary>
        public ref float EndTime
            => ref _EndTime;

        /************************************************************************************************************************/

        [SerializeField]
        [Tooltip("The amount of damage this hit deals")]
        private int _Damage;

        /// <summary>[<see cref="SerializeField"/>]
        /// The amount of damage this hit deals.
        /// </summary>
        public ref int Damage
            => ref _Damage;

        /************************************************************************************************************************/

        [SerializeField]
        [Tooltip("The knockback force applied to objects by this hit")]
        private float _Force;

        /// <summary>[<see cref="SerializeField"/>]
        /// The knockback <see cref="Hit.force"/> applied to objects by this hit.
        /// </summary>
        public ref float Force
            => ref _Force;

        /************************************************************************************************************************/

        [SerializeField, Degrees, Tooltip(Strings.Tooltips.HitAngle)]
        private float _Angle;

        /// <summary>[<see cref="SerializeField"/>]
        /// The direction in which the <see cref="Force"/> is applied (in degrees).
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item>0 = Forward</item>
        /// <item>90 = Up</item>
        /// <item>180 = Backward</item>
        /// <item>-90 = Down</item>
        /// </list>
        /// </remarks>
        public ref float Angle
            => ref _Angle;

        /************************************************************************************************************************/

        [SerializeField]
        [Tooltip("The outline of the area affected by this hit")]
        private Vector2[] _Area;

        /// <summary>[<see cref="SerializeField"/>]
        /// The outline of the area affected by this hit.
        /// </summary>
        public ref Vector2[] Area
            => ref _Area;

        /************************************************************************************************************************/

        public static void InitializeEvents(HitData[] hits, AnimancerEvent.Sequence events, float length)
        {
            if (hits == null)
                return;

            // Each of the events needs to call CharacterAnimancerComponent.GetCurrent on its own in case the sequence
            // is being shared by multiple characters.

            var inverseAnimationLength = 1f / length;

            var count = hits.Length;
            events.Capacity = Math.Max(events.Capacity, events.Count + count);

            var normalizedEndTime = float.IsNaN(events.NormalizedEndTime)
                ? 1
                : events.NormalizedEndTime;

            var previousIndex = -1;
            for (int i = 0; i < count; i++)
            {
                var hit = hits[i];
                var start = hit._StartTime * inverseAnimationLength;
                var end = hit._EndTime * inverseAnimationLength;

                Debug.Assert(
                    start < end,
                    $"{nameof(HitData)}.{nameof(StartTime)} must be less than its {nameof(EndTime)}.");

                previousIndex = events.Add(previousIndex + 1, start, () =>
                {
                    var attacker = CharacterAnimancerComponent.GetCurrent();
                    attacker.AddHitBox(hit);
                });

                if (end < normalizedEndTime)
                {
                    previousIndex = events.Add(previousIndex + 1, end, () =>
                    {
                        var attacker = CharacterAnimancerComponent.GetCurrent();
                        attacker.RemoveHitBox(hit);
                    });
                }
            }

            events.OnEnd += () =>
            {
                var attacker = CharacterAnimancerComponent.GetCurrent();
                attacker.ClearHitBoxes();

                // The above code assumes this sequence will be the only thing managing hit boxes while it's active.
                // If there could be other things with their own hit boxes at the same time, we'd need to only remove our ones:
                // for (int i = 0; i < hits.Length; i++)
                //     attacker.RemoveHitBox(hits[i]);
            };
        }

        /************************************************************************************************************************/

        public bool IsActiveAt(float time)
            => time >= _StartTime
            && time < _EndTime;

        /************************************************************************************************************************/

        public override string ToString()
            => $"{nameof(HitData)}({GetDescription(", ")})";

        public string GetDescription(string delimiter) =>
            $"{nameof(StartTime)}={StartTime}" +
            $"{delimiter}{nameof(EndTime)}={EndTime}" +
            $"{delimiter}{nameof(Damage)}={Damage}" +
            $"{delimiter}{nameof(Force)}={Force}";

        /************************************************************************************************************************/

        public static Vector2 AngleToDirection(float angle, Quaternion rotation, bool flipX = false)
        {
            var direction = AngleToDirection(angle, flipX);
            return rotation * direction;
        }

        public static Vector2 AngleToDirection(float angle, bool flipX = false)
        {
            angle *= Mathf.Deg2Rad;

            var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            if (flipX)
                direction.x = -direction.x;

            return direction;
        }

        public static float DirectionToAngle(Vector2 direction, bool flipX = false)
        {
            if (flipX)
                direction.x = -direction.x;

            return Mathf.Atan2(direction.y, direction.x);
        }

        /************************************************************************************************************************/
    }
}
