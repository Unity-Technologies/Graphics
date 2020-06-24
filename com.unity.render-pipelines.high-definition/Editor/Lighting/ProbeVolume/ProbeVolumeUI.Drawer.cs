using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.Rendering;
using UnityEditorInternal;

// TODO(Nicholas): deduplicate with DensityVolumeUI.Drawer.cs.
namespace UnityEditor.Rendering.HighDefinition
{
    using CED = CoreEditorDrawer<SerializedProbeVolume>;

    static partial class ProbeVolumeUI
    {
        [System.Flags]
        enum Expandable
        {
            Volume = 1 << 0,
            Probes = 1 << 1,
            Baking = 1 << 2
        }

        readonly static ExpandedState<Expandable, ProbeVolume> k_ExpandedStateVolume = new ExpandedState<Expandable, ProbeVolume>(Expandable.Volume, "HDRP");
        readonly static ExpandedState<Expandable, ProbeVolume> k_ExpandedStateProbes = new ExpandedState<Expandable, ProbeVolume>(Expandable.Probes, "HDRP");
        readonly static ExpandedState<Expandable, ProbeVolume> k_ExpandedStateBaking = new ExpandedState<Expandable, ProbeVolume>(Expandable.Baking, "HDRP");

        internal static readonly CED.IDrawer Inspector = CED.Group(
            CED.Group(
                Drawer_FeatureWarningMessage
                ),
            CED.Conditional(
                IsFeatureDisabled,
                Drawer_FeatureEnableInfo
                ),
            CED.Conditional(
                IsFeatureEnabled,
                CED.Group(
                    CED.FoldoutGroup(
                        Styles.k_VolumeHeader,
                        Expandable.Volume,
                        k_ExpandedStateVolume,
                        Drawer_ToolBar,
                        Drawer_AdvancedSwitch,
                        Drawer_VolumeContent
                        ),
                    CED.space,
                    CED.FoldoutGroup(
                        Styles.k_ProbesHeader,
                        Expandable.Probes,
                        k_ExpandedStateProbes,
                        Drawer_PrimarySettings
                        ),
                    CED.space,
                    CED.FoldoutGroup(
                        Styles.k_BakingHeader,
                        Expandable.Baking,
                        k_ExpandedStateBaking,
                        Drawer_BakeToolBar
                        )
                    )
                )
            );

        static bool IsFeatureEnabled(SerializedProbeVolume serialized, Editor owner)
        {
            return ShaderOptions.ProbeVolumesEvaluationMode != (int)ProbeVolumesEvaluationModes.Disabled;
        }

        static bool IsFeatureDisabled(SerializedProbeVolume serialized, Editor owner)
        {
            return ShaderOptions.ProbeVolumesEvaluationMode == (int)ProbeVolumesEvaluationModes.Disabled;
        }

        static void Drawer_FeatureWarningMessage(SerializedProbeVolume serialized, Editor owner)
        {
            EditorGUILayout.HelpBox(Styles.k_FeatureWarning, MessageType.Warning);
        }

        static void Drawer_FeatureEnableInfo(SerializedProbeVolume serialized, Editor owner)
        {
            EditorGUILayout.HelpBox(Styles.k_FeatureEnableInfo, MessageType.Error);
        }

