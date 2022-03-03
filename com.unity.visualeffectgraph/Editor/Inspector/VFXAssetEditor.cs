using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;
using UnityEditor.Callbacks;
using UnityEditor.VFX;
using UnityEditor.VFX.UI;

using UnityObject = UnityEngine.Object;


class VFXExternalShaderProcessor : AssetPostprocessor
{
    public const string k_ShaderDirectory = "Shaders";
    public const string k_ShaderExt = ".vfxshader";
    public static bool allowExternalization { get { return EditorPrefs.GetBool(VFXViewPreference.allowShaderExternalizationKey, false); } }

    void OnPreprocessAsset()
    {
        if (!allowExternalization)
            return;
        bool isVFX = assetPath.EndsWith(VisualEffectResource.Extension);
        if (isVFX)
        {
            string vfxName = Path.GetFileNameWithoutExtension(assetPath);
            string vfxDirectory = Path.GetDirectoryName(assetPath);

            string shaderDirectory = vfxDirectory + "/" + k_ShaderDirectory + "/" + vfxName;

            if (!Directory.Exists(shaderDirectory))
            {
                return;
            }
            VisualEffectAsset asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(assetPath);
            if (asset == null)
                return;

            bool oneFound = false;
            VisualEffectResource resource = asset.GetResource();
            if (resource == null)
                return;
            VFXShaderSourceDesc[] descs = resource.shaderSources;

            foreach (var shaderPath in Directory.GetFiles(shaderDirectory))
            {
                if (shaderPath.EndsWith(k_ShaderExt))
                {
                    System.IO.StreamReader file = new System.IO.StreamReader(shaderPath);

                    string shaderLine = file.ReadLine();
                    file.Close();
                    if (shaderLine == null || !shaderLine.StartsWith("//"))
                        continue;

                    string[] shaderParams = shaderLine.Split(',');

                    string shaderName = shaderParams[0].Substring(2);

                    int index;
                    if (!int.TryParse(shaderParams[1], out index))
                        continue;

                    if (index < 0 || index >= descs.Length)
                        continue;
                    if (descs[index].name != shaderName)
                        continue;

                    string shaderSource = File.ReadAllText(shaderPath);
                    //remove the first two lines that where added when externalized
                    shaderSource = shaderSource.Substring(shaderSource.IndexOf("\n", shaderSource.IndexOf("\n") + 1) + 1);

                    descs[index].source = shaderSource;
                    oneFound = true;
                }
            }
            if (oneFound)
            {
                resource.shaderSources = descs;
            }
        }
    }
}

[CustomEditor(typeof(VisualEffectAsset))]
[CanEditMultipleObjects]
class VisualEffectAssetEditor : Editor
{
#if UNITY_2021_1_OR_NEWER
    [OnOpenAsset(OnOpenAssetAttributeMode.Validate)]
    public static bool WillOpenInUnity(int instanceID)
    {
        var obj = EditorUtility.InstanceIDToObject(instanceID);
        if (obj is VFXGraph || obj is VFXModel || obj is VFXUI)
            return true;
        else if (obj is VisualEffectAsset)
            return true;
        else if (obj is VisualEffectSubgraph)
            return true;
        return false;
    }

#endif
    [OnOpenAsset(1)]
    public static bool OnOpenVFX(int instanceID, int line)
    {
        var obj = EditorUtility.InstanceIDToObject(instanceID);
        if (obj is VFXGraph || obj is VFXModel || obj is VFXUI)
        {
            // for visual effect graph editor ScriptableObject select them when double clicking on them.
            //Since .vfx importer is a copyasset, the default is to open it with an external editor.
            Selection.activeInstanceID = instanceID;
            return true;
        }
        else if (obj is VisualEffectAsset)
        {
            VFXViewWindow.GetWindow<VFXViewWindow>().LoadAsset(obj as VisualEffectAsset, null);
            return true;
        }
        else if (obj is VisualEffectSubgraph)
        {
            VisualEffectResource resource = VisualEffectResource.GetResourceAtPath(AssetDatabase.GetAssetPath(obj));

            VFXViewWindow.GetWindow<VFXViewWindow>().LoadResource(resource, null);
            return true;
        }
        else if (obj is Material || obj is ComputeShader)
        {
            string path = AssetDatabase.GetAssetPath(instanceID);

            if (path.EndsWith(VisualEffectResource.Extension))
            {
                var resource = VisualEffectResource.GetResourceAtPath(path);
                if (resource != null)
                {
                    int index = resource.GetShaderIndex(obj);
                    resource.ShowGeneratedShaderFile(index, line);
                    return true;
                }
            }
        }
        return false;
    }

