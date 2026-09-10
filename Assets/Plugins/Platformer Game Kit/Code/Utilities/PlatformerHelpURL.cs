// Platformer Game Kit // https://kybernetik.com.au/platformer // Copyright 2018-2025 Kybernetik //

using Animancer;
using System;
using UnityEngine;

namespace PlatformerGameKit
{
    /// <summary>[Assert-Conditional]
    /// A <see cref="HelpURLAttribute"/> which points to Platformer Game Kit's documentation.
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    [System.Diagnostics.Conditional(Animancer.Strings.Assertions)]
    public class PlatformerHelpUrlAttribute : HelpURLAttribute
    {
        /************************************************************************************************************************/

        /// <summary>Creates a new <see cref="PlatformerHelpUrlAttribute"/>.</summary>
        public PlatformerHelpUrlAttribute(string url)
            : base(url)
        { }

        /************************************************************************************************************************/

        /// <summary>Creates a new <see cref="PlatformerHelpUrlAttribute"/>.</summary>
        public PlatformerHelpUrlAttribute(Type type)
            : base(GetApiDocumentationUrl(type))
        { }

        /************************************************************************************************************************/

        /// <summary>Returns a URL for the given `type`'s API Documentation page.</summary>
        public static string GetApiDocumentationUrl(Type type)
            => GetApiDocumentationUrl(Strings.Documentation + "/api/", type);

        /// <summary>Returns a URL for the given `type`'s API Documentation page.</summary>
        public static string GetApiDocumentationUrl(string prefix, Type type)
        {
            var url = StringBuilderPool.Instance.Acquire();

            url.Append(prefix);

            if (!string.IsNullOrEmpty(type.Namespace))
                url.Append(type.Namespace).Append('/');

            url.Append(type.Name.Replace('`', '_'));

            return url.ReleaseToString();
        }

        /************************************************************************************************************************/
    }
}
