using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    [HasDependencies(typeof(MinimalSubGraphNode))]
    [Title("Utility", "Sub-graph")]
    class SubGraphNode : AbstractMaterialNode
        , IGeneratesBodyCode
        , IOnAssetEnabled
        , IGeneratesFunction
        , IMayRequireNormal
        , IMayRequireTangent
        , IMayRequireBitangent
        , IMayRequireMeshUV
        , IMayRequireScreenPosition
        , IMayRequireNDCPosition
        , IMayRequirePixelPosition
        , IMayRequireViewDirection
        , IMayRequirePosition
        , IMayRequirePositionPredisplacement
        , IMayRequireVertexColor
        , IMayRequireTime
        , IMayRequireFaceSign
        , IMayRequireCameraOpaqueTexture
        , IMayRequireDepthTexture
        , IMayRequireVertexSkinning
        , IMayRequireVertexID
    {
        [Serializable]
        public class MinimalSubGraphNode : IHasDependencies
        {
            [SerializeField]
            string m_SerializedSubGraph = string.Empty;

            public void GetSourceAssetDependencies(AssetCollection assetCollection)
            {
                var assetReference = JsonUtility.FromJson<SubGraphAssetReference>(m_SerializedSubGraph);
                string guidString = assetReference?.subGraph?.guid;
                if (!string.IsNullOrEmpty(guidString) && GUID.TryParse(guidString, out GUID guid))
                {
                    // subgraphs are read as artifacts
                    // they also should be pulled into .unitypackages
                    assetCollection.AddAssetDependency(
                        guid,
                        AssetCollection.Flags.ArtifactDependency |
                        AssetCollection.Flags.IsSubGraph |
                        AssetCollection.Flags.IncludeInExportPackage);
                }
            }
        }

        [Serializable]
        class SubGraphHelper
        {
            public SubGraphAsset subGraph;
        }

        [Serializable]
        class SubGraphAssetReference
        {
            public AssetReference subGraph = default;

            public override string ToString()
            {
                return $"subGraph={subGraph}";
            }
        }

        [Serializable]
        class AssetReference
        {
            public long fileID = default;
            public string guid = default;
            public int type = default;

            public override string ToString()
            {
                return $"fileID={fileID}, guid={guid}, type={type}";
            }
        }

        [SerializeField]
        string m_SerializedSubGraph = string.Empty;

        [NonSerialized]
        SubGraphAsset m_SubGraph; // This should not be accessed directly by most code -- use the asset property instead, and check for NULL! :)

        [SerializeField]
        List<string> m_PropertyGuids = new List<string>();

        [SerializeField]
        List<int> m_PropertyIds = new List<int>();

        [SerializeField]
        List<string> m_Dropdowns = new List<string>();

        [SerializeField]
        List<string> m_DropdownSelectedEntries = new List<string>();

        public string subGraphGuid
        {
            get
            {
                var assetReference = JsonUtility.FromJson<SubGraphAssetReference>(m_SerializedSubGraph);
                return assetReference?.subGraph?.guid;
            }
        }

        void LoadSubGraph()
        {
            if (m_SubGraph == null)
            {
                if (string.IsNullOrEmpty(m_SerializedSubGraph))
                {
                    return;
                }

                var graphGuid = subGraphGuid;
                var assetPath = AssetDatabase.GUIDToAssetPath(graphGuid);
                if (string.IsNullOrEmpty(assetPath))
                {
                    // this happens if the editor has never seen the GUID
                    // error will be printed by validation code in this case
                    return;
                }
                m_SubGraph = AssetDatabase.LoadAssetAtPath<SubGraphAsset>(assetPath);
                if (m_SubGraph == null)
                {
                    // this happens if the editor has seen the GUID, but the file has been deleted since then
                    // error will be printed by validation code in this case
                    return;
                }
                m_SubGraph.LoadGraphData();
                m_SubGraph.LoadDependencyData();

                name = m_SubGraph.name;
            }
        }

        public SubGraphAsset asset
        {
            get
            {
                LoadSubGraph();
                return m_SubGraph;
            }
            set
            {
                if (asset == value)
                    return;

                var helper = new SubGraphHelper();
                helper.subGraph = value;
                m_SerializedSubGraph = EditorJsonUtility.ToJson(helper, true);
                m_SubGraph = null;
                UpdateSlots();

                Dirty(ModificationScope.Topological);
            }
        }

        public override bool hasPreview
        {
            get { return true; }
        }

        public override PreviewMode previewMode
        {
            get
            {
                PreviewMode mode = m_PreviewMode;
                if ((mode == PreviewMode.Inherit) && (asset != null))
                    mode = asset.previewMode;
                return mode;
            }
        }

        public SubGraphNode()
        {
            name = "Sub Graph";
        }

        public override bool allowedInSubGraph
        {
            get { return true; }
        }

        public override bool canSetPrecision
        {
            get { return asset?.subGraphGraphPrecision == GraphPrecision.Graph; }
        }

        public override void GetInputSlots<T>(MaterialSlot startingSlot, List<T> foundSlots)
        {
            var allSlots = new List<T>();
            GetInputSlots<T>(allSlots);
            var info = asset?.GetOutputDependencies(startingSlot.RawDisplayName());
            if (info != null)
            {
                foreach (var slot in allSlots)
                {
                    if (info.ContainsSlot(slot))
                        foundSlots.Add(slot);
                }
            }
        }

        public override void GetOutputSlots<T>(MaterialSlot startingSlot, List<T> foundSlots)
        {
            var allSlots = new List<T>();
            GetOutputSlots<T>(allSlots);
            var info = asset?.GetInputDependencies(startingSlot.RawDisplayName());
            if (info != null)
            {
                foreach (var slot in allSlots)
                {
                    if (info.ContainsSlot(slot))
                        foundSlots.Add(slot);
                }
            }
        }

        ShaderStageCapability GetSlotCapability(MaterialSlot slot)
        {
            SlotDependencyInfo dependencyInfo;
            if (slot.isInputSlot)
                dependencyInfo = asset?.GetInputDependencies(slot.RawDisplayName());
            else
                dependencyInfo = asset?.GetOutputDependencies(slot.RawDisplayName());

            if (dependencyInfo != null)
                return dependencyInfo.capabilities;
            return ShaderStageCapability.All;
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            var outputGraphPrecision = asset?.outputGraphPrecision ?? GraphPrecision.Single;
            var outputPrecision = outputGraphPrecision.ToConcrete(concretePrecision);

            if (asset == null || hasError)
            {
                var outputSlots = new List<MaterialSlot>();
                GetOutputSlots(outputSlots);

                foreach (var slot in outputSlots)
                {
                    sb.AppendLine($"{slot.concreteValueType.ToShaderString(outputPrecision)} {GetVariableNameForSlot(slot.id)} = {slot.GetDefaultValue(GenerationMode.ForReals)};");
                }

                return;
            }

            var inputVariableName = $"_{GetVariableNameForNode()}";

            GenerationUtils.GenerateSurfaceInputTransferCode(sb, asset.requirements, asset.inputStructName, inputVariableName);

            // declare output variables
            foreach (var outSlot in asset.outputs)
                sb.AppendLine("{0} {1};", outSlot.concreteValueType.ToShaderString(outputPrecision), GetVariableNameForSlot(outSlot.id));

            var arguments = new List<string>();
            foreach (AbstractShaderProperty prop in asset.inputs)
            {
                // setup the property concrete precision (fallback to node concrete precision when it's switchable)
                prop.SetupConcretePrecision(this.concretePrecision);
                var inSlotId = m_PropertyIds[m_PropertyGuids.IndexOf(prop.guid.ToString())];
                arguments.Add(GetSlotValue(inSlotId, generationMode, prop.concretePrecision));

                if (prop.isConnectionTestable)
                    arguments.Add(IsSlotConnected(inSlotId) ? "true" : "false");
            }

            var dropdowns = asset.dropdowns;
            foreach (var dropdown in dropdowns)
            {
                var name = GetDropdownEntryName(dropdown.referenceName);
                if (dropdown.ContainsEntry(name))
                    arguments.Add(dropdown.IndexOfName(name).ToString());
                else
                    arguments.Add(dropdown.value.ToString());
            }

            // pass surface inputs through
            arguments.Add(inputVariableName);

            foreach (var outSlot in asset.outputs)
                arguments.Add(GetVariableNameForSlot(outSlot.id));

            foreach (var feedbackSlot in asset.vtFeedbackVariables)
            {
                string feedbackVar = GetVariableNameForNode() + "_" + feedbackSlot;
                sb.AppendLine("{0} {1};", ConcreteSlotValueType.Vector4.ToShaderString(ConcretePrecision.Single), feedbackVar);
                arguments.Add(feedbackVar);
            }

            sb.TryAppendIndentation();
            sb.Append(asset.functionName);
            sb.Append("(");
            bool firstArg = true;
            foreach (var arg in arguments)
            {
                if (!firstArg)
                    sb.Append(", ");
                firstArg = false;
                sb.Append(arg);
            }
            sb.Append(");");
            sb.AppendNewLine();
        }

        public void OnEnable()
        {
            UpdateSlots();
        }

        public bool Reload(HashSet<string> changedFileDependencyGUIDs)
        {
            if (!changedFileDependencyGUIDs.Contains(subGraphGuid))
            {
                return false;
            }

            if (asset == null)
            {
                // asset missing or deleted
                return true;
            }

            if (changedFileDependencyGUIDs.Contains(asset.assetGuid) || asset.descendents.Any(changedFileDependencyGUIDs.Contains))
            {
                m_SubGraph = null;
                UpdateSlots();

                if (hasError)
                {
                    return true;
                }

                owner.ClearErrorsForNode(this);
                ValidateNode();
                Dirty(ModificationScope.Graph);
            }

            return true;
        }

        public override void UpdatePrecision(List<MaterialSlot> inputSlots)
        {
            if (asset != null)
            {
                if (asset.subGraphGraphPrecision == GraphPrecision.Graph)
                {
                    // subgraph is defined to be switchable, so use the default behavior to determine precision
                    base.UpdatePrecision(inputSlots);
                }
                else
                {
                    // subgraph sets a specific precision, force that
                    graphPrecision = asset.subGraphGraphPrecision;
                    concretePrecision = graphPrecision.ToConcrete(owner.graphDefaultConcretePrecision);
                }
            }
            else
            {
                // no subgraph asset; use default behavior
                base.UpdatePrecision(inputSlots);
            }
        }

        public virtual void UpdateSlots()
        {
            var validNames = new List<int>();
            if (asset == null)
            {
                return;
            }

            var props = asset.inputs;
            var toFix = new HashSet<(SlotReference from, SlotReference to)>();
            foreach (var prop in props)
            {
                SlotValueType valueType = prop.concreteShaderValueType.ToSlotValueType();
                var propertyString = prop.guid.ToString();
                var propertyIndex = m_PropertyGuids.IndexOf(propertyString);
                if (propertyIndex < 0)
                {
                    propertyIndex = m_PropertyGuids.Count;
                    m_PropertyGuids.Add(propertyString);
                    m_PropertyIds.Add(prop.guid.GetHashCode());
                }
                var id = m_PropertyIds[propertyIndex];

                //for whatever reason, it seems like shader property ids changed between 21.2a17 and 21.2b1
                //tried tracking it down, couldnt find any reason for it, so we gotta fix it in post (after we deserialize)
                List<MaterialSlot> inputs = new List<MaterialSlot>();
                MaterialSlot found = null;
                GetInputSlots(inputs);
                foreach (var input in inputs)
                {
                    if (input.shaderOutputName == prop.referenceName && input.id != id)
                    {
                        found = input;
                        break;
                    }
                }

                MaterialSlot slot = MaterialSlot.CreateMaterialSlot(valueType, id, prop.displayName, prop.referenceName, SlotType.Input, Vector4.zero, ShaderStageCapability.All);

                // Copy defaults
                switch (prop.concreteShaderValueType)
                {
                    case ConcreteSlotValueType.SamplerState:
                    {
                        var tSlot = slot as SamplerStateMaterialSlot;
                        var tProp = prop as SamplerStateShaderProperty;
                        if (tSlot != null && tProp != null)
                            tSlot.defaultSamplerState = tProp.value;
                    }
                    break;
                    case ConcreteSlotValueType.Matrix4:
                    {
                        var tSlot = slot as Matrix4MaterialSlot;
                        var tProp = prop as Matrix4ShaderProperty;
                        if (tSlot != null && tProp != null)
                            tSlot.value = tProp.value;
                    }
                    break;
                    case ConcreteSlotValueType.Matrix3:
                    {
                        var tSlot = slot as Matrix3MaterialSlot;
                        var tProp = prop as Matrix3ShaderProperty;
                        if (tSlot != null && tProp != null)
                            tSlot.value = tProp.value;
                    }
                    break;
                    case ConcreteSlotValueType.Matrix2:
                    {
                        var tSlot = slot as Matrix2MaterialSlot;
                        var tProp = prop as Matrix2ShaderProperty;
                        if (tSlot != null && tProp != null)
                            tSlot.value = tProp.value;
                    }
                    break;
                    case ConcreteSlotValueType.Texture2D:
                    {
                        var tSlot = slot as Texture2DInputMaterialSlot;
                        var tProp = prop as Texture2DShaderProperty;
                        if (tSlot != null && tProp != null)

                            tSlot.texture = tProp.value.texture;
                    }
                    break;
                    case ConcreteSlotValueType.Texture2DArray:
                    {
                        var tSlot = slot as Texture2DArrayInputMaterialSlot;
                        var tProp = prop as Texture2DArrayShaderProperty;
                        if (tSlot != null && tProp != null)
                            tSlot.textureArray = tProp.value.textureArray;
                    }
                    break;
                    case ConcreteSlotValueType.Texture3D:
                    {
                        var tSlot = slot as Texture3DInputMaterialSlot;
                        var tProp = prop as Texture3DShaderProperty;
                        if (tSlot != null && tProp != null)
                            tSlot.texture = tProp.value.texture;
                    }
                    break;
                    case ConcreteSlotValueType.Cubemap:
                    {
                        var tSlot = slot as CubemapInputMaterialSlot;
                        var tProp = prop as CubemapShaderProperty;
                        if (tSlot != null && tProp != null)
                            tSlot.cubemap = tProp.value.cubemap;
                    }
                    break;
                    case ConcreteSlotValueType.Gradient:
                    {
                        var tSlot = slot as GradientInputMaterialSlot;
                        var tProp = prop as GradientShaderProperty;
                        if (tSlot != null && tProp != null)
                            tSlot.value = tProp.value;
                    }
                    break;
                    case ConcreteSlotValueType.Vector4:
                    {
                        var tSlot = slot as Vector4MaterialSlot;
                        var vector4Prop = prop as Vector4ShaderProperty;
                        var colorProp = prop as ColorShaderProperty;
                        if (tSlot != null && vector4Prop != null)
                            tSlot.value = vector4Prop.value;
                        else if (tSlot != null && colorProp != null)
                            tSlot.value = colorProp.value;
                    }
                    break;
                    case ConcreteSlotValueType.Vector3:
                    {
                        var tSlot = slot as Vector3MaterialSlot;
                        var tProp = prop as Vector3ShaderProperty;
                        if (tSlot != null && tProp != null)
                            tSlot.value = tProp.value;
                    }
                    break;
                    case ConcreteSlotValueType.Vector2:
                    {
                        var tSlot = slot as Vector2MaterialSlot;
                        var tProp = prop as Vector2ShaderProperty;
                        if (tSlot != null && tProp != null)
                            tSlot.value = tProp.value;
                    }
                    break;
                    case ConcreteSlotValueType.Vector1:
                    {
                        var tSlot = slot as Vector1MaterialSlot;
                        var tProp = prop as Vector1ShaderProperty;
                        if (tSlot != null && tProp != null)
                            tSlot.value = tProp.value;
                    }
                    break;
                    case ConcreteSlotValueType.Boolean:
                    {
                        var tSlot = slot as BooleanMaterialSlot;
                        var tProp = prop as BooleanShaderProperty;
                        if (tSlot != null && tProp != null)
                            tSlot.value = tProp.value;
                    }
                    break;
                }

                AddSlot(slot);
                validNames.Add(id);

                if (found != null)
                {
                    List<IEdge> edges = new List<IEdge>();
                    owner.GetEdges(found.slotReference, edges);
                    foreach (var edge in edges)
                    {
                        toFix.Add((edge.outputSlot, slot.slotReference));
                    }
                }
            }

            foreach (var slot in asset.outputs)
            {
                var outputStage = GetSlotCapability(slot);
                var newSlot = MaterialSlot.CreateMaterialSlot(slot.valueType, slot.id, slot.RawDisplayName(),
                    slot.shaderOutputName, SlotType.Output, Vector4.zero, outputStage, slot.hidden);
                AddSlot(newSlot);
                validNames.Add(slot.id);
            }

            RemoveSlotsNameNotMatching(validNames, true);

            // sort slot order to match subgraph property order
            SetSlotOrder(validNames);

            foreach (var (from, to) in toFix)
            {
                //for whatever reason, in this particular error fix, GraphView will incorrectly either add two edgeViews or none
                //but it does work correctly if we dont notify GraphView of this added edge. Gross.
                owner.UnnotifyAddedEdge(owner.Connect(from, to));
            }
        }

        void ValidateShaderStage()
        {
            if (asset != null)
            {
                List<MaterialSlot> slots = new List<MaterialSlot>();
                GetInputSlots(slots);
                GetOutputSlots(slots);

                foreach (MaterialSlot slot in slots)
                    slot.stageCapability = GetSlotCapability(slot);
            }
        }

        public override void ValidateNode()
        {
            base.ValidateNode();

            if (asset == null)
            {
                hasError = true;
                var assetGuid = subGraphGuid;
                var assetPath = string.IsNullOrEmpty(subGraphGuid) ? null : AssetDatabase.GUIDToAssetPath(assetGuid);
                if (string.IsNullOrEmpty(assetPath))
                {
                    owner.AddValidationError(objectId, $"Could not find Sub Graph asset with GUID {assetGuid}.");
                }
                else
                {
                    owner.AddValidationError(objectId, $"Could not load Sub Graph asset at \"{assetPath}\" with GUID {assetGuid}.");
                }

                return;
            }

            if (owner.isSubGraph && (asset.descendents.Contains(owner.assetGuid) || asset.assetGuid == owner.assetGuid))
            {
                hasError = true;
                owner.AddValidationError(objectId, $"Detected a recursion in Sub Graph asset at \"{AssetDatabase.GUIDToAssetPath(subGraphGuid)}\" with GUID {subGraphGuid}.");
            }
            else if (!asset.isValid)
            {
                hasError = true;
                owner.AddValidationError(objectId, $"Sub Graph has errors, asset at \"{AssetDatabase.GUIDToAssetPath(subGraphGuid)}\" with GUID {subGraphGuid}.");
            }
            else if (!owner.isSubGraph && owner.activeTargets.Any(x => asset.unsupportedTargets.Contains(x)))
            {
                SetOverrideActiveState(ActiveState.ExplicitInactive);
                owner.AddValidationError(objectId, $"Sub Graph contains nodes that are unsupported by the current active targets, asset at \"{AssetDatabase.GUIDToAssetPath(subGraphGuid)}\" with GUID {subGraphGuid}.");
            }

            // detect disconnected VT properties, and VT layer count mismatches
            foreach (var paramProp in asset.inputs)
            {
                if (paramProp is VirtualTextureShaderProperty vtProp)
                {
                    int paramLayerCount = vtProp.value.layers.Count;

                    var argSlotId = m_PropertyIds[m_PropertyGuids.IndexOf(paramProp.guid.ToString())];      // yikes
                    if (!IsSlotConnected(argSlotId))
                    {
                        owner.AddValidationError(objectId, $"A VirtualTexture property must be connected to the input slot \"{paramProp.displayName}\"");
                    }
                    else
                    {
                        var argProp = GetSlotProperty(argSlotId) as VirtualTextureShaderProperty;
                        if (argProp != null)
                        {
                            int argLayerCount = argProp.value.layers.Count;

                            if (argLayerCount != paramLayerCount)
                                owner.AddValidationError(objectId, $"Input \"{paramProp.displayName}\" has different number of layers from the connected property \"{argProp.displayName}\"");
                        }
                        else
                        {
                            owner.AddValidationError(objectId, $"Input \"{paramProp.displayName}\" is not connected to a valid VirtualTexture property");
                        }
                    }

                    break;
                }
            }

            ValidateShaderStage();
        }

        public override void CollectShaderProperties(PropertyCollector visitor, GenerationMode generationMode)
        {
            base.CollectShaderProperties(visitor, generationMode);

            if (asset == null)
                return;

            foreach (var property in asset.nodeProperties)
            {
                visitor.AddShaderProperty(property);
            }
        }

        public AbstractShaderProperty GetShaderProperty(int id)
        {
            var index = m_PropertyIds.IndexOf(id);
            if (index >= 0)
            {
                var guid = m_PropertyGuids[index];
                return asset?.inputs.Where(x => x.guid.ToString().Equals(guid)).FirstOrDefault();
            }
            return null;
        }

        public void CollectShaderKeywords(KeywordCollector keywords, GenerationMode generationMode)
        {
            if (asset == null)
                return;

            foreach (var keyword in asset.keywords)
            {
                keywords.AddShaderKeyword(keyword as ShaderKeyword);
            }
        }

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            base.CollectPreviewMaterialProperties(properties);

            if (asset == null)
                return;

            foreach (var property in asset.nodeProperties)
            {
                properties.Add(property.GetPreviewMaterialProperty());
            }
        }

        public virtual void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            if (asset == null || hasError)
                return;

            registry.RequiresIncludes(asset.includes);

            var graphData = registry.builder.currentNode.owner;
            var graphDefaultConcretePrecision = graphData.graphDefaultConcretePrecision;

            foreach (var function in asset.functions)
            {
                var name = function.key;
                var source = function.value;
                var graphPrecisionFlags = function.graphPrecisionFlags;

                // the subgraph may use multiple precision variants of this function internally
                // here we iterate through all the requested precisions and forward those requests out to the graph
                for (int requestedGraphPrecision = 0; requestedGraphPrecision <= (int)GraphPrecision.Half; requestedGraphPrecision++)
                {
                    // only provide requested precisions
                    if ((graphPrecisionFlags & (1 << requestedGraphPrecision)) != 0)
                    {
                        // when a function coming from a subgraph asset has a graph precision of "Graph",
                        // that means it is up to the subgraph NODE to decide (i.e. us!)
                        GraphPrecision actualGraphPrecision = (GraphPrecision)requestedGraphPrecision;

                        // subgraph asset setting falls back to this node setting (when switchable)
                        actualGraphPrecision = actualGraphPrecision.GraphFallback(this.graphPrecision);

                        // which falls back to the graph default concrete precision
                        ConcretePrecision actualConcretePrecision = actualGraphPrecision.ToConcrete(graphDefaultConcretePrecision);

                        // forward the function into the current graph
                        registry.ProvideFunction(name, actualGraphPrecision, actualConcretePrecision, sb => sb.AppendLines(source));
                    }
                }
            }
        }

        public NeededCoordinateSpace RequiresNormal(ShaderStageCapability stageCapability)
        {
            if (asset == null)
                return NeededCoordinateSpace.None;

            return asset.requirements.requiresNormal;
        }

        public bool RequiresMeshUV(UVChannel channel, ShaderStageCapability stageCapability)
        {
            if (asset == null)
                return false;

            return asset.requirements.requiresMeshUVs.Contains(channel);
        }

        public bool RequiresScreenPosition(ShaderStageCapability stageCapability)
        {
            if (asset == null)
                return false;

            return asset.requirements.requiresScreenPosition;
        }

        public bool RequiresNDCPosition(ShaderStageCapability stageCapability)
        {
            if (asset == null)
                return false;

            return asset.requirements.requiresNDCPosition;
        }

        public bool RequiresPixelPosition(ShaderStageCapability stageCapability)
        {
            if (asset == null)
                return false;

            return asset.requirements.requiresPixelPosition;
        }

        public NeededCoordinateSpace RequiresViewDirection(ShaderStageCapability stageCapability)
        {
            if (asset == null)
                return NeededCoordinateSpace.None;

            return asset.requirements.requiresViewDir;
        }

        public NeededCoordinateSpace RequiresPosition(ShaderStageCapability stageCapability)
        {
            if (asset == null)
                return NeededCoordinateSpace.None;

            return asset.requirements.requiresPosition;
        }

        public NeededCoordinateSpace RequiresPositionPredisplacement(ShaderStageCapability stageCapability = ShaderStageCapability.All)
        {
            if (asset == null)
                return NeededCoordinateSpace.None;

            return asset.requirements.requiresPositionPredisplacement;
        }

        public NeededCoordinateSpace RequiresTangent(ShaderStageCapability stageCapability)
        {
            if (asset == null)
                return NeededCoordinateSpace.None;

            return asset.requirements.requiresTangent;
        }

        public bool RequiresTime()
        {
            if (asset == null)
                return false;

            return asset.requirements.requiresTime;
        }

        public bool RequiresFaceSign(ShaderStageCapability stageCapability)
        {
            if (asset == null)
                return false;

            return asset.requirements.requiresFaceSign;
        }

        public NeededCoordinateSpace RequiresBitangent(ShaderStageCapability stageCapability)
        {
            if (asset == null)
                return NeededCoordinateSpace.None;

            return asset.requirements.requiresBitangent;
        }

        public bool RequiresVertexColor(ShaderStageCapability stageCapability)
        {
            if (asset == null)
                return false;

            return asset.requirements.requiresVertexColor;
        }

        public bool RequiresCameraOpaqueTexture(ShaderStageCapability stageCapability)
        {
            if (asset == null)
                return false;

            return asset.requirements.requiresCameraOpaqueTexture;
        }

        public bool RequiresDepthTexture(ShaderStageCapability stageCapability)
        {
            if (asset == null)
                return false;

            return asset.requirements.requiresDepthTexture;
        }

        public bool RequiresVertexSkinning(ShaderStageCapability stageCapability)
        {
            if (asset == null)
                return false;

            return asset.requirements.requiresVertexSkinning;
        }

        public bool RequiresVertexID(ShaderStageCapability stageCapability)
        {
            if (asset == null)
                return false;

            return asset.requirements.requiresVertexID;
        }

        public string GetDropdownEntryName(string referenceName)
        {
            var index = m_Dropdowns.IndexOf(referenceName);
            return index >= 0 ? m_DropdownSelectedEntries[index] : string.Empty;
        }

        public void SetDropdownEntryName(string referenceName, string value)
        {
            var index = m_Dropdowns.IndexOf(referenceName);
            if (index >= 0)
            {
                m_DropdownSelectedEntries[index] = value;
            }
            else
            {
                m_Dropdowns.Add(referenceName);
                m_DropdownSelectedEntries.Add(value);
            }
        }
    }
}
