using UnityEditor;
using UnityEngine;
namespace CreaJuego.Editor
{
    [InitializeOnLoad]
    public static class WorkshopSceneGuides
    {
        static WorkshopSceneGuides()=>SceneView.duringSceneGui+=DrawFrame;
        private static void DrawFrame(SceneView view)
        {
            if(Event.current.type!=EventType.Repaint) return;
            var camera=WorldAuthoringService.OutputCamera();
            if(camera==null || !camera.orthographic) return;
            if(SceneObjects.All<GameItem>(camera.gameObject.scene).Length==0) return;
            float depth=-camera.transform.position.z/camera.transform.forward.z;
            if(!float.IsFinite(depth) || depth<=0) return;
            var corners=new Vector3[4];
            var uv=new[]{new Vector2(0,0),new Vector2(0,1),new Vector2(1,1),new Vector2(1,0)};
            for(int i=0;i<4;i++) corners[i]=camera.ViewportToWorldPoint(new Vector3(uv[i].x,uv[i].y,depth));
            Handles.color=new Color(.3f,.75f,1,.9f);
            Handles.DrawAAPolyLine(3,corners[0],corners[1],corners[2],corners[3],corners[0]);
            if(!view.in2DMode) return;
            Vector2 a=HandleUtility.WorldToGUIPoint(corners[1]),b=HandleUtility.WorldToGUIPoint(corners[3]);
            float width=view.position.width,height=view.position.height;
            float left=Mathf.Clamp(Mathf.Min(a.x,b.x),0,width),right=Mathf.Clamp(Mathf.Max(a.x,b.x),0,width);
            float top=Mathf.Clamp(Mathf.Min(a.y,b.y),0,height),bottom=Mathf.Clamp(Mathf.Max(a.y,b.y),0,height);
            Handles.BeginGUI();
            var dim=new Color(0,0,0,.38f);
            EditorGUI.DrawRect(new Rect(0,0,width,top),dim);
            EditorGUI.DrawRect(new Rect(0,bottom,width,height-bottom),dim);
            EditorGUI.DrawRect(new Rect(0,top,left,bottom-top),dim);
            EditorGUI.DrawRect(new Rect(right,top,width-right,bottom-top),dim);
            GUI.Label(new Rect(left+8,top+8,260,22),"Encuadre del juego");
            Handles.EndGUI();
        }
        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
        private static void DrawBoundary(InvisibleBoundary boundary,GizmoType type)
        {
            var box=boundary.GetComponent<BoxCollider2D>();
            if(box==null) return;
            var previous=Gizmos.matrix;
            Gizmos.matrix=boundary.transform.localToWorldMatrix;
            Gizmos.color=new Color(1,.3f,.65f,.15f);
            Gizmos.DrawCube(box.offset,box.size);
            Gizmos.color=new Color(1,.3f,.65f,.9f);
            Gizmos.DrawWireCube(box.offset,box.size);
            Gizmos.matrix=previous;
            Handles.Label(boundary.transform.position,"Límite invisible");
        }
    }
}
