using System;
using System.Linq;
using UnityEngine;

namespace CreaJuego.Web
{
    [CreateAssetMenu(menuName="CreaJuego/Web/Paleta de tiles")]
    public sealed class RuntimeTilePaletteDefinition:ScriptableObject
    {
        public string id="terreno";
        public string displayName="Terreno";
        public RuntimeTileDefinition[] tiles=Array.Empty<RuntimeTileDefinition>();
        public RuntimeTileDefinition Find(string tileId)=>tiles?.FirstOrDefault(value=>value!=null&&value.id==tileId);
        public RuntimeTileDefinition Default=>tiles?.FirstOrDefault(value=>value!=null&&value.tile!=null);
    }
}
