using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Editor for LensFlareComponentSRP: Lens Flare Data-Driven which can be added on any GameObject
    /// </summary>
    [CanEditMultipleObjects]
    [CustomEditor(typeof(LensFlareComponentSRP))]
    [SupportedOnRenderPipeline]
    class LensFlareComponentSRPEditor : Editor
    {
        SerializedProperty m_LensFlareData;
        SerializedProperty m_Intensity;
        SerializedProperty m_Scale;
        SerializedProperty m_MaxAttenuationDistance;
        SerializedProperty m_MaxAttenuationScale;
        SerializedProperty m_DistanceAttenuationCurve;
        SerializedProperty m_ScaleByDistanceCurve;
        SerializedProperty m_AttenuationByLightShape;
        SerializedProperty m_RadialScreenAttenuationCurve;
        SerializedProperty m_UseOcclusion;
        SerializedProperty m_EnvironementOcclusion;
        SerializedProperty m_OcclusionRadius;
        SerializedProperty m_SamplesCount;
        SerializedProperty m_OcclusionOffset;
        SerializedProperty m_AllowOffScreen;
        SerializedProperty m_OcclusionRemapTextureCurve;
        SerializedProperty m_OcclusionRemapCurve;
        SerializedProperty m_LightOverride;

        Light m_AttachedLight;

        void MakeTextureDirtyCallback()
        {
            LensFlareComponentSRP comp = target as LensFlareComponentSRP;
            comp.occlusionRemapCurve.SetDirty();
        }

        void OnEnable()
        {
            if (targets.Length == 1)
                m_AttachedLight = (target as Component)?.GetComponent<Light>();
            else
                m_AttachedLight = null;
            PropertyFetcher<LensFlareComponentSRP> entryPoint = new PropertyFetcher<LensFlareComponentSRP>(serializedObject);
            m_LensFlareData = entryPoint.Find("m_LensFlareData");
            m_Intensity = entryPoint.Find(x => x.intensity);
            m_Scale = entryPoint.Find(x => x.scale);
            m_MaxAttenuationDistance = entryPoint.Find(x => x.maxAttenuationDistance);
            m_DistanceAttenuationCurve = entryPoint.Find(x => x.distanceAttenuationCurve);
            m_MaxAttenuationScale = entryPoint.Find(x => x.maxAttenuationScale);
            m_ScaleByDistanceCurve = entryPoint.Find(x => x.scaleByDistanceCurve);
            m_AttenuationByLightShape = entryPoint.Find(x => x.attenuationByLightShape);
            m_RadialScreenAttenuationCurve = entryPoint.Find(x => x.radialScreenAttenuationCurve);
            m_UseOcclusion = entryPoint.Find(x => x.useOcclusion);
            m_EnvironementOcclusion = entryPoint.Find(x => x.environmentOcclusion);
            m_OcclusionRadius = entryPoint.Find(x => x.occlusionRadius);
            m_SamplesCount = entryPoint.Find(x => x.sampleCount);
            m_OcclusionOffset = entryPoint.Find(x => x.occlusionOffset);
            m_AllowOffScreen = entryPoint.Find(x => x.allowOffScreen);
            m_OcclusionRemapTextureCurve = entryPoint.Find(x => x.occlusionRemapCurve);
            m_OcclusionRemapCurve = m_OcclusionRemapTextureCurve.FindPropertyRelative("m_Curve");
            m_LightOverride = entryPoint.Find(x => x.lightOverride);

            Undo.undoRedoPerformed += MakeTextureDirtyCallback;
        }

        void OnDisable()
        {
            Undo.undoRedoPerformed -= MakeTextureDirtyCallback;
        }

        /// <summary>
        /// Implement this function to make a custom inspector
        /// </summary>
        public override void OnInspectorGUI()
        {
            var renderPipelineAssetType = GraphicsSettings.currentRenderPipelineAssetType;
            if (renderPipelineAssetType != null && renderPipelineAssetType.Name == "HDRenderPipelineAsset")
            {
                if (!(bool)Type.GetType("UnityEditor.Rendering.HighDefinition.HDEditorUtils,Unity.RenderPipelines.HighDefinition.Editor")
                    .GetMethod("DataDrivenLensFlareHelpBox", BindingFlags.Static | BindingFlags.NonPublic)
                    .Invoke(null, null))
                    return;
            }

            LensFlareComponentSRP lensFlareData = m_Intensity.serializedObject.targetObject as LensFlareComponentSRP;
            bool attachedToLight = false;
            bool lightIsDirLight = false;
            Light light = null;
            if (lensFlareData != null &&
                (light = lensFlareData.GetComponent<Light>()) != null)
            {
                attachedToLight = true;
                if (light.type == LightType.Directional)
                    lightIsDirLight = true;
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.LabelField(Styles.generalData.text, EditorStyles.boldLabel);
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    bool showCopy = m_LensFlareData.objectReferenceValue != null;
                    int buttonWidth = showCopy ? 45 : 60;

                    EditorGUILayout.PropertyField(m_LensFlareData, Styles.lensFlareData);
                    if (GUILayout.Button(Styles.newButton, showCopy ? EditorStyles.miniButtonLeft : EditorStyles.miniButton, GUILayout.Width(buttonWidth)))
                    {
                        // By default, try to put assets in a folder next to the currently active
                        // scene file. If the user isn't a scene, put them in root instead.
                        var actualTarget = target as LensFlareComponentSRP;
                        var targetName = actualTarget.name + " Lens Flare (SRP)";
                        var scene = actualTarget.gameObject.scene;
                        var asset = LensFlareEditorUtils.CreateLensFlareDataSRPAsset(scene, targetName);
                        m_LensFlareData.objectReferenceValue = asset;
                    }
                    if (showCopy && GUILayout.Button(Styles.cloneButton, EditorStyles.miniButtonRight, GUILayout.Width(buttonWidth)))
                    {
                        // Duplicate the currently assigned profile and save it as a new profile
                        var origin = m_LensFlareData.objectReferenceValue;
                        var path = AssetDatabase.GetAssetPath(m_LensFlareData.objectReferenceValue);

                        path = CoreEditorUtils.IsAssetInReadOnlyPackage(path)

                            // We may be in a read only package, in that case we need to clone the volume profile in an
                            // editable area, such as the root of the project.
                            ? AssetDatabase.GenerateUniqueAssetPath(Path.Combine("Assets", Path.GetFileName(path)))

                            // Otherwise, duplicate next to original asset.
                            : AssetDatabase.GenerateUniqueAssetPath(path);

                        var asset = Instantiate(origin);
                        AssetDatabase.CreateAsset(asset, path);
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();

                        m_LensFlareData.objectReferenceValue = asset;
                    }
                }
                EditorGUILayout.PropertyField(m_Intensity, Styles.intensity);
                EditorGUILayout.PropertyField(m_Scale, Styles.scale);
                if (lensFlareData.lensFlareData != null && (lensFlareData.lensFlareData.HasAModulateByLightColorElement() || lensFlareData.attenuationByLightShape))
                {
                    EditorGUILayout.PropertyField(m_LightOverride, Styles.lightOverride);
                }
                if (!lightIsDirLight)
                {
                    if (attachedToLight)
                        EditorGUILayout.PropertyField(m_AttenuationByLightShape, Styles.attenuationByLightShape);
                    EditorGUILayout.PropertyField(m_MaxAttenuationDistance, Styles.maxAttenuationDistance);
                    ++EditorGUI.indentLevel;
                    EditorGUILayout.PropertyField(m_DistanceAttenuationCurve, Styles.distanceAttenuationCurve);
                    --EditorGUI.indentLevel;
                    EditorGUILayout.PropertyField(m_MaxAttenuationScale, Styles.maxAttenuationScale);
                    ++EditorGUI.indentLevel;
                    EditorGUILayout.PropertyField(m_ScaleByDistanceCurve, Styles.scaleByDistanceCurve);
                    --EditorGUI.indentLevel;
                }
                EditorGUILayout.PropertyField(m_RadialScreenAttenuationCurve, Styles.radialScreenAttenuationCurve);
            }

            EditorGUILayout.PropertyField(m_UseOcclusion, Styles.enableOcclusion);
            if (m_UseOcclusion.boolValue)
            {
                ++EditorGUI.indentLevel;
                EditorGUI.BeginDisabledGroup(m_AttachedLight != null && m_AttachedLight.type != LightType.Directional);
                {
                    if (RenderPipelineManager.currentPipeline is ICloudBackground)
                        EditorGUILayout.PropertyField(m_EnvironementOcclusion, Styles.environmentOcclusion);
                }
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.PropertyField(m_OcclusionRadius, Styles.occlusionRadius);
                EditorGUILayout.PropertyField(m_SamplesCount, Styles.sampleCount);
                EditorGUILayout.PropertyField(m_OcclusionOffset, Styles.occlusionOffset);
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(m_OcclusionRemapCurve, Styles.occlusionRemapCurve);
                if (EditorGUI.EndChangeCheck())
                {
                    LensFlareComponentSRP comp = target as LensFlareComponentSRP;
                    comp.occlusionRemapCurve.SetDirty();
                }
                --EditorGUI.indentLevel;
            }
            EditorGUILayout.PropertyField(m_AllowOffScreen, Styles.allowOffScreen);

            if (EditorGUI.EndChangeCheck())
            {
                m_LensFlareData.serializedObject.ApplyModifiedProperties();
            }
        }

        static class Styles
        {
            static public readonly GUIContent generalData = EditorGUIUtility.TrTextContent("General");

            static public readonly GUIContent lensFlareData = EditorGUIUtility.TrTextContent("Lens Flare Data", "Specifies the SRP Lens Flare Data asset this component uses.");
            static public readonly GUIContent newButton = EditorGUIUtility.TrTextContent("New", "Create a new SRP Lens Flare Data asset.");
            static public readonly GUIContent cloneButton = EditorGUIUtility.TrTextContent("Clone", "Create a new SRP Lens Flare Data asset and copy the content of the currently assigned data.");
            static public readonly GUIContent intensity = EditorGUIUtility.TrTextContent("Intensity", "Sets the intensity of the lens flare.");
            static public readonly GUIContent scale = EditorGUIUtility.TrTextContent("Scale", "Sets the scale of the lens flare.");
            static public readonly GUIContent maxAttenuationDistance = EditorGUIUtility.TrTextContent("Attenuation Distance", "Sets the distance, in meters, between the start and the end of the Distance Attenuation Curve.");
            static public readonly GUIContent distanceAttenuationCurve = EditorGUIUtility.TrTextContent("Attenuation Distance Curve", "Specifies the curve that reduces the effect of the lens flare  based on the distance between the GameObject this asset is attached to and the Camera.");
            static public readonly GUIContent maxAttenuationScale = EditorGUIUtility.TrTextContent("Scale Distance", "Sets the distance, in meters, between the start and the end of the Scale Attenuation Curve.");
            static public readonly GUIContent scaleByDistanceCurve = EditorGUIUtility.TrTextContent("Scale Distance Curve", "Specifies the curve used to calculate the size of the lens flare based on the distance between the GameObject this asset is attached to, and the Camera.");
            static public readonly GUIContent attenuationByLightShape = EditorGUIUtility.TrTextContent("Attenuation By Light Shape", "When enabled, if the component is attached to a light, automatically reduces the effect of the lens flare based on the type and shape of the light.");
            static public readonly GUIContent radialScreenAttenuationCurve = EditorGUIUtility.TrTextContent("Screen Attenuation Curve", "Specifies the curve that modifies the intensity of the lens flare based on its distance from the edge of the screen.");
            static public readonly GUIContent enableOcclusion = EditorGUIUtility.TrTextContent("Screen Space Occlusion", "When enabled, the renderer uses the depth buffer to occlude (partially or completely) the lens flare. Partial occlusion also occurs when the lens flare is partially offscreen.");
            static public readonly GUIContent environmentOcclusion = EditorGUIUtility.TrTextContent("Environment Occlusion", "When enabled, environment effects supported by the render pipeline can be used to occlude lens flares.\nThis may include opacity from volumetric clouds, background clouds, fog and water.");
            static public readonly GUIContent occlusionRadius = EditorGUIUtility.TrTextContent("Occlusion Radius", "Sets the radius, in meters, around the light used to compute the occlusion of the lens flare. If this area is half occluded by geometry (or half off-screen), the intensity of the lens flare is cut by half.");
            static public readonly GUIContent sampleCount = EditorGUIUtility.TrTextContent("Sample Count", "Sets the number of random samples used inside the Occlusion Radius area. A higher sample count gives a smoother attenuation when occluded.");
            static public readonly GUIContent occlusionOffset = EditorGUIUtility.TrTextContent("Occlusion Offset", "Sets the offset of the occlusion area in meters between the GameObject this asset is attached to, and the Camera. A positive value moves the occlusion area closer to the Camera.");
            static public readonly GUIContent occlusionRemapCurve = EditorGUIUtility.TrTextContent("Occlusion Remap Curve", "Specifies the curve used to remap the occlusion of the flare. By default, the occlusion is linear, between 0 and 1. This can be specifically useful to occlude flare more drastically when behind clouds.");
            static public readonly GUIContent allowOffScreen = EditorGUIUtility.TrTextContent("Allow OffScreen", "When enabled, allows the lens flare to affect the scene even when it is outside the Camera's field of view.");
            static public readonly GUIContent volumetricCloudOcclusion = EditorGUIUtility.TrTextContent("Volumetric Clouds", "When enabled, HDRP uses the volumetric clouds texture (in screen space) for the occlusion.");
            static public readonly GUIContent lightOverride = EditorGUIUtility.TrTextContent("Light Override", "Specifies the light component where the color and shape values are fetched from when using \"Modulate By Light Color\" or \"Attenuation By Light Shape\" properties on a Lens Flare Element. If nothing is specified, the light component from this gameobject is used.");
        }
    }
}
