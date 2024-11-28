using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition
{
    [FilePath("UserSettings/HDRPUserSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    class HDUserSettings : ScriptableSingleton<HDUserSettings>
    {
        [SerializeField]
        bool m_WizardPopupAlreadyShownOnce = false;
        [SerializeField]
        bool m_WizardNeedRestartAfterChangingToDX12 = false;
        [SerializeField]
        bool m_WizardNeedToRunFixAllAgainAfterDomainReload = false;
        [SerializeField]
        InclusiveMode m_WizardFixAllAfterDomainReloadInclusiveMode;
        [SerializeField]
        bool m_WizardPopupAtStart = true;
        [SerializeField]
        List<int> m_OpenConfigs = new List<int>() {(int)InclusiveMode.HDRP};

        public static bool IsOpen(InclusiveMode mode)
        {
            return instance.m_OpenConfigs.Contains((int)mode);
        }

        public static void SetOpen(InclusiveMode mode, bool open)
        {
            bool contains = instance.m_OpenConfigs.Contains((int)mode);
            switch (open)
            {
                case true when !contains:
                    instance.m_OpenConfigs.Add((int)mode);
                    break;
                case false when contains:
                    instance.m_OpenConfigs.Remove((int)mode);
                    break;
            }
            instance.Save();
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

        public static InclusiveMode wizardFixAllAfterDomainReloadInclusiveMode
        {
            get => instance.m_WizardFixAllAfterDomainReloadInclusiveMode;
            set
            {
                instance.m_WizardFixAllAfterDomainReloadInclusiveMode = value;
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

        public static bool wizardIsStartPopup
        {
            get
            {
                if (!InternalEditorUtility.isHumanControllingUs || AssetDatabase.IsAssetImportWorkerProcess())
                    return false;

                return instance.m_WizardPopupAtStart;
            }
            set
            {
                instance.m_WizardPopupAtStart = value;
                instance.Save();
            }
        }

        void Save()
            => Save(true);
    }
}
