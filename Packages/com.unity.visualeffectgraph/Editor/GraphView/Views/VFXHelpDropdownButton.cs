using System.Linq;

using UnityEditor.Experimental;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace UnityEditor.VFX.UI
{
    class VFXHelpDropdownButton : DropDownButtonBase
    {
        const string k_AdditionalSamples = "VisualEffectGraph Additions";
        const string k_AdditionalHelpers = "OutputEvent Helpers";
        const string k_LearningSamples = "Learning Templates";
        const string k_ManualUrl = @"https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@{0}/index.html";

        string m_ManualUrlWithVersion;
        ListRequest m_PackageManagerRequest;

        public VFXHelpDropdownButton(VFXView vfxView)
            : base(
                nameof(VFXHelpDropdownButton),
                vfxView,
                "VFXHelpDropdownPanel",
                "Open the user manual of Visual Effect Graph",
                "help-button",
                EditorResources.iconsPath + "_Help.png",
                true)
        {
            var installSamplesButton = m_PopupContent.Q<Button>("installSamples");
            installSamplesButton.clicked += () => InstallSample(k_AdditionalSamples);

            var installHelpersButton = m_PopupContent.Q<Button>("graphAddition");
            installHelpersButton.clicked += () => InstallSample(k_AdditionalHelpers);

            var installLearningButton = m_PopupContent.Q<Button>("learningSamples");
            installLearningButton.clicked += () => InstallSample(k_LearningSamples);
        }

        protected override Vector2 GetPopupSize() => new Vector2(200, 224);

        protected override void OnMainButton()
        {
            if (string.IsNullOrEmpty(m_ManualUrlWithVersion)) 
                m_ManualUrlWithVersion = DocumentationInfo.GetDefaultPackageLink(Documentation.packageName);

            GotoUrl(m_ManualUrlWithVersion);
        }

        void GotoUrl(string url) => Help.BrowseURL(url);

        void InstallSample(string sampleName)
        {
            var sample = Sample.FindByPackage(VisualEffectGraphPackageInfo.name, null).SingleOrDefault(x => x.displayName == sampleName);
            if (!string.IsNullOrEmpty(sample.displayName))
            {
                if (!sample.isImported)
                {
                    sample.Import();
                }
                else
                {
                    var reinstall = EditorUtility.DisplayDialog("Warning", "This sample package is already installed.\nDo you want to reinstall it?", "Yes", "No");
                    if (reinstall)
                    {
                        sample.Import(Sample.ImportOptions.OverridePreviousImports);
                    }
                }
            }
            else
            {
                Debug.LogWarning($"Could not find sample package {sampleName}");
            }
        }
    }
}
