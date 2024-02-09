# if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;

namespace UnityEngine.Rendering
{
    public class RequiredSettingsSOT<T> : RequiredSettingsSO where T : RequiredSettingBase
    {
        [SerializeField]
        public List<T> m_requiredSettings;

        public override List<RequiredSettingBase> requiredSettings => new List<RequiredSettingBase>(m_requiredSettings);


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

    public abstract class RequiredSettingsSO : ScriptableObject, RequiredSettingsSOI
    {
        public abstract List<RequiredSettingBase> requiredSettings { get; }
    }

    public interface RequiredSettingsSOI
    {
        public List<RequiredSettingBase> requiredSettings { get; }
    }
}
#endif