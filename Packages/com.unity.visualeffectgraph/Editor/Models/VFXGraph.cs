//#define USE_SHADER_AS_SUBASSET
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

using Unity.Profiling;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.VFX.Block;
using UnityEditor.VFX.UI;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.Profiling;

using UnityObject = UnityEngine.Object;

namespace UnityEditor.VFX
{
    [InitializeOnLoad]
    class VFXGraphPreprocessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            List<string> assetToReimport = null;

#if VFX_HAS_TIMELINE
            UnityEditor.VFX.Migration.ActivationToControlTrack.SanitizePlayable(importedAssets);
#endif

            if (deletedAssets.Any())
            {
                VFXViewWindow.GetAllWindows().ToList().ForEach(x => x.UpdateHistory());
            }

            var isAnySubgraphImported = importedAssets.Any(VisualEffectAssetModificationProcessor.IsVFXSubgraphExtension);

            foreach (var assetPath in importedAssets)
            {
                bool isVFX = VisualEffectAssetModificationProcessor.HasVFXExtension(assetPath);
                if (isVFX)
                {
                    VisualEffectResource resource = VisualEffectResource.GetResourceAtPath(assetPath);
                    if (resource == null)
                        continue;
                    VFXGraph graph = resource.GetOrCreateGraph(); //resource.graph should be already != null at this stage but GetOrCreateGraph is also assigning the visualEffectResource. It's required for UpdateSubAssets
                    if (graph != null)
                    {
                        bool wasGraphSanitized = graph.sanitized;

                        try
                        {
                            graph.SanitizeForImport();
                            if (!wasGraphSanitized && graph.sanitized)
                            {
                                assetToReimport ??= new List<string>();
                                assetToReimport.Add(assetPath);
                            }
                        }
                        catch (Exception exception)
                        {
                            Debug.LogErrorFormat("Exception during sanitization of {0} : {1}", assetPath, exception);
                        }

                        var window = VFXViewWindow.GetWindow(graph, false, false);
                        if (window != null)
                        {
                            window.UpdateTitle(assetPath);
                            // Force blackboard update only when a subgraph gets re-imported
                            if (isAnySubgraphImported)
                            {
                                window.graphView?.blackboard.Update(true);
                            }
                        }
                    }
                    else
                    {
                        Debug.LogErrorFormat("VisualEffectGraphResource without graph : {0}", assetPath);
                    }
                }
            }

