using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CreaJuego.Web
{
    public sealed class RuntimeSelectionService
    {
        Func<string,GameItem> resolver;
        public GameItem SelectedItem{get;private set;}
        public string SelectedInstanceId{get;private set;}
        public event Action<GameItem> SelectionChanged;
        public void Bind(Func<string,GameItem> itemResolver)=>resolver=itemResolver;
        public GameItem Select(string instanceId)
        {
            var next=string.IsNullOrEmpty(instanceId)?null:resolver?.Invoke(instanceId);
            if(next==null)instanceId=null;
            if(next==SelectedItem&&instanceId==SelectedInstanceId)return next;
            SelectedItem=next;SelectedInstanceId=instanceId;SelectionChanged?.Invoke(next);return next;
        }
        public GameItem Select(GameObject hit)
        {
            var item=hit!=null?hit.GetComponentInParent<GameItem>():null;
            var marker=item!=null?item.GetComponent<RuntimeAuthoredItem>():null;
            var instanceId=marker?.instanceId;
            if(item==SelectedItem&&instanceId==SelectedInstanceId)return item;
            SelectedItem=item;SelectedInstanceId=instanceId;SelectionChanged?.Invoke(item);return item;
        }
        public void Clear()=>Select((string)null);
    }

    public static class RuntimeAuthoringHitTest
    {
        sealed class Candidate{public GameItem item;public int layer,order;public float area,distance;public bool selected;}
        public static GameItem Pick(Vector2 point,IEnumerable<GameItem> items,string selectedId=null)
        {
            var candidates=new List<Candidate>();
            foreach(var item in items.Where(i=>i!=null&&i.gameObject.activeInHierarchy))
            {
                var marker=item.GetComponent<RuntimeAuthoredItem>();if(marker==null)continue;
                var renderers=item.GetComponentsInChildren<SpriteRenderer>(false);bool visualHit=false;Bounds bounds=default;int layer=int.MinValue,order=int.MinValue;
                foreach(var renderer in renderers)
                {
                    if(!renderer.enabled||renderer.sprite==null||!renderer.bounds.Contains(point))continue;
                    if(!visualHit){bounds=renderer.bounds;visualHit=true;}else bounds.Encapsulate(renderer.bounds);
                    int value=SortingLayer.GetLayerValueFromID(renderer.sortingLayerID);if(value>layer||(value==layer&&renderer.sortingOrder>order)){layer=value;order=renderer.sortingOrder;}
                }
                if(!visualHit){var collider=item.GetComponentInChildren<Collider2D>();if(collider==null||!collider.bounds.Contains(point))continue;bounds=collider.bounds;layer=-1;order=-1;}
                candidates.Add(new Candidate{item=item,layer=layer,order=order,area=Mathf.Max(.0001f,bounds.size.x*bounds.size.y),distance=((Vector2)item.transform.position-point).sqrMagnitude,selected=marker.instanceId==selectedId});
            }
            return candidates.OrderByDescending(v=>v.layer).ThenByDescending(v=>v.order).ThenBy(v=>v.area).ThenByDescending(v=>v.selected).ThenBy(v=>v.distance).Select(v=>v.item).FirstOrDefault();
        }
    }
    public sealed class RuntimeHistory
    {
        readonly List<string> states=new List<string>();int cursor=-1;
        public bool CanUndo=>cursor>0;
        public bool CanRedo=>cursor+1<states.Count;
        public int StateCount=>states.Count;
        public void Reset(CreaJuegoProjectData data){states.Clear();states.Add(ProjectSerializer.ToJson(data));cursor=0;}
        public void Record(CreaJuegoProjectData data)
        {
            var json=ProjectSerializer.ToJson(data);if(cursor>=0&&states[cursor]==json)return;
            if(cursor+1<states.Count)states.RemoveRange(cursor+1,states.Count-cursor-1);states.Add(json);cursor=states.Count-1;
            if(states.Count>40){states.RemoveAt(0);cursor--;}
        }
        public CreaJuegoProjectData Undo()=>CanUndo?ProjectSerializer.FromJson(states[--cursor]):null;
        public CreaJuegoProjectData Redo()=>CanRedo?ProjectSerializer.FromJson(states[++cursor]):null;
    }
    public static class RuntimeSnap
    {
        public const float Step=.25f;
        public static Vector3 Position(Vector3 value,bool enabled){if(enabled){value.x=Mathf.Round(value.x/Step)*Step;value.y=Mathf.Round(value.y/Step)*Step;}return value;}
        public static float Width(float value,bool enabled)=>Mathf.Clamp(enabled?Mathf.Round(value/Step)*Step:value,.5f,30);
    }
    public sealed class RuntimePreflightContext
    {
        public bool cameraAvailable=true,validateSupport=true,requireRecovery=true;
    }
    public static class RuntimePreflight
    {
        public static string Validate(CreaJuegoProjectData data,Func<string,GameItemDefinition> find,RuntimePreflightContext context=null)
        {
            var kinds=new Dictionary<RuntimeItemData,GameItemDefinition>();
            foreach(var item in data.objects){var def=find(item.definitionId);if(def==null||def.prefab==null)return "Hay un elemento sin contenido disponible.";kinds[item]=def;}
            var players=kinds.Where(p=>p.Value.kind==ItemKind.Player).ToArray();var platforms=kinds.Where(p=>p.Value.kind==ItemKind.Platform).ToArray();var goals=kinds.Count(p=>p.Value.kind==ItemKind.Goal);
            if(players.Length!=1)return players.Length==0?"Añade un Jugador antes de jugar.":"Tu juego debe tener un solo Jugador.";
            if(platforms.Length==0)return "Agrega al menos una Plataforma para comenzar.";
            if(goals!=1)return goals==0?"El nivel necesita una Meta.":"Tu juego debe tener una sola Meta.";
            if(context==null)return null;
            if(data.bounds==null||!data.bounds.IsValid)return "El tamaño del nivel no es válido. Elige otro tamaño.";
            if(!context.cameraAvailable)return "Prepara la vista de juego antes de jugar.";
            if(context.requireRecovery&&players[0].Value.prefab.GetComponent<PlayerFallRecovery>()==null)return "El personaje necesita volver al inicio si cae. Créalo de nuevo desde el catálogo.";
            if(context.validateSupport&&!SeemsSupported(players[0].Key,players[0].Value,platforms))return "El personaje parece estar en el aire. Ponlo sobre una plataforma.";
            return null;
        }
        static bool SeemsSupported(RuntimeItemData player,GameItemDefinition definition,KeyValuePair<RuntimeItemData,GameItemDefinition>[] platforms)
        {
            var box=definition.prefab.GetComponent<BoxCollider2D>();if(box==null)return false;
            var bottom=player.position.y+(box.offset.y-box.size.y*.5f)*player.scale.y;
            var half=box.size.x*.5f*Mathf.Abs(player.scale.x);
            foreach(var pair in platforms)
            {
                var surface=pair.Value.prefab.GetComponent<BoxCollider2D>();if(surface==null)continue;
                var platform=pair.Key;var top=platform.position.y+(surface.offset.y+surface.size.y*.5f)*platform.scale.y;
                var width=Mathf.Max(.5f,platform.platformWidth)*Mathf.Abs(platform.scale.x);
                if(Mathf.Abs(player.position.x-platform.position.x)<half+width*.5f&&bottom-top>=-.2f&&bottom-top<=.45f)return true;
            }
            return false;
        }
    }
    public static class RuntimePlatformGeometry
    {
        public static void Apply(GameItem item,float width)
        {
            if(item==null||item.definition==null||item.definition.kind!=ItemKind.Platform)return;
            width=Mathf.Clamp(width,.5f,30);var box=item.GetComponent<BoxCollider2D>();if(box==null)return;
            var size=box.size;size.x=width;box.size=size;
            var visual=item.GetComponent<ItemVisual>();if(visual!=null&&visual.geometrySource!=null)visual.geometrySource.size=size;
            var renderer=ItemVisual.Resolve(item);if(renderer==null||renderer.sprite==null)return;
            renderer.drawMode=SpriteDrawMode.Simple;var spriteSize=renderer.sprite.bounds.size;
            renderer.transform.localScale=new Vector3(width/Mathf.Max(.001f,spriteSize.x),size.y/Mathf.Max(.001f,spriteSize.y),1);
        }
    }
}