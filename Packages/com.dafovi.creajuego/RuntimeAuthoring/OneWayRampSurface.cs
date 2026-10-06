using UnityEngine;

namespace CreaJuego.Web
{
    /// <summary>
    /// Configura una rampa como superficie atravesable desde abajo.
    /// PlatformEffector2D usa el eje superior local, por lo que también funciona
    /// cuando la rampa está rotada hacia cualquiera de los dos lados.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D), typeof(PlatformEffector2D))]
    public sealed class OneWayRampSurface : MonoBehaviour
    {
        [SerializeField, Range(120f, 180f)] float surfaceArc = 160f;

        public float SurfaceArc => surfaceArc;

        void Awake() => Configure();

#if UNITY_EDITOR
        void OnValidate() => Configure();
#endif

        public void Configure()
        {
            var surface = GetComponent<Collider2D>();
            var effector = GetComponent<PlatformEffector2D>();
            if (surface == null || effector == null) return;

            surface.usedByEffector = true;
            effector.useOneWay = true;
            effector.useOneWayGrouping = true;
            effector.surfaceArc = surfaceArc;
            effector.useSideFriction = false;
            effector.useSideBounce = false;
            effector.sideArc = 0f;
        }
    }
}
