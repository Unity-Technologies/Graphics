using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.UIElements;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

/// <remarks>
/// To implement this, the package needs to starts with k_srpPrefixPackage
/// Then, in the package.json, an array can be added after the path variable of the sample. The path should start from the Packages/ folder, as such:
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
class SampleDependencyImporter : IPackageManagerExtension
{
    /// <summary>
    /// An implementation of AssetPostProcessor which will raise an event when a new asset is imported.
    /// </summary>
    class SamplePostprocessor : AssetPostprocessor
    {
        public static event Action<string> AssetImported;

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            for (int i = 0; i < importedAssets.Length; i++)
                AssetImported?.Invoke(importedAssets[i]);
        }
    }

    static SampleDependencyImporter()
    {
        PackageManagerExtensions.RegisterExtension(new SampleDependencyImporter());
    }
    
    const string k_unityPrefixPackage = "com.unity.";
    bool importingTextMeshProEssentialResources = false;

    PackageInfo m_PackageInfo;
    List<Sample> m_Samples;
    SampleList m_SampleList;

    VisualElement IPackageManagerExtension.CreateExtensionUI() => default;
    public void OnPackageAddedOrUpdated(PackageInfo packageInfo) {}
    public void OnPackageRemoved(PackageInfo packageInfo) {}

    /// <summary>
    /// Called when the package selection changes in the Package Manager window.
    /// The dependency importer will track the selected package and its sample configuration.
    /// </summary>
    void IPackageManagerExtension.OnPackageSelectionChange(PackageInfo packageInfo)
    {
        var isUnityPackage = packageInfo != null && packageInfo.name.StartsWith(k_unityPrefixPackage);
        
        if (isUnityPackage)
        {

           
           

                m_PackageInfo = packageInfo;
            m_Samples = GetSamples(packageInfo);
            if (TryLoadSampleConfiguration(m_PackageInfo, out m_SampleList))
            {
                SamplePostprocessor.AssetImported += LoadAssetDependencies;
            }
        }
        else
        {
            m_PackageInfo = null;
            SamplePostprocessor.AssetImported -= LoadAssetDependencies;
        }
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
    /// Handles loading common asset dependencies if required.
    /// </summary>
    void LoadAssetDependencies(string assetPath)
    {

        ImportTextMeshProEssentialResources();

        if (m_SampleList != null)
        {
            var assetsImported = false;

            for (int i = 0; i < m_Samples.Count; ++i)
            {
                string pathPrefix = $"Assets/Samples/{m_PackageInfo.displayName}/{m_PackageInfo.version}/";
                // Import dependencies if we are importing the root directory of the sample. 
                // We also test the start of the path to avoid triggering the import if an asset is imported that has the same name of a sample
                var isSampleDirectory = assetPath.EndsWith(m_Samples[i].displayName) && assetPath.StartsWith(pathPrefix);
                if (isSampleDirectory)
                {
                    // Retrieving the dependencies of the sample that is currently being imported.
                    SampleInformation currentSampleInformation = GetSampleInformation(m_Samples[i].displayName);

                    if (currentSampleInformation != null)
                    {
                        // Import the common asset dependencies
                        assetsImported = ImportDependencies(m_PackageInfo, currentSampleInformation.dependencies); 
                    }
                }
            }
            
            

            if (assetsImported)
                AssetDatabase.Refresh();
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
    static bool ImportDependencies(PackageInfo packageInfo, string[] paths)
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
              
                CopyDirectory(dependencyPath, $"{Application.dataPath}/Samples/{currentDependencyPackageInfo.displayName}/{folderToCopyName}");
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
    /// Returns all samples part of the specified package.
    /// </summary>
    /// <param name="packageInfo"></param>
    /// <returns></returns>
    static List<Sample> GetSamples(PackageInfo packageInfo)
    {
        // Find all samples for the package
        var samples = Sample.FindByPackage(packageInfo.name, packageInfo.version);
        return new List<Sample>(samples);
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

