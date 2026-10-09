using System;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace CreaJuego.Web
{
    public enum RuntimeTransformTool{Move,Resize,Paint,Erase}
    public enum RuntimeMoveAxis{None,Free,Horizontal,Vertical}

    [RequireComponent(typeof(RuntimeAuthoringController))]
    public sealed class RuntimeAuthoringInput:MonoBehaviour
    {
        RuntimeAuthoringController controller;Camera View=>controller.buildCamera;
        bool dragging,panning,resizing,painting,resizeHorizontal,resizePositive,changed,fallingGuide;Vector3 offset,last;Vector3Int lastPaintCell;float fixedEdge;RuntimeMoveAxis moveAxis;
        LineRenderer outline,worldFrame,movementPath,movementArrow,moveHorizontal,moveVertical;SpriteRenderer leftHandle,rightHandle,topHandle,bottomHandle,movementStart,movementEnd,moveCenter;Material lineMaterial;Sprite handleSprite;
        public bool HasMovementGuide=>movementPath!=null&&movementPath.gameObject.activeInHierarchy;
        public Vector3 MovementGuideStart=>movementStart!=null?movementStart.transform.position:Vector3.zero;
        public Vector3 MovementGuideEnd=>movementEnd!=null?movementEnd.transform.position:Vector3.zero;
        public float WorldFrameWidth=>worldFrame!=null?worldFrame.widthMultiplier:0;
        public Color WorldFrameColor=>worldFrame!=null?worldFrame.startColor:Color.clear;
        public RuntimeTransformTool Tool{get;private set;}=RuntimeTransformTool.Move;
        public bool HasVisibleResizeHandles=>leftHandle!=null&&leftHandle.gameObject.activeInHierarchy;
        public event Action<RuntimeTransformTool> ToolChanged;
        void Awake(){controller=GetComponent<RuntimeAuthoringController>();controller.Selection.SelectionChanged+=Selected;controller.ProjectChanged+=RefreshWorld;}
        void Start(){lineMaterial=new Material(Shader.Find("Sprites/Default"));handleSprite=CreateHandleSprite();worldFrame=Line("Zona del nivel",new Color(1f,.72f,.05f,.98f),.08f,110);worldFrame.positionCount=4;worldFrame.loop=true;worldFrame.numCornerVertices=2;RefreshWorld();}
        void OnDestroy(){controller.Selection.SelectionChanged-=Selected;controller.ProjectChanged-=RefreshWorld;if(lineMaterial!=null)Destroy(lineMaterial);if(handleSprite!=null){var texture=handleSprite.texture;Destroy(handleSprite);Destroy(texture);}}
        void Update()
        {
            bool build=controller.Mode==AuthoringMode.Build;if(worldFrame!=null)worldFrame.gameObject.SetActive(build);SetSelectionVisible(build);if(!build||View==null)return;UpdateGuideScale();if(Mouse.current==null)return;
            var mouse=Mouse.current;var screen=mouse.position.ReadValue();var world=View.ScreenToWorldPoint(new Vector3(screen.x,screen.y,-View.transform.position.z));world.z=0;bool overUI=RuntimePointerContext.IsPointerOverBlockingUI(screen);bool inViewport=View.pixelRect.Contains(screen);
            if(mouse.leftButton.wasPressedThisFrame&&!overUI&&inViewport)
            {
                if(Tool==RuntimeTransformTool.Paint||Tool==RuntimeTransformTool.Erase)
                {
                    var objectHit=RuntimeAuthoringHitTest.Pick(world,controller.buildRoot.GetComponentsInChildren<GameItem>(),controller.SelectedId());
                    if(objectHit!=null)
                    {
                        var instanceId=objectHit.GetComponent<RuntimeAuthoredItem>()?.instanceId;var selectedItem=controller.Selection.Select(instanceId);SetTool(RuntimeTransformTool.Move);if(selectedItem!=null)BeginMove(selectedItem,world,RuntimeMoveAxis.Free);return;
                    }
                    painting=true;changed=controller.PaintTile(world,Tool==RuntimeTransformTool.Erase,false);lastPaintCell=controller.TileCell(world);controller.Selection.Clear();return;
                }
                if(TryBeginResize(screen)){changed=false;return;}
                if(TryBeginMoveGizmo(screen,world)){changed=false;return;}
                var hit=RuntimeAuthoringHitTest.Pick(world,controller.buildRoot.GetComponentsInChildren<GameItem>(),controller.SelectedId());var item=controller.Selection.Select(hit!=null?hit.GetComponent<RuntimeAuthoredItem>()?.instanceId:null);
                if(item!=null&&Tool==RuntimeTransformTool.Move)BeginMove(item,world,RuntimeMoveAxis.Free);else if(item==null){panning=true;last=world;}
            }
            if(mouse.leftButton.isPressed&&painting){var cell=controller.TileCell(world);if(cell!=lastPaintCell){changed|=controller.PaintTile(world,Tool==RuntimeTransformTool.Erase,false);lastPaintCell=cell;}}
            else if(mouse.leftButton.isPressed&&resizing)Resize(world);
            else if(mouse.leftButton.isPressed&&dragging)DragMove(world);
            else if(mouse.leftButton.isPressed&&panning){var delta=last-world;View.transform.position+=delta;last=View.ScreenToWorldPoint(new Vector3(screen.x,screen.y,-View.transform.position.z));last.z=0;}
            if(mouse.leftButton.wasReleasedThisFrame){if(dragging)EndMove();else if((resizing||painting)&&changed)controller.CommitEdit();panning=resizing=painting=changed=false;moveAxis=RuntimeMoveAxis.None;}
            var wheel=mouse.scroll.ReadValue().y;if(Mathf.Abs(wheel)>.01f&&!overUI&&inViewport)View.orthographicSize=RuntimePointerContext.Zoom(View.orthographicSize,wheel,2,100);
            var keys=Keyboard.current;if(keys!=null&&(keys.leftCtrlKey.isPressed||keys.rightCtrlKey.isPressed)&&keys.zKey.wasPressedThisFrame){if(keys.leftShiftKey.isPressed||keys.rightShiftKey.isPressed)controller.Redo();else controller.Undo();}
            if(keys!=null&&(keys.leftCtrlKey.isPressed||keys.rightCtrlKey.isPressed)&&keys.yKey.wasPressedThisFrame)controller.Redo();
            bool editingText=EventSystem.current!=null&&EventSystem.current.currentSelectedGameObject!=null&&EventSystem.current.currentSelectedGameObject.GetComponent<UnityEngine.UI.InputField>()!=null;
            if(keys!=null&&!editingText&&!keys.leftCtrlKey.isPressed&&!keys.rightCtrlKey.isPressed){if(keys.wKey.wasPressedThisFrame)SetTool(RuntimeTransformTool.Move);else if(keys.rKey.wasPressedThisFrame)SetTool(RuntimeTransformTool.Resize);else if(keys.pKey.wasPressedThisFrame)SetTool(RuntimeTransformTool.Paint);else if(keys.eKey.wasPressedThisFrame)SetTool(RuntimeTransformTool.Erase);}UpdateSelectionVisuals();
        }
        public void SetTool(RuntimeTransformTool tool)
        {
            if(Tool==tool){SetSelectionVisible(controller!=null&&controller.Mode==AuthoringMode.Build);return;}
            Tool=tool;dragging=panning=resizing=painting=changed=false;moveAxis=RuntimeMoveAxis.None;SetSelectionVisible(controller!=null&&controller.Mode==AuthoringMode.Build);ToolChanged?.Invoke(tool);
        }
        public bool BeginMoveHandle(Vector2 screen,Vector3 world)=>TryBeginMoveGizmo(screen,world);
        public bool DragMove(Vector3 world)
        {
            if(!dragging)return false;var selected=controller.Selection.SelectedItem;if(selected==null){dragging=false;return false;}var before=selected.transform.position;var target=world+offset;if(moveAxis==RuntimeMoveAxis.Horizontal)target.y=last.y;else if(moveAxis==RuntimeMoveAxis.Vertical)target.x=last.x;controller.MoveSelected(target,false);bool moved=(before-selected.transform.position).sqrMagnitude>.0001f;changed|=moved;return moved;
        }
        public void EndMove(){if(dragging&&changed)controller.CommitEdit();dragging=changed=false;moveAxis=RuntimeMoveAxis.None;}
        bool TryBeginMoveGizmo(Vector2 screen,Vector3 world)
        {
            var item=controller.Selection.SelectedItem;if(Tool!=RuntimeTransformTool.Move||item==null||moveCenter==null||moveHorizontal==null||moveVertical==null)return false;
            var center=(Vector2)View.WorldToScreenPoint(moveCenter.transform.position);var xEnd=(Vector2)View.WorldToScreenPoint(moveHorizontal.GetPosition(4));var yEnd=(Vector2)View.WorldToScreenPoint(moveVertical.GetPosition(4));var axis=RuntimeMoveGizmoHit.Resolve(screen,center,xEnd,yEnd,14);
            // La ruta azul/naranja es una guía, pero visualmente parece un control de movimiento.
            // Aceptarla también como zona de agarre evita que el participante arrastre la flecha
            // sin mover el objeto. La distancia del recorrido se sigue editando en Propiedades.
            if(axis==RuntimeMoveAxis.None&&movementPath!=null&&movementPath.gameObject.activeInHierarchy)
            {
                var routeStart=(Vector2)View.WorldToScreenPoint(movementPath.GetPosition(0));
                var routeEnd=(Vector2)View.WorldToScreenPoint(movementPath.GetPosition(1));
                if(RuntimeMoveGizmoHit.IsNearSegment(screen,routeStart,routeEnd,18))axis=RuntimeMoveAxis.Free;
            }
            if(axis==RuntimeMoveAxis.None)return false;BeginMove(item,world,axis);return true;
        }
        void BeginMove(GameItem item,Vector3 world,RuntimeMoveAxis axis){dragging=true;changed=false;moveAxis=axis;offset=item.transform.position-world;last=item.transform.position;}
        bool TryBeginResize(Vector2 screen)
        {
            if(Tool!=RuntimeTransformTool.Resize)return false;
            var item=controller.Selection.SelectedItem;if(item==null||item.definition==null||item.definition.kind==ItemKind.Background||leftHandle==null)return false;
            var handles=new[]{leftHandle,rightHandle,bottomHandle,topHandle};var positions=handles.Select(handle=>(Vector2)View.WorldToScreenPoint(handle.transform.position)).ToArray();int closest=RuntimeResizeHit.Resolve(screen,positions,12);
            if(closest<0)return false;
            var bounds=ItemBounds(item);resizeHorizontal=closest<2;resizePositive=closest==1||closest==3;
            fixedEdge=resizeHorizontal?(resizePositive?bounds.min.x:bounds.max.x):(resizePositive?bounds.min.y:bounds.max.y);resizing=true;return true;
        }
        void Resize(Vector3 movingPoint)
        {
            var item=controller.Selection.SelectedItem;if(item==null)return;var bounds=ItemBounds(item);var size=new Vector2(bounds.size.x,bounds.size.y);var center=new Vector2(bounds.center.x,bounds.center.y);
            float moving=resizeHorizontal?movingPoint.x:movingPoint.y;float extent=RuntimeSnap.Width(Mathf.Abs(moving-fixedEdge),controller.Project.alignAutomatically);
            if(resizeHorizontal){size.x=extent;center.x=fixedEdge+(resizePositive?extent*.5f:-extent*.5f);}else{size.y=extent;center.y=fixedEdge+(resizePositive?extent*.5f:-extent*.5f);}
            var data=controller.SelectedData();var beforeScale=data.scale;var beforePosition=data.position;controller.ResizeSelected(size,center,false);changed|=(beforeScale-data.scale).sqrMagnitude>.0001f||(beforePosition-data.position).sqrMagnitude>.0001f;UpdateSelectionVisuals();
        }        void Selected(GameItem item)
        {
            DestroySelection();if(item==null)return;outline=Line("Selección",new Color(1,.85f,.1f),.06f,100);
            if(item.definition!=null&&item.definition.kind!=ItemKind.Background){leftHandle=Handle("Tamaño izquierdo");rightHandle=Handle("Tamaño derecho");topHandle=Handle("Tamaño superior");bottomHandle=Handle("Tamaño inferior");moveHorizontal=Line("Mover horizontal",new Color(.95f,.2f,.16f,.98f),.08f,103);moveHorizontal.positionCount=8;moveVertical=Line("Mover vertical",new Color(.25f,.9f,.28f,.98f),.08f,103);moveVertical.positionCount=8;moveCenter=GuidePoint("Mover libre",new Color(1f,.85f,.1f,.98f));moveCenter.sortingOrder=104;}
            fallingGuide=controller.IsCatchMode&&item.definition!=null&&(item.definition.kind==ItemKind.Prize||item.definition.kind==ItemKind.Hazard);
            if(item.definition!=null&&(item.definition.kind==ItemKind.MovingPlatform||item.definition.kind==ItemKind.Enemy)||fallingGuide){var color=fallingGuide?(item.definition.kind==ItemKind.Prize?new Color(1f,.8f,.15f,.95f):new Color(1f,.25f,.2f,.95f)):item.definition.kind==ItemKind.Enemy?new Color(1f,.35f,.2f,.95f):new Color(.2f,.75f,1f,.95f);movementPath=Line(fallingGuide?"Trayectoria de caída":"Recorrido",color,.075f,99);movementPath.positionCount=2;movementArrow=Line("Dirección",color,.075f,99);movementArrow.positionCount=3;movementStart=GuidePoint(fallingGuide?"Aparece aquí":"Inicio del recorrido",color);movementEnd=GuidePoint(fallingGuide?"Final de caída":"Final del recorrido",color);}
            UpdateSelectionVisuals();
        }
        void UpdateSelectionVisuals()
        {
            var item=controller.Selection.SelectedItem;if(item==null||outline==null)return;var bounds=ItemBounds(item);var z=item.transform.position.z-.1f;outline.SetPositions(new[]{new Vector3(bounds.min.x,bounds.min.y,z),new Vector3(bounds.min.x,bounds.max.y,z),new Vector3(bounds.max.x,bounds.max.y,z),new Vector3(bounds.max.x,bounds.min.y,z),new Vector3(bounds.min.x,bounds.min.y,z)});
            if(leftHandle!=null){float size=Mathf.Clamp(View.orthographicSize*.045f,.18f,.65f);leftHandle.transform.position=new Vector3(bounds.min.x,bounds.center.y,z-.01f);rightHandle.transform.position=new Vector3(bounds.max.x,bounds.center.y,z-.01f);topHandle.transform.position=new Vector3(bounds.center.x,bounds.max.y,z-.01f);bottomHandle.transform.position=new Vector3(bounds.center.x,bounds.min.y,z-.01f);leftHandle.transform.localScale=rightHandle.transform.localScale=topHandle.transform.localScale=bottomHandle.transform.localScale=Vector3.one*size;}
            if(moveCenter!=null)
            {
                var center=new Vector3(bounds.center.x,bounds.center.y,z-.04f);float arm=Mathf.Clamp(View.orthographicSize*.14f,.8f,4f),head=arm*.22f;moveCenter.transform.position=center;moveCenter.transform.localScale=Vector3.one*Mathf.Clamp(View.orthographicSize*.025f,.16f,.5f);
                var left=center+Vector3.left*arm;var right=center+Vector3.right*arm;moveHorizontal.SetPositions(new[]{left+Vector3.up*head,left,left+Vector3.down*head,left,right,right+Vector3.up*head,right,right+Vector3.down*head});
                var bottom=center+Vector3.down*arm;var top=center+Vector3.up*arm;moveVertical.SetPositions(new[]{bottom+Vector3.left*head,bottom,bottom+Vector3.right*head,bottom,top,top+Vector3.left*head,top,top+Vector3.right*head});
            }
            if(movementPath!=null){Vector3 start,end;if(fallingGuide)RuntimeCatchGuide.TryGetRoute(item,controller.Project.bounds,out start,out end);else RuntimeMovementGuide.TryGetRoute(item,out start,out end);start.z=end.z=z-.02f;movementPath.SetPositions(new[]{start,end});float pointSize=Mathf.Clamp(View.orthographicSize*.035f,.14f,2f);movementStart.transform.position=start;movementEnd.transform.position=end;movementStart.transform.localScale=movementEnd.transform.localScale=Vector3.one*pointSize;float arrowSize=Mathf.Clamp(View.orthographicSize*.04f,.4f,3f);var direction=(end-start).normalized;var normal=new Vector3(-direction.y,direction.x);var tip=end;var basePoint=end-direction*arrowSize;movementArrow.SetPositions(new[]{basePoint+normal*arrowSize*.45f,tip,basePoint-normal*arrowSize*.45f});}
        }
        void UpdateGuideScale()
        {
            float width=RuntimeGuideScale.WorldWidth(View.orthographicSize,View.pixelHeight);if(worldFrame!=null)worldFrame.widthMultiplier=width;float guideWidth=Mathf.Clamp(View.orthographicSize*.012f,.06f,1.2f);if(outline!=null)outline.widthMultiplier=guideWidth*1.25f;if(movementPath!=null)movementPath.widthMultiplier=guideWidth*1.35f;if(movementArrow!=null)movementArrow.widthMultiplier=guideWidth*1.35f;if(moveHorizontal!=null)moveHorizontal.widthMultiplier=guideWidth*1.6f;if(moveVertical!=null)moveVertical.widthMultiplier=guideWidth*1.6f;
        }
        void RefreshWorld()
        {
            if(worldFrame==null||controller.Project?.bounds==null)return;var b=controller.Project.bounds;worldFrame.SetPositions(new[]{new Vector3(b.left,b.bottom,5),new Vector3(b.left,b.top,5),new Vector3(b.right,b.top,5),new Vector3(b.right,b.bottom,5)});UpdateGuideScale();UpdateSelectionVisuals();
        }
        void SetSelectionVisible(bool visible){if(outline!=null)outline.gameObject.SetActive(visible);bool showResize=visible&&Tool==RuntimeTransformTool.Resize,showMove=visible&&Tool==RuntimeTransformTool.Move;foreach(var handle in new[]{leftHandle,rightHandle,topHandle,bottomHandle})if(handle!=null)handle.gameObject.SetActive(showResize);if(moveHorizontal!=null){moveHorizontal.gameObject.SetActive(showMove);moveVertical.gameObject.SetActive(showMove);moveCenter.gameObject.SetActive(showMove);}if(movementPath!=null){movementPath.gameObject.SetActive(visible);movementArrow.gameObject.SetActive(visible);movementStart.gameObject.SetActive(visible);movementEnd.gameObject.SetActive(visible);}}
        void DestroySelection(){if(outline!=null)Destroy(outline.gameObject);foreach(var handle in new[]{leftHandle,rightHandle,topHandle,bottomHandle,movementStart,movementEnd,moveCenter})if(handle!=null)Destroy(handle.gameObject);foreach(var line in new[]{movementPath,movementArrow,moveHorizontal,moveVertical})if(line!=null)Destroy(line.gameObject);outline=movementPath=movementArrow=moveHorizontal=moveVertical=null;leftHandle=rightHandle=topHandle=bottomHandle=movementStart=movementEnd=moveCenter=null;fallingGuide=false;}        LineRenderer Line(string name,Color color,float width,int order){var go=new GameObject(name);go.transform.SetParent(transform,false);var line=go.AddComponent<LineRenderer>();line.material=lineMaterial;line.startColor=line.endColor=color;line.startWidth=line.endWidth=width;line.positionCount=5;line.loop=false;line.sortingOrder=order;return line;}
        SpriteRenderer Handle(string name){var go=new GameObject(name,typeof(RuntimeResizeHandle),typeof(SpriteRenderer));go.transform.SetParent(transform,false);var renderer=go.GetComponent<SpriteRenderer>();renderer.sprite=handleSprite;renderer.color=new Color(1,.85f,.1f);renderer.sortingOrder=101;return renderer;}
        SpriteRenderer GuidePoint(string name,Color color){var go=new GameObject(name,typeof(SpriteRenderer));go.transform.SetParent(transform,false);var renderer=go.GetComponent<SpriteRenderer>();renderer.sprite=handleSprite;renderer.color=color;renderer.sortingOrder=100;return renderer;}
        static Sprite CreateHandleSprite(){var texture=new Texture2D(8,8,TextureFormat.RGBA32,false);var pixels=new Color[64];for(int i=0;i<pixels.Length;i++)pixels[i]=Color.white;texture.SetPixels(pixels);texture.Apply();return Sprite.Create(texture,new Rect(0,0,8,8),new Vector2(.5f,.5f),8);}
        static Bounds ItemBounds(GameItem item){var collider=item.GetComponent<Collider2D>();if(collider!=null)return collider.bounds;var renderer=ItemVisual.Resolve(item);return renderer!=null?renderer.bounds:new Bounds(item.transform.position,Vector3.one);}
    }
    public static class RuntimeMovementGuide
    {
        public static bool TryGetRoute(GameItem item,out Vector3 start,out Vector3 end)
        {
            start=end=Vector3.zero;if(item==null||item.definition==null||(item.definition.kind!=ItemKind.MovingPlatform&&item.definition.kind!=ItemKind.Enemy))return false;
            start=item.transform.position;end=start+Vector3.right*Mathf.Max(.5f,item.distance);return true;
        }
    }
    public sealed class RuntimeResizeHandle:MonoBehaviour{}
    public static class RuntimeMoveGizmoHit
    {
        public static RuntimeMoveAxis Resolve(Vector2 point,Vector2 center,Vector2 xEnd,Vector2 yEnd,float radius)
        {
            if(Vector2.Distance(point,center)<=radius)return RuntimeMoveAxis.Free;
            var xStart=center-(xEnd-center);if(DistanceToSegment(point,xStart,xEnd)<=radius)return RuntimeMoveAxis.Horizontal;
            var yStart=center-(yEnd-center);if(DistanceToSegment(point,yStart,yEnd)<=radius)return RuntimeMoveAxis.Vertical;
            return RuntimeMoveAxis.None;
        }
        public static bool IsNearSegment(Vector2 point,Vector2 start,Vector2 end,float radius)=>DistanceToSegment(point,start,end)<=radius;
        static float DistanceToSegment(Vector2 point,Vector2 a,Vector2 b){var line=b-a;float length=line.sqrMagnitude;if(length<.001f)return Vector2.Distance(point,a);float t=Mathf.Clamp01(Vector2.Dot(point-a,line)/length);return Vector2.Distance(point,a+line*t);}
    }
    public static class RuntimeResizeHit
    {
        public static int Resolve(Vector2 point,System.Collections.Generic.IReadOnlyList<Vector2> handles,float radius)
        {
            if(handles==null||handles.Count<4)return -1;int closest=-1;float distance=float.MaxValue;var distances=new float[handles.Count];
            for(int i=0;i<handles.Count;i++){distances[i]=Vector2.Distance(point,handles[i]);if(distances[i]<distance){distance=distances[i];closest=i;}}
            if(distance>radius)return -1;int opposite=closest==0?1:closest==1?0:closest==2?3:2;return distances[opposite]<=radius?-1:closest;
        }
    }
    public static class RuntimeGuideScale
    {
        public static float WorldWidth(float orthographicSize,int pixelHeight)
        {
            float worldPerPixel=2f*Mathf.Max(.01f,orthographicSize)/Mathf.Max(1,pixelHeight);
            float pixels=Mathf.Lerp(4f,8f,Mathf.InverseLerp(8f,100f,orthographicSize));
            return Mathf.Clamp(worldPerPixel*pixels,.04f,2f);
        }
    }
    public static class RuntimeCatchGuide
    {
        public static bool TryGetRoute(GameItem item,RuntimeLevelBounds bounds,out Vector3 start,out Vector3 end)
        {
            start=end=Vector3.zero;if(item==null||item.definition==null||bounds==null||(item.definition.kind!=ItemKind.Prize&&item.definition.kind!=ItemKind.Hazard))return false;
            start=item.transform.position;start.x=Mathf.Clamp(start.x,bounds.left+.25f,bounds.right-.25f);start.y=Mathf.Max(start.y,bounds.top-.75f);end=new Vector3(start.x,bounds.bottom,start.z);return true;
        }
    }
    public static class RuntimePointerContext
    {
        public const float ZoomSensitivity=.12f;
        static readonly System.Collections.Generic.List<RaycastResult> Hits=new System.Collections.Generic.List<RaycastResult>();
        public static bool IsPointerOverBlockingUI(Vector2 position){if(EventSystem.current==null)return false;Hits.Clear();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=position},Hits);return Hits.Count>0;}
        public static float NormalizeWheel(float delta)=>Mathf.Abs(delta)>=10?delta/120f:delta;
        public static float Zoom(float current,float delta,float minimum,float maximum)=>Mathf.Clamp(current*Mathf.Exp(-NormalizeWheel(delta)*ZoomSensitivity),minimum,maximum);
    }
}
