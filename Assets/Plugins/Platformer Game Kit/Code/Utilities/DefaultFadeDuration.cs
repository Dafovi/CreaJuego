// Platformer Game Kit // https://kybernetik.com.au/platformer // Copyright 2021-2025 Kybernetik //

namespace Animancer
{
    /// <summary>Sets the <see cref="AnimancerGraph.DefaultFadeDuration"/> to 0 on startup (editor and runtime).</summary>
    internal static class DefaultFadeDuration
    {
        /************************************************************************************************************************/

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
            => AnimancerGraph.DefaultFadeDuration = 0;

        /************************************************************************************************************************/
    }
}
