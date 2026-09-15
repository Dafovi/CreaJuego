using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CreaJuego.Editor
{
    public static class WorkshopGameService
    {
        public const string WorksRoot = "Assets/TrabajosTaller";

        public static WorkshopGameMetadata Metadata(Scene scene) => SceneObjects.All<WorkshopGameMetadata>(scene).FirstOrDefault();

        public static string CreateNew(string gameName, string teamName)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Detén el juego antes de crear uno nuevo.");
            if (string.IsNullOrWhiteSpace(gameName) || string.IsNullOrWhiteSpace(teamName))
                throw new ArgumentException("Escribe el nombre del juego y del equipo.");

            var teamFolder = WorksRoot + "/" + WithPrefix("Equipo", teamName);
            Directory.CreateDirectory(AbsolutePath(teamFolder));
            AssetDatabase.Refresh();
            var scenePath = AssetDatabase.GenerateUniqueAssetPath(teamFolder + "/" + WithPrefix("Juego", gameName) + ".unity");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var information = new GameObject("Información del juego", typeof(WorkshopGameMetadata));
            SceneManager.MoveGameObjectToScene(information, scene);
            information.GetComponent<WorkshopGameMetadata>().Configure(gameName, teamName);
            SceneService.Prepare();
            TrySetDefaultBackground();

            Directory.CreateDirectory(AbsolutePath(Path.GetDirectoryName(scenePath).Replace('\\', '/') + "/Imagenes"));
            AssetDatabase.Refresh();
            if (!EditorSceneManager.SaveScene(scene, scenePath)) throw new InvalidOperationException("Unity no pudo guardar el juego nuevo.");
            WorkshopEditorConfiguration.ConfigureGameView();
            return scenePath;
        }

        public static string ImagesFolder(Scene scene)
        {
            if (!scene.IsValid() || string.IsNullOrWhiteSpace(scene.path) || !scene.path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Guarda el juego con CreaJuego > Nuevo juego antes de importar imágenes.");
            return Path.GetDirectoryName(scene.path).Replace('\\', '/') + "/Imagenes";
        }

        private static void TrySetDefaultBackground()
        {
            var option = ItemService.Catalog().Select(d => d.appearancePack?.CategoryFor(ItemKind.Background))
                .Where(c => c != null).Distinct().SelectMany(c => c.options).FirstOrDefault(o => o != null && o.Preview != null);
            if (option != null) WorldAuthoringService.SetBackground(option.Preview);
        }

        public static string AbsolutePath(string assetPath) => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Application.dataPath), assetPath));

        private static string WithPrefix(string prefix, string value)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var clean = new string(value.Trim().Select(c => invalid.Contains(c) ? '_' : char.IsWhiteSpace(c) ? '_' : c).ToArray());
            while (clean.Contains("__")) clean = clean.Replace("__", "_");
            clean = clean.Trim('_', '.');
            if (string.IsNullOrWhiteSpace(clean)) clean = "Sin_nombre";
            return clean.StartsWith(prefix + "_", StringComparison.OrdinalIgnoreCase) ? clean : prefix + "_" + clean;
        }
    }
}
