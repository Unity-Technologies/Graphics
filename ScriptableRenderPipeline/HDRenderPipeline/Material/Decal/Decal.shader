Shader "HDRenderPipeline/Decal"
{
    Properties
    {      
        _BaseColorMap("BaseColorMap", 2D) = "white" {}     
    }

    HLSLINCLUDE

    #pragma target 4.5
    #pragma only_renderers d3d11 ps4 metal // TEMP: until we go futher in dev
    //#pragma enable_d3d11_debug_symbols

    //-------------------------------------------------------------------------------------
    // Variant
    //-------------------------------------------------------------------------------------
/*
    #pragma shader_feature _ALPHATEST_ON
    #pragma shader_feature _DEPTHOFFSET_ON
    #pragma shader_feature _DOUBLESIDED_ON
    #pragma shader_feature _ _VERTEX_DISPLACEMENT _PIXEL_DISPLACEMENT
    #pragma shader_feature _VERTEX_DISPLACEMENT_LOCK_OBJECT_SCALE
    #pragma shader_feature _DISPLACEMENT_LOCK_TILING_SCALE
    #pragma shader_feature _PIXEL_DISPLACEMENT_LOCK_OBJECT_SCALE
    #pragma shader_feature _VERTEX_WIND
    #pragma shader_feature _ _REFRACTION_PLANE _REFRACTION_SPHERE

    #pragma shader_feature _ _MAPPING_PLANAR _MAPPING_TRIPLANAR
    #pragma shader_feature _NORMALMAP_TANGENT_SPACE
    #pragma shader_feature _ _REQUIRE_UV2 _REQUIRE_UV3

    #pragma shader_feature _NORMALMAP
    #pragma shader_feature _MASKMAP
    #pragma shader_feature _BENTNORMALMAP
    #pragma shader_feature _EMISSIVE_COLOR_MAP
    #pragma shader_feature _ENABLESPECULAROCCLUSION
    #pragma shader_feature _HEIGHTMAP
    #pragma shader_feature _TANGENTMAP
    #pragma shader_feature _ANISOTROPYMAP
    #pragma shader_feature _DETAIL_MAP
    #pragma shader_feature _SUBSURFACE_RADIUS_MAP
    #pragma shader_feature _THICKNESSMAP
    #pragma shader_feature _SPECULARCOLORMAP

    // Keyword for transparent
    #pragma shader_feature _SURFACE_TYPE_TRANSPARENT
    #pragma shader_feature _ _BLENDMODE_ALPHA _BLENDMODE_ADD _BLENDMODE_MULTIPLY _BLENDMODE_PRE_MULTIPLY
    #pragma shader_feature _BLENDMODE_PRESERVE_SPECULAR_LIGHTING
    #pragma shader_feature _ENABLE_FOG_ON_TRANSPARENT

    // MaterialId are used as shader feature to allow compiler to optimize properly
    // Note _MATID_STANDARD is not define as there is always the default case "_". We assign default as _MATID_STANDARD, so we never test _MATID_STANDARD
    #pragma shader_feature _ _MATID_SSS _MATID_ANISO _MATID_SPECULAR _MATID_CLEARCOAT

    #pragma multi_compile LIGHTMAP_OFF LIGHTMAP_ON
    #pragma multi_compile DIRLIGHTMAP_OFF DIRLIGHTMAP_COMBINED
    #pragma multi_compile DYNAMICLIGHTMAP_OFF DYNAMICLIGHTMAP_ON
    // enable dithering LOD crossfade
    #pragma multi_compile _ LOD_FADE_CROSSFADE
    // TODO: We should have this keyword only if VelocityInGBuffer is enable, how to do that ?
    //#pragma multi_compile VELOCITYOUTPUT_OFF VELOCITYOUTPUT_ON
*/


    //-------------------------------------------------------------------------------------
    // Define
    //-------------------------------------------------------------------------------------
/*
    #define UNITY_MATERIAL_LIT // Need to be define before including Material.hlsl
    // Use surface gradient normal mapping as it handle correctly triplanar normal mapping and multiple UVSet
    #define SURFACE_GRADIENT
    // This shader support vertex modification
    #define HAVE_VERTEX_MODIFICATION
*/
    //-------------------------------------------------------------------------------------
    // Include
    //-------------------------------------------------------------------------------------


    #include "../../../Core/ShaderLibrary/Common.hlsl"
    #include "../../../Core/ShaderLibrary/Wind.hlsl"
    #include "../../ShaderPass/FragInputs.hlsl"
    #include "../../ShaderPass/ShaderPass.cs.hlsl"


    //-------------------------------------------------------------------------------------
    // variable declaration
    //-------------------------------------------------------------------------------------

    #include "DecalProperties.hlsl"

    // All our shaders use same name for entry point
    #pragma vertex Vert
    #pragma fragment Frag

    ENDHLSL

    SubShader
    {
        Pass
        {
            Name "DBuffer"  // Name is not used
            Tags { "LightMode" = "dBuffer" } // This will be only for opaque object based on the RenderQueue index

            Cull [_CullMode]
			ZWrite Off
			ZTest [_ZTestMode]

            Stencil
            {
                Ref  [_StencilRef]
                Comp Always
                Pass Replace				
            }

            HLSLPROGRAM

            #include "../../ShaderVariables.hlsl"
			#include "OutputBuffers.hlsl"
			#include "Decal.hlsl"
//            #include "../../Material/Material.hlsl"
//            #include "ShaderPass/LitSharePass.hlsl"
//            #include "LitData.hlsl"
			#include "../../ShaderPass/VaryingMesh.hlsl"
			#include "../../ShaderPass/ShaderPassDBuffer.hlsl"

            ENDHLSL
        }
	}
//    CustomEditor "Experimental.Rendering.HDPipeline.LitGUI"
}
