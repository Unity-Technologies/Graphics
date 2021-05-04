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
        AbsoluteWorldSpacePosition,
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

    public class DynamicDefaultValue
    {
        public Matrix4x4 matrixDefault;
    };

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
        protected DEF m_Definition;

        // runtime data (not serialized)
        ShaderFunction mainFunction;
        ShaderFunction previewFunction;
        List<MaterialSlot> definitionSlots = new List<MaterialSlot>();

        // temporary data (used during BuildRuntime)
        int newSlotId = -1;

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

        SandboxType ISandboxNodeBuildContext.AddType(SandboxTypeDefinition typeDef)
        {
            throw new NotImplementedException();
        }

        bool ISandboxNodeBuildContext.GetInputConnected(string pinName)
        {
            MaterialSlot inputSlot = GetSlotByShaderOutputName(pinName);
            if ((inputSlot == null) || (inputSlot.isOutputSlot))
                return false;

            // TODO: there's a slight mismatch here between isConnected and what GetInputConnected is supposed to return
            // GetInputConnected is supposed to return whether anything is overriding the definition default (including local slot "defaultValue"), whereas
            // inputSlot.isConnected only refers to whether there is a wire attached..
            return inputSlot.isConnected;
        }

        SandboxType ISandboxNodeBuildContext.GetInputType(string pinName)
        {
            // lookup type on the slots
            MaterialSlot inputSlot = GetSlotByShaderOutputName(pinName);
            if ((inputSlot == null) || (inputSlot.isOutputSlot))
                return null;

            // the input slot doesn't know the connected type, but the output slot does...
            // so jump to the connected output slot
            var outputSlot = GetConnectedSlot(inputSlot);
            if ((outputSlot == null) || (outputSlot.isInputSlot))
                return null;        // TODO: should really return the type of the default here...

            // problem is.. the actual type is not really (easily) known here..  (old system is weird)
            // it's actually a combination of the ConcreteSlotValueType and the node precision...
            // but we can approximate the type (at least good enough for now) by looking at the ConcreteSlotValueType
            ConcreteSlotValueType vtype = outputSlot.concreteValueType;
            switch (vtype)
            {
                case ConcreteSlotValueType.SamplerState:
                    return Types._UnitySamplerState;
                case ConcreteSlotValueType.Matrix4:
                    return Types._precision4x4;
                case ConcreteSlotValueType.Matrix3:
                    return Types._precision3x3;
                case ConcreteSlotValueType.Matrix2:
                    return Types._precision2x2;
                case ConcreteSlotValueType.Texture2D:
                    return Types._UnityTexture2D;
                case ConcreteSlotValueType.Texture2DArray:
                    break;
                case ConcreteSlotValueType.Texture3D:
                    break;
                case ConcreteSlotValueType.Cubemap:
                    break;
                case ConcreteSlotValueType.Gradient:
                    break;
                case ConcreteSlotValueType.Vector4:
                    return Types._precision4;
                case ConcreteSlotValueType.Vector3:
                    return Types._precision3;
                case ConcreteSlotValueType.Vector2:
                    return Types._precision2;
                case ConcreteSlotValueType.Vector1:
                    return Types._precision;
                case ConcreteSlotValueType.Boolean:
                    return Types._bool;
                case ConcreteSlotValueType.VirtualTexture:
                    break;
            }
            return null;
        }

        void AddSlotInternal(SlotType slotType, SandboxType type, string name, System.Object defaultValue = null)
        {
            // AbstractMaterialNode requires that slot IDs are the unique stable identifier,
            // whereas we are using the parameter Name as the unique stable identifier
            int slotId = -1;
            var existingSlot = GetSlotByShaderOutputName(name);
            if (existingSlot != null)
            {
                // if a slot exists with the name, re-use the slot ID for it
                slotId = existingSlot.id;
            }
            else
            {
                // no existing slot with that name .. just allocate a new id
                slotId = newSlotId;
                newSlotId++;
            }

            MaterialSlot s;
            /*
             * // TODO: handle color type
                                if (attribute.binding == Binding.None && !par.IsOut && par.ParameterType == typeof(Color))
                                    s = new ColorRGBAMaterialSlot(attribute.slotId, name, par.Name, SlotType.Input, attribute.defaultValue ?? Vector4.zero, stageCapability: attribute.stageCapability, hidden: attribute.hidden);
                                else if (attribute.binding == Binding.None && !par.IsOut && par.ParameterType == typeof(ColorRGBA))
                                    s = new ColorRGBAMaterialSlot(attribute.slotId, name, par.Name, SlotType.Input, attribute.defaultValue ?? Vector4.zero, stageCapability: attribute.stageCapability, hidden: attribute.hidden);
                                else if (attribute.binding == Binding.None && !par.IsOut && par.ParameterType == typeof(ColorRGB))
                                    s = new ColorRGBMaterialSlot(attribute.slotId, name, par.Name, SlotType.Input, attribute.defaultValue ?? Vector4.zero, ColorMode.Default, stageCapability: attribute.stageCapability, hidden: attribute.hidden);
                                else if (attribute.binding == Binding.None || par.IsOut)
            */
            // we assume strings are for external input bindings
            if (defaultValue is Binding binding)
            {
                s = SandboxNodeUtils.CreateBoundSlot(binding, slotId, name, name, ShaderStageCapability.All
                    // attribute.stageCapability, attribute.hidden      // TODO
                );
            }
            else
            {
                // massage defaults into old system
                Vector4 defaultVector = Vector4.zero;
                switch (defaultValue)
                {
                    case float f:
                        defaultVector = new Vector4(f, f, f, f);
                        break;
                    case Vector2 vec2:
                        defaultVector = new Vector4(vec2.x, vec2.y, 0.0f, 0.0f);
                        break;
                    case Vector3 vec3:
                        defaultVector = new Vector4(vec3.x, vec3.y, vec3.z, 0.0f);
                        break;
                    case Vector4 vec4:
                        defaultVector = vec4;
                        break;
                }

                if (defaultValue is DynamicDefaultValue dynamicDefault)
                {
                    var dyn = new DynamicValueMaterialSlot(slotId, name, name, slotType, dynamicDefault.matrixDefault
                        // shaderStageCapability: attribute.stageCapability,        // TODO: ability to tag stage capabilities
                        // hidden: attribute.hidden                                 // TODO: what's this used for?
                    );

                    // because it's a dynamic slot, we need to set the current type ourselves
                    var concreteType = SandboxNodeUtils.ConvertSandboxTypeToConcreteSlotValueType(type);
                    dyn.SetConcreteType(concreteType);
                    s = dyn;
                }
                else
                {
                    s = MaterialSlot.CreateMaterialSlot(
                        SandboxNodeUtils.ConvertSandboxValueTypeToSlotValueType(type),
                        slotId,
                        name,
                        name,
                        slotType,
                        defaultVector
                        // shaderStageCapability: attribute.stageCapability,        // TODO: ability to tag stage capabilities
                        // hidden: attribute.hidden                                 // TODO: what's this used for?
                    );
                }

                if (existingSlot != null)
                    s.CopyValuesFrom(existingSlot);
            }

            definitionSlots.Add(s);
        }

        void ISandboxNodeBuildContext.AddInputSlot(SandboxType type, string name, System.Object defaultValue)
        {
            AddSlotInternal(SlotType.Input, type, name, defaultValue);
        }

        void ISandboxNodeBuildContext.AddOutputSlot(SandboxType type, string name)
        {
            AddSlotInternal(SlotType.Output, type, name);
        }

        void ISandboxNodeBuildContext.SetMainFunction(ShaderFunction function, bool declareSlots)
        {
            mainFunction = function;
            if (declareSlots)
            {
                foreach (ShaderFunction.Parameter p in function.Parameters)
                {
                    AddSlotInternal(
                        p.IsInput ? SlotType.Input : SlotType.Output,
                        p.Type,
                        p.Name,
                        p.DefaultValue);
                }
            }
        }

        void ISandboxNodeBuildContext.SetPreviewFunction(ShaderFunction function, PreviewMode defaultPreviewMode)
        {
            previewFunction = function;

            // TODO this isn't correct -- stomps on user selection
            m_PreviewMode = defaultPreviewMode;
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

        public override void Setup()
        {
            UpdateSlotsFromDefinition();
            base.Setup();
        }

        public override void Concretize()
        {
            UpdateSlotsFromDefinition();
            base.Concretize();
        }

        protected void RebuildNode()
        {
            UpdateSlotsFromDefinition();
            Dirty(ModificationScope.Topological);
        }

        private void UpdateSlotsFromDefinition()
        {
            ClearRuntimeData();

            // figure out where new slots start to get allocated
            newSlotId = GetUnusedSlotId();

            m_Definition.BuildRuntime(this);

            // update actual slots from the definition slots
            foreach (var s in definitionSlots)
            {
                // we must always replace here, as types may change
                AddSlot(s, false);
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

                    // find slot by name
                    var slot = tempSlots.Find(s => s.shaderOutputName == p.Name);
                    if (slot == null)
                    {
                        Debug.LogWarning("SandboxNode: Cannot find Slot " + p.Name);
                    }
                    else
                    {
                        if (p.IsInput)
                        {
                            if (slot == null)
                                sb.Add(p.DefaultValue?.ToString() ?? "null");
                            else
                                sb.Add(GetSlotValue(slot.id, generationMode));
                        }
                        else
                            sb.Add(GetVariableNameForSlot(slot.id));
                    }
                }
                sb.Add(");");
                sb.NewLine();
            }
        }

        public virtual void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            var function = (generationMode == GenerationMode.Preview) ? previewFunction : mainFunction;

            SandboxNodeUtils.ProvideFunctionToRegistry(function, registry);
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
