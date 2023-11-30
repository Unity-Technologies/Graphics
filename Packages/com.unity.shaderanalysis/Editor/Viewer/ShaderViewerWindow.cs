using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.ShaderAnalysis;
using UnityEditor.ShaderAnalysis.Internal;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using ShaderAnalysisReport = UnityEditor.ShaderAnalysis.ShaderAnalysisReport;

public class ShaderViewerWindow : EditorWindow
{
    class RegisterUsageNode
    {
        public VisualElement backgroundRegisterLifetime;
        public List<VisualElement> readBars = new();
        public List<VisualElement> writeBars = new();
        public CodeLine line;
        public int registerIndex;
        public int aliveStartLineIndex;
        public int aliveEndLineIndex;

        public readonly HashSet<RegisterUsageNode> reads = new();
        public RegisterUsageNode write;

        public override int GetHashCode()
        {
            var hash = registerIndex;
            hash ^= hash * 31 + aliveStartLineIndex;
            hash ^= hash * 31 + aliveEndLineIndex;
            return hash;
        }
    }

    const int k_CodeLabelWidth = 700;
    const int k_JumpTableWidth = 40;
    [SerializeField] Object currentUnityObject;
    [SerializeField] string selectedComputeShaderKernelName;
    [SerializeField] List<string> selectedShaderKeywords = new();
    [SerializeField, SerializeReference] ShaderBuildReport.PerformanceUnit lastPerfUnit;
    [SerializeField, SerializeReference] ShaderBuildReport lastBuildReport;
    [SerializeField] int lastPlatformIndex;

    private VisualElement codeVGPRView;
    private VisualElement assemblyVGPRView;
    private VisualElement codeSGPRView;
    private VisualElement assemblySGPRView;
    private VisualElement cbufferOrderView;
    private Tab vgprAnalysisTab;
    private Tab vgprAssemblyAnalysisTab;
    private Tab cbufferReadOrderTab;
    private Tab sgprAssemblyAnalysisTab;
    private Tab sgprCodeAnalysisTab;
    private VisualElement computeShaderFilterSettings;
    private VisualElement shaderFilterSettings;
    private VisualElement materialFilterSettings;
    private Tab currentlySelectedTab;
    private DropdownField targetPlatform;

    HashSet<RegisterUsageNode> vgprGraph = new();
    private List<VisualElement> allVGPRBars = new();

    private AnalyzedShader currentAnalyzedShader;

    [MenuItem("Window/Analysis/Shader Analysis Viewer")]
    public static void ShowShaderAnalysis() => CreateWindow<ShaderViewerWindow>();

    public static void ShowReportFromShaderPerfInspector(ShaderBuildReport.PerformanceUnit p)
    {
        ShaderViewerWindow viewer = CreateWindow<ShaderViewerWindow>();
        viewer.lastPerfUnit = p;
        viewer.UpdateViewWithCurrentProgram(p);
    }

