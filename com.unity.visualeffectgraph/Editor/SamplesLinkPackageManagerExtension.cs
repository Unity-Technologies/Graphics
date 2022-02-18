#if UNITY_2018_2_OR_NEWER
#define NEW_PACKMAN

using System;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.UI;
using UnityEngine;

#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#else
using UnityEngine.Experimental.UIElements;
#endif

[UnityEditor.InitializeOnLoad]
internal class SamplesLinkPackageManagerExtension : IPackageManagerExtension
{
    VisualElement rootVisualElement;
    const string SAMPLEBUTTON_TEXT = "Download VFX Graph Samples from Github";
    const string GITHUB_URL = "https://github.com/Unity-Technologies/VisualEffectGraph-Samples";
    const string VFX_GRAPH_NAME = "com.unity.visualeffectgraph";

    private Button samplesButton;
    private VisualElement parent;

    public VisualElement CreateExtensionUI()
    {
        samplesButton = new Button();
        samplesButton.text = SAMPLEBUTTON_TEXT;
        samplesButton.clickable.clicked += () => Application.OpenURL(GITHUB_URL);

        return samplesButton;
    }

    static SamplesLinkPackageManagerExtension()
    {
        PackageManagerExtensions.RegisterExtension(new SamplesLinkPackageManagerExtension());
    }

    void IPackageManagerExtension.OnPackageSelectionChange(PackageInfo packageInfo)
    {
        // Prevent the button from rendering on other packages
        if (samplesButton.parent != null)
            parent = samplesButton.parent;

        bool shouldRender = packageInfo?.name == VFX_GRAPH_NAME;
        if (!shouldRender)
        {
            samplesButton.RemoveFromHierarchy();
        }
        else
        {
            parent.Add(samplesButton);
        }
    }

    void IPackageManagerExtension.OnPackageAddedOrUpdated(PackageInfo packageInfo) { }

    void IPackageManagerExtension.OnPackageRemoved(PackageInfo packageInfo) { }
}

#endif