        static void Drawer_BakeToolBar(SerializedProbeVolume serialized, Editor owner)
        {
            var asset = serialized.probeVolumeAsset.objectReferenceValue as ProbeVolumeAsset;

            if (ShaderConfig.s_ProbeVolumesBilateralFilteringMode == ProbeVolumesBilateralFilteringModes.OctahedralDepth
                && asset != null && asset.payload.dataOctahedralDepth == null)
            {
                EditorGUILayout.HelpBox(Styles.k_FeatureOctahedralDepthEnabledNoData, MessageType.Error);
            }
            
            if (ShaderConfig.s_ProbeVolumesBilateralFilteringMode != ProbeVolumesBilateralFilteringModes.OctahedralDepth
                && asset != null && asset.payload.dataOctahedralDepth != null)
            {
                EditorGUILayout.HelpBox(Styles.k_FeatureOctahedralDepthDisableYesData, MessageType.Error);
            }

            EditorGUILayout.PropertyField(serialized.probeVolumeAsset, Styles.s_DataAssetLabel);

            EditorGUILayout.Slider(serialized.backfaceTolerance, 0.0f, 1.0f, Styles.s_BackfaceToleranceLabel);
            EditorGUILayout.PropertyField(serialized.dilationIterations, Styles.s_DilationIterationLabel);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Styles.k_BakeSelectedText))
            {
                ProbeVolumeManager.BakeSelected();
            }
            GUILayout.EndHorizontal();
        }

        static void Drawer_ToolBar(SerializedProbeVolume serialized, Editor owner)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            EditMode.DoInspectorToolbar(new[] { ProbeVolumeEditor.k_EditShape, ProbeVolumeEditor.k_EditBlend }, Styles.s_Toolbar_Contents, () =>
                {
                    var bounds = new Bounds();
                    foreach (Component targetObject in owner.targets)
                    {
                        bounds.Encapsulate(targetObject.transform.position);
                    }
                    return bounds;
                },
                owner);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        static void Drawer_PrimarySettings(SerializedProbeVolume serialized, Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.drawProbes, Styles.s_DrawProbesLabel);
            EditorGUILayout.PropertyField(serialized.probeSpacingMode, Styles.s_ProbeSpacingModeLabel);
            switch ((ProbeSpacingMode)serialized.probeSpacingMode.enumValueIndex)
            {
                case ProbeSpacingMode.Density:
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.DelayedFloatField(serialized.densityX, Styles.s_DensityXLabel);
                    EditorGUILayout.DelayedFloatField(serialized.densityY, Styles.s_DensityYLabel);
                    EditorGUILayout.DelayedFloatField(serialized.densityZ, Styles.s_DensityZLabel);
                    if (EditorGUI.EndChangeCheck())
                    {
                        serialized.resolutionX.intValue = Mathf.Max(1, Mathf.RoundToInt(serialized.densityX.floatValue * serialized.size.vector3Value.x));
                        serialized.resolutionY.intValue = Mathf.Max(1, Mathf.RoundToInt(serialized.densityY.floatValue * serialized.size.vector3Value.y));
                        serialized.resolutionZ.intValue = Mathf.Max(1, Mathf.RoundToInt(serialized.densityZ.floatValue * serialized.size.vector3Value.z));
                    }
                    break;
                }

                case ProbeSpacingMode.Resolution:
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.DelayedIntField(serialized.resolutionX, Styles.s_ResolutionXLabel);
                    EditorGUILayout.DelayedIntField(serialized.resolutionY, Styles.s_ResolutionYLabel);
                    EditorGUILayout.DelayedIntField(serialized.resolutionZ, Styles.s_ResolutionZLabel);
                    if (EditorGUI.EndChangeCheck())
                    {
                        serialized.resolutionX.intValue = Mathf.Max(1, serialized.resolutionX.intValue);
                        serialized.resolutionY.intValue = Mathf.Max(1, serialized.resolutionY.intValue);
                        serialized.resolutionZ.intValue = Mathf.Max(1, serialized.resolutionZ.intValue);

                        serialized.densityX.floatValue = (float)serialized.resolutionX.intValue / Mathf.Max(1e-5f, serialized.size.vector3Value.x);
                        serialized.densityY.floatValue = (float)serialized.resolutionY.intValue / Mathf.Max(1e-5f, serialized.size.vector3Value.y);
                        serialized.densityZ.floatValue = (float)serialized.resolutionZ.intValue / Mathf.Max(1e-5f, serialized.size.vector3Value.z);
                    }
                    break;
                }

                default: break;
            }
        }

        static void Drawer_AdvancedSwitch(SerializedProbeVolume serialized, Editor owner)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                bool advanced = serialized.advancedFade.boolValue;
                advanced = GUILayout.Toggle(advanced, Styles.s_AdvancedModeContent, EditorStyles.miniButton, GUILayout.Width(70f), GUILayout.ExpandWidth(false));
                foreach (var containedBox in ProbeVolumeEditor.blendBoxes.Values)
                {
                    containedBox.monoHandle = !advanced;
                }
                if (serialized.advancedFade.boolValue ^ advanced)
                {
                    serialized.advancedFade.boolValue = advanced;
                }
            }
        }

        static void Drawer_VolumeContent(SerializedProbeVolume serialized, Editor owner)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serialized.size, Styles.s_Size);
            if (EditorGUI.EndChangeCheck())
            {
                Vector3 tmpClamp = serialized.size.vector3Value;
                tmpClamp.x = Mathf.Max(0f, tmpClamp.x);
                tmpClamp.y = Mathf.Max(0f, tmpClamp.y);
                tmpClamp.z = Mathf.Max(0f, tmpClamp.z);
                serialized.size.vector3Value = tmpClamp;
            }

            Vector3 s = serialized.size.vector3Value;
            EditorGUI.BeginChangeCheck();
            if (serialized.advancedFade.boolValue)
            {
                EditorGUI.BeginChangeCheck();
                CoreEditorUtils.DrawVector6(Styles.s_BlendLabel, serialized.positiveFade, serialized.negativeFade, Vector3.zero, s, InfluenceVolumeUI.k_HandlesColor, serialized.size);
                if (EditorGUI.EndChangeCheck())
                {
                    //forbid positive/negative box that doesn't intersect in inspector too
                    Vector3 positive = serialized.positiveFade.vector3Value;
                    Vector3 negative = serialized.negativeFade.vector3Value;
                    for (int axis = 0; axis < 3; ++axis)
                    {
                        if (positive[axis] > 1f - negative[axis])
                        {
                            if (positive == serialized.positiveFade.vector3Value)
                            {
                                negative[axis] = 1f - positive[axis];
                            }
                            else
                            {
                                positive[axis] = 1f - negative[axis];
                            }
                        }
                    }

                    serialized.positiveFade.vector3Value = positive;
                    serialized.negativeFade.vector3Value = negative;
                }
            }
            else
            {
                EditorGUI.BeginChangeCheck();
                float distanceMax = Mathf.Min(s.x, s.y, s.z);
                float uniformFadeDistance = serialized.uniformFade.floatValue * distanceMax;
                uniformFadeDistance = EditorGUILayout.FloatField(Styles.s_BlendLabel, uniformFadeDistance);
                if (EditorGUI.EndChangeCheck())
                {
                    serialized.uniformFade.floatValue = Mathf.Clamp(uniformFadeDistance / distanceMax, 0f, 0.5f);
                }
            }
            if (EditorGUI.EndChangeCheck())
            {
                Vector3 posFade = new Vector3();
                posFade.x = Mathf.Clamp01(serialized.positiveFade.vector3Value.x);
                posFade.y = Mathf.Clamp01(serialized.positiveFade.vector3Value.y);
                posFade.z = Mathf.Clamp01(serialized.positiveFade.vector3Value.z);

                Vector3 negFade = new Vector3();
                negFade.x = Mathf.Clamp01(serialized.negativeFade.vector3Value.x);
                negFade.y = Mathf.Clamp01(serialized.negativeFade.vector3Value.y);
                negFade.z = Mathf.Clamp01(serialized.negativeFade.vector3Value.z);

                serialized.positiveFade.vector3Value = posFade;
                serialized.negativeFade.vector3Value = negFade;
            }

            // Distance fade.
            {
                EditorGUI.BeginChangeCheck();

                float distanceFadeStart = EditorGUILayout.FloatField(Styles.s_DistanceFadeStartLabel, serialized.distanceFadeStart.floatValue);
                float distanceFadeEnd   = EditorGUILayout.FloatField(Styles.s_DistanceFadeEndLabel,   serialized.distanceFadeEnd.floatValue);

                if (EditorGUI.EndChangeCheck())
                {
                    distanceFadeStart = Mathf.Max(0, distanceFadeStart);
                    distanceFadeEnd   = Mathf.Max(distanceFadeStart, distanceFadeEnd);

                    serialized.distanceFadeStart.floatValue = distanceFadeStart;
                    serialized.distanceFadeEnd.floatValue   = distanceFadeEnd;
                }
            }

            EditorGUILayout.PropertyField(serialized.lightLayers);
            EditorGUILayout.PropertyField(serialized.volumeBlendMode, Styles.s_VolumeBlendModeLabel);
            EditorGUILayout.Slider(serialized.weight, 0.0f, 1.0f, Styles.s_WeightLabel);
            {
                EditorGUI.BeginChangeCheck();
                float normalBiasWS = EditorGUILayout.FloatField(Styles.s_NormalBiasWSLabel, serialized.normalBiasWS.floatValue);
                if (EditorGUI.EndChangeCheck())
                {
                    serialized.normalBiasWS.floatValue = Mathf.Max(0, normalBiasWS);
                }
            }
            EditorGUILayout.PropertyField(serialized.debugColor, Styles.s_DebugColorLabel);

            if (ShaderConfig.s_ProbeVolumesAdditiveBlending == 0 && serialized.volumeBlendMode.intValue != (int)VolumeBlendMode.Normal)
            {
                EditorGUILayout.HelpBox(Styles.k_FeatureAdditiveBlendingDisabledError, MessageType.Error);
            }
        }
    }
}
