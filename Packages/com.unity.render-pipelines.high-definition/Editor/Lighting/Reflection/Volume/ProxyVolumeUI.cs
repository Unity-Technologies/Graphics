using System;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    using CED = CoreEditorDrawer<SerializedProxyVolume>;

    class ProxyVolumeUI
    {
        internal static GUIContent shapeContent = L10n.TextContent("Shape", "The shape of the Proxy.\nInfinite is compatible with any kind of InfluenceShape.", null, null);
        internal static GUIContent boxSizeContent = L10n.TextContent("Box Size", "The size of the box.", null, null);
        internal static GUIContent sphereRadiusContent = L10n.TextContent("Sphere Radius", "The radius of the sphere.", null, null);

        public static readonly CED.IDrawer SectionShape = CED.Group((serialized, owner) =>
        {
            if (serialized.shape.hasMultipleDifferentValues)
            {
                EditorGUI.showMixedValue = true;
                EditorGUILayout.PropertyField(serialized.shape, shapeContent);
                EditorGUI.showMixedValue = false;
                return;
            }
            else
                EditorGUILayout.PropertyField(serialized.shape, shapeContent);

            switch ((ProxyShape)serialized.shape.intValue)
            {
                case ProxyShape.Box:
                    EditorGUILayout.PropertyField(serialized.boxSize, boxSizeContent);
                    break;
                case ProxyShape.Sphere:
                    EditorGUILayout.PropertyField(serialized.sphereRadius, sphereRadiusContent);
                    break;
                case ProxyShape.Infinite:
                    break;
                default: throw new ArgumentOutOfRangeException();
            }
        });
    }
}
