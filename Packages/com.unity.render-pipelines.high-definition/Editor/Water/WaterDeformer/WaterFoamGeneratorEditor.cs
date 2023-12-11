using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(WaterFoamGenerator))]
    sealed partial class WaterFoamGeneratorEditor : Editor
    {
        static readonly Color k_HandleColor = new Color(0 / 255f, 0xE5 / 255f, 0xFF / 255f, 1f).gamma;

        SerializedProperty m_Type;
        SerializedProperty m_RegionSize;
        SerializedProperty m_Texture;
        SerializedProperty m_SurfaceFoamDimmer;
        SerializedProperty m_DeepFoamDimmer;
        SerializedProperty m_ScaleMode;

        // Material params
        SerializedProperty m_Resolution;
        SerializedProperty m_UpdateMode;
        SerializedProperty m_Material;
        Editor m_MaterialEditor;

        HierarchicalBox m_BoxHandle;

        void OnEnable()
        {
            var o = new PropertyFetcher<WaterFoamGenerator>(serializedObject);
            m_Type = o.Find(x => x.type);
            m_RegionSize = o.Find(x => x.regionSize);
            m_Texture = o.Find(x => x.texture);
            m_SurfaceFoamDimmer = o.Find(x => x.surfaceFoamDimmer);
            m_DeepFoamDimmer = o.Find(x => x.deepFoamDimmer);
            m_ScaleMode = o.Find(x => x.scaleMode);

            // Material parameters
            m_Resolution = o.Find(x => x.resolution);
            m_UpdateMode = o.Find(x => x.updateMode);
            m_Material = o.Find(x => x.material);

            m_BoxHandle = new HierarchicalBox(k_HandleColor, new[] { k_HandleColor, k_HandleColor, k_HandleColor, k_HandleColor, k_HandleColor, k_HandleColor })
            {
                monoHandle = false,
                allowNegativeSize = true,
            };
        }

        void OnDisable()
        {
            UnityEngine.Rendering.CoreUtils.Destroy(m_MaterialEditor);
        }

        static public readonly GUIContent k_TypeText = EditorGUIUtility.TrTextContent("Type", "Specifies the type of the foam generator.");
        static public readonly GUIContent k_RegionSizeText = EditorGUIUtility.TrTextContent("Region Size", "Sets the region size of the foam generator.");
        static public readonly GUIContent k_TextureText = EditorGUIUtility.TrTextContent("Texture", "Specifies the texture used to generate the foam. The red channel holds the surface foam and the green channel holds the deep foam.");
        static public readonly GUIContent k_SurfaceFoamDimmerText = EditorGUIUtility.TrTextContent("Surface Foam Dimmer", "Specifies the dimmer for the surface foam.");
        static public readonly GUIContent k_DeepFoamDimmerText = EditorGUIUtility.TrTextContent("Deep Foam Dimmer", "Specifies the dimmer for the deep foam.");

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_ScaleMode);
            EditorGUILayout.PropertyField(m_RegionSize, k_RegionSizeText);

            EditorGUILayout.PropertyField(m_Type, k_TypeText);
            if (!m_Type.hasMultipleDifferentValues)
            {
                using (new EditorGUI.IndentLevelScope())
                    SurfaceTypeGUI();
            }

            serializedObject.ApplyModifiedProperties();
        }

        void CommonFields()
        {
            EditorGUILayout.PropertyField(m_SurfaceFoamDimmer, k_SurfaceFoamDimmerText);
            EditorGUILayout.PropertyField(m_DeepFoamDimmer, k_DeepFoamDimmerText);
        }

        public void SurfaceTypeGUI()
        {
            WaterFoamGeneratorType type = (WaterFoamGeneratorType)m_Type.enumValueIndex;
            switch (type)
            {
                case WaterFoamGeneratorType.Texture:
                    EditorGUILayout.PropertyField(m_Texture, k_TextureText);
                    CommonFields();
                    break;

                case WaterFoamGeneratorType.Material:
                    WaterSurfaceEditor.MaterialFieldWithButton(null, m_Material, () => {
                        return WaterDeformerEditor.CreateDefaultDecalMaterial(target as MonoBehaviour);
                    });

                    if (m_Material.objectReferenceValue == null)
                        break;

                    if ((target as WaterFoamGenerator).IsValidMaterial())
                    {
                        WaterDeformerEditor.ResolutionField(m_Resolution, WaterDeformerEditor.k_Resolution);
                        EditorGUILayout.PropertyField(m_UpdateMode);
                        CommonFields();

                        if ((target as WaterFoamGenerator).HasPropertyBlock())
                            EditorGUILayout.HelpBox("A MaterialPropertyBlock is used to modify Material values.", MessageType.Info);

                        EditorGUILayout.Space();
                        EditorGUILayout.Space();
                        WaterDeformerEditor.MaterialInspector(m_Material, ref m_MaterialEditor);
                    }
                    else
                        EditorGUILayout.HelpBox("Foam Generators only work with Water Decal Materials.", MessageType.Error);

                    break;

                default:
                    CommonFields();
                    break;
            }
        }

        // Anis 11/09/21: Currently, there is a bug that makes the icon disappear after the first selection
        // if we do not have this. Given that the geometry is procedural, we need this to be able to
        // select the water surfaces.
        [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
        static void DrawGizmosSelected(WaterFoamGenerator foamGenerator, GizmoType gizmoType)
        {
        }

        void OnSceneGUI()
        {
            WaterFoamGenerator generator = target as WaterFoamGenerator;
            var tr = generator.transform;
            var rotation = Quaternion.Euler(0, tr.eulerAngles.y, 0);
            var regionSize = generator.regionSize;
            Vector2 scale = generator.scale;

            using (new Handles.DrawingScope(Matrix4x4.TRS(Vector3.zero, rotation, Vector3.one)))
            {
                m_BoxHandle.center = Quaternion.Inverse(rotation) * tr.position;
                m_BoxHandle.size = new Vector3(regionSize.x * scale.x, 1, regionSize.y * scale.y);
                EditorGUI.BeginChangeCheck();
                m_BoxHandle.DrawHull(true);
                m_BoxHandle.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObjects(new Object[] { tr, generator }, "Update Generator Region");
                    tr.position = rotation * m_BoxHandle.center;
                    generator.regionSize = new Vector2(m_BoxHandle.size.x / scale.x, m_BoxHandle.size.z / scale.y);
                }
            }
        }
    }
}