    ReorderableList m_ReorderableList;
    List<IVFXSubRenderer> m_OutputContexts = new List<IVFXSubRenderer>();
    VFXGraph m_CurrentGraph;

    void OnReorder(ReorderableList list)
    {
        for (int i = 0; i < m_OutputContexts.Count(); ++i)
        {
            m_OutputContexts[i].vfxSystemSortPriority = i;
        }
    }

    private void DrawOutputContextItem(Rect rect, int index, bool isActive, bool isFocused)
    {
        var context = m_OutputContexts[index] as VFXContext;

        var systemName = context.GetGraph().systemNames.GetUniqueSystemName(context.GetData());
        var contextLetter = context.letter;
        var contextName = string.IsNullOrEmpty(context.label) ? context.libraryName : context.label;
        var fullName = string.Format("{0}{1}/{2}", systemName, contextLetter != '\0' ? "/" + contextLetter : string.Empty, contextName);

        EditorGUI.LabelField(rect, EditorGUIUtility.TempContent(fullName));
    }

    private void DrawHeader(Rect rect)
    {
        EditorGUI.LabelField(rect, EditorGUIUtility.TrTextContent("Output Render Order"));
    }

    static Mesh s_CubeWireFrame;
    void OnEnable()
    {
        m_OutputContexts.Clear();
        VisualEffectAsset target = this.target as VisualEffectAsset;
        var resource = target.GetResource();
        if (resource != null) //Can be null if VisualEffectAsset is in Asset Bundle
        {
            m_CurrentGraph = resource.GetOrCreateGraph();
            m_CurrentGraph.systemNames.Sync(m_CurrentGraph);
            m_OutputContexts.AddRange(m_CurrentGraph.children.OfType<IVFXSubRenderer>().OrderBy(t => t.vfxSystemSortPriority));
        }

        m_ReorderableList = new ReorderableList(m_OutputContexts, typeof(IVFXSubRenderer));
        m_ReorderableList.displayRemove = false;
        m_ReorderableList.displayAdd = false;
        m_ReorderableList.onReorderCallback = OnReorder;
        m_ReorderableList.drawHeaderCallback = DrawHeader;

        m_ReorderableList.drawElementCallback = DrawOutputContextItem;

        if (m_VisualEffectGO == null)
        {
            m_PreviewUtility = new PreviewRenderUtility();
            m_PreviewUtility.camera.fieldOfView = 60.0f;
            m_PreviewUtility.camera.allowHDR = true;
            m_PreviewUtility.camera.allowMSAA = false;
            m_PreviewUtility.camera.farClipPlane = 10000.0f;
            m_PreviewUtility.camera.clearFlags = CameraClearFlags.SolidColor;
            m_PreviewUtility.ambientColor = new Color(.1f, .1f, .1f, 1.0f);
            m_PreviewUtility.lights[0].intensity = 1.4f;
            m_PreviewUtility.lights[0].transform.rotation = Quaternion.Euler(40f, 40f, 0);
            m_PreviewUtility.lights[1].intensity = 1.4f;

            m_VisualEffectGO = new GameObject("VisualEffect (Preview)");

            m_VisualEffectGO.hideFlags = HideFlags.DontSave;
            m_VisualEffect = m_VisualEffectGO.AddComponent<VisualEffect>();
            m_PreviewUtility.AddManagedGO(m_VisualEffectGO);

            m_VisualEffectGO.transform.localPosition = Vector3.zero;
            m_VisualEffectGO.transform.localRotation = Quaternion.identity;
            m_VisualEffectGO.transform.localScale = Vector3.one;

            m_VisualEffect.visualEffectAsset = target;

            m_CurrentBounds = new Bounds(Vector3.zero, Vector3.one);
            m_FrameCount = 0;
            m_Distance = 10;
            m_Angles = Vector3.forward;

            if (s_CubeWireFrame == null)
            {
                s_CubeWireFrame = new Mesh();

                var vertices = new Vector3[]
                {
                    new Vector3(-0.5f, -0.5f, -0.5f),
                    new Vector3(-0.5f, -0.5f, 0.5f),
                    new Vector3(-0.5f, 0.5f, 0.5f),
                    new Vector3(-0.5f, 0.5f, -0.5f),

                    new Vector3(0.5f, -0.5f, -0.5f),
                    new Vector3(0.5f, -0.5f, 0.5f),
                    new Vector3(0.5f, 0.5f, 0.5f),
                    new Vector3(0.5f, 0.5f, -0.5f)
                };


                var indices = new int[]
                {
                    0, 1,
                    0, 3,
                    0, 4,

                    6, 2,
                    6, 5,
                    6, 7,

                    1, 2,
                    1, 5,

                    3, 7,
                    3, 2,

                    4, 5,
                    4, 7
                };
                s_CubeWireFrame.vertices = vertices;
                s_CubeWireFrame.SetIndices(indices, MeshTopology.Lines, 0);
            }
        }

        var targetResources = targets.Cast<VisualEffectAsset>().Select(t => t.GetResource()).Where(t => t != null).ToArray();
        if (targetResources.Any())
        {
            resourceObject = new SerializedObject(targetResources);
            resourceUpdateModeProperty = resourceObject.FindProperty("m_Infos.m_UpdateMode");
            cullingFlagsProperty = resourceObject.FindProperty("m_Infos.m_CullingFlags");
            motionVectorRenderModeProperty = resourceObject.FindProperty("m_Infos.m_RendererSettings.motionVectorGenerationMode");
            prewarmDeltaTime = resourceObject.FindProperty("m_Infos.m_PreWarmDeltaTime");
            prewarmStepCount = resourceObject.FindProperty("m_Infos.m_PreWarmStepCount");
            initialEventName = resourceObject.FindProperty("m_Infos.m_InitialEventName");
        }
    }

