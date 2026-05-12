//#define USE_SHADER_AS_SUBASSET
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Unity.Profiling;
using UnityEditor.AssetImporters;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.VFX.Block;
using UnityEditor.VFX.UI;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.VFX;
using Object = System.Object;
using UnityObject = UnityEngine.Object;

namespace UnityEditor.VFX
{
    [InitializeOnLoad]
    class VFXGraphPreprocessor : AssetPostprocessor
    {
        static bool IsVFXImportDependency(string importedAsset, GUID importedGuid = default)
        {
            if (VisualEffectAssetModificationProcessor.HasVFXExtension(importedAsset)
                || importedAsset.EndsWith(ShaderGraph.ShaderGraphImporter.Extension, StringComparison.OrdinalIgnoreCase)
                || importedAsset.EndsWith("pcache", StringComparison.OrdinalIgnoreCase)
                || importedAsset.EndsWith("hlsl", StringComparison.OrdinalIgnoreCase))
                return true;

            if (importedAsset.EndsWith("cs", StringComparison.OrdinalIgnoreCase))
            {
                if (importedGuid == default)
                {
                    if (CustomSpawnerVariant.SpawnerCallbacksPaths.Contains(importedAsset))
                        return true;
                }
                else
                {
                    if (CustomSpawnerVariant.SpawnerCallbacksGuids.Contains(importedGuid))
                        return true;
                }
            }

            return false;
        }

        static bool IsVFXImportSourceDependency(string importedAsset, GUID importedGuid = default)
        {
            return VisualEffectAssetModificationProcessor.IsVFXSubgraphExtension(importedAsset);
        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
#if VFX_HAS_TIMELINE
            UnityEditor.VFX.Migration.ActivationToControlTrack.SanitizePlayable(importedAssets);
#endif
            bool anyVfxRelatedAssetDeleted = false;
            HashSet<EntityId> vfxRelatedAssetModified = null;

            foreach (var asset in deletedAssets)
            {
                if (VisualEffectAssetModificationProcessor.HasVFXExtension(asset))
                {
                    VisualEffectResource.ForgetAtPath(asset);
                    anyVfxRelatedAssetDeleted = true;
                }
                else if (IsVFXImportDependency(asset))
                    anyVfxRelatedAssetDeleted = true;
            }

            if (!VFXViewWindow.HasAnyWindow())
                return; //Early return, all these updates are only relevant if a VFXViewWindow is opened for live edition         

            if (!anyVfxRelatedAssetDeleted)
            {
                foreach (var asset in importedAssets)
                {
                    if (IsVFXImportDependency(asset))
                    {
                        var mainObject = AssetDatabase.LoadMainAssetAtPath(asset);
                        if (mainObject)
                        {
                            vfxRelatedAssetModified ??= new();
                            vfxRelatedAssetModified.Add(mainObject.GetEntityId());
                            if (mainObject is Shader)
                            {
                                var shaderGraphVfxAsset = VFXShaderGraphHelpers.LoadShaderGraphAssetAtPath(asset);
                                if (shaderGraphVfxAsset)
                                    vfxRelatedAssetModified.Add(shaderGraphVfxAsset.GetEntityId());
                            }
                            else if (mainObject is VisualEffectAsset vfxAsset
                                     && vfxAsset.GetResource() is { } resource
                                     && resource.GetGraph() is { } graph)
                            {
                                vfxRelatedAssetModified.Add(graph.GetEntityId());
                            }
                        }
                    }
                }
            }

            if (!anyVfxRelatedAssetDeleted && vfxRelatedAssetModified == null)
                return;

            foreach (var window in VFXViewWindow.GetAllWindows())
            {
                window.UpdateHistory();
                var resource = window.displayedResource;
                if (resource != null)
                {
                    window.UpdateTitle(AssetDatabase.GetAssetPath(resource));
                    if (resource.GetGraph() is { } graph
                        && (anyVfxRelatedAssetDeleted || graph.IsDependentOnAnyOf(vfxRelatedAssetModified)))
                    {
                        graph.PrepareGraph();
                        if (!resource.isSubgraph
                            && window.autoCompile
                            && window.graphView != null
                            && window.graphView.isDisconnecting != true
                            && window.graphView.controller != null
                            && window.graphView.controller.graph == graph) //controller not connected or created yet
                        {
                            if (resource.asset != null)
                            {
                                //Not only CompileAndUpdateAsset to be sure UpdateAuthoringCompileData
                                window.graphView.Compile();
                            }
                            else
                            {
                                Debug.LogErrorFormat("VisualEffectGraphResource without asset : {0}", AssetDatabase.GetAssetPath(resource));
                            }
                        }
                    }

                    // Force blackboard update only when a subgraph gets re-imported
                    window.graphView?.blackboard.Update(true);
                }
            }
        }

