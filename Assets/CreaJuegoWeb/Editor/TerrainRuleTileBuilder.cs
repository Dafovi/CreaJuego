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

        static TerrainRuleTileBuilder(){EditorApplication.delayCall+=EnsureTerrainPalette;}

        [MenuItem("CreaJuego/Web/Actualizar terreno automático")]
        public static void EnsureTerrainPalette()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=EnsureTerrainPalette;return;}
            var sprites=AssetDatabase.LoadAllAssetsAtPath(SpriteSheetPath).OfType<Sprite>().Where(sprite=>sprite.name.StartsWith("TileGround",StringComparison.Ordinal)).ToDictionary(sprite=>sprite.name);
            if(!Enumerable.Range(1,9).All(index=>sprites.ContainsKey("TileGround"+index)))return;
            var rule=AssetDatabase.LoadAssetAtPath<RuleTile>(RuleTilePath);
            if(rule==null){rule=ScriptableObject.CreateInstance<RuleTile>();AssetDatabase.CreateAsset(rule,RuleTilePath);}
            ConfigureRuleTile(rule,sprites);

            var palette=AssetDatabase.LoadAssetAtPath<RuntimeTilePaletteDefinition>(PalettePath);
            if(palette==null){palette=ScriptableObject.CreateInstance<RuntimeTilePaletteDefinition>();AssetDatabase.CreateAsset(palette,PalettePath);}
            palette.id="bosque-plains";palette.displayName="Terreno del bosque";
            palette.tiles=new[]{new RuntimeTileDefinition{id="plains-auto-terrain",legacyIds=new[]{"plains-ground-1","plains-ground-2","plains-ground-14"},displayName="Terreno automático",icon=sprites["TileGround2"],tile=rule,underlayTile=AssetDatabase.LoadAssetAtPath<TileBase>(UnderlayPath),layer=RuntimeTileLayerRole.Solid}};
            EditorUtility.SetDirty(rule);EditorUtility.SetDirty(palette);AssetDatabase.SaveAssets();
        }

        static void ConfigureRuleTile(RuleTile rule,IReadOnlyDictionary<string,Sprite> sprites)
        {
            rule.m_DefaultSprite=sprites["TileGround5"];rule.m_DefaultColliderType=Tile.ColliderType.Grid;rule.m_TilingRules??=new List<RuleTile.TilingRule>();rule.m_TilingRules.Clear();
            for(int mask=0;mask<16;mask++)
            {
                bool up=(mask&1)!=0,right=(mask&2)!=0,down=(mask&4)!=0,left=(mask&8)!=0;
                int row=!up?0:!down?2:1;int column=!left?0:!right?2:1;var sprite=sprites["TileGround"+(row*3+column+1)];
                var tilingRule=new RuleTile.TilingRule{m_Sprites=new[]{sprite},m_ColliderType=Tile.ColliderType.Grid};
                tilingRule.ApplyNeighbors(new Dictionary<Vector3Int,int>{{Vector3Int.up,up?RuleTile.TilingRuleOutput.Neighbor.This:RuleTile.TilingRuleOutput.Neighbor.NotThis},{Vector3Int.right,right?RuleTile.TilingRuleOutput.Neighbor.This:RuleTile.TilingRuleOutput.Neighbor.NotThis},{Vector3Int.down,down?RuleTile.TilingRuleOutput.Neighbor.This:RuleTile.TilingRuleOutput.Neighbor.NotThis},{Vector3Int.left,left?RuleTile.TilingRuleOutput.Neighbor.This:RuleTile.TilingRuleOutput.Neighbor.NotThis}});
                rule.m_TilingRules.Add(tilingRule);
            }
        }
    }
}
