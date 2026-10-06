using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
namespace CreaJuego.PlaygroundBackend
{
    public static class WorkshopInput
    {
        public const float RunMultiplier=1.65f;
        public static bool AttackPressed()
        {
            bool key=Keyboard.current!=null&&Keyboard.current.xKey.wasPressedThisFrame;
            bool click=Mouse.current!=null&&Mouse.current.leftButton.wasPressedThisFrame;
            bool overInterface=EventSystem.current!=null&&EventSystem.current.IsPointerOverGameObject();
            return AttackPressed(key,click,overInterface);
        }
        public static bool AttackPressed(bool keyPressed,bool mousePressed,bool pointerOverInterface)=>keyPressed||(mousePressed&&!pointerOverInterface);
        public static bool SprintHeld()
        {
            var keys=Keyboard.current;
            return keys!=null&&(keys.leftShiftKey.isPressed||keys.rightShiftKey.isPressed);
        }
        public static float SprintMultiplier(bool sprintHeld)=>sprintHeld?RunMultiplier:1f;
        public static Vector2 ReadMovement()
        {
            var keys=Keyboard.current;
            if(keys==null) return Vector2.zero;
            bool left=keys.aKey.isPressed || keys.leftArrowKey.isPressed;
            bool right=keys.dKey.isPressed || keys.rightArrowKey.isPressed;
            return new Vector2((right?1:0)-(left?1:0),0);
        }
    }
}

