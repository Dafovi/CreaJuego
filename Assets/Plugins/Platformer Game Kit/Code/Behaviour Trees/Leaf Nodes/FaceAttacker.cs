// Platformer Game Kit // https://kybernetik.com.au/platformer // Copyright 2021-2025 Kybernetik //

using PlatformerGameKit.Characters;
using System;

namespace PlatformerGameKit.BehaviourTrees
{
    /// <summary>
    /// A <see cref="LeafNode"/> which causes the character to turn towards the attacker whenever they get hit.
    /// </summary>
    /// <remarks>
    /// <strong>Documentation:</strong>
    /// <see href="https://kybernetik.com.au/platformer/docs/characters/brains/behaviour/specific#leaves">
    /// Behaviour Tree Brains - Leaves</see>
    /// </remarks>
    /// https://kybernetik.com.au/platformer/api/PlatformerGameKit.BehaviourTrees/FaceAttacker
    /// 
    [Serializable]
    public class FaceAttacker : LeafNode
    {
        /************************************************************************************************************************/

        private Character _Character;

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override Result Execute()
        {
            _Character = Context<Character>.Current;
            _Character.Health.OnHitReceived += TurnToFaceAttacker;
            return Result.Pass;
        }

        /************************************************************************************************************************/

        /// <summary>Turns the character to fade the <see cref="Hit.source"/>.</summary>
        private void TurnToFaceAttacker(Hit hit)
        {
            if (hit.source == null)
                return;

            var direction = hit.source.position.x - _Character.Body.Position.x;
            if (direction == 0)
                return;

            _Character.MovementDirectionX = direction > 0 ? 1 : -1;
        }

        /************************************************************************************************************************/
    }
}
