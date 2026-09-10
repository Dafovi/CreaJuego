// Platformer Game Kit // https://kybernetik.com.au/platformer // Copyright 2021-2025 Kybernetik //

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member.

using UnityEngine;

namespace PlatformerGameKit
{
    /// <summary>Various string constants used throughout the <see cref="PlatformerGameKit"/>.</summary>
    /// https://kybernetik.com.au/platformer/api/PlatformerGameKit/Strings
    /// 
    public static class Strings
    {
        /************************************************************************************************************************/

        /// <summary>The name of this product.</summary>
        public const string ProductName = "Platformer Game Kit";

        /// <summary>The standard prefix for <see cref="AddComponentMenu"/>.</summary>
        public const string MenuPrefix = ProductName + "/";

        /// <summary>The URL of the website where the Platformer Game Kit documentation is hosted.</summary>
        public const string Documentation = "https://kybernetik.com.au/platformer";

        /// <summary>The URL of the website where the Platformer Game Kit API documentation is hosted.</summary>
        public const string APIDocumentation = Documentation + "/api/" + nameof(PlatformerGameKit);

        public const string Docs = Documentation + "/docs/";

        /// <summary>The URL of the website where the Platformer Game Kit sample documentation is hosted.</summary>
        public const string Samples = Docs + "scenes";

        public const string ChangeLogPrefix = Docs + "changes";

        public const string Discussions = "https://discussions.unity.com/t/850162";

        public const string Issues = "https://github.com/KybernetikGames/platformer/issues";

        /// <summary>The email address which handles support for the Platformer Game Kit.</summary>
        public const string DeveloperEmail = "mail@kybernetik.com.au";

#if UNITY_EDITOR
        /// <summary>[Editor-Only] A common [<see cref="TooltipAttribute.tooltip"/>] for Debug Line Duration fields.</summary>
        public const string DebugLineDurationTooltip =
            "[Editor-Only] Determines how long scene view debug lines are shown for this object.";
#endif

        /// <summary>The URL of the file which lists the Platformer Game Kit's latest version.</summary>
        public const string LatestVersion = Documentation + "/latest-version.txt";

        /************************************************************************************************************************/

        /// <summary>Tooltips for various fields.</summary>
        public static class Tooltips
        {
            public const string HitAngle = "The direction in which the knockback force is applied (in degrees)" +
                "\n• 0 = Forward" +
                "\n• 90 = Up" +
                "\n• 180 = Backward" +
                "\n• -90 = Down";
        }

        /************************************************************************************************************************/
    }
}
