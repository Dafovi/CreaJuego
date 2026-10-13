using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CreaJuego.Web
{
    public enum RuntimeTileLayerRole { Solid, OneWay, Decoration }
    public enum RuntimeTileBrush { Pencil, Line, Rectangle, Ramp }

    public static class RuntimeTileBrushGeometry
    {
        public static IEnumerable<Vector3Int> Cells(RuntimeTileBrush brush,Vector3Int start,Vector3Int end)
        {
            if(brush==RuntimeTileBrush.Rectangle)return Rectangle(start,end);
            if(brush==RuntimeTileBrush.Line)return Line(start,end);
            return new[]{end};
        }

        public static IEnumerable<Vector3Int> Line(Vector3Int start,Vector3Int end)
        {
            var cells=new List<Vector3Int>();int x=start.x,y=start.y,dx=Mathf.Abs(end.x-x),dy=Mathf.Abs(end.y-y),sx=x<end.x?1:-1,sy=y<end.y?1:-1,err=dx-dy;
            while(true){cells.Add(new Vector3Int(x,y,0));if(x==end.x&&y==end.y)break;int twice=err*2;if(twice>-dy){err-=dy;x+=sx;}if(twice<dx){err+=dx;y+=sy;}}
            return cells;
        }

        public static IEnumerable<Vector3Int> Rectangle(Vector3Int start,Vector3Int end)
        {
            var cells=new List<Vector3Int>();int minX=Mathf.Min(start.x,end.x),maxX=Mathf.Max(start.x,end.x),minY=Mathf.Min(start.y,end.y),maxY=Mathf.Max(start.y,end.y);
            for(int y=minY;y<=maxY;y++)for(int x=minX;x<=maxX;x++)cells.Add(new Vector3Int(x,y,0));return cells;
        }

        public static IEnumerable<Vector3Int> RampFill(Vector3Int start,Vector3Int end)
        {
            if(start.x==end.x)return Array.Empty<Vector3Int>();
            if(start.x>end.x){var swap=start;start=end;end=swap;}int steps=end.x-start.x,direction=Math.Sign(end.y-start.y),floor=Mathf.Min(start.y,end.y);var cells=new List<Vector3Int>();
            for(int index=0;index<steps;index++)
            {
                int surface=direction>0?start.y+index:start.y-index-1;
                for(int y=floor;y<surface;y++)cells.Add(new Vector3Int(start.x+index,y,0));
            }
            return cells;
        }

        public static bool RampTouchesCell(RuntimeTileRampData ramp,Vector3Int cell)
        {
            if(ramp==null)return false;
            if(cell.x==ramp.startX&&cell.y==ramp.startY||cell.x==ramp.endX&&cell.y==ramp.endY)return true;
            int left=Mathf.Min(ramp.startX,ramp.endX),right=Mathf.Max(ramp.startX,ramp.endX);if(cell.x<left||cell.x>=right)return false;
            bool storedLeftToRight=ramp.startX<=ramp.endX;int startX=storedLeftToRight?ramp.startX:ramp.endX,startY=storedLeftToRight?ramp.startY:ramp.endY,endY=storedLeftToRight?ramp.endY:ramp.startY;
            int offset=cell.x-startX;int occupiedY=endY>startY?startY+offset:startY-offset-1;return cell.y==occupiedY;
        }
    }

    [Serializable]
    public sealed class RuntimeTileDefinition
    {
        public string id,displayName;
        [Tooltip("Tema que agrupa este terreno en la interfaz educativa.")] public string theme="Terreno";
        public string[] legacyIds=Array.Empty<string>();
        public Sprite icon;
        public TileBase tile;
        [Tooltip("Tile visual que rellena la celda detrás de un Rule Tile con bordes transparentes.")] public TileBase underlayTile;
        [Tooltip("Pieza triangular que sube de izquierda a derecha.")] public Sprite rampAscending;
        [Tooltip("Pieza triangular que baja de izquierda a derecha.")] public Sprite rampDescending;
        public RuntimeTileLayerRole layer=RuntimeTileLayerRole.Solid;
        public Sprite Preview=>icon!=null?icon:(tile as Tile)?.sprite;
        public bool SupportsRamp=>rampAscending!=null&&rampDescending!=null;
    }

    public sealed class RuntimeTilemapWorld
    {
        readonly RuntimeTilePaletteDefinition palette;
        readonly Dictionary<RuntimeTileLayerRole,Tilemap> maps=new Dictionary<RuntimeTileLayerRole,Tilemap>();
        Tilemap underlay;
        public Grid Grid{get;private set;}
        public int TileCount=>maps.Values.Sum(map=>map.GetUsedTilesCount());

        public static RuntimeTilemapWorld Build(Transform parent,CreaJuegoProjectData project,RuntimeTilePaletteDefinition palette,bool authoring)
        {
            if(parent==null||project==null||palette==null)return null;
            var world=new RuntimeTilemapWorld(palette);world.Create(parent,project,authoring);return world;
        }

        RuntimeTilemapWorld(RuntimeTilePaletteDefinition palette)=>this.palette=palette;

        void Create(Transform parent,CreaJuegoProjectData project,bool authoring)
        {
            var root=new GameObject("Terreno por tiles",typeof(Grid));root.transform.SetParent(parent,false);Grid=root.GetComponent<Grid>();Grid.cellSize=Vector3.one;
            underlay=CreateVisualLayer(root.transform,"Relleno del terreno",-3);foreach(RuntimeTileLayerRole role in Enum.GetValues(typeof(RuntimeTileLayerRole)))maps[role]=CreateLayer(root.transform,role,authoring);
            foreach(var layer in project.tileLayers??new List<RuntimeTileLayerData>())foreach(var cell in layer?.cells??new List<RuntimeTileCellData>())
            {
                var definition=palette.Find(cell.tileId);if(definition?.tile==null)continue;var position=new Vector3Int(cell.x,cell.y,0);maps[definition.layer].SetTile(position,definition.tile);underlay.SetTile(position,definition.underlayTile);
            }
            foreach(var ramp in project.tileRamps??new List<RuntimeTileRampData>())CreateRamp(root.transform,ramp,authoring);
        }

        void CreateRamp(Transform parent,RuntimeTileRampData data,bool authoring)
        {
            var definition=palette.Find(data.tileId);if(definition==null||!definition.SupportsRamp||data.startX>=data.endX)return;bool ascending=data.endY>data.startY;var sprite=ascending?definition.rampAscending:definition.rampDescending;int pieces=data.endX-data.startX;
            var root=new GameObject("Rampa de tiles");root.transform.SetParent(parent,false);float spriteWidth=Mathf.Max(.01f,sprite.bounds.size.x),spriteHeight=Mathf.Max(.01f,sprite.bounds.size.y);
            for(int index=0;index<pieces;index++)
            {
                int y=ascending?data.startY+index:data.startY-index-1;var piece=new GameObject("Tile de pendiente "+(index+1),typeof(SpriteRenderer));piece.transform.SetParent(root.transform,false);piece.transform.localPosition=new Vector3(data.startX+index+.5f,y+.5f,0);piece.transform.localScale=new Vector3(1f/spriteWidth,1f/spriteHeight,1);var renderer=piece.GetComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.sortingOrder=-1;
            }
            var edge=root.AddComponent<EdgeCollider2D>();edge.points=new[]{new Vector2(data.startX,data.startY),new Vector2(data.endX,data.endY)};edge.enabled=!authoring;var body=root.AddComponent<Rigidbody2D>();body.bodyType=RigidbodyType2D.Static;body.simulated=!authoring;var effector=root.AddComponent<PlatformEffector2D>();effector.useOneWay=true;effector.surfaceArc=170;edge.usedByEffector=true;
        }

        static Tilemap CreateLayer(Transform parent,RuntimeTileLayerRole role,bool authoring)
        {
            var go=new GameObject(LayerId(role),typeof(Tilemap),typeof(TilemapRenderer));go.transform.SetParent(parent,false);var map=go.GetComponent<Tilemap>();var renderer=go.GetComponent<TilemapRenderer>();renderer.sortingOrder=role==RuntimeTileLayerRole.Decoration?-5:-2;
            if(role!=RuntimeTileLayerRole.Decoration)
            {
                var collider=go.AddComponent<TilemapCollider2D>();collider.enabled=!authoring;
                var body=go.AddComponent<Rigidbody2D>();body.bodyType=RigidbodyType2D.Static;body.simulated=!authoring;
                if(role==RuntimeTileLayerRole.OneWay){var effector=go.AddComponent<PlatformEffector2D>();effector.useOneWay=true;collider.usedByEffector=true;}
            }
            return map;
        }
        static Tilemap CreateVisualLayer(Transform parent,string name,int order){var go=new GameObject(name,typeof(Tilemap),typeof(TilemapRenderer));go.transform.SetParent(parent,false);go.GetComponent<TilemapRenderer>().sortingOrder=order;return go.GetComponent<Tilemap>();}

        public Vector3Int WorldToCell(Vector3 world)=>Grid.WorldToCell(world);
        public bool SetCell(CreaJuegoProjectData project,Vector3Int position,string tileId,bool erase)
        {
            if(project==null)return false;var definition=erase?null:palette.Find(tileId);if(!erase&&(definition==null||definition.tile==null))return false;
            var existing=FindCells(project,position).ToArray();if(erase)
            {
                if(existing.Length==0)return false;RemoveCells(existing);ClearVisualCell(position);RefreshAround(position);return true;
            }
            if(existing.Length==1&&existing[0].cell.tileId==definition.id)return false;
            RemoveCells(existing);ClearVisualCell(position);var layer=Layer(project,definition.layer);layer.cells.Add(new RuntimeTileCellData{x=position.x,y=position.y,tileId=definition.id});
            maps[definition.layer].SetTile(position,definition.tile);underlay.SetTile(position,definition.underlayTile);RefreshAround(position);return true;
        }

        static IEnumerable<(RuntimeTileLayerData layer,RuntimeTileCellData cell)> FindCells(CreaJuegoProjectData project,Vector3Int position)
        {
            foreach(var layer in project.tileLayers??new List<RuntimeTileLayerData>())
            {
                if(layer?.cells==null)continue;foreach(var cell in layer.cells.Where(cell=>cell.x==position.x&&cell.y==position.y))yield return (layer,cell);
            }
        }
        static void RemoveCells(IEnumerable<(RuntimeTileLayerData layer,RuntimeTileCellData cell)> cells){foreach(var value in cells)value.layer.cells.Remove(value.cell);}
        void ClearVisualCell(Vector3Int position){foreach(var map in maps.Values)map.SetTile(position,null);underlay.SetTile(position,null);}
        void RefreshAround(Vector3Int position)
        {
            foreach(var map in maps.Values)for(int y=-1;y<=1;y++)for(int x=-1;x<=1;x++)map.RefreshTile(position+new Vector3Int(x,y,0));
            underlay.RefreshTile(position);
        }
        static RuntimeTileLayerData Layer(CreaJuegoProjectData project,RuntimeTileLayerRole role)
        {
            project.tileLayers??=new List<RuntimeTileLayerData>();var id=LayerId(role);var layer=project.tileLayers.FirstOrDefault(value=>value.id==id);if(layer!=null)return layer;layer=new RuntimeTileLayerData{id=id};project.tileLayers.Add(layer);return layer;
        }
        public static string LayerId(RuntimeTileLayerRole role)=>role==RuntimeTileLayerRole.OneWay?"plataformas":role==RuntimeTileLayerRole.Decoration?"decoracion":"terreno";
    }
}
