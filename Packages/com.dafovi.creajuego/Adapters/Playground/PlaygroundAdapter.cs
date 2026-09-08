using Playground.Attributes;
using Playground.Conditions;
using Playground.Movement;
using Playground.Utilities;
using UnityEngine;

namespace CreaJuego.PlaygroundBackend
{
    [DefaultExecutionOrder(-200)]
    [RequireComponent(typeof(GameItem))]
    public sealed class PlaygroundAdapter : MonoBehaviour, IItemBackend, IBackendValidation
    {
        private void Awake() => ApplyConfiguration();
        private void Start() => ApplyConfiguration();
        public void ApplyConfiguration()
        {
            var item = GetComponent<GameItem>();
            var session = Application.isPlaying ? DemoSession.InScene(gameObject.scene) : null;
            if (TryGetComponent<SpriteRenderer>(out var renderer)) renderer.color = item.tint;
            if (TryGetComponent<Move>(out var move)) { move.movementSource = WorkshopInput.ReadMovement; move.speed = item.speed / 20f; move.movementType = Enums.MovementType.OnlyHorizontal; }
            if (TryGetComponent<Jump>(out var jump)) { jump.jumpStrength = item.jump; jump.enabled = item.canJump; }
            if (TryGetComponent<CollectableAttribute>(out var prize))
            {
                prize.pointsWorth = item.points;
                if (Application.isPlaying) prize.interactionAllowed = () => session != null && session.State == GameSessionState.Playing;
            }
            if (TryGetComponent<ModifyHealthAttribute>(out var hazard))
            {
                hazard.healthChange = -item.damage; hazard.destroyWhenActivated = item.disappear;
                if (Application.isPlaying) hazard.interactionAllowed = () => session != null && session.State == GameSessionState.Playing;
            }
            if (TryGetComponent<HealthSystemAttribute>(out var health))
            {
                health.health = item.health;
                if (Application.isPlaying)
                {
                    health.modificationAllowed = () => session != null && session.State == GameSessionState.Playing;
                    health.healthChanged = remaining => { if (item.definition.kind == ItemKind.Player) session?.ObserveHealth(remaining); };
                }
            }
            if (TryGetComponent<Patrol>(out var patrol))
            {
                patrol.speed = item.speed;
                patrol.waypoints = new[] { (Vector2)transform.position + Vector2.right * item.distance };
            }
        }
        public string ConfigurationError()
        {
            var item = GetComponent<GameItem>();
            if (!isActiveAndEnabled || item.definition == null) return "Este elemento necesita un comportamiento activo.";
            if (!TryGetComponent<SpriteRenderer>(out var sprite) || sprite.sprite == null || !sprite.enabled) return "Falta la apariencia de " + item.definition.displayName + ".";
            if (item.definition.kind != ItemKind.Decoration && (!TryGetComponent<Collider2D>(out var collider) || !collider.enabled))
                return "La superficie de " + item.definition.displayName + " está incompleta.";
            switch (item.definition.kind)
            {
                case ItemKind.Player:
                    if (!CompareTag("Player") || GetComponent<Move>() == null || GetComponent<Jump>() == null ||
                        GetComponent<GroundedJumpGate>() == null || GetComponent<HealthSystemAttribute>() == null ||
                        GetComponent<Rigidbody2D>() == null || !GetComponent<Move>().enabled || !GetComponent<GroundedJumpGate>().enabled || !GetComponent<HealthSystemAttribute>().enabled || !GetComponent<Rigidbody2D>().simulated || GetComponent<Rigidbody2D>().bodyType != RigidbodyType2D.Dynamic) return "Recrea el personaje desde el catálogo para recuperar sus controles.";
                    break;
                case ItemKind.Prize:
                    if ((GetComponent<CollectableAttribute>() == null || !GetComponent<CollectableAttribute>().enabled)) return "Recrea este premio desde el catálogo.";
                    break;
                case ItemKind.Hazard:
                    if ((GetComponent<ModifyHealthAttribute>() == null || !GetComponent<ModifyHealthAttribute>().enabled)) return "Recrea este peligro desde el catálogo.";
                    break;
                case ItemKind.Enemy:
                    if (GetComponent<ModifyHealthAttribute>() == null || GetComponent<Patrol>() == null || GetComponent<Rigidbody2D>() == null || item.disappear) return "Recrea este enemigo desde el catálogo.";
                    break;
                case ItemKind.Goal:
                    var condition = GetComponent<ConditionArea>();
                    var action = GetComponent<ReachGoalAction>();
                    if (condition == null || action == null || !condition.enabled || !condition.filterByTag || condition.eventType != ConditionArea.ColliderEventTypes.Enter || condition.useCustomActions ||
                        condition.filterTag != "Player" || !condition.happenOnlyOnce || (condition.actions == null || condition.actions.Count != 1 || !condition.actions.Contains(action)))
                        return "Recrea esta meta desde el catálogo para recuperar la llegada.";
                    break;
            }
            return null;
        }
    }
}

