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
        JsonRef<DEF> m_Definition;

        // runtime data (not serialized)
        ShaderFunction mainFunction;
        List<MaterialSlot> definitionSlots = new List<MaterialSlot>();

        #region ISandboxNodeBuildContext
        void ClearRuntimeData()
        {
            mainFunction = null;
            definitionSlots.Clear();
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
                    s = MaterialSlot.CreateMaterialSlot(
                        ConvertSandboxValueTypeToSlotValueType(p.Type),
                        slotId,
                        p.Name,
                        p.Name,
                        p.IsOutput ? SlotType.Output : SlotType.Input,
                        (p.DefaultValue as Vector4 ? ) ?? Vector4.zero              // TODO: handle non vector4 defaults
                        // shaderStageCapability: attribute.stageCapability,
                        // hidden: attribute.hidden
                    );

                    /*                    else
                                            s = CreateBoundSlot(attribute.binding, attribute.slotId, name, par.Name, attribute.stageCapability, attribute.hidden);
                    */

                    definitionSlots.Add(s);
                }
            }
        }

        // TODO: move to static utils?
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
            if (type == Types._float3)
            {
                return SlotValueType.Vector3;
            }
            if (type == Types._float4)
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
            get { return false; }
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
            Debug.Log("OnBeforeSerialize: " + m_Definition.value);
            // ensure all runtime data is not serialized by clearing it out
            ClearRuntimeData();
            base.OnBeforeSerialize();
        }

        public override void OnBeforeDeserialize()
        {
            Debug.Log("OnBeforeDeserialize: " + m_Definition.value);
            base.OnBeforeDeserialize();
        }

        public override void OnAfterDeserialize(string json)
        {
            Debug.Log("OnAfterDeserialize: " + m_Definition.value);
            base.OnAfterDeserialize(json);
        }

        private void UpdateSlotsFromDefinition()
        {
            ClearRuntimeData();
            m_Definition.value.BuildRuntime(this);

            // update actual slots from the definition slots
            foreach (var s in definitionSlots)
            {
                AddSlot(s);
            }
            RemoveSlotsNameNotMatching(definitionSlots.Select(x => x.id), true);
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            sb.AppendLine("// Sandbox Node Code here!");
//             using (var tempSlots = PooledList<MaterialSlot>.Get())
//             {
//                 GetOutputSlots(tempSlots);
//                 foreach (var outSlot in tempSlots)
//                 {
//                     sb.AppendLine(outSlot.concreteValueType.ToShaderString(PrecisionUtil.Token) + " " + GetVariableNameForSlot(outSlot.id) + ";");
//                 }
//
//                 string call = GetFunctionName() + "(";
//                 bool first = true;
//                 tempSlots.Clear();
//                 GetSlots(tempSlots);
//                 tempSlots.Sort((slot1, slot2) => slot1.id.CompareTo(slot2.id));
//                 foreach (var slot in tempSlots)
//                 {
//                     if (!first)
//                     {
//                         call += ", ";
//                     }
//                     first = false;
//
//                     if (slot.isInputSlot)
//                         call += GetSlotValue(slot.id, generationMode);
//                     else
//                         call += GetVariableNameForSlot(slot.id);
//                 }
//                 call += ");";
//
//                 sb.AppendLine(call);
//             }
        }

        public virtual void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            registry.ProvideFunction("myFunction", sb =>
            {
                sb.AppendLine("// myFunction declaration goes here");
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
