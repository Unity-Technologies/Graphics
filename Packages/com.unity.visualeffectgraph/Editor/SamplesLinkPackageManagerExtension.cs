using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VFX
{

[UnityEditor.InitializeOnLoad]
class SamplesLinkPackageManagerExtension : IPackageManagerExtension
{
    VisualElement rootVisualElement;
    const string SAMPLEBUTTON_TEXT = "Open VFX Graph Samples project on Github";
    const string GITHUB_URL = "https://github.com/Unity-Technologies/VisualEffectGraph-Samples";

    private Button samplesButton;
    private VisualElement parent;

    public VisualElement CreateExtensionUI()
    {
        samplesButton = new Button();
        samplesButton.text = SAMPLEBUTTON_TEXT;
        samplesButton.clickable.clicked += () => Application.OpenURL(GITHUB_URL);
        return samplesButton;
    }

    void IPackageManagerExtension.OnPackageSelectionChange(PackageManager.PackageInfo packageInfo)
    {
        if (samplesButton == null)
            return;

        // Prevent the button from rendering on other packages
        if (samplesButton.parent != null)
            parent = samplesButton.parent;

        bool shouldRender = packageInfo?.name == VisualEffectGraphPackageInfo.name;
        if (!shouldRender)
        {
            samplesButton.RemoveFromHierarchy();
        }
        else
        {
            parent.Add(samplesButton);
        }
    }

    void IPackageManagerExtension.OnPackageAddedOrUpdated(PackageManager.PackageInfo packageInfo) { }

    void IPackageManagerExtension.OnPackageRemoved(PackageManager.PackageInfo packageInfo) { }

    static SamplesLinkPackageManagerExtension()
    {
        PackageManagerExtensions.RegisterExtension(new SamplesLinkPackageManagerExtension());
    }
}

}

