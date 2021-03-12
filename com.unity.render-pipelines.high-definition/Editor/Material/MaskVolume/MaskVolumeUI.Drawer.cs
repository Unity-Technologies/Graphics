using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEditorInternal;

// TODO(Nicholas): deduplicate with DensityVolumeUI.Drawer.cs.
namespace UnityEditor.Rendering.HighDefinition
{
    using CED = CoreEditorDrawer<SerializedMaskVolume>;

    static partial class MaskVolumeUI
    {
        [System.Flags]
        enum Expandable
        {
            Volume = 1 << 0,
            Masks = 1 << 1,
            Paint = 1 << 2
        }

        readonly static ExpandedState<Expandable, MaskVolume> k_ExpandedStateVolume = new ExpandedState<Expandable, MaskVolume>(Expandable.Volume, "HDRP");
        readonly static ExpandedState<Expandable, MaskVolume> k_ExpandedStateMasks = new ExpandedState<Expandable, MaskVolume>(Expandable.Masks, "HDRP");
        readonly static ExpandedState<Expandable, MaskVolume> k_ExpandedStatePaint = new ExpandedState<Expandable, MaskVolume>(Expandable.Paint, "HDRP");

        internal static readonly CED.IDrawer Inspector = CED.Group(
            CED.Conditional(
                IsFeatureDisabled,
                Drawer_FeatureEnableInfo
                ),
            CED.FoldoutGroup(
                Styles.k_PaintHeader,
                Expandable.Paint,
                k_ExpandedStatePaint,
                Drawer_Paint
                ),
            CED.FoldoutGroup(
                Styles.k_VolumeHeader,
                Expandable.Volume,
                k_ExpandedStateVolume,
                Drawer_VolumeContent
                ),
            CED.FoldoutGroup(
                Styles.k_MasksHeader,
                Expandable.Masks,
                k_ExpandedStateMasks,
                Drawer_PrimarySettings
                )
            );

        static bool IsFeatureEnabled(SerializedMaskVolume serialized, Editor owner)
        {
            var pipelineAsset = HDRenderPipeline.currentAsset;
            return pipelineAsset != null && pipelineAsset.currentPlatformRenderPipelineSettings.supportMaskVolume;
        }

        static bool IsFeatureDisabled(SerializedMaskVolume serialized, Editor owner)
        {
            return !IsFeatureEnabled(serialized, owner);
        }

        static void Drawer_FeatureEnableInfo(SerializedMaskVolume serialized, Editor owner)
        {
            EditorGUILayout.HelpBox(Styles.k_FeatureEnableInfo, MessageType.Error);

            var pipelineAsset = HDRenderPipeline.currentAsset;
            if (pipelineAsset != null && GUILayout.Button("Select Pipeline Asset"))
                Selection.activeObject = pipelineAsset;

            EditorGUILayout.Space();
        }

        static void Drawer_Paint(SerializedMaskVolume serialized, Editor owner)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            EditMode.DoInspectorToolbar(new[] {MaskVolumeEditor.k_EditPaint}, Styles.s_PaintToolbarContents, () =>
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

            EditorGUILayout.Space();

            var brushColor = MaskVolumeEditor.BrushColor;
            ChannelField(ref MaskVolumeEditor.BrushApplyRed, ref brushColor.r, "R");
            ChannelField(ref MaskVolumeEditor.BrushApplyGreen, ref brushColor.g, "G");
            ChannelField(ref MaskVolumeEditor.BrushApplyBlue, ref brushColor.b, "B");
            brushColor.a = (byte) EditorGUILayout.IntSlider("Opacity", brushColor.a, 0, 255);
            MaskVolumeEditor.BrushColor = brushColor;

            EditorGUILayout.Space();

            MaskVolumeEditor.Brush.Radius = EditorGUILayout.FloatField("Radius", MaskVolumeEditor.Brush.Radius);
            if (MaskVolumeEditor.Brush.Radius < float.Epsilon)
                MaskVolumeEditor.Brush.Radius = float.Epsilon;
            MaskVolumeEditor.BrushHardness = EditorGUILayout.Slider("Hardness", MaskVolumeEditor.BrushHardness, 0f, 1f);

            EditorGUILayout.Space();

            MaskVolumeEditor.Brush.MeshCollidersOnly = EditorGUILayout.Toggle("Meshes Only", MaskVolumeEditor.Brush.MeshCollidersOnly);

            LayerMask newMask = EditorGUILayout.MaskField("Layer Mask", InternalEditorUtility.LayerMaskToConcatenatedLayersMask(MaskVolumeEditor.Brush.PhysicsLayerMask), InternalEditorUtility.layers);
            MaskVolumeEditor.Brush.PhysicsLayerMask = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(newMask);

            EditorGUILayout.Space();
        }

