using Playground.Attributes;
using Playground.Movement;
using Playground.Utilities;
using UnityEngine;

namespace CreaJuego.PlaygroundBackend
{
    [DefaultExecutionOrder(-200)]
    [RequireComponent(typeof(GameItem))]
    public sealed class PlaygroundAdapter : MonoBehaviour, IItemBackend
    {
        private void Awake() => ApplyConfiguration();

        public void ApplyConfiguration()
        {
            var item = GetComponent<GameItem>();
            if (TryGetComponent<SpriteRenderer>(out var renderer)) renderer.color = item.tint;
            // Playground applies force; scale educational speed to a manageable workshop range.
            if (TryGetComponent<Move>(out var move)) { move.speed = item.speed / 20f; move.movementType = Enums.MovementType.OnlyHorizontal; }
            if (TryGetComponent<Jump>(out var jump)) { jump.jumpStrength = item.jump; jump.groundTag = "Untagged"; }
            if (TryGetComponent<CollectableAttribute>(out var prize)) prize.pointsWorth = item.points;
            if (TryGetComponent<ModifyHealthAttribute>(out var hazard)) { hazard.healthChange = -item.damage; hazard.destroyWhenActivated = item.disappear; }
            if (TryGetComponent<HealthSystemAttribute>(out var health)) health.health = item.health;
            if (TryGetComponent<Patrol>(out var patrol))
            {
                patrol.speed = item.speed;
                patrol.waypoints = new[] { (Vector2)transform.position + Vector2.right * item.distance };
            }
        }
    }
}
