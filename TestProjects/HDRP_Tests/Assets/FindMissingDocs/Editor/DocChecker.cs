using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System;
using System.Linq;
using System.IO;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.Rendering.HighDefinition;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditor;
using System.Diagnostics;
using System.Linq.Expressions;
using Debug = UnityEngine.Debug;

[Serializable]
public class DocChecker : EditorWindow
{
    const string key = "HDRP Documnetation Checker";

    ListRequest         packageListRequest;
    PackageCollection   collection;
    string[]            packageNames;
    Event               e => Event.current;

    int                 selectedPackage
    {
        get => EditorPrefs.GetInt($"{key} selectedPackage", 0);
        set => EditorPrefs.SetInt($"{key} selectedPackage", value);
    }
    string              outputFolder
    {
        get => EditorPrefs.GetString($"{key} outputFolder", "");
        set => EditorPrefs.SetString($"{key} outputFolder", value);
    }
    [SerializeField]
    bool                excludeTests
    {
        get => EditorPrefs.GetBool($"{key} excludeTests", true);
        set => EditorPrefs.SetBool($"{key} excludeTests", value);
    }
    
    public string undocumentedEntitiesFilePath => outputFolder + "/" + "undocumented_entities.txt";

    [MenuItem ("Window/Doc Checker")]
    public new static void Show() => EditorWindow.GetWindow<DocChecker>();

    void OnGUI ()
    {
        UpdatePackagesList();

        if (IsLoading())
        {
            EditorGUILayout.LabelField("Loading Packages ...");
            return ;
        }

        if (collection == null)
            return;

        packageNames = collection.Select(c => c.name).ToArray();

        GUIStyle labelStyle = EditorStyles.largeLabel;
        labelStyle.wordWrap = true;

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("1. First select a target package and an output directory.", labelStyle);
            EditorGUILayout.Space();
            selectedPackage = EditorGUILayout.Popup("Target Package", selectedPackage, packageNames);
            EditorGUILayout.HelpBox("Note that it only works on local packages", MessageType.Info);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"Output Folder: {outputFolder}");
                if (GUILayout.Button("Select"))
                    outputFolder = EditorUtility.OpenFolderPanel("Documentation Output", "", "");
                if (GUILayout.Button("Show In Explorer"))
                    Application.OpenURL(outputFolder);
            }
            excludeTests = EditorGUILayout.Toggle("Exclude Tests", excludeTests);
        }

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("2. Then List all the undocumented entities of the selected package.", labelStyle);
            EditorGUILayout.Space();
            if (GUILayout.Button("Generate Undocumented List"))
            {
                GenerateDocumentation();
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            if (IsGenerated())
            {
                EditorGUILayout.LabelField("3. Open the result file.", labelStyle);
                EditorGUILayout.Space();
                if (GUILayout.Button("Show Undocumented Classes"))
                    Application.OpenURL(undocumentedEntitiesFilePath);
            }
        }
    }

    void UpdatePackagesList()
    {
        if (e.type != EventType.Layout)
            return;

        if (packageListRequest == null && collection == null)
            packageListRequest = Client.List();

        if (packageListRequest != null && packageListRequest.IsCompleted)
        {
            collection = packageListRequest.Result;
            packageListRequest = null;
        }
    }

    bool IsLoading() => packageListRequest != null && collection == null;
    bool IsGenerated() => File.Exists(undocumentedEntitiesFilePath);

    void GenerateDocumentation()
    {
        FindMissingDocs();
    }

    public static string GetMonoPath()
    {
        var monoPath = Path.Combine(EditorApplication.applicationContentsPath, "MonoBleedingEdge/bin", Application.platform == RuntimePlatform.WindowsEditor ? "mono.exe" : "mono");
        return monoPath;
    }

    void FindMissingDocs()
    {
        var monopath = GetMonoPath();
        var exePath = Path.GetFullPath("Assets/FindMissingDocs/Bin~/FindMissingDocs/FindMissingDocs.exe");

        // find the package path:
        string absolutePackagePath = Path.GetFullPath($"Packages/{packageNames[selectedPackage]}");

        List<string> excludePaths = new List<string>();
        excludePaths.AddRange(Directory.GetDirectories(absolutePackagePath, "*~", SearchOption.AllDirectories));
        excludePaths.AddRange(Directory.GetDirectories(absolutePackagePath, ".*", SearchOption.AllDirectories));
        if (excludeTests)
            excludePaths.AddRange(Directory.GetDirectories(absolutePackagePath, "Tests", SearchOption.AllDirectories));
        // foreach (var assembly in info)
        // {
        //     //exclude sources from test assemblies explicitly. Do not exclude entire directories, as there may be nested public asmdefs
        //     if (validationAssemblyInformation.IsTestAssembly(assembly) && assembly.assemblyKind == AssemblyInfo.AssemblyKind.Asmdef)
        //         excludePaths.AddRange(assembly.assembly.sourceFiles);
        // }
        string responseFileParameter = string.Empty;
        string responseFilePath = null;
        if (excludePaths.Count > 0)
        {
            responseFilePath = Path.GetTempFileName();
            var excludedPathsParameter = $@"--excluded-paths=""{string.Join(",", excludePaths.Select(s => Path.GetFullPath(s)))}""";
            File.WriteAllText(responseFilePath, excludedPathsParameter);
            responseFileParameter = $@"--response-file=""{responseFilePath}""";
        }

        EditorUtility.DisplayProgressBar("Gathering Undocumented Entities", "Nyan nyan nyan nyan nyan nyan nyan nyan nyan nyan nyan nyan nyan nyan nyan nyan nyan nyan nyan nyan nyan", 0);


        var startInfo = new ProcessStartInfo(monopath, $@"""{exePath}"" --root-path=""{absolutePackagePath}"" {responseFileParameter}")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        var process = Process.Start(startInfo);

        var stdout = new ProcessOutputStreamReader(process, process.StandardOutput);
        var stderr = new ProcessOutputStreamReader(process, process.StandardError);
        process.WaitForExit();
        var stdoutLines = stdout.GetOutput();
        var stderrLines = stderr.GetOutput();
        if (stderrLines.Length > 0)
        {
            Debug.LogError($"Internal Error running FindMissingDocs. Output:\n{string.Join("\n", stderrLines)}");
            return;
        }

        if (stdoutLines.Length > 0)
        {
            var errorMessage = FormatErrorMessage(stdoutLines);
            // Debug.Log(errorMessage);
            File.WriteAllText(undocumentedEntitiesFilePath, errorMessage);
        }

        if (responseFilePath != null)
            File.Delete(responseFilePath);
        
        EditorUtility.ClearProgressBar();
        Debug.Log("Done !");
    }

    public static string FormatErrorMessage(IEnumerable<string> expectedMessages)
    {
        return $@"The following APIs are missing documentation: {string.Join(Environment.NewLine, expectedMessages)}";
    }
}