        static void ChannelField(ref bool enabled, ref byte value, string label)
        {
            var rect = EditorGUILayout.GetControlRect();

            var buttonRect = rect;
            buttonRect.x += EditorGUI.indentLevel * 15f;
            buttonRect.width = 18f;
            GUIContent gc = new GUIContent();
            gc.text = label;
            enabled = GUI.Toggle(buttonRect, enabled, gc, "Button");

            var sliderRect = rect;
            sliderRect.x += 33f;
            sliderRect.width -= 33f;
            value = (byte)EditorGUI.IntSlider(sliderRect, value, 0, 255);
        }
        
        static void Drawer_PrimarySettings(SerializedMaskVolume serialized, Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.drawGizmos, Styles.s_DrawGizmosLabel);
            EditorGUILayout.PropertyField(serialized.maskSpacingMode, Styles.s_MaskSpacingModeLabel);
            switch ((MaskSpacingMode)serialized.maskSpacingMode.enumValueIndex)
            {
                case MaskSpacingMode.Density:
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

                case MaskSpacingMode.Resolution:
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

            EditorGUILayout.Space();

            var maskVolume = (MaskVolume)owner.target;
            var maskVolumeAsset = (MaskVolumeAsset)serialized.maskVolumeAsset.objectReferenceValue;
            var noAsset = maskVolumeAsset == null;

            if (noAsset)
            {
                EditorGUILayout.HelpBox("Please create the asset after setting the Mask Volume dimensions.", MessageType.Error);
            }
            else if (!maskVolume.IsAssetCompatibleResolution())
            {
                var parameters = maskVolume.parameters;
                EditorGUILayout.HelpBox($"The asset assigned to this Mask Volume does not have matching data dimensions " +
                                        $"({maskVolumeAsset.resolutionX}x{maskVolumeAsset.resolutionY}x{maskVolumeAsset.resolutionZ} vs. " +
                                        $"{parameters.resolutionX}x{parameters.resolutionY}x{parameters.resolutionZ}), please recreate the asset.",
                                        MessageType.Error);
            }

            if (GUILayout.Button(noAsset ? Styles.k_CreateAssetText : Styles.k_RecreateAssetText))
                maskVolume.CreateAsset();

            EditorGUILayout.PropertyField(serialized.maskVolumeAsset, Styles.s_DataAssetLabel);
        }

        static void Drawer_VolumeContent(SerializedMaskVolume serialized, Editor owner)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            EditMode.DoInspectorToolbar(new[] { MaskVolumeEditor.k_EditShape, MaskVolumeEditor.k_EditBlend, MaskVolumeEditor.k_EditPaint }, Styles.s_VolumeToolbarContents, () =>
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
            
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                bool advanced = serialized.advancedFade.boolValue;
                advanced = GUILayout.Toggle(advanced, Styles.s_AdvancedModeContent, EditorStyles.miniButton, GUILayout.Width(70f), GUILayout.ExpandWidth(false));
                foreach (var containedBox in MaskVolumeEditor.blendBoxes.Values)
                {
                    containedBox.monoHandle = !advanced;
                }
                if (serialized.advancedFade.boolValue ^ advanced)
                {
                    serialized.advancedFade.boolValue = advanced;
                }
            }
            
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
            EditorGUILayout.PropertyField(serialized.blendMode, Styles.s_VolumeBlendModeLabel);
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

            EditorGUILayout.Space();
        }
    }
}
