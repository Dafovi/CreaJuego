using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace CreaJuego.Web
{
    [RequireComponent(typeof(RuntimeAuthoringController))]
    public sealed class RuntimeAuthoringInput:MonoBehaviour
    {
        RuntimeAuthoringController controller;Camera View=>controller.buildCamera;
        bool dragging,panning,resizing,resizeRight,changed;Vector3 offset,last;float fixedEdge;
        LineRenderer outline,worldFrame;SpriteRenderer leftHandle,rightHandle;Material lineMaterial;Sprite handleSprite;
        void Awake(){controller=GetComponent<RuntimeAuthoringController>();controller.Selection.SelectionChanged+=Selected;controller.ProjectChanged+=RefreshWorld;}
        void Start(){lineMaterial=new Material(Shader.Find("Sprites/Default"));handleSprite=CreateHandleSprite();worldFrame=Line("Zona del nivel",new Color(.2f,.75f,1,.65f),.045f,50);RefreshWorld();}
        void OnDestroy(){controller.Selection.SelectionChanged-=Selected;controller.ProjectChanged-=RefreshWorld;if(lineMaterial!=null)Destroy(lineMaterial);if(handleSprite!=null){var texture=handleSprite.texture;Destroy(handleSprite);Destroy(texture);}}
        void Update()
        {
            bool build=controller.Mode==AuthoringMode.Build;if(worldFrame!=null)worldFrame.gameObject.SetActive(build);SetSelectionVisible(build);if(!build||Mouse.current==null||View==null)return;
            var mouse=Mouse.current;var screen=mouse.position.ReadValue();var world=View.ScreenToWorldPoint(new Vector3(screen.x,screen.y,-View.transform.position.z));world.z=0;bool overUI=EventSystem.current!=null&&EventSystem.current.IsPointerOverGameObject();
            if(mouse.leftButton.wasPressedThisFrame&&!overUI)
            {
                if(TryBeginResize(screen)){changed=false;return;}
                var hit=Physics2D.OverlapPoint(world);var item=controller.Selection.Select(hit!=null?hit.gameObject:null);
                if(item!=null){dragging=true;changed=false;offset=item.transform.position-world;last=item.transform.position;}else{panning=true;last=world;}
            }
            if(mouse.leftButton.isPressed&&resizing)Resize(world.x);
            else if(mouse.leftButton.isPressed&&dragging){var before=controller.Selection.SelectedItem.transform.position;controller.MoveSelected(world+offset,false);changed|=(before-controller.Selection.SelectedItem.transform.position).sqrMagnitude>.0001f;}
            else if(mouse.leftButton.isPressed&&panning){var delta=last-world;View.transform.position+=delta;last=View.ScreenToWorldPoint(new Vector3(screen.x,screen.y,-View.transform.position.z));last.z=0;}
            if(mouse.leftButton.wasReleasedThisFrame){if((dragging||resizing)&&changed)controller.CommitEdit();dragging=panning=resizing=changed=false;}
            var wheel=mouse.scroll.ReadValue().y;if(Mathf.Abs(wheel)>.1f)View.orthographicSize=Mathf.Clamp(View.orthographicSize-wheel*.005f,2,30);
            var keys=Keyboard.current;if(keys!=null&&(keys.leftCtrlKey.isPressed||keys.rightCtrlKey.isPressed)&&keys.zKey.wasPressedThisFrame){if(keys.leftShiftKey.isPressed||keys.rightShiftKey.isPressed)controller.Redo();else controller.Undo();}
            if(keys!=null&&(keys.leftCtrlKey.isPressed||keys.rightCtrlKey.isPressed)&&keys.yKey.wasPressedThisFrame)controller.Redo();UpdateSelectionVisuals();
        }
        bool TryBeginResize(Vector2 screen)
        {
            var item=controller.Selection.SelectedItem;if(item==null||item.definition.kind!=ItemKind.Platform||leftHandle==null)return false;
            float left=Vector2.Distance(screen,View.WorldToScreenPoint(leftHandle.transform.position)),right=Vector2.Distance(screen,View.WorldToScreenPoint(rightHandle.transform.position));if(Mathf.Min(left,right)>24)return false;
            resizeRight=right<left;var bounds=item.GetComponent<BoxCollider2D>().bounds;fixedEdge=resizeRight?bounds.min.x:bounds.max.x;resizing=true;return true;
        }
        void Resize(float movingEdge)
        {
            var width=Mathf.Abs(movingEdge-fixedEdge);var snapped=RuntimeSnap.Width(width,controller.Project.alignAutomatically);var center=fixedEdge+(resizeRight?snapped*.5f:-snapped*.5f);var data=controller.SelectedData();var before=data.platformWidth;controller.ResizeSelected(snapped,center,false);changed|=Mathf.Abs(before-data.platformWidth)>.001f;UpdateSelectionVisuals();
        }
        void Selected(GameItem item)
        {
            DestroySelection();if(item==null)return;outline=Line("Selección",new Color(1,.85f,.1f),.06f,100);
            if(item.definition!=null&&item.definition.kind==ItemKind.Platform){leftHandle=Handle("Ancho izquierdo");rightHandle=Handle("Ancho derecho");}UpdateSelectionVisuals();
        }
        void UpdateSelectionVisuals()
        {
            var item=controller.Selection.SelectedItem;if(item==null||outline==null)return;var bounds=ItemBounds(item);var z=item.transform.position.z-.1f;outline.SetPositions(new[]{new Vector3(bounds.min.x,bounds.min.y,z),new Vector3(bounds.min.x,bounds.max.y,z),new Vector3(bounds.max.x,bounds.max.y,z),new Vector3(bounds.max.x,bounds.min.y,z),new Vector3(bounds.min.x,bounds.min.y,z)});
            if(leftHandle!=null){float size=Mathf.Clamp(View.orthographicSize*.045f,.18f,.65f);leftHandle.transform.position=new Vector3(bounds.min.x,bounds.center.y,z-.01f);rightHandle.transform.position=new Vector3(bounds.max.x,bounds.center.y,z-.01f);leftHandle.transform.localScale=rightHandle.transform.localScale=Vector3.one*size;}
        }
        void RefreshWorld()
        {
            if(worldFrame==null||controller.Project?.bounds==null)return;var b=controller.Project.bounds;worldFrame.SetPositions(new[]{new Vector3(b.left,b.bottom,5),new Vector3(b.left,b.top,5),new Vector3(b.right,b.top,5),new Vector3(b.right,b.bottom,5),new Vector3(b.left,b.bottom,5)});UpdateSelectionVisuals();
        }
        void SetSelectionVisible(bool visible){if(outline!=null)outline.gameObject.SetActive(visible);if(leftHandle!=null){leftHandle.gameObject.SetActive(visible);rightHandle.gameObject.SetActive(visible);}}
        void DestroySelection(){if(outline!=null)Destroy(outline.gameObject);if(leftHandle!=null)Destroy(leftHandle.gameObject);if(rightHandle!=null)Destroy(rightHandle.gameObject);outline=null;leftHandle=rightHandle=null;}
        LineRenderer Line(string name,Color color,float width,int order){var go=new GameObject(name);go.transform.SetParent(transform,false);var line=go.AddComponent<LineRenderer>();line.material=lineMaterial;line.startColor=line.endColor=color;line.startWidth=line.endWidth=width;line.positionCount=5;line.loop=false;line.sortingOrder=order;return line;}
        SpriteRenderer Handle(string name){var go=new GameObject(name,typeof(RuntimeResizeHandle),typeof(SpriteRenderer));go.transform.SetParent(transform,false);var renderer=go.GetComponent<SpriteRenderer>();renderer.sprite=handleSprite;renderer.color=new Color(1,.85f,.1f);renderer.sortingOrder=101;return renderer;}
        static Sprite CreateHandleSprite(){var texture=new Texture2D(8,8,TextureFormat.RGBA32,false);var pixels=new Color[64];for(int i=0;i<pixels.Length;i++)pixels[i]=Color.white;texture.SetPixels(pixels);texture.Apply();return Sprite.Create(texture,new Rect(0,0,8,8),new Vector2(.5f,.5f),8);}
        static Bounds ItemBounds(GameItem item){var collider=item.GetComponent<Collider2D>();if(collider!=null)return collider.bounds;var renderer=ItemVisual.Resolve(item);return renderer!=null?renderer.bounds:new Bounds(item.transform.position,Vector3.one);}
    }
    public sealed class RuntimeResizeHandle:MonoBehaviour{}
}