using System;
using System.IO;
using CreaJuego.Web;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CreaJuego.Web.Editor
{
    /// <summary>
    /// Imports a CreaJuego project into the editable Web scene and persists its
    /// generated objects and terrain so they are visible before entering Play Mode.
    /// </summary>
    public static class EditableProjectImporter
    {
        const string CommandLineArgument = "-creajuegoProject";
        const string MenuPath = "CreaJuego/Proyecto/Importar JSON en escena editable...";

        [MenuItem(MenuPath)]
        public static void ImportWithFilePicker()
        {
            var path = EditorUtility.OpenFilePanel(
                "Importar nivel de CreaJuego",
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "json,creajuego");
            if (string.IsNullOrWhiteSpace(path)) return;

            try
            {
                ImportFromPath(path);
                EditorUtility.DisplayDialog(
                    "Nivel importado",
                    "El nivel quedó reconstruido y guardado en WebAuthoringSpike. Se puede revisar sin entrar en Play Mode.",
                    "Aceptar");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("No se pudo importar", exception.Message, "Aceptar");
            }
        }

        [MenuItem(MenuPath, true)]
        public static bool CanImportWithFilePicker()
        {
            return !EditorApplication.isPlaying;
        }

        public static void ImportFromCommandLine()
        {
            var arguments = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(arguments, CommandLineArgument);
            if (index < 0 || index + 1 >= arguments.Length)
                throw new ArgumentException($"Falta {CommandLineArgument} seguido de la ruta del proyecto.");
            ImportFromPath(arguments[index + 1]);
        }

        public static void ImportFromPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                throw new FileNotFoundException("No se encontró el proyecto de CreaJuego.", path);

            var json = File.ReadAllText(path);
            var imported = ProjectSerializer.FromJson(json);
            if (imported.objects.Count == 0 && imported.tileLayers.Count == 0)
                throw new InvalidDataException("El archivo no contiene objetos ni terreno para reconstruir.");

            var scene = EditorSceneManager.OpenScene(WebSpikeBuilder.ScenePath, OpenSceneMode.Single);
            var controller = UnityEngine.Object.FindAnyObjectByType<RuntimeAuthoringController>();
            if (controller == null)
                throw new InvalidOperationException("WebAuthoringSpike no contiene RuntimeAuthoringController.");

            controller.ReceiveProjectJson(json);
            if (!string.Equals(controller.Project.projectName, imported.projectName, StringComparison.Ordinal) ||
                controller.Project.objects.Count != imported.objects.Count)
                throw new InvalidDataException("CreaJuego no pudo aplicar completamente el proyecto importado.");

            controller.FrameAll();
            if (!controller.CaptureEditableScene(true))
                throw new InvalidOperationException("No se pudo capturar el nivel reconstruido en la escena editable.");

            controller.ui?.RefreshEditableLayout();
            EditorUtility.SetDirty(controller);
            if (controller.ui != null) EditorUtility.SetDirty(controller.ui);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, WebSpikeBuilder.ScenePath))
                throw new IOException("Unity no pudo guardar WebAuthoringSpike.");
            AssetDatabase.SaveAssets();

            var tileCount = 0;
            foreach (var layer in controller.Project.tileLayers)
                tileCount += layer?.cells?.Count ?? 0;
            Debug.Log($"CREAJUEGO_EDITABLE_IMPORT_READY name={controller.Project.projectName} objects={controller.Project.objects.Count} tiles={tileCount} ramps={controller.Project.tileRamps.Count}");
        }
    }
}
