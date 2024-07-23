# if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;

namespace UnityEngine.Rendering
{
    public class PRSRequiredSettingsSOT<T> : PRSRequiredSettingsSO where T : PRSRequiredSettingBase
    {
        [SerializeField]
        public List<T> m_requiredSettings;

        public override List<PRSRequiredSettingBase> requiredSettings => new List<PRSRequiredSettingBase>(m_requiredSettings);


        public bool allGood
        {
            get
            {
                if (requiredSettings == null || requiredSettings.Count == 0)
                    return true;

                foreach(var setting in requiredSettings)
                {
                    if (!setting.state)
                        return false;
                }
                return true;
            }
        }
    }

    public abstract class PRSRequiredSettingsSO : ScriptableObject, PRSRequiredSettingsSOI
    {
        public abstract List<PRSRequiredSettingBase> requiredSettings { get; }
    }

    public interface PRSRequiredSettingsSOI
    {
        public List<PRSRequiredSettingBase> requiredSettings { get; }
    }
}
#endif