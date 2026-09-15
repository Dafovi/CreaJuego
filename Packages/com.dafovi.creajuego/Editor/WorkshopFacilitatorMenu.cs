using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CreaJuego.Editor
{
    public static class WorkshopFacilitatorMenu
    {
        [MenuItem("CreaJuego/Facilitador/Nuevo juego", false, 100)]
        private static void NewGame() => NewWorkshopGameWindow.Open();

        [MenuItem("CreaJuego/Facilitador/Preparar escena", false, 101)]
        private static void Prepare() => SceneService.Prepare();

        [MenuItem("CreaJuego/Facilitador/Revisar juego", false, 102)]
        private static void Review()
        {
            CreaJuegoPlayBarWindow.Open();
            var failed = SceneService.Validate().Where(c => !c.passed).Select(c => c.help).Distinct().ToArray();
            Debug.Log(failed.Length == 0 ? "CreaJuego: el juego está listo para probar." : "CreaJuego necesita atención: " + string.Join(" ", failed));
        }

        [MenuItem("CreaJuego/Facilitador/Volver jugador al inicio", false, 103)]
        public static void ReturnPlayerToStart()
        {
            var player = SceneObjects.All<GameItem>(SceneManager.GetActiveScene()).FirstOrDefault(i => i.definition != null && i.definition.kind == ItemKind.Player);
            if (player == null) throw new InvalidOperationException("Este juego todavía no tiene Jugador.");
            var recovery = player.GetComponent<PlayerFallRecovery>();
            if (recovery == null) throw new InvalidOperationException("Pulsa Preparar escena para activar la recuperación del Jugador.");
            if (!Application.isPlaying)
            {
                Undo.RecordObjects(new UnityEngine.Object[] { player.transform, player.GetComponent<Rigidbody2D>() }, "Volver jugador al inicio");
            }
            recovery.ReturnToStart();
            if (!Application.isPlaying) EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
            Selection.activeGameObject = player.gameObject;
        }

        [MenuItem("CreaJuego/Facilitador/Abrir carpeta de trabajos", false, 104)]
        private static void OpenWorksFolder()
        {
            Directory.CreateDirectory(WorkshopGameService.AbsolutePath(WorkshopGameService.WorksRoot));
            AssetDatabase.Refresh();
            EditorUtility.RevealInFinder(WorkshopGameService.AbsolutePath(WorkshopGameService.WorksRoot));
        }

        [MenuItem("CreaJuego/Facilitador/Reabrir ventanas del taller", false, 105)]
        private static void Reopen() => CreaJuegoWindow.Open();
    }
}