    PreviewRenderUtility m_PreviewUtility;

    GameObject m_VisualEffectGO;
    VisualEffect m_VisualEffect;
    Vector3 m_Angles;
    float m_Distance;
    Bounds m_CurrentBounds;

    int m_FrameCount = 0;

    const int kSafeFrame = 2;

    public override bool HasPreviewGUI()
    {
        return !serializedObject.isEditingMultipleObjects;
    }

    void ComputeFarNear()
    {
        if (m_CurrentBounds.size != Vector3.zero)
        {
            float maxBounds = Mathf.Sqrt(m_CurrentBounds.size.x * m_CurrentBounds.size.x + m_CurrentBounds.size.y * m_CurrentBounds.size.y + m_CurrentBounds.size.z * m_CurrentBounds.size.z);
            m_PreviewUtility.camera.farClipPlane = m_Distance + maxBounds * 1.1f;
            m_PreviewUtility.camera.nearClipPlane = Mathf.Max(0.0001f, (m_Distance - maxBounds));
            m_PreviewUtility.camera.nearClipPlane = Mathf.Max(0.0001f, (m_Distance - maxBounds));
        }
    }

    public override void OnInteractivePreviewGUI(Rect r, GUIStyle background)
    {
        if (m_VisualEffectGO == null)
            OnEnable();

        bool isRepaint = (Event.current.type == EventType.Repaint);

        m_Angles = VFXPreviewGUI.Drag2D(m_Angles, r);
        Renderer renderer = m_VisualEffectGO.GetComponent<Renderer>();
        if (renderer == null)
            return;

        if (renderer.bounds.size != Vector3.zero)
        {
            m_CurrentBounds = renderer.bounds;

            //make sure that none of the bounds values are 0
            if (m_CurrentBounds.size.x == 0)
            {
                Vector3 size = m_CurrentBounds.size;
                size.x = (m_CurrentBounds.size.y + m_CurrentBounds.size.z) * 0.1f;
                m_CurrentBounds.size = size;
            }
            if (m_CurrentBounds.size.y == 0)
            {
                Vector3 size = m_CurrentBounds.size;
                size.y = (m_CurrentBounds.size.x + m_CurrentBounds.size.z) * 0.1f;
                m_CurrentBounds.size = size;
            }
            if (m_CurrentBounds.size.z == 0)
            {
                Vector3 size = m_CurrentBounds.size;
                size.z = (m_CurrentBounds.size.x + m_CurrentBounds.size.y) * 0.1f;
                m_CurrentBounds.size = size;
            }
        }

        if (m_FrameCount == kSafeFrame) // wait to frame before asking the renderer bounds as it is a computed value.
        {
            float maxBounds = Mathf.Sqrt(m_CurrentBounds.size.x * m_CurrentBounds.size.x + m_CurrentBounds.size.y * m_CurrentBounds.size.y + m_CurrentBounds.size.z * m_CurrentBounds.size.z);
            m_Distance = Mathf.Max(0.01f, maxBounds * 1.25f);
            ComputeFarNear();
        }
        else
        {
            ComputeFarNear();
        }
        m_FrameCount++;
        if (Event.current.isScrollWheel)
        {
            m_Distance *= 1 + (Event.current.delta.y * .015f);
        }

        if (m_Mat == null)
            m_Mat = (Material)EditorGUIUtility.LoadRequired("SceneView/HandleLines.mat");

        if (isRepaint)
        {
            m_PreviewUtility.BeginPreview(r, background);

            Quaternion rot = Quaternion.Euler(0, m_Angles.x, 0) * Quaternion.Euler(m_Angles.y, 0, 0);
            m_PreviewUtility.camera.transform.position = m_CurrentBounds.center + rot * new Vector3(0, 0, -m_Distance);
            m_PreviewUtility.camera.transform.localRotation = rot;
            m_PreviewUtility.DrawMesh(s_CubeWireFrame, Matrix4x4.TRS(m_CurrentBounds.center, Quaternion.identity, m_CurrentBounds.size), m_Mat, 0);
            m_PreviewUtility.Render(true);
            m_PreviewUtility.EndAndDrawPreview(r);

            // Ask for repaint so the effect is animated.
            Repaint();
        }
    }

