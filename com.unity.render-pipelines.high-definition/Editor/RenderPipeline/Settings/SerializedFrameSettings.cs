using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    class SerializedFrameSettings
    {
        SerializedProperty rootData;
        SerializedProperty rootOverride;

        public SerializedObject serializedObject => rootData.serializedObject;
        
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

        public bool? IsEnabled(FrameSettingsField field) => HaveMultipleValue(field) ? (bool?)null : rootData.GetBitArrayAt((uint)field);
        public void SetEnabled(FrameSettingsField field, bool value)
        {
            if(rootData.serializedObject.isEditingMultipleObjects)
            {
                var objects = rootData.serializedObject.targetObjects;
                for (int index = 0; index < objects.Length; ++index)
                    GetData(objects[index]).SetEnabled(field, value);
            }
            else
                rootData.SetBitArrayAt((uint)field, value);
        }
        public bool HaveMultipleValue(FrameSettingsField field)
        {
            var objects = rootData.serializedObject.targetObjects;
            bool value = GetData(objects[0]).IsEnabled(field);
            for (int index = 1; index < objects.Length; ++index)
                if (value ^ (GetData(objects[index]).IsEnabled(field)))
                    return true;
            return false;
        }

        public bool GetOverrides(FrameSettingsField field) => rootOverride?.GetBitArrayAt((uint)field) ?? false; //rootOverride can be null in case of hdrpAsset defaults
        public void SetOverrides(FrameSettingsField field, bool value) => rootOverride?.SetBitArrayAt((uint)field, value); //rootOverride can be null in case of hdrpAsset defaults
        public bool HaveMultipleOverride(FrameSettingsField field)
        {
            bool value = GetOverrides(field);
            var objects = rootOverride?.serializedObject?.targetObjects;
            for (int index = 1; index < (objects?.Length ?? 0); ++index)
                if (value ^ (GetMask(objects[index])?.mask[(uint)field] ?? false))
                    return true;
            return false;
        }

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


        public SerializedFrameSettings(SerializedProperty rootData, SerializedProperty rootOverride)
        {
            this.rootData = rootData.FindPropertyRelative("bitDatas");
            this.rootOverride = rootOverride?.FindPropertyRelative("mask");  //rootOverride can be null in case of hdrpAsset defaults
        }
    }
}
