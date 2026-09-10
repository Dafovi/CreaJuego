// Platformer Game Kit // https://kybernetik.com.au/platformer // Copyright 2021-2025 Kybernetik //

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value.

using Animancer;
using UnityEngine;

namespace PlatformerGameKit.Characters.States
{
    /// <summary>A <see cref="CharacterState"/> that plays an animation then returns to idle.</summary>
    /// <remarks>
    /// <strong>Documentation:</strong>
    /// <see href="https://kybernetik.com.au/platformer/docs/characters/states/attack/basic">
    /// Basic Action</see>
    /// </remarks>
    /// https://kybernetik.com.au/platformer/api/PlatformerGameKit.Characters.States/BasicActionState
    /// 
    [AddComponentMenu(MenuPrefix + "Basic Action State")]
    [PlatformerHelpUrl(typeof(BasicActionState))]
    public class BasicActionState : AttackState
    {
        /************************************************************************************************************************/

        [SerializeReference] private ITransition _Animation;

        /************************************************************************************************************************/

        protected virtual void Awake()
        {
            _Animation.Events.OnEnd += Character.StateMachine.ForceSetDefaultState;
        }

        /************************************************************************************************************************/

        public override void OnEnterState()
        {
            base.OnEnterState();
            Character.Animancer.Play(_Animation);
        }

        /************************************************************************************************************************/
    }
}
