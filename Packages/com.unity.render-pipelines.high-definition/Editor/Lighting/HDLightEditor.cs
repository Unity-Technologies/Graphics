using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(Light))]
    [SupportedOnRenderPipeline(typeof(HDRenderPipelineAsset))]
    sealed partial class HDLightEditor : LightEditor
    {
        public SerializedHDLight m_SerializedHDLight;

        HDAdditionalLightData[] m_AdditionalLightDatas;

        HDAdditionalLightData targetAdditionalData
            => m_AdditionalLightDatas[ReferenceTargetIndex(this)];

        public HDAdditionalLightData GetAdditionalDataForTargetIndex(int i)
            => m_AdditionalLightDatas[i];

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

            HDLightUI.RegisterEditor(this);
        }

        void OnDisable()
        {
            // Update emissive mesh and light intensity when undo/redo
            Undo.undoRedoPerformed -= OnUndoRedo;
            HDLightUI.UnregisterEditor(this);
        }

        void OnUndoRedo()
        {
            // Serialized object is lossing references after an undo
            if (m_SerializedHDLight.serializedObject.targetObject != null)
            {
                m_SerializedHDLight.serializedObject.Update();
                foreach (var hdLightData in m_AdditionalLightDatas)
                    if (hdLightData != null)
                    {
                        if (hdLightData.lightIdxForCachedShadows >= 0) // If it is within the cached system we need to evict it.
                            HDShadowManager.cachedShadowManager.EvictLight(hdLightData, hdLightData.legacyLight.type);

                        hdLightData.UpdateAreaLightEmissiveMesh();
                        hdLightData.UpdateRenderEntity();
                    }
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

            if (HDEditorUtils.IsPresetEditor(this))
            {
                HDLightUI.PresetInspector.Draw(m_SerializedHDLight, this);
            }
            else
            {
                using (new EditorGUILayout.VerticalScope())
                    HDLightUI.Inspector.Draw(m_SerializedHDLight, this);
            }
            if (EditorGUI.EndChangeCheck())
            {
                m_SerializedHDLight.Apply();

                foreach (var hdLightData in m_AdditionalLightDatas)
                {
                    hdLightData.UpdateAllLightValues();
                    hdLightData.UpdateRenderEntity();
                }
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
            if (targetAdditionalData == null)
                return;

            // Each handles manipulate only one light
            // Thus do not rely on serialized properties
            LightType lightType = targetAdditionalData.legacyLight.type;

            if (lightType == LightType.Directional || lightType == LightType.Point)
            {
                base.OnSceneGUI();
            }
            else if (lightType == LightType.Disc)
            {
                EditorGUI.BeginChangeCheck();

                base.OnSceneGUI();

                if (EditorGUI.EndChangeCheck())
                {
                    // Necessary since the built-in disk light logic doesn't update the HDRP property when
                    // changing the radius through the disk's gizmo in the scene view.
                    m_SerializedHDLight.shapeWidth.floatValue = targetAdditionalData.legacyLight.areaSize.x;
                    m_SerializedHDLight.Apply();
                }
            }
            else
                HDLightUI.DrawHandles(targetAdditionalData, this);

            if (lightType == LightType.Directional)
            {
                var hdriSkies = GetHDRISkys();
                foreach (var sky in hdriSkies)
                {
                    if (sky.lockSun.value)
                    {
                        Vector3 currentRot = targetAdditionalData.legacyLight.transform.rotation.eulerAngles;
                        if (Math.Abs(sky.rotation.value - currentRot.y) > 0.01f)
                        {
                            sky.sunInitialRotation.value = 0f - currentRot.y;
                            sky.rotation.value = currentRot.y;
                            EditorUtility.SetDirty(sky);
                        }
                    }
                }
            }
        }

        List<HDRISky> GetHDRISkys()
        {
            LayerMask volumesMask = LayerMask.NameToLayer("Everything");
            var volumes = VolumeManager.instance.GetVolumes(volumesMask);

            List<HDRISky> skies = new List<HDRISky>();
            foreach (var volume in volumes)
            {
                var profile = volume.HasInstantiatedProfile() ? volume.profile : volume.sharedProfile;
                if (profile == null)
                    continue;

                foreach (var component in profile.components)
                {
                    HDRISky sky = component as HDRISky;
                    if (sky != null)
                    {
                        skies.Add(sky);
                    }
                }
            }

            return skies;
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
