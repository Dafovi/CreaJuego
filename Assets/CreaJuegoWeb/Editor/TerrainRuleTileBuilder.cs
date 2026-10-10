using System;
using System.Collections.Generic;
using System.Linq;
using CreaJuego.Web;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CreaJuego.Web.Editor
{
    [InitializeOnLoad]
    public static class TerrainRuleTileBuilder
    {
        const string PalettePath="Assets/CreaJuegoWeb/Content/TerrainTiles.asset";
        const string RuleTilePath="Assets/CreaJuegoWeb/Content/PlainsTerrainRule.asset";
        const string SpriteSheetPath="Assets/2D Pixel Art Platformer Biome - Plains/Sprites.png";
        const string UnderlayPath="Assets/2D Pixel Art Platformer Biome - Plains/Tilemap/TileBackGround5.asset";
        const string VillageSheetPath="Assets/Cainos/Pixel Art Platformer - Village Props/Texture/TX Tileset Ground.png";
        const string VillageRulePath="Assets/CreaJuegoWeb/Content/VillageTerrainRule.asset";
        const string SkullTilePath="Assets/NovaDevs/2D Platformer - Skull Garden  Tilesets Asset Pack/Tile Palette/Platform 1 Palette/Skull Garden Tiles_96x96 Sprite Sheet_1.asset";
        const string CaveDirectory="Assets/Cave Platformer Tileset/Individual PNG files/Tileset/cave_tileset";

        static TerrainRuleTileBuilder(){EditorApplication.delayCall+=EnsureTerrainPalette;}

        [MenuItem("CreaJuego/Web/Actualizar terreno automático")]
        public static void EnsureTerrainPalette()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=EnsureTerrainPalette;return;}
            var palette=AssetDatabase.LoadAssetAtPath<RuntimeTilePaletteDefinition>(PalettePath);
            if(palette==null){palette=ScriptableObject.CreateInstance<RuntimeTilePaletteDefinition>();AssetDatabase.CreateAsset(palette,PalettePath);}
            palette.id="creajuego-terrenos";palette.displayName="Terrenos de CreaJuego";var definitions=new List<RuntimeTileDefinition>();
            AddPlains(definitions);AddVillage(definitions);AddSkullGarden(definitions);AddCave(definitions);
            if(definitions.Count==0)return;palette.tiles=definitions.ToArray();EditorUtility.SetDirty(palette);AssetDatabase.SaveAssets();
        }

        static void AddPlains(ICollection<RuntimeTileDefinition> definitions)
        {
            var sprites=AssetDatabase.LoadAllAssetsAtPath(SpriteSheetPath).OfType<Sprite>().Where(sprite=>sprite.name.StartsWith("TileGround",StringComparison.Ordinal)).ToDictionary(sprite=>sprite.name);
            if(!Enumerable.Range(1,9).All(index=>sprites.ContainsKey("TileGround"+index)))return;var ordered=Enumerable.Range(1,9).Select(index=>sprites["TileGround"+index]).ToArray();var rule=EnsureRuleTile(RuleTilePath,ordered);
            definitions.Add(new RuntimeTileDefinition{id="plains-auto-terrain",legacyIds=new[]{"plains-ground-1","plains-ground-2","plains-ground-14"},displayName="Bosque",theme="Naturaleza",icon=ordered[1],tile=rule,underlayTile=AssetDatabase.LoadAssetAtPath<TileBase>(UnderlayPath),layer=RuntimeTileLayerRole.Solid});
        }

        static void AddVillage(ICollection<RuntimeTileDefinition> definitions)
        {
            var sprites=AssetDatabase.LoadAllAssetsAtPath(VillageSheetPath).OfType<Sprite>().ToDictionary(sprite=>sprite.name);var ordered=Enumerable.Range(0,9).Select(index=>sprites.TryGetValue("TX Tileset Ground_"+index,out var sprite)?sprite:null).ToArray();
            if(ordered.Any(sprite=>sprite==null))return;var rule=EnsureRuleTile(VillageRulePath,ordered);definitions.Add(new RuntimeTileDefinition{id="village-auto-terrain",displayName="Aldea",theme="Aldea",icon=ordered[1],tile=rule,layer=RuntimeTileLayerRole.Solid});
        }

        static void AddSkullGarden(ICollection<RuntimeTileDefinition> definitions)
        {
            var source=AssetDatabase.LoadAssetAtPath<Tile>(SkullTilePath);if(source?.sprite==null)return;var tile=EnsureScaledTile("Assets/CreaJuegoWeb/Content/SkullGardenTile.asset",source.sprite,source.sprite.pixelsPerUnit/source.sprite.rect.width);
            definitions.Add(new RuntimeTileDefinition{id="skull-garden-stone",displayName="Piedra oscura",theme="Jardín de calaveras",icon=source.sprite,tile=tile,layer=RuntimeTileLayerRole.Solid});
        }

        static void AddCave(ICollection<RuntimeTileDefinition> definitions)
        {
            AddCaveTile(definitions,1,"cave-gray","Roca gris");AddCaveTile(definitions,33,"cave-brown","Roca marrón");
        }

        static void AddCaveTile(ICollection<RuntimeTileDefinition> definitions,int index,string id,string label)
        {
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>($"{CaveDirectory}/main_tiles_16x16_{index}.png");if(sprite==null)return;var tile=EnsureScaledTile($"Assets/CreaJuegoWeb/Content/{id}.asset",sprite,sprite.pixelsPerUnit/sprite.rect.width);
            definitions.Add(new RuntimeTileDefinition{id=id,displayName=label,theme="Cueva",icon=sprite,tile=tile,layer=RuntimeTileLayerRole.Solid});
        }

        static RuleTile EnsureRuleTile(string path,IReadOnlyList<Sprite> sprites)
        {
            var rule=AssetDatabase.LoadAssetAtPath<RuleTile>(path);if(rule==null){rule=ScriptableObject.CreateInstance<RuleTile>();AssetDatabase.CreateAsset(rule,path);}ConfigureRuleTile(rule,sprites);EditorUtility.SetDirty(rule);return rule;
        }

        static Tile EnsureScaledTile(string path,Sprite sprite,float scale)
        {
            var tile=AssetDatabase.LoadAssetAtPath<Tile>(path);if(tile==null){tile=ScriptableObject.CreateInstance<Tile>();AssetDatabase.CreateAsset(tile,path);}tile.sprite=sprite;tile.colliderType=Tile.ColliderType.Grid;tile.transform=Matrix4x4.Scale(new Vector3(scale,scale,1));EditorUtility.SetDirty(tile);return tile;
        }

        static void ConfigureRuleTile(RuleTile rule,IReadOnlyList<Sprite> sprites)
        {
            rule.m_DefaultSprite=sprites[4];rule.m_DefaultColliderType=Tile.ColliderType.Grid;rule.m_TilingRules??=new List<RuleTile.TilingRule>();rule.m_TilingRules.Clear();
            for(int mask=0;mask<16;mask++)
            {
                bool up=(mask&1)!=0,right=(mask&2)!=0,down=(mask&4)!=0,left=(mask&8)!=0;
                int row=!up?0:!down?2:1;int column=!left?0:!right?2:1;var sprite=sprites[row*3+column];
                var tilingRule=new RuleTile.TilingRule{m_Sprites=new[]{sprite},m_ColliderType=Tile.ColliderType.Grid};
                tilingRule.ApplyNeighbors(new Dictionary<Vector3Int,int>{{Vector3Int.up,up?RuleTile.TilingRuleOutput.Neighbor.This:RuleTile.TilingRuleOutput.Neighbor.NotThis},{Vector3Int.right,right?RuleTile.TilingRuleOutput.Neighbor.This:RuleTile.TilingRuleOutput.Neighbor.NotThis},{Vector3Int.down,down?RuleTile.TilingRuleOutput.Neighbor.This:RuleTile.TilingRuleOutput.Neighbor.NotThis},{Vector3Int.left,left?RuleTile.TilingRuleOutput.Neighbor.This:RuleTile.TilingRuleOutput.Neighbor.NotThis}});
                rule.m_TilingRules.Add(tilingRule);
            }
        }
    }
}
