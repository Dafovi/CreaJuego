using System.IO;
using System.Linq;
using CreaJuego.Editor;
using UnityEditor;
using UnityEngine;

namespace CreaJuego.Starter
{
    // Explicit migration of educational metadata only; never rebuild authored scenes or prefabs.
    public static class EducationalLayerV1
    {
        public static void Apply()
        {
            foreach (var definition in ItemService.Catalog().Where(d => AssetDatabase.GetAssetPath(d).StartsWith(DemoBuilder.Root + "/")))
            {
                string path = AssetDatabase.GetAssetPath(definition);
                string backup = ".spike/educational-v1-before/" + definition.id + ".asset";
                Directory.CreateDirectory(Path.GetDirectoryName(backup));
                if (!File.Exists(backup)) File.Copy(path, backup);
                definition.availableInWorkshop = definition.kind != ItemKind.MovingPlatform && definition.kind != ItemKind.Enemy && definition.kind != ItemKind.Decoration;
                definition.allowMultiple = definition.kind != ItemKind.Player;
                definition.icon = MakeIcon(definition);
                switch (definition.kind)
                {
                    case ItemKind.Player:
                        definition.category = "Personaje";
                        definition.learningHint = "Este es el personaje que controla quien juega. Muévelo con A/D o las flechas y salta con Espacio.";
                        Set(definition, nameof(GameItem.speed), "Movimiento", "Velocidad", "Controla qué tan rápido se mueve el personaje.");
                        Set(definition, nameof(GameItem.jump), "Salto", "Fuerza de salto", "Aumenta este valor para saltar más alto.");
                        Set(definition, nameof(GameItem.health), "Vida", "Puntos de vida", "Cuánto daño puede recibir antes de terminar la partida.");
                        break;
                    case ItemKind.Platform:
                        definition.learningHint = "Un lugar firme donde el personaje puede caminar y apoyarse.";
                        Set(definition, nameof(GameItem.tint), "Apariencia", "Color", "Elige el color de esta plataforma al jugar.");
                        break;
                    case ItemKind.Prize:
                        definition.learningHint = "El personaje recoge este premio al tocarlo y suma los puntos que elijas.";
                        Set(definition, nameof(GameItem.points), "Recompensa", "Puntos", "Cantidad que suma al recoger este premio.", "puntos");
                        break;
                    case ItemKind.Hazard:
                        definition.description = "Al tocarlo pierdes puntos de vida.";
                        definition.learningHint = "Al tocar este peligro, el personaje pierde puntos de vida. Si llega a cero, termina la partida.";
                        Set(definition, nameof(GameItem.damage), "Contacto", "Daño al personaje", "Vida que pierde al entrar en contacto.");
                        Set(definition, nameof(GameItem.disappear), "Contacto", "Desaparecer después del contacto", "El peligro se retira después del primer contacto.");
                        break;
                    case ItemKind.Goal:
                        definition.learningHint = "Llega aquí para completar el recorrido. El mensaje celebra el final de tu juego.";
                        Set(definition, nameof(GameItem.message), "Resultado", "Mensaje al llegar", "Escribe un mensaje de hasta 120 caracteres.");
                        break;
                }
                EditorUtility.SetDirty(definition);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("CREAJUEGO_EDUCATIONAL_METADATA_V1_READY");
        }

        private static void Set(GameItemDefinition d, string path, string group, string label, string help, string unit = "")
        {
            var property = d.properties.Single(p => p.path == path);
            property.group = group; property.label = label; property.help = help; property.unit = unit;
        }

        private static Sprite MakeIcon(GameItemDefinition d)
        {
            string directory = DemoBuilder.Root + "/Content/Icons";
            Directory.CreateDirectory(directory);
            string path = directory + "/" + d.id + ".png";
            if (!File.Exists(path))
            {
                var image = new Texture2D(32, 32, TextureFormat.RGBA32, false);
                var pixels = new Color[1024];
                Color color = d.prefab.GetComponent<GameItem>().tint;
                for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
                {
                    float dx = x - 15.5f;
                    bool inside = d.kind switch
                    {
                        ItemKind.Player => (dx * dx + (y - 24) * (y - 24) < 25) || (Mathf.Abs(dx) < 7 && y > 3 && y < 18),
                        ItemKind.Platform => x > 2 && x < 29 && y > 10 && y < 20,
                        ItemKind.Prize => Mathf.Abs(dx) + Mathf.Abs(y - 15.5f) < 13,
                        ItemKind.Hazard => y > 3 && y < 28 && Mathf.Abs(dx) < (28 - y) * .5f,
                        ItemKind.Goal => (x > 5 && x < 9 && y > 2 && y < 29) || (x >= 9 && x < 27 && y > 16 && y < 28),
                        _ => dx * dx + (y - 15.5f) * (y - 15.5f) < 130
                    };
                    pixels[y * 32 + x] = inside ? color : Color.clear;
                }
                image.SetPixels(pixels); image.Apply(); File.WriteAllBytes(path, image.EncodeToPNG()); Object.DestroyImmediate(image);
                AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite; importer.spritePixelsPerUnit = 32; importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