        static GUID[] OnFilterImportDependencies(GUID[] externalGuids, string[] externalPaths, bool sourceOnly)
        {
            var importDependencies = new List<GUID>(externalGuids.Length);
            Func<string, GUID, bool> checkFunc = sourceOnly ? IsVFXImportSourceDependency : IsVFXImportDependency;

            for (int i = 0; i < externalGuids.Length; ++i)
            {
                var guid = externalGuids[i];
                var path = externalPaths[i];
                if (checkFunc(path, guid))
                    importDependencies.Add(guid);
            }
            return importDependencies.ToArray();
        }

        static VisualEffectAssetDesc OnCompileResource(VisualEffectResource resource, AssetImportContext context)
        {
            if (context == null)
                throw new NullReferenceException("Unexpected null import context");

            if (resource.isSubgraph)
                throw new InvalidOperationException("Unexpected invoke of OnCompileResource: " + resource.name);

            if (resource != null)
            {
                VFXGraph graph = resource.GetGraph();
                if (graph != null)
                {
                    if (VFXViewPreference.advancedLogs)
                        Debug.Log($"VfxGraph::CompileForImport {graph.GetEntityId()} {graph.name} {AssetDatabase.GetAssetPath(graph)}");

                    if (graph.GetCompilationMode() != VFXCompilationMode.Runtime)
                        throw new InvalidOperationException("Unexpected compilation mode, compilation mode isn't serialized and should always be runtime in OnCompileResource.");

                    graph.ForceShaderDebugSymbols(VFXViewPreference.generateShadersWithDebugSymbols);
                    graph.SetCompilationMode(VFXViewPreference.forceEditionCompilation ? VFXCompilationMode.Edition : VFXCompilationMode.Runtime);

                    graph.PrepareGraph();
                    graph.errorManager.RefreshCompilationReport();

                    bool instancingEnabled = resource.instancingMode != VFXInstancingMode.Disabled;
                    bool compileInitialVariant = resource.compileInitialVariants;

                    var generate = graph.GenerateVisualEffectAssetDesc(instancingEnabled, compileInitialVariant, context);
                    if (generate.previewShaders.Count > 0)
                        Debug.LogError("OnCompileResource error - Unexpected preview shaders generated with ImportContext available");
                    return generate.desc;
                }
                else
                    Debug.LogError("OnCompileResource error - VisualEffectResource without graph");
            }
            else
            {
                Debug.LogError("OnCompileResource error - VisualEffectResource null");
            }

            return default;
        }
        
