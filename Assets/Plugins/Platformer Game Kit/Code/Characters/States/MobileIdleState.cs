// Platformer Game Kit // https://kybernetik.com.au/platformer // Copyright 2021-2025 Kybernetik //

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value.

using UnityEngine;

namespace PlatformerGameKit.Characters.States
{
    /// <summary>An <see cref="IdleState"/> that can move.</summary>
    /// <remarks>
    /// <strong>Documentation:</strong>
    /// <see href="https://kybernetik.com.au/platformer/docs/characters/states/idle/mobile-idle">
    /// Mobile Idle</see>
    /// </remarks>
    /// https://kybernetik.com.au/platformer/api/PlatformerGameKit.Characters.States/MobileIdleState
    /// 
    [AddComponentMenu(MenuPrefix + "Mobile Idle State")]
    [PlatformerHelpUrl(typeof(MobileIdleState))]
    public class MobileIdleState : IdleState
    {
        /************************************************************************************************************************/

        [SerializeField, Range(0, 1)]
        [Tooltip("The character's speed is multiplied by this value while in this state")]
        private float _MovementSpeedMultiplier = 1;

        public override float MovementSpeedMultiplier
            => _MovementSpeedMultiplier;

        /************************************************************************************************************************/
    }
}
