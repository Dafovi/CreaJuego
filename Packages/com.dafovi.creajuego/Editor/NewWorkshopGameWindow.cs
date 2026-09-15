using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace CreaJuego.Editor
{
    public sealed class NewWorkshopGameWindow : EditorWindow
    {
        private TextField gameName, teamName;
        private Label message;

        [MenuItem("CreaJuego/Nuevo juego", false, 1)]
        public static void Open()
        {
            var window = GetWindow<NewWorkshopGameWindow>(true, "Nuevo juego");
            window.minSize = new Vector2(380, 230);
            window.maxSize = new Vector2(540, 320);
            window.Show();
        }

        public void CreateGUI()
        {
            WorkshopWindowStyle.Apply(this);
            rootVisualElement.Add(WorkshopWindowStyle.Styled(new Label("CREAR UN JUEGO NUEVO"), "section-title"));
            rootVisualElement.Add(new Label("CreaJuego guardará una escena y una carpeta de imágenes para este equipo.") { style = { whiteSpace = WhiteSpace.Normal } });
            gameName = new TextField("Nombre del juego") { name = "nombre-juego" };
            teamName = new TextField("Equipo o grupo") { name = "nombre-equipo" };
            rootVisualElement.Add(gameName);
            rootVisualElement.Add(teamName);
            rootVisualElement.Add(new Button(Create) { text = "Crear juego", name = "crear-juego" });
            message = new Label { style = { whiteSpace = WhiteSpace.Normal } };
            rootVisualElement.Add(message);
        }

        private void Create()
        {
            try
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) { message.text = "No se creó un juego nuevo."; return; }
                var path = WorkshopGameService.CreateNew(gameName.value, teamName.value);
                CreaJuegoWindow.Open();
                Debug.Log("Juego creado en " + path);
                Close();
            }
            catch (Exception error) { message.text = error.Message; }
        }
    }
}
