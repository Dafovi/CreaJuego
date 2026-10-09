using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CreaJuego.Web
{
    public enum RuntimeTileLayerRole { Solid, OneWay, Decoration }

    [Serializable]
    public sealed class RuntimeTileDefinition
    {
        public string id,displayName;
        public Sprite icon;
        public TileBase tile;
        public RuntimeTileLayerRole layer=RuntimeTileLayerRole.Solid;
        public Sprite Preview=>icon!=null?icon:(tile as Tile)?.sprite;
    }

    public sealed class RuntimeTilemapWorld
    {
        readonly RuntimeTilePaletteDefinition palette;
        readonly Dictionary<RuntimeTileLayerRole,Tilemap> maps=new Dictionary<RuntimeTileLayerRole,Tilemap>();
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
            foreach(RuntimeTileLayerRole role in Enum.GetValues(typeof(RuntimeTileLayerRole)))maps[role]=CreateLayer(root.transform,role,authoring);
            foreach(var layer in project.tileLayers??new List<RuntimeTileLayerData>())foreach(var cell in layer?.cells??new List<RuntimeTileCellData>())
            {
                var definition=palette.Find(cell.tileId);if(definition?.tile==null)continue;maps[definition.layer].SetTile(new Vector3Int(cell.x,cell.y,0),definition.tile);
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

        public Vector3Int WorldToCell(Vector3 world)=>Grid.WorldToCell(world);
        public bool SetCell(CreaJuegoProjectData project,Vector3Int position,string tileId,bool erase)
        {
            if(project==null)return false;var definition=erase?null:palette.Find(tileId);if(!erase&&(definition==null||definition.tile==null))return false;
            var existing=FindCell(project,position,out var existingLayer);if(erase)
            {
                if(existing==null)return false;existingLayer.cells.Remove(existing);foreach(var map in maps.Values)map.SetTile(position,null);return true;
            }
            if(existing!=null&&existing.tileId==definition.id)return false;
            if(existing!=null)existingLayer.cells.Remove(existing);var layer=Layer(project,definition.layer);layer.cells.Add(new RuntimeTileCellData{x=position.x,y=position.y,tileId=definition.id});
            foreach(var pair in maps)pair.Value.SetTile(position,pair.Key==definition.layer?definition.tile:null);return true;
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
