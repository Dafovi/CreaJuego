using System;
using UnityEngine;

namespace CreaJuego
{
    public enum ItemKind { Player, Platform, Prize, Hazard, Goal, MovingPlatform, Enemy, Decoration }
    public enum EducationalControl { Float, Integer, Toggle, Text, Color }

    [Serializable]
    public sealed class EducationalProperty
    {
        public string path;
        public string label;
        public string group;
        public string unit;
        // Optional boolean on GameItem; empty means always visible.
        public string visibleWhen;
        [TextArea] public string help;
        public EducationalControl control;
        public float minimum;
        public float maximum = 10;
    }

    [CreateAssetMenu(menuName = "CreaJuego/Definición de elemento")]
    public sealed class GameItemDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        public string category;
        [TextArea] public string description;
        [TextArea] public string learningHint;
        public Sprite icon;
        public GameObject prefab;
        public ItemKind kind;
        public int order;
        public bool availableInWorkshop = true;
        public bool allowMultiple = true;
        public EducationalProperty[] properties = Array.Empty<EducationalProperty>();
    }
}
