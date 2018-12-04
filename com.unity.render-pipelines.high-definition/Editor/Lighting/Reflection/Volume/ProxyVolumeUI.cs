using System;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using CED = CoreEditorDrawer<ProxyVolumeUI, SerializedProxyVolume>;

    class ProxyVolumeUI : IUpdateable<SerializedProxyVolume>
    {
        #region Skin
        internal static GUIContent shapeContent = CoreEditorUtils.GetContent("Shape|The shape of the Proxy.\nInfinite is compatible with any kind of InfluenceShape.");
        internal static GUIContent boxSizeContent = CoreEditorUtils.GetContent("Box Size|The size of the box.");
        internal static GUIContent sphereRadiusContent = CoreEditorUtils.GetContent("Sphere Radius|The radius of the sphere.");
        #endregion

        #region Inspector
        #pragma warning disable 618
        public static readonly CED.IDrawer SectionShape = CED.Action((s, d, o) =>
        {
            if (d.shape.hasMultipleDifferentValues)
            {
                EditorGUI.showMixedValue = true;
                EditorGUILayout.PropertyField(d.shape, shapeContent);
                EditorGUI.showMixedValue = false;
                return;
            }
            else
                EditorGUILayout.PropertyField(d.shape, shapeContent);

            switch ((ProxyShape)d.shape.intValue)
            {
                case ProxyShape.Box:
                    EditorGUILayout.PropertyField(d.boxSize, boxSizeContent);
                    break;
                case ProxyShape.Sphere:
                    EditorGUILayout.PropertyField(d.sphereRadius, sphereRadiusContent);
                    break;
                case ProxyShape.Infinite:
                    break;
                default: throw new ArgumentOutOfRangeException();
            }
        });
        #pragma warning restore 618
        #endregion

        #region Body
        public void Update(SerializedProxyVolume data) { }
        #endregion
    }
}
