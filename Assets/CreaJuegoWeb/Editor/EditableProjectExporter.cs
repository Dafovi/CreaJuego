using System;
using System.IO;
using System.Text;
using CreaJuego.Web;
using UnityEditor;
using UnityEngine;

namespace CreaJuego.Web.Editor
{
    /// <summary>
    /// Exports the current in-memory authoring project while Unity is in Play Mode.
    /// The resulting JSON can later be imported into the editable scene.
    /// </summary>
    public static class EditableProjectExporter
    {
        const string MenuPath = "CreaJuego/Proyecto/Exportar JSON desde Play Mode...";

        [MenuItem(MenuPath)]
        public static void ExportWithFilePicker()
        {
            var controller = FindController();
            if (!EditorApplication.isPlaying || controller == null)
            {
                EditorUtility.DisplayDialog(
                    "Exportación no disponible",
                    "Entra en Play Mode y abre CreaJuego antes de exportar el nivel.",
                    "Aceptar");
                return;
            }

            var path = EditorUtility.SaveFilePanel(
                "Exportar nivel actual de CreaJuego",
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                SafeFileName(controller.Project?.projectName),
                "json");
            if (string.IsNullOrWhiteSpace(path)) return;

            try
            {
                ExportToPath(controller, path);
                EditorUtility.DisplayDialog(
                    "Nivel exportado",
                    "El estado actual del nivel quedó guardado como JSON. Sal de Play Mode y usa CreaJuego > Proyecto > Importar JSON en escena editable para dejarlo definitivo.",
                    "Aceptar");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("No se pudo exportar", exception.Message, "Aceptar");
            }
        }

        [MenuItem(MenuPath, true)]
        public static bool CanExportWithFilePicker()
        {
            return EditorApplication.isPlaying && FindController() != null;
        }

        public static void ExportToPath(RuntimeAuthoringController controller, string path)
        {
            if (controller == null)
                throw new ArgumentNullException(nameof(controller));
            ExportProjectToPath(controller.Project, path);
        }

        public static void ExportProjectToPath(CreaJuegoProjectData project, string path)
        {
            if (project == null)
                throw new InvalidOperationException("CreaJuego no tiene un proyecto activo para exportar.");
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("La ruta de exportación está vacía.", nameof(path));

            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(path, ProjectSerializer.ToJson(project), new UTF8Encoding(false));

            var tileCount = 0;
            foreach (var layer in project.tileLayers)
                tileCount += layer?.cells?.Count ?? 0;
            Debug.Log($"CREAJUEGO_PLAYMODE_EXPORT_READY path={path} name={project.projectName} objects={project.objects.Count} tiles={tileCount} ramps={project.tileRamps.Count}");
        }

        static RuntimeAuthoringController FindController()
        {
            return UnityEngine.Object.FindAnyObjectByType<RuntimeAuthoringController>(FindObjectsInactive.Include);
        }

        static string SafeFileName(string projectName)
        {
            var name = string.IsNullOrWhiteSpace(projectName) ? "nivel-creajuego" : projectName.Trim();
            foreach (var invalid in Path.GetInvalidFileNameChars()) name = name.Replace(invalid, '-');
            return string.IsNullOrWhiteSpace(name) ? "nivel-creajuego" : name;
        }
    }
}