        static VFXGraphPreprocessor()
        {
            VisualEffectResource.onFilterImportDependencies = OnFilterImportDependencies;
            VisualEffectResource.onEarlyGetAuthoringCompileData = VFXGraph.TryRetrieveVisualEffectAssetDescFromAuthoringToImport;
            VisualEffectResource.onCompileResource = OnCompileResource;
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

        public static void Build()
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
            foreach (var vfxObj in GetAllVisualEffectObjects())
            {
                if (VFXViewPreference.advancedLogs)
                    Debug.Log($"Sanitize VFX asset: {vfxObj.Key} ({vfxObj.Value})");

                var resource = VisualEffectResource.GetResourceAtPath(vfxObj.Value);
                if (resource != null)
                {
                    resource.GetGraph().PrepareGraph();
                    EditorUtility.SetDirty(resource);
                }
            }

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
        public static VFXGraph GetGraph(this VisualEffectResource resource)
        {
            var graph = resource.graph as VFXGraph;
            graph?.InternalSetOwner(resource);
            return graph;
        }

        public static VFXGraph CreateGraph(this VisualEffectResource resource)
        {
            var graph = ScriptableObject.CreateInstance<VFXGraph>();
            resource.graph = graph;
            graph.hideFlags |= HideFlags.HideInHierarchy;
            graph.InternalSetOwner(resource);
            // in this case we must update the subassets so that the graph is added to the resource dependencies
            graph.UpdateSubAssets();

            return graph;
        }

        public static void UpdateSubAssets(this VisualEffectResource resource)
        {
            resource.GetGraph().UpdateSubAssets();
        }

        public static void WriteAssetWithSubAssets(this VisualEffectResource resource)
        {
            var graph = resource.GetGraph();
            graph.UpdateSubAssets();
            resource.WriteAsset();
        }

        public static bool IsAssetEditable(this VisualEffectResource resource)
        {
            return AssetDatabase.IsOpenForEdit((UnityEngine.Object)resource.asset ?? resource.subgraph, StatusQueryOptions.UseCachedIfPossible);
        }

        public static void DestroyTransientResourceDeep(this VisualEffectResource resource)
        {
            if (VFXViewPreference.advancedLogs)
                Debug.Log($"VfxGraph::DestroyTransientResourceDeep {resource.GetEntityId()} {resource.name}");

            if (EditorUtility.IsPersistent(resource))
                throw new InvalidOperationException("Visual Effect Resource is persistent. This method only destroys transient resources");

            var graph = resource.GetGraph();
            if (graph != null) // It's possible other model have already been deleted, in that case just delete the resource copy
            {
                var preAllocatedSet = new HashSet<ScriptableObject> { graph };
                graph.CollectDependencies(preAllocatedSet);

                foreach (var obj in preAllocatedSet)
                    UnityObject.DestroyImmediate(obj);
            }

            UnityObject.DestroyImmediate(resource);
        }
    }

    static class VisualEffectObjectExtensions
    {
        public static VisualEffectResource GetResourceAtPathAndForget(this VisualEffectObject asset)
        {
            string assetPath = AssetDatabase.GetAssetPath(asset);
            VisualEffectResource resource = VisualEffectResource.GetResourceAtPathAndForget(assetPath);

            if (resource == null && !string.IsNullOrEmpty(assetPath))
                throw new NullReferenceException($"VFX resource does not exist for this asset at path: {assetPath}");

            resource.assetPathString = assetPath;

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
        // 19: Change sticky notes theme serialization
        public static readonly int CurrentVersion = 19;

        void OnDestroy()
        {
            ClearPreviewAssets();
        }

        public override void OnSRPChanged()
        {
            m_ExpressionGraphDirty = true;
        }

        public VisualEffectResource visualEffectResource => m_Owner;

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
            SetCustomAttributeDirty();
        }

        public override bool IsDependentOnAnyOf(HashSet<EntityId> dependencies)
        {
            if (dependencies.Contains(GetEntityId()))
                return true;

            return base.IsDependentOnAnyOf(dependencies);
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
            if (VFXViewPreference.advancedLogs)
                Debug.Log($"VfxGraph::SanitizeGraph {this.GetEntityId()} {name} {AssetDatabase.GetAssetPath(this)}");

            bool wasDirty = EditorUtility.IsDirty(this);

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
            m_GraphVersion = CurrentVersion;

            if (!wasDirty && EditorUtility.IsDirty(this))
                Debug.LogWarning($"{visualEffectResource.assetPathString} - The source graph was out of date and has been upgraded before compilation - Save the graph to store the changes in the source asset");

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
            }

