using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AssetPackage;
using UnityEditor.PackageManager.UI;
using UnityEngine;
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
/// </remarks>

[InitializeOnLoad]
internal static class SampleDependencyImporter
{
    static bool s_ImportingTextMeshProEssentialResources = false;

    static SampleDependencyImporter()
    {
        Sample.OnBeforeImportFinish += OnSampleImported;
    }

    static void OnSampleImported(IReadOnlyList<SampleImportEventData> events)
    {
        bool dependenciesImported = false;

        foreach (var data in events)
        {
            var packageInfo = PackageInfo.FindForPackageName(data.packageTechnicalName);
            if (packageInfo == null)
                continue;

            if (TryLoadSampleConfiguration(packageInfo, out var sampleList))
            {
                var sampleInfo = FindSampleByDisplayName(sampleList, data.sampleDisplayName);
                if (sampleInfo?.dependencies != null && sampleInfo.dependencies.Length > 0)
                {
                    dependenciesImported |= ImportDependencies(sampleInfo.dependencies);
                }
            }
        }

        if (dependenciesImported)
            ImportTextMeshProEssentialResources();
    }

    static SampleInformation FindSampleByDisplayName(SampleList sampleList, string displayName)
    {
        if (sampleList?.samples == null)
            return null;

        foreach (var sample in sampleList.samples)
        {
            if (sample.displayName == displayName)
                return sample;
        }
        return null;
    }

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

    static bool ImportDependencies(string[] paths)
    {
        if (paths == null)
            return false;

        var assetsImported = false;
        foreach (var path in paths)
        {
            var dependencyPath = Path.GetFullPath($"Packages/{path}");
            if (Directory.Exists(dependencyPath))
            {
                var depPackageInfo = PackageInfo.FindForAssetPath(dependencyPath);
                if (depPackageInfo == null)
                {
                    Debug.LogError($"Could not resolve package info for dependency at {dependencyPath}.");
                    continue;
                }
                var folders = path.Split('/');
                var folderName = folders[Math.Max(folders.Length - 1, 0)];

                CopyDirectory(dependencyPath,
                    $"{Application.dataPath}/Samples/{depPackageInfo.displayName}/{depPackageInfo.version}/{folderName}");
                assetsImported = true;
            }
            else
            {
                Debug.LogError($"Dependency at {dependencyPath} does not exist. Ensure the package is imported.");
            }
        }

        return assetsImported;
    }

    static void ImportTextMeshProEssentialResources()
    {
        string essentialResourcesFolder = Path.GetFullPath("Assets/TextMesh Pro");
        bool essentialResourcesImported = Directory.Exists(essentialResourcesFolder);

        if (s_ImportingTextMeshProEssentialResources && essentialResourcesImported)
            s_ImportingTextMeshProEssentialResources = false;

        string packageFullPath = Path.GetFullPath("Packages/com.unity.ugui");
        if (Directory.Exists(packageFullPath) && !s_ImportingTextMeshProEssentialResources && !essentialResourcesImported)
        {
            s_ImportingTextMeshProEssentialResources = true;
            Package.Import(packageFullPath + "/Package Resources/TMP Essential Resources.unitypackage", interactive: false);
        }
    }

    static void CopyDirectory(string sourcePath, string targetPath)
    {
        var source = new DirectoryInfo(sourcePath);
        if (!source.Exists)
            throw new DirectoryNotFoundException($"{sourcePath} directory not found");

        var target = new DirectoryInfo(targetPath);
        if (target.Exists)
            target.Delete(true);

        Directory.CreateDirectory(targetPath);

        foreach (FileInfo file in source.GetFiles())
            file.CopyTo(Path.Combine(targetPath, file.Name));

        foreach (DirectoryInfo child in source.GetDirectories())
            CopyDirectory(child.FullName, Path.Combine(targetPath, child.Name));
    }
}
