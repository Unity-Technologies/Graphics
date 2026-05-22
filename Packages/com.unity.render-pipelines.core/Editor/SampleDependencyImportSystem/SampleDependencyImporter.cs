using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.PackageManager;
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

    VisualElement injectingElement;
    VisualElement _panelRoot;
    VisualElement panelRoot
    {
        get
        {
            _panelRoot ??= injectingElement?.panel?.visualTree;
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

        injectingElement.RegisterCallback<AttachToPanelEvent>((callback) => {
            // Clear cached elements when panel is attached
            _panelRoot = null;
            samplesButton = null;
            RefreshSampleButtons();
        });

        return injectingElement;
    }

    Button samplesButton;
    Button m_InjectedDetailsButton;
    bool m_IsOnAllSamplesTab;

    const string samplesButtonName = "samplesButton";
    const string samplesListContainerClassName = "sampleContainer";
    const string importButtonClassName = "actionButtonsContainer";
    const string injectedButtonClassName = "importWithDependenciesButton";

    internal void RefreshSampleButtons()
    {
		if (m_IsOnAllSamplesTab)
		{
			RefreshSampleButtonInDetailsView();
		}
		else
		{
			RefreshSampleButtonInListView();
		}
    }
	
	internal void RefreshSampleButtonInListView()
	{
		if (injectingElement == null || m_PackageInfo == null || m_SampleList == null || panelRoot == null)
			return;

		// Hook up to the "Samples" tab button click event
		if (samplesButton == null)
		{
			samplesButton = panelRoot.Q<Button>(name: samplesButtonName);
			if (samplesButton != null)
				samplesButton.clicked += RefreshSampleButtons;
		}

		// Delay injection to allow UI to populate sample containers
		EditorApplication.delayCall += () =>
		{
			if (panelRoot == null || m_SampleList == null)
				return;

			var sampleContainers = panelRoot.Query(className: samplesListContainerClassName).ToList();
			var bound = Mathf.Min(sampleContainers.Count, m_SampleList.samples.Length);

			// Iterate through each sample in the list
			for (int i = 0; i < bound; i++)
			{
				var sampleInfo = m_SampleList.samples[i];
				var actionButtonsContainer = sampleContainers[i].Q(name: importButtonClassName);

				if (sampleInfo.dependencies == null || sampleInfo.dependencies.Length == 0)
				{
					RestoreOriginalImportButton(actionButtonsContainer);
					continue;
				}

				SwapImportButtonToImportDependencies(actionButtonsContainer, sampleInfo);
			}
		};
	}

	internal void RefreshSampleButtonInDetailsView()
    {
		var root = GetPackageManagerWindowRoot();
		if (root == null)
			return;

		string selectedSampleName = GetSelectedSampleNameFromUI();
		if (string.IsNullOrEmpty(selectedSampleName))
			return;

		// Find the matching sample in m_SampleList
		SampleInformation matchingSample = null;
		if (m_SampleList != null && m_SampleList.samples != null)
		{
			foreach (var sample in m_SampleList.samples)
			{
				if (sample.displayName == selectedSampleName)
				{
					matchingSample = sample;
					break;
				}
			}
		}

		var actionButtonsContainer = root.Q(name: importButtonClassName);

		if (matchingSample == null || matchingSample.dependencies == null || matchingSample.dependencies.Length == 0)
		{
			RestoreOriginalImportButton(actionButtonsContainer);
			return;
		}

		SwapImportButtonToImportDependencies(actionButtonsContainer, matchingSample);
    }
	
	internal void SwapImportButtonToImportDependencies(VisualElement actionButtonsContainer, SampleInformation sampleToImport)
	{
		if (actionButtonsContainer == null)
			return;

		// Find the original import button (not our injected one)
		Button originalImportButton = null;
		var buttons = actionButtonsContainer.Query<Button>().ToList();

		foreach (var button in buttons)
		{
			bool isInjected = false;
			foreach (var className in button.GetClasses())
			{
				if (className == injectedButtonClassName)
				{
					isInjected = true;
					break;
				}
			}

			if (!isInjected)
			{
				originalImportButton = button;
				break;
			}
		}

		if (originalImportButton == null)
			return;

		// Hide the original button
		originalImportButton.style.display = DisplayStyle.None;

		// Remove any existing injected button - we need to create a fresh one
		// so the click handler captures the correct sampleToImport
		originalImportButton.parent.Q<Button>(className: injectedButtonClassName)?.RemoveFromHierarchy();

		// Create a new injected button
		Button newInjectedButton = new Button();
		foreach (var className in originalImportButton.GetClasses())
			newInjectedButton.AddToClassList(className);
		newInjectedButton.AddToClassList(injectedButtonClassName);
		newInjectedButton.text = originalImportButton.text;

		// Insert button at the same position as the original
		originalImportButton.parent.Insert(originalImportButton.parent.IndexOf(originalImportButton), newInjectedButton);

		// Capture package info at button creation time (not by reference)
		var capturedPackageInfo = m_PackageInfo;

		// Set up click handler
		newInjectedButton.clicked += () =>
		{
			if (capturedPackageInfo == null)
				return;

			ImportSampleDependencies(sampleToImport);
			foreach (Sample sample in Sample.FindByPackage(capturedPackageInfo.name, capturedPackageInfo.version))
			{
				if (sample.displayName == sampleToImport.displayName)
					sample.Import(Sample.ImportOptions.HideImportWindow | Sample.ImportOptions.OverridePreviousImports);
			}
		};

		// Track button only in details view
		if (m_IsOnAllSamplesTab)
			m_InjectedDetailsButton = newInjectedButton;
	}

	/// <summary>
	/// Removes any injected button and restores the original import button visibility.
	/// Called when switching to a sample without dependencies.
	/// </summary>
	void RestoreOriginalImportButton(VisualElement actionButtonsContainer)
	{
		if (actionButtonsContainer == null)
			return;

		// Remove injected button
		actionButtonsContainer.Q<Button>(className: injectedButtonClassName)?.RemoveFromHierarchy();

		// Restore original button visibility
		var originalButton = actionButtonsContainer.Q<Button>();
		if (originalButton != null)
			originalButton.style.display = DisplayStyle.Flex;

		if (m_IsOnAllSamplesTab)
			m_InjectedDetailsButton = null;
	}

	/// <summary>
    /// Attempts to retrieve PackageInfo from the currently selected sample.
    /// This is needed when on "All Samples" tab where packageInfo parameter is null.
    /// </summary>
    private PackageInfo TryGetPackageInfoFromSelectedSample()
    {
        try
        {
            string selectedSampleName = GetSelectedSampleNameFromUI();
            if (string.IsNullOrEmpty(selectedSampleName))
                return null;

            var listRequest = Client.List(true);

            while (!listRequest.IsCompleted)
                System.Threading.Thread.Sleep(10);

            if (listRequest.Status != StatusCode.Success)
                return null;

            // Search all packages for the selected sample
            foreach (var pkg in listRequest.Result)
            {
                var samples = Sample.FindByPackage(pkg.name, pkg.version);
                foreach (var sample in samples)
                {
                    if (sample.displayName == selectedSampleName)
                        return pkg;
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

	/// <summary>
    /// Gets the Package Manager window's root visual element.
    /// </summary>
	private VisualElement GetPackageManagerWindowRoot()
	{
		try
		{
			var windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
			foreach (var window in windows)
			{
				if (window.GetType().Name == "PackageManagerWindow")
					return window.rootVisualElement;
			}
		}
		catch { }

		return null;
	}
	
	/// <summary>
    /// Extracts the selected sample's display name from the Package Manager UI.
    /// </summary>
    private string GetSelectedSampleNameFromUI()
    {
        VisualElement root = panelRoot ?? GetPackageManagerWindowRoot();
        if (root == null)
            return null;

        // Find the details area
        var detailsArea = root.Q(className: "detailsArea") ?? root.Q(className: "detail");
        VisualElement searchRoot = detailsArea ?? root;

        // Try to find as Label first
        var sampleDisplayNameLabel = searchRoot.Q<Label>(name: "sampleDisplayName");
        if (sampleDisplayNameLabel != null && !string.IsNullOrEmpty(sampleDisplayNameLabel.text))
            return sampleDisplayNameLabel.text;

        // Try without type constraint
        var sampleDisplayNameElement = searchRoot.Q(name: "sampleDisplayName");
        if (sampleDisplayNameElement != null)
        {
            if (sampleDisplayNameElement is Label label && !string.IsNullOrEmpty(label.text))
                return label.text;
            if (sampleDisplayNameElement is TextElement textElement && !string.IsNullOrEmpty(textElement.text))
                return textElement.text;
        }

        // Broader search - find all and filter by details area
        var results = searchRoot.Query(name: "sampleDisplayName").ToList();
        if (results.Count > 0)
        {
            for (int i = results.Count - 1; i >= 0; i--)
            {
                var result = results[i];

                if (result is Label resultLabel && !string.IsNullOrEmpty(resultLabel.text))
                {
                    if (m_IsOnAllSamplesTab || results.Count == 1)
                        return resultLabel.text;
                }
                else if (result is TextElement resultTextElement && !string.IsNullOrEmpty(resultTextElement.text))
                {
                    if (m_IsOnAllSamplesTab || results.Count == 1)
                        return resultTextElement.text;
                }
            }
        }

        return null;
    }

    public void OnPackageAddedOrUpdated(PackageInfo packageInfo) {}
    public void OnPackageRemoved(PackageInfo packageInfo) {}

    /// <summary>
    /// Called when the package selection changes in the Package Manager window.
    /// </summary>
    void IPackageManagerExtension.OnPackageSelectionChange(PackageInfo packageInfo)
    {
		// This will be null if we come from the "all samples" tab
        if (packageInfo == null)
        {
            m_IsOnAllSamplesTab = true;
            EditorApplication.delayCall += () =>
            {
                var recoveredPackageInfo = TryGetPackageInfoFromSelectedSample();
                if (recoveredPackageInfo != null)
                {
                    ProcessPackageInfo(recoveredPackageInfo);
                }
                else
                {
                    m_PackageInfo = null;
                    m_SampleList = null;
                    m_IsOnAllSamplesTab = false;
                    RefreshSampleButtons();
                }
            };
            return;
        }

        m_IsOnAllSamplesTab = false;
        ProcessPackageInfo(packageInfo);
    }
	
	/// <summary>
    /// Processes the package info and loads sample configuration.
    /// </summary>
    private void ProcessPackageInfo(PackageInfo packageInfo)
    {
        m_PackageInfo = packageInfo;
		
		int sampleCount = new List<Sample>(Sample.FindByPackage(packageInfo.name, packageInfo.version)).Count;

        if (sampleCount > 0)
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
    /// Imports sample dependencies by sample information.
    /// </summary>
    void ImportSampleDependencies(SampleInformation sampleInformation)
    {
        if (sampleInformation == null)
            return;

        bool assetsImported = ImportDependencies(sampleInformation.dependencies);
        ImportTextMeshProEssentialResources();

        if (assetsImported)
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
    }

    /// <summary>
    /// Imports sample dependencies from PackageInfo and Sample struct.
    /// </summary>
    internal void ImportSampleDependencies(PackageInfo packageInfo, Sample sample)
    {
        if (TryLoadSampleConfiguration(packageInfo, out var sampleList))
        {
            if (sampleList.samples != null)
            {
                foreach (var sampleInfo in sampleList.samples)
                {
                    if (sampleInfo.displayName == sample.displayName)
                    {
                        ImportSampleDependencies(sampleInfo);
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Import TMP Essential Resources folder to avoid popup on scene open.
    /// </summary>
    public void ImportTextMeshProEssentialResources()
    {
        string essentialResourcesFolder = Path.GetFullPath("Assets/TextMesh Pro");
        bool essentialResourcesImported = Directory.Exists(essentialResourcesFolder);

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
    /// Imports specified dependencies from the package into the project.
    /// </summary>
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
                PackageInfo packageInfo = PackageInfo.FindForAssetPath(dependencyPath);
                string[] folders = path.Split('/');
                string folderName = folders[Mathf.Max(folders.Length - 1, 0)];

                CopyDirectory(dependencyPath, $"{Application.dataPath}/Samples/{packageInfo.displayName}/{packageInfo.version}/{folderName}");
                assetsImported = true;
            }
            else
            {
                Debug.LogError($"The dependency at {dependencyPath} does not exist. Ensure the package is imported.");
            }
        }

        return assetsImported;
    }

    /// <summary>
    /// Copies a directory from source to target path. Overwrites existing directories.
    /// </summary>
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

internal class SampleDependencyImporterPostProcessor : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        SampleDependencyImporter.instance?.RefreshSampleButtons();
    }
}

