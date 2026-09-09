using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CreaJuego.Editor
{
    public sealed class SceneCheck
    {
        public string label, help;
        public bool passed;
        public SceneCheck(string label, bool passed, string help) { this.label = label; this.passed = passed; this.help = help; }
    }
    public static class SceneService
    {
        public static List<SceneCheck> Validate()
        {
            var scene = SceneManager.GetActiveScene();
            var items = SceneObjects.All<GameItem>(scene);
            var active = items.Where(i => i.gameObject.activeInHierarchy && i.enabled && i.definition != null).ToArray();
            var sessions = SceneObjects.All<MonoBehaviour>(scene).OfType<IWorkshopSession>().ToArray();
            var checks = new List<SceneCheck>
            {
                new SceneCheck("Una escena de taller", SceneManager.sceneCount == 1 && PrefabStageUtility.GetCurrentPrefabStage() == null, "Abre una sola escena para jugar este taller."),
                new SceneCheck("Personaje", active.Count(i => i.definition.kind == ItemKind.Player) == 1 && items.Count(i => i.definition != null && i.definition.kind == ItemKind.Player) == 1, "Tu juego necesita exactamente un personaje activo."),
                new SceneCheck("Cámara", SceneObjects.All<Camera>(scene).Any(c => c.isActiveAndEnabled), "Prepara la escena para poder ver el juego."),
                new SceneCheck("Sesión", sessions.Length == 1, "Prepara la escena para controlar el inicio y el final."),
                new SceneCheck("Marcador", sessions.Length == 1 && sessions[0].ConfigurationError() == null, sessions.Length == 1 ? sessions[0].ConfigurationError() ?? "Puntos y puntos de vida preparados." : "Prepara el marcador de puntos y puntos de vida."),
                new SceneCheck("Meta", active.Any(i => i.definition.kind == ItemKind.Goal), "Agrega una Meta para indicar dónde termina el recorrido.")
            };
            var invalid = items.Select(i => ItemError(i)).FirstOrDefault(s => s != null);
            checks.Add(new SceneCheck("Elementos completos", invalid == null, invalid ?? "Los elementos están preparados."));
            return checks;
        }
        private static string ItemError(GameItem item)
        {
            if (item.definition == null || item.definition.prefab == null) return "Hay un elemento sin contenido. Recréalo desde el catálogo.";
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject) > 0) return "Hay un elemento incompleto. Recréalo desde el catálogo.";
            var backends = item.GetComponents<MonoBehaviour>().OfType<IItemBackend>().ToArray();
            if (backends.Length != 1 || !(backends[0] is IBackendValidation validation)) return "Falta un comportamiento válido en " + item.definition.displayName + ".";
            return validation.ConfigurationError();
        }
        public static bool TryPlay(out List<SceneCheck> checks)
        {
            checks = Validate();
            if (checks.Any(c => !c.passed)) return false;
            EditorApplication.EnterPlaymode(); return true;
        }
        [MenuItem("CreaJuego/Preparar escena")]
        public static void Prepare()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Detén el juego antes de preparar.");
            var scene = SceneManager.GetActiveScene();
            if (PrefabStageUtility.GetCurrentPrefabStage() != null || SceneManager.sceneCount != 1) throw new InvalidOperationException("Abre una sola escena de taller.");
            if (SceneObjects.All<MonoBehaviour>(scene).OfType<IWorkshopSession>().Any())
                return; // Never overwrite or duplicate existing session infrastructure.
            if (SceneObjects.All<Canvas>(scene).Length > 0) throw new InvalidOperationException("Ya hay indicaciones en esta escena. Recupera su sesión o prepara una escena nueva.");
            var packs = AssetDatabase.FindAssets("t:ContentPackDefinition").Select(g => AssetDatabase.LoadAssetAtPath<ContentPackDefinition>(AssetDatabase.GUIDToAssetPath(g))).Where(p => p.sceneServices != null).ToArray();
            if (packs.Length != 1 || packs[0].sceneServices == null) throw new InvalidOperationException("No está disponible la preparación del pack.");
            bool hasCamera = SceneObjects.All<Camera>(scene).Any(c => c.isActiveAndEnabled);
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
            var root = (GameObject)PrefabUtility.InstantiatePrefab(packs[0].sceneServices, scene);
            Undo.RegisterCreatedObjectUndo(root, "Preparar escena");
            if (hasCamera)
                foreach (var camera in root.GetComponentsInChildren<Camera>()) Undo.DestroyObjectImmediate(camera.gameObject);
            EditorSceneManager.MarkSceneDirty(scene); Undo.CollapseUndoOperations(group);
        }
        public static string RuntimeHelp()
        {
            var session = SceneObjects.All<MonoBehaviour>(SceneManager.GetActiveScene()).OfType<IWorkshopSession>().FirstOrDefault();
            if (session == null) return "Muévete con A/D o las flechas, salta con Espacio y golpea con X.";
            return session.State == GameSessionState.Won ? "¡Tu recorrido puede completarse!" :
                session.State == GameSessionState.Lost ? "Sin puntos de vida. Detén el juego para cambiar tu recorrido." :
                "Haz clic en Juego. Muévete con A/D o las flechas, salta con Espacio y golpea con X.";
        }
    }
}



