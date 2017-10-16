using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    public abstract class CodeFunctionNode : AbstractMaterialNode
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

        protected struct Vector1
        {}

        protected struct Texture2D
        {}

        protected struct SamplerState
        {}

        protected struct DynamicDimensionVector
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

        private static string BindChannelToShaderName(Binding channel)
        {
            switch (channel)
            {
                case Binding.None:
                    return "ERROR!";
                case Binding.ObjectSpaceNormal:
                    return ShaderGeneratorNames.ObjectSpaceNormal;
                case Binding.ObjectSpaceTangent:
                    return ShaderGeneratorNames.ObjectSpaceTangent;
                case Binding.ObjectSpaceBitangent:
                    return ShaderGeneratorNames.ObjectSpaceBiTangent;
                case Binding.ObjectSpacePosition:
                    return ShaderGeneratorNames.ObjectSpacePosition;
                case Binding.ViewSpaceNormal:
                    return ShaderGeneratorNames.ViewSpaceNormal;
                case Binding.ViewSpaceTangent:
                    return ShaderGeneratorNames.ViewSpaceTangent;
                case Binding.ViewSpaceBitangent:
                    return ShaderGeneratorNames.ViewSpaceBiTangent;
                case Binding.ViewSpacePosition:
                    return ShaderGeneratorNames.ViewSpacePosition;
                case Binding.WorldSpaceNormal:
                    return ShaderGeneratorNames.WorldSpaceNormal;
                case Binding.WorldSpaceTangent:
                    return ShaderGeneratorNames.WorldSpaceTangent;
                case Binding.WorldSpaceBitangent:
                    return ShaderGeneratorNames.WorldSpaceBiTangent;
                case Binding.WorldSpacePosition:
                    return ShaderGeneratorNames.WorldSpacePosition;
                case Binding.TangentSpaceNormal:
                    return ShaderGeneratorNames.TangentSpaceNormal;
                case Binding.TangentSpaceTangent:
                    return ShaderGeneratorNames.TangentSpaceTangent;
                case Binding.TangentSpaceBitangent:
                    return ShaderGeneratorNames.TangentSpaceBiTangent;
                case Binding.TangentSpacePosition:
                    return ShaderGeneratorNames.TangentSpacePosition;
                case Binding.MeshUV0:
                    return UVChannel.uv0.GetUVName();
                case Binding.MeshUV1:
                    return UVChannel.uv1.GetUVName();
                case Binding.MeshUV2:
                    return UVChannel.uv2.GetUVName();
                case Binding.MeshUV3:
                    return UVChannel.uv3.GetUVName();
                case Binding.ScreenPosition:
                    return ShaderGeneratorNames.ScreenPosition;
                case Binding.ObjectSpaceViewDirection:
                    return ShaderGeneratorNames.ObjectSpaceViewDirection;
                case Binding.ViewSpaceViewDirection:
                    return ShaderGeneratorNames.ViewSpaceViewDirection;
                case Binding.WorldSpaceViewDirection:
                    return ShaderGeneratorNames.WorldSpaceViewDirection;
                case Binding.TangentSpaceViewDirection:
                    return ShaderGeneratorNames.TangentSpaceViewDirection;
                case Binding.VertexColor:
                    return ShaderGeneratorNames.VertexColor;
                default:
                    throw new ArgumentOutOfRangeException("channel", channel, null);
            }
        }

        [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
        protected class SlotAttribute : Attribute
        {
            public int slotId { get; private set; }
            public Binding binding { get; private set; }
            public bool hidden { get; private set; }
            public Vector4? defaultValue { get; private set; }

            public SlotAttribute(int mSlotId, Binding mImplicitBinding)
            {
                slotId = mSlotId;
                binding = mImplicitBinding;
                defaultValue = null;
            }

            public SlotAttribute(int mSlotId, Binding mImplicitBinding, bool mHidden)
            {
                slotId = mSlotId;
                binding = mImplicitBinding;
                hidden = mHidden;
                defaultValue = null;
            }

            public SlotAttribute(int mSlotId, Binding mImplicitBinding, float defaultX, float defaultY, float defaultZ, float defaultW)
            {
                slotId = mSlotId;
                binding = mImplicitBinding;
                defaultValue = new Vector4(defaultX, defaultY, defaultZ, defaultW);
            }
        }

        protected static MethodInfo GetMethodInfo(LambdaExpression expression)
        {
            MethodCallExpression outermostExpression = expression.Body as MethodCallExpression;
             
            if (outermostExpression == null)
            {
                throw new ArgumentException("Invalid Expression. Expression should consist of a Method call only.");
            }
             
            return outermostExpression.Method;
        }

        protected abstract MethodInfo GetFunctionToConvert();

        private static SlotValueType ConvertTypeToSlotValueType(ParameterInfo p)
        {
            Type t = p.ParameterType;
            if (p.ParameterType.IsByRef)
                t = p.ParameterType.GetElementType();

            if (t == typeof(Vector1))
            {
                return SlotValueType.Vector1;
            }
            if (t == typeof(Vector2))
            {
                return SlotValueType.Vector2;
            }
            if (t == typeof(Vector3))
            {
                return SlotValueType.Vector3;
            }
            if (t == typeof(Vector4))
            {
                return SlotValueType.Vector4;
            }
            if (t == typeof(Texture2D))
            {
                return SlotValueType.Texture2D;
            }
            if (t == typeof(SamplerState))
            {
                return SlotValueType.SamplerState;
            }
            if (t == typeof(DynamicDimensionVector))
            {
                return SlotValueType.Dynamic;
            }
            if (t == typeof(Matrix4x4))
            {
                return SlotValueType.Matrix4;
            }
            throw new ArgumentException("Unsupported type " + t);
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            var method = GetFunctionToConvert();

            if (method == null)
                throw new ArgumentException("Mapped method is null on node" + this);

            if (method.ReturnType != typeof(string))
                throw new ArgumentException("Mapped function should return string");

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

                slots.Add(new MaterialSlot(attribute.slotId, par.Name, par.Name, par.IsOut ? SlotType.Output : SlotType.Input,
                        ConvertTypeToSlotValueType(par), attribute.defaultValue ?? Vector4.zero, hidden: attribute.hidden));


                m_Slots.Add(attribute);
            }
            foreach (var slot in slots)
            {
                AddSlot(slot);
            }
            RemoveSlotsNameNotMatching(slots.Select(x => x.id));
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            foreach (var outSlot in GetOutputSlots<MaterialSlot>())
            {
                visitor.AddShaderChunk(GetParamTypeName(outSlot) + " " + GetVariableNameForSlot(outSlot.id) + ";", true);
            }

            string call = GetFunctionName() + "(";
            bool first = true;
            foreach (var arg in GetSlots<MaterialSlot>().OrderBy(x => x.id))
            {
                if (!first)
                {
                    call += ", ";
                }
                first = false;

                if (arg.isInputSlot)
                {
                    var inEdges = owner.GetEdges(arg.slotReference);
                    if (!inEdges.Any())
                    {
                        var info = m_Slots.FirstOrDefault(x => x.slotId == arg.id);
                        if (info != null)
                        {
                            var bindingInfo = info.binding;
                            if (bindingInfo != Binding.None)
                            {
                                call += BindChannelToShaderName(bindingInfo);
                                continue;
                            }
                        }
                    }

                    call += GetSlotValue(arg.id, generationMode);
                }
                else
                    call += GetVariableNameForSlot(arg.id);
            }
            call += ");";

            visitor.AddShaderChunk(call, true);
        }

        private string GetParamTypeName(MaterialSlot slot)
        {
            return ConvertConcreteSlotValueTypeToString(precision, slot.concreteValueType);
        }

        private string GetFunctionName()
        {
            var function = GetFunctionToConvert();
            return function.Name + "_" + (function.IsStatic ? string.Empty : GuidEncoder.Encode(guid) + "_") + precision;
        }

        private string GetFunctionHeader()
        {
            string header = "void " + GetFunctionName() + "(";

            var first = true;
            foreach (var slot in GetSlots<MaterialSlot>().OrderBy(x => x.id))
            {
                if (!first)
                    header += ", ";

                first = false;

                if (slot.isOutputSlot)
                    header += "out ";

                header += GetParamTypeName(slot) + " " + slot.shaderOutputName;
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
            foreach (var param in info.GetParameters())
                args.Add(GetDefault(param.ParameterType));

            var result = info.Invoke(this, args.ToArray()) as string;

            if (string.IsNullOrEmpty(result))
                return string.Empty;

            result = result.Replace("{precision}", precision.ToString());
            foreach (var slot in GetSlots<MaterialSlot>())
            {
                var toReplace = string.Format("{{slot{0}dimension}}", slot.id);
                var replacement = ConvertConcreteSlotValueTypeToString(slot.concreteValueType);
                result = result.Replace(toReplace, replacement);
            }
            return result;
        }

        public virtual void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            string function = GetFunctionHeader() + GetFunctionBody(GetFunctionToConvert());
            visitor.AddShaderChunk(function, true);
        }

        private bool NodeRequiresBinding(Binding channel)
        {
            foreach (var slot in GetSlots<MaterialSlot>())
            {
                if (SlotRequiresBinding(channel, slot))
                    return true;
            }
            return false;
        }

        private bool SlotRequiresBinding(Binding channel, [NotNull] MaterialSlot slot)
        {
            if (slot.isOutputSlot)
                return false;

            var inEdges = owner.GetEdges(slot.slotReference);
            if (inEdges.Any())
                return false;

            var slotAttr = m_Slots.FirstOrDefault(x => x.slotId == slot.id);
            if (slotAttr != null && slotAttr.binding == channel)
                return true;

            return false;
        }

        private static SlotAttribute GetSlotAttribute([NotNull] ParameterInfo info)
        {
            var attrs = info.GetCustomAttributes(typeof(SlotAttribute), false).OfType<SlotAttribute>().ToList();
            return attrs.FirstOrDefault();
        }

        public NeededCoordinateSpace RequiresNormal()
        {
            var binding = NeededCoordinateSpace.None;
            if (NodeRequiresBinding(Binding.ObjectSpaceNormal))
                binding |= NeededCoordinateSpace.Object;
            if (NodeRequiresBinding(Binding.ViewSpaceNormal))
                binding |= NeededCoordinateSpace.View;
            if (NodeRequiresBinding(Binding.WorldSpaceNormal))
                binding |= NeededCoordinateSpace.World;
            if (NodeRequiresBinding(Binding.TangentSpaceNormal))
                binding |= NeededCoordinateSpace.Tangent;

            return binding;
        }

        public bool RequiresMeshUV(UVChannel channel)
        {
            switch (channel)
            {
                case UVChannel.uv0:
                    return NodeRequiresBinding(Binding.MeshUV0);
                case UVChannel.uv1:
                    return NodeRequiresBinding(Binding.MeshUV1);
                case UVChannel.uv2:
                    return NodeRequiresBinding(Binding.MeshUV2);
                case UVChannel.uv3:
                    return NodeRequiresBinding(Binding.MeshUV3);
                default:
                    throw new ArgumentOutOfRangeException("channel", channel, null);
            }
        }

        public bool RequiresScreenPosition()
        {
            return NodeRequiresBinding(Binding.ScreenPosition);
        }

        public NeededCoordinateSpace RequiresViewDirection()
        {
            var binding = NeededCoordinateSpace.None;
            if (NodeRequiresBinding(Binding.ObjectSpaceViewDirection))
                binding |= NeededCoordinateSpace.Object;
            if (NodeRequiresBinding(Binding.ViewSpaceViewDirection))
                binding |= NeededCoordinateSpace.View;
            if (NodeRequiresBinding(Binding.WorldSpaceViewDirection))
                binding |= NeededCoordinateSpace.World;
            if (NodeRequiresBinding(Binding.TangentSpaceNormal))
                binding |= NeededCoordinateSpace.Tangent;

            return binding;
        }

        public NeededCoordinateSpace RequiresPosition()
        {
            var binding = NeededCoordinateSpace.None;
            if (NodeRequiresBinding(Binding.ObjectSpacePosition))
                binding |= NeededCoordinateSpace.Object;
            if (NodeRequiresBinding(Binding.ViewSpacePosition))
                binding |= NeededCoordinateSpace.View;
            if (NodeRequiresBinding(Binding.WorldSpacePosition))
                binding |= NeededCoordinateSpace.World;
            if (NodeRequiresBinding(Binding.TangentSpacePosition))
                binding |= NeededCoordinateSpace.Tangent;

            return binding;
        }

        public NeededCoordinateSpace RequiresTangent()
        {
            var binding = NeededCoordinateSpace.None;
            if (NodeRequiresBinding(Binding.ObjectSpaceTangent))
                binding |= NeededCoordinateSpace.Object;
            if (NodeRequiresBinding(Binding.ViewSpaceTangent))
                binding |= NeededCoordinateSpace.View;
            if (NodeRequiresBinding(Binding.WorldSpaceTangent))
                binding |= NeededCoordinateSpace.World;
            if (NodeRequiresBinding(Binding.TangentSpaceTangent))
                binding |= NeededCoordinateSpace.Tangent;

            return binding;
        }

        public NeededCoordinateSpace RequiresBitangent()
        {
            var binding = NeededCoordinateSpace.None;
            if (NodeRequiresBinding(Binding.ObjectSpaceBitangent))
                binding |= NeededCoordinateSpace.Object;
            if (NodeRequiresBinding(Binding.ViewSpaceBitangent))
                binding |= NeededCoordinateSpace.View;
            if (NodeRequiresBinding(Binding.WorldSpaceBitangent))
                binding |= NeededCoordinateSpace.World;
            if (NodeRequiresBinding(Binding.TangentSpaceBitangent))
                binding |= NeededCoordinateSpace.Tangent;

            return binding;
        }

        public bool RequiresVertexColor()
        {
            return NodeRequiresBinding(Binding.VertexColor);
        }
    }
}
