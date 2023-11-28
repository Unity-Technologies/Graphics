using UnityEngine.Rendering.HighDefinition;
using System;
using Object = UnityEngine.Object;

namespace UnityEditor.Rendering.HighDefinition
{
    class SerializedFrameSettings
    {
        public class Data
        {
            internal SerializedObject[] bitArrayTargetSerializedObjects;
            internal SerializedProperty root;
            SerializedBitArrayAny m_BitDatas;
            public SerializedProperty sssQualityMode;
            public SerializedProperty sssQualityLevel;
            public SerializedProperty sssCustomSampleBudget;
            public SerializedProperty sssDownsampleSteps;
            public SerializedProperty lodBias;
            public SerializedProperty lodBiasMode;
            public SerializedProperty lodBiasQualityLevel;
            public SerializedProperty maximumLODLevel;
            public SerializedProperty maximumLODLevelMode;
            public SerializedProperty maximumLODLevelQualityLevel;
            public SerializedProperty materialQuality;
            public SerializedProperty msaaMode;

            public Object[] targetObjects => root.serializedObject.targetObjects;

            public LitShaderMode? litShaderMode
            {
                get
                {
                    bool? val = GetEnabled(FrameSettingsField.LitShaderMode);
                    return val == null
                        ? (LitShaderMode?)null
                        : val.Value == true
                        ? LitShaderMode.Deferred
                        : LitShaderMode.Forward;
                }
                set => SetEnabled(FrameSettingsField.LitShaderMode, value == LitShaderMode.Deferred);
            }

            public bool? GetEnabled(FrameSettingsField field)
                => HasMultipleDifferentValues(field) ? (bool?)null : GetEnabledUnchecked(field);
            public bool GetEnabledUnchecked(FrameSettingsField field)
                => m_BitDatas.GetBitAt((uint)field);
            public void SetEnabled(FrameSettingsField field, bool value)
                => m_BitDatas.SetBitAt((uint)field, value);
            public bool HasMultipleDifferentValues(FrameSettingsField field)
                => m_BitDatas.HasBitMultipleDifferentValue((uint)field);

            public Data(SerializedProperty rootData, SerializedObject[] bitArrayTargetSerializedObjects = null)
            {
                root = rootData;
                this.bitArrayTargetSerializedObjects = bitArrayTargetSerializedObjects ?? Helper.GetBitArrayTargetSerializedObjects(rootData.serializedObject);
                m_BitDatas = rootData.FindPropertyRelative("bitDatas").ToSerializedBitArray(this.bitArrayTargetSerializedObjects);
            
                sssQualityMode = rootData.FindPropertyRelative("sssQualityMode");
                sssQualityLevel = rootData.FindPropertyRelative("sssQualityLevel");
                sssCustomSampleBudget = rootData.FindPropertyRelative("sssCustomSampleBudget");
                sssDownsampleSteps = rootData.FindPropertyRelative("sssCustomDownsampleSteps");
                lodBias = rootData.FindPropertyRelative("lodBias");
                lodBiasMode = rootData.FindPropertyRelative("lodBiasMode");
                lodBiasQualityLevel = rootData.FindPropertyRelative("lodBiasQualityLevel");
                maximumLODLevel = rootData.FindPropertyRelative("maximumLODLevel");
                maximumLODLevelMode = rootData.FindPropertyRelative("maximumLODLevelMode");
                maximumLODLevelQualityLevel = rootData.FindPropertyRelative("maximumLODLevelQualityLevel");
                materialQuality = rootData.FindPropertyRelative("materialQuality");
                msaaMode = rootData.FindPropertyRelative("msaaMode");
            }
            
            public void Update() => root.serializedObject.Update();
            public void ApplyModifiedProperties() => root.serializedObject.ApplyModifiedProperties();
        }

        public class Mask
        {
            internal SerializedProperty root;
            SerializedBitArrayAny m_BitOverrides;

            public bool? GetOverrided(FrameSettingsField field)
                => HasMultipleDifferentOverrides(field) ? (bool?)null : GetOverridedUnchecked(field);
            public bool GetOverridedUnchecked(FrameSettingsField field)
                => m_BitOverrides.GetBitAt((uint)field);
            public void SetOverrided(FrameSettingsField field, bool value)
                => m_BitOverrides.SetBitAt((uint)field, value);
            public bool HasMultipleDifferentOverrides(FrameSettingsField field)
                => m_BitOverrides.HasBitMultipleDifferentValue((uint)field);

            public Mask(SerializedProperty rootOverrides, Data rootData)
            {
                root = rootOverrides;
                m_BitOverrides = rootOverrides.FindPropertyRelative("mask").ToSerializedBitArray(rootData.bitArrayTargetSerializedObjects);
            }
            
            public void Update() => root.serializedObject.Update();
            public void ApplyModifiedProperties() => root.serializedObject.ApplyModifiedProperties();
        }
        
        public static class Helper
        {
            public static SerializedObject[] GetBitArrayTargetSerializedObjects(SerializedObject so)
            {
                var bitArrayTargetObjects = so.targetObjects;
                SerializedObject[] bitArrayTargetSerializedObjects = new SerializedObject[bitArrayTargetObjects.Length];
                for (int i = 0; i < bitArrayTargetObjects.Length; i++)
                {
                    bitArrayTargetSerializedObjects[i] = new SerializedObject(bitArrayTargetObjects[i]);
                }
                return bitArrayTargetSerializedObjects;
            }

        }
        
        public Data data { get; private set; }
        public Mask mask { get; private set; }

        #region Retarget API to keep compatibility
        public Object[] targetObjects => data.targetObjects;
        public LitShaderMode? litShaderMode
        {
            get => data.litShaderMode;
            set => data.litShaderMode = value;
        }

        public bool? IsEnabled(FrameSettingsField field) => data.GetEnabled(field);
        public void SetEnabled(FrameSettingsField field, bool value) => data.SetEnabled(field, value);
        public bool HasMultipleDifferentValues(FrameSettingsField field) => data.HasMultipleDifferentValues(field);

        public bool? GetOverrided(FrameSettingsField field) => mask.GetOverrided(field);
        public void SetOverrided(FrameSettingsField field, bool value) => mask.SetOverrided(field, value);
        public bool HasMultipleDifferentOverrides(FrameSettingsField field) => mask.HasMultipleDifferentOverrides(field);
        #endregion

        public SerializedFrameSettings(SerializedProperty rootDatas, SerializedProperty rootOverrides)
        {
            var targets = Helper.GetBitArrayTargetSerializedObjects(rootDatas.serializedObject);
            data = new(rootDatas, targets);

            //rootOverride can be null in case of hdrpAsset defaults
            if (rootOverrides == null)
                return;
            
            mask = new(rootOverrides, data);
        }
        
        public void Update() => data.Update();
        public void ApplyModifiedProperties() => data.ApplyModifiedProperties();
    }
}
