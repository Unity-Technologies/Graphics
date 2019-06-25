using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Hlsl;
using static UnityEditor.ShaderGraph.Hlsl.Intrinsics;

namespace UnityEditor.ShaderGraph
{
    abstract class CodeFunctionNode : AbstractMaterialNode
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
    {
        [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
        public class HlslCodeGenAttribute : Attribute
        {
        }


        [NonSerialized]
        private List<SlotAttribute> m_Slots = new List<SlotAttribute>();

        public override bool hasPreview
        {
            get { return true; }
        }

        protected CodeFunctionNode()
        {
            UpdateNodeAfterDeserialization();
        }

        protected struct Boolean
        {}

        protected struct Vector1
        {}

        protected struct Texture2D
        {}

        protected struct Texture2DArray
        {}

        protected struct Texture3D
        {}

        protected struct SamplerState
        {}

        protected struct Gradient
        {}

        protected struct DynamicDimensionVector
        {}

        protected struct ColorRGBA
        {}

        protected struct ColorRGB
        {}

        protected struct Matrix3x3
        {}

        protected struct Matrix2x2
        {}

        protected struct DynamicDimensionMatrix
        {}

        protected enum Binding
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

        [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
        protected class SlotAttribute : Attribute
        {
            public int slotId { get; private set; }
            public Binding binding { get; private set; }
            public bool hidden { get; private set; }
            public Vector4? defaultValue { get; private set; }
            public ShaderStageCapability stageCapability { get; private set; }

            public SlotAttribute(int mSlotId, Binding mImplicitBinding, ShaderStageCapability mStageCapability = ShaderStageCapability.All)
            {
                slotId = mSlotId;
                binding = mImplicitBinding;
                defaultValue = null;
                stageCapability = mStageCapability;
            }

            public SlotAttribute(int mSlotId, Binding mImplicitBinding, bool mHidden, ShaderStageCapability mStageCapability = ShaderStageCapability.All)
            {
                slotId = mSlotId;
                binding = mImplicitBinding;
                hidden = mHidden;
                defaultValue = null;
                stageCapability = mStageCapability;
            }

            public SlotAttribute(int mSlotId, Binding mImplicitBinding, float defaultX, float defaultY, float defaultZ, float defaultW, ShaderStageCapability mStageCapability = ShaderStageCapability.All)
            {
                slotId = mSlotId;
                binding = mImplicitBinding;
                defaultValue = new Vector4(defaultX, defaultY, defaultZ, defaultW);
                stageCapability = mStageCapability;
            }
        }

        protected abstract MethodInfo GetFunctionToConvert();

        private static SlotValueType ConvertTypeToSlotValueType(ParameterInfo p)
        {
            Type t = p.ParameterType;
            if (p.ParameterType.IsByRef)
                t = p.ParameterType.GetElementType();

            if (t == typeof(Boolean))
            {
                return SlotValueType.Boolean;
            }
            if (t == typeof(Vector1) || t == typeof(Hlsl.Float))
            {
                return SlotValueType.Vector1;
            }
            if (t == typeof(Vector2) || t == typeof(Hlsl.Float2))
            {
                return SlotValueType.Vector2;
            }
            if (t == typeof(Vector3) || t == typeof(Hlsl.Float3))
            {
                return SlotValueType.Vector3;
            }
            if (t == typeof(Vector4) || t == typeof(Hlsl.Float4))
            {
                return SlotValueType.Vector4;
            }
            if (t == typeof(Color))
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
            if (t == typeof(Texture2D))
            {
                return SlotValueType.Texture2D;
            }
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
                return SlotValueType.Vector4;
            }
            if (t == typeof(Matrix4x4))
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
            throw new ArgumentException("Unsupported type " + t);
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            var method = GetFunctionToConvert();

            if (method == null)
                throw new ArgumentException("Mapped method is null on node" + this);

            bool isHlslCodeGen = method.GetCustomAttribute<HlslCodeGenAttribute>() != null;
            if (isHlslCodeGen)
            {
                if (method.ReturnType != typeof(void))
                    throw new ArgumentException("Hlsl mapped function should return void");
            }
            else
            {
                if (method.ReturnType != typeof(string))
                    throw new ArgumentException("Mapped function should return string");
            }

            // validate no duplicates
            var slotAtributes = method.GetParameters().Select(GetSlotAttribute).ToList();
            if (slotAtributes.Any(x => x == null))
                throw new ArgumentException("Missing SlotAttribute on " + method.Name);

            if (slotAtributes.GroupBy(x => x.slotId).Any(x => x.Count() > 1))
                throw new ArgumentException("Duplicate SlotAttribute on " + method.Name);

            List<MaterialSlot> slots = new List<MaterialSlot>();
            foreach (var par in method.GetParameters())
            {
                var attribute = GetSlotAttribute(par);
                var name = GraphUtil.ConvertCamelCase(par.Name, true);

                MaterialSlot s;
                if (attribute.binding == Binding.None && !par.IsOut && (par.ParameterType == typeof(Color) || par.ParameterType == typeof(ColorRGBA)))
                    s = new ColorRGBAMaterialSlot(attribute.slotId, name, par.Name, SlotType.Input, attribute.defaultValue ?? Vector4.zero, stageCapability: attribute.stageCapability, hidden: attribute.hidden);
                else if (attribute.binding == Binding.None && !par.IsOut && par.ParameterType == typeof(ColorRGB))
                    s = new ColorRGBMaterialSlot(attribute.slotId, name, par.Name, SlotType.Input, attribute.defaultValue ?? Vector4.zero, ColorMode.Default, stageCapability: attribute.stageCapability, hidden: attribute.hidden);
                else if (attribute.binding == Binding.None || par.IsOut)
                    s = MaterialSlot.CreateMaterialSlot(
                            ConvertTypeToSlotValueType(par),
                            attribute.slotId,
                            name,
                            par.Name,
                            par.IsOut ? SlotType.Output : SlotType.Input,
                            attribute.defaultValue ?? Vector4.zero,
                            shaderStageCapability: attribute.stageCapability,
                            hidden: attribute.hidden,
                            dynamicDimensionGroup: par.GetCustomAttribute<Hlsl.AnyDimensionAttribute>()?.Group
                            );
                else
                    s = CreateBoundSlot(attribute.binding, attribute.slotId, name, par.Name, attribute.stageCapability, attribute.hidden);
                slots.Add(s);

                m_Slots.Add(attribute);
            }
            foreach (var slot in slots)
            {
                AddSlot(slot);
            }
            RemoveSlotsNameNotMatching(slots.Select(x => x.id));
        }

        private static MaterialSlot CreateBoundSlot(Binding attributeBinding, int slotId, string displayName, string shaderOutputName, ShaderStageCapability shaderStageCapability, bool hidden)
        {
            switch (attributeBinding)
            {
                case Binding.ObjectSpaceNormal:
                    return new NormalMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.Object, shaderStageCapability);
                case Binding.ObjectSpaceTangent:
                    return new TangentMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.Object, shaderStageCapability);
                case Binding.ObjectSpaceBitangent:
                    return new BitangentMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.Object, shaderStageCapability);
                case Binding.ObjectSpacePosition:
                    return new PositionMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.Object, shaderStageCapability);
                case Binding.ViewSpaceNormal:
                    return new NormalMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.View, shaderStageCapability);
                case Binding.ViewSpaceTangent:
                    return new TangentMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.View, shaderStageCapability);
                case Binding.ViewSpaceBitangent:
                    return new BitangentMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.View, shaderStageCapability);
                case Binding.ViewSpacePosition:
                    return new PositionMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.View, shaderStageCapability);
                case Binding.WorldSpaceNormal:
                    return new NormalMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.World, shaderStageCapability);
                case Binding.WorldSpaceTangent:
                    return new TangentMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.World, shaderStageCapability);
                case Binding.WorldSpaceBitangent:
                    return new BitangentMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.World, shaderStageCapability);
                case Binding.WorldSpacePosition:
                    return new PositionMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.World, shaderStageCapability);
                case Binding.TangentSpaceNormal:
                    return new NormalMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.Tangent, shaderStageCapability);
                case Binding.TangentSpaceTangent:
                    return new TangentMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.Tangent, shaderStageCapability);
                case Binding.TangentSpaceBitangent:
                    return new BitangentMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.Tangent, shaderStageCapability);
                case Binding.TangentSpacePosition:
                    return new PositionMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.Tangent, shaderStageCapability);
                case Binding.MeshUV0:
                    return new UVMaterialSlot(slotId, displayName, shaderOutputName, UVChannel.UV0, shaderStageCapability);
                case Binding.MeshUV1:
                    return new UVMaterialSlot(slotId, displayName, shaderOutputName, UVChannel.UV1, shaderStageCapability);
                case Binding.MeshUV2:
                    return new UVMaterialSlot(slotId, displayName, shaderOutputName, UVChannel.UV2, shaderStageCapability);
                case Binding.MeshUV3:
                    return new UVMaterialSlot(slotId, displayName, shaderOutputName, UVChannel.UV3, shaderStageCapability);
                case Binding.ScreenPosition:
                    return new ScreenPositionMaterialSlot(slotId, displayName, shaderOutputName, ScreenSpaceType.Default, shaderStageCapability);
                case Binding.ObjectSpaceViewDirection:
                    return new ViewDirectionMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.Object, shaderStageCapability);
                case Binding.ViewSpaceViewDirection:
                    return new ViewDirectionMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.View, shaderStageCapability);
                case Binding.WorldSpaceViewDirection:
                    return new ViewDirectionMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.World, shaderStageCapability);
                case Binding.TangentSpaceViewDirection:
                    return new ViewDirectionMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.Tangent, shaderStageCapability);
                case Binding.VertexColor:
                    return new VertexColorMaterialSlot(slotId, displayName, shaderOutputName, shaderStageCapability);
                default:
                    throw new ArgumentOutOfRangeException("attributeBinding", attributeBinding, null);
            }
        }

        private void EvaluateConstant(MethodInfo function)
        {
            Constants = null;

            if (function.GetCustomAttribute<HlslCodeGenAttribute>() == null)
                return;

            var parms = function.GetParameters();
            var args = new object[parms.Length];
            int outputCount = 0;

            s_TempSlots.Clear();
            GetSlots(s_TempSlots);

            foreach (var slot in s_TempSlots)
            {
                if (!slot.isInputSlot)
                {
                    ++outputCount;
                    continue;
                }

                var parmIndex = Array.FindIndex(parms, e => e.GetCustomAttribute<SlotAttribute>().slotId == slot.id);
                if (parmIndex < 0)
                    return;

                var edges = owner.GetEdges(slot.slotReference).ToArray();
                if (edges.Any())
                {
                    var fromSocketRef = edges[0].outputSlot;
                    var fromNode = owner.GetNodeFromGuid<AbstractMaterialNode>(fromSocketRef.nodeGuid);
                    if (fromNode.Constants == null)
                        return; // not a constant

                    var constantIndex = Array.FindIndex(fromNode.Constants, e => e.slotId == fromSocketRef.slotId);
                    if (constantIndex < 0 || fromNode.Constants[constantIndex].value == null)
                        return; // not a constant

                    var value = fromNode.Constants[constantIndex].value;
                    if (slot is Vector2MaterialSlot)
                    {
                        if (value is Float f1)
                            args[parmIndex] = Float2(f1, 0);
                    }
                    else if (slot is Vector3MaterialSlot)
                    {
                        if (value is Float f1)
                            args[parmIndex] = Float3(f1, 0, 0);
                        else if (value is Float2 f2)
                            args[parmIndex] = Float3(f2, 0);
                    }
                    else if (slot is Vector4MaterialSlot)
                    {
                        if (value is Float f1)
                            args[parmIndex] = Float4(f1, 0, 0, 0);
                        else if (value is Float2 f2)
                            args[parmIndex] = Float4(f2, 0, 0);
                        else if (value is Float3 f3)
                            args[parmIndex] = Float4(f3, 0);
                    }

                    if (args[parmIndex] == null)
                        args[parmIndex] = fromNode.Constants[constantIndex].value;
                }
                else
                {
                    // Default
                    if (slot is Vector1MaterialSlot vec1Slot)
                        args[parmIndex] = Float(vec1Slot.value);
                    else if (slot is Vector2MaterialSlot vec2Slot)
                        args[parmIndex] = Float2(vec2Slot.value.x, vec2Slot.value.y);
                    else if (slot is Vector3MaterialSlot vec3Slot)
                        args[parmIndex] = Float3(vec3Slot.value.x, vec3Slot.value.y, vec3Slot.value.z);
                    else if (slot is Vector4MaterialSlot vec4Slot)
                        args[parmIndex] = Float4(vec4Slot.value.x, vec4Slot.value.y, vec4Slot.value.z, vec4Slot.value.w);
                    else
                        return;
                }
            }

            function.Invoke(null, args);

            var constants = new (int slotId, object value)[outputCount];
            outputCount = 0;
            foreach (var slot in s_TempSlots)
            {
                if (slot.isInputSlot)
                    continue;

                var parmIndex = Array.FindIndex(parms, e => e.GetCustomAttribute<SlotAttribute>().slotId == slot.id);
                if (parmIndex < 0)
                    continue;

                if (args[parmIndex] == null)
                    return;

                var value = args[parmIndex];
                if (slot.concreteValueType == ConcreteSlotValueType.Vector1)
                {
                    if (value is Float2 f2)
                        value = f2.x;
                    else if (value is Float3 f3)
                        value = f3.x;
                    else if (value is Float4 f4)
                        value = f4.x;
                }
                else if (slot.concreteValueType == ConcreteSlotValueType.Vector2)
                {
                    if (value is Float3 f3)
                        value = f3.xy;
                    else if (value is Float4 f4)
                        value = f4.xy;
                }
                else if (slot.concreteValueType == ConcreteSlotValueType.Vector3)
                {
                    if (value is Float4 f4)
                        value = f4.xyz;
                }

                constants[outputCount++] = (slot.id, value);
            }

            Constants = constants;
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GraphContext graphContext, GenerationMode generationMode)
        {
            EvaluateConstant(GetFunctionToConvert());

            s_TempSlots.Clear();
            GetOutputSlots(s_TempSlots);
            foreach (var outSlot in s_TempSlots)
            {
                if (outSlot is IDynamicDimensionSlot dynamic && dynamic.isDynamic && dynamic.IsShrank)
                    sb.AppendLine($"{outSlot.valueType.ToShaderString()} tmpOut{GetVariableNameForSlot(outSlot.id)};");
                else
                    sb.AppendLine(outSlot.concreteValueType.ToShaderString() + " " + GetVariableNameForSlot(outSlot.id) + ";");
            }

            string call = GetFunctionName() + "(";
            bool first = true;
            s_TempSlots.Clear();
            GetSlots(s_TempSlots);
            s_TempSlots.Sort((slot1, slot2) => slot1.id.CompareTo(slot2.id));
            foreach (var slot in s_TempSlots)
            {
                if (!first)
                {
                    call += ", ";
                }
                first = false;

                if (slot.isInputSlot)
                {
                    if (slot is IDynamicDimensionSlot dynamic && dynamic.isDynamic && dynamic.IsShrank)
                    {
                        if (slot.concreteValueType != ConcreteSlotValueType.Vector1)
                            call += $"{slot.valueType.ToShaderString()}({GetSlotValue(slot.id, generationMode)}{String.Concat(Enumerable.Repeat(", 0", slot.valueType.GetChannelCount() - slot.concreteValueType.GetChannelCount()))})";
                        else
                            call += $"{GetSlotValue(slot.id, generationMode)}";
                    }
                    else
                        call += GetSlotValue(slot.id, generationMode);
                }
                else
                {
                    if (slot is IDynamicDimensionSlot dynamic && dynamic.isDynamic && dynamic.IsShrank)
                        call += "tmpOut";
                    call += GetVariableNameForSlot(slot.id);
                }
            }
            call += ");";
            sb.AppendLine(call);

            s_TempSlots.Clear();
            GetOutputSlots(s_TempSlots);
            foreach (var outSlot in s_TempSlots)
            {
                if (outSlot is IDynamicDimensionSlot dynamic && dynamic.isDynamic && dynamic.IsShrank)
                    sb.AppendLine($"{outSlot.concreteValueType.ToShaderString()} {GetVariableNameForSlot(outSlot.id)} = tmpOut{GetVariableNameForSlot(outSlot.id)}.{String.Concat("xyzw".Take(outSlot.concreteValueType.GetChannelCount()))};");
            }
        }

        private string GetFunctionName()
        {
            var function = GetFunctionToConvert();
            return function.Name + (function.IsStatic ? string.Empty : "_" + GuidEncoder.Encode(guid)) + "_" + concretePrecision.ToShaderString();
        }

        private string GetFunctionHeader()
        {
            string header = "void " + GetFunctionName() + "(";

            s_TempSlots.Clear();
            GetSlots(s_TempSlots);
            s_TempSlots.Sort((slot1, slot2) => slot1.id.CompareTo(slot2.id));
            var first = true;
            foreach (var slot in s_TempSlots)
            {
                if (!first)
                    header += ", ";

                first = false;

                if (slot.isOutputSlot)
                    header += "out ";

                header += slot.valueType.ToShaderString() + " " + slot.shaderOutputName;
            }

            header += ")";
            return header;
        }

        private static object GetDefault(Type type)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        private string GetFunctionBody(MethodInfo info)
        {
            var args = new List<object>();
            var parms = info.GetParameters();
            foreach (var param in parms)
            {
                var arg = GetDefault(param.ParameterType);
                if (param.ParameterType == typeof(Float))
                {
                    arg = new Float() { Code = param.Name, Value = null };
                }
                else if (param.ParameterType == typeof(Float2))
                {
                    arg = new Float2() { Code = param.Name, Value = null };
                }
                else if (param.ParameterType == typeof(Float3))
                {
                    arg = new Float3() { Code = param.Name, Value = null };
                }
                else if (param.ParameterType == typeof(Float4))
                {
                    arg = new Float4() { Code = param.Name, Value = null };
                }
                args.Add(arg);
            }

            var argsArray = args.ToArray();
            var result = info.Invoke(this, argsArray) as string;

            if (info.GetCustomAttribute<HlslCodeGenAttribute>() != null)
            {
                result += "{" + Environment.NewLine;
                for (int i = 0; i < args.Count; ++i)
                {
                    if (info.GetParameters()[i].IsOut)
                    {
                        if (argsArray[i] is Float f1)
                            result += parms[i].Name + " = " + f1.Code + ";" + Environment.NewLine;
                        else if (argsArray[i] is Float2 f2)
                            result += parms[i].Name + " = " + f2.Code + ";" + Environment.NewLine;
                        else if (argsArray[i] is Float3 f3)
                            result += parms[i].Name + " = " + f3.Code + ";" + Environment.NewLine;
                        else if (argsArray[i] is Float4 f4)
                            result += parms[i].Name + " = " + f4.Code + ";" + Environment.NewLine;
                    }
                }
                result += "}" + Environment.NewLine;
            }

            if (string.IsNullOrEmpty(result))
                return string.Empty;

            s_TempSlots.Clear();
            GetSlots(s_TempSlots);
            foreach (var slot in s_TempSlots)
            {
                var toReplace = string.Format("{{slot{0}dimension}}", slot.id);
                var replacement = NodeUtils.GetSlotDimension(slot.concreteValueType);
                result = result.Replace(toReplace, replacement);
            }
            return result;
        }

        public virtual void GenerateNodeFunction(FunctionRegistry registry, GraphContext graphContext, GenerationMode generationMode)
        {
            registry.ProvideFunction(GetFunctionName(), s =>
                {
                    s.AppendLine(GetFunctionHeader());
                    var functionBody = GetFunctionBody(GetFunctionToConvert());
                    var lines = functionBody.Trim('\r', '\n', '\t', ' ');
                    s.AppendLines(lines);
                });
        }

        private static SlotAttribute GetSlotAttribute([NotNull] ParameterInfo info)
        {
            var attrs = info.GetCustomAttributes(typeof(SlotAttribute), false).OfType<SlotAttribute>().ToList();
            return attrs.FirstOrDefault();
        }

        public NeededCoordinateSpace RequiresNormal(ShaderStageCapability stageCapability)
        {
            var binding = NeededCoordinateSpace.None;
            s_TempSlots.Clear();
            GetInputSlots(s_TempSlots);
            foreach (var slot in s_TempSlots)
                binding |= slot.RequiresNormal();
            return binding;
        }

        public NeededCoordinateSpace RequiresViewDirection(ShaderStageCapability stageCapability)
        {
            var binding = NeededCoordinateSpace.None;
            s_TempSlots.Clear();
            GetInputSlots(s_TempSlots);
            foreach (var slot in s_TempSlots)
                binding |= slot.RequiresViewDirection();
            return binding;
        }

        public NeededCoordinateSpace RequiresPosition(ShaderStageCapability stageCapability)
        {
            s_TempSlots.Clear();
            GetInputSlots(s_TempSlots);
            var binding = NeededCoordinateSpace.None;
            foreach (var slot in s_TempSlots)
                binding |= slot.RequiresPosition();
            return binding;
        }

        public NeededCoordinateSpace RequiresTangent(ShaderStageCapability stageCapability)
        {
            s_TempSlots.Clear();
            GetInputSlots(s_TempSlots);
            var binding = NeededCoordinateSpace.None;
            foreach (var slot in s_TempSlots)
                binding |= slot.RequiresTangent();
            return binding;
        }

        public NeededCoordinateSpace RequiresBitangent(ShaderStageCapability stageCapability)
        {
            s_TempSlots.Clear();
            GetInputSlots(s_TempSlots);
            var binding = NeededCoordinateSpace.None;
            foreach (var slot in s_TempSlots)
                binding |= slot.RequiresBitangent();
            return binding;
        }

        public bool RequiresMeshUV(UVChannel channel, ShaderStageCapability stageCapability)
        {
            s_TempSlots.Clear();
            GetInputSlots(s_TempSlots);
            foreach (var slot in s_TempSlots)
            {
                if (slot.RequiresMeshUV(channel))
                    return true;
            }
            return false;
        }

        public bool RequiresScreenPosition(ShaderStageCapability stageCapability)
        {
            s_TempSlots.Clear();
            GetInputSlots(s_TempSlots);
            foreach (var slot in s_TempSlots)
            {
                if (slot.RequiresScreenPosition())
                    return true;
            }
            return false;
        }

        public bool RequiresVertexColor(ShaderStageCapability stageCapability)
        {
            s_TempSlots.Clear();
            GetInputSlots(s_TempSlots);
            foreach (var slot in s_TempSlots)
            {
                if (slot.RequiresVertexColor())
                    return true;
            }
            return false;
        }
    }
}
