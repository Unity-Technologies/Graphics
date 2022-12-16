using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [CustomEditor(typeof(WaterFoamGenerator))]
    sealed partial class WaterFoamGeneratorEditor : Editor
    {
        SerializedProperty m_Type;
        SerializedProperty m_RegionSize;
        SerializedProperty m_Texture;
        SerializedProperty m_SurfaceFoamDimmer;
        SerializedProperty m_DeepFoamDimmer;

        void OnEnable()
        {
            var o = new PropertyFetcher<WaterFoamGenerator>(serializedObject);
            m_Type = o.Find(x => x.type);
            m_RegionSize = o.Find(x => x.regionSize);
            m_Texture = o.Find(x => x.texture);
            m_SurfaceFoamDimmer = o.Find(x => x.surfaceFoamDimmer);
            m_DeepFoamDimmer = o.Find(x => x.deepFoamDimmer);
        }

        static public readonly GUIContent k_TypeText = EditorGUIUtility.TrTextContent("Type", "Specifies the type of the foam generator.");
        static public readonly GUIContent k_RegionSizeText = EditorGUIUtility.TrTextContent("Region Size", "Sets the region size of the foam generator.");
        static public readonly GUIContent k_TextureText = EditorGUIUtility.TrTextContent("Texture", "Specifies the texture used to generate the foam. The red channel holds the surface foam and the green channel holds the deep foam.");
        static public readonly GUIContent k_SurfaceFoamDimmerText = EditorGUIUtility.TrTextContent("Surface Foam Dimmer", "Specifies the dimmer for the surface foam.");
        static public readonly GUIContent k_DeepFoamDimmerText = EditorGUIUtility.TrTextContent("Deep Foam Dimmer", "Specifies the dimmer for the deep foam.");

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_Type, k_TypeText);
            EditorGUILayout.PropertyField(m_RegionSize, k_RegionSizeText);

            WaterFoamGeneratorType type = (WaterFoamGeneratorType)m_Type.enumValueIndex;
            if (type == WaterFoamGeneratorType.Texture)
            {
                EditorGUILayout.PropertyField(m_Texture, k_TextureText);
            }

            m_SurfaceFoamDimmer.floatValue = EditorGUILayout.Slider(k_SurfaceFoamDimmerText, m_SurfaceFoamDimmer.floatValue, 0.0f, 1.0f);
            m_DeepFoamDimmer.floatValue = EditorGUILayout.Slider(k_DeepFoamDimmerText, m_DeepFoamDimmer.floatValue, 0.0f, 1.0f);

            serializedObject.ApplyModifiedProperties();
        }

        // Anis 11/09/21: Currently, there is a bug that makes the icon disappear after the first selection
        // if we do not have this. Given that the geometry is procedural, we need this to be able to
        // select the water surfaces.
        [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
        static void DrawGizmosSelected(WaterFoamGenerator foamGenerator, GizmoType gizmoType)
        {
        }
    }
}
