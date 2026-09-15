using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CreaJuego.Editor
{
    public static class WorkshopImageImportService
    {
        public static Sprite ImportAndAssign(string sourcePath, GameItem[] items)
        {
            if (items == null || items.Length == 0) throw new ArgumentException("Selecciona un elemento antes de elegir una imagen.");
            if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath)) throw new FileNotFoundException("No se encontró la imagen elegida.", sourcePath);
            var extension = Path.GetExtension(sourcePath).ToLowerInvariant();
            if (extension != ".png" && extension != ".jpg" && extension != ".jpeg") throw new ArgumentException("Elige una imagen PNG o JPG.");
            foreach (var item in items)
                if (item == null || item.gameObject.scene != SceneManager.GetActiveScene()) throw new ArgumentException("La selección no pertenece al juego abierto.");

            var folder = WorkshopGameService.ImagesFolder(SceneManager.GetActiveScene());
            Directory.CreateDirectory(WorkshopGameService.AbsolutePath(folder));
            var fileName = Sanitize(Path.GetFileNameWithoutExtension(sourcePath));
            var assetPath = AssetDatabase.GenerateUniqueAssetPath(folder + "/" + fileName + extension);
            File.Copy(sourcePath, WorkshopGameService.AbsolutePath(assetPath), false);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Unity no pudo preparar la imagen elegida.");
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = extension == ".png";
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null) throw new InvalidOperationException("Unity no pudo convertir la imagen en una apariencia 2D.");
            ItemAppearance.SetCustomSprite(items, sprite);
            return sprite;
        }

        private static string Sanitize(string name)
        {
            foreach (var character in Path.GetInvalidFileNameChars()) name = name.Replace(character, '_');
            return string.IsNullOrWhiteSpace(name) ? "Imagen" : name.Trim();
        }
    }
}
