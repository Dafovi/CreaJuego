// Platformer Game Kit // https://kybernetik.com.au/platformer // Copyright 2021-2025 Kybernetik //

#if UNITY_EDITOR

using Animancer.Editor;
using PlatformerGameKit.Editor;
using UnityEditor;
using UnityEngine;

namespace PlatformerGameKit
{
    /// https://kybernetik.com.au/platformer/api/PlatformerGameKit/AttackTransition
    partial class AttackTransition : ITransitionGUI
    {
        /************************************************************************************************************************/

        void ITransitionGUI.OnPreviewSceneGUI(TransitionPreviewDetails details)
            => HitData.Drawer.OnPreviewSceneGUI(ref _Hits, details, details.Animancer.States.GetOrCreate(this));

        /************************************************************************************************************************/

        void ITransitionGUI.OnTimelineBackgroundGUI()
            => HitData.Drawer.OnTimelineBackgroundGUI(_Hits);

        /************************************************************************************************************************/

        void ITransitionGUI.OnTimelineForegroundGUI()
            => HitData.Drawer.OnTimelineForegroundGUI(_Hits);

        /************************************************************************************************************************/

        /// <inheritdoc/>
        [CustomPropertyDrawer(typeof(AttackTransition), true)]
        public class Drawer : ClipTransitionDrawer
        {
            /************************************************************************************************************************/

            /// <inheritdoc/>
            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                var height = base.GetPropertyHeight(property, label);
                if (property.isExpanded)
                {
                    var lineHeight = AnimancerGUI.LineHeight + AnimancerGUI.StandardSpacing;
                    height += lineHeight * 3;
                }
                return height;
            }

            /************************************************************************************************************************/

            /// <inheritdoc/>
            protected override void DoChildPropertyGUI(ref Rect area, SerializedProperty rootProperty,
                SerializedProperty property, GUIContent label)
            {
                if (property.propertyPath.EndsWith("." + nameof(_Hits)))
                {
                    DoGroupFields(ref area);
                }

                base.DoChildPropertyGUI(ref area, rootProperty, property, label);
            }

            /************************************************************************************************************************/

            private static AttackTransition _Transition;

            private static readonly MixedValueFieldGUI<int> DamageField = new(
                (index) => _Transition.Hits[index].Damage,
                (index, value) => _Transition.Hits[index].Damage = value,
                EditorGUI.IntField);

            private static readonly MixedValueFieldGUI<float> ForceField = new(
                (index) => _Transition.Hits[index].Force,
                (index, value) => _Transition.Hits[index].Force = value,
                EditorGUI.FloatField);

            private static readonly MixedValueFieldGUI<float> AngleField = new(
                (index) => _Transition.Hits[index].Angle,
                (index, value) => _Transition.Hits[index].Angle = value,
                EditorGUI.FloatField);

            private void DoGroupFields(ref Rect area)
            {
                _Transition = Context.Transition as AttackTransition;
                if (_Transition == null)
                    return;

                var count = _Transition.Hits.Length;
                var property = Context.Property;

                using (var label = PooledGUIContent.Acquire("Damage"))
                    DamageField.Draw(area, label, count, property);

                AnimancerGUI.NextVerticalArea(ref area);

                using (var label = PooledGUIContent.Acquire("Force"))
                    ForceField.Draw(area, label, count, property);

                AnimancerGUI.NextVerticalArea(ref area);

                using (var label = PooledGUIContent.Acquire("Angle"))
                    AngleField.Draw(area, label, count, property);

                AnimancerGUI.NextVerticalArea(ref area);

                _Transition = null;
            }

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
    }
}

#endif
