using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(WaterDecal))]
    partial class WaterDecalEditor : Editor
    {
        static readonly Color k_HandleColor = new Color(0 / 255f, 0xE5 / 255f, 0xFF / 255f, 1f).gamma;

        // General parameters
        SerializedProperty m_Size;
        SerializedProperty m_ScaleMode;

        // Material params
        SerializedProperty m_Resolution;
        SerializedProperty m_UpdateMode;
        SerializedProperty m_Material;
        Editor m_MaterialEditor;

        SerializedProperty m_Amplitude;
        SerializedProperty m_SurfaceFoamDimmer;
        SerializedProperty m_DeepFoamDimmer;

        HierarchicalBox m_BoxHandle;

        internal enum DefaultWaterDecal
        {
            Empty,
            DeformerAndFoam
        }

        void OnEnable()
        {
            var o = new PropertyFetcher<WaterDecal>(serializedObject);

            // General parameters
            m_Size = o.Find(x => x.regionSize);
            m_ScaleMode = o.Find(x => x.scaleMode);

            // Material parameters
            m_Resolution = o.Find(x => x.resolution);
            m_UpdateMode = o.Find(x => x.updateMode);
            m_Material = o.Find(x => x.material);

            m_Amplitude = o.Find(x => x.amplitude);
            m_SurfaceFoamDimmer = o.Find(x => x.surfaceFoamDimmer);
            m_DeepFoamDimmer = o.Find(x => x.deepFoamDimmer);

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

        // General parameters
        static public readonly GUIContent k_TypeText = EditorGUIUtility.TrTextContent("Type", "Specifies the type of the deformer. Shore Wave will generate foam by default without any additional Foam Generator.");
        static public readonly GUIContent k_RegionSizeText = EditorGUIUtility.TrTextContent("Size", "Controls the region size of the deformer. Outside this region, there will be no deformation.");

        // Material parameters
        static public readonly GUIContent k_Resolution = EditorGUIUtility.TrTextContent("Resolution", "Specifies the resolution when written inside the atlas.");
        static public readonly GUIContent k_Cross = EditorGUIUtility.TrTextContent("x");

        static public readonly GUIContent k_AmplitudeText = EditorGUIUtility.TrTextContent("Amplitude", "Sets the vertical amplitude of the deformation.");
        static public readonly GUIContent k_SurfaceFoamDimmerText = EditorGUIUtility.TrTextContent("Surface Foam Dimmer", "Specifies the dimmer for the surface foam.");
        static public readonly GUIContent k_DeepFoamDimmerText = EditorGUIUtility.TrTextContent("Deep Foam Dimmer", "Specifies the dimmer for the deep foam.");

        static public readonly GUIContent k_NewWaterDecalMaterialButtonText = EditorGUIUtility.TrTextContent("New", "Creates a new Water Decal shader and Material asset templates.");
        static public readonly string k_NewEmptyWaterDecalText = "Empty Water Decal";
        static public readonly string k_NewDeformerAndFoamWaterDecalText = "Deformer and Foam Water Decal";
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_ScaleMode);
            EditorGUILayout.PropertyField(m_Size, k_RegionSizeText);

            MaterialGUI();

            // Apply the properties
            serializedObject.ApplyModifiedProperties();
        }

        internal void WaterDecalMaterialFieldWithButton(SerializedProperty prop)
        {
            const int k_NewFieldWidth = 70;

            var rect = EditorGUILayout.GetControlRect();
            rect.xMax -= k_NewFieldWidth + 2;

            EditorGUI.PropertyField(rect, prop);

            var newFieldRect = rect;
            newFieldRect.x = rect.xMax + 2;
            newFieldRect.width = k_NewFieldWidth;

            if (!EditorGUI.DropdownButton(newFieldRect, k_NewWaterDecalMaterialButtonText, FocusType.Keyboard))
                return;

            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent(k_NewEmptyWaterDecalText), false, () => CreateDefaultDecalMaterial(target as MonoBehaviour, DefaultWaterDecal.Empty));
            menu.AddItem(new GUIContent(k_NewDeformerAndFoamWaterDecalText), false, () => CreateDefaultDecalMaterial(target as MonoBehaviour, DefaultWaterDecal.DeformerAndFoam));
            menu.DropDown(newFieldRect);
        }

        public void MaterialGUI()
        {
            WaterDecalMaterialFieldWithButton(m_Material);

            if (m_Material.objectReferenceValue == null)
                return;

            if ((target as WaterDecal).IsValidMaterial())
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    ResolutionField(m_Resolution, k_Resolution);
                    EditorGUILayout.PropertyField(m_UpdateMode);

                    using (new EditorGUI.DisabledScope(!WaterSystem.IsAffectingProperty(target as WaterDecal, HDShaderIDs._AffectDeformation)))
                        EditorGUILayout.PropertyField(m_Amplitude, k_AmplitudeText);
                    using (new EditorGUI.DisabledScope(!WaterSystem.IsAffectingProperty(target as WaterDecal, HDShaderIDs._AffectsFoam)))
                    {
                        EditorGUILayout.PropertyField(m_SurfaceFoamDimmer, k_SurfaceFoamDimmerText);
                        EditorGUILayout.PropertyField(m_DeepFoamDimmer, k_DeepFoamDimmerText);
                    }
                }

                if ((target as WaterDecal).HasPropertyBlock())
                    EditorGUILayout.HelpBox("A MaterialPropertyBlock is used to modify Material values.", MessageType.Info);

                EditorGUILayout.Space();
                EditorGUILayout.Space();
                MaterialInspector(m_Material, ref m_MaterialEditor);
            }
            else
                EditorGUILayout.HelpBox("Water Decals only work with Water Decal Materials.", MessageType.Error);
        }

        public static Material CreateDefaultDecalMaterial(MonoBehaviour obj, DefaultWaterDecal defaultWaterDecal)
        {
            string directory = WaterSurfaceEditor.GetWaterResourcesPath(obj);
            System.IO.Directory.CreateDirectory(directory);

            string baseName = "";
            string materialName = "";
            string path = "";
            Material material = null;

            switch (defaultWaterDecal)
            {
                case DefaultWaterDecal.Empty:
                    materialName = "New " + k_NewEmptyWaterDecalText;
                    baseName = directory + "/" + materialName;
                    path = AssetDatabase.GenerateUniqueAssetPath(baseName + ".shadergraph");
                    material = new Material(ShaderGraph.WaterDecalSubTarget.CreateWaterDecalGraphAtPath(path));
                    break;
                case DefaultWaterDecal.DeformerAndFoam:
                    materialName = "New " + k_NewDeformerAndFoamWaterDecalText;
                    baseName = directory + "/" + materialName;
                    path = AssetDatabase.GenerateUniqueAssetPath(baseName + ".shadergraph");
                    material = new Material(GraphicsSettings.GetRenderPipelineSettings<WaterSystemRuntimeResources>().waterDecalMigrationShader);
                    break;
                default:
                    Debug.LogError("Water Decal creation failed.");
                    break;
            }

            if (material != null)
            {
                material.parent = AssetDatabase.LoadAssetAtPath<Material>(path);
                AssetDatabase.CreateAsset(material, AssetDatabase.GenerateUniqueAssetPath(baseName + ".mat"));
                EditorGUIUtility.PingObject(material);

                // Setting this new material on the current water decal
                WaterDecal waterDecal = obj as WaterDecal;
                waterDecal.material = material;
            }

            return material;
        }

        static internal void MaterialInspector(SerializedProperty material, ref Editor editor)
        {
            CreateCachedEditor(material.objectReferenceValue, typeof(MaterialEditor), ref editor);

            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            editor.DrawHeader();
            using (new EditorGUI.DisabledScope((editor.target.hideFlags & HideFlags.NotEditable) != 0))
                editor.OnInspectorGUI();
            EditorGUI.indentLevel = indent;
        }

        static internal void ResolutionField(SerializedProperty resolution, GUIContent title)
        {
            int indent = EditorGUI.indentLevel;
            var rect = EditorGUILayout.GetControlRect();

            EditorGUI.BeginProperty(rect, title, resolution);
            GUILayout.BeginHorizontal();
            {
                rect = EditorGUI.PrefixLabel(rect, title);

                const int crossWidth = 14;
                const int padding = 4;

                var leftRect = rect;
                leftRect.width = rect.width / 2 - crossWidth;

                var crossRect = rect;
                crossRect.width = crossWidth;
                crossRect.x = leftRect.xMax + 2 * padding;

                var rightRect = leftRect;
                rightRect.x = crossRect.xMax + padding;

                void DelayedField(Rect rect, SerializedProperty prop)
                {
                    EditorGUI.BeginChangeCheck();
                    int value = EditorGUI.DelayedIntField(rect, prop.intValue);
                    if (EditorGUI.EndChangeCheck())
                        prop.intValue = Mathf.Clamp(value, 1, 32768);
                }

                EditorGUI.indentLevel = 0;
                DelayedField(leftRect, resolution.FindPropertyRelative("x"));
                GUI.Label(crossRect, k_Cross);
                DelayedField(rightRect, resolution.FindPropertyRelative("y"));
                EditorGUI.indentLevel = indent;
            }
            GUILayout.EndHorizontal();
            EditorGUI.EndProperty();
        }

        [MenuItem("CONTEXT/WaterDecal/Reset", false, 0)]
        static void ResetWaterDecal(MenuCommand menuCommand)
        {
            GameObject go = ((WaterDecal)menuCommand.context).gameObject;
            Assert.IsNotNull(go);

            var decal = go.GetComponent<WaterDecal>();

            Undo.RecordObject(decal, "Reset Water Decal");
            decal.Reset();
        }

        // Anis 11/09/21: Currently, there is a bug that makes the icon disappear after the first selection
        // if we do not have this. Given that the geometry is procedural, we need this to be able to
        // select the water surfaces.
        [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
        static void DrawGizmosSelected(WaterDecal waterSurface, GizmoType gizmoType)
        {
        }

        protected void OnSceneGUI()
        {
            WaterDecal decal = target as WaterDecal;
            var tr = decal.transform;
            var rotation = Quaternion.Euler(0, tr.eulerAngles.y, 0);
            var regionSize = new Vector3(decal.regionSize.x, decal.amplitude * 2, decal.regionSize.y);

            using (new Handles.DrawingScope(Matrix4x4.TRS(Vector3.zero, rotation, Vector3.one)))
            {
                Vector3 scale = decal.effectiveScale;
                m_BoxHandle.center = Quaternion.Inverse(rotation) * tr.position;
                m_BoxHandle.size = Vector3.Scale(regionSize, scale);
                EditorGUI.BeginChangeCheck();
                m_BoxHandle.DrawHull(true);
                m_BoxHandle.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObjects(new Object[] { tr, decal }, "Update Water Decal Region");
                    tr.position = rotation * m_BoxHandle.center;
                    decal.regionSize = Vector2.Max(new Vector2(m_BoxHandle.size.x / scale.x, m_BoxHandle.size.z / scale.z), Vector2.one);
                    decal.amplitude = m_BoxHandle.size.y * 0.5f / scale.y;
                }
            }
        }
    }

#pragma warning disable 618 // Type or member is obsolete
    // Kept for migration
    [CanEditMultipleObjects]
    [CustomEditor(typeof(WaterDeformer))]
    sealed partial class WaterDeformerEditor : WaterDecalEditor
    {
        [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
        static void DrawGizmosSelected(WaterDecal waterSurface, GizmoType gizmoType) { }
        new void OnSceneGUI() => base.OnSceneGUI();
    }

    [CanEditMultipleObjects]
    [CustomEditor(typeof(WaterFoamGenerator))]
    sealed partial class WaterFoamGeneratorEditor : WaterDecalEditor
    {
        [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
        static void DrawGizmosSelected(WaterDecal waterSurface, GizmoType gizmoType) { }
        new void OnSceneGUI() => base.OnSceneGUI();
    }
#pragma warning restore 618
}
