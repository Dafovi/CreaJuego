// Platformer Game Kit // https://kybernetik.com.au/platformer // Copyright 2021-2025 Kybernetik //

using System;

namespace PlatformerGameKit.BehaviourTrees
{
    /// <summary>
    /// A <see cref="GroupNode"/> which executes each child that returns <see cref="Result.Fail"/> until one doesn't.
    /// </summary>
    /// <remarks>
    /// Not sure whether this class description is darker than <see cref="Sequence"/> or not.
    /// <para></para>
    /// <strong>Documentation:</strong>
    /// <see href="https://kybernetik.com.au/platformer/docs/characters/brains/behaviour/general#groups">
    /// Behaviour Tree Brains - Groups</see>
    /// </remarks>
    /// https://kybernetik.com.au/platformer/api/PlatformerGameKit.BehaviourTrees/Selector
    [Serializable]
    public class Selector : GroupNode
    {
        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override Result Execute()
        {
            for (int i = 0; i < Children.Length; i++)
            {
                var behaviour = Children[i];
                if (behaviour == null)
                    continue;

                var result = behaviour.Execute();
                if (result != Result.Fail)
                    return result;
            }

            return Result.Fail;
        }

        /************************************************************************************************************************/
    }
}