            //Relaunch previously skipped OnCompileResource
            if (assetToReimport != null)
            {
                AssetDatabase.StartAssetEditing();
                foreach (var assetPath in assetToReimport)
                {
                    try
                    {
                        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogErrorFormat("Exception during reimport of {0} : {1}", assetPath, exception);
                    }
                }
                AssetDatabase.StopAssetEditing();
            }
        }

        static string[] OnAddResourceDependencies(string assetPath)
        {
            VisualEffectResource resource = VisualEffectResource.GetResourceAtPath(assetPath);
            if (resource != null)
            {
                if (resource.graph is VFXGraph)
                    return resource.GetOrCreateGraph().UpdateImportDependencies();
                Debug.LogError("VisualEffectGraphResource without graph");
            }
            return null;
        }

        static void OnCompileResource(VisualEffectResource resource)
        {
            if (resource != null)
            {
                VFXGraph graph = resource.graph as VFXGraph;
                if (graph != null)
                {
                    if (!graph.sanitized)
                    {
                        //Early return, the reimport will be forced with the next OnPostprocessAllAssets after Sanitize
                        resource.ClearRuntimeData();
                    }
                    else
                    {
                        //Workaround, use backup system to prevent any modification of the graph during compilation
                        //The responsible of this unexpected change is PrepareSubgraphs => RecurseSubgraphRecreateCopy => ResyncSlots
                        //It will let the VFXGraph in a really bad state after compilation.
                        graph = resource.GetOrCreateGraph();
                        var dependencies = new HashSet<ScriptableObject>();
                        dependencies.Add(graph);
                        graph.CollectDependencies(dependencies);
                        var backup = VFXMemorySerializer.StoreObjectsToByteArray(dependencies.ToArray(), CompressionLevel.None);

                        graph.errorManager.RefreshCompilationReport();
                        graph.CompileForImport();

                        VFXGraph.restoringGraph = true;
                        try
                        {
                            VFXMemorySerializer.ExtractObjects(backup, false);
                        }
                        finally
                        {
                            VFXGraph.restoringGraph = false;
                        }
                        //The backup during undo/redo is actually calling UnknownChange after ExtractObjects
                        //You have to avoid because it will call ResyncSlot
                    }
                }
                else
                    Debug.LogError("OnCompileResource error - VisualEffectResource without graph");
            }
        }

        static void OnSetupMaterial(VisualEffectResource resource, Material material, UnityObject model)
        {
            if (resource != null)
            {
                // sanity checks
                if (resource.graph == null)
                {
                    Debug.LogError("OnSetupMaterial error - VisualEffectResource without graph");
                    return;
                }
                if (!(model is VFXModel))
                {
                    Debug.LogError("OnSetupMaterial error - Passed object is not a VFXModel");
                    return;
                }
                //if (resource.graph != ((VFXModel)model).GetGraph())
                //{
                //    Debug.LogError("OnSetupMaterial error - VisualEffectResource and VFXModel graph do not match");
                //    return;
                //}

                if (!resource.GetOrCreateGraph().sanitized)
                {
                    Debug.LogError("OnSetupMaterial error - Graph hasn't been sanitized");
                    return;
                }

                // Actual call
                if (model is IVFXSubRenderer)
                {
                    ((IVFXSubRenderer)model).SetupMaterial(material);
                }
            }
        }

        static VFXGraphPreprocessor()
        {
            EditorApplication.update += CheckCompilationVersion;

            VisualEffectResource.onAddResourceDependencies = OnAddResourceDependencies;
            VisualEffectResource.onCompileResource = OnCompileResource;
            VisualEffectResource.onSetupMaterial = OnSetupMaterial;
        }

        static void CheckCompilationVersion()
        {
            EditorApplication.update -= CheckCompilationVersion;

            UnityObject vfxmanager = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/VFXManager.asset").FirstOrDefault();
            SerializedObject serializedVFXManager = new SerializedObject(vfxmanager);
            var compiledVersionProperty = serializedVFXManager.FindProperty("m_CompiledVersion");
            var runtimeVersionProperty = serializedVFXManager.FindProperty("m_RuntimeVersion");

            if (compiledVersionProperty.intValue != VFXGraphCompiledData.compiledVersion || runtimeVersionProperty.intValue != VisualEffectAsset.currentRuntimeDataVersion)
            {
                string[] allVisualEffectAssets = AssetDatabase.FindAssets("t:VisualEffectAsset");
                compiledVersionProperty.intValue = (int)VFXGraphCompiledData.compiledVersion;
                runtimeVersionProperty.intValue = (int)VisualEffectAsset.currentRuntimeDataVersion;
                serializedVFXManager.ApplyModifiedProperties();

                AssetDatabase.StartAssetEditing();
                try
                {
                    foreach (var guid in AssetDatabase.FindAssets("t:VisualEffectAsset"))
                    {
                        var path = AssetDatabase.GUIDToAssetPath(guid);

                        AssetDatabase.ImportAsset(path);
                    }

                    VFXAssetManager.ImportAllVFXShaders();
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                }
            }
        }
    }
    class VFXAssetManager : EditorWindow
    {
        public static Dictionary<VisualEffectObject, string> GetAllVisualEffectObjects()
        {
            var allVisualEffectObjects = new Dictionary<VisualEffectObject, string>();
            var vfxObjectsGuid = AssetDatabase.FindAssets("t:VisualEffectObject");
            foreach (var guid in vfxObjectsGuid)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var vfxObj = AssetDatabase.LoadAssetAtPath<VisualEffectObject>(assetPath);
                if (vfxObj != null)
                {
                    allVisualEffectObjects[vfxObj] = assetPath;
                }
            }

            return allVisualEffectObjects;
        }

        public static Dictionary<Shader, string> GetAllShaderGraph()
        {
            var allShaderGraphObjects = new Dictionary<Shader, string>();
            var shaderGraphGuids = AssetDatabase.FindAssets("t:Shader");
            foreach (var guid in shaderGraphGuids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var shaderGraph = AssetDatabase.LoadAssetAtPath<Shader>(assetPath);
                if (shaderGraph != null)
                {
                    allShaderGraphObjects[shaderGraph] = assetPath;
                }
            }

            return allShaderGraphObjects;
        }

        // Import VFX shader graph assets
        // Because some shader compatible with VFX can be there before the Visual Effect package is installed
        // We must re-import them to generate the ShaderGraphVfxAsset
        public static void ImportAllVFXShaders()
        {
            var currentSrpBinder = VFXLibrary.currentSRPBinder;
            if (currentSrpBinder != null)
            {
                foreach (var (shader, path) in GetAllShaderGraph())
                {
                    var assets = AssetDatabase.LoadAllAssetsAtPath(path);
                    if (assets.OfType<ShaderGraphVfxAsset>().Any())
                    {
                        continue;
                    }

                    if (shader != null && currentSrpBinder.IsShaderVFXCompatible(shader))
                    {
                        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                    }
                }
            }
        }

        public static void Build(bool forceDirty = false)
        {
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var vfxObj in GetAllVisualEffectObjects())
                {
                    if (VFXViewPreference.advancedLogs)
                        Debug.Log($"Recompile VFX asset: {vfxObj.Key} ({vfxObj.Value})");

                    var resource = VisualEffectResource.GetResourceAtPath(vfxObj.Value);
                    if (resource != null)
                    {
                        AssetDatabase.ImportAsset(vfxObj.Value);
                        if (forceDirty)
                            EditorUtility.SetDirty(resource);
                    }
                }

                VFXExpression.ClearCache();

                ImportAllVFXShaders();
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            EditorUtility.UnloadUnusedAssetsImmediate();
            GC.Collect();
        }

        [MenuItem("Edit/VFX/Rebuild And Save All VFX Graphs", priority = 10319)]
        public static void BuildAndSave()
        {
            Build(true);
            AssetDatabase.SaveAssets();
        }
    }

    class VisualEffectAssetModificationProcessor : UnityEditor.AssetModificationProcessor
    {
        public static bool HasVFXExtension(string filePath)
        {
            if (!string.IsNullOrEmpty(filePath) &&
                (filePath.EndsWith(VisualEffectResource.Extension, StringComparison.OrdinalIgnoreCase)
              || filePath.EndsWith(VisualEffectSubgraphBlock.Extension, StringComparison.OrdinalIgnoreCase)
              || filePath.EndsWith(VisualEffectSubgraphOperator.Extension, StringComparison.OrdinalIgnoreCase)))
            {

// See this PR https://github.com/Unity-Technologies/Graphics/pull/6890
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
                return !AssetDatabase.IsValidFolder(filePath);
#else
                return true;
#endif
            }

            return false;
        }

        public static bool IsVFXSubgraphExtension(string filePath)
        {
            return filePath.EndsWith(VisualEffectSubgraphBlock.Extension, StringComparison.OrdinalIgnoreCase)
                   || filePath.EndsWith(VisualEffectSubgraphOperator.Extension, StringComparison.OrdinalIgnoreCase);
        }

        static string[] OnWillSaveAssets(string[] paths)
        {
            Profiler.BeginSample("VisualEffectAssetModicationProcessor.OnWillSaveAssets");
            bool started = false;
            try {
                foreach (string path in paths.Where(HasVFXExtension))
                {
                    if (!started)
                    {
                        started = true;
                        AssetDatabase.StartAssetEditing();
                    }
                    var vfxResource = VisualEffectResource.GetResourceAtPath(path);
                    vfxResource?.WriteAssetWithSubAssets();
                }
            }
            finally
            {
                if (started)
                    AssetDatabase.StopAssetEditing();
            }
            Profiler.EndSample();
            return paths;
        }
    }

    static class VisualEffectResourceExtensions
    {
        public static VFXGraph GetOrCreateGraph(this VisualEffectResource resource)
        {
            VFXGraph graph = resource.graph as VFXGraph;

            if (graph == null)
            {
                string assetPath = AssetDatabase.GetAssetPath(resource);
                AssetDatabase.ImportAsset(assetPath);

                graph = resource.GetContents().OfType<VFXGraph>().FirstOrDefault();
            }

            if (graph == null)
            {
                graph = ScriptableObject.CreateInstance<VFXGraph>();
                resource.graph = graph;
                graph.hideFlags |= HideFlags.HideInHierarchy;
                graph.visualEffectResource = resource;
                // in this case we must update the subassets so that the graph is added to the resource dependencies
                graph.UpdateSubAssets();
            }

            graph.visualEffectResource = resource;
            return graph;
        }

        public static void UpdateSubAssets(this VisualEffectResource resource)
        {
            resource.GetOrCreateGraph().UpdateSubAssets();
        }

        public static void WriteAssetWithSubAssets(this VisualEffectResource resource)
        {
            var graph = resource.GetOrCreateGraph();
            graph.UpdateSubAssets();
            resource.WriteAsset();
        }

        public static bool IsAssetEditable(this VisualEffectResource resource)
        {
            return AssetDatabase.IsOpenForEdit((UnityEngine.Object)resource.asset ?? resource.subgraph, StatusQueryOptions.UseCachedIfPossible);
        }
    }

    static class VisualEffectObjectExtensions
    {
        public static VisualEffectResource GetOrCreateResource(this VisualEffectObject asset)
        {
            string assetPath = AssetDatabase.GetAssetPath(asset);
            VisualEffectResource resource = VisualEffectResource.GetResourceAtPath(assetPath);

            if (resource == null && !string.IsNullOrEmpty(assetPath))
            {
                resource = new VisualEffectResource();
                resource.SetAssetPath(assetPath);
            }
            return resource;
        }

        public static VisualEffectResource GetResource(this VisualEffectObject asset)
        {
            string assetPath = AssetDatabase.GetAssetPath(asset);
            VisualEffectResource resource = VisualEffectResource.GetResourceAtPath(assetPath);

            if (resource == null && !string.IsNullOrEmpty(assetPath))
                throw new NullReferenceException($"VFX resource does not exist for this asset at path: {assetPath}");

            return resource;
        }
    }

    class VFXGraph : VFXModel
    {
        // Please add increment reason for each version below
        // 1: Size refactor
        // 2: Change some SetAttribute to spaceable slot
        // 3: Remove Masked from blendMode in Outputs and split feature to UseAlphaClipping
        // 4: TransformVector|Position|Direction & DistanceToSphere|Plane|Line have now spaceable outputs
        // 5: Harmonized position blocks composition: PositionAABox was the only one with Overwrite position
        // 6: Remove automatic strip orientation from quad strip context
        // 7: Add CameraBuffer type
        // 8: Bounds computation introduces a BoundsSettingMode for VFXDataParticles
        // 9: Update HDRP decal angle fade encoding
        // 10: Position Mesh and Skinned Mesh out of experimental (changing the list of flag and output types)
        // 11: Instancing
        // 12: Change space value of VFXSpace.None from 'int.MaxValue' to '-1'
        // 13: Unexpected incorrect synchronization of output with ShaderGraph
        // 14: ShaderGraph integration uses the material variant workflow
        // 15: New ShaderGraph integration uses independent output
        // 16: Add a collection of custom attributes (to be listed in blackboard)
        // 17: New Flipbook player and split the different Flipbook modes in UVMode into separate variables
        // 18: Change ProbabilitySampling m_IntegratedRandomDeprecated changed to m_Mode
        public static readonly int CurrentVersion = 18;

        public override void OnSRPChanged()
        {
            m_GraphSanitized = false;
            m_ExpressionGraphDirty = true;
        }

        public VisualEffectResource visualEffectResource
        {
            get
            {
                return m_Owner;
            }
            set
            {
                if (m_Owner != value)
                {
                    m_Owner = value;
                    m_Owner.graph = this;
                    m_ExpressionGraphDirty = true;
                }
            }
        }
        [SerializeField]
        VFXUI m_UIInfos;

        public VFXUI UIInfos
        {
            get
            {
                if (m_UIInfos == null)
                {
                    m_UIInfos = ScriptableObject.CreateInstance<VFXUI>();
                }
                return m_UIInfos;
            }
        }

        [SerializeField]
        List<VFXCustomAttributeDescriptor> m_CustomAttributes;
        // Do not serialize custom attributes imported from sub-graphs
        readonly List<VFXCustomAttributeDescriptor> m_DependenciesCustomAttributes = new();

        public IEnumerable<VFXCustomAttributeDescriptor> customAttributes => (m_CustomAttributes ??= new List<VFXCustomAttributeDescriptor>()).Concat(m_DependenciesCustomAttributes);

        public VFXParameterInfo[] m_ParameterInfo;

        private VFXErrorManager m_ErrorManager;
        private readonly VFXSystemNames m_SystemNames = new();
        private readonly VFXAttributesManager m_AttributesManager = new();

        public VFXErrorManager errorManager => m_ErrorManager ??= new VFXErrorManager();
        public VFXSystemNames systemNames => m_SystemNames;
        public VFXAttributesManager attributesManager => m_AttributesManager;

        public void BuildParameterInfo()
        {
            m_ParameterInfo = VFXParameterInfo.BuildParameterInfo(this);
            VisualEffectEditor.RepaintAllEditors();
        }

        public override bool AcceptChild(VFXModel model, int index = -1)
        {
            return !(model is VFXGraph); // Can hold any model except other VFXGraph
        }

        public void SyncCustomAttributes()
        {
            m_CustomAttributes.RemoveAll(x => x == null);
            foreach (var attributeDescriptor in customAttributes.ToArray())
            {
                attributeDescriptor.graph = this;
                m_AttributesManager.TryRegisterCustomAttribute(attributeDescriptor.attributeName, attributeDescriptor.type, attributeDescriptor.description, out _);

                var usages = GetCustomAttributeUsage(attributeDescriptor.attributeName).ToArray();

                attributeDescriptor.ClearSubgraphUse();
                foreach (var usage in usages.Where(VFXSubgraphUtility.IsSubgraphModel))
                {
                    attributeDescriptor.AddSubgraphUse(usage.name);
                }

                // Remove custom attributes from sub-graphs that are not used by sub-graph anymore
                if (attributeDescriptor.usedInSubgraphs == null && m_DependenciesCustomAttributes.Contains(attributeDescriptor))
                {
                    m_DependenciesCustomAttributes.Remove(attributeDescriptor);
                    SetCustomAttributeDirty();
                }

                // Check if custom attribute is used, but not in sub-graph and not yet in the serialized collection
                if (attributeDescriptor.usedInSubgraphs == null && usages.Length > 0 && !m_CustomAttributes.Contains(attributeDescriptor))
                {
                    m_CustomAttributes.Add(attributeDescriptor);
                    attributeDescriptor.isReadOnly = false;
                    SetCustomAttributeDirty();
                }
                // Move custom attributes used in subgraph into the transient collection
                else if (attributeDescriptor.usedInSubgraphs != null && m_CustomAttributes.Contains(attributeDescriptor))
                {
                    m_CustomAttributes.Remove(attributeDescriptor);
                    if (!m_DependenciesCustomAttributes.Contains(attributeDescriptor))
                    {
                        m_DependenciesCustomAttributes.Add(attributeDescriptor);
                    }
                    attributeDescriptor.isReadOnly = true;
                    SetCustomAttributeDirty();
                }
            }

            // Remove custom attributes from attribute manager if they do not exist anymore
            foreach (var customAttribute in m_AttributesManager.GetCustomAttributes().ToArray())
            {
                if (customAttributes.All(x => string.Compare(x.attributeName, customAttribute.name, StringComparison.OrdinalIgnoreCase) != 0))
                {
                    m_AttributesManager.UnregisterCustomAttribute(customAttribute.name);
                }
            }
        }

        public bool TryAddCustomAttribute(string attributeName, VFXValueType type, string description, bool isReadOnly, out VFXAttribute newAttribute)
        {
            var signature = CustomAttributeUtility.GetSignature(type);

            if (m_AttributesManager.TryRegisterCustomAttribute(attributeName, signature, description, out newAttribute))
            {
                var customAttribute = CreateInstance<VFXCustomAttributeDescriptor>();
                customAttribute.attributeName = newAttribute.name;
                customAttribute.type = CustomAttributeUtility.GetSignature(type);
                customAttribute.description = description;
                customAttribute.graph = this;
                customAttribute.isReadOnly = isReadOnly;

                if (!isReadOnly)
                {
                    m_CustomAttributes.Add(customAttribute);
                }
                else
                {
                    m_DependenciesCustomAttributes.Add(customAttribute);
                }

                if (!isReadOnly) // if not from subgraph
                    Invalidate(InvalidationCause.kStructureChanged);
                return true;
            }

            return false;
        }

        public bool IsCustomAttributeUsed(string attributeName)
        {
            // First look at operators
            if (children
                .OfType<IVFXAttributeUsage>()
                .SelectMany(x => x.usedAttributes)
                .Any(x => string.Compare(x.name, attributeName, StringComparison.OrdinalIgnoreCase) == 0))
                return true;

            // Look in context blocks
            if (children
                .OfType<VFXContext>()
                .SelectMany(x => x.children)
                .OfType<IVFXAttributeUsage>()
                .SelectMany(x => x.usedAttributes)
                .Distinct()
                .Any(x => string.Compare(x.name, attributeName, StringComparison.OrdinalIgnoreCase) == 0))
                return true;

            return false;
        }

        public void SetCustomAttributeOrder(string attributeName, int order)
        {
            if (TryFindCustomAttributeDescriptor(attributeName, out var attributeDescriptor))
            {
                m_CustomAttributes.Remove(attributeDescriptor);
                m_CustomAttributes.Insert(order, attributeDescriptor);
                Invalidate(InvalidationCause.kStructureChanged);
            }
        }

        public bool TryFindCustomAttributeDescriptor(string attributeName, out VFXCustomAttributeDescriptor attributeDescriptor)
        {
            attributeDescriptor = customAttributes.SingleOrDefault(x => string.Compare(attributeName, x.attributeName, StringComparison.OrdinalIgnoreCase) == 0);
            return attributeDescriptor != null;
        }

        public IEnumerable<string> GetUnusedCustomAttributes()
        {
            var objs = new HashSet<ScriptableObject>();
            CollectDependencies(objs, true);

            var nodesUsingCustomAttribute = objs
                .OfType<IVFXAttributeUsage>()
                .SelectMany(x => x.usedAttributes)
                .Where(x => this.attributesManager.IsCustom(x.name))
                .Select(x => x.name)
                .ToArray();

            return this.attributesManager.GetCustomAttributeNames().Except(nodesUsingCustomAttribute);
        }

        public VFXAttribute DuplicateCustomAttribute(string attributeName)
        {
            var newAttribute = m_AttributesManager.Duplicate(attributeName);
            var currentIndex = m_CustomAttributes.FindIndex(x => x.attributeName == attributeName);
            var order = currentIndex >= 0 ? currentIndex + 1 : m_CustomAttributes.Count;
            if (TryAddCustomAttribute(newAttribute.name, newAttribute.type, newAttribute.description, false, out var attribute))
            {
                SetCustomAttributeOrder(attribute.name, order);
            }

            return attribute;
        }

        public void RemoveCustomAttribute(string attributeName)
        {
            var existingAttribute = this.FindCustomAttribute(attributeName);
            if (existingAttribute != null)
            {
                foreach (var usage in GetCustomAttributeUsage(attributeName).ToArray())
                {
                    if (Selection.Contains(usage))
                        Selection.Remove(usage);
                    RemoveModel(usage);
                }

                m_AttributesManager.UnregisterCustomAttribute(attributeName);
                m_CustomAttributes.Remove(existingAttribute);

                Invalidate(this, InvalidationCause.kStructureChanged);
            }
        }

        public bool TryRenameCustomAttribute(string oldName, string newName)
        {
            var customAttributeDescriptor = FindCustomAttribute(oldName);

            var usingNodes = GetRecursiveChildren()
                .OfType<IVFXAttributeUsage>()
                .Where(x => x.usedAttributes.Any(x => string.Compare(x.name, oldName, StringComparison.OrdinalIgnoreCase) == 0))
                .ToArray();

            var result = this.m_AttributesManager.TryRename(oldName, newName);
            if (result == RenameStatus.Success)
            {
                customAttributeDescriptor.attributeName = newName;

                foreach (var customAttributeNode in usingNodes)
                {
                    customAttributeNode.Rename(oldName, newName);
                }

                Invalidate(this, InvalidationCause.kStructureChanged);
                return true;
            }

            // Already renamed
            if (result == RenameStatus.NotFound && FindCustomAttribute(newName) != null)
            {
                return true;
            }

            if (result == RenameStatus.NameUsed)
            {
                Debug.LogWarning("You are trying to rename a custom attribute with a name that is already used by another custom attribute");
            }
            return false;
        }

        public bool TryUpdateCustomAttribute(string attributeName, CustomAttributeUtility.Signature type, string description, bool? isReadOnly = null)
        {
            var customAttributeDescriptor = this.FindCustomAttribute(attributeName);
            if (this.attributesManager.TryUpdate(attributeName, type, description))
            {
                customAttributeDescriptor.type = type;
                customAttributeDescriptor.description = description;

                var usingNodes = GetRecursiveChildren()
                    .OfType<IVFXAttributeUsage>()
                    .Where(x => x.usedAttributes.Any(x => string.Compare(x.name, attributeName, StringComparison.OrdinalIgnoreCase) == 0));

                foreach (var node in usingNodes)
                {
                    ((VFXModel)node).Invalidate(InvalidationCause.kSettingChanged);
                }

                if (isReadOnly == false || (isReadOnly == null && !customAttributeDescriptor.isReadOnly)) // if not from subgraph
                    Invalidate(this, InvalidationCause.kStructureChanged);
                return true;
            }

            if (customAttributeDescriptor != null && isReadOnly.HasValue && isReadOnly.Value != customAttributeDescriptor.isReadOnly)
            {
                customAttributeDescriptor.isReadOnly = isReadOnly.Value;
                if (isReadOnly.Value)
                {
                    m_CustomAttributes.Remove(customAttributeDescriptor);
                    if (!m_DependenciesCustomAttributes.Contains(customAttributeDescriptor))
                    {
                        m_DependenciesCustomAttributes.Add(customAttributeDescriptor);
                    }
                }
                else
                {
                    if (!m_CustomAttributes.Contains(customAttributeDescriptor))
                    {
                        m_CustomAttributes.Add(customAttributeDescriptor);
                    }
                    m_DependenciesCustomAttributes.Remove(customAttributeDescriptor);
                }
            }

            return false;
        }

        public void SetCustomAttributeExpanded(string attributeName, bool isExpanded)
        {
            var customAttributeDescriptor = this.FindCustomAttribute(attributeName);
            customAttributeDescriptor.isExpanded = isExpanded;
        }

        public object Backup()
        {
            Profiler.BeginSample("VFXGraph.Backup");
            var dependencies = new HashSet<ScriptableObject>();

            dependencies.Add(this);
            CollectDependencies(dependencies);

            var result = VFXMemorySerializer.StoreObjectsToByteArray(dependencies.ToArray(), CompressionLevel.Fastest);

            Profiler.EndSample();

            return result;
        }

        public void Restore(object str)
        {
            Profiler.BeginSample("VFXGraph.Restore");
            var scriptableObject = VFXMemorySerializer.ExtractObjects(str as byte[], false);
            var graph = scriptableObject.OfType<VFXGraph>().Single();
            graph.SyncCustomAttributes();

            Profiler.BeginSample("VFXGraph.Restore SendUnknownChange");
            foreach (var model in scriptableObject.OfType<VFXModel>())
            {
                model.OnUnknownChange();
            }
            Profiler.EndSample();
            Profiler.EndSample();
            m_SystemNames.Sync(this);
            m_ExpressionGraphDirty = true;
            m_ExpressionValuesDirty = true;
            m_DependentDirty = true;
            SetCustomAttributeDirty();
        }

        public override void CollectDependencies(HashSet<ScriptableObject> objs, bool ownedOnly = true)
        {
            Profiler.BeginSample("VFXEditor.CollectDependencies");
            try
            {
                if (m_UIInfos != null)
                    objs.Add(m_UIInfos);
                m_CustomAttributes?.ForEach(x => { if (x != null) objs.Add(x); });

                base.CollectDependencies(objs, ownedOnly);
            }
            finally
            {
                Profiler.EndSample();
            }
        }

        static readonly ProfilerMarker k_ProfilerMarkerSanitizeGraph = new("VFXEditor.SanitizeGraph");
        public void SanitizeGraph()
        {
            if (m_GraphSanitized)
                return;

            using var profilerScope = k_ProfilerMarkerSanitizeGraph.Auto();

            var objs = new HashSet<ScriptableObject>();
            CollectDependencies(objs);

            if (version < 7)
            {
                SanitizeCameraBuffers(objs);
            }

            SyncCustomAttributes();
            foreach (var model in objs.OfType<VFXModel>())
            {
                try
                {
                    model.Sanitize(m_GraphVersion); // This can modify dependencies but newly created model are supposed safe so we dont care about retrieving new dependencies
                }
                catch (Exception e)
                {
                    Debug.LogError(string.Format("Exception while sanitizing model: {0} of type {1}: {2} {3}", model.name, model.GetType(), e, e.StackTrace));
                }
            }
            if (m_UIInfos != null)
                try
                {
                    m_UIInfos.Sanitize(this);
                }
                catch (Exception e)
                {
                    Debug.LogError(string.Format("Exception while sanitizing VFXUI: : {0} {1}", e, e.StackTrace));
                }

            systemNames.Sync(this);

            if (version < 11)
            {
                visualEffectResource.instancingMode = VFXInstancingMode.Disabled;
            }

            if (version < 14)
            {
                objs
                    .OfType<IVFXAttributeUsage>()
                    .SelectMany(x => x.usedAttributes)
                    .Where(x => m_AttributesManager.IsCustom(x.name))
                    .GroupBy(x => x.name)
                    .Select(x => x.First())
                    .Where(x => customAttributes.All(y => y.attributeName != x.name))
                    .ToList()
                    .ForEach(x => TryAddCustomAttribute(x.name, x.type, string.Empty, false, out _));
            }

            int resourceCurrentVersion = 0;
            // Stop using reflection after 2020.2;
            FieldInfo info = typeof(VisualEffectResource).GetField("CurrentVersion", BindingFlags.Static | System.Reflection.BindingFlags.Public);
            if (info != null)
                resourceCurrentVersion = (int)info.GetValue(null);

            if (m_ResourceVersion < resourceCurrentVersion) // Graph not up to date
            {
                if (m_ResourceVersion < 1) // Version before gradient interpreted as linear
                {
                    foreach (var model in objs.OfType<VFXSlotGradient>())
                    {
                        Gradient value = (Gradient)model.value;
                        GradientColorKey[] keys = value.colorKeys;

                        for (int i = 0; i < keys.Length; ++i)
                        {
                            var colorKey = keys[i];
                            colorKey.color = colorKey.color.linear;
                            keys[i] = colorKey;
                        }
                        value.colorKeys = keys;
                        model.value = new Gradient();
                        model.value = value;
                    }
                }
            }
            m_ResourceVersion = resourceCurrentVersion;
            m_GraphSanitized = true;
            m_GraphVersion = CurrentVersion;

            UpdateSubAssets(); //Force remove no more referenced object from the asset & *important* register as persistent new dependencies
        }

        internal void SyncContextLetters()
        {
            Dictionary<VFXData, List<VFXContext>> systems = new Dictionary<VFXData, List<VFXContext>>();

            var models = new HashSet<ScriptableObject>();
            CollectDependencies(models, false);
            var allContexts = models.OfType<VFXContext>();
            foreach (var context in allContexts)
            {
                var data = context.GetData();
                if (data != null)
                {
                    if (systems.TryGetValue(data, out var systemContexts))
                    {
                        systemContexts.Add(context);
                    }
                    else
                    {
                        systems[data] = new List<VFXContext>() { context };
                    }
                }
            }
            foreach (var system in systems)
            {
                VFXContextType type = VFXContextType.None;
                VFXContext prevContext = null;
                char letter = 'A';
                foreach (var context in system.Value.OrderBy(t => t.contextType))
                {
                    if (context.contextType == type)
                    {
                        if (prevContext != null)
                        {
                            letter = 'A';
                            prevContext.letter = letter;
                            prevContext = null;
                        }

                        if (letter == 'Z') // loop back to A in the unlikely event that there are more than 26 contexts
                            letter = 'a';
                        else if (letter == 'z')
                            letter = 'α';
                        else if (letter == 'ω')
                            letter = 'A';
                        context.letter = ++letter;
                    }
                    else
                    {
                        context.letter = '\0';
                        prevContext = context;
                    }
                    type = context.contextType;
                }
            }
        }

        private IEnumerable<VFXModel> GetCustomAttributeUsage(string attributeName)
        {
            bool IsAttributeUsed(IVFXAttributeUsage attributeUsage, string attrName)
            {
                return attributeUsage.usedAttributes.Any(x => string.Compare(x.name, attrName, StringComparison.OrdinalIgnoreCase) == 0);
            }

            foreach (var child in children.Where(x => x is IVFXAttributeUsage))
            {
                if (IsAttributeUsed((IVFXAttributeUsage)child, attributeName))
                    yield return child;
            }

            foreach (var context in children.OfType<VFXContext>())
            {
                foreach (var block in context.children)
                {
                    if (IsAttributeUsed(block, attributeName))
                        yield return block;
                }
            }
        }

        private VFXCustomAttributeDescriptor FindCustomAttribute(string attributeName)
        {
            return customAttributes.FirstOrDefault(x => string.Compare(attributeName, x.attributeName, StringComparison.OrdinalIgnoreCase) == 0);
        }

        private void SanitizeCameraBuffers(HashSet<ScriptableObject> objs)
        {
            List<Tuple<int, string, int, string>> links = new List<Tuple<int, string, int, string>>();
            var cameraSlots = objs.Where(obj => obj is VFXSlot && (obj as VFXSlot).value is CameraType).ToArray();
            for (int i = 0; i < cameraSlots.Length; ++i)
            {
                var cameraSlot = cameraSlots[i] as VFXSlot;

                var depthBufferSlot = cameraSlot.children.First(slot => slot.name == "depthBuffer");
                SanitizeCameraBufferLinks(depthBufferSlot, i, cameraSlots, links);

                var colorBufferSlot = cameraSlot.children.First(slot => slot.name == "colorBuffer");
                SanitizeCameraBufferLinks(colorBufferSlot, i, cameraSlots, links);

                objs.Remove(cameraSlots[i]);
                cameraSlots[i] = cameraSlot.Recreate();
                objs.Add(cameraSlots[i]);
            }
            foreach (var link in links)
            {
                var cameraSlotFrom = cameraSlots[link.Item1] as VFXSlot;
                var slotFrom = cameraSlotFrom.children.First(slot => slot.name == link.Item2);

                var cameraSlotTo = cameraSlots[link.Item3] as VFXSlot;
                var slotTo = cameraSlotTo.children.First(slot => slot.name == link.Item4);

                slotFrom.Link(slotTo);
            }
        }

        private void SanitizeCameraBufferLinks(VFXSlot slotFrom, int indexFrom, ScriptableObject[] cameraSlots, List<Tuple<int, string, int, string>> links)
        {
            if (slotFrom != null && !(slotFrom is VFXSlotCameraBuffer))
            {
                foreach (var slotTo in slotFrom.LinkedSlots)
                {
                    int indexTo = Array.IndexOf(cameraSlots, slotTo.GetMasterSlot());
                    if (indexTo >= 0)
                    {
                        links.Add(new Tuple<int, string, int, string>(indexFrom, slotFrom.name, indexTo, slotTo.name));
                    }
                }
            }
        }

        public void ClearCompileData()
        {
            m_CompiledData = null;


            m_ExpressionValuesDirty = true;
        }

        [SerializeField]
        List<string> m_ImportDependencies;

        public void UpdateSubAssets()
        {
            if (visualEffectResource == null)
                return;
            Profiler.BeginSample("VFXEditor.UpdateSubAssets");
            try
            {
                var currentObjects = new HashSet<ScriptableObject>();
                currentObjects.Add(this);
                CollectDependencies(currentObjects);

                visualEffectResource.SetContents(currentObjects.Cast<UnityObject>().ToArray());
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
            finally
            {
                Profiler.EndSample();
            }
        }

        protected override void OnInvalidate(VFXModel model, VFXModel.InvalidationCause cause)
        {
            if (cause == VFXModel.InvalidationCause.kStructureChanged
                || cause == VFXModel.InvalidationCause.kSettingChanged
                || cause == VFXModel.InvalidationCause.kConnectionChanged)
                m_SystemNames.Sync(this);

            base.OnInvalidate(model, cause);

            if (model is VFXParameter    //Something changed directly on VFXParameter (e.g. exposed state boolean)
                || model is VFXSlot && (model as VFXSlot).owner is VFXParameter //Something changed on a slot owned by a VFXParameter (e.g. the default value)
                || cause == VFXModel.InvalidationCause.kStructureChanged //A VFXParameter could have been removed
            )
            {
                BuildParameterInfo();
            }

            if (cause == VFXModel.InvalidationCause.kStructureChanged)
            {
                UpdateSubAssets();
                if (model == this)
                    VFXSubgraphContext.CallOnGraphChanged(this);

                m_DependentDirty = true;
            }

            if (cause == VFXModel.InvalidationCause.kSettingChanged && model is VFXParameter)
            {
                VFXSubgraphContext.CallOnGraphChanged(this);
                m_DependentDirty = true;
            }

            if ((cause == InvalidationCause.kStructureChanged ||
                cause == InvalidationCause.kParamChanged ||
                cause == InvalidationCause.kSettingChanged ||
                cause == InvalidationCause.kSpaceChanged ||
                cause == InvalidationCause.kConnectionChanged ||
                cause == InvalidationCause.kUIChanged) &&
                (model.hideFlags & HideFlags.DontSave) == 0)
            {
                EditorUtility.SetDirty(this);
            }

            if (cause == VFXModel.InvalidationCause.kExpressionGraphChanged)
            {
                m_ExpressionGraphDirty = true;
                m_DependentDirty = true;
            }

            if (cause == VFXModel.InvalidationCause.kParamChanged)
            {
                m_ExpressionValuesDirty = true;
                m_DependentDirty = true;
            }

            if (cause == VFXModel.InvalidationCause.kMaterialChanged)
            {
                m_MaterialsDirty = true;
            }
        }

        public uint FindReducedExpressionIndexFromSlotCPU(VFXSlot slot)
        {
            RecompileIfNeeded(false, true);
            return compiledData.FindReducedExpressionIndexFromSlotCPU(slot);
        }

        public void SetCompilationMode(VFXCompilationMode mode, bool reimport = true)
        {
            if (m_CompilationMode != mode && !GetResource().isSubgraph)
            {
                m_CompilationMode = mode;
                SetExpressionGraphDirty();
                if (reimport)
                    AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(this));
            }
        }

        public VFXCompilationMode GetCompilationMode()
        {
            return m_CompilationMode;
        }

        public void ForceShaderDebugSymbols(bool enable, bool reimport = true)
        {
            if (m_ForceShaderDebugSymbols != enable)
            {
                m_ForceShaderDebugSymbols = enable;
                if (reimport)
                    AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(this));
            }
        }

        public bool GetForceShaderDebugSymbols()
        {
            return m_ForceShaderDebugSymbols;
        }

        public void SetForceShaderValidation(bool forceShaderValidation, bool reimport = true)
        {
            if (m_ForceShaderValidation != forceShaderValidation)
            {
                m_ForceShaderValidation = forceShaderValidation;
                if (m_ForceShaderValidation)
                {
                    SetExpressionGraphDirty();
                    if (reimport)
                        AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(this));
                }
            }
        }

        public bool IsExpressionGraphDirty()
        {
            return m_ExpressionGraphDirty;
        }

        public void SetExpressionGraphDirty(bool dirty = true)
        {
            m_ExpressionGraphDirty = dirty;
            m_DependentDirty = dirty;
        }

        public void SetExpressionValueDirty()
        {
            m_ExpressionValuesDirty = true;
            m_DependentDirty = true;
        }

        public bool IsCustomAttributeDirty() => m_CustomAttributesDirty;
        public void SetCustomAttributeDirty(bool isDirty = true) => m_CustomAttributesDirty = isDirty;

        public void BuildSubgraphDependencies()
        {
            if (m_SubgraphDependencies == null)
                m_SubgraphDependencies = new List<VisualEffectObject>();
            else
                m_SubgraphDependencies.Clear();

            HashSet<VisualEffectObject> explored = new HashSet<VisualEffectObject>();
            RecurseBuildDependencies(explored, children);
        }

        void RecurseBuildDependencies(HashSet<VisualEffectObject> explored, IEnumerable<VFXModel> models)
        {
            foreach (var model in models)
            {
                if (model is VFXSubgraphContext)
                {
                    var subgraphContext = model as VFXSubgraphContext;

                    if (subgraphContext.subgraph != null && !explored.Contains(subgraphContext.subgraph))
                    {
                        explored.Add(subgraphContext.subgraph);
                        m_SubgraphDependencies.Add(subgraphContext.subgraph);
                        RecurseBuildDependencies(explored, subgraphContext.subgraph.GetResource().GetOrCreateGraph().children);
                    }
                }
                else if (model is VFXSubgraphOperator)
                {
                    var subgraphOperator = model as VFXSubgraphOperator;

                    if (subgraphOperator.subgraph != null && !explored.Contains(subgraphOperator.subgraph))
                    {
                        explored.Add(subgraphOperator.subgraph);
                        m_SubgraphDependencies.Add(subgraphOperator.subgraph);
                        RecurseBuildDependencies(explored, subgraphOperator.subgraph.GetResource().GetOrCreateGraph().children);
                    }
                }
                else if (model is VFXContext)
                {
                    foreach (var block in (model as VFXContext).children)
                    {
                        if (block is VFXSubgraphBlock)
                        {
                            var subgraphBlock = block as VFXSubgraphBlock;

                            if (subgraphBlock.subgraph != null && !explored.Contains(subgraphBlock.subgraph))
                            {
                                explored.Add(subgraphBlock.subgraph);
                                m_SubgraphDependencies.Add(subgraphBlock.subgraph);
                                RecurseBuildDependencies(explored, subgraphBlock.subgraph.GetResource().GetOrCreateGraph().children);
                            }
                        }
                    }
                }
            }
        }

        void RecurseSubgraphRecreateCopy(IEnumerable<VFXModel> children)
        {
            foreach (var child in children)
            {
                if (child is VFXSubgraphContext)
                {
                    var subgraphContext = child as VFXSubgraphContext;
                    subgraphContext.RecreateCopy();
                    if (subgraphContext.subgraph != null)
                    {
                        RecurseSubgraphRecreateCopy(subgraphContext.subChildren);
                    }
                }
                else if (child is VFXContext)
                {
                    foreach (var block in child.children)
                    {
                        if (block is VFXSubgraphBlock)
                        {
                            var subgraphBlock = block as VFXSubgraphBlock;
                            subgraphBlock.RecreateCopy();
                            if (subgraphBlock.subgraph != null)
                                RecurseSubgraphRecreateCopy(subgraphBlock.subChildren);
                        }
                    }
                }
                else if (child is VFXSubgraphOperator operatorChild)
                {
                    operatorChild.RecreateCopy();
                    if (operatorChild.ResyncSlots(true))
                        operatorChild.UpdateOutputExpressionsIfNeeded();
                }
            }
        }

        private void SetFlattenedParentToSubblocks()
        {
            foreach (var child in children.OfType<VFXContext>())
                foreach (var block in child.children.OfType<VFXSubgraphBlock>())
                    block.SetSubblocksFlattenedParent();
        }

        void RecurseSubgraphPatchInputExpression(IEnumerable<VFXModel> children)
        {
            foreach (var child in children)
            {
                if (child is VFXSubgraphContext)
                {
                    var subgraphContext = child as VFXSubgraphContext;
                    subgraphContext.PatchInputExpressions();
                }
                else if (child is VFXContext)
                {
                    foreach (var block in child.children)
                    {
                        if (block is VFXSubgraphBlock)
                        {
                            var subgraphBlock = block as VFXSubgraphBlock;
                            subgraphBlock.PatchInputExpressions();
                        }
                    }
                }
                else if (child is VFXSubgraphOperator operatorChild)
                {
                    operatorChild.ResyncSlots(false);
                    operatorChild.UpdateOutputExpressionsIfNeeded();
                }
            }
            foreach (var child in children)
            {
                if (child is VFXSubgraphContext)
                {
                    var subgraphContext = child as VFXSubgraphContext;
                    if (subgraphContext.subgraph != null && subgraphContext.subChildren != null)
                        RecurseSubgraphPatchInputExpression(subgraphContext.subChildren);
                }
                else if (child is VFXContext)
                {
                    foreach (var block in child.children)
                    {
                        if (block is VFXSubgraphBlock)
                        {
                            var subgraphBlock = block as VFXSubgraphBlock;
                            if (subgraphBlock.subgraph != null && subgraphBlock.subChildren != null)
                                RecurseSubgraphPatchInputExpression(subgraphBlock.subChildren);
                        }
                    }
                }
            }
        }

        void SubgraphDirty(VisualEffectObject subgraph)
        {
            if (m_SubgraphDependencies != null && m_SubgraphDependencies.Contains(subgraph))
            {
                PrepareSubgraphs();
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(this));
            }
        }

        private void PrepareSubgraphs()
        {
            Profiler.BeginSample("PrepareSubgraphs");
            RecurseSubgraphRecreateCopy(children);
            SetFlattenedParentToSubblocks();
            RecurseSubgraphPatchInputExpression(children);
            Profiler.EndSample();
        }

        IEnumerable<VFXGraph> GetAllGraphs<T>() where T : VisualEffectObject
        {
            var guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);

            foreach (var assetPath in guids.Select(t => AssetDatabase.GUIDToAssetPath(t)))
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
                if (asset != null)
                {
                    var graph = asset.GetResource().GetOrCreateGraph();
                    yield return graph;
                }
            }
        }

        //Explicit compile must be used if we want to force compilation even if a dependency is needed, which me must not do on a deleted library import.
        public static bool explicitCompile { get; set; } = false;
        //Set to true when restoring graph post compilation. Some costly behavior can be skipped in that situation (like reloading the whole UI). This is a safe hack.
        public static bool restoringGraph { get; set; } = false;

        public void SanitizeForImport()
        {
            // We arrive from AssetPostProcess so dependencies are already loaded no need to worry about them (FB #1364156)
            SyncCustomAttributes();
            foreach (var child in children)
                child.CheckGraphBeforeImport();

            SanitizeGraph();
        }

        public void CompileForImport()
        {
            bool isSubgraph = GetResource().isSubgraph;

            SyncCustomAttributes();
            if (!isSubgraph)
            {
                // Check Graph Before Import can be needed to synchronize modified shaderGraph
                foreach (var child in children)
                    child.CheckGraphBeforeImport();

                // Graph must have been sanitized at this point by the VFXGraphPreprocessor.OnPreprocess
                BuildSubgraphDependencies();
                PrepareSubgraphs();
                //Need to sync the context letters after PrepareSubgraphs because it recreates the subgraph's contexts
                SyncContextLetters();

                compiledData.Compile(m_CompilationMode, m_ForceShaderValidation, VFXViewPreference.generateShadersWithDebugSymbols || m_ForceShaderDebugSymbols, VFXAnalytics.GetInstance());
            }
            m_ExpressionGraphDirty = false;
            m_ExpressionValuesDirty = false;
        }

        public void RecompileIfNeeded(bool preventRecompilation = false, bool preventDependencyRecompilation = false)
        {
            SanitizeGraph();

            if (!GetResource().isSubgraph)
            {
                bool considerGraphDirty = m_ExpressionGraphDirty && !preventRecompilation;
                if (considerGraphDirty)
                {
                    BuildSubgraphDependencies();
                    PrepareSubgraphs();

                    compiledData.Compile(m_CompilationMode, m_ForceShaderValidation, VFXViewPreference.generateShadersWithDebugSymbols || m_ForceShaderDebugSymbols, VFXAnalytics.GetInstance());
                }
                else
                {
                    if (m_ExpressionValuesDirty && !m_ExpressionGraphDirty)
                        compiledData.UpdateValues();
                    if (m_MaterialsDirty && GetResource().asset != null)
                        UnityEngine.VFX.VFXManager.ResyncMaterials(GetResource().asset);
                }

                if (considerGraphDirty)
                    m_ExpressionGraphDirty = false;

                m_ExpressionValuesDirty = false;
                m_MaterialsDirty = false;
            }
            else if (m_ExpressionGraphDirty && !preventRecompilation)
            {
                BuildSubgraphDependencies();
                PrepareSubgraphs();
                m_ExpressionGraphDirty = false;
            }
            if (!preventDependencyRecompilation && m_DependentDirty)
            {
                var obj = GetResource().visualEffectObject;
                foreach (var graph in GetAllGraphs<VisualEffectAsset>())
                {
                    graph.SubgraphDirty(obj);
                }
                m_DependentDirty = false;
            }

            errorManager.GenerateErrors();
        }

        public void RegisterCompileError(string error, string description, VFXModel model)
        {
            errorManager.compileReporter.RegisterError(error, VFXErrorType.Error, description, model);
        }

        private VFXGraphCompiledData compiledData
        {
            get
            {
                if (m_CompiledData == null)
                    m_CompiledData = new VFXGraphCompiledData(this);
                return m_CompiledData;
            }
        }

        public bool sanitized { get { return m_GraphSanitized; } }

        public int version { get { return m_GraphVersion; } }

        [SerializeField]
        private int m_GraphVersion = CurrentVersion;

        [SerializeField]
        private int m_ResourceVersion;

        [NonSerialized]
        private bool m_GraphSanitized = false;
        private bool m_ExpressionGraphDirty = true;
        private bool m_ExpressionValuesDirty = true;
        private bool m_DependentDirty = true;
        private bool m_MaterialsDirty = false;
        private bool m_CustomAttributesDirty = false;

        private VFXGraphCompiledData m_CompiledData;

        private VFXCompilationMode m_CompilationMode = VFXCompilationMode.Runtime;
        private bool m_ForceShaderDebugSymbols = false;
        private bool m_ForceShaderValidation = false;

        [NonSerialized]
        public Action<VFXGraph> onRuntimeDataChanged;

        [SerializeField]
        private List<VisualEffectObject> m_SubgraphDependencies = new List<VisualEffectObject>();

        [SerializeField]
        private string m_CategoryPath;

        public string categoryPath
        {
            get { return m_CategoryPath; }
            set { m_CategoryPath = value; }//TODO invalidate cache here
        }

        public ReadOnlyCollection<VisualEffectObject> subgraphDependencies
        {
            get { return m_SubgraphDependencies.AsReadOnly(); }
        }

        public string[] UpdateImportDependencies()
        {
            visualEffectResource.ClearImportDependencies();

            var dependencies = new HashSet<int>();
            GetImportDependentAssets(dependencies);

            var guids = new HashSet<string>();
            foreach (var dependency in dependencies)
            {
                if (dependency == 0)
                    continue;

                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(dependency, out string guid, out long localId))
                {
                    if (!guids.Contains(guid))
                    {
                        guids.Add(guid);
                        visualEffectResource.AddImportDependency(guid);
                    }
                }
            }
            return guids.ToArray();
        }

        private VisualEffectResource m_Owner;
    }
}
