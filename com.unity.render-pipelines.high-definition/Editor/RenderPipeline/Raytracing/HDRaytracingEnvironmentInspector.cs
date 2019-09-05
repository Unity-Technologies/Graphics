using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.Rendering;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.HighDefinition
{
    using CED = CoreEditorDrawer<SerializedHDRaytracingEnvironment>;

    [CustomEditor(typeof(HDRaytracingEnvironment))]
    class HDRaytracingEnvironmentInspector : Editor
    {
#if ENABLE_RAYTRACING
        protected static class Styles
        {
            // Generic
            public static readonly GUIContent genericSectionText = EditorGUIUtility.TrTextContent("Generic Attributes");
            public static readonly GUIContent rayBiasText = EditorGUIUtility.TrTextContent("Ray Bias");

            /////////////////////////////////////////////////////////////////////////////////////////////////
            // Ambient Occlusion
            public static readonly GUIContent aoSectionText = EditorGUIUtility.TrTextContent("Ray-traced Ambient Occlusion");
            public static readonly GUIContent aoLayerMaskText = EditorGUIUtility.TrTextContent("AO Layer Mask");

            /////////////////////////////////////////////////////////////////////////////////////////////////
            // Reflections
            public static GUIContent reflSectionText = new GUIContent("Ray-traced Reflections");
            public static GUIContent reflLayerMaskText = EditorGUIUtility.TrTextContent("Reflection Layer Mask");

            /////////////////////////////////////////////////////////////////////////////////////////////////
            // Area Light Shadow
            public static GUIContent shadowSectionText = new GUIContent("Ray-traced Shadows");
            public static GUIContent shadowLayerMaskText = EditorGUIUtility.TrTextContent("Shadow Layer Mask");

            /////////////////////////////////////////////////////////////////////////////////////////////////
            // Recursive Tracing
            public static readonly GUIContent recursiveRayTracingSectionText = EditorGUIUtility.TrTextContent("Recursive Ray Tracing");
            public static readonly GUIContent raytracedLayerMaskText = EditorGUIUtility.TrTextContent("Recursive Ray Tracing Layer Mask");

            /////////////////////////////////////////////////////////////////////////////////////////////////
            // Indirect Diffuse
            public static readonly GUIContent indirectDiffuseSectionText = EditorGUIUtility.TrTextContent("Indirect Diffuse Raytracing");
            public static readonly GUIContent indirectDiffuseLayerMaskText = EditorGUIUtility.TrTextContent("Indirect Diffuse Layer Mask");
        }

        SerializedHDRaytracingEnvironment m_SerializedHDRaytracingEnvironment;

        static readonly CED.IDrawer Inspector;

        enum Expandable
        {
            Generic = 1 << 0,
            AmbientOcclusion = 1 << 1,
            Reflection = 1 << 2,
            LightCluster = 1 << 3,
            AreaShadow = 1 << 4,
            RecursiveRayTracing = 1 << 5,
            IndirectDiffuse = 1 << 6
        }
        static ExpandedState<Expandable, HDRaytracingEnvironment> k_ExpandedState;

        static HDRaytracingEnvironmentInspector()
        {
            Inspector = CED.Group(CED.FoldoutGroup(Styles.genericSectionText, Expandable.Generic, k_ExpandedState, GenericSubMenu),
                        CED.FoldoutGroup(Styles.aoSectionText, Expandable.AmbientOcclusion, k_ExpandedState, AmbientOcclusionSubMenu),
                        CED.FoldoutGroup(Styles.reflSectionText, Expandable.Reflection, k_ExpandedState, ReflectionsSubMenu),
                        CED.FoldoutGroup(Styles.shadowSectionText, Expandable.AreaShadow, k_ExpandedState, AreaShadowSubMenu),
                        CED.FoldoutGroup(Styles.recursiveRayTracingSectionText, Expandable.RecursiveRayTracing, k_ExpandedState, RaytracingSubMenu),
                        CED.FoldoutGroup(Styles.indirectDiffuseSectionText, Expandable.IndirectDiffuse, k_ExpandedState, IndirectDiffuseSubMenu));
        }
        static void GenericSubMenu(SerializedHDRaytracingEnvironment rtEnv, Editor owner)
        {
            // AO Specific fields
            EditorGUILayout.PropertyField(rtEnv.rayBias, Styles.rayBiasText);
        }

        static void UpdateEnvironmentSubScenes(SerializedHDRaytracingEnvironment rtEnv)
        {
            rtEnv.Apply();
            HDRenderPipeline hdPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;
            if (hdPipeline != null)
            {
                hdPipeline.m_RayTracingManager.UpdateEnvironmentSubScenes();
            }
        }

        static void AmbientOcclusionSubMenu(SerializedHDRaytracingEnvironment rtEnv, Editor owner)
        {
            // For the layer masks, we want to make sure the matching resources will be available during the following draw call. So we need to force a propagation to
            // the non serialized object and update the subscenes
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(rtEnv.aoLayerMask, Styles.aoLayerMaskText);
            if(EditorGUI.EndChangeCheck())
            {
                UpdateEnvironmentSubScenes(rtEnv);
            }
        }

        static void ReflectionsSubMenu(SerializedHDRaytracingEnvironment rtEnv, Editor owner)
        {
            // For the layer masks, we want to make sure the matching resources will be available during the following draw call. So we need to force a propagation to
            // the non serialized object and update the sub-scenes
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(rtEnv.reflLayerMask, Styles.reflLayerMaskText);
            if(EditorGUI.EndChangeCheck())
            {
                UpdateEnvironmentSubScenes(rtEnv);
            }
        }

        static void RaytracingSubMenu(SerializedHDRaytracingEnvironment rtEnv, Editor owner)
        {
            // For the layer masks, we want to make sure the matching resources will be available during the following draw call. So we need to force a propagation to
            // the non serialized object and update the sub-scenes
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(rtEnv.raytracedLayerMask, Styles.raytracedLayerMaskText);
            if(EditorGUI.EndChangeCheck())
            {
                UpdateEnvironmentSubScenes(rtEnv);
            }
        }

        static void AreaShadowSubMenu(SerializedHDRaytracingEnvironment rtEnv, Editor owner)
        {
            // For the layer masks, we want to make sure the matching resources will be available during the following draw call. So we need to force a propagation to
            // the non serialized object and update the sub-scenes
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(rtEnv.shadowLayerMask, Styles.shadowLayerMaskText);
            if(EditorGUI.EndChangeCheck())
            {
                UpdateEnvironmentSubScenes(rtEnv);
            }
        }

        static void IndirectDiffuseSubMenu(SerializedHDRaytracingEnvironment rtEnv, Editor owner)
        {
            // For the layer masks, we want to make sure the matching resources will be available during the following draw call. So we need to force a propagation to
            // the non serialized object and update the sub-scenes
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(rtEnv.indirectDiffuseLayerMask, Styles.indirectDiffuseLayerMaskText);
            if(EditorGUI.EndChangeCheck())
            {
                UpdateEnvironmentSubScenes(rtEnv);
            }
        }

        protected void OnEnable()
        {
            HDRaytracingEnvironment rtEnv = (HDRaytracingEnvironment)target;

            // Get & automatically add additional HD data if not present
            m_SerializedHDRaytracingEnvironment = new SerializedHDRaytracingEnvironment(rtEnv);

            k_ExpandedState = new ExpandedState<Expandable, HDRaytracingEnvironment>(~(-1), "HDRP");

        }

        public override void OnInspectorGUI()
        {
            m_SerializedHDRaytracingEnvironment.Update();
            Inspector.Draw(m_SerializedHDRaytracingEnvironment, this);
            m_SerializedHDRaytracingEnvironment.Apply();
        }
#endif
    }
}
