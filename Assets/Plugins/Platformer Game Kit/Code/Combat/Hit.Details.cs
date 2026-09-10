// Platformer Game Kit // https://kybernetik.com.au/platformer // Copyright 2021-2025 Kybernetik //

#pragma warning disable CS0282 // There is no defined ordering between fields in multiple declarations of partial struct.

using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace PlatformerGameKit
{
    /// https://kybernetik.com.au/platformer/docs/combat/hits
    /// https://kybernetik.com.au/platformer/api/PlatformerGameKit/Hit
    public partial struct Hit
    {
        /************************************************************************************************************************/

        /// <summary>The object causing this hit.</summary>
        public Transform source;

        /// <summary>The <see cref="Team"/> that the <see cref="source"/> is on.</summary>
        public Team team;

        /// <summary>The amount of damage this hit will deal.</summary>
        public int damage;

        /// <summary>The amount of knockback force applied to the object being hit.</summary>
        public float force;

        /// <summary>The direction in which the <see cref="force"/> is applied.</summary>
        /// <remarks>This vector is normalized.</remarks>
        public Vector2 direction;

        /************************************************************************************************************************/

        /// <summary>Creates a new <see cref="Hit"/> with the specified details.</summary>
        public Hit(
            Transform source,
            Team team,
            int damage,
            float force,
            Vector2 direction,
            ICollection<ITarget> ignore = null)
        {
            target = null;
            this.source = source;
            this.team = team;
            this.damage = damage;
            this.force = force;
            this.direction = direction;
            this.ignore = ignore;
        }

        /************************************************************************************************************************/

        readonly partial void AppendDetails(StringBuilder text)
        {
            text.Append($", {nameof(source)}='").Append(source != null ? source.name : "null")
                .Append($"', {nameof(team)}=").Append(team)
                .Append($", {nameof(damage)}=").Append(damage)
                .Append($", {nameof(force)}=").Append(force);
        }

        /************************************************************************************************************************/
    }
}
