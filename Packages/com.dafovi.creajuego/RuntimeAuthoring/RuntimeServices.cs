using System;
using UnityEngine;
namespace CreaJuego.Web
{
    public sealed class RuntimeSelectionService
    {
        public GameItem SelectedItem {get;private set;} public event Action<GameItem> SelectionChanged;
        public GameItem Select(GameObject hit)
        {
            var next=hit!=null?hit.GetComponentInParent<GameItem>():null;
            if(next==SelectedItem)return next; SelectedItem=next; SelectionChanged?.Invoke(next); return next;
        }
        public void Clear()=>Select(null);
    }
    public sealed class RuntimeHistory
    {
        readonly System.Collections.Generic.List<string> states=new System.Collections.Generic.List<string>(); int cursor=-1;
        public bool CanUndo=>cursor>0; public bool CanRedo=>cursor+1<states.Count;
        public void Reset(CreaJuegoProjectData data){states.Clear();states.Add(ProjectSerializer.ToJson(data));cursor=0;}
        public void Record(CreaJuegoProjectData data){if(cursor+1<states.Count)states.RemoveRange(cursor+1,states.Count-cursor-1);states.Add(ProjectSerializer.ToJson(data));cursor=states.Count-1;if(states.Count>40){states.RemoveAt(0);cursor--;}}
        public CreaJuegoProjectData Undo()=>CanUndo?ProjectSerializer.FromJson(states[--cursor]):null;
        public CreaJuegoProjectData Redo()=>CanRedo?ProjectSerializer.FromJson(states[++cursor]):null;
    }
    public static class RuntimePreflight
    {
        public static string Validate(CreaJuegoProjectData data,Func<string,GameItemDefinition> find)
        {
            int players=0,platforms=0,goals=0;
            foreach(var value in data.objects){var definition=find(value.definitionId);if(definition==null||definition.prefab==null)return "Hay un elemento sin contenido disponible.";if(definition.kind==ItemKind.Player)players++;if(definition.kind==ItemKind.Platform)platforms++;if(definition.kind==ItemKind.Goal)goals++;}
            if(players!=1)return players==0?"Añade un Jugador antes de jugar.":"Tu juego debe tener un solo Jugador.";
            if(platforms<1)return "Añade al menos una Plataforma.";
            if(goals!=1)return goals==0?"Añade una Meta.":"Tu juego debe tener una sola Meta.";
            return null;
        }
    }
}