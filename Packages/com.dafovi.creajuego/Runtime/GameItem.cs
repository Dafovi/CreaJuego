using UnityEngine;

namespace CreaJuego
{
    // Stable educational state: the view and content never bind to vendor fields.
    [DisallowMultipleComponent]
    public sealed class GameItem : MonoBehaviour
    {
        public GameItemDefinition definition;
        [Range(.1f, 12)] public float speed = 2;
        [Range(1, 20)] public float jump = 10;
        [Range(.5f, 10)] public float distance = 3;
        [Range(1, 10)] public int health = 3;
        [Range(1, 10)] public int damage = 1;
        [Range(1, 100)] public int points = 1;
        public bool disappear = true;
        public string message = "¡Llegaste a la meta!";
        public Color tint = Color.white;
    }

    public interface IItemBackend
    {
        void ApplyConfiguration();
    }
}