    Material m_Mat;

    void OnDisable()
    {
        if (!UnityObject.ReferenceEquals(m_VisualEffectGO, null))
        {
            UnityObject.DestroyImmediate(m_VisualEffectGO);
        }
        if (m_PreviewUtility != null)
        {
            m_PreviewUtility.Cleanup();
        }
    }

    private static readonly GUIContent[] k_CullingOptionsContents = new GUIContent[]
    {
        EditorGUIUtility.TrTextContent("Recompute bounds and simulate when visible"),
        EditorGUIUtility.TrTextContent("Always recompute bounds, simulate only when visible"),
        EditorGUIUtility.TrTextContent("Always recompute bounds and simulate")
    };
    static readonly VFXCullingFlags[] k_CullingOptionsValue = new VFXCullingFlags[]
    {
        VFXCullingFlags.CullSimulation | VFXCullingFlags.CullBoundsUpdate,
        VFXCullingFlags.CullSimulation,
        VFXCullingFlags.CullNone,
    };

    private string UpdateModeToString(VFXUpdateMode mode)
    {
        return ObjectNames.NicifyVariableName(mode.ToString());
    }

    SerializedObject resourceObject;
    SerializedProperty resourceUpdateModeProperty;
    SerializedProperty cullingFlagsProperty;
    SerializedProperty motionVectorRenderModeProperty;
    SerializedProperty prewarmDeltaTime;
    SerializedProperty prewarmStepCount;
    SerializedProperty initialEventName;

    private static readonly float k_MinimalCommonDeltaTime = 1.0f / 800.0f;

