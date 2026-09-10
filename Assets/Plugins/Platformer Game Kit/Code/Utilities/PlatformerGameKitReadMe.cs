// Platformer Game Kit // https://kybernetik.com.au/platformer // Copyright 2021-2025 Kybernetik //

#if UNITY_EDITOR

using UnityEngine;

namespace PlatformerGameKit
{
    /// <summary>[Editor-Only] A welcome screen for <see cref="PlatformerGameKit"/>.</summary>
    // [CreateAssetMenu]
    [PlatformerHelpUrl(typeof(PlatformerGameKitReadMe))]
    public class PlatformerGameKitReadMe : Animancer.Editor.ReadMe
    {
        /************************************************************************************************************************/

        /// <summary>The release ID of the current version.</summary>
        /// <example><list type="bullet">
        /// <item>[1] = v1.0: 2021-07-29.</item>
        /// <item>[2] = v1.1: 2023-04-09.</item>
        /// <item>[3] = v1.2: 2024-09-06.</item>
        /// <item>[4] = v1.2.1: 2025-09-19.</item>
        /// </list></example>
        public override int ReleaseNumber => 4;

        /// <inheritdoc/>
        public override string PrefKey => nameof(PlatformerGameKit);

        /// <inheritdoc/>
        public override string BaseProductName => Strings.ProductName;

        /// <inheritdoc/>
        public override string VersionName => "v1.2.1";

        /// <inheritdoc/>
        public override string DocumentationURL => Strings.Documentation;

        /// <inheritdoc/>
        public override string ChangeLogURL => Strings.ChangeLogPrefix;// + "/v1-2";

        /// <inheritdoc/>
        public override string SamplesURL => Strings.Samples;

        /// <inheritdoc/>
        public override string UpdateURL => Strings.LatestVersion;

        /************************************************************************************************************************/

        public PlatformerGameKitReadMe() : base(
            new LinkSection("Issues",
                "for questions, suggestions, and bug reports",
                Strings.Issues),
            new LinkSection("Discussions",
                "for general discussions, feedback, and news",
                Strings.Discussions),
            new LinkSection("Email",
                "for anything private",
                GetEmailURL(Strings.DeveloperEmail, Strings.ProductName),
                Strings.DeveloperEmail))
        { }

        /************************************************************************************************************************/
    }
}

#endif
