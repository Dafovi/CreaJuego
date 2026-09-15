using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CreaJuego.Editor
{
    public static class PlayerSafetyService
    {
        public static bool HasSupportingSurface(GameItem player)
        {
            if (player == null || player.definition == null) return false;
            Physics2D.SyncTransforms();
            var playerColliders = player.GetComponents<Collider2D>().Where(c => c.enabled && !c.isTrigger).ToArray();
            var surfaces = SceneObjects.All<GameItem>(player.gameObject.scene)
                .Where(i => i != player && i.definition != null &&
                    (i.definition.kind == ItemKind.Platform || i.definition.kind == ItemKind.MovingPlatform))
                .SelectMany(i => i.GetComponents<Collider2D>()).Where(c => c.enabled && !c.isTrigger);
            foreach (var person in playerColliders)
            foreach (var surface in surfaces)
            {
                var feet = person.bounds.min.y;
                var top = surface.bounds.max.y;
                var horizontal = person.bounds.max.x > surface.bounds.min.x + .02f && person.bounds.min.x < surface.bounds.max.x - .02f;
                if (horizontal && feet - top >= -.12f && feet - top <= .35f) return true;
            }
            return false;
        }

        public static int EnsureRecovery(Scene scene)
        {
            int added = 0;
            foreach (var player in SceneObjects.All<GameItem>(scene).Where(i => i.definition != null && i.definition.kind == ItemKind.Player))
            {
                var recovery = player.GetComponent<PlayerFallRecovery>();
                if (recovery == null)
                {
                    recovery = Undo.AddComponent<PlayerFallRecovery>(player.gameObject);
                    added++;
                }
                recovery.CaptureCurrentPosition();
                EditorUtility.SetDirty(recovery);
                PrefabUtility.RecordPrefabInstancePropertyModifications(recovery);
            }
            if (added > 0) EditorSceneManager.MarkSceneDirty(scene);
            return added;
        }
    }
}
