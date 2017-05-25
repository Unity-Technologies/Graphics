using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("UV/ParallaxOcclusionMapping")]
    public class ParallaxOcclusionMappingNode : 
        AbstractMaterialNode, 
        IGeneratesBodyCode, 
        IGeneratesFunction, 
        IMayRequireMeshUV,
        IMayRequireViewDirection,
        IMayRequireNormal,
        IMayRequireViewDirectionTangentSpace
    {
        protected const string kTextureSlotShaderName = "Texture";
        protected const string kOutputSlotShaderName = "UV";
        
        public const int TextureSlotId = 0;             // 'tex'
        public const int OutputSlotId = 1;

        public override bool hasPreview
        {
            get { return true; }
        }

        public override PreviewMode previewMode
        {
            get
            {
                return PreviewMode.Preview3D;
            }
        }

        public ParallaxOcclusionMappingNode()
        {
            name = "ParallaxOcclusionMapping";
            UpdateNodeAfterDeserialization();
        }

        public string GetFunctionName()
        {
            return "unity_parallax_occlusion_mapping_" + precision;
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(GetTextureSlot());
            AddSlot(GetOutputSlot());
            RemoveSlotsNameNotMatching(validSlots);
        }

        protected int[] validSlots
        {
            get { return new[] { TextureSlotId, OutputSlotId }; }
        }
           
        protected virtual MaterialSlot GetTextureSlot()
        {
            return new MaterialSlot(TextureSlotId, GetTextureSlotName(), kTextureSlotShaderName, SlotType.Input, SlotValueType.sampler2D, Vector4.zero);
        }

        protected virtual MaterialSlot GetOutputSlot()
        {
            return new MaterialSlot(OutputSlotId, GetOutputSlotName(), kOutputSlotShaderName, SlotType.Output, SlotValueType.Vector2, Vector4.zero);
        }

        protected virtual string GetTextureSlotName()
        {
            return kTextureSlotShaderName;
        }
        
        protected virtual string GetOutputSlotName()
        {
            return kOutputSlotShaderName;
        }

        protected virtual string GetFunctionPrototype(
            string tex, string UVs, string viewTangentSpace, string worldSpaceNormal, string worldSpaceViewDirection)
        {
            return "inline " + precision + "2 " + GetFunctionName() + " (" +
                "sampler2D " + tex + ", " +
                precision + "2 " + UVs + ", " +
                precision + "3 " + viewTangentSpace + ", " +
                precision + "3 " + worldSpaceNormal + ", " +
                precision + "3 " + worldSpaceViewDirection + ")";
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            NodeUtils.SlotConfigurationExceptionIfBadConfiguration(
                this, 
                new[] { TextureSlotId }, 
                new[] { OutputSlotId });
            string textureValue = GetSlotValue(TextureSlotId, generationMode);

            visitor.AddShaderChunk(precision + "2 " + GetVariableNameForSlot(OutputSlotId) + " = " + 
                GetFunctionCallBody(textureValue) + ";", true);
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputString = new ShaderGenerator();
            outputString.AddShaderChunk(
                    GetFunctionPrototype("tex", "UVs", "viewTangentSpace", "worldSpaceNormal", "worldSpaceViewDirection" ), 
                    false);
            
            outputString.AddShaderChunk("{", false);
            outputString.Indent();

            outputString.AddShaderChunk(precision + "2 " + "height_map_dimensions = " + precision + "2" + "(256.0f, 256.0f);      //HARDCODE", false);
            //height_map.tex.GetDimensions(height_map_dimensions.x, height_map_dimensions.y);

            outputString.AddShaderChunk(precision + "2 texcoord= UVs;", false);

            // Compute the current gradients:
            outputString.AddShaderChunk(precision + "2 " + " texcoords_per_size = texcoord * height_map_dimensions;", false);

            // Compute all 4 derivatives in x and y in a single instruction to optimize:
            outputString.AddShaderChunk("float2 dx, dy;", false);

            outputString.AddShaderChunk(" float4 temp_ddx = ddx(float4(texcoords_per_size, texcoord));", false);

            outputString.AddShaderChunk("dx.xy = temp_ddx.zw;", false);

            outputString.AddShaderChunk("float4 temp_ddy = ddy(float4(texcoords_per_size, texcoord));", false);

            outputString.AddShaderChunk("dy.xy = temp_ddy.zw;", false);

            // Start the current sample located at the input texture coordinate, which would correspond
            // to computing a bump mapping result:
            outputString.AddShaderChunk(precision + "2 " + "result_texcoord = texcoord;", false);

            outputString.AddShaderChunk("float height_scale_value = 1.0f;", false);
            outputString.AddShaderChunk("float height_scale_adjust = 0.02f;", false);


            outputString.AddShaderChunk("float per_pixel_height_scale_value = height_scale_value * height_scale_adjust;", false);

            outputString.AddShaderChunk("if (per_pixel_height_scale_value > 0)", false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();

            // Parallax occlusion mapping offset computation 
            //--------------

            // Utilize dynamic flow control to change the number of samples per ray 
            // depending on the viewing angle for the surface. Oblique angles require 
            // smaller step sizes to achieve more accurate precision for computing displacement.
            // We express the sampling rate as a linear function of the angle between 
            // the geometric normal and the view direction ray:
            outputString.AddShaderChunk("float max_samples = 30.0f;", false);
            outputString.AddShaderChunk("float min_samples = 4.0f;", false);

            outputString.AddShaderChunk("float view_dot_normal= dot(worldSpaceNormal, worldSpaceViewDirection);", false);

            outputString.AddShaderChunk("int number_of_steps = (int)lerp(max_samples, min_samples, saturate(view_dot_normal));", false);

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

            outputString.AddShaderChunk("float current_height = 0.0;", false);
            outputString.AddShaderChunk("float step_size = 1.0 / (float)number_of_steps;", false);

            outputString.AddShaderChunk("float previous_height = 1.0;", false);
            outputString.AddShaderChunk("float next_height = 0.0;", false);


            outputString.AddShaderChunk("int step_index = 0;", false);

            // Optimization: this should move to vertex shader, however, we compute it here for simplicity of
            // integration into our shaders for now.
            outputString.AddShaderChunk("float3 normalized_view_dir_in_tangent_space = normalize(viewTangentSpace.xyz);", false);

            // Compute initial parallax displacement direction:
            outputString.AddShaderChunk("float2 parallax_direction = normalize(viewTangentSpace.xy);", false);

            // The length of this vector determines the furthest amount of displacement:
            outputString.AddShaderChunk("float parallax_direction_length = length(normalized_view_dir_in_tangent_space);", false);

            outputString.AddShaderChunk(
                "float max_parallax_amount = sqrt(parallax_direction_length * parallax_direction_length - viewTangentSpace.z * viewTangentSpace.z) / viewTangentSpace.z;", false);

            // Compute the actual reverse parallax displacement vector:
            outputString.AddShaderChunk("float2 parallax_offset_in_tangent_space = parallax_direction * max_parallax_amount;", false);

            // Need to scale the amount of displacement to account for different height ranges
            // in height maps. This is controlled by an artist-editable parameter:
            outputString.AddShaderChunk("parallax_offset_in_tangent_space *= per_pixel_height_scale_value;", false);

            outputString.AddShaderChunk("float2 texcoord_offset_per_step = step_size * parallax_offset_in_tangent_space;", false);

            outputString.AddShaderChunk(precision + "2 " + "current_texcoord_offset = texcoord;", false);
            outputString.AddShaderChunk("float current_bound = 1.0;", false);

            outputString.AddShaderChunk("float current_parallax_amount = 0.0;", false);

            outputString.AddShaderChunk("float2 pt1 = 0;", false);
            outputString.AddShaderChunk("float2 pt2 = 0;", false);


            outputString.AddShaderChunk(precision + "2 " + "temp_texcoord_offset = 0;", false);

            outputString.AddShaderChunk("while (step_index < number_of_steps)", false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();

            outputString.AddShaderChunk("current_texcoord_offset -= texcoord_offset_per_step;", false);

            // Sample height map which in this case is stored in the alpha channel of the normal map:
            outputString.AddShaderChunk("current_height = tex2Dgrad(tex, current_texcoord_offset, dx, dy).r;", false);

            outputString.AddShaderChunk("current_bound -= step_size;", false);

            outputString.AddShaderChunk("if (current_height > current_bound)", false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();

            outputString.AddShaderChunk("pt1 = float2(current_bound, current_height);", false);

            outputString.AddShaderChunk("pt2 = float2(current_bound + step_size, previous_height);", false);


            outputString.AddShaderChunk("temp_texcoord_offset = current_texcoord_offset - texcoord_offset_per_step;", false);
            
            outputString.AddShaderChunk("step_index = number_of_steps + 1;", false);

            outputString.Deindent();
            outputString.AddShaderChunk("}", false);
            outputString.AddShaderChunk("else", false);

            outputString.AddShaderChunk("{", false);
            outputString.Indent();

            outputString.AddShaderChunk("step_index++;", false);
            outputString.AddShaderChunk("previous_height = current_height;", false);

            outputString.Deindent();
            outputString.AddShaderChunk("}", false);

            outputString.Deindent();
            outputString.AddShaderChunk("}   // End of while ( step_index < number_of_steps)", false);


            outputString.AddShaderChunk("float delta2 = pt2.x - pt2.y;", false);
            outputString.AddShaderChunk("float delta1 = pt1.x - pt1.y;", false);

            outputString.AddShaderChunk("float denominator = delta2 - delta1;", false);

            // SM 3.0 and above requires a check for divide by zero since that operation
            // will generate an 'Inf' number instead of 0
            outputString.AddShaderChunk("if (denominator== 0.0f) ", false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();
            outputString.AddShaderChunk("current_parallax_amount= 0.0f;", false);
            outputString.Deindent();
            outputString.AddShaderChunk("}", false);
            outputString.AddShaderChunk("else", false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();
            outputString.AddShaderChunk("current_parallax_amount= (pt1.x* delta2 - pt2.x* delta1) / denominator;", false);
            outputString.Deindent();
            outputString.AddShaderChunk("}", false);

            outputString.AddShaderChunk("float2 parallax_offset = parallax_offset_in_tangent_space * (1.0f - current_parallax_amount);", false);

            // The computed texture offset for the displaced point on the pseudo-extruded surface:
            outputString.AddShaderChunk("float2 parallaxed_texcoord = texcoord - parallax_offset;", false);

            outputString.AddShaderChunk("result_texcoord = parallaxed_texcoord;", false);
            outputString.Deindent();
            outputString.AddShaderChunk("}", false);
            
            outputString.Deindent();
					
            outputString.AddShaderChunk("return result_texcoord;", false);
            
            outputString.Deindent();
            outputString.AddShaderChunk("}", false);

            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }

        protected virtual string GetFunctionCallBody(string texValue)
        {
            var channel = UVChannel.uv0;
            
            return GetFunctionName() + " (" +
                texValue + ", " +
                channel.GetUVName() + ", " +
                ShaderGeneratorNames.TangentSpaceViewDirection + ", " +
                ShaderGeneratorNames.WorldSpaceNormal + ", " +
                ShaderGeneratorNames.WorldSpaceViewDirection + ")";
        }

        public bool RequiresMeshUV(UVChannel channel)
        {
            return channel == UVChannel.uv0;
        }
        public bool RequiresViewDirectionTangentSpace()
        {
            return true;
        }
        public bool RequiresNormal()
        {
            return true;
        }
        public bool RequiresViewDirection()
        {
            return true;
        }
    }
}

