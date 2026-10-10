using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CreaJuego.Web
{
    public enum RuntimeTileLayerRole { Solid, OneWay, Decoration }
    public enum RuntimeTileBrush { Pencil, Line, Rectangle }

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
        public RuntimeTileLayerRole layer=RuntimeTileLayerRole.Solid;
        public Sprite Preview=>icon!=null?icon:(tile as Tile)?.sprite;
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
            var existing=FindCell(project,position,out var existingLayer);if(erase)
            {
                if(existing==null)return false;existingLayer.cells.Remove(existing);foreach(var map in maps.Values)map.SetTile(position,null);underlay.SetTile(position,null);return true;
            }
            if(existing!=null&&existing.tileId==definition.id)return false;
            if(existing!=null)existingLayer.cells.Remove(existing);var layer=Layer(project,definition.layer);layer.cells.Add(new RuntimeTileCellData{x=position.x,y=position.y,tileId=definition.id});
            foreach(var pair in maps)pair.Value.SetTile(position,pair.Key==definition.layer?definition.tile:null);underlay.SetTile(position,definition.underlayTile);return true;
        }

        static RuntimeTileCellData FindCell(CreaJuegoProjectData project,Vector3Int position,out RuntimeTileLayerData owner)
        {
            foreach(var layer in project.tileLayers??new List<RuntimeTileLayerData>())
            {
                var found=layer?.cells?.FirstOrDefault(cell=>cell.x==position.x&&cell.y==position.y);if(found!=null){owner=layer;return found;}
            }
            owner=null;return null;
        }
        static RuntimeTileLayerData Layer(CreaJuegoProjectData project,RuntimeTileLayerRole role)
        {
            project.tileLayers??=new List<RuntimeTileLayerData>();var id=LayerId(role);var layer=project.tileLayers.FirstOrDefault(value=>value.id==id);if(layer!=null)return layer;layer=new RuntimeTileLayerData{id=id};project.tileLayers.Add(layer);return layer;
        }
        public static string LayerId(RuntimeTileLayerRole role)=>role==RuntimeTileLayerRole.OneWay?"plataformas":role==RuntimeTileLayerRole.Decoration?"decoracion":"terreno";
    }
}
