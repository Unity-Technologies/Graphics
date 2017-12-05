using System.Reflection;
using UnityEngine;

/*namespace UnityEditor.ShaderGraph
{
    [Title("OLD", "ParallaxOcclusionMapping")]
    public class ParallaxOcclusionMappingNode : CodeFunctionNode
    {
        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_POM", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_POM(
            [Slot(1, Binding.None)] Texture2D tex,
            [Slot(2, Binding.None)] Vector1 heightScale,
            [Slot(3, Binding.MeshUV0)] Vector2 UVs,
            [Slot(4, Binding.TangentSpaceViewDirection)] Vector3 viewTangentSpace,
            [Slot(5, Binding.WorldSpaceNormal)] Vector3 worldSpaceNormal,
            [Slot(6, Binding.WorldSpaceViewDirection)] Vector3 worldSpaceViewDirection,
            [Slot(7, Binding.None)] out Vector2 result)
        {
            result = Vector2.zero;

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
}*/
