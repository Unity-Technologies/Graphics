using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.UIElements;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

/// <remarks>
/// In the package.json, an array can be added after the path variable of the sample. The path should start from the Packages/ folder, as such:
/// "samples": [
/// {
///     "displayName": "Sample name",
///     "description": "Sample description",
///     "path": "Samples~/Stuff",
///     "dependencies": 
///         [
///             "com.unity.render-pipelines.core/Samples~/CommonMeshes",
///             "com.unity.render-pipelines.core/Samples~/CommonTextures",
///             "com.unity.render-pipelines.universal/Samples~/CommonURPMaterials",
///             "com.unity.render-pipelines.high-definition/Samples~/CommonHDRPMaterials",
///         ]
/// },
/// {
/// </remarks>

[InitializeOnLoad]
internal class SampleDependencyImporter : IPackageManagerExtension
{
    internal static SampleDependencyImporter instance { get; private set; }

    static SampleDependencyImporter()
    {
        instance = new SampleDependencyImporter();
        PackageManagerExtensions.RegisterExtension(instance);
    }

    bool importingTextMeshProEssentialResources = false;

    PackageInfo m_PackageInfo;
    SampleList m_SampleList;
    List<Sample> m_Samples;

    VisualElement injectingElement;
    VisualElement _panelRoot;
    VisualElement panelRoot
    {
        get
        {
            _panelRoot ??= injectingElement.panel.visualTree;
            return _panelRoot;
        }
    }

    /// <summary>
    /// Use the extension UI to "inject" an invisible element in package manager UI
    /// that will serve as a base to hook up additional logic to the import buttons.
    /// </summary>
    VisualElement IPackageManagerExtension.CreateExtensionUI()
    {
        injectingElement = new VisualElement();
        injectingElement.style.display = DisplayStyle.None;

        // This callback is called once the element is added to the UI, at this point we should have access to rest of the elements.
        injectingElement.RegisterCallback<AttachToPanelEvent>((callback) => {
            //Force clear the cached elements to fetch those from the newly openned window
            _panelRoot = null;
            samplesButton = null;

            RefreshSampleButtons();
        });

        return injectingElement;
    }

    Button samplesButton;
    const string samplesButtonName = "samplesButton";
    const string sampleContainerClassName = "sampleContainer";
    const string importButtonClassName = "importButton";
    const string injectedButtonClassName = "importWithDependenciesButton";

    void RefreshSampleButtons()
    {
        if (injectingElement == null || m_PackageInfo == null || m_SampleList == null)
            return;

        // Call refresh of samples and button injection when switching to the "Samples" tab.
        if (samplesButton == null )
        {
            samplesButton = panelRoot.Q<Button>(name: samplesButtonName);
            if (samplesButton != null)
                samplesButton.clicked += RefreshSampleButtons;
        }

        // Get all of the samples container elements.
        var query = panelRoot.Query(className: sampleContainerClassName);
        query.Build();
        var sampleContainers = query.ToList();

        var bound = Mathf.Min(sampleContainers.Count, m_SampleList.samples.Length);

        for (int i=0; i<bound; i++)
        {
            // Check if the sample has dependencies, if not just skip the injection.
            var sampleInfo = m_SampleList.samples[i];
            if (sampleInfo.dependencies == null || sampleInfo.dependencies.Length == 0)
                continue;

            // Inject the button if not already.
            var sampleContainer = sampleContainers[i];
            var injectedButton = sampleContainer.Q<Button>(className: injectedButtonClassName);

            if (injectedButton == null)
            {
                // Get and hide the original import button.
                var importButton = sampleContainer.Q<Button>(className: importButtonClassName);
                importButton.style.display = DisplayStyle.None;

                // Create a new button copying the original one with our additional class.
                injectedButton = new Button();
                foreach (var c in importButton.GetClasses())
                    injectedButton.AddToClassList(c);
                injectedButton.AddToClassList(injectedButtonClassName);
                injectedButton.text = importButton.text;

                // Add the new button at the same place as the original one.
                importButton.parent.Insert(importButton.parent.IndexOf(importButton), injectedButton);

                // Need to copy i for the lambda.
                var index = i;
                // On click of the imported button, import the dependencies first then call the original button logic.
                injectedButton.clicked += () => {
                    ImportSampleDependencies(index);

                    using (var ev = NavigationSubmitEvent.GetPooled())
                    {
                        ev.target = importButton;
                        importButton.SendEvent(ev);
                    }
                };
            }
        };
    }

    public void OnPackageAddedOrUpdated(PackageInfo packageInfo) {}
    public void OnPackageRemoved(PackageInfo packageInfo) {}

    /// <summary>
    /// Called when the package selection changes in the Package Manager window.
    /// The dependency importer will track the selected package and its sample configuration.
    /// </summary>
    void IPackageManagerExtension.OnPackageSelectionChange(PackageInfo packageInfo)
    {
        m_PackageInfo = packageInfo;

        if (packageInfo == null)
            return;

        // Only trigger the import if the package has samples. 
        if (new List<Sample>(Sample.FindByPackage(packageInfo.name, packageInfo.version)).Count > 0)
        {
            TryLoadSampleConfiguration(m_PackageInfo, out m_SampleList);
        }
        else
        {
            m_PackageInfo = null;
            m_SampleList = null;
        }

        RefreshSampleButtons();
    }

