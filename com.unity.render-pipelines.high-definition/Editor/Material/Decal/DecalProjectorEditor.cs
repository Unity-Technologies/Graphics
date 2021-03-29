using System;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using static UnityEditorInternal.EditMode;
using UnityEditor.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    [CustomEditor(typeof(DecalProjector))]
    [CanEditMultipleObjects]
    partial class DecalProjectorEditor : DecalEditorBase
    {
        SerializedProperty m_DrawDistanceProperty;
        SerializedProperty m_FadeScaleProperty;
        SerializedProperty m_AffectsTransparencyProperty;
        SerializedProperty m_FadeFactor;
        SerializedProperty m_DecalLayerMask;

        int layerMask => (target as Component).gameObject.layer;
        bool layerMaskHasMultipleValue
        {
            get
            {
                if (targets.Length < 2)
                    return false;
                int layerMask = (targets[0] as Component).gameObject.layer;
                for (int index = 0; index < targets.Length; ++index)
                {
                    if ((targets[index] as Component).gameObject.layer != layerMask)
                        return true;
                }
                return false;
            }
        }

        bool showAffectTransparency => ((target as DecalProjector).material != null) && DecalSystem.IsHDRenderPipelineDecal((target as DecalProjector).material.shader);

        bool showAffectTransparencyHaveMultipleDifferentValue
        {
            get
            {
                if (targets.Length < 2)
                    return false;
                DecalProjector decalProjector0 = (targets[0] as DecalProjector);
                bool show = decalProjector0.material != null && DecalSystem.IsHDRenderPipelineDecal(decalProjector0.material.shader);
                for (int index = 0; index < targets.Length; ++index)
                {
                    if ((targets[index] as DecalProjector).material != null)
                    {
                        DecalProjector decalProjectori = (targets[index] as DecalProjector);
                        if (decalProjectori != null && DecalSystem.IsHDRenderPipelineDecal(decalProjectori.material.shader) ^ show)
                            return true;
                    }
                }
                return false;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            m_DrawDistanceProperty = serializedObject.FindProperty("m_DrawDistance");
            m_FadeScaleProperty = serializedObject.FindProperty("m_FadeScale");
            m_AffectsTransparencyProperty = serializedObject.FindProperty("m_AffectsTransparency");
            m_FadeFactor = serializedObject.FindProperty("m_FadeFactor");
            m_DecalLayerMask = serializedObject.FindProperty("m_DecalLayerMask");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUI.BeginChangeCheck();
            {
                /*
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                DoInspectorToolbar(k_EditVolumeModes, editVolumeLabels, GetBoundsGetter(target as DecalProjector), this);
                DoInspectorToolbar(k_EditUVAndPivotModes, editPivotLabels, GetBoundsGetter(target as DecalProjector), this);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();


                EditorGUILayout.Space();

                Rect rect = EditorGUILayout.GetControlRect(true, EditorGUI.GetPropertyHeight(SerializedPropertyType.Vector2, k_SizeContent));
                EditorGUI.BeginProperty(rect, k_SizeSubContent[0], m_SizeValues[0]);
                EditorGUI.BeginProperty(rect, k_SizeSubContent[1], m_SizeValues[1]);
                float[] size = new float[2] { m_SizeValues[0].floatValue, m_SizeValues[1].floatValue };
                EditorGUI.BeginChangeCheck();
                EditorGUI.MultiFloatField(rect, k_SizeContent, k_SizeSubContent, size);
                if (EditorGUI.EndChangeCheck())
                {
                    for (int i = 0; i < 2; ++i)
                        UpdateSize(i, Mathf.Max(0, size[i]), m_SizeValues[i].floatValue);
                }
                EditorGUI.EndProperty();
                EditorGUI.EndProperty();

                EditorGUI.BeginChangeCheck();
                float oldSizeZ = m_SizeValues[2].floatValue;
                EditorGUILayout.PropertyField(m_SizeValues[2], k_ProjectionDepthContent);
                if (EditorGUI.EndChangeCheck())
                {
                    UpdateSize(2, Mathf.Max(0, m_SizeValues[2].floatValue), oldSizeZ);
                }

                EditorGUILayout.PropertyField(m_Offset, k_Offset);

                EditorGUILayout.PropertyField(m_MaterialProperty, k_MaterialContent);

                */

                // HDRP Specific
                bool decalLayerEnabled = false;
                HDRenderPipelineAsset hdrp = HDRenderPipeline.currentAsset;
                if (hdrp != null)
                {
                    decalLayerEnabled = hdrp.currentPlatformRenderPipelineSettings.supportDecals && hdrp.currentPlatformRenderPipelineSettings.supportDecalLayers;
                    using (new EditorGUI.DisabledScope(!decalLayerEnabled))
                    {
                        EditorGUILayout.PropertyField(m_DecalLayerMask, k_DecalLayerMaskContent);
                    }
                }

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(m_DrawDistanceProperty, k_DistanceContent);
                if (EditorGUI.EndChangeCheck() && m_DrawDistanceProperty.floatValue < 0f)
                    m_DrawDistanceProperty.floatValue = 0f;

                EditorGUILayout.PropertyField(m_FadeScaleProperty, k_FadeScaleContent);
                using (new EditorGUI.DisabledScope(!decalLayerEnabled))
                {
                    float angleFadeMinValue = m_StartAngleFadeProperty.floatValue;
                    float angleFadeMaxValue = m_EndAngleFadeProperty.floatValue;
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.MinMaxSlider(k_AngleFadeContent, ref angleFadeMinValue, ref angleFadeMaxValue, 0.0f, 180.0f);
                    if (EditorGUI.EndChangeCheck())
                    {
                        m_StartAngleFadeProperty.floatValue = angleFadeMinValue;
                        m_EndAngleFadeProperty.floatValue = angleFadeMaxValue;
                    }
                }

                if (!decalLayerEnabled)
                {
                    EditorGUILayout.HelpBox("Enable 'Decal Layers' in your HDRP Asset if you want to control the Angle Fade. There is a performance cost of enabling this option.",
                        MessageType.Info);
                }
                EditorGUILayout.PropertyField(m_FadeFactor, k_FadeFactorContent);

                // only display the affects transparent property if material is HDRP/decal
                if (showAffectTransparencyHaveMultipleDifferentValue)
                {
                    using (new EditorGUI.DisabledScope(true))
                        EditorGUILayout.LabelField(EditorGUIUtility.TrTextContent("Multiple material type in selection"));
                }
                else if (showAffectTransparency)
                    EditorGUILayout.PropertyField(m_AffectsTransparencyProperty, k_AffectTransparentContent);
            }
            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();

            if (layerMaskHasMultipleValue || layerMask != (target as Component).gameObject.layer)
            {
                foreach (var decalProjector in targets)
                {
                    (decalProjector as DecalProjector).OnValidate();
                }
            }

            if (m_MaterialEditor != null)
            {
                // We need to prevent the user to edit default decal materials
                bool isDefaultMaterial = false;
                bool isValidDecalMaterial = true;
                var hdrp = HDRenderPipeline.currentAsset;
                if (hdrp != null)
                {
                    foreach (var decalProjector in targets)
                    {
                        var mat = (decalProjector as DecalProjector).material;

                        isDefaultMaterial |= mat == hdrp.GetDefaultDecalMaterial();
                        isValidDecalMaterial &= mat != null && DecalSystem.IsDecalMaterial(mat);
                    }
                }

                if (isValidDecalMaterial)
                {
                    // Draw the material's foldout and the material shader field
                    // Required to call m_MaterialEditor.OnInspectorGUI ();
                    m_MaterialEditor.DrawHeader();

                    using (new EditorGUI.DisabledGroupScope(isDefaultMaterial))
                    {
                        // Draw the material properties
                        // Works only if the foldout of m_MaterialEditor.DrawHeader () is open
                        m_MaterialEditor.OnInspectorGUI();
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("Decal only work with Decal Material. Decal Material can be selected in the shader list HDRP/Decal or can be created from a Decal Master Node.",
                        MessageType.Error);
                }
            }
        }

        protected override void DecalUpdateCallback( DecalBase decalProjector )
        {
            // Smoothly update the decal image projected
            DecalSystem.instance.UpdateCachedData(decalProjector.Handle, (decalProjector as DecalProjector).GetCachedDecalData());
        }
    }
}