            if ((cause == InvalidationCause.kStructureChanged ||
                cause == InvalidationCause.kParamChanged ||
                cause == InvalidationCause.kMaterialChanged ||
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
                flattenedParent?.Invalidate(model, cause); // propagate expression graph changes from subgraphs
            }

            if (cause == VFXModel.InvalidationCause.kParamChanged)
            {
                m_ExpressionValuesDirty = true;
            }

            if (cause == VFXModel.InvalidationCause.kMaterialChanged)
            {
                m_MaterialsDirty = true;
            }
        }

        public uint FindReducedExpressionIndexFromSlotCPU(VFXSlot slot)
        {
            RecompileIfNeeded(false);
            return compiledData.FindReducedExpressionIndexFromSlotCPU(slot);
        }

        public void SetCompilationMode(VFXCompilationMode mode)
        {
            if (m_CompilationMode != mode && !GetResource().isSubgraph)
            {
                m_CompilationMode = mode;
                SetExpressionGraphDirty();
            }
        }

        public VFXCompilationMode GetCompilationMode()
        {
            return m_CompilationMode;
        }

        public void ForceShaderDebugSymbols(bool enable)
        {
            if (m_ForceShaderDebugSymbols != enable)
            {
                m_ForceShaderDebugSymbols = enable;
                SetExpressionGraphDirty();
            }
        }

        public void SetForceShaderValidation(bool forceShaderValidation)
        {
            if (m_ForceShaderValidation != forceShaderValidation)
            {
                m_ForceShaderValidation = forceShaderValidation;
                SetExpressionGraphDirty();
            }
        }

        public bool IsExpressionGraphDirty()
        {
            return m_ExpressionGraphDirty;
        }

        public void SetExpressionGraphDirty(bool dirty = true)
        {
            m_ExpressionGraphDirty = dirty;
        }

