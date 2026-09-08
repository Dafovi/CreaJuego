using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
namespace CreaJuego.Editor
{
    public sealed class CreaJuegoPlayBarWindow : EditorWindow
    {
        private ScrollView validation;
        private VisualElement footer;
        private Button play,prepare;
        private Label status;
        [MenuItem("CreaJuego/Abrir barra de juego")]
        public static void Open()=>GetWindow<CreaJuegoPlayBarWindow>("Jugar");
        private static T Styled<T>(T element,string style) where T:VisualElement=>WorkshopWindowStyle.Styled(element,style);
        private void OnEnable() {
            Undo.undoRedoPerformed+=RefreshPreflight;EditorApplication.hierarchyChanged+=RefreshPreflight;
            EditorApplication.projectChanged+=RefreshPreflight;
            EditorSceneManager.activeSceneChangedInEditMode+=SceneChanged;
            EditorApplication.playModeStateChanged+=PlayChanged;ObjectChangeEvents.changesPublished+=ObjectsChanged;
        }
        private void OnDisable() {
            Undo.undoRedoPerformed-=RefreshPreflight;EditorApplication.hierarchyChanged-=RefreshPreflight;
            EditorApplication.projectChanged-=RefreshPreflight;
            EditorSceneManager.activeSceneChangedInEditMode-=SceneChanged;
            EditorApplication.playModeStateChanged-=PlayChanged;ObjectChangeEvents.changesPublished-=ObjectsChanged;
        }
        private void SceneChanged(Scene a,Scene b)=>RefreshPreflight();
        private void ObjectsChanged(ref ObjectChangeEventStream stream)=>RefreshPreflight();
        public void CreateGUI() {
            minSize=new Vector2(420,160);WorkshopWindowStyle.Apply(this);
            rootVisualElement.AddToClassList("playbar-window");
            footer=Styled(new VisualElement {name="juego-estado"},"play-footer");
            validation=Styled(new ScrollView {name="validacion"},"validation");
            play=Styled(new Button(TogglePlay){name="jugar",text="▶ JUGAR"},"primary");
            footer.Add(validation);footer.Add(play);rootVisualElement.Add(footer);
            prepare=new Button(PrepareScene){name="preparar-escena",text="Preparar escena"};
            rootVisualElement.Add(prepare);
            status=Styled(new Label(){name="estado"},"status");rootVisualElement.Add(status);
            status.schedule.Execute(()=>{if(EditorApplication.isPlaying)status.text=SceneService.RuntimeHelp();}).Every(250);
            PlayChanged(default);
        }        private void RefreshPreflight()
        {
            if(validation==null || EditorApplication.isPlayingOrWillChangePlaymode) return;
            ShowChecks(SceneService.Validate());
        }
        private void ShowChecks(System.Collections.Generic.List<SceneCheck> checks)
        {
            var view=WorkshopPresentation.Describe(checks);
            validation.Clear(); validation.style.display=DisplayStyle.Flex;
            footer.EnableInClassList("ready",view.ready); footer.EnableInClassList("needs-help",!view.ready);
            validation.Add(Styled(new Label(view.title){name="preflight-title"},"preflight-title"));
            validation.Add(Styled(new Label(view.help){name="preflight-help"},"hint"));
            var badges=Styled(new VisualElement(),"check-list");
            foreach(var check in view.checks) {
                string text=check.passed ? "✓ "+check.text : check.optional ? "+ "+check.text+" (opcional)" : "• "+check.text;
                badges.Add(Styled(new Label(text),check.passed?"check-ok":"check-missing"));
            }
            validation.Add(badges);
        }
        private void PrepareScene()
        {
            try { SceneService.Prepare(); var checks=SceneService.Validate(); ShowChecks(checks); status.text="Escena preparada. Añade tu personaje y el recorrido."; }
            catch(Exception error){status.text=error.Message;}
        }
        private void TogglePlay()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.ExitPlaymode();return;}
            if(!SceneService.TryPlay(out var checks)){ShowChecks(checks);status.text=WorkshopPresentation.Describe(checks).help;}
            else validation.style.display=DisplayStyle.None;
        }
        private void PlayChanged(PlayModeStateChange state)
        {
            if(play==null)return;
            bool playing=EditorApplication.isPlayingOrWillChangePlaymode;
            play.text=playing?"■ DETENER":"▶ JUGAR";
            prepare.SetEnabled(!playing);

            if(playing) {
                validation.Clear();
                validation.Add(Styled(new Label("Tu juego está en marcha"),"preflight-title"));
                validation.Add(Styled(new Label("A/D o flechas para moverte · Espacio para saltar"),"hint"));
                validation.style.display=DisplayStyle.Flex;
            } else RefreshPreflight();
            status.text=playing?"Haz clic en Juego para jugar tu recorrido.":"Los cambios se hacen antes de jugar. Ctrl+Z deshace tu último cambio.";
        }
    }
}

