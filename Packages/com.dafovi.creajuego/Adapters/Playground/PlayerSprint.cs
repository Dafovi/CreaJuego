using System;
using Playground.Movement;
using UnityEngine;

namespace CreaJuego.PlaygroundBackend
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(GameItem),typeof(Move))]
    public sealed class PlayerSprint : MonoBehaviour
    {
        GameItem item;
        Move movement;
        public Func<bool> sprintHeld;
        public float CurrentMultiplier { get; private set; } = 1f;

        void Awake() => Resolve();
        void Update() => Apply();

        void Resolve()
        {
            if(item==null)item=GetComponent<GameItem>();
            if(movement==null)movement=GetComponent<Move>();
            if(sprintHeld==null)sprintHeld=WorkshopInput.SprintHeld;
        }

        public void Apply()
        {
            Resolve();
            CurrentMultiplier=WorkshopInput.SprintMultiplier(sprintHeld!=null&&sprintHeld());
            if(item!=null&&movement!=null)movement.speed=item.speed/20f*CurrentMultiplier;
        }
    }
}