        public void SetExpressionValueDirty()
        {
            m_ExpressionValuesDirty = true;
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
                        RecurseBuildDependencies(explored, subgraphContext.subgraph.GetResource().GetGraph().children);
                    }
                }
                else if (model is VFXSubgraphOperator)
                {
                    var subgraphOperator = model as VFXSubgraphOperator;

                    if (subgraphOperator.subgraph != null && !explored.Contains(subgraphOperator.subgraph))
                    {
                        explored.Add(subgraphOperator.subgraph);
                        m_SubgraphDependencies.Add(subgraphOperator.subgraph);
                        RecurseBuildDependencies(explored, subgraphOperator.subgraph.GetResource().GetGraph().children);
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
                                RecurseBuildDependencies(explored, subgraphBlock.subgraph.GetResource().GetGraph().children);
                            }
                        }
                    }
                }
            }
        }

        static void SubgraphRecreateCopyIfNeeded(IEnumerable<VFXModel> children, bool force = false)
        {
            foreach (var child in children)
            {
                if (child is VFXSubgraphContext)
                {
                    var subgraphContext = child as VFXSubgraphContext;
                    subgraphContext.RecreateCopyIfNeeded(force);
                }
                else if (child is VFXContext)
                {
                    foreach (var block in child.children)
                    {
                        if (block is VFXSubgraphBlock)
                        {
                            var subgraphBlock = block as VFXSubgraphBlock;
                            subgraphBlock.RecreateCopyIfNeeded(force);
                        }
                    }
                }
                else if (child is VFXSubgraphOperator operatorChild)
                {
                    operatorChild.RecreateCopyIfNeeded(force);
                    if (operatorChild.ResyncSlots(true))
                        operatorChild.UpdateOutputExpressionsIfNeeded();
                }
            }
        }

        public static void RecurseSubgraphPatchInputExpression(IEnumerable<VFXModel> children)
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
                                RecurseSubgraphPatchInputExpression(subgraphBlock.resourceCopy.GetGraph().children); // TODO TMP nested subblocks fix. Should clean subchildren
                        }
                    }
                }
            }
        }

        void PrepareSubgraphs()
        {
            if (VFXViewPreference.advancedLogs)
                Debug.Log($"VfxGraph::PrepareSubgraphs {this.GetEntityId()} {name} {AssetDatabase.GetAssetPath(this)}");

            Profiler.BeginSample("PrepareSubgraphs");
            SubgraphRecreateCopyIfNeeded(children, false);

            // redundant but ensures consistency
            VFXSubgraphUtility.SetSubgraphFlattenParentsDeep(this);
            RecurseSubgraphPatchInputExpression(children);

            Profiler.EndSample();
        }

        static readonly ProfilerMarker k_ProfilerMarkerPrepareGraph = new("VFXEditor.PrepareGraph");

        public void ResyncGraphDependencies()
        {
            foreach (var child in children)
                child.ResyncDependencies();
        }

        public void PrepareGraph()
        {
            using var scope = k_ProfilerMarkerPrepareGraph.Auto();

            if (VFXViewPreference.advancedLogs)
                Debug.Log($"VfxGraph::PrepareGraph {this.GetEntityId()} {name} {AssetDatabase.GetAssetPath(this)} {Environment.StackTrace}");

            // We arrive from AssetPostProcess so dependencies are already loaded no need to worry about them (FB #1364156)
            

            ResyncGraphDependencies();

            SanitizeGraph();

            BuildSubgraphDependencies();
            PrepareSubgraphs();

            SyncCustomAttributes();

            //Need to sync the context letters after PrepareSubgraphs because it recreates the subgraph's contexts
            systemNames.Sync(this);
            SyncContextLetters();
        }

        internal VFXGraphCompiledData.VFXCompileOutput Compile()
        {
            bool generateShadersDebugSymbols = VFXViewPreference.generateShadersWithDebugSymbols || m_ForceShaderDebugSymbols;
            if (VFXViewPreference.advancedLogs)
                Debug.Log($"VfxGraph::Compile {this.GetEntityId()} {AssetDatabase.GetAssetPath(this)} {m_CompilationMode}");
            if (VFXViewPreference.useNewCompiler)
                return m_NewCompiler.Compile(this, m_CompilationMode, generateShadersDebugSymbols);
            else
                return compiledData.Compile(m_CompilationMode, generateShadersDebugSymbols, VFXAnalytics.GetInstance());
        }

        private static System.Reflection.PropertyInfo kGetAllowLocking = typeof(Material).GetProperty("allowLocking", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        private List<UnityObject> m_PreviewAsset = new();
        private void ClearPreviewAssets()
        {
            foreach (var previewAsset in m_PreviewAsset)
            {
                if (!previewAsset.hideFlags.HasFlag(HideFlags.HideAndDontSave))
                    Debug.LogError("Unexpected preview asset: " + previewAsset);

                UnityObject.DestroyImmediate(previewAsset, true);
            }
            m_PreviewAsset.Clear();
        }

        class AuthoringBackupResult
        {
            public VFXGraphCompiledData.VFXCompileOutput output;
            public bool instancingEnabled;
            public bool initialVariant;
            public VFXCompilationMode mode;
        }

        //N.B.: This static won't survive with domain reload: with dirty vfx, entering playmode then save don't use the fast path
        static readonly Dictionary<GUID, AuthoringBackupResult> s_AuthoringCompilationOutput = new();
        public static bool TryRetrieveVisualEffectAssetDescFromAuthoringToImport(GUID guid, AssetImportContext ctx, out VisualEffectAssetDesc desc)
        {
            if (!s_AuthoringCompilationOutput.TryGetValue(guid, out var authoringBackup)
                || authoringBackup == null
                || !authoringBackup.output.success)
            {
                desc = default;
                return false;
            }

            desc = GenerateVisualEffectAssetDesc(authoringBackup.output, authoringBackup.instancingEnabled, authoringBackup.initialVariant, authoringBackup.mode, ctx, null);
            return true;
        }

        public static void RegisterAuthoringCompileData(GUID guid)
        {
            if (guid.Empty())
                throw new InvalidOperationException("Unexpected empty guid");

            if (!s_AuthoringCompilationOutput.TryAdd(guid, new AuthoringBackupResult()))
                Debug.LogError("Already registered authoring guid: " + guid);
        }

        public static void UpdateAuthoringCompileData(GUID guid, VFXGraphCompiledData.VFXCompileOutput output, bool instancingEnabled, bool initialVariant, VFXCompilationMode mode)
        {
            if (guid.Empty())
                throw new InvalidOperationException("Unexpected empty guid");

            if (!s_AuthoringCompilationOutput.TryGetValue(guid, out var authoringBackup) || authoringBackup == null)
                throw new InvalidOperationException("Not registered guid: " + guid);

            authoringBackup.output = output;
            authoringBackup.instancingEnabled = instancingEnabled;
            authoringBackup.initialVariant = initialVariant;
            authoringBackup.mode = mode;
        }

        public static void UpdateAuthoringValues(GUID guid, VFXExpressionValueContainerDesc[] expressionValues)
        {
            if (guid.Empty())
                throw new InvalidOperationException("Unexpected empty guid");

            if (!s_AuthoringCompilationOutput.TryGetValue(guid, out var authoringBackup) || authoringBackup == null)
                throw new InvalidOperationException("Not registered guid: " + guid);

            authoringBackup.output.assetDesc.sheet.values = expressionValues;
        }

        public static void UnregisterAuthoringCompileData(GUID guid)
        {
            if (guid.Empty())
                throw new InvalidOperationException("Unexpected empty guid");

            if (!s_AuthoringCompilationOutput.Remove(guid))
                Debug.LogError("Trying to remove unknown authoring guid: " + guid);
        }

        static void AddObjectToAsset(AssetImportContext ctx, UnityObject newObject, Dictionary<string, int> uniqueIdTracker)
        {
            var currentId = newObject.name;
            if (uniqueIdTracker.TryGetValue(currentId, out var count))
            {
                count++;
                uniqueIdTracker[currentId] = count;
                currentId = $"{currentId} ({count})"; //Prevent warning about "Identifier uniqueness violation"
            }
            else
            {
                uniqueIdTracker[currentId] = 0;
            }
            ctx.AddObjectToAsset(currentId, newObject);
        }

        internal (VisualEffectAssetDesc desc, VFXGraphCompiledData.VFXCompileOutput output, List<UnityObject> previewShaders) GenerateVisualEffectAssetDesc(bool instancingEnabled, bool compileInitialVariant, AssetImportContext ctx)
        {
            if (VFXViewPreference.advancedLogs)
                Debug.Log($"VfxGraph::GenerateVisualAssetDesc {this.GetEntityId()} {name} {AssetDatabase.GetAssetPath(this)}");

            var output = Compile();
            errorManager.GenerateErrors();

            var previewShaders = new List<UnityObject>();
            var desc = GenerateVisualEffectAssetDesc(output, instancingEnabled, compileInitialVariant, m_CompilationMode, ctx, previewShaders);
            return (desc, output, previewShaders);
        }

        static readonly ProfilerMarker k_GenerateVisualEffectAssetDescMaker = new("VFXEditor.GenerateVisualEffectAssetDescFromCompileOutput");
        static readonly ProfilerMarker k_CreateMaterialMaker = new("VFXEditor.CreateMaterial");

        static VisualEffectAssetDesc GenerateVisualEffectAssetDesc(VFXGraphCompiledData.VFXCompileOutput compilationOutput, bool instancingEnabled, bool compileInitialVariant, VFXCompilationMode compilationMode, AssetImportContext ctx, List<UnityObject> previewAsset)
        {
            using var globalAutoScope = k_GenerateVisualEffectAssetDescMaker.Auto();
            if (compilationOutput.success)
            {
                var overridenSystemDesc = new List<VFXEditorSystemDesc>();
                Dictionary<string, int> uniqueIdTracker = ctx != null ? new() : null;
                foreach (var system in compilationOutput.assetDesc.systemDesc)
                {
                    var overridenTask = new List<VFXEditorTaskDesc>();
                    if (system.tasks != null) foreach (var task in system.tasks)
                    {
                        UnityObject currentProcessor = null;
                        if (task.shaderSourceIndex >= 0)
                        {
                            var shaderSource = compilationOutput.assetDesc.shaderSourceDesc[task.shaderSourceIndex];
                            using var createShaderScope = new ProfilerMarker($"CreateShader ({shaderSource.name})").Auto();
                            if (shaderSource.compute)
                            {
                                currentProcessor = ctx == null ? ShaderUtil.CreateComputeShaderAsset(shaderSource.source) : ShaderUtil.CreateComputeShaderAsset(ctx, shaderSource.source);
                            }
                            else
                            {
                                currentProcessor = ctx == null ? ShaderUtil.CreateShaderAsset(shaderSource.source) : ShaderUtil.CreateShaderAsset(ctx, shaderSource.source, compileInitialVariant);
                            }
                            currentProcessor.name = shaderSource.name;
                            
                            if (ctx == null)
                            {
                                currentProcessor.hideFlags = HideFlags.HideAndDontSave;
                                previewAsset.Add(currentProcessor);
                            }
                            else
                            {
                                AddObjectToAsset(ctx, currentProcessor, uniqueIdTracker);
                            }
                        }
                        else if (task.processor is Shader || task.processor is MonoScript)
                        {
                            currentProcessor = task.processor;
                        }
                        else if (task.processor != null)
                        {
                            throw new InvalidOperationException("Unexpected processor type:" + task.processor.GetType());
                        }

                        if (currentProcessor != null && currentProcessor is Shader shader)
                        {
                            using var createMaterialScope = k_CreateMaterialMaker.Auto();
                            Material writableMaterial;

                            bool systemHasInstancing = instancingEnabled && system.flags.HasFlag(VFXSystemFlag.SystemUsesInstancedRendering);
                            if (!task.usesMaterialVariant)
                            {
                                writableMaterial = new Material(shader);
                                writableMaterial.name = shader.name;
                                writableMaterial.enableInstancing = systemHasInstancing;

                                if (ctx == null)
                                {
                                    writableMaterial.hideFlags = HideFlags.HideAndDontSave;
                                    previewAsset.Add(writableMaterial);
                                }
                                else
                                {
                                    AddObjectToAsset(ctx, writableMaterial, uniqueIdTracker);
                                }
                            }
                            else
                            {
                                var parentMaterial = new Material(shader);
                                
                                parentMaterial.name = shader.name + "_Parent";
                                parentMaterial.enableInstancing = systemHasInstancing;
                                parentMaterial.SetPropertyLock(1 << 2, true); //Matches MaterialSerializedProperty.EnableInstancingVariants

                                writableMaterial = new Material(parentMaterial)
                                {
                                    parent = parentMaterial,
                                };
                                writableMaterial.name = shader.name;
                                kGetAllowLocking.SetValue(writableMaterial, false);

                                if (ctx == null)
                                {
                                    parentMaterial.hideFlags = HideFlags.HideAndDontSave;
                                    writableMaterial.hideFlags = HideFlags.HideAndDontSave;
                                    previewAsset.Add(parentMaterial);
                                    previewAsset.Add(writableMaterial);
                                }
                                else
                                {
                                    AddObjectToAsset(ctx, parentMaterial, uniqueIdTracker);
                                    AddObjectToAsset(ctx, writableMaterial, uniqueIdTracker);
                                }
                            }

                            //Former OnSetupMaterial equivalent
                            var model = EditorUtility.EntityIdToObject(task.modelId);
                            if (model is IVFXSubRenderer subRenderer)
                            {
                                subRenderer.SetupMaterial(writableMaterial);
                            }

                            currentProcessor = writableMaterial;
                        }

                        var newTask = task;
                        newTask.processor = currentProcessor;
                        overridenTask.Add(newTask);
                    }

                    var newSystem = system;
                    if (system.tasks != null && system.tasks.Length != overridenTask.Count)
                        throw new InvalidOperationException("Unexpected copy of task");

                    newSystem.tasks = overridenTask.ToArray();
                    overridenSystemDesc.Add(newSystem);
                }

                if (compilationOutput.assetDesc.systemDesc.Length != overridenSystemDesc.Count)
                    throw new InvalidOperationException("Unexpected copy of system");

                if (ctx != null)
                {
                    foreach (var sourceDependency in compilationOutput.sourceDependencies)
                        ctx.DependsOnSourceAsset(sourceDependency);
                }

                var desc = new VisualEffectAssetDesc()
                {
                    sheet = compilationOutput.assetDesc.sheet,
                    systemDesc = overridenSystemDesc.ToArray(),
                    eventDesc = compilationOutput.assetDesc.eventDesc,
                    gpuBufferDesc = compilationOutput.assetDesc.gpuBufferDesc,
                    cpuBufferDesc = compilationOutput.assetDesc.cpuBufferDesc,
                    temporaryBufferDesc = compilationOutput.assetDesc.temporaryBufferDesc,
                    shaderSourceDesc = compilationOutput.assetDesc.shaderSourceDesc,
                    rendererSettings = compilationOutput.assetDesc.rendererSettings,
                    compilationMode = compilationMode,
                };

                return desc;
            }

            //LogError is already handled by "Unity cannot compile the VisualEffectAsset", only empty asset
            return new VisualEffectAssetDesc() { compilationMode = compilationMode };
        }

        internal (VFXGraphCompiledData.VFXCompileOutput output, bool instancingEnabled, bool initialVariant, VFXCompilationMode mode) CompileAndUpdateAsset(VisualEffectAsset asset)
        {
            if (VFXViewPreference.advancedLogs)
                Debug.Log($"VfxGraph::CompileAndUpdateAsset {this.GetEntityId()} {name} {AssetDatabase.GetAssetPath(this)}");

            bool instancingEnabled = asset.instancingMode != VFXInstancingMode.Disabled;
            bool initialVariant = asset.GetResource().compileInitialVariants;
            var generate = GenerateVisualEffectAssetDesc(instancingEnabled, initialVariant, null);

            ClearPreviewAssets(); //Must precede SetVisualEffectAssetDesc immediately, prevents crash from deleted asset (See MainThreadCleanUp)
            VisualEffectAssetUtility.SetVisualEffectAssetDesc(asset, generate.desc);
            m_PreviewAsset = generate.previewShaders;

            return (generate.output, instancingEnabled, initialVariant, m_CompilationMode);
        }

        public VFXGraphCompiledData.VFXCompileOutput RecompileIfNeeded(bool preventRecompilation = false)
        {
            var output = new VFXGraphCompiledData.VFXCompileOutput
            {
                success = false
            };

            if (!GetResource().isSubgraph)
            {
                bool considerGraphDirty = m_ExpressionGraphDirty && !preventRecompilation;
                if (considerGraphDirty)
                {
                    BuildSubgraphDependencies();
                    PrepareSubgraphs();
                    output = Compile();
                }
                else
                {
                    if (m_ExpressionValuesDirty && !m_ExpressionGraphDirty)
                        output.assetDesc.sheet.values = compiledData.UpdateValues();
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

            errorManager.GenerateErrors();
            return output;
        }

        public void RegisterCompileError(string error, string description, VFXModel model)
        {
            errorManager.compileReporter.RegisterError(error, VFXErrorType.Error, description, model);
        }

        public override void OnEnable()
        {
            base.OnEnable();
            m_CompiledData = null;
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

        public int version { get { return m_GraphVersion; } }

        [SerializeField]
        private int m_GraphVersion = CurrentVersion;

        [SerializeField]
        private int m_ResourceVersion;

        private bool m_ExpressionGraphDirty = true;
        private bool m_ExpressionValuesDirty = true;
        private bool m_MaterialsDirty = false;
        private bool m_CustomAttributesDirty = false;

        [NonSerialized]
        private VFXGraphCompiledData m_CompiledData;
        private VfxGraphCompiler m_NewCompiler = new();

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

        private VisualEffectResource m_Owner;

        internal void InternalSetOwner(VisualEffectResource resource)
        {
            m_Owner = resource;
        }
    }
}