    /// <summary>
    /// Load the sample configuration for the specified package, if one is available.
    /// </summary>
    static bool TryLoadSampleConfiguration(PackageInfo packageInfo, out SampleList configuration)
    {
        var configurationPath = $"{packageInfo.assetPath}/package.json";
        if (File.Exists(configurationPath))
        {
            var configurationText = File.ReadAllText(configurationPath);
            configuration = JsonUtility.FromJson<SampleList>(configurationText);

            return true;
        }

        configuration = null;
        return false;
    }

    /// <summary>
    /// Imports a sample dependencies by sample index in the list of samples of the package.
    /// </summary>
    void ImportSampleDependencies( int sampleIndex )
    {
        if (m_SampleList != null && m_SampleList.samples != null && m_SampleList.samples.Length > sampleIndex)
            ImportSampleDependencies(m_SampleList.samples[sampleIndex]);
    }

    /// <summary>
    /// Imports a sample dependencies by sample information.
    /// </summary>
    void ImportSampleDependencies(SampleInformation sampleInformation )
    {
        if (sampleInformation == null) return;

        bool assetsImported = ImportDependencies(sampleInformation.dependencies);
        ImportTextMeshProEssentialResources();

        if ( assetsImported)
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
    }


    /// <summary>
    /// Imports a sample dependencies from PackageInfo and Sample struct.
    /// </summary>
    internal void ImportSampleDependencies( PackageInfo packageInfo, Sample sample )
    {
        if (TryLoadSampleConfiguration(packageInfo, out var sampleList))
        {
            if (sampleList.samples != null && sampleList.samples.Length > 0)
            {
                for (int i=0; i<sampleList.samples.Length; i++)
                {
                    if ( sampleList.samples[i].displayName == sample.displayName )
                    {
                        ImportSampleDependencies(sampleList.samples[i]);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Import TMP Essential Resources folder to avoid having a popup on scene open.
    /// </summary>
    public void ImportTextMeshProEssentialResources()
    {
        string essentialResourcesFolder = Path.GetFullPath("Assets/TextMesh Pro");
        bool essentialResourcesImported = Directory.Exists(essentialResourcesFolder);
        // If the folder exists and we were importing, this means the import is done. 
        if (importingTextMeshProEssentialResources && essentialResourcesImported)
            importingTextMeshProEssentialResources = false;

        string packageFullPath = Path.GetFullPath("Packages/com.unity.ugui");
        if (Directory.Exists(packageFullPath) && !importingTextMeshProEssentialResources && !essentialResourcesImported)
        {
            importingTextMeshProEssentialResources = true;
            AssetDatabase.ImportPackage(packageFullPath + "/Package Resources/TMP Essential Resources.unitypackage", interactive: false);
        }
    }
    
    /// <summary>
    /// Returns the properties of the samples based on the sample displayName
    /// </summary>
    public SampleInformation GetSampleInformation(string sampleName)
    {
        foreach(SampleInformation sample in m_SampleList.samples)
        {
            if(sample.displayName == sampleName)
            {
                return sample;
            }
        }
        
        return null;
    }

    /// <summary>
    /// Imports specified dependencies from the package into the project.
    /// </summary>
    static bool ImportDependencies(string[] paths)
    {
        if (paths == null)
            return false;

        var assetsImported = false;
        for (int i = 0; i < paths.Length; ++i)
        {
            var dependencyPath = Path.GetFullPath($"Packages/{paths[i]}");
            if (Directory.Exists(dependencyPath))
            {
                //Getting the PackageInfo from the path to be able to retrieve the displayName and version of the package. 
                PackageInfo currentDependencyPackageInfo = PackageInfo.FindForAssetPath(dependencyPath);
                //Split the path from the package folder into an array of folders
                string[] foldersArray = paths[i].Split('/'); 
                //Last folder is the one we want to copy
                string folderToCopyName = foldersArray[Mathf.Max(foldersArray.Length-1,0)]; 
              
                CopyDirectory(dependencyPath, $"{Application.dataPath}/Samples/{currentDependencyPackageInfo.displayName}/{currentDependencyPackageInfo.version}/{folderToCopyName}");
                assetsImported = true;
            }
            else
            {
                Debug.LogError($"The dependency located at {dependencyPath} does not exists and has not been imported. Make sure the package of the dependency is imported in the project.");
            }
        }

        return assetsImported;
    }

    /// <summary>
    /// Copies a directory from the source to target path. Overwrites existing directories.
    /// </summary>
    static void CopyDirectory(string sourcePath, string targetPath)
    {
        // Verify source directory
        var source = new DirectoryInfo(sourcePath);
        if (!source.Exists)
            throw new DirectoryNotFoundException($"{sourcePath} directory not found");

        // Delete pre-existing directory at target path
        var target = new DirectoryInfo(targetPath);
        if (target.Exists)
            target.Delete(true);

        Directory.CreateDirectory(targetPath);

        // Copy all files to target path
        foreach (FileInfo file in source.GetFiles())
        {
            var newFilePath = Path.Combine(targetPath, file.Name);
            file.CopyTo(newFilePath);
        }

        // Recursively copy all subdirectories
        foreach (DirectoryInfo child in source.GetDirectories())
        {
            var newDirectoryPath = Path.Combine(targetPath, child.Name);
            CopyDirectory(child.FullName, newDirectoryPath);
        }
    }
}

