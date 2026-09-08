using System;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace CreaJuego.Editor
{
    public sealed class CreaJuegoWindow : EditorWindow
    {
        private ScrollView catalog, properties;
        private Label status;
        private Button play;
        private SerializedObject binding;

        [MenuItem("CreaJuego/Abrir taller")]
        public static void Open() => GetWindow<CreaJuegoWindow>("CreaJuego Lab");

        private void OnEnable()
        {
            Selection.selectionChanged += ShowSelection;
            Undo.undoRedoPerformed += ShowSelection;
            EditorApplication.playModeStateChanged += PlayChanged;
            EditorApplication.projectChanged += RefreshCatalog;
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= ShowSelection;
            Undo.undoRedoPerformed -= ShowSelection;
            EditorApplication.playModeStateChanged -= PlayChanged;
            EditorApplication.projectChanged -= RefreshCatalog;
            properties?.Unbind();
            binding?.Dispose();
            binding = null;
        }

        public void CreateGUI()
        {
            minSize = new Vector2(610, 440);
            var root = rootVisualElement;
            root.Clear();
            root.style.backgroundColor = new Color(.065f, .085f, .12f);
            root.style.paddingLeft = root.style.paddingRight = 16;
            root.style.paddingTop = root.style.paddingBottom = 14;
            var header = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 12 } };
            var title = new Label("CreaJuego  /  LAB") { style = { fontSize = 23, unityFontStyleAndWeight = FontStyle.Bold, flexGrow = 1 } };
            header.Add(title);
            play = new Button(TogglePlay) { text = "▶ PROBAR", name = "probar", style = { height = 36, width = 130, backgroundColor = new Color(.12f,.48f,.4f) } };
            header.Add(play);
            root.Add(header);
            root.Add(new Label("Primero crea. Luego descubre cómo funciona.") { style = { color = new Color(.6f,.75f,.78f), marginBottom = 16 } });
            var columns = new VisualElement { style = { flexDirection = FlexDirection.Row, flexGrow = 1 } };
            catalog = new ScrollView { name = "catalogo", style = { width = Length.Percent(48), marginRight = 16 } };
            properties = new ScrollView { name = "propiedades", style = { flexGrow = 1 } };
            columns.Add(catalog); columns.Add(properties); root.Add(columns);
            status = new Label("Añade un elemento y colócalo en la vista Escena de Unity.") { style = { whiteSpace = WhiteSpace.Normal, marginTop = 14, color = new Color(.7f,.8f,.85f) } };
            root.Add(status);
            RefreshCatalog(); ShowSelection(); PlayChanged(default);
        }

        private void RefreshCatalog()
        {
            if (catalog == null) return;
            catalog.Clear();
            var definitions = ItemService.WorkshopCatalog();
            if (definitions.Length == 0) catalog.Add(new HelpBox("Prepara el pack desde CreaJuego > Preparar demo.", HelpBoxMessageType.Info));
            foreach (var group in definitions.GroupBy(d => d.category))
            {
                catalog.Add(new Label(group.Key.ToUpperInvariant()) { style = { marginTop = 12, marginBottom = 6, unityFontStyleAndWeight = FontStyle.Bold } });
                foreach (var definition in group)
                {
                    var button = new Button(() => CreateItem(definition)) { name = "crear-" + definition.id, tooltip = definition.description, style = { minHeight = 76, marginBottom = 8, flexDirection = FlexDirection.Row, alignItems = Align.Center, paddingLeft = 10, paddingRight = 10 } };
                    button.Add(Icon(definition));
                    var text = new VisualElement { style = { flexGrow = 1, flexShrink = 1 } };
                    text.Add(new Label("+  " + definition.displayName) { style = { fontSize = 15 } });
                    text.Add(new Label(definition.description) { style = { fontSize = 12, whiteSpace = WhiteSpace.Normal, color = new Color(.65f,.72f,.8f) } });
                    button.Add(text);
                    catalog.Add(button);
                }
            }
            catalog.SetEnabled(!EditorApplication.isPlayingOrWillChangePlaymode);
        }

        private static VisualElement Icon(GameItemDefinition definition) => new Image
        {
            sprite = definition.icon, name = "icono-" + definition.id,
            style = { width = 32, height = 32, marginRight = 10, flexShrink = 0 }
        };

        private void CreateItem(GameItemDefinition definition)
        {
            try
            {
                var point = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.pivot : Vector3.zero;
                point.z = 0;
                var item = ItemService.Create(definition, point);
                EditorGUIUtility.PingObject(item.gameObject);
                status.text = definition.learningHint;
            }
            catch (Exception error) { status.text = error.Message; }
        }

        private void ShowSelection()
        {
            if (properties == null) return;
            properties.Unbind(); properties.Clear(); binding?.Dispose(); binding = null;
            properties.Add(new Label("TU ELEMENTO") { style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 12 } });
            var items = Selection.gameObjects.Select(g => g.GetComponent<GameItem>()).ToArray();
            if (items.Length == 0 || items.Any(i => i == null || i.definition == null || EditorUtility.IsPersistent(i)))
            {
                properties.Add(new HelpBox("Elige un elemento CreaJuego en la escena para configurarlo.", HelpBoxMessageType.Info)); return;
            }
            var definition = items[0].definition;
            if (items.Any(i => i.definition != definition))
            {
                properties.Add(new HelpBox("Para cambiar varios a la vez, elige elementos del mismo tipo.", HelpBoxMessageType.Info)); return;
            }
            var heading = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 8 } };
            heading.Add(Icon(definition));
            heading.Add(new Label(definition.displayName + (items.Length > 1 ? " × " + items.Length : "")) { style = { fontSize = 20 } });
            properties.Add(heading);
            properties.Add(new Label(definition.learningHint) { style = { whiteSpace = WhiteSpace.Normal, marginBottom = 16 } });
            var actions = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 16 } };
            var duplicate = new Button(() => RunAction(() => ItemService.Duplicate(items[0]))) { text = "Duplicar", name = "duplicar", tooltip = definition.allowMultiple ? "Crea una copia con los mismos ajustes." : "Este taller usa un solo personaje." };
            duplicate.SetEnabled(items.Length == 1 && definition.allowMultiple);
            var delete = new Button(() => RunAction(() => ItemService.Delete(items[0]))) { text = "Eliminar", name = "eliminar", tooltip = "Puedes recuperarlo con Ctrl+Z." };
            delete.SetEnabled(items.Length == 1);
            actions.Add(duplicate); actions.Add(delete); properties.Add(actions);
            binding = new SerializedObject(items.Cast<UnityEngine.Object>().ToArray());
            string lastGroup = null;
            foreach (var descriptor in definition.properties)
            {
                var p = binding.FindProperty(descriptor.path);
                if (p == null) { properties.Add(new HelpBox("Propiedad no disponible: " + descriptor.label, HelpBoxMessageType.Warning)); continue; }
                if (!string.IsNullOrEmpty(descriptor.group) && lastGroup != descriptor.group)
                {
                    properties.Add(new Label(descriptor.group.ToUpperInvariant()) { style = { unityFontStyleAndWeight = FontStyle.Bold, marginTop = 8, marginBottom = 10 } });
                    lastGroup = descriptor.group;
                }
                var row = new VisualElement { name = "fila-" + descriptor.path, style = { marginBottom = 16 } };
                VisualElement field;
                switch (descriptor.control)
                {
                    case EducationalControl.Float:
                        var slider = new Slider(descriptor.label, descriptor.minimum, descriptor.maximum) { showInputField = true };
                        slider.BindProperty(p); field = slider; break;
                    case EducationalControl.Integer:
                        var integer = new IntegerField(descriptor.label);
                        integer.RegisterValueChangedCallback(evt =>
                        {
                            int bounded = Mathf.Clamp(evt.newValue, (int)descriptor.minimum, (int)descriptor.maximum);
                            if (bounded != evt.newValue) { evt.StopImmediatePropagation(); integer.value = bounded; }
                        });
                        integer.BindProperty(p); field = integer; break;
                    case EducationalControl.Toggle:
                        var toggle = new Toggle(descriptor.label); toggle.BindProperty(p); field = toggle; break;
                    case EducationalControl.Text:
                        var text = new TextField(descriptor.label) { multiline = true, maxLength = 120 };
                        text.BindProperty(p); field = text; break;
                    default:
                        var color = new ColorField(descriptor.label) { showAlpha = false }; color.BindProperty(p); field = color; break;
                }
                field.name = "propiedad-" + descriptor.path;
                field.tooltip = descriptor.help;
                row.Add(field);
                if (!string.IsNullOrEmpty(descriptor.unit)) row.Add(new Label(descriptor.unit) { style = { fontSize = 11 } });
                row.Add(new Label(descriptor.help) { style = { whiteSpace = WhiteSpace.Normal, fontSize = 12, color = new Color(.65f,.72f,.8f), marginTop = 4 } });
                if (!string.IsNullOrEmpty(descriptor.visibleWhen))
                {
                    var condition = binding.FindProperty(descriptor.visibleWhen);
                    if (condition != null && condition.propertyType == SerializedPropertyType.Boolean)
                    {
                        void Visibility(SerializedProperty value) => row.style.display = value.hasMultipleDifferentValues || value.boolValue ? DisplayStyle.Flex : DisplayStyle.None;
                        Visibility(condition); row.TrackPropertyValue(condition, Visibility);
                    }
                }
                properties.Add(row);
            }
            properties.SetEnabled(!EditorApplication.isPlayingOrWillChangePlaymode);
        }

        private void RunAction(System.Action action)
        {
            try { action(); status.text = "Ctrl+Z deshace el cambio. Ctrl+Y lo rehace."; }
            catch (Exception error) { status.text = error.Message; }
        }

        private void TogglePlay()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) EditorApplication.ExitPlaymode();
            else EditorApplication.EnterPlaymode();
        }

        private void PlayChanged(PlayModeStateChange state)
        {
            if (play == null) return;
            bool playing = EditorApplication.isPlayingOrWillChangePlaymode;
            play.text = playing ? "■ DETENER" : "▶ PROBAR";
            catalog.SetEnabled(!playing); properties.SetEnabled(!playing);
            status.text = playing ? "Haz clic en Juego. Flechas: moverte · Espacio: saltar. Detén la prueba para seguir creando." : "Los cambios se hacen antes de probar. Ctrl+Z deshace tu último cambio.";
        }
    }
}
