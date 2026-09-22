using UnityEngine;
using UnityEngine.UI;

namespace CreaJuego.Web
{
    [DisallowMultipleComponent]
    public sealed class RuntimeCanvasBackground:MonoBehaviour
    {
        Camera targetCamera;
        Canvas canvas;
        Image image;
        GameItem item;

        public Camera TargetCamera=>targetCamera;
        public Image Image=>image;

        public void Configure(Camera target)
        {
            targetCamera=target;
            EnsureVisual();
            Refresh();
        }

        void LateUpdate()=>Refresh();

        void EnsureVisual()
        {
            if(canvas!=null&&image!=null)return;
            var canvasObject=new GameObject("Lienzo del fondo",typeof(RectTransform),typeof(Canvas));
            canvasObject.transform.SetParent(transform,false);
            canvas=canvasObject.GetComponent<Canvas>();
            canvas.renderMode=RenderMode.ScreenSpaceCamera;
            canvas.overrideSorting=true;
            canvas.sortingOrder=-32000;

            var imageObject=new GameObject("Imagen del fondo",typeof(RectTransform),typeof(Image));
            imageObject.transform.SetParent(canvasObject.transform,false);
            image=imageObject.GetComponent<Image>();
            image.raycastTarget=false;
            image.preserveAspect=false;
            var rect=(RectTransform)imageObject.transform;
            rect.anchorMin=Vector2.zero;
            rect.anchorMax=Vector2.one;
            rect.offsetMin=rect.offsetMax=Vector2.zero;
        }

        public void Refresh()
        {
            if(targetCamera==null)return;
            EnsureVisual();
            if(item==null)item=GetComponent<GameItem>();
            image.sprite=item!=null ? item.customSprite ?? item.SelectedAppearance?.sprite ?? item.definition?.icon : null;
            image.color=item!=null?ItemVisual.BaseColor(item):Color.white;
            canvas.worldCamera=targetCamera;
            canvas.planeDistance=Mathf.Clamp(targetCamera.farClipPlane-1,targetCamera.nearClipPlane+.01f,targetCamera.farClipPlane-.01f);
        }
    }
}
