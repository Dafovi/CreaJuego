using UnityEditor;
using UnityEngine;

namespace CreaJuego.Editor
{
    public static class EducationalGizmos
    {
        public static bool TryGetMovementPath(GameItem item, out Vector3 start, out Vector3 end)
        {
            start = end = Vector3.zero;
            if (item == null || item.definition == null ||
                item.definition.kind != ItemKind.Enemy && item.definition.kind != ItemKind.MovingPlatform)
                return false;

            start = item.transform.position;
            end = start + Vector3.right * Mathf.Max(.5f, item.distance);
            return true;
        }

        public static string InteractionLabel(GameItem item)
        {
            if (item == null || item.definition == null) return string.Empty;
            switch (item.definition.kind)
            {
                case ItemKind.Platform: return "Superficie";
                case ItemKind.MovingPlatform: return "Superficie móvil";
                case ItemKind.Prize: return "Se recoge";
                case ItemKind.Hazard: return "Hace daño";
                case ItemKind.Enemy: return "Hace daño";
                case ItemKind.Goal: return "Meta";
                default: return string.Empty;
            }
        }

        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Pickable)]
        private static void DrawItem(GameItem item, GizmoType gizmoType)
        {
            var selected = (gizmoType & GizmoType.Selected) != 0;
            if (TryGetMovementPath(item, out var start, out var end))
            {
                var color = item.definition.kind == ItemKind.Enemy
                    ? new Color(1f, .35f, .2f, selected ? 1f : .65f)
                    : new Color(.2f, .75f, 1f, selected ? 1f : .65f);
                DrawRoute(start, end, color, selected ? "Recorrido: " + item.distance.ToString("0.#") : string.Empty);
            }

            var label = InteractionLabel(item);
            if (selected && !string.IsNullOrEmpty(label))
                DrawInteraction(item, label);

            if (selected && item.definition != null && item.definition.kind == ItemKind.Player)
                DrawFallRecovery(item.GetComponent<PlayerFallRecovery>());
        }


        private static void DrawRoute(Vector3 start, Vector3 end, Color color, string label)
        {
            using (new Handles.DrawingScope(color))
            {
                Handles.DrawAAPolyLine(4f, start, end);
                Handles.DrawSolidDisc(start, Vector3.forward, .11f);
                Handles.DrawWireDisc(end, Vector3.forward, .16f);
                var direction = (end - start).normalized;
                var tipBase = end - direction * .32f;
                var side = Vector3.Cross(Vector3.forward, direction) * .16f;
                Handles.DrawAAPolyLine(4f, tipBase + side, end, tipBase - side);
            }
            if (!string.IsNullOrEmpty(label))
                Handles.Label((start + end) * .5f + Vector3.up * .25f, label, LabelStyle());
        }

        private static void DrawInteraction(GameItem item, string label)
        {
            var collider = item.GetComponent<Collider2D>();
            if (collider == null) return;
            var bounds = collider.bounds;
            using (new Handles.DrawingScope(new Color(1f, .85f, .1f, .9f)))
                Handles.DrawWireCube(bounds.center, bounds.size);
            Handles.Label(bounds.center + Vector3.up * (bounds.extents.y + .2f), label, LabelStyle());
        }

        private static void DrawFallRecovery(PlayerFallRecovery recovery)
        {
            if (recovery == null || !recovery.HasSafePosition) return;
            var center = new Vector3(recovery.SafePosition.x, recovery.FallLimit, 0);
            using (new Handles.DrawingScope(new Color(.95f, .25f, .25f, .8f)))
                Handles.DrawDottedLine(center + Vector3.left * 5, center + Vector3.right * 5, 5);
            Handles.Label(center + Vector3.up * .2f, "Si cae aquí, vuelve al inicio", LabelStyle());
        }


        private static GUIStyle LabelStyle()
        {
            var style = new GUIStyle(EditorStyles.boldLabel);
            style.normal.textColor = Color.white;
            style.alignment = TextAnchor.MiddleCenter;
            return style;
        }
    }
}