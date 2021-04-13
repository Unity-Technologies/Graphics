using UnityEngine.Rendering.HighDefinition;
using System;

using Object = UnityEngine.Object;

namespace UnityEditor.Rendering.HighDefinition
{
    class SerializedFrameSettings
    {
        SerializedProperty m_RootData;
        SerializedProperty m_RootOverrides;
        SerializedBitArray128 m_BitDatas;
        SerializedBitArray128 m_BitOverrides;
        public SerializedProperty sssQualityMode;
        public SerializedProperty sssQualityLevel;
        public SerializedProperty sssCustomSampleBudget;
        public SerializedProperty lodBias;
        public SerializedProperty lodBiasMode;
        public SerializedProperty lodBiasQualityLevel;
        public SerializedProperty maximumLODLevel;
        public SerializedProperty maximumLODLevelMode;
        public SerializedProperty maximumLODLevelQualityLevel;
        public SerializedProperty materialQuality;

        public SerializedObject serializedObject => m_RootData.serializedObject;

        public LitShaderMode? litShaderMode
        {
            get
            {
                bool? val = IsEnabled(FrameSettingsField.LitShaderMode);
                return val == null
                    ? (LitShaderMode?)null
                    : val.Value == true
                    ? LitShaderMode.Deferred
                    : LitShaderMode.Forward;
            }
            set => SetEnabled(FrameSettingsField.LitShaderMode, value == LitShaderMode.Deferred);
        }

        public bool? IsEnabled(FrameSettingsField field)
            => HaveMultipleValue(field) ? (bool?)null : m_BitDatas.GetBitAt((uint)field);
        public void SetEnabled(FrameSettingsField field, bool value)
            => m_BitDatas.SetBitAt((uint)field, value);
        public bool HaveMultipleValue(FrameSettingsField field)
            => m_BitDatas.HasBitMultipleDifferentValue((uint)field);

        public bool GetOverrides(FrameSettingsField field)
            => m_BitOverrides?.GetBitAt((uint)field) ?? false; //rootOverride can be null in case of hdrpAsset defaults
        public void SetOverrides(FrameSettingsField field, bool value)
            => m_BitOverrides?.SetBitAt((uint)field, value); //rootOverride can be null in case of hdrpAsset defaults
        public bool HaveMultipleOverride(FrameSettingsField field)
            => m_BitOverrides?.HasBitMultipleDifferentValue((uint)field) ?? false;

        ref FrameSettings GetData(Object obj)
        {
            if (obj is HDAdditionalCameraData)
                return ref (obj as HDAdditionalCameraData).renderingPathCustomFrameSettings;
            if (obj is HDProbe)
                return ref (obj as HDProbe).frameSettings;
            if (obj is HDRenderPipelineAsset)
                switch (HDRenderPipelineUI.selectedFrameSettings)
                {
                    case HDRenderPipelineUI.SelectedFrameSettings.Camera:
                        return ref (obj as HDRenderPipelineAsset).GetDefaultFrameSettings(FrameSettingsRenderType.Camera);
                    case HDRenderPipelineUI.SelectedFrameSettings.BakedOrCustomReflection:
                        return ref (obj as HDRenderPipelineAsset).GetDefaultFrameSettings(FrameSettingsRenderType.CustomOrBakedReflection);
                    case HDRenderPipelineUI.SelectedFrameSettings.RealtimeReflection:
                        return ref (obj as HDRenderPipelineAsset).GetDefaultFrameSettings(FrameSettingsRenderType.RealtimeReflection);
                    default:
                        throw new System.ArgumentException("Unknown kind of HDRenderPipelineUI.SelectedFrameSettings");
                }
            throw new System.ArgumentException("Unknown kind of object");
        }

        FrameSettingsOverrideMask? GetMask(Object obj)
        {
            if (obj is HDAdditionalCameraData)
                return (obj as HDAdditionalCameraData).renderingPathCustomFrameSettingsOverrideMask;
            if (obj is HDProbe)
                return (obj as HDProbe).frameSettingsOverrideMask;
            if (obj is HDRenderPipelineAsset)
                return null;
            throw new System.ArgumentException("Unknown kind of object");
        }

        public SerializedFrameSettings(SerializedProperty rootData, SerializedProperty rootOverrides)
        {
            m_RootData      = rootData;
            m_RootOverrides = rootOverrides;
            m_BitDatas      = rootData.FindPropertyRelative("bitDatas").ToSerializeBitArray128();
            m_BitOverrides  = rootOverrides?.FindPropertyRelative("mask").ToSerializeBitArray128();  //rootOverride can be null in case of hdrpAsset defaults

            sssQualityMode              = rootData.FindPropertyRelative("sssQualityMode");
            sssQualityLevel             = rootData.FindPropertyRelative("sssQualityLevel");
            sssCustomSampleBudget       = rootData.FindPropertyRelative("sssCustomSampleBudget");
            lodBias                     = rootData.FindPropertyRelative("lodBias");
            lodBiasMode                 = rootData.FindPropertyRelative("lodBiasMode");
            lodBiasQualityLevel         = rootData.FindPropertyRelative("lodBiasQualityLevel");
            maximumLODLevel             = rootData.FindPropertyRelative("maximumLODLevel");
            maximumLODLevelMode         = rootData.FindPropertyRelative("maximumLODLevelMode");
            maximumLODLevelQualityLevel = rootData.FindPropertyRelative("maximumLODLevelQualityLevel");
            materialQuality             = rootData.Find((FrameSettings s) => s.materialQuality);
        }

        public struct TitleDrawingScope : IDisposable
        {
            bool hasOverride;

            public TitleDrawingScope(UnityEngine.Rect rect, UnityEngine.GUIContent label, SerializedFrameSettings serialized)
            {
                EditorGUI.BeginProperty(rect, label, serialized.m_RootData);

                hasOverride = serialized.m_BitOverrides != null;
                if (hasOverride)
                    EditorGUI.BeginProperty(rect, label, serialized.m_RootOverrides);
            }

            void IDisposable.Dispose()
            {
                EditorGUI.EndProperty();
                if (hasOverride)
                    EditorGUI.EndProperty();
            }
        }
    }
}