    public override void OnInspectorGUI()
    {
        resourceObject.Update();

        GUI.enabled = AssetDatabase.IsOpenForEdit(this.target, StatusQueryOptions.UseCachedIfPossible);

        VFXUpdateMode initialUpdateMode = (VFXUpdateMode)0;
        bool? initialFixedDeltaTime = null;
        bool? initialProcessEveryFrame = null;
        bool? initialIgnoreGameTimeScale = null;
        if (resourceUpdateModeProperty.hasMultipleDifferentValues)
        {
            var resourceUpdateModeProperties = resourceUpdateModeProperty.serializedObject.targetObjects
                .Select(o => new SerializedObject(o)
                    .FindProperty(resourceUpdateModeProperty.propertyPath))
                .ToArray();                                 //N.B.: This will create garbage
            var allDeltaTime = resourceUpdateModeProperties.Select(o => ((VFXUpdateMode)o.intValue & VFXUpdateMode.DeltaTime) == VFXUpdateMode.DeltaTime)
                .Distinct();
            var allProcessEveryFrame = resourceUpdateModeProperties.Select(o => ((VFXUpdateMode)o.intValue & VFXUpdateMode.ExactFixedTimeStep) == VFXUpdateMode.ExactFixedTimeStep)
                .Distinct();
            var allIgnoreScale = resourceUpdateModeProperties.Select(o => ((VFXUpdateMode)o.intValue & VFXUpdateMode.IgnoreTimeScale) == VFXUpdateMode.IgnoreTimeScale)
                .Distinct();
            if (allDeltaTime.Count() == 1)
                initialFixedDeltaTime = !allDeltaTime.First();
            if (allProcessEveryFrame.Count() == 1)
                initialProcessEveryFrame = allProcessEveryFrame.First();
            if (allIgnoreScale.Count() == 1)
                initialIgnoreGameTimeScale = allIgnoreScale.First();
        }
        else
        {
            initialUpdateMode = (VFXUpdateMode)resourceUpdateModeProperty.intValue;
            initialFixedDeltaTime = !((initialUpdateMode & VFXUpdateMode.DeltaTime) == VFXUpdateMode.DeltaTime);
            initialProcessEveryFrame = (initialUpdateMode & VFXUpdateMode.ExactFixedTimeStep) == VFXUpdateMode.ExactFixedTimeStep;
            initialIgnoreGameTimeScale = (initialUpdateMode & VFXUpdateMode.IgnoreTimeScale) == VFXUpdateMode.IgnoreTimeScale;
        }

        EditorGUI.showMixedValue = !initialFixedDeltaTime.HasValue;
        var deltaTimeContent = EditorGUIUtility.TrTextContent("Fixed Delta Time", "If enabled, use visual effect manager fixed delta time mode, otherwise, use the default Time.deltaTime.");
        var processEveryFrameContent = EditorGUIUtility.TrTextContent("Exact Fixed Time", "Only relevant when using Fixed Delta Time. When enabled, several updates can be processed per frame (e.g.: if a frame is 10ms and the fixed frame rate is set to 5 ms, the effect will update twice with a 5ms deltaTime instead of once with a 10ms deltaTime). This method is expensive and should only be used for high-end scenarios.");
        var ignoreTimeScaleContent = EditorGUIUtility.TrTextContent("Ignore Time Scale", "When enabled, the computed visual effect delta time ignores the game Time Scale value (Play Rate is still applied).");

        EditorGUI.BeginChangeCheck();

        VisualEffectEditor.ShowHeader(EditorGUIUtility.TrTextContent("Update mode"), false, false);
        bool newFixedDeltaTime = EditorGUILayout.Toggle(deltaTimeContent, initialFixedDeltaTime ?? false);
        bool newExactFixedTimeStep = false;
        EditorGUI.showMixedValue = !initialProcessEveryFrame.HasValue;
        EditorGUI.BeginDisabledGroup((!initialFixedDeltaTime.HasValue || !initialFixedDeltaTime.Value) && !resourceUpdateModeProperty.hasMultipleDifferentValues);
        newExactFixedTimeStep = EditorGUILayout.Toggle(processEveryFrameContent, initialProcessEveryFrame ?? false);
        EditorGUI.EndDisabledGroup();
        EditorGUI.showMixedValue = !initialIgnoreGameTimeScale.HasValue;
        bool newIgnoreTimeScale = EditorGUILayout.Toggle(ignoreTimeScaleContent, initialIgnoreGameTimeScale ?? false);

        if (EditorGUI.EndChangeCheck())
        {
            if (!resourceUpdateModeProperty.hasMultipleDifferentValues)
            {
                var newUpdateMode = (VFXUpdateMode)0;
                if (!newFixedDeltaTime)
                    newUpdateMode = newUpdateMode | VFXUpdateMode.DeltaTime;
                if (newExactFixedTimeStep)
                    newUpdateMode = newUpdateMode | VFXUpdateMode.ExactFixedTimeStep;
                if (newIgnoreTimeScale)
                    newUpdateMode = newUpdateMode | VFXUpdateMode.IgnoreTimeScale;

                resourceUpdateModeProperty.intValue = (int)newUpdateMode;
                resourceObject.ApplyModifiedProperties();
            }
            else
            {
                var resourceUpdateModeProperties = resourceUpdateModeProperty.serializedObject.targetObjects.Select(o => new SerializedObject(o).FindProperty(resourceUpdateModeProperty.propertyPath));
                foreach (var property in resourceUpdateModeProperties)
                {
                    var updateMode = (VFXUpdateMode)property.intValue;

                    if (initialFixedDeltaTime.HasValue)
                    {
                        if (!newFixedDeltaTime)
                            updateMode = updateMode | VFXUpdateMode.DeltaTime;
                        else
                            updateMode = updateMode & ~VFXUpdateMode.DeltaTime;
                    }
                    else
                    {
                        if (newFixedDeltaTime)
                            updateMode = updateMode & ~VFXUpdateMode.DeltaTime;
                    }

                    if (newExactFixedTimeStep)
                        updateMode = updateMode | VFXUpdateMode.ExactFixedTimeStep;
                    else if (initialProcessEveryFrame.HasValue)
                        updateMode = updateMode & ~VFXUpdateMode.ExactFixedTimeStep;

                    if (newIgnoreTimeScale)
                        updateMode = updateMode | VFXUpdateMode.IgnoreTimeScale;
                    else if (initialIgnoreGameTimeScale.HasValue)
                        updateMode = updateMode & ~VFXUpdateMode.IgnoreTimeScale;

                    property.intValue = (int)updateMode;
                    property.serializedObject.ApplyModifiedProperties();
                }
            }
        }
        VisualEffectAsset asset = (VisualEffectAsset)target;
        VisualEffectResource resource = asset.GetResource();

        //The following should be working, and works for newly created systems, but fails for old systems,
        //due probably to incorrectly pasting the VFXData when creating them.
        // bool hasAutomaticBoundsSystems = resource.GetOrCreateGraph().children
        //     .OfType<VFXDataParticle>().Any(d => d.boundsMode == BoundsSettingMode.Automatic);

        bool hasAutomaticBoundsSystems = resource.GetOrCreateGraph().children
            .OfType<VFXBasicInitialize>().Any(d => (d.GetData() as VFXDataParticle).boundsMode == BoundsSettingMode.Automatic);
        using (new EditorGUI.DisabledScope(hasAutomaticBoundsSystems))
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUI.showMixedValue = cullingFlagsProperty.hasMultipleDifferentValues;
            string forceSimulateTooltip = hasAutomaticBoundsSystems
                ? " When using systems with Bounds Mode set to Automatic, this has to be set to Always recompute bounds and simulate."
                : "";
            EditorGUILayout.PrefixLabel(EditorGUIUtility.TrTextContent("Culling Flags", "Specifies how the system recomputes its bounds and simulates when off-screen." + forceSimulateTooltip));
            EditorGUI.BeginChangeCheck();

            int newOption =
                EditorGUILayout.Popup(
                    Array.IndexOf(k_CullingOptionsValue, (VFXCullingFlags)cullingFlagsProperty.intValue),
                    k_CullingOptionsContents);
            if (EditorGUI.EndChangeCheck())
            {
                cullingFlagsProperty.intValue = (int)k_CullingOptionsValue[newOption];
                resourceObject.ApplyModifiedProperties();
            }
        }

