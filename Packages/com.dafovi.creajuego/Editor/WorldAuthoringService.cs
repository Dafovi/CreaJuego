using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;
namespace CreaJuego.Editor
{
    public static class WorldAuthoringService
    {
        private static Scene EditableScene()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || PrefabStageUtility.GetCurrentPrefabStage()!=null || SceneManager.sceneCount!=1)
                throw new InvalidOperationException("Detén el juego y abre una sola escena de taller.");
            return SceneManager.GetActiveScene();
        }
        public static Camera OutputCamera()=>SceneObjects.All<Camera>(SceneManager.GetActiveScene()).FirstOrDefault(c=>c.isActiveAndEnabled);
        public static WorkshopBackground Background()=>SceneObjects.All<WorkshopBackground>(SceneManager.GetActiveScene()).FirstOrDefault();
        public static WorkshopBackground SetBackground(Sprite sprite)
        {
            var scene=EditableScene();
            var backgrounds=SceneObjects.All<WorkshopBackground>(scene);
            if(backgrounds.Length>1) throw new InvalidOperationException("Hay varios fondos. Conserva uno antes de continuar.");
            var background=backgrounds.FirstOrDefault();
            if(sprite==null) {
                if(background!=null) Undo.DestroyObjectImmediate(background.gameObject);
                return null;
            }
            var camera=OutputCamera();
            if(camera==null) throw new InvalidOperationException("Prepara la escena antes de añadir un fondo.");
            Undo.IncrementCurrentGroup(); int group=Undo.GetCurrentGroup();
            if(background==null) {
                var go=new GameObject("Fondo",typeof(SpriteRenderer),typeof(WorkshopBackground));
                SceneManager.MoveGameObjectToScene(go,scene);
                background=go.GetComponent<WorkshopBackground>();
                Undo.RegisterCreatedObjectUndo(go,"Añadir fondo");
            }
            Undo.RecordObjects(new UnityEngine.Object[]{background,background.transform,background.GetComponent<SpriteRenderer>()},"Cambiar fondo");
            background.output=camera;
            background.GetComponent<SpriteRenderer>().sprite=sprite;
            background.Fit();
            foreach(var obj in new UnityEngine.Object[]{background,background.transform,background.GetComponent<SpriteRenderer>()})
                PrefabUtility.RecordPrefabInstancePropertyModifications(obj);
            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(group);
            return background;
        }
        public static InvisibleBoundary AddBoundary(Vector3 position)
        {
            var scene=EditableScene();
            var go=new GameObject("Límite invisible",typeof(BoxCollider2D),typeof(InvisibleBoundary));
            SceneManager.MoveGameObjectToScene(go,scene);
            position.z=0; go.transform.position=position;
            go.GetComponent<BoxCollider2D>().size=new Vector2(.5f,20);
            Undo.RegisterCreatedObjectUndo(go,"Añadir límite invisible");
            Selection.activeGameObject=go;
            EditorSceneManager.MarkSceneDirty(scene);
            return go.GetComponent<InvisibleBoundary>();
        }
        public static void EnsureCamera()
        {
            var scene=EditableScene();
            var output=OutputCamera();
            if(output==null) return;
            Undo.IncrementCurrentGroup(); int group=Undo.GetCurrentGroup();
            var rig=SceneObjects.All<WorkshopCameraRig>(scene).FirstOrDefault();
            if(rig==null) {
                // Respect camera work authored outside CreaJuego.
                if(SceneObjects.All<CinemachineVirtualCameraBase>(scene).Length>0) return;

                if(output.GetComponent<CinemachineBrain>()==null) Undo.AddComponent<CinemachineBrain>(output.gameObject);
                var go=new GameObject("Seguimiento del personaje",typeof(CinemachineCamera),typeof(CinemachinePositionComposer),typeof(WorkshopCameraRig));
                SceneManager.MoveGameObjectToScene(go,scene);
                go.transform.SetPositionAndRotation(output.transform.position,output.transform.rotation);
                rig=go.GetComponent<WorkshopCameraRig>(); rig.output=output;
                rig.cameraController=go.GetComponent<CinemachineCamera>();
                rig.cameraController.Lens=LensSettings.FromCamera(output);
                var composer=go.GetComponent<CinemachinePositionComposer>();
                composer.CameraDistance=Mathf.Abs(output.transform.position.z);
                composer.Damping=new Vector3(.3f,.3f,0);
                Undo.RegisterCreatedObjectUndo(go,"Preparar cámara que sigue al personaje");
                Undo.CollapseUndoOperations(group);
            }
            if(rig.cameraController==null) return;
            Undo.RecordObject(rig.cameraController,"Seguir al personaje");
            rig.ResolvePlayer();
            Undo.CollapseUndoOperations(group);
            PrefabUtility.RecordPrefabInstancePropertyModifications(rig.cameraController);
            EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
