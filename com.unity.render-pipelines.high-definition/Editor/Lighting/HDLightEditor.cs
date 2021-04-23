using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace UnityEditor.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [CustomEditorForRenderPipeline(typeof(Light), typeof(HDRenderPipelineAsset))]
    sealed partial class HDLightEditor : LightEditor
    {
        public SerializedHDLight m_SerializedHDLight;

        HDAdditionalLightData[] m_AdditionalLightDatas;

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
            m_SerializedHDLight = new SerializedHDLight(m_AdditionalLightDatas, settings);

            // Update emissive mesh and light intensity when undo/redo
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        void OnDisable()
        {
            // Update emissive mesh and light intensity when undo/redo
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        void OnUndoRedo()
        {
            // Serialized object is lossing references after an undo
            if (m_SerializedHDLight.serializedObject.targetObject != null)
            {
                m_SerializedHDLight.serializedObject.Update();
                foreach (var hdLightData in m_AdditionalLightDatas)
                    if (hdLightData != null)
                        hdLightData.UpdateAreaLightEmissiveMesh();
            }

            // if Type or ShowEmissive Mesh undone, we must fetxh again the emissive meshes
            m_SerializedHDLight.FetchAreaLightEmissiveMeshComponents();
        }

        public override void OnInspectorGUI()
        {
            m_SerializedHDLight.Update();

            // Add space before the first collapsible area
            EditorGUILayout.Space();

            ApplyAdditionalComponentsVisibility(true);

            EditorGUI.BeginChangeCheck();
            HDLightUI.Inspector.Draw(m_SerializedHDLight, this);
            if (EditorGUI.EndChangeCheck())
            {
                m_SerializedHDLight.Apply();

                foreach (var hdLightData in m_AdditionalLightDatas)
                    hdLightData.UpdateAllLightValues();
            }

            if (m_SerializedHDLight.needUpdateAreaLightEmissiveMeshComponents)
                UpdateAreaLightEmissiveMeshComponents();
        }

        void UpdateAreaLightEmissiveMeshComponents()
        {
            foreach (var hdLightData in m_AdditionalLightDatas)
            {
                hdLightData.UpdateAreaLightEmissiveMesh();
            }

            m_SerializedHDLight.needUpdateAreaLightEmissiveMeshComponents = false;
        }

        // Internal utilities
        void ApplyAdditionalComponentsVisibility(bool hide)
        {
            // UX team decided that we should always show component in inspector.
            // However already authored scene save this settings, so force the component to be visible
            foreach (var t in m_SerializedHDLight.serializedObject.targetObjects)
                if (((HDAdditionalLightData)t).hideFlags == HideFlags.HideInInspector)
                    ((HDAdditionalLightData)t).hideFlags = HideFlags.None;
        }

        protected override void OnSceneGUI()
        {
            // Each handles manipulate only one light
            // Thus do not rely on serialized properties
            HDLightType lightType = targetAdditionalData.type;

            if (lightType == HDLightType.Directional
                || lightType == HDLightType.Point
                || lightType == HDLightType.Area && targetAdditionalData.areaLightShape == AreaLightShape.Disc)
                base.OnSceneGUI();
            else
                HDLightUI.DrawHandles(targetAdditionalData, this);
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
