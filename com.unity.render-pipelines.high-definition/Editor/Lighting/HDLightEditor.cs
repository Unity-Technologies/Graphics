using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [CanEditMultipleObjects]
    [CustomEditorForRenderPipeline(typeof(Light), typeof(HDRenderPipelineAsset))]
    sealed partial class HDLightEditor : LightEditor
    {
        public SerializedHDLight m_SerializedHDLight;

        HDAdditionalLightData[] m_AdditionalLightDatas;
        AdditionalShadowData[] m_AdditionalShadowDatas;

        HDAdditionalLightData targetAdditionalData
            => m_AdditionalLightDatas[ReferenceTargetIndex(this)];
        
        static Func<Editor, int> ReferenceTargetIndex;

        static HDLightEditor()
        {
            var type = typeof(UnityEditor.Editor);
            var propertyInfo = type.GetProperty("referenceTargetIndex", BindingFlags.NonPublic | BindingFlags.Instance);
            var getterMethodInfo = propertyInfo.GetGetMethod(true);
            var instance = Expression.Parameter(typeof(Editor), "instance");
            var getterCall = Expression.Call(instance, getterMethodInfo);
            var lambda = Expression.Lambda<Func<Editor, int>>(getterCall, instance);
            ReferenceTargetIndex = lambda.Compile();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            // Get & automatically add additional HD data if not present
            m_AdditionalLightDatas = CoreEditorUtils.GetAdditionalData<HDAdditionalLightData>(targets, HDAdditionalLightData.InitDefaultHDAdditionalLightData);
            m_AdditionalShadowDatas = CoreEditorUtils.GetAdditionalData<AdditionalShadowData>(targets, HDAdditionalShadowData.InitDefaultHDAdditionalShadowData);
            m_SerializedHDLight = new SerializedHDLight(m_AdditionalLightDatas, m_AdditionalShadowDatas, settings);

            // Update emissive mesh and light intensity when undo/redo
            Undo.undoRedoPerformed += () =>
            {
                // Serialized object is lossing references after an undo
                if (m_SerializedHDLight.serializedLightDatas.targetObject != null)
                {
                    m_SerializedHDLight.serializedLightDatas.ApplyModifiedProperties();
                    foreach (var hdLightData in m_AdditionalLightDatas)
                        if (hdLightData != null)
                            hdLightData.UpdateAreaLightEmissiveMesh();
                }
            };

            // If the light is disabled in the editor we force the light upgrade from his inspector
            foreach (var additionalLightData in m_AdditionalLightDatas)
                additionalLightData.UpgradeLight();
        }

        public override void OnInspectorGUI()
        {
            m_SerializedHDLight.Update();

            // Add space before the first collapsible area
            EditorGUILayout.Space();

            ApplyAdditionalComponentsVisibility(true);

            HDLightUI.Inspector.Draw(m_SerializedHDLight, this);

            m_SerializedHDLight.Apply();

            if (m_SerializedHDLight.needUpdateAreaLightEmissiveMeshComponents)
                UpdateAreaLightEmissiveMeshComponents();
        }

        void UpdateAreaLightEmissiveMeshComponents()
        {
            foreach (var hdLightData in m_AdditionalLightDatas)
            {
                hdLightData.UpdateAreaLightEmissiveMesh();

                MeshRenderer emissiveMeshRenderer = hdLightData.GetComponent<MeshRenderer>();
                MeshFilter emissiveMeshFilter = hdLightData.GetComponent<MeshFilter>();

                // If the display emissive mesh is disabled, skip to the next selected light
                if (emissiveMeshFilter == null || emissiveMeshRenderer == null)
                    continue;

                // We only load the mesh and it's material here, because we can't do that inside HDAdditionalLightData (Editor assembly)
                // Every other properties of the mesh is updated in HDAdditionalLightData to support timeline and editor records
                switch (hdLightData.lightTypeExtent)
                {
                    case LightTypeExtent.Tube:
                        emissiveMeshFilter.mesh = HDEditorUtils.LoadAsset<Mesh>("Runtime/RenderPipelineResources/Mesh/Cylinder.fbx");
                        break;
                    case LightTypeExtent.Rectangle:
                    default:
                        emissiveMeshFilter.mesh = HDEditorUtils.LoadAsset<Mesh>("Runtime/RenderPipelineResources/Mesh/Quad.FBX");
                        break;
                }
                if (emissiveMeshRenderer.sharedMaterial == null)
                {
                    emissiveMeshRenderer.sharedMaterial = new Material(Shader.Find("HDRP/Unlit"));
                }
                emissiveMeshRenderer.sharedMaterial.SetFloat("_IncludeIndirectLighting", 0.0f);
            }

            m_SerializedHDLight.needUpdateAreaLightEmissiveMeshComponents = false;
        }

        // Internal utilities
        void ApplyAdditionalComponentsVisibility(bool hide)
        {
            // UX team decided that we should always show component in inspector.
            // However already authored scene save this settings, so force the component to be visible
            // var flags = hide ? HideFlags.HideInInspector : HideFlags.None;
            var flags = HideFlags.None;

            foreach (var t in m_SerializedHDLight.serializedLightDatas.targetObjects)
                ((HDAdditionalLightData)t).hideFlags = flags;

            foreach (var t in m_SerializedHDLight.serializedShadowDatas.targetObjects)
                ((AdditionalShadowData)t).hideFlags = flags;
        }
        
        protected override void OnSceneGUI()
        {
            // Each handles manipulate only one light
            // Thus do not rely on serialized properties
            Light light = target as Light;
            HDAdditionalLightData additionalLightData = targetAdditionalData;
            if (additionalLightData.lightTypeExtent == LightTypeExtent.Punctual && (light.type == LightType.Directional || light.type == LightType.Point))
                base.OnSceneGUI();
            else
                HDLightUI.DrawHandles(additionalLightData, this);
        }

        internal Color legacyLightColor
        {
            get
            {
                Light light = (Light)target;
                return light.enabled ? LightEditor.kGizmoLight : LightEditor.kGizmoDisabledLight;
            }
        }
    }
}
