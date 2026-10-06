using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CreaJuego.Web
{
    // Pointer-up commits one history entry after a slider drag.
    public sealed class RuntimeSliderCommit : MonoBehaviour, IPointerUpHandler
    {
        public Action commit;
        public void OnPointerUp(PointerEventData eventData) => commit?.Invoke();
    }
}
