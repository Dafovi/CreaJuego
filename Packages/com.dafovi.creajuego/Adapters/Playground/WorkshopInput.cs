using UnityEngine;
using UnityEngine.InputSystem;
namespace CreaJuego.PlaygroundBackend
{
    public static class WorkshopInput
    {
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
