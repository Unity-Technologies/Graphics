#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    public class RequiredSettingsSO_HDRP : RequiredSettingsSOT<RequiredSettingHDRP>
    {
    }

    [System.Serializable]
    public class RequiredSettingHDRP : RequiredSettingBase
    {
        [SerializeField, HDRPSettingsSection()]
        public int uiSectionInt = 0;
        [SerializeField, HDRPSettingsSection("uiSectionInt")]
        public int uiSubSectionInt = 0;

        public override string projectSettingsPath => "Project/Quality/HDRP";
    }

    public class HDRPSettingsSectionAttribute : PropertyAttribute
    {
        public HDRPSettingsSectionAttribute(string rootSection = null)
        {
            this.rootSection = rootSection;
        }
        public enum SettingsGroup
        {
            Root,
            Rendering,
            Decal,
            Lighting,
            LightingQuality,
            PostProcess,
            PostProcessQuality,
            Shadows,
            Qualities
        }
        public SettingsGroup settingsGroup;
        public string rootSection;
    }
}
#endif
