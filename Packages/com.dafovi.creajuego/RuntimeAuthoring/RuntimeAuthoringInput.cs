using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
namespace CreaJuego.Web
{
    [RequireComponent(typeof(RuntimeAuthoringController))]
    public sealed class RuntimeAuthoringInput:MonoBehaviour
    {
        RuntimeAuthoringController controller; Camera View=>controller.buildCamera; bool dragging,panning;Vector3 offset,last;LineRenderer outline;
        void Awake(){controller=GetComponent<RuntimeAuthoringController>();controller.Selection.SelectionChanged+=Selected;}
        void OnDestroy(){controller.Selection.SelectionChanged-=Selected;}
        void Update()
        {
            if(controller.Mode!=AuthoringMode.Build||Mouse.current==null||View==null)return;
            var mouse=Mouse.current;var screen=mouse.position.ReadValue();var world=View.ScreenToWorldPoint(new Vector3(screen.x,screen.y,-View.transform.position.z));world.z=0;
            bool overUI=EventSystem.current!=null&&EventSystem.current.IsPointerOverGameObject();
            if(mouse.leftButton.wasPressedThisFrame&&!overUI)
            {
                var hit=Physics2D.OverlapPoint(world);var item=controller.Selection.Select(hit!=null?hit.gameObject:null);
                if(item!=null){dragging=true;offset=item.transform.position-world;last=item.transform.position;}else{panning=true;last=world;}
            }
            if(mouse.leftButton.isPressed&&dragging)controller.MoveSelected(world+offset,false);
            if(mouse.leftButton.isPressed&&panning){var delta=last-world;View.transform.position+=delta;last=View.ScreenToWorldPoint(new Vector3(screen.x,screen.y,-View.transform.position.z));last.z=0;}
            if(mouse.leftButton.wasReleasedThisFrame){if(dragging)controller.MoveSelected(controller.Selection.SelectedItem.transform.position,true);dragging=panning=false;}
            var wheel=mouse.scroll.ReadValue().y;if(Mathf.Abs(wheel)>.1f)View.orthographicSize=Mathf.Clamp(View.orthographicSize-wheel*.005f,2,20);
            var keys=Keyboard.current;if(keys!=null&&(keys.leftCtrlKey.isPressed||keys.rightCtrlKey.isPressed)&&keys.zKey.wasPressedThisFrame){if(keys.leftShiftKey.isPressed||keys.rightShiftKey.isPressed)controller.Redo();else controller.Undo();}
            if(keys!=null&&(keys.leftCtrlKey.isPressed||keys.rightCtrlKey.isPressed)&&keys.yKey.wasPressedThisFrame)controller.Redo();
            if(outline!=null&&controller.Selection.SelectedItem!=null)UpdateOutline(controller.Selection.SelectedItem);
        }
        void Selected(GameItem item)
        {
            if(outline!=null)Destroy(outline.gameObject);if(item==null)return;var go=new GameObject("Selección visual");outline=go.AddComponent<LineRenderer>();outline.material=new Material(Shader.Find("Sprites/Default"));outline.startColor=outline.endColor=new Color(1,.85f,.1f);outline.startWidth=outline.endWidth=.06f;outline.positionCount=5;outline.loop=false;outline.sortingOrder=100;UpdateOutline(item);
        }
        void UpdateOutline(GameItem item)
        {
            var collider=item.GetComponent<Collider2D>();Bounds b=collider!=null?collider.bounds:ItemVisual.Resolve(item).bounds;var z=item.transform.position.z-.1f;
            outline.SetPositions(new[]{new Vector3(b.min.x,b.min.y,z),new Vector3(b.min.x,b.max.y,z),new Vector3(b.max.x,b.max.y,z),new Vector3(b.max.x,b.min.y,z),new Vector3(b.min.x,b.min.y,z)});
        }
    }
}