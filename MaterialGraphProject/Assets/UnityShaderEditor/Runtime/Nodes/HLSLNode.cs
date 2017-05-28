using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("TEST/Add Node")]
    public class SnippetAddNode : SimpleNode
    {

        protected  override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Add", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static void Unity_Add(out DynamicDimensionVector result, [Bind(BindChannel.MeshUV0)]DynamicDimensionVector first, DynamicDimensionVector second)
        {
        }

        protected override string GetFunctionBody()
        {
            return
@"
{
    result = first + second;
}
";
        }
    }

    [Title("TEST/POM Node")]
    public class SnippetPOMNode : SimpleNode
    {
        protected  override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_POM", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static void Unity_POM(
            out Vector2 result,
            Sampler2D tex,
            ShaderSingle heightScale,
            [Bind(BindChannel.MeshUV0)] Vector2 UVs,
            [Bind(BindChannel.ViewDirectionTangentSpace)] Vector3 viewTangentSpace,
            [Bind(BindChannel.Normal)] Vector3 worldSpaceNormal,
            [Bind(BindChannel.ViewDirection)] Vector3 worldSpaceViewDirection)
        {
            result = Vector2.zero;
        }

        protected override string GetFunctionBody()
        {
            return
@"
{
    float2 height_map_dimensions = float2(256.0f, 256.0f);      //HARDCODE
    //height_map.tex.GetDimensions(height_map_dimensions.x, height_map_dimensions.y);

    float2 texcoord= UVs;

    // Compute the current gradients:
    float2 texcoords_per_size = texcoord * height_map_dimensions;

    // Compute all 4 derivatives in x and y in a single instruction to optimize:
    float2 dx, dy;
    float4 temp_ddx = ddx(float4(texcoords_per_size, texcoord));
    dx.xy = temp_ddx.zw;
    float4 temp_ddy = ddy(float4(texcoords_per_size, texcoord));
    dy.xy = temp_ddy.zw;

    // Start the current sample located at the input texture coordinate, which would correspond
    // to computing a bump mapping result:
    float2 result_texcoord = texcoord;

    float height_scale_value = heightScale;
    float height_scale_adjust = height_scale_value;

    float per_pixel_height_scale_value = height_scale_value * heightScale;

    // Parallax occlusion mapping offset computation
    //--------------

    // Utilize dynamic flow control to change the number of samples per ray
    // depending on the viewing angle for the surface. Oblique angles require
    // smaller step sizes to achieve more accurate precision for computing displacement.
    // We express the sampling rate as a linear function of the angle between
    // the geometric normal and the view direction ray:
    float max_samples = 30.0f;
    float min_samples = 4.0f;

    float view_dot_normal= dot(worldSpaceNormal, worldSpaceViewDirection);

    int number_of_steps = (int)lerp(max_samples, min_samples, saturate(view_dot_normal));

    // Intersect the view ray with the height field profile along the direction of
    // the parallax offset ray (computed in the vertex shader. Note that the code is
    // designed specifically to take advantage of the dynamic flow control constructs
    // in HLSL and is very sensitive to specific syntax. When converting to other examples,
    // if still want to use dynamic flow control in the resulting assembly shader,
    // care must be applied.
    //
    // In the below steps we approximate the height field profile as piecewise linear
    // curve. We find the pair of endpoints between which the intersection between the
    // height field profile and the view ray is found and then compute line segment
    // intersection for the view ray and the line segment formed by the two endpoints.
    // This intersection is the displacement offset from the original texture coordinate.
    // See the above SI3D 06 paper for more details about the process and derivation.
    //

    float current_height = 0.0;
    float step_size = 1.0 / (float)number_of_steps;

    float previous_height = 1.0;
    float next_height = 0.0;


    int step_index = 0;

    // Optimization: this should move to vertex shader, however, we compute it here for simplicity of
    // integration into our shaders for now.
    float3 normalized_view_dir_in_tangent_space = normalize(viewTangentSpace.xyz);

    // Compute initial parallax displacement direction:
    float2 parallax_direction = normalize(viewTangentSpace.xy);

    // The length of this vector determines the furthest amount of displacement:
    float parallax_direction_length = length(normalized_view_dir_in_tangent_space);


    float max_parallax_amount = sqrt(parallax_direction_length * parallax_direction_length - viewTangentSpace.z * viewTangentSpace.z) / viewTangentSpace.z;

    // Compute the actual reverse parallax displacement vector:
    float2 parallax_offset_in_tangent_space = parallax_direction * max_parallax_amount;

    // Need to scale the amount of displacement to account for different height ranges
    // in height maps. This is controlled by an artist-editable parameter:
    parallax_offset_in_tangent_space *= saturate(heightScale);
    float2 texcoord_offset_per_step = step_size * parallax_offset_in_tangent_space;

    float2 current_texcoord_offset = texcoord;
    float current_bound = 1.0;
    float current_parallax_amount = 0.0;

    float2 pt1 = 0;
    float2 pt2 = 0;


    float2 temp_texcoord_offset = 0;

    while (step_index < number_of_steps)
    {
        current_texcoord_offset -= texcoord_offset_per_step;

        // Sample height map which in this case is stored in the alpha channel of the normal map:
        current_height = tex2Dgrad(tex, current_texcoord_offset, dx, dy).r;

        current_bound -= step_size;

        if (current_height > current_bound)
        {
            pt1 = float2(current_bound, current_height);
            pt2 = float2(current_bound + step_size, previous_height);
            temp_texcoord_offset = current_texcoord_offset - texcoord_offset_per_step;
            step_index = number_of_steps + 1;
        }
        else
        {
            step_index++;
            previous_height = current_height;
        }
     }   // End of while ( step_index < number_of_steps)


    float delta2 = pt2.x - pt2.y;
    float delta1 = pt1.x - pt1.y;

    float denominator = delta2 - delta1;

    // SM 3.0 and above requires a check for divide by zero since that operation
    // will generate an 'Inf' number instead of 0
    if (denominator== 0.0f)
    {
        current_parallax_amount= 0.0f;
    }
    else
    {
        current_parallax_amount= (pt1.x* delta2 - pt2.x* delta1) / denominator;
    }

    float2 parallax_offset = parallax_offset_in_tangent_space * (1.0f - current_parallax_amount);

    // The computed texture offset for the displaced point on the pseudo-extruded surface:
    float2 parallaxed_texcoord = texcoord - parallax_offset;

    result = parallaxed_texcoord;
}
";
        }
    }

    public abstract class SimpleNode : AbstractMaterialNode
        , IGeneratesBodyCode
        , IGeneratesFunction
        , IMayRequireNormal
        , IMayRequireTangent
        , IMayRequireBitangent
        , IMayRequireMeshUV
        , IMayRequireScreenPosition
        , IMayRequireViewDirection
        , IMayRequireWorldPosition
        , IMayRequireVertexColor
        , IMayRequireViewDirectionTangentSpace
    {
        private List<KeyValuePair<int, ParameterInfo>> m_ParamMap = new List<KeyValuePair<int, ParameterInfo>>();

        public override bool hasPreview
        {
            get { return true; }
        }

        public SimpleNode()
        {
            UpdateNodeAfterDeserialization();
        }

        protected struct ShaderSingle
        {}

        protected struct Texture2D
        {}

        protected struct Sampler2D
        {}

        protected struct SamplerState
        {}

        protected struct DynamicDimensionVector
        {}

        protected enum BindChannel
        {
            None,
            Normal,
            Tangent,
            Bitangent,
            MeshUV0,
            MeshUV1,
            MeshUV2,
            MeshUV3,
            ScreenPosition,
            ViewDirection,
            WorldPosition,
            VertexColor,
            ViewDirectionTangentSpace
        }

        private static string BindChannelToShaderName(BindChannel channel)
        {
            switch (channel)
            {
                case BindChannel.None:
                    return "ERROR!";
                case BindChannel.Normal:
                    return ShaderGeneratorNames.WorldSpaceNormal;
                case BindChannel.Tangent:
                    return ShaderGeneratorNames.WorldSpaceTangent;
                case BindChannel.Bitangent:
                    return ShaderGeneratorNames.WorldSpaceBitangent;
                case BindChannel.MeshUV0:
                    return ShaderGeneratorNames.GetUVName(UVChannel.uv0);
                case BindChannel.MeshUV1:
                    return ShaderGeneratorNames.GetUVName(UVChannel.uv1);
                case BindChannel.MeshUV2:
                    return ShaderGeneratorNames.GetUVName(UVChannel.uv2);
                case BindChannel.MeshUV3:
                    return ShaderGeneratorNames.GetUVName(UVChannel.uv3);
                case BindChannel.ScreenPosition:
                    return ShaderGeneratorNames.ScreenPosition;
                case BindChannel.ViewDirection:
                    return ShaderGeneratorNames.WorldSpaceViewDirection;
                case BindChannel.WorldPosition:
                    return ShaderGeneratorNames.WorldSpacePosition;
                case BindChannel.VertexColor:
                    return ShaderGeneratorNames.VertexColor;
                case BindChannel.ViewDirectionTangentSpace:
                    return ShaderGeneratorNames.TangentSpaceViewDirection;
                default:
                    throw new ArgumentOutOfRangeException("channel", channel, null);
            }
        }

        [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
        protected class BindAttribute : Attribute
        {
            public BindChannel channel { get; private set; }

            public BindAttribute(BindChannel mChannel)
            {
                channel = mChannel;
            }
        }

        protected abstract MethodInfo GetFunctionToConvert();

        private static SlotValueType ConvertTypeToSlotValueType(ParameterInfo p)
        {
            Type t = p.ParameterType;
            if (p.ParameterType.IsByRef)
                t = p.ParameterType.GetElementType();

            if (t == typeof(ShaderSingle))
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
            if (t == typeof(Sampler2D))
            {
                return SlotValueType.Sampler2D;
            }
            if (t == typeof(SamplerState))
            {
                return SlotValueType.SamplerState;
            }
            if (t == typeof(DynamicDimensionVector))
            {
                return SlotValueType.Dynamic;
            }
            throw new ArgumentException("Unsupported type " + t);
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            var method = GetFunctionToConvert();
            var returnType = method.ReturnType;

            if (returnType != typeof(void))
                return;

            var inStart = 1;
            var outStart = -1;
            List<MaterialSlot> slots = new List<MaterialSlot>();
            foreach (var par in method.GetParameters())
            {
                if (par.IsOut)
                {
                    slots.Add(new MaterialSlot(outStart, par.Name, par.Name, SlotType.Output,
                        ConvertTypeToSlotValueType(par), Vector4.zero));
                    m_ParamMap.Add(new KeyValuePair<int, ParameterInfo>(outStart, par));
                    outStart--;
                }
                else
                {
                    slots.Add(new MaterialSlot(inStart, par.Name, par.Name, SlotType.Input,
                        ConvertTypeToSlotValueType(par), Vector4.zero));
                    m_ParamMap.Add(new KeyValuePair<int, ParameterInfo>(inStart, par));
                    inStart++;
                }
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
                visitor.AddShaderChunk(GetParamName(outSlot) + " " + GetVariableNameForSlot(outSlot.id) + ";", true);
            }

            string call = GetFunctionName() + "(";
            bool first = true;
            foreach (var arg in GetSlots<MaterialSlot>())
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
                        var info = m_ParamMap.Where(x => x.Key == arg.id).Select(x => x.Value).FirstOrDefault();
                        if (info != null)
                        {
                            var bindingInfo = GetSlotBinding(arg, info);
                            if (bindingInfo != BindChannel.None)
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

        private string GetParamName(MaterialSlot slot)
        {
            return ConvertConcreteSlotValueTypeToString(precision, slot.concreteValueType);
        }

        private string GetFunctionName()
        {
            return GetFunctionToConvert().Name + "_" + precision;
        }

        private string GetFunctionHeader()
        {
            string header = "void " + GetFunctionName() + "(";

            var first = true;
            foreach (var kvp in m_ParamMap)
            {
                if (!first)
                    header += ", ";

                first = false;

                var slot = FindSlot<MaterialSlot>(kvp.Key);
                if (slot == null)
                    throw new ArgumentException("something is wrong");

                if (kvp.Value.IsOut)
                    header += "out ";
                header += GetParamName(slot) + " " + kvp.Value.Name;
            }

            header += ")";
            return header;
        }

        protected abstract string GetFunctionBody();

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            string function = GetFunctionHeader() + GetFunctionBody();
            visitor.AddShaderChunk(function, true);
        }

        private bool NodeRequiresBinding(BindChannel channel)
        {
            foreach (var kvp in m_ParamMap)
            {
                var slot = FindSlot<MaterialSlot>(kvp.Key);
                if (slot == null)
                    throw new ArgumentException("something is wrong");

                if (SlotRequiresBinding(channel, slot, kvp.Value))
                    return true;
            }
            return false;
        }

        private bool SlotRequiresBinding(BindChannel channel, [NotNull]MaterialSlot slot, [NotNull]ParameterInfo info)
        {
            if (slot.isOutputSlot)
                return false;

            var inEdges = owner.GetEdges(slot.slotReference);
            if (inEdges.Any())
                return false;

            foreach (var attr in info.GetCustomAttributes(typeof(BindAttribute), false).OfType<BindAttribute>())
            {
                if (attr.channel == channel)
                    return true;
            }
            return false;
        }

        private static BindChannel GetSlotBinding([NotNull]MaterialSlot slot, [NotNull]ParameterInfo info)
        {
            if (slot.isOutputSlot)
                return BindChannel.None;

            var attrs = info.GetCustomAttributes(typeof(BindAttribute), false).OfType<BindAttribute>().ToList();
            if (attrs.Count > 0)
                return attrs.First().channel;
            return BindChannel.None;
        }

        public bool RequiresNormal()
        {
            return NodeRequiresBinding(BindChannel.Normal);
        }

        public bool RequiresMeshUV(UVChannel channel)
        {
            switch (channel)
            {
                case UVChannel.uv0:
                    return NodeRequiresBinding(BindChannel.MeshUV0);
                case UVChannel.uv1:
                    return NodeRequiresBinding(BindChannel.MeshUV1);
                case UVChannel.uv2:
                    return NodeRequiresBinding(BindChannel.MeshUV2);
                case UVChannel.uv3:
                    return NodeRequiresBinding(BindChannel.MeshUV3);
                default:
                    throw new ArgumentOutOfRangeException("channel", channel, null);
            }
        }

        public bool RequiresScreenPosition()
        {
            return NodeRequiresBinding(BindChannel.ScreenPosition);
        }

        public bool RequiresViewDirection()
        {
            return NodeRequiresBinding(BindChannel.ViewDirection);
        }

        public bool RequiresViewDirectionTangentSpace()
        {

            return NodeRequiresBinding(BindChannel.ViewDirectionTangentSpace);
        }

        public bool RequiresWorldPosition()
        {
            return NodeRequiresBinding(BindChannel.WorldPosition);
        }

        public bool RequiresTangent()
        {
            return NodeRequiresBinding(BindChannel.Tangent);
        }

        public bool RequiresBitangent()
        {
            return NodeRequiresBinding(BindChannel.Bitangent);
        }

        public bool RequiresVertexColor()
        {

            return NodeRequiresBinding(BindChannel.VertexColor);
        }

        /*
        public string outputDimension
        {
            get { return ConvertConcreteSlotValueTypeToString(FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType); }
        }
        public string inputDimension
        {
            get { return ConvertConcreteSlotValueTypeToString(FindInputSlot<MaterialSlot>(InputSlotId).concreteValueType); }
        }*/
    }
}
