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
        , IMayRequireViewDirection
        , IMayRequirePosition
        , IMayRequireVertexColor
        , IMayRequireTime
        , IMayRequireFaceSign
        , IMayRequireCameraOpaqueTexture
        , IMayRequireDepthTexture
    {
        [Serializable]
        public class MinimalSubGraphNode : IHasDependencies
        {
            [SerializeField]
            string m_SerializedSubGraph = string.Empty;

            public void GetSourceAssetDependencies(List<string> paths)
            {
                var assetReference = JsonUtility.FromJson<SubGraphAssetReference>(m_SerializedSubGraph);
                var guid = assetReference?.subGraph?.guid;
                if (guid != null)
                {
                    paths.Add(AssetDatabase.GUIDToAssetPath(guid));
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
        SubGraphAsset m_SubGraph;

        [SerializeField]
        List<string> m_PropertyGuids = new List<string>();

        [SerializeField]
        List<int> m_PropertyIds = new List<int>();

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
                m_SubGraph = AssetDatabase.LoadAssetAtPath<SubGraphAsset>(assetPath);
                if (m_SubGraph == null)
                {
                    return;
                }

                name = m_SubGraph.name;
                concretePrecision = m_SubGraph.outputPrecision;
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
            get { return asset != null; }
        }

        public override PreviewMode previewMode
        {
            get
            {
                if (asset == null)
                    return PreviewMode.Preview2D;

                return PreviewMode.Preview3D;
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
            get { return false; }
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            if (asset == null || hasError)
            {
                var outputSlots = new List<MaterialSlot>();
                GetOutputSlots(outputSlots);
                var outputPrecision = asset != null ? asset.outputPrecision : ConcretePrecision.Float;
                foreach (var slot in outputSlots)
                {
                    sb.AppendLine($"{slot.concreteValueType.ToShaderString(outputPrecision)} {GetVariableNameForSlot(slot.id)} = {slot.GetDefaultValue(GenerationMode.ForReals)};");
                }

                return;
            }

            var inputVariableName = $"_{GetVariableNameForNode()}";

            GenerationUtils.GenerateSurfaceInputTransferCode(sb, asset.requirements, asset.inputStructName, inputVariableName);

            foreach (var outSlot in asset.outputs)
                sb.AppendLine("{0} {1};", outSlot.concreteValueType.ToShaderString(asset.outputPrecision), GetVariableNameForSlot(outSlot.id));

            var arguments = new List<string>();
            foreach (var prop in asset.inputs)
            {
                prop.ValidateConcretePrecision(asset.graphPrecision);
                var inSlotId = m_PropertyIds[m_PropertyGuids.IndexOf(prop.guid.ToString())];

                switch(prop)
                {
                    case Texture2DShaderProperty texture2DProp:
                        arguments.Add(string.Format("TEXTURE2D_ARGS({0}, sampler{0}), {0}_TexelSize", GetSlotValue(inSlotId, generationMode, prop.concretePrecision)));
                        break;
                    case Texture2DArrayShaderProperty texture2DArrayProp:
                        arguments.Add(string.Format("TEXTURE2D_ARRAY_ARGS({0}, sampler{0})", GetSlotValue(inSlotId, generationMode, prop.concretePrecision)));
                        break;
                    case Texture3DShaderProperty texture3DProp:
                        arguments.Add(string.Format("TEXTURE3D_ARGS({0}, sampler{0})", GetSlotValue(inSlotId, generationMode, prop.concretePrecision)));
                        break;
                    case CubemapShaderProperty cubemapProp:
                        arguments.Add(string.Format("TEXTURECUBE_ARGS({0}, sampler{0})", GetSlotValue(inSlotId, generationMode, prop.concretePrecision)));
                        break;
                    default:
                        arguments.Add(string.Format("{0}", GetSlotValue(inSlotId, generationMode, prop.concretePrecision)));
                        break;
                }
            }

            // pass surface inputs through
            arguments.Add(inputVariableName);

            foreach (var outSlot in asset.outputs)
                arguments.Add(GetVariableNameForSlot(outSlot.id));

            sb.AppendLine("{0}({1});", asset.functionName, arguments.Aggregate((current, next) => string.Format("{0}, {1}", current, next)));
        }

        public void OnEnable()
        {
            UpdateSlots();
        }

        public void Reload(HashSet<string> changedFileDependencies)
        {
            if (asset == null)
            {
                return;
            }

            if (changedFileDependencies.Contains(asset.assetGuid) || asset.descendents.Any(changedFileDependencies.Contains))
            {
                m_SubGraph = null;
                UpdateSlots();

                if (hasError)
                {
                    return;
                }

                owner.ClearErrorsForNode(this);
                ValidateNode();
                Dirty(ModificationScope.Graph);
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
                MaterialSlot slot = MaterialSlot.CreateMaterialSlot(valueType, id, prop.displayName, prop.referenceName, SlotType.Input, Vector4.zero, ShaderStageCapability.All);

                // Copy defaults
                switch(prop.concreteShaderValueType)
                {
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
            }

            var outputStage = asset.effectiveShaderStage;

            foreach (var slot in asset.outputs)
            {
                AddSlot(MaterialSlot.CreateMaterialSlot(slot.valueType, slot.id, slot.RawDisplayName(),
                    slot.shaderOutputName, SlotType.Output, Vector4.zero, outputStage));
                validNames.Add(slot.id);
            }

            RemoveSlotsNameNotMatching(validNames, true);
        }

        void ValidateShaderStage()
        {
            if (asset != null)
            {
                List<MaterialSlot> slots = new List<MaterialSlot>();
                GetInputSlots(slots);
                GetOutputSlots(slots);

                var outputStage = asset.effectiveShaderStage;
                foreach (MaterialSlot slot in slots)
                    slot.stageCapability = outputStage;
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
                    owner.AddValidationError(guid, $"Could not find Sub Graph asset with GUID {assetGuid}.");
                }
                else
                {
                    owner.AddValidationError(guid, $"Could not load Sub Graph asset at \"{assetPath}\" with GUID {assetGuid}.");
                }

                return;
            }

            if (asset.isRecursive || owner.isSubGraph && (asset.descendents.Contains(owner.assetGuid) || asset.assetGuid == owner.assetGuid))
            {
                hasError = true;
                owner.AddValidationError(guid, $"Detected a recursion in Sub Graph asset at \"{AssetDatabase.GUIDToAssetPath(subGraphGuid)}\" with GUID {subGraphGuid}.");
            }
            else if (!asset.isValid)
            {
                hasError = true;
                owner.AddValidationError(guid, $"Invalid Sub Graph asset at \"{AssetDatabase.GUIDToAssetPath(subGraphGuid)}\" with GUID {subGraphGuid}.");
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

            foreach (var function in asset.functions)
            {
                registry.ProvideFunction(function.key, s =>
                {
                    s.AppendLines(function.value);
                });
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
    }
}
