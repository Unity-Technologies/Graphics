using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Scripting.APIUpdating;
using static Unity.Rendering.Universal.ShaderUtils;

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

            public static readonly GUIContent Emission = EditorGUIUtility.TrTextContent("Emission");
            // The order of the following lists have to match. Unlike the default light explorer, URP also
            // exposes the realtime direct emission flag, which drives the _EMISSION keyword of URP shaders.
            public static readonly string[] EmissionOptions = { L10n.Tr("Realtime Direct Emission", null), L10n.Tr("Realtime Indirect Emission", null), L10n.Tr("Baked Emission", null) };
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
                    foreach (var mat in mr.sharedMaterials)
                    {
                        if (mat != null && mat.HasProperty("_EmissionColor")
                            && (mat.globalIlluminationFlags & (MaterialGlobalIlluminationFlags.RealtimeDirectEmission | MaterialGlobalIlluminationFlags.RealtimeIndirectEmission | MaterialGlobalIlluminationFlags.BakedEmission)) != MaterialGlobalIlluminationFlags.None)
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
            // Reuse the base columns (icon, name and color) and only replace the emission flags column,
            // since URP additionally exposes the realtime direct emission flag.
            var columns = base.GetEmissivesColumns();

            columns[2] = new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Int, Styles.Emission, "m_LightmapFlags", 120, (r, prop, dep) =>
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

                        EditorGUI.BeginProperty(r, GUIContent.none, prop);
                        EditorGUI.BeginChangeCheck();

                        flags = EditorGUI.MaskField(r, flags, Styles.EmissionOptions);

                        if (EditorGUI.EndChangeCheck())
                        {
                            // Only touch the emission flags, so other flags like EmissiveIsBlack are preserved.
                            MaterialGlobalIlluminationFlags giFlags = material.globalIlluminationFlags;
                            for (int i = 0; i < Styles.EmissionOptionsInternal.Length; i++)
                            {
                                if ((flags & 1 << i) != 0)
                                    giFlags |= Styles.EmissionOptionsInternal[i];
                                else
                                    giFlags &= ~Styles.EmissionOptionsInternal[i];
                            }

                            Undo.RecordObject(material, $"Modify Emission Flags of {material.name}");
                            material.globalIlluminationFlags = giFlags;
                            EditorUtility.SetDirty(material);

                            // Update the material's keywords to match the changed flags, e.g. _EMISSION.
                            UpdateMaterial(material, MaterialUpdateType.ModifiedMaterial);

                            prop.serializedObject.Update();
                        }
                        EditorGUI.EndProperty();
                    }
                }, copyDelegate: (target, source) =>
                    {
                        Material targetMaterial = (Material)target.serializedObject.targetObject;
                        if (!IsEditable(targetMaterial))
                            return;

                        Material sourceMaterial = (Material)source.serializedObject.targetObject;

                        // Only copy the emission flags exposed in the UI, so flags like EmissiveIsBlack stay derived from the target's own state.
                        MaterialGlobalIlluminationFlags giFlags = targetMaterial.globalIlluminationFlags;
                        for (int i = 0; i < Styles.EmissionOptionsInternal.Length; i++)
                        {
                            if ((sourceMaterial.globalIlluminationFlags & Styles.EmissionOptionsInternal[i]) != MaterialGlobalIlluminationFlags.None)
                                giFlags |= Styles.EmissionOptionsInternal[i];
                            else
                                giFlags &= ~Styles.EmissionOptionsInternal[i];
                        }

                        Undo.RecordObject(targetMaterial, $"Modify Emission Flags of {targetMaterial.name}");
                        targetMaterial.globalIlluminationFlags = giFlags;
                        EditorUtility.SetDirty(targetMaterial);

                        // Update the material's keywords to match the changed flags, e.g. _EMISSION.
                        UpdateMaterial(targetMaterial, MaterialUpdateType.ModifiedMaterial);

                        target.serializedObject.Update();
                    }); // 2: GI

            return columns;
        }
    }
}
