using UnityEngine;
using UnityEditorInternal;
using System.IO;

namespace UnityEditor.Rendering.HighDefinition
{
    [FilePath("UserSettings/HDRPUserSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    class HDUserSettings : ScriptableSingleton<HDUserSettings>
    {
        [SerializeField]
        bool m_WizardPopupAlreadyShownOnce = false;
        [SerializeField]
        int m_WizardActiveTab = 0;
        [SerializeField]
        bool m_WizardNeedRestartAfterChangingToDX12 = false;
        [SerializeField]
        bool m_WizardNeedToRunFixAllAgainAfterDomainReload = false;

        public static int wizardActiveTab
        {
            get => instance.m_WizardActiveTab;
            set
            {
                instance.m_WizardActiveTab = value;
                instance.Save();
            }
        }

        public static bool wizardPopupAlreadyShownOnce
        {
            get => instance.m_WizardPopupAlreadyShownOnce;
            set
            {
                instance.m_WizardPopupAlreadyShownOnce = value;
                instance.Save();
            }
        }

        public static bool wizardNeedToRunFixAllAgainAfterDomainReload
        {
            get => instance.m_WizardNeedToRunFixAllAgainAfterDomainReload;
            set
            {
                instance.m_WizardNeedToRunFixAllAgainAfterDomainReload = value;
                instance.Save();
            }
        }

        public static bool wizardNeedRestartAfterChangingToDX12
        {
            get => instance.m_WizardNeedRestartAfterChangingToDX12;
            set
            {
                instance.m_WizardNeedRestartAfterChangingToDX12 = value;
                instance.Save();
            }
        }

        void Save()
            => Save(true);
    }
}
