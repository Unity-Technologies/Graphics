using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor
{
    /// <summary>
    /// Editor script for the Lighting Explorer.
    /// </summary>
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    public class LightExplorer : DefaultLightingExplorerExtension
    {
        private static class Styles
        {
            public static readonly GUIContent Enabled = EditorGUIUtility.TrTextContent("Enabled");
            public static readonly GUIContent Name = EditorGUIUtility.TrTextContent("Name");
            public static readonly GUIContent Mode = EditorGUIUtility.TrTextContent("Mode");

            public static readonly GUIContent HDR = EditorGUIUtility.TrTextContent("HDR");
            public static readonly GUIContent ShadowDistance = EditorGUIUtility.TrTextContent("Shadow Distance");
            public static readonly GUIContent NearPlane = EditorGUIUtility.TrTextContent("Near Plane");
            public static readonly GUIContent FarPlane = EditorGUIUtility.TrTextContent("Far Plane");
            public static readonly GUIContent Resolution = EditorGUIUtility.TrTextContent("Resolution");

            public static readonly GUIContent[] ReflectionProbeModeTitles = { EditorGUIUtility.TrTextContent("Baked"), EditorGUIUtility.TrTextContent("Realtime"), EditorGUIUtility.TrTextContent("Custom") };
            public static readonly int[] ReflectionProbeModeValues = { (int)ReflectionProbeMode.Baked, (int)ReflectionProbeMode.Realtime, (int)ReflectionProbeMode.Custom };
            public static readonly GUIContent[] ReflectionProbeSizeTitles = { EditorGUIUtility.TrTextContent("16"),
                                                                              EditorGUIUtility.TrTextContent("32"),
                                                                              EditorGUIUtility.TrTextContent("64"),
                                                                              EditorGUIUtility.TrTextContent("128"),
                                                                              EditorGUIUtility.TrTextContent("256"),
                                                                              EditorGUIUtility.TrTextContent("512"),
                                                                              EditorGUIUtility.TrTextContent("1024"),
                                                                              EditorGUIUtility.TrTextContent("2048") };
            public static readonly int[] ReflectionProbeSizeValues = { 16, 32, 64, 128, 256, 512, 1024, 2048 };


            public static readonly GUIContent SelectObjects = EditorGUIUtility.TrTextContent("");
            public static readonly GUIContent SelectObjectsButton = EditorGUIUtility.TrTextContentWithIcon("", "Find References in Scene", "UnityEditor.FindDependencies");
            public static readonly GUIContent Color = EditorGUIUtility.TrTextContent("Color");

            public static readonly GUIContent Emission = EditorGUIUtility.TrTextContent("Emission");
            // The order of the following lists have to match.
            public static readonly string[] EmissionOptions = { "Realtime Direct Emission", "Realtime Indirect Emission", "Baked Emission" };
            public static readonly MaterialGlobalIlluminationFlags[] EmissionOptionsInternal = { MaterialGlobalIlluminationFlags.RealtimeDirectEmission, MaterialGlobalIlluminationFlags.RealtimeIndirectEmission, MaterialGlobalIlluminationFlags.BakedEmission };

        }


        /// <inheritdoc />
        protected override LightingExplorerTableColumn[] Get2DLightColumns()
        {
            // Get the base columns from DefaultLightingExplorerExtension
            var baseColumns = base.Get2DLightColumns();

            // Replace the Light Type column (index 2) with our custom implementation that supports providers
            var typeHeader = EditorGUIUtility.TrTextContent("Type");
            baseColumns[2] = new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Enum, typeHeader, "m_LightType", 100, (r, prop, dep) =>
            {
                if (prop != null && prop.serializedObject.targetObject != null)
                {
                    if (prop.intValue != (int)UnityEngine.Rendering.Universal.Light2D.LightType.Parametric)
                    {
                        // Use the shared utility method that includes provider support
                        UnityEditor.Rendering.Universal.Light2DEditorUtility.DrawLightTypePopup(r, GUIContent.none, prop.serializedObject, layoutMode: false);
                    }
                    else
                    {
                        // Handle deprecated Parametric type
                        var parametricStyle = EditorGUIUtility.TrTextContentWithIcon("Parametric", "Parametric Lights have been deprecated. To continue, upgrade your Parametric Lights to Freeform Lights to enjoy similar light functionality.", MessageType.Warning);
                        EditorGUI.LabelField(r, parametricStyle);
                    }
                }
            });

            return baseColumns;
        }

        /// <inheritdoc />
        protected override LightingExplorerTableColumn[] GetReflectionProbeColumns()
        {
            return new[]
            {
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Checkbox, Styles.Enabled, "m_Enabled", 50), // 0: Enabled
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Name, Styles.Name, null, 200),  // 1: Name
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Int, Styles.Mode, "m_Mode", 70, (r, prop, dep) =>
                {
                    EditorGUI.IntPopup(r, prop, Styles.ReflectionProbeModeTitles, Styles.ReflectionProbeModeValues, GUIContent.none);
                }),     // 2: Mode
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Checkbox, Styles.HDR, "m_HDR", 35),  // 3: HDR
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Enum, Styles.Resolution, "m_Resolution", 100, (r, prop, dep) =>
                {
                    EditorGUI.IntPopup(r, prop, Styles.ReflectionProbeSizeTitles, Styles.ReflectionProbeSizeValues, GUIContent.none);
                },
                (lhs, rhs) =>
                {
                    return lhs.intValue.CompareTo(rhs.intValue);
                }), // 4: Probe Resolution
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Float, Styles.ShadowDistance, "m_ShadowDistance", 100), // 5: Shadow Distance
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Float, Styles.NearPlane, "m_NearClip", 70), // 6: Near Plane
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Float, Styles.FarPlane, "m_FarClip", 70), // 7: Far Plane
            };
        }

        /// <inheritdoc />
        protected internal override Object[] GetEmissives()
        {
            HashSet<Material> materials = new HashSet<Material>();
            foreach (var mr in GetObjectsForLightingExplorer<MeshRenderer>())
            {
                if (GameObjectUtility.AreStaticEditorFlagsSet(mr.gameObject, StaticEditorFlags.ContributeGI) && mr.sharedMaterials != null)
                {
                    foreach (var mat in  mr.sharedMaterials)
                    {
                        if (mat != null && mat.HasProperty("_EmissionColor"))
                        {
                            materials.Add(mat);
                        }
                    }
                }
            }
            Material[] res = new Material[materials.Count];
            materials.CopyTo(res);
            return res;
        }

        /// <inheritdoc />
        protected override LightingExplorerTableColumn[] GetEmissivesColumns()
        {
            return new[]
            {
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Custom, Styles.SelectObjects, "m_LightmapFlags", 20, (r, prop, dep) =>
                {
                    if (GUI.Button(r, Styles.SelectObjectsButton, "label"))
                    {
                        SearchForReferences(prop);
                    }
                }),     // 0: Icon
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Name, Styles.Name, null, 200), // 1: Name
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Int, Styles.Emission, "m_LightmapFlags", 120, (r, prop, dep) =>
                {
                    if (!prop.serializedObject.targetObject.GetType().Equals(typeof(Material)))
                        return;

                    using (new EditorGUI.DisabledScope(!IsEditable(prop.serializedObject.targetObject)))
                    {
                        Material material = (Material)prop.serializedObject.targetObject;

                        int flags = 0;
                        for (int i = 0; i < Styles.EmissionOptionsInternal.Length; i++)
                        {
                            if ((material.globalIlluminationFlags & Styles.EmissionOptionsInternal[i]) != MaterialGlobalIlluminationFlags.None)
                                flags |= 1 << i;
                        }

                        EditorGUI.BeginChangeCheck();
                        flags = EditorGUI.MaskField(r, flags, Styles.EmissionOptions);
                        var globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                        for (int i = 0; i < Styles.EmissionOptions.Length; i++)
                        {
                            if((flags & 1 << i) != 0)
                                globalIlluminationFlags |= Styles.EmissionOptionsInternal[i];
                        }
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(material, $"Modify Emission Flags of {material.name}");
                            material.globalIlluminationFlags = globalIlluminationFlags;
                            EditorUtility.SetDirty(material);
                        }

                    }
                }),     // 2: GI
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Custom, Styles.Color, "m_Shader", 120, (r, prop, dep) =>
                {
                    if (!prop.serializedObject.targetObject.GetType().Equals(typeof(Material)))
                        return;

                    using (new EditorGUI.DisabledScope(!IsEditable(prop.serializedObject.targetObject)))
                    {
                        Material material = (Material)prop.serializedObject.targetObject;

                        Color color = material.GetColor("_EmissionColor");

                        EditorGUI.BeginProperty(r, GUIContent.none, prop);
                        EditorGUI.BeginChangeCheck();
                        Color newValue = EditorGUI.ColorField(r, GUIContent.none, color, true, false, true);

                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(material, $"Modify Emission Color of {material.name}");
                            material.SetColor("_EmissionColor", newValue);
                            EditorUtility.SetDirty(material);
                        }
                        EditorGUI.EndProperty();
                    }
                }, (lhs, rhs) =>
                    {
                        float lh, ls, lv, rh, rs, rv;
                        Color.RGBToHSV(((Material)lhs.serializedObject.targetObject).GetColor("_EmissionColor"), out lh, out ls, out lv);
                        Color.RGBToHSV(((Material)rhs.serializedObject.targetObject).GetColor("_EmissionColor"), out rh, out rs, out rv);
                        return lv.CompareTo(rv);
                    }, (target, source) =>
                    {
                        Material sourceMaterial = (Material)source.serializedObject.targetObject;
                        Color color = sourceMaterial.GetColor("_EmissionColor");

                        Material targetMaterial = (Material)target.serializedObject.targetObject;
                        Undo.RecordObject(targetMaterial, $"Modify Emission Flags of {targetMaterial.name}");
                        targetMaterial.SetColor("_EmissionColor", color);
                        EditorUtility.SetDirty(targetMaterial);
                    }) // 3: Color
            };
        }
    }
}