        EditorGUILayout.EndHorizontal();

        VisualEffectEditor.ShowHeader(EditorGUIUtility.TrTextContent("Initial state"), false, false);
        if (prewarmDeltaTime != null && prewarmStepCount != null)
        {
            if (!prewarmDeltaTime.hasMultipleDifferentValues && !prewarmStepCount.hasMultipleDifferentValues)
            {
                var currentDeltaTime = prewarmDeltaTime.floatValue;
                var currentStepCount = prewarmStepCount.intValue;
                var currentTotalTime = currentDeltaTime * currentStepCount;
                EditorGUI.BeginChangeCheck();
                currentTotalTime = EditorGUILayout.FloatField(EditorGUIUtility.TrTextContent("PreWarm Total Time", "Sets the time in seconds to advance the current effect to when it is initially played. "), currentTotalTime);
                if (EditorGUI.EndChangeCheck())
                {
                    if (currentStepCount <= 0)
                    {
                        prewarmStepCount.intValue = currentStepCount = 1;
                    }

                    currentDeltaTime = currentTotalTime / currentStepCount;
                    prewarmDeltaTime.floatValue = currentDeltaTime;
                    resourceObject.ApplyModifiedProperties();
                }

                EditorGUI.BeginChangeCheck();
                currentStepCount = EditorGUILayout.IntField(EditorGUIUtility.TrTextContent("PreWarm Step Count", "Sets the number of simulation steps the prewarm should be broken down to. "), currentStepCount);
                if (EditorGUI.EndChangeCheck())
                {
                    if (currentStepCount <= 0 && currentTotalTime != 0.0f)
                    {
                        prewarmStepCount.intValue = currentStepCount = 1;
                    }

                    currentDeltaTime = currentTotalTime == 0.0f ? 0.0f : currentTotalTime / currentStepCount;
                    prewarmDeltaTime.floatValue = currentDeltaTime;
                    prewarmStepCount.intValue = currentStepCount;
                    resourceObject.ApplyModifiedProperties();
                }

                EditorGUI.BeginChangeCheck();
                currentDeltaTime = EditorGUILayout.FloatField(EditorGUIUtility.TrTextContent("PreWarm Delta Time", "Sets the time in seconds for each step to achieve the desired total prewarm time."), currentDeltaTime);
                if (EditorGUI.EndChangeCheck())
                {
                    if (currentDeltaTime < k_MinimalCommonDeltaTime)
                    {
                        prewarmDeltaTime.floatValue = currentDeltaTime = k_MinimalCommonDeltaTime;
                    }

                    if (currentDeltaTime > currentTotalTime)
                    {
                        currentTotalTime = currentDeltaTime;
                    }

                    if (currentTotalTime != 0.0f)
                    {
                        var candidateStepCount_A = Mathf.FloorToInt(currentTotalTime / currentDeltaTime);
                        var candidateStepCount_B = Mathf.RoundToInt(currentTotalTime / currentDeltaTime);

                        var totalTime_A = currentDeltaTime * candidateStepCount_A;
                        var totalTime_B = currentDeltaTime * candidateStepCount_B;

                        if (Mathf.Abs(totalTime_A - currentTotalTime) < Mathf.Abs(totalTime_B - currentTotalTime))
                        {
                            currentStepCount = candidateStepCount_A;
                        }
                        else
                        {
                            currentStepCount = candidateStepCount_B;
                        }

                        prewarmStepCount.intValue = currentStepCount;
                    }
                    prewarmDeltaTime.floatValue = currentDeltaTime;
                    resourceObject.ApplyModifiedProperties();
                }
            }
            else
            {
                //Multi selection case, can't resolve total time easily
                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = prewarmStepCount.hasMultipleDifferentValues;
                EditorGUILayout.PropertyField(prewarmStepCount, EditorGUIUtility.TrTextContent("PreWarm Step Count", "Sets the number of simulation steps the prewarm should be broken down to."));
                EditorGUI.showMixedValue = prewarmDeltaTime.hasMultipleDifferentValues;
                EditorGUILayout.PropertyField(prewarmDeltaTime, EditorGUIUtility.TrTextContent("PreWarm Delta Time", "Sets the time in seconds for each step to achieve the desired total prewarm time."));
                if (EditorGUI.EndChangeCheck())
                {
                    if (prewarmDeltaTime.floatValue < k_MinimalCommonDeltaTime)
                        prewarmDeltaTime.floatValue = k_MinimalCommonDeltaTime;
                    resourceObject.ApplyModifiedProperties();
                }
            }
        }

