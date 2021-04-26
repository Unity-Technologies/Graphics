using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.ShaderGraph.Serialization;

namespace UnityEditor.ShaderGraph
{
    // temp -- until we have a better way to declare external inputs as default values to function
    internal enum Binding
    {
        None,
        ObjectSpaceNormal,
        ObjectSpaceTangent,
        ObjectSpaceBitangent,
        ObjectSpacePosition,
        ViewSpaceNormal,
        ViewSpaceTangent,
        ViewSpaceBitangent,
        ViewSpacePosition,
        WorldSpaceNormal,
        WorldSpaceTangent,
        WorldSpaceBitangent,
        WorldSpacePosition,
        TangentSpaceNormal,
        TangentSpaceTangent,
        TangentSpaceBitangent,
        TangentSpacePosition,
        MeshUV0,
        MeshUV1,
        MeshUV2,
        MeshUV3,
        ScreenPosition,
        ObjectSpaceViewDirection,
        ViewSpaceViewDirection,
        WorldSpaceViewDirection,
        TangentSpaceViewDirection,
        VertexColor,
    }

    interface ISandboxNodeDefinition
    {
        void BuildRuntime(ISandboxNodeBuildContext context);
    }

    abstract class SandboxNode<DEF> : AbstractMaterialNode
        , IGeneratesBodyCode
        , IGeneratesFunction
        , IMayRequireNormal
        , IMayRequireTangent
        , IMayRequireBitangent
        , IMayRequireMeshUV
        , IMayRequireScreenPosition
        , IMayRequireViewDirection
        , IMayRequirePosition
        , IMayRequireVertexColor
        , ISandboxNodeBuildContext
        where DEF : JsonObject, ISandboxNodeDefinition, new()
    {
        [SerializeField]
        DEF m_Definition;

        // runtime data (not serialized)
        ShaderFunction mainFunction;
        ShaderFunction previewFunction;
        List<MaterialSlot> definitionSlots = new List<MaterialSlot>();

        #region ISandboxNodeBuildContext
        void ClearRuntimeData()
        {
            mainFunction = null;
            definitionSlots.Clear();
        }

        void ISandboxNodeBuildContext.SetName(string name)
        {
            this.name = name;
        }

        SandboxValueType ISandboxNodeBuildContext.AddType(SandboxValueTypeDefinition typeDef)
        {
            throw new NotImplementedException();
        }

        SandboxValueType ISandboxNodeBuildContext.GetInputType(string pinName)
        {
            throw new NotImplementedException();
        }

        void ISandboxNodeBuildContext.SetMainFunction(ShaderFunction function, bool declareStaticPins)
        {
            mainFunction = function;
            if (declareStaticPins)
            {
                foreach (ShaderFunction.Parameter p in function.Parameters)
                {
                    int slotId = definitionSlots.Count;

                    MaterialSlot s;
/*
                    if (attribute.binding == Binding.None && !par.IsOut && par.ParameterType == typeof(Color))
                        s = new ColorRGBAMaterialSlot(attribute.slotId, name, par.Name, SlotType.Input, attribute.defaultValue ?? Vector4.zero, stageCapability: attribute.stageCapability, hidden: attribute.hidden);
                    else if (attribute.binding == Binding.None && !par.IsOut && par.ParameterType == typeof(ColorRGBA))
                        s = new ColorRGBAMaterialSlot(attribute.slotId, name, par.Name, SlotType.Input, attribute.defaultValue ?? Vector4.zero, stageCapability: attribute.stageCapability, hidden: attribute.hidden);
                    else if (attribute.binding == Binding.None && !par.IsOut && par.ParameterType == typeof(ColorRGB))
                        s = new ColorRGBMaterialSlot(attribute.slotId, name, par.Name, SlotType.Input, attribute.defaultValue ?? Vector4.zero, ColorMode.Default, stageCapability: attribute.stageCapability, hidden: attribute.hidden);
                    else if (attribute.binding == Binding.None || par.IsOut)
*/
                    // we assume strings are for external input bindings
                    if (p.DefaultValue is Binding binding)
                    {
                        s = CreateBoundSlot(binding, slotId, p.Name, p.Name, ShaderStageCapability.All
                            // attribute.stageCapability, attribute.hidden      // TODO
                        );
                    }
                    else
                    {
                        // hack massage defaults into old system
                        Vector4 defaultValue = Vector4.zero;
                        switch (p.DefaultValue)
                        {
                            case float f:
                                defaultValue = new Vector4(f, f, f, f);
                                break;
                            case Vector2 vec2:
                                defaultValue = new Vector4(vec2.x, vec2.y, 0.0f, 0.0f);
                                break;
                            case Vector3 vec3:
                                defaultValue = new Vector4(vec3.x, vec3.y, vec3.z, 0.0f);
                                break;
                            case Vector4 vec4:
                                defaultValue = vec4;
                                break;
                        }

                        s = MaterialSlot.CreateMaterialSlot(
                            ConvertSandboxValueTypeToSlotValueType(p.Type),
                            slotId,
                            p.Name,
                            p.Name,
                            p.IsOutput ? SlotType.Output : SlotType.Input,
                            defaultValue
                            // shaderStageCapability: attribute.stageCapability,        // TODO: ability to tag stage capabilities
                            // hidden: attribute.hidden                                 // TODO: what's this used for?
                        );
                    }

                    /*                    else          // TODO: bound default slots

                    */

                    definitionSlots.Add(s);
                }
            }
        }

        void ISandboxNodeBuildContext.SetPreviewFunction(ShaderFunction function)
        {
            previewFunction = function;
        }

        // TODO: move to static utils?

        private static MaterialSlot CreateBoundSlot(Binding binding, int slotId, string displayName, string shaderOutputName, ShaderStageCapability shaderStageCapability, bool hidden = false)
        {
            switch (binding)
            {
                case Binding.ObjectSpaceNormal:
                    return new NormalMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.Object, shaderStageCapability, hidden);
                case Binding.ObjectSpaceTangent:
                    return new TangentMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.Object, shaderStageCapability, hidden);
                case Binding.ObjectSpaceBitangent:
                    return new BitangentMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.Object, shaderStageCapability, hidden);
                case Binding.ObjectSpacePosition:
                    return new PositionMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.Object, shaderStageCapability, hidden);
                case Binding.ViewSpaceNormal:
                    return new NormalMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.View, shaderStageCapability, hidden);
                case Binding.ViewSpaceTangent:
                    return new TangentMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.View, shaderStageCapability, hidden);
                case Binding.ViewSpaceBitangent:
                    return new BitangentMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.View, shaderStageCapability, hidden);
                case Binding.ViewSpacePosition:
                    return new PositionMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.View, shaderStageCapability, hidden);
                case Binding.WorldSpaceNormal:
                    return new NormalMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.World, shaderStageCapability, hidden);
                case Binding.WorldSpaceTangent:
                    return new TangentMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.World, shaderStageCapability, hidden);
                case Binding.WorldSpaceBitangent:
                    return new BitangentMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.World, shaderStageCapability, hidden);
                case Binding.WorldSpacePosition:
                    return new PositionMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.World, shaderStageCapability, hidden);
                case Binding.TangentSpaceNormal:
                    return new NormalMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.Tangent, shaderStageCapability, hidden);
                case Binding.TangentSpaceTangent:
                    return new TangentMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.Tangent, shaderStageCapability, hidden);
                case Binding.TangentSpaceBitangent:
                    return new BitangentMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.Tangent, shaderStageCapability, hidden);
                case Binding.TangentSpacePosition:
                    return new PositionMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.Tangent, shaderStageCapability, hidden);
                case Binding.MeshUV0:
                    return new UVMaterialSlot(slotId, displayName, shaderOutputName, UVChannel.UV0, shaderStageCapability, hidden);
                case Binding.MeshUV1:
                    return new UVMaterialSlot(slotId, displayName, shaderOutputName, UVChannel.UV1, shaderStageCapability, hidden);
                case Binding.MeshUV2:
                    return new UVMaterialSlot(slotId, displayName, shaderOutputName, UVChannel.UV2, shaderStageCapability, hidden);
                case Binding.MeshUV3:
                    return new UVMaterialSlot(slotId, displayName, shaderOutputName, UVChannel.UV3, shaderStageCapability, hidden);
                case Binding.ScreenPosition:
                    return new ScreenPositionMaterialSlot(slotId, displayName, shaderOutputName, ScreenSpaceType.Default, shaderStageCapability, hidden);
                case Binding.ObjectSpaceViewDirection:
                    return new ViewDirectionMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.Object, shaderStageCapability, hidden);
                case Binding.ViewSpaceViewDirection:
                    return new ViewDirectionMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.View, shaderStageCapability, hidden);
                case Binding.WorldSpaceViewDirection:
                    return new ViewDirectionMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.World, shaderStageCapability, hidden);
                case Binding.TangentSpaceViewDirection:
                    return new ViewDirectionMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.Tangent, shaderStageCapability, hidden);
                case Binding.VertexColor:
                    return new VertexColorMaterialSlot(slotId, displayName, shaderOutputName, shaderStageCapability, hidden);
                default:
                    throw new ArgumentOutOfRangeException("binding", binding, null);
            }
        }

        public static SlotValueType ConvertSandboxValueTypeToSlotValueType(SandboxValueType type)
        {
            if (type == Types._bool)
            {
                return SlotValueType.Boolean;
            }
            if ((type == Types._float) || (type == Types._half) || (type == Types._precision))
            {
                return SlotValueType.Vector1;
            }
            if ((type == Types._float2) || (type == Types._half2) || (type == Types._precision2))
            {
                return SlotValueType.Vector2;
            }
            if ((type == Types._float3) || (type == Types._half3) || (type == Types._precision3))
            {
                return SlotValueType.Vector3;
            }
            if ((type == Types._float4) || (type == Types._half4) || (type == Types._precision4))
            {
                return SlotValueType.Vector4;
            }
/*            if (t == typeof(Color))
            {
                return SlotValueType.Vector4;
            }
            if (t == typeof(ColorRGBA))
            {
                return SlotValueType.Vector4;
            }
            if (t == typeof(ColorRGB))
            {
                return SlotValueType.Vector3;
            }
*/
            if (type == Types._UnityTexture2D)
            {
                return SlotValueType.Texture2D;
            }
            /*
                        if (t == typeof(Texture2DArray))
                        {
                            return SlotValueType.Texture2DArray;
                        }
                        if (t == typeof(Texture3D))
                        {
                            return SlotValueType.Texture3D;
                        }
                        if (t == typeof(Cubemap))
                        {
                            return SlotValueType.Cubemap;
                        }
                        if (t == typeof(Gradient))
                        {
                            return SlotValueType.Gradient;
                        }
                        if (t == typeof(SamplerState))
                        {
                            return SlotValueType.SamplerState;
                        }
                        if (t == typeof(DynamicDimensionVector))
                        {
                            return SlotValueType.DynamicVector;
                        }
                        if (type == Types._float4x4)
                        {
                            return SlotValueType.Matrix4;
                        }
                        if (t == typeof(Matrix3x3))
                        {
                            return SlotValueType.Matrix3;
                        }
                        if (t == typeof(Matrix2x2))
                        {
                            return SlotValueType.Matrix2;
                        }
                        if (t == typeof(DynamicDimensionMatrix))
                        {
                            return SlotValueType.DynamicMatrix;
                        }
            */
            throw new ArgumentException("Unsupported type " + type.Name);
        }

        void ISandboxNodeBuildContext.Error(string message)
        {
            throw new NotImplementedException();
        }

        #endregion

        public override bool hasPreview
        {
            get { return (previewFunction != null); }
        }

        protected SandboxNode()
        {
            // construct the definition
            m_Definition = new DEF();
            UpdateSlotsFromDefinition();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            UpdateSlotsFromDefinition();
        }

        public override void OnBeforeSerialize()
        {
            base.OnBeforeSerialize();
        }

        public override void OnBeforeDeserialize()
        {
            base.OnBeforeDeserialize();
        }

        public override void OnAfterDeserialize(string json)
        {
            base.OnAfterDeserialize(json);
        }

        private void UpdateSlotsFromDefinition()
        {
            ClearRuntimeData();
            m_Definition.BuildRuntime(this);

            // update actual slots from the definition slots
            foreach (var s in definitionSlots)
            {
                AddSlot(s);
            }
            RemoveSlotsNameNotMatching(definitionSlots.Select(x => x.id), true);
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            using (var tempSlots = PooledList<MaterialSlot>.Get())
            {
                // declare output slot variables
                GetOutputSlots(tempSlots);
                foreach (var outSlot in tempSlots)
                {
                    sb.AppendIndentation();
                    sb.Add(outSlot.concreteValueType.ToShaderString(PrecisionUtil.Token), " ", GetVariableNameForSlot(outSlot.id), ";");
                    sb.NewLine();
                }

                // call node function
                sb.AppendIndentation();
                sb.Add(mainFunction.Name, "(");

                tempSlots.Clear();

                // get all slots
                GetSlots(tempSlots);
                bool firstParam = true;
                for (int pIndex = 0; pIndex < mainFunction.Parameters.Count; pIndex++)
                {
                    var p = mainFunction.Parameters[pIndex];
                    if (!firstParam)
                        sb.Add(", ");
                    firstParam = false;

                    // find slot -- ids are parameter index
                    var slot = tempSlots.Find(s => s.id == pIndex);
                    if (p.IsInput)
                        sb.Add(GetSlotValue(slot.id, generationMode));
                    else
                        sb.Add(GetVariableNameForSlot(slot.id));
                }
                sb.Add(");");
                sb.NewLine();
            }
        }

        public virtual void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            registry.ProvideFunction(mainFunction.Name, sb =>
            {
                mainFunction.AppendHLSLDeclarationString(sb);
            });
        }

        public NeededCoordinateSpace RequiresNormal(ShaderStageCapability stageCapability)
        {
            var binding = NeededCoordinateSpace.None;
            using (var tempSlots = PooledList<MaterialSlot>.Get())
            {
                GetInputSlots(tempSlots);
                foreach (var slot in tempSlots)
                    binding |= slot.RequiresNormal();
                return binding;
            }
        }

        public NeededCoordinateSpace RequiresViewDirection(ShaderStageCapability stageCapability)
        {
            var binding = NeededCoordinateSpace.None;
            using (var tempSlots = PooledList<MaterialSlot>.Get())
            {
                GetInputSlots(tempSlots);
                foreach (var slot in tempSlots)
                    binding |= slot.RequiresViewDirection();
                return binding;
            }
        }

        public NeededCoordinateSpace RequiresPosition(ShaderStageCapability stageCapability)
        {
            using (var tempSlots = PooledList<MaterialSlot>.Get())
            {
                GetInputSlots(tempSlots);
                var binding = NeededCoordinateSpace.None;
                foreach (var slot in tempSlots)
                    binding |= slot.RequiresPosition();
                return binding;
            }
        }

        public NeededCoordinateSpace RequiresTangent(ShaderStageCapability stageCapability)
        {
            using (var tempSlots = PooledList<MaterialSlot>.Get())
            {
                GetInputSlots(tempSlots);
                var binding = NeededCoordinateSpace.None;
                foreach (var slot in tempSlots)
                    binding |= slot.RequiresTangent();
                return binding;
            }
        }

        public NeededCoordinateSpace RequiresBitangent(ShaderStageCapability stageCapability)
        {
            using (var tempSlots = PooledList<MaterialSlot>.Get())
            {
                GetInputSlots(tempSlots);
                var binding = NeededCoordinateSpace.None;
                foreach (var slot in tempSlots)
                    binding |= slot.RequiresBitangent();
                return binding;
            }
        }

        public bool RequiresMeshUV(UVChannel channel, ShaderStageCapability stageCapability)
        {
            using (var tempSlots = PooledList<MaterialSlot>.Get())
            {
                GetInputSlots(tempSlots);
                foreach (var slot in tempSlots)
                {
                    if (slot.RequiresMeshUV(channel))
                        return true;
                }

                return false;
            }
        }

        public bool RequiresScreenPosition(ShaderStageCapability stageCapability)
        {
            using (var tempSlots = PooledList<MaterialSlot>.Get())
            {
                GetInputSlots(tempSlots);
                foreach (var slot in tempSlots)
                {
                    if (slot.RequiresScreenPosition(stageCapability))
                        return true;
                }

                return false;
            }
        }

        public bool RequiresVertexColor(ShaderStageCapability stageCapability)
        {
            using (var tempSlots = PooledList<MaterialSlot>.Get())
            {
                GetInputSlots(tempSlots);
                foreach (var slot in tempSlots)
                {
                    if (slot.RequiresVertexColor())
                        return true;
                }

                return false;
            }
        }
    }
}