    public static void ShowReportFromFile()
    {
        var args = Environment.GetCommandLineArgs();
        string filePath = null;

        for (int i = 0; i < args.Length; i++)
            if (args[i] == "-perfFile" && i != args.Length - 1)
                filePath = args[i + 1];

        if (filePath == null)
        {
            Debug.LogError("-perfFile argument not provided");
            return;
        }

        try
        {
            ShaderViewerWindow viewer = CreateWindow<ShaderViewerWindow>();
            var content = File.ReadAllText(filePath);
            viewer.UpdateViewWithTextFile(content);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    public void CreateGUI()
    {
        titleContent = new GUIContent("Shader Analysis Viewer");
        var toolbarAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/com.unity.shaderanalysis/Editor/Viewer/Toolbar.uxml");
        VisualElement toolbar = toolbarAsset.Instantiate();
        rootVisualElement.Add(toolbar);
        var shaderField = toolbar.Q<ObjectField>("ShaderField");
        var analyzeButton = toolbar.Q<Button>("Analyze");
        vgprAnalysisTab = toolbar.Q<Tab>("VGPRCodeAnalysisTab");
        vgprAnalysisTab.selected += tab =>
        {
            currentlySelectedTab = tab;
            UpdateSelectedTabView();
        };
        currentlySelectedTab = vgprAnalysisTab;
        vgprAssemblyAnalysisTab = toolbar.Q<Tab>("VGPRAssemblyAnalysisTab");
        vgprAssemblyAnalysisTab.selected += tab =>
        {
            currentlySelectedTab = tab;
            UpdateSelectedTabView();
        };
        cbufferReadOrderTab = toolbar.Q<Tab>("CBufferReadOrderTab");
        cbufferReadOrderTab.selected += tab =>
        {
            currentlySelectedTab = tab;
            UpdateSelectedTabView();
        };
        sgprCodeAnalysisTab = toolbar.Q<Tab>("SGPRCodeAnalysisTab");
        sgprCodeAnalysisTab.selected += tab =>
        {
            currentlySelectedTab = tab;
            UpdateSelectedTabView();
        };
        sgprAssemblyAnalysisTab = toolbar.Q<Tab>("SGPRAssemblyAnalysisTab");
        sgprAssemblyAnalysisTab.selected += tab =>
        {
            currentlySelectedTab = tab;
            UpdateSelectedTabView();
        };

        var hotfix = toolbar.Q<VisualElement>("unity-tab-view__header-container");
        computeShaderFilterSettings = toolbar.Q<VisualElement>("ComputeShaderFilterSettings");
        shaderFilterSettings = toolbar.Q<VisualElement>("ShaderFilterSettings");
        materialFilterSettings = toolbar.Q<VisualElement>("MaterialFilterSettings");
        shaderField.RegisterValueChangedCallback((v) =>
        {
            if (v.newValue is not Material and not ComputeShader and not Shader and not null)
            {
                Debug.LogError("Only Materials, Shaders and ComputeShaders are allowed in this field.");
                return;
            }

            computeShaderFilterSettings.style.display = DisplayStyle.None;
            shaderFilterSettings.style.display = DisplayStyle.None;
            materialFilterSettings.style.display = DisplayStyle.None;
            currentUnityObject = v.newValue;

            if (v.newValue == null)
                return;

            switch (v.newValue)
            {
                case ComputeShader cs:
                    computeShaderFilterSettings.style.display = DisplayStyle.Flex;
                    computeShaderFilterSettings.Clear();
                    // computeShaderKeywordSearchField.menu.ClearItems();
                    var data = ShaderAnalyzer.ListComputeShaderKernels(cs);
                    if (data == null) // compute shader can have compilation errors
                        return;
                    if (!data.kernelNames.Contains(selectedComputeShaderKernelName))
                        selectedComputeShaderKernelName = data.kernelNames.First();
                    var tb = new ToolbarMenu(){ text = selectedComputeShaderKernelName ?? "Kernel Name"};
                    foreach (var kernel in data.kernelNames)
                        tb.menu.AppendAction(kernel, a =>
                        {
                            selectedComputeShaderKernelName = a.name;
                            tb.text = a.name;
                            UpdateViewWithCurrentKernel(lastBuildReport);
                        }, a => selectedComputeShaderKernelName == a.name ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
                    computeShaderFilterSettings.Add(tb);
                    var sf = new ToolbarPopupSearchField() { placeholderText = "Active Keywords" };
                    foreach (var keyword in data.multiCompiles)
                    {
                        sf.menu.AppendAction(keyword, a =>
                        {
                            if (selectedShaderKeywords.Contains(a.name))
                                selectedShaderKeywords.Remove(a.name);
                            else
                                selectedShaderKeywords.Add(a.name);
                        }, a =>
                            selectedShaderKeywords.Contains(a.name) ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
                    }

                    computeShaderFilterSettings.Add(sf);
                    break;
            }
        });
        shaderField.value = currentUnityObject;
        hotfix.style.minHeight = 22; // fix tab overlap by tab content
        analyzeButton.clicked += AnalyzeContent;
        var codeSearch = toolbar.Q<ToolbarSearchField>("CodeSearch");
        codeSearch.RegisterValueChangedCallback(v =>
        {
            HighlightCodeLines(v.newValue);
        });

        targetPlatform = toolbar.Q<DropdownField>("PlatformField");
        targetPlatform.index = lastPlatformIndex;
        targetPlatform.RegisterValueChangedCallback(v => lastPlatformIndex = targetPlatform.index);

        rootVisualElement.schedule.Execute(Update).Every(33); // 30hz refresh rate

        CreateViews();

        if (lastPerfUnit != null)
            UpdateViewWithCurrentProgram(lastPerfUnit);
    }

    private void OnDestroy()
    {
        if (Application.isBatchMode)
            EditorApplication.Exit(0);
    }

    void AnalyzeContent()
    {
        CompileShader();
    }

    const float compilationTimeout = 60;

    private AsyncBuildReportJob currentCompilationJob;
    private float timeAtJobStart;

    void CreateViews()
    {
        codeVGPRView = new ScrollView();
        assemblyVGPRView = new ScrollView();
        assemblySGPRView = new ScrollView();
        codeSGPRView = new ScrollView();
        cbufferOrderView = new ScrollView();

        if (currentAnalyzedShader != null)
            UpdateViews();
        else
        {
            codeVGPRView.Add(new Label("Null Compiled Shader"));
            cbufferOrderView.Add(new Label("Null Compiled Shader"));
        }

        rootVisualElement.Add(new IMGUIContainer(() =>
        {
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
                ClearSelectedRegisterNode();
        }));

        vgprAnalysisTab.Add(codeVGPRView);
        cbufferReadOrderTab.Add(cbufferOrderView);
        vgprAssemblyAnalysisTab.Add(assemblyVGPRView);
        sgprCodeAnalysisTab.Add(codeSGPRView);
        sgprAssemblyAnalysisTab.Add(assemblySGPRView);
    }

    private int lastCBufferReadAddress;
    private List<VisualElement> codeLineViews = new();
    void UpdateViews()
    {
        Random.InitState(42);

        codeVGPRView.Clear();
        assemblyVGPRView.Clear();
        cbufferOrderView.Clear();
        codeLineViews.Clear();
        codeSGPRView.Clear();
        assemblySGPRView.Clear();
        vgprGraph.Clear();
        allVGPRBars.Clear();
        lastCBufferReadAddress = -1;
        UpdateSelectedTabView();
    }

    void UpdateSelectedTabView()
    {
        if (currentlySelectedTab == vgprAnalysisTab && codeVGPRView.childCount == 0)
        {
            int assemblyLineIndex = 0;
            for (int i = 0; i < currentAnalyzedShader.lines.Count; i++)
            {
                var line = currentAnalyzedShader.lines[i];
                codeVGPRView.Add(CreateRegisterViewLine(RegisterType.Vector, line, i, assemblyLineIndex));
                assemblyLineIndex += line.assemblyLines.Count;
            }

            for (int i = 0; i < currentAnalyzedShader.lines.Count; i++)
            {
                var line = currentAnalyzedShader.lines[i];
                CreatRegisterGraph(RegisterType.Vector, codeVGPRView, line, i);
                CreateJumpLine(RegisterType.Vector, codeVGPRView, line, i);
            }
        }

        if (currentlySelectedTab == vgprAssemblyAnalysisTab && assemblyVGPRView.childCount == 0)
        {
            int assemblyLineCounter = 0;
            foreach (var line in currentAnalyzedShader.lines)
                CreateRegisterViewAssemblyLines(RegisterType.Vector, assemblyVGPRView, line, ref assemblyLineCounter);

            assemblyLineCounter = 0;
            for (int i = 0; i < currentAnalyzedShader.lines.Count; i++)
            {
                var line = currentAnalyzedShader.lines[i];
                foreach (var assemblyLine in line.assemblyLines)
                {
                    CreatRegisterGraph(RegisterType.Vector, assemblyVGPRView, assemblyLine, assemblyLineCounter, i);
                    // CreateJumpLine(assemblyVGPRView, assemblyLine, i);
                    assemblyLineCounter++;
                }
            }
        }

        if (currentlySelectedTab == sgprCodeAnalysisTab && codeSGPRView.childCount == 0)
        {
            int assemblyLineIndex = 0;
            for (int i = 0; i < currentAnalyzedShader.lines.Count; i++)
            {
                var line = currentAnalyzedShader.lines[i];
                codeSGPRView.Add(CreateRegisterViewLine(RegisterType.Scalar, line, i, assemblyLineIndex));
                assemblyLineIndex += line.assemblyLines.Count;
            }

            for (int i = 0; i < currentAnalyzedShader.lines.Count; i++)
            {
                var line = currentAnalyzedShader.lines[i];
                CreatRegisterGraph(RegisterType.Scalar, codeSGPRView, line, i);
                CreateJumpLine(RegisterType.Scalar, codeSGPRView, line, i);
            }
        }

        if (currentlySelectedTab == sgprAssemblyAnalysisTab && assemblySGPRView.childCount == 0)
        {
            int assemblyLineCounter = 0;
            foreach (var line in currentAnalyzedShader.lines)
                CreateRegisterViewAssemblyLines(RegisterType.Scalar, assemblySGPRView, line, ref assemblyLineCounter);

            assemblyLineCounter = 0;
            for (int i = 0; i < currentAnalyzedShader.lines.Count; i++)
            {
                var line = currentAnalyzedShader.lines[i];
                foreach (var assemblyLine in line.assemblyLines)
                {
                    CreatRegisterGraph(RegisterType.Scalar, assemblySGPRView, assemblyLine, assemblyLineCounter, i);
                    // CreateJumpLine(assemblyVGPRView, assemblyLine, i);
                    assemblyLineCounter++;
                }
            }
        }

        if (currentlySelectedTab == cbufferReadOrderTab && cbufferOrderView.childCount == 0)
        {
            int assemblyLineIndex = 0;
            for (int i = 0; i < currentAnalyzedShader.lines.Count; i++)
            {
                var line = currentAnalyzedShader.lines[i];
                cbufferOrderView.Add(CreateCBufferReadViewLine(line, i, assemblyLineIndex));
                assemblyLineIndex += line.assemblyLines.Count;
            }
        }
    }

    void HighlightCodeLines(string search)
    {
        if (currentAnalyzedShader == null)
            return;

        foreach (var codeView in codeLineViews)
            codeView.Q<Label>().style.backgroundColor = Color.clear;

        if (String.IsNullOrWhiteSpace(search))
            return;

        int lineIndex = 0;
        foreach (var code in currentAnalyzedShader.lines)
        {
            if (code.code.Contains(search, StringComparison.OrdinalIgnoreCase))
                codeLineViews[lineIndex].Q<Label>().style.backgroundColor = new Color(0.27f, 0.41f, 0.52f);
            lineIndex++;
        }
    }

    void CompileShader()
    {
        if (currentUnityObject == null)
            return;

        var profile = ShaderProfile.ComputeProgram;
        if (currentUnityObject is Material)
            profile = ShaderProfile.PixelProgram; //  TODO: vertex choice
        var filter = new ShaderProgramFilter { includedPassNames = default };
        var set = new KeywordSet(selectedShaderKeywords);
        filter.includedKeywords.Add(set);

        var target = targetPlatform.index == 0 ? BuildTarget.PS4 : BuildTarget.PS5;
        currentCompilationJob = (AsyncBuildReportJob)EditorShaderTools.GenerateBuildReportAsyncGeneric(ShaderAnalysisReport.New(currentUnityObject, target, profile, filter));
        currentCompilationJob.throwOnError = true;
        timeAtJobStart = Time.realtimeSinceStartup;
    }

    void Update()
    {
        if (currentCompilationJob != null)
        {
            if (!currentCompilationJob.IsComplete())
            {
                if (Time.realtimeSinceStartup - timeAtJobStart > compilationTimeout)
                {
                    currentCompilationJob.Cancel();
                    throw new Exception($"Timeout {compilationTimeout} s");
                }

                try
                {
                    if (currentCompilationJob.Tick())
                        currentCompilationJob.SetProgress(1, "Completed");
                    EditorUpdateManager.Tick();
                }
                catch (Exception e)
                {
                    currentCompilationJob.Cancel();
                    Debug.LogException(e);
                }
            }
            else
            {
                var finishedJob = currentCompilationJob;
                currentCompilationJob = null;
                lastBuildReport = finishedJob.builtReport;

                UpdateViewWithCurrentKernel(finishedJob.builtReport);
            }
        }
    }

    void UpdateViewWithCurrentKernel(ShaderBuildReport report)
    {
        var kernelUnit = report.performanceUnits.FirstOrDefault(u => u.program.name == selectedComputeShaderKernelName);

        if (kernelUnit == null)
        {
            Debug.LogError("Kernel name " + selectedComputeShaderKernelName + " not found!");
            return;
        }

        lastPerfUnit = kernelUnit;
        UpdateViewWithCurrentProgram(kernelUnit);
    }

    void UpdateViewWithCurrentProgram(ShaderBuildReport.PerformanceUnit kernelUnit)
    {
        try
        {
            string fileContent;
            int maxVGPR = -1;
            int maxSGPR = -1;

            if (String.IsNullOrEmpty(kernelUnit.extraPerfDataFile))
                fileContent = kernelUnit.rawReport;
            else
            {
                fileContent = File.ReadAllText(kernelUnit.extraPerfDataFile);
                maxVGPR = Mathf.Max(kernelUnit.parsedReport.VGPRUsedCount, kernelUnit.parsedReport.VGPRCount);
                maxSGPR = Mathf.Max(kernelUnit.parsedReport.SGPRUsedCount, kernelUnit.parsedReport.SGPRCount);
            }

            UpdateViewWithTextFile(fileContent, maxVGPR, maxSGPR);
            UpdateViews();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    void UpdateViewWithTextFile(string fileContent, int maxVGPRUsed = -1, int maxSGPRUsed = -1)
    {
        try
        {
            currentAnalyzedShader = ShaderAnalyzer.ParseCompiledShader(fileContent);
            currentAnalyzedShader.maxVGPRUsed = maxVGPRUsed == -1 ? currentAnalyzedShader.maxVGPRAlive : maxVGPRUsed;
            currentAnalyzedShader.maxSGPRUsed = maxSGPRUsed == -1 ? currentAnalyzedShader.maxSGPRAlive : maxSGPRUsed;
            UpdateViews();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private Font codeFont = null;
    Label GetCodeLineLabel(CodeLine line, int lineIndex, int assemblyLineStart)
    {
        var codeLine = new Label() { text = $"{lineIndex:0000} {line.highlightedCode}" };
        string tooltip = "";
        int i = 0;
        foreach (var assembly in line.assemblyLines)
            tooltip += $"{assemblyLineStart + i++:0000} {assembly.assembly}\n";
        codeLine.tooltip = tooltip + "\n" + Path.GetFileName(line.filePath) + ":" + line.line;
        if (line.jumpData != null)
            codeLine.tooltip += "\n\nJumps to line " + line.jumpData.jumpLineIndex;
        if (codeFont == null)
            codeFont = AssetDatabase.LoadAssetAtPath<Font>("Packages/com.unity.shaderanalysis/Editor/Viewer/Hack-Regular.ttf");
        codeLine.style.unityFont = new StyleFont(codeFont);
        codeLine.style.unityFontDefinition = FontDefinition.FromFont(codeFont);
        codeLine.style.width = k_CodeLabelWidth;
        codeLine.style.overflow = Overflow.Hidden;
        codeLine.name = "Code";

        return codeLine;
    }

    Label GetCodeLineLabel(AssemblyLine assemblyLine, CodeLine line, int lineIndex)
    {
        var codeLine = new Label() { text = $"{lineIndex:0000} {assemblyLine.assembly}" };
        codeLine.tooltip = line.highlightedCode + "\n" + Path.GetFileName(line.filePath) + ":" + line.line;
        if (line.jumpData != null)
            codeLine.tooltip += "\n\nJumps to line " + line.jumpData.jumpLineIndex;
        if (codeFont == null)
            codeFont = AssetDatabase.LoadAssetAtPath<Font>("Packages/com.unity.shaderanalysis/Editor/Viewer/Hack-Regular.ttf");
        codeLine.style.unityFont = new StyleFont(codeFont);
        codeLine.style.unityFontDefinition = FontDefinition.FromFont(codeFont);
        codeLine.style.width = k_CodeLabelWidth;
        codeLine.style.overflow = Overflow.Hidden;

        return codeLine;
    }

    // VGPR occupancy directly taken from the graph on console (not AMD desktop GPU)
    private int[] vgprOccupancy = { 24, 28, 36, 36, 40, 48, 64, 84, 128, 256 };
    private Dictionary<int, int> vgprOccupancy2 = new()
    {
        {20, 24},
        {18, 28},
        {16, 32},
        {14, 36},
        {12, 40},
        {11, 44},
        {10, 48},
        {9, 56},
        {8, 64},
        {7, 72},
        {6, 84},
        {5, 100},
        {4, 128},
        {3, 168},
        {2, 256}, // Max VGPR alloc
    };
    int GetVGPROccupancy(int vgpr)
    {
        int max = GetMaxOccupancy(currentAnalyzedShader);
        if (currentAnalyzedShader.target == BuildTarget.PS4)
        {
            for (int i = 0; i < vgprOccupancy.Length; i++)
            {
                if (vgpr <= vgprOccupancy[i])
                    return max - i;
            }
        }
        else
        {
            foreach (var kp in vgprOccupancy2)
            {
                if (vgpr <= kp.Value)
                    return kp.Key;
            }
        }
        return max;
    }

    // numbers may be approximate :shrug:
    private int[] sgprOccupancy = { 48, 52, 52, 72, 80, 96, 120, 168, 248 };

    int GetSGPROccupancy(int sgpr)
    {
        int max = GetMaxOccupancy(currentAnalyzedShader);

        if (currentAnalyzedShader.target == BuildTarget.PS4)
        {
            for (int i = 0; i < sgprOccupancy.Length; i++)
            {
                if (sgpr <= sgprOccupancy[i])
                    return max - i;
            }
        }
        else
        {
            for (int i = 0; i < sgprOccupancy.Length; i++)
            {
                if (sgpr <= sgprOccupancy[i])
                    return max - i * 2; // TODO: This is wrong, find correct numbers
            }
        }

        return max;
    }

    int GetMaxOccupancy(AnalyzedShader analyzedShader)
    {
        if (analyzedShader.target == BuildTarget.PS4)
            return 10;
        else
            return 20;
    }

    VisualElement CreateRegisterViewLine(RegisterType registerType, CodeLine line, int lineIndex, int assemblyLineIndex)
    {
        VisualElement lineView = new();
        lineView.style.flexDirection = FlexDirection.Row;

        var codeLabel = GetCodeLineLabel(line, lineIndex, assemblyLineIndex);
        lineView.Add(codeLabel);

        int offsetLeft = (registerType == RegisterType.Vector ? currentAnalyzedShader.maxVGPRUsed : currentAnalyzedShader.maxSGPRUsed) * 12 + k_JumpTableWidth;
        int occupancy = registerType == RegisterType.Vector ? GetVGPROccupancy(line.vgprPressure) : GetSGPROccupancy(line.sgprPressure);
        int registerPressure = registerType == RegisterType.Vector ? line.vgprPressure : line.sgprPressure;
        int maxRegisterAlive = registerType == RegisterType.Vector ? currentAnalyzedShader.maxVGPRAlive : currentAnalyzedShader.maxSGPRAlive;
        var maxOccupancy = registerType == RegisterType.Vector ? GetVGPROccupancy(currentAnalyzedShader.maxVGPRAlive) : GetSGPROccupancy(currentAnalyzedShader.maxSGPRAlive);
        int maxAnalyzedRegisterOnLine = registerType == RegisterType.Vector ? currentAnalyzedShader.maxAnalyzedAliveVGPROnLine : currentAnalyzedShader.maxAnalyzedAliveSGPROnLine;

        Color.RGBToHSV(new Color(0.28f, 0.83f, 0.07f), out var RH, out var RG, out var RB);
        Color.RGBToHSV(new Color(0.84f, 0.14f, 0.11f), out var GH, out var GG, out var GB);

        // Show occupancy per line and wavefront number.
        var square = new VisualElement();
        square.style.width = square.style.minWidth = 5 * registerPressure;
        square.style.height = 15;
        square.style.marginLeft = offsetLeft;
        float f = 1 - (occupancy / (float)GetMaxOccupancy(currentAnalyzedShader));
        var gradient = Color.HSVToRGB(Mathf.Lerp(RH, GH, f), RG, RB) ;
        square.style.backgroundColor = registerPressure == maxRegisterAlive ? new Color(1f, 0.28f, 1f) : occupancy == maxOccupancy ? new Color(0.98f, 0.2f, 0.56f) : gradient;
        square.tooltip = registerPressure + " " + (registerType == RegisterType.Vector ? "VGPR" : "SGPR") +" (" + occupancy + "/" + GetMaxOccupancy(currentAnalyzedShader) + " waves)";
        lineView.Add(square);

        // Show the graph of alive registers (analyzed from the assembly read / write data)
        var square2 = new VisualElement();
        int aliveRegisterCount = currentAnalyzedShader.aliveRegistersPerLine[lineIndex].Count(r => r.Key.type == registerType);
        square2.style.width = square2.style.minWidth = 5 * aliveRegisterCount;
        square2.style.height = 15;
        square2.style.marginLeft = 5 * maxRegisterAlive - square.style.width.value.value + 5;
        float f2 = aliveRegisterCount / (float)maxAnalyzedRegisterOnLine;
        square2.style.backgroundColor = Color.HSVToRGB(Mathf.Lerp(RH, GH, f2), RG, RB);
        square2.tooltip = aliveRegisterCount.ToString();
        lineView.Add(square2);

        lineView.RegisterCallback<MouseOverEvent>(t =>
        {
            lineView.style.backgroundColor = new Color(0.33f, 0.33f, 0.33f);
        });

        lineView.RegisterCallback<MouseOutEvent>(t =>
        {
            lineView.style.backgroundColor = Color.clear;
        });

        lineView.RegisterCallback<MouseDownEvent>(m =>
        {
            if (m.clickCount == 2)
            {
                int index = line.filePath.IndexOf("Packages/", StringComparison.Ordinal);
                if (index == -1)
                {
                    Application.OpenURL(line.filePath);
                }
                else
                {
                    var scriptAsset = AssetDatabase.LoadAssetAtPath<Object>(line.filePath.Substring(index));
                    AssetDatabase.OpenAsset(scriptAsset, line.line);
                }
            }
        });

        codeLineViews.Add(lineView);
        return lineView;
    }

    void CreateRegisterViewAssemblyLines(RegisterType registerType, VisualElement targetTab, CodeLine line, ref int assemblyLineIndex)
    {
        int maxOccupancy = (registerType == RegisterType.Vector) ? GetVGPROccupancy(currentAnalyzedShader.maxVGPRAlive) : GetSGPROccupancy(currentAnalyzedShader.maxSGPRAlive);
        foreach (var assemblyLine in line.assemblyLines)
        {
            VisualElement lineView = new();
            lineView.style.flexDirection = FlexDirection.Row;

            var codeLabel = GetCodeLineLabel(assemblyLine, line, assemblyLineIndex);
            lineView.Add(codeLabel);

            int offsetLeft = (registerType == RegisterType.Vector ? currentAnalyzedShader.maxVGPRUsed : currentAnalyzedShader.maxSGPRUsed) * 12 + k_JumpTableWidth;
            int occupancy = GetVGPROccupancy(assemblyLine.vgprPressure);

            Color.RGBToHSV(new Color(0.28f, 0.83f, 0.07f), out var RH, out var RG, out var RB);
            Color.RGBToHSV(new Color(0.84f, 0.14f, 0.11f), out var GH, out var GG, out var GB);

            // Show occupancy per line and wavefront number.
            var square = new VisualElement();
            square.style.width = square.style.minWidth = 5 * assemblyLine.vgprPressure;
            square.style.height = 15;
            square.style.marginLeft = offsetLeft;
            float f = 1 - (occupancy / (float)GetMaxOccupancy(currentAnalyzedShader));
            var gradient = Color.HSVToRGB(Mathf.Lerp(RH, GH, f), RG, RB) ;
            square.style.backgroundColor = assemblyLine.vgprPressure == currentAnalyzedShader.maxVGPRAlive ? new Color(1f, 0.28f, 1f) : occupancy == maxOccupancy ? new Color(0.98f, 0.2f, 0.56f) : gradient;
            square.tooltip = assemblyLine.vgprPressure + " " + (registerType == RegisterType.Vector ? "VGPR" : "SGPR") + " (" + occupancy + "/" + GetMaxOccupancy(currentAnalyzedShader) + " waves)";
            lineView.Add(square);

            // // Show the graph of alive registers (analyzed from the assembly read / write data)
            // var square2 = new VisualElement();
            // int aliveRegisterCount = currentAnalyzedShader.aliveRegistersPerLine[lineIndex].Count;
            // square2.style.width = square2.style.minWidth = 5 * aliveRegisterCount;
            // square2.style.height = 15;
            // square2.style.marginLeft = 5 * currentAnalyzedShader.maxVGPRAlive - square.style.width.value.value + 5;
            // float f2 = aliveRegisterCount / (float)currentAnalyzedShader.maxAnalyzedAliveVGPROnLine;
            // square2.style.backgroundColor = Color.HSVToRGB(Mathf.Lerp(RH, GH, f2), RG, RB);
            // square2.tooltip = aliveRegisterCount.ToString();
            // lineView.Add(square2);

            lineView.RegisterCallback<MouseOverEvent>(t =>
            {
                lineView.style.backgroundColor = new Color(0.33f, 0.33f, 0.33f);
            });

            lineView.RegisterCallback<MouseOutEvent>(t =>
            {
                lineView.style.backgroundColor = Color.clear;
            });

            lineView.RegisterCallback<MouseDownEvent>(m =>
            {
                if (m.clickCount == 2)
                {
                    int index = line.filePath.IndexOf("Packages/", StringComparison.Ordinal);
                    if (index == -1)
                    {
                        Application.OpenURL(line.filePath);
                    }
                    else
                    {
                        var scriptAsset = AssetDatabase.LoadAssetAtPath<Object>(line.filePath.Substring(index));
                        AssetDatabase.OpenAsset(scriptAsset, line.line);
                    }
                }
            });

            codeLineViews.Add(lineView);

            targetTab.Add(lineView);
            assemblyLineIndex++;
        }
    }

    void CreatRegisterGraph(RegisterType registerType, VisualElement targetTab, CodeLine line, int lineIndex)
    {
        var aliveRegisters = currentAnalyzedShader.aliveRegistersPerLine[lineIndex];
        for (int i = 0; i < (registerType == RegisterType.Vector ? currentAnalyzedShader.maxVGPRUsed : currentAnalyzedShader.maxSGPRUsed); i++)
        {
            bool registerRead = line.registerReads.Any(r => r.type == registerType && r.registerIndex == i);
            bool registerWrite = line.registerWrites.Any(r => r.type == registerType && r.registerIndex == i);
            bool alive = aliveRegisters.TryGetValue(new Register { type = registerType, registerIndex = i }, out var metaData);
            Color aliveColor = Color.white;
            if (alive)
                aliveColor = Color.Lerp(new Color(0.19f, 0.6f, 1f), new Color(1f, 0.18f, 0.44f), metaData.aliveCodeLineCount / (float)currentAnalyzedShader.maxRegisterAliveLineCount);

            var nodes = vgprGraph.Where(n => n.registerIndex == i && lineIndex >= n.aliveStartLineIndex && lineIndex <= n.aliveEndLineIndex).ToList();
            if (alive)
            {
                // Draw VGPR lifetime
                if (metaData.start)
                {
                    var backgroundSquare = new VisualElement();
                    backgroundSquare.style.marginRight = 1;
                    backgroundSquare.style.marginLeft = 1;
                    backgroundSquare.style.color = backgroundSquare.style.backgroundColor = aliveColor;
                    backgroundSquare.style.position = Position.Absolute;
                    backgroundSquare.style.top = lineIndex * 15;
                    backgroundSquare.style.left = k_CodeLabelWidth + i * 12;
                    backgroundSquare.tooltip = $"v{i}\n{metaData.sourceWriteLine.highlightedCode}";
                    backgroundSquare.style.minWidth = backgroundSquare.style.width = 10;
                    backgroundSquare.style.minHeight =
                        backgroundSquare.style.height = 15 * (metaData.aliveCodeLineCount + 1);
                    targetTab.Add(backgroundSquare);
                    allVGPRBars.Add(backgroundSquare);

                    var n2 = new RegisterUsageNode
                    {
                        registerIndex = i,
                        aliveStartLineIndex = lineIndex,
                        aliveEndLineIndex = lineIndex + metaData.aliveCodeLineCount,
                    };
                    nodes.Add(n2);
                    vgprGraph.Add(n2);

                    foreach (var node in nodes)
                    {
                        node.line = line;
                        if (node.registerIndex == i && node.aliveStartLineIndex == lineIndex && node.aliveEndLineIndex == lineIndex + metaData.aliveCodeLineCount)
                            node.backgroundRegisterLifetime = backgroundSquare;
                    }

                    int registerIndex = i;
                    backgroundSquare.RegisterCallback<MouseDownEvent>(m =>
                    {
                        if (m.clickCount == 1)
                        {
                            var toHighlight = vgprGraph.Where(n => n.registerIndex == registerIndex && lineIndex >= n.aliveStartLineIndex && lineIndex <= n.aliveEndLineIndex).ToList();
                            HighlightRelatedVGPRUsage(toHighlight);
                        }
                    });
                }
            }

            // Draw read and write data
            VisualElement GetRWBar(Color color, float topOffset = 0)
            {
                var bar = new VisualElement();
                bar.style.width = 10;
                bar.style.height = 5;
                bar.style.marginRight = 1;
                bar.style.marginLeft = 1; //offsetLeft - bar.style.width.value.value - 1;
                bar.style.color = bar.style.backgroundColor = color;
                bar.style.marginTop = topOffset;
                bar.style.position = Position.Absolute;
                bar.style.top = lineIndex * 15;
                bar.style.left = k_CodeLabelWidth + i * 12;

                allVGPRBars.Add(bar);
                return bar;
            }

            if (registerWrite)
            {
                var bar = GetRWBar(new Color(0.78f, 0.18f, 0.11f));
                foreach (var node in nodes)
                    node.writeBars.Add(bar);
                targetTab.Add(bar);
            }

            if (registerRead)
            {
                var bar = GetRWBar(new Color(0.14f, 0.22f, 0.76f), 10f);
                targetTab.Add(bar);
                foreach (var node in nodes)
                {
                    node.readBars.Add(bar);

                    // Find all writes on the same code line and register these as reads from this register
                    foreach (var register in aliveRegisters)
                    {
                        if (register.Key.type != registerType)
                            continue;

                        bool writeTo = line.registerWrites.Any(r =>
                            r.type == registerType && r.registerIndex == register.Key.registerIndex);
                        if (writeTo)
                        {
                            // Add the reference node if it exists, otherwise we create a new empty one. It'll be initialized when drawing the bar background
                            var linkedNodes = vgprGraph.Where(n =>
                                n.registerIndex == register.Key.registerIndex &&
                                lineIndex >= n.aliveStartLineIndex &&
                                lineIndex <= n.aliveEndLineIndex).ToList();
                            if (linkedNodes.Count == 0)
                            {
                                var f = new RegisterUsageNode
                                {
                                    registerIndex = register.Key.registerIndex,
                                    aliveStartLineIndex = register.Value.startCodeLine,
                                    aliveEndLineIndex = register.Value.startCodeLine + register.Value.aliveCodeLineCount
                                };
                                vgprGraph.Add(f);
                                node.reads.Add(f);
                            }
                            else
                            {
                                foreach (var f in linkedNodes)
                                {
                                    vgprGraph.Add(f);
                                    node.reads.Add(f);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    void CreatRegisterGraph(RegisterType registerType, VisualElement targetTab, AssemblyLine line, int assemblyLineIndex, int codeLineIndex)
    {
        var aliveRegisters = currentAnalyzedShader.aliveRegistersPerAssemblyLine[assemblyLineIndex];
        for (int i = 0; i < (registerType == RegisterType.Vector ? currentAnalyzedShader.maxVGPRUsed : currentAnalyzedShader.maxSGPRUsed); i++)
        {
            bool registerRead = line.registerReads.Any(r => r.type == registerType && r.registerIndex == i);
            bool registerWrite = line.registerWrites.Any(r => r.type == registerType && r.registerIndex == i);
            bool alive = aliveRegisters.TryGetValue(new Register { type = registerType, registerIndex = i }, out var metaData);
            Color aliveColor = Color.white;
            if (alive)
                aliveColor = Color.Lerp(new Color(0.19f, 0.6f, 1f), new Color(1f, 0.18f, 0.44f), metaData.aliveAssemblyLineCount / (float)currentAnalyzedShader.maxRegisterAliveAssemblyLineCount);

            if (alive)
            {
                // Draw VGPR lifetime
                if (metaData.start)
                {
                    var backgroundSquare = new VisualElement();
                    backgroundSquare.style.marginRight = 1;
                    backgroundSquare.style.marginLeft = 1;
                    backgroundSquare.style.color = backgroundSquare.style.backgroundColor = aliveColor;
                    backgroundSquare.style.position = Position.Absolute;
                    backgroundSquare.style.top = assemblyLineIndex * 15;
                    backgroundSquare.style.left = k_CodeLabelWidth + i * 12;
                    backgroundSquare.tooltip = $"v{i}\n{metaData.sourceWriteLine.highlightedCode}";
                    backgroundSquare.style.minWidth = backgroundSquare.style.width = 10;
                    backgroundSquare.style.minHeight =
                        backgroundSquare.style.height = 15 * (metaData.aliveAssemblyLineCount + 1);
                    targetTab.Add(backgroundSquare);
                    allVGPRBars.Add(backgroundSquare);
                }
            }

            // Draw read and write data
            VisualElement GetRWBar(Color color, float topOffset = 0)
            {
                var bar = new VisualElement();
                bar.style.width = 10;
                bar.style.height = 5;
                bar.style.marginRight = 1;
                bar.style.marginLeft = 1; //offsetLeft - bar.style.width.value.value - 1;
                bar.style.color = bar.style.backgroundColor = color;
                bar.style.marginTop = topOffset;
                bar.style.position = Position.Absolute;
                bar.style.top = assemblyLineIndex * 15;
                bar.style.left = k_CodeLabelWidth + i * 12;

                allVGPRBars.Add(bar);
                return bar;
            }

            if (registerWrite)
            {
                var bar = GetRWBar(new Color(0.78f, 0.18f, 0.11f));
                targetTab.Add(bar);
            }

            if (registerRead)
            {
                var bar = GetRWBar(new Color(0.14f, 0.22f, 0.76f), 10f);
                targetTab.Add(bar);
            }
        }
    }

    void HighlightRelatedVGPRUsage(List<RegisterUsageNode> nodes)
    {
        foreach (var bar in allVGPRBars)
            bar.style.backgroundColor = bar.style.color.value / 2; // We use .color to store the old background color instead of using a map

        // First grey-out all the nodes visually
        // Then select iterate over node dependencies and children and remove the grey-out
        var maxLine = nodes.First().aliveStartLineIndex;
        Stack<RegisterUsageNode> childNodes = new();
        foreach (var node in nodes)
            childNodes.Push(node);

        HashSet<RegisterUsageNode> alreadyVisitedNodes = new();
        while (childNodes.Count != 0)
        {
            var n = childNodes.Pop();

            if (n.backgroundRegisterLifetime == null)
            {
                Debug.Log("Node " + n.registerIndex + ", " + n.aliveStartLineIndex + ", " + n.aliveEndLineIndex + " not found!");
                continue;
            }

            // if (n.aliveStartLineIndex < maxLine)
            //     continue;

            n.backgroundRegisterLifetime.style.backgroundColor = n.backgroundRegisterLifetime.style.color;
            foreach (var readBar in n.readBars)
                readBar.style.backgroundColor = readBar.style.color;
            foreach (var writeBar in n.writeBars)
                writeBar.style.backgroundColor = writeBar.style.color;

            if (alreadyVisitedNodes.Add(n))
            {
                foreach (var writeToRegister in n.reads)
                    childNodes.Push(writeToRegister);
            }
        }
    }

    void ClearSelectedRegisterNode()
    {
        foreach (var bar in allVGPRBars)
            bar.style.backgroundColor = bar.style.color; // We use .color to store the old background color instead of using a map
    }

    void CreateJumpLine(RegisterType registerType, VisualElement targetTab, CodeLine line, int index)
    {
        if (line.jumpData == null)
            return;

        int diff = line.jumpData.jumpLineIndex - index;
        var jumpSquare = new VisualElement();
        jumpSquare.style.marginRight = 1;
        jumpSquare.style.marginLeft = 1;
        jumpSquare.style.backgroundColor = Random.ColorHSV(0f, 1f, 0.7f, 0.9f, 0.5f, 1);
        jumpSquare.style.position = Position.Absolute;
        jumpSquare.style.top = index * 15;
        int max = registerType == RegisterType.Vector ? currentAnalyzedShader.maxVGPRUsed : currentAnalyzedShader.maxSGPRUsed;
        jumpSquare.style.left = k_CodeLabelWidth + max * 12 + line.jumpData.indentLevel * 7;
        jumpSquare.style.minWidth = jumpSquare.style.width = 5;
        // TODO: negative height
        jumpSquare.style.minHeight = jumpSquare.style.height = 15 * diff;
        jumpSquare.tooltip = $"Jumps from line {line.jumpData.fromIndex} to {line.jumpData.jumpLineIndex}\n\n";
        jumpSquare.tooltip += $"From: {currentAnalyzedShader.lines[line.jumpData.fromIndex].highlightedCode}\n";
        jumpSquare.tooltip += $"To: {currentAnalyzedShader.lines[line.jumpData.jumpLineIndex].highlightedCode}";
        targetTab.Add(jumpSquare);

        int start = Mathf.Min(line.jumpData.fromIndex, line.jumpData.jumpLineIndex);
        int end = Mathf.Max(line.jumpData.fromIndex, line.jumpData.jumpLineIndex);
        jumpSquare.RegisterCallback<MouseOverEvent>(t =>
        {
            for (int i = start; i <= end; i++)
                codeLineViews[i].style.backgroundColor = new Color(0.33f, 0.33f, 0.33f);
        });

        jumpSquare.RegisterCallback<MouseOutEvent>(t =>
        {
            for (int i = start; i <= end; i++)
                codeLineViews[i].style.backgroundColor = Color.clear;
        });
    }

    VisualElement CreateCBufferReadViewLine(CodeLine line, int lineIndex, int assemblyLineStart)
    {
        VisualElement lineView = new();
        lineView.style.flexDirection = FlexDirection.Row;

        lineView.Add(GetCodeLineLabel(line, lineIndex, assemblyLineStart));

        foreach (var cbuffer in line.cbufferReads)
        {
            Label l;
            if (cbuffer.dynamicScalarOffset)
                l = new Label{ text = $"<color=#1365bd>Read {cbuffer.dwordReadCount} DWORD scalar offset.</color>"};
            else
            {
                string color = "098f1d";
                // TODO: take in account the base address of the load to detect loading from different structs
                if (lastCBufferReadAddress != -1 && lastCBufferReadAddress < cbuffer.offset)
                    color = "c93702";
                l = new Label { text = $"<color=#{color}>Read {cbuffer.dwordReadCount} DWORD at offset {cbuffer.offset}.</color>" };
                lastCBufferReadAddress = cbuffer.offset;
            }

            lineView.Add(l);
        }

        return lineView;
    }
}