        if (initialEventName != null)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = initialEventName.hasMultipleDifferentValues;
            EditorGUILayout.PropertyField(initialEventName, new GUIContent("Initial Event Name", "Sets the name of the event which triggers once the system is activated. Default: ‘OnPlay’."));
            if (EditorGUI.EndChangeCheck())
            {
                resourceObject.ApplyModifiedProperties();
            }
        }

        if (!serializedObject.isEditingMultipleObjects)
        {
            asset = (VisualEffectAsset)target;
            resource = asset.GetResource();

            m_OutputContexts.Clear();
            m_OutputContexts.AddRange(resource.GetOrCreateGraph().children.OfType<IVFXSubRenderer>().OrderBy(t => t.vfxSystemSortPriority));

            m_ReorderableList.DoLayoutList();

            VisualEffectEditor.ShowHeader(EditorGUIUtility.TrTextContent("Shaders"), false, false);

            string assetPath = AssetDatabase.GetAssetPath(asset);
            UnityObject[] objects = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            string directory = Path.GetDirectoryName(assetPath) + "/" + VFXExternalShaderProcessor.k_ShaderDirectory + "/" + asset.name + "/";

            foreach (var obj in objects)
            {
                if (obj is Material || obj is ComputeShader)
                {
                    GUILayout.BeginHorizontal();
                    Rect r = GUILayoutUtility.GetRect(0, 18, GUILayout.ExpandWidth(true));

                    int buttonsWidth = VFXExternalShaderProcessor.allowExternalization ? 240 : 160;

                    int index = resource.GetShaderIndex(obj);

                    var shader = obj;
                    if (obj is Material) // Retrieve the shader from the material
                        shader = ((Material)(obj)).shader;
                    if (shader == null)
                        continue;

                    Rect labelR = r;
                    labelR.width -= buttonsWidth;
                    GUI.Label(labelR, shader.name);

                    if (index >= 0)
                    {
                        if (VFXExternalShaderProcessor.allowExternalization && index < resource.GetShaderSourceCount())
                        {
                            string shaderSourceName = resource.GetShaderSourceName(index);
                            string externalPath = directory + shaderSourceName;

                            externalPath = directory + shaderSourceName.Replace('/', '_') + VFXExternalShaderProcessor.k_ShaderExt;

                            Rect buttonRect = r;
                            buttonRect.xMin = labelR.xMax;
                            buttonRect.width = 80;
                            labelR.width += 80;
                            if (System.IO.File.Exists(externalPath))
                            {
                                if (GUI.Button(buttonRect, "Reveal External"))
                                {
                                    EditorUtility.RevealInFinder(externalPath);
                                }
                            }
                            else
                            {
                                if (GUI.Button(buttonRect, "Externalize"))
                                {
                                    Directory.CreateDirectory(directory);

                                    File.WriteAllText(externalPath, "//" + shaderSourceName + "," + index.ToString() + "\n//Don't delete the previous line or this one\n" + resource.GetShaderSource(index));
                                }
                            }
                        }

                        Rect buttonR = r;
                        buttonR.xMin = labelR.xMax;
                        buttonR.width = 110;
                        labelR.width += 110;
                        if (GUI.Button(buttonR, "Show Generated"))
                        {
                            resource.ShowGeneratedShaderFile(index);
                        }
                    }

                    Rect selectButtonR = r;
                    selectButtonR.xMin = labelR.xMax;
                    selectButtonR.width = 50;
                    if (GUI.Button(selectButtonR, "Select"))
                    {
                        Selection.activeObject = shader;
                    }
                    GUILayout.EndHorizontal();
                }
            }
        }
        GUI.enabled = false;
    }
}


static class VFXPreviewGUI
{
    static int sliderHash = "Slider".GetHashCode();
    public static Vector2 Drag2D(Vector2 scrollPosition, Rect position)
    {
        int id = GUIUtility.GetControlID(sliderHash, FocusType.Passive);
        Event evt = Event.current;
        switch (evt.GetTypeForControl(id))
        {
            case EventType.MouseDown:
                if (position.Contains(evt.mousePosition) && position.width > 50)
                {
                    GUIUtility.hotControl = id;
                    evt.Use();
                    EditorGUIUtility.SetWantsMouseJumping(1);
                }
                break;
            case EventType.MouseDrag:
                if (GUIUtility.hotControl == id)
                {
                    scrollPosition -= -evt.delta * (evt.shift ? 3 : 1) / Mathf.Min(position.width, position.height) * 140.0f;
                    scrollPosition.y = Mathf.Clamp(scrollPosition.y, -90, 90);
                    evt.Use();
                    GUI.changed = true;
                }
                break;
            case EventType.MouseUp:
                if (GUIUtility.hotControl == id)
                    GUIUtility.hotControl = 0;
                EditorGUIUtility.SetWantsMouseJumping(0);
                break;
        }
        return scrollPosition;
    }
}
