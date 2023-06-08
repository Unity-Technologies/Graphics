Shader "Hidden/HDRP/Sky/HDRISky"
{
    HLSLINCLUDE

    #pragma vertex Vert

    #pragma editor_sync_compilation
    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

    #define LIGHTLOOP_DISABLE_TILE_AND_CLUSTER

    #pragma multi_compile_local_fragment _ DISTORTION_PROCEDURAL DISTORTION_FLOWMAP

    #pragma multi_compile_fragment _ DEBUG_DISPLAY
    #pragma multi_compile_fragment PUNCTUAL_SHADOW_LOW PUNCTUAL_SHADOW_MEDIUM PUNCTUAL_SHADOW_HIGH
    #pragma multi_compile_fragment DIRECTIONAL_SHADOW_LOW DIRECTIONAL_SHADOW_MEDIUM DIRECTIONAL_SHADOW_HIGH
    #pragma multi_compile_fragment AREA_SHADOW_MEDIUM AREA_SHADOW_HIGH

    #pragma multi_compile_fragment USE_FPTL_LIGHTLIST USE_CLUSTERED_LIGHTLIST

    #define ATTRIBUTES_NEED_NORMAL
    #define ATTRIBUTES_NEED_TANGENT
    #define VARYINGS_NEED_POSITION_WS
    #define VARYINGS_NEED_TANGENT_TO_WORLD

    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"

    #define SHADERPASS SHADERPASS_FORWARD_UNLIT

    #define HAS_LIGHTLOOP

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonLighting.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Sky/SkyUtils.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SDF2D.hlsl"

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariablesFunctions.hlsl"

    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/HDShadow.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/PunctualLightCommon.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/HDShadowLoop.hlsl"

    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/NormalBuffer.hlsl"

    TEXTURECUBE(_Cubemap);
    SAMPLER(sampler_Cubemap);
    float4 _Cubemap_HDR;

    TEXTURE2D(_Flowmap);
    SAMPLER(sampler_Flowmap);

    float4 _SkyParam; // x exposure, y multiplier, zw rotation (cosPhi and sinPhi)
    float4 _BackplateParameters0; // xy: scale, z: groundLevel, w: projectionDistance
    float4 _BackplateParameters1; // x: BackplateType, y: BlendAmount, zw: backplate rotation (cosPhi_plate, sinPhi_plate)
    float4 _BackplateParameters2; // xy: BackplateTextureRotation (cos/sin), zw: Backplate Texture Offset
    float3 _BackplateShadowTint;  // xyz: ShadowTint
    uint   _BackplateShadowFilter;

    float4 _FlowmapParam; // x upper hemisphere only, y scroll factor, zw scroll direction (cosPhi and sinPhi)

    #define _Intensity          _SkyParam.x
    #define _CosPhi             _SkyParam.z
    #define _SinPhi             _SkyParam.w
    #define _CosSinPhi          _SkyParam.zw
    #define _Scales             _BackplateParameters0.xy
    #define _ScaleX             _BackplateParameters0.x
    #define _ScaleY             _BackplateParameters0.y
    #define _GroundLevel        _BackplateParameters0.z
    #define _ProjectionDistance _BackplateParameters0.w
    #define _BackplateType      _BackplateParameters1.x
    #define _BlendAmount        _BackplateParameters1.y
    #define _CosPhiPlate        _BackplateParameters1.z
    #define _SinPhiPlate        _BackplateParameters1.w
    #define _CosSinPhiPlate     _BackplateParameters1.zw
    #define _CosPhiPlateTex     _BackplateParameters2.x
    #define _SinPhiPlateTex     _BackplateParameters2.y
    #define _CosSinPhiPlateTex  _BackplateParameters2.xy
    #define _OffsetTexX         _BackplateParameters2.z
    #define _OffsetTexY         _BackplateParameters2.w
    #define _OffsetTex          _BackplateParameters2.zw
    #define _ShadowTint         _BackplateShadowTint.rgb
    #define _ShadowFilter       _BackplateShadowFilter
    #define _UpperHemisphere    _FlowmapParam.x
    #define _ScrollFactor       _FlowmapParam.y
    #define _ScrollDirection    _FlowmapParam.zw

    struct Attributes
    {
        uint vertexID : SV_VertexID;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        UNITY_VERTEX_OUTPUT_STEREO
    };

    Varyings Vert(Attributes input)
    {
        Varyings output;
        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
        output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID, UNITY_RAW_FAR_CLIP_VALUE);
        return output;
    }

    // TODO: cf. dir.y == 0
    float3 GetPositionOnInfinitePlane(float3 dir)
    {
        const float alpha = (_GroundLevel - _WorldSpaceCameraPos.y)/dir.y;

        return _WorldSpaceCameraPos + alpha*dir;
    }

    float GetSDF(out float scale, float2 position)
    {
        position = RotationUp(float3(position.x, 0.0f, position.y), _CosSinPhiPlate).xz;
        if (_BackplateType == 0) // Circle
        {
            scale = _ScaleX;
            return CircleSDF(position, _ScaleX);
        }
        else if (_BackplateType == 1) // Rectangle
        {
            scale = min(_ScaleX, _ScaleY);
            return RectangleSDF(position, _Scales);
        }
        else if (_BackplateType == 2) // Ellipse
        {
            scale = min(_ScaleX, _ScaleY);
            return EllipseSDF(position, _Scales);
        }
        else //if (_BackplateType == 3) // Infinite backplate
        {
            scale = FLT_MAX;
            return CircleSDF(position, scale);
        }
    }

    void IsBackplateCommon(out float sdf, out float localScale, out float3 positionOnBackplatePlane, float3 dir)
    {
        positionOnBackplatePlane = GetPositionOnInfinitePlane(dir);

        sdf = GetSDF(localScale, positionOnBackplatePlane.xz);
    }

    bool IsHit(float sdf, float dirY)
    {
        return sdf < 0.0f && dirY < 0.0f && _WorldSpaceCameraPos.y > _GroundLevel;
    }

    bool IsBackplateHit(out float3 positionOnBackplatePlane, float3 dir)
    {
        float sdf;
        float localScale;
        IsBackplateCommon(sdf, localScale, positionOnBackplatePlane, dir);

        return IsHit(sdf, dir.y);
    }

    bool IsBackplateHitWithBlend(out float3 positionOnBackplatePlane, out float blend, float3 dir)
    {
        float sdf;
        float localScale;
        IsBackplateCommon(sdf, localScale, positionOnBackplatePlane, dir);

        blend = smoothstep(0.0f, localScale*_BlendAmount, max(-sdf, 0));

        return IsHit(sdf, dir.y);
    }

    float3 GetSkyColor(float3 dir)
    {
#if defined(DISTORTION_PROCEDURAL) || defined(DISTORTION_FLOWMAP)
        if (dir.y >= 0 || !_UpperHemisphere)
        {
            float2 alpha = frac(float2(_ScrollFactor, _ScrollFactor + 0.5)) - 0.5;

#ifdef DISTORTION_FLOWMAP
            float3 tangent = normalize(cross(dir, float3(0.0, 1.0, 0.0)));
            float3 bitangent = cross(tangent, dir);

            float3 windDir = RotationUp(dir, _ScrollDirection);
            float2 flow = SAMPLE_TEXTURE2D_LOD(_Flowmap, sampler_Flowmap, GetLatLongCoords(windDir, _UpperHemisphere), 0).rg * 2.0 - 1.0;

            float3 dd = flow.x * tangent + flow.y * bitangent;
#else
            float3 windDir = float3(_ScrollDirection.x, 0.0f, _ScrollDirection.y);
            float3 dd = windDir*sin(dir.y*PI*0.5);
#endif

            // Sample twice
            float3 color1 = DecodeHDREnvironment(SAMPLE_TEXTURECUBE_LOD(_Cubemap, sampler_Cubemap, dir + alpha.x * dd, 0), _Cubemap_HDR);
            float3 color2 = DecodeHDREnvironment(SAMPLE_TEXTURECUBE_LOD(_Cubemap, sampler_Cubemap, dir + alpha.y * dd, 0), _Cubemap_HDR);

            // Blend color samples
            return lerp(color1, color2, abs(2.0 * alpha.x));
        }
        else
#endif

        return DecodeHDREnvironment(SAMPLE_TEXTURECUBE_LOD(_Cubemap, sampler_Cubemap, dir, 0), _Cubemap_HDR);
    }

    float4 GetColorWithRotation(float3 dir, float exposure, float2 cos_sin)
    {
        dir = RotationUp(dir, cos_sin);

        float3 skyColor = GetSkyColor(dir)*_Intensity*exposure;
        skyColor = ClampToFloat16Max(skyColor);

        return float4(skyColor, 1.0);
    }

    float4 RenderSky(Varyings input, float exposure)
    {
        float3 viewDirWS = GetSkyViewDirWS(input.positionCS.xy);

        // Reverse it to point into the scene
        float3 dir = -viewDirWS;

        return GetColorWithRotation(dir, exposure, _CosSinPhi);
    }

    float3 GetScreenSpaceAmbientOcclusionForBackplate(float2 positionSS, float NdotV, float perceptualRoughness)
    {
        float indirectAmbientOcclusion = 1.0 - LOAD_TEXTURE2D_X(_AmbientOcclusionTexture, positionSS).x;
        float directAmbientOcclusion   = lerp(1.0, indirectAmbientOcclusion, _AmbientOcclusionParam.w);

        return lerp(_AmbientOcclusionParam.rgb, 1.0, directAmbientOcclusion);
    }

    float4 RenderSkyWithBackplate(Varyings input, float3 positionOnBackplate, float exposure, float3 originalDir, float blend, float depth)
    {
        // Reverse it to point into the scene
        float3 offset = RotationUp(float3(_OffsetTexX, 0.0, _OffsetTexY), _CosSinPhiPlate);
        float3 dir    = positionOnBackplate - float3(0.0, _ProjectionDistance + _GroundLevel, 0.0) + offset; // No need for normalization

        PositionInputs posInput = GetPositionInput(input.positionCS.xy, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);

        HDShadowContext shadowContext = InitShadowContext();
        float shadow;
        float3 shadow3;
        ShadowLoopMin(shadowContext, posInput, float3(0.0, 1.0, 0.0), _ShadowFilter, RENDERING_LAYERS_MASK, shadow3);
        shadow = dot(shadow3, float3(1.0 / 3.0, 1.0 / 3.0, 1.0 / 3.0));

        float3 shadowColor = ComputeShadowColor(shadow, _ShadowTint, 0.0);

        float3 output = lerp(              GetColorWithRotation(originalDir,                         exposure, _CosSinPhi).rgb,
                             shadowColor * GetColorWithRotation(RotationUp(dir, _CosSinPhiPlateTex), exposure, _CosSinPhi).rgb, blend);

        float3 ao = GetScreenSpaceAmbientOcclusionForBackplate(posInput.positionSS, originalDir.z, 1.0);

        return float4(ao * output, exposure);
    }

    float4 FragBaking(Varyings input) : SV_Target
    {
        return RenderSky(input, 1.0);
    }

    float4 FragRender(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        return RenderSky(input, GetCurrentExposureMultiplier());
    }

    float4 RenderBackplate(Varyings input, float exposure)
    {
        float3 viewDirWS = -GetSkyViewDirWS(input.positionCS.xy);
        float3 finalPos;
        float depth;
        float blend;
        if (IsBackplateHitWithBlend(finalPos, blend, viewDirWS))
        {
            depth = ComputeNormalizedDeviceCoordinatesWithZ(finalPos - _WorldSpaceCameraPos, UNITY_MATRIX_VP).z;
        }
        else
        {
            depth = UNITY_RAW_FAR_CLIP_VALUE;
        }

        float curDepth = LoadCameraDepth(input.positionCS.xy);

        if (curDepth > depth)
            discard;

        float4 results = 0; // Warning
        if (curDepth == UNITY_RAW_FAR_CLIP_VALUE)
            results = RenderSky(input, exposure);
        else if (curDepth <= depth)
            results = RenderSkyWithBackplate(input, finalPos, exposure, viewDirWS, blend, depth);

        return results;
    }

    float4 FragRenderBackplate(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        return RenderBackplate(input, GetCurrentExposureMultiplier());
    }

    float GetDepthWithBackplate(Varyings input)
    {
        float3 viewDirWS = -GetSkyViewDirWS(input.positionCS.xy);
        float3 finalPos;
        float depth;
        if (IsBackplateHit(finalPos, viewDirWS))
        {
            depth = ComputeNormalizedDeviceCoordinatesWithZ(finalPos - _WorldSpaceCameraPos, UNITY_MATRIX_VP).z;
        }
        else
        {
            depth = UNITY_RAW_FAR_CLIP_VALUE;
        }

        return depth;
    }

    float4 FragRenderBackplateDepth(Varyings input, out float depth : SV_Depth) : SV_Target0
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        depth = GetDepthWithBackplate(input);

        NormalData normalData;
        normalData.normalWS            = float3(0, 1, 0);
        normalData.perceptualRoughness = 1.0f;

        float4 gbufferNormal = 0;

        if (depth != UNITY_RAW_FAR_CLIP_VALUE)
            EncodeIntoNormalBuffer(normalData, gbufferNormal);

        return gbufferNormal;
    }

    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        // Regular HDRI Sky
        // For cubemap
        Pass
        {
            Name "FragBaking"
            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
                #pragma fragment FragBaking
            ENDHLSL
        }

        // For fullscreen Sky
        Pass
        {
            Name "FragRender"
            ZWrite Off
            ZTest LEqual
            Blend Off
            Cull Off

            HLSLPROGRAM
                #pragma fragment FragRender
            ENDHLSL
        }

        // For fullscreen Sky with Backplate
        Pass
        {
            Name "FragRenderBackplate"
            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
                #pragma fragment FragRenderBackplate
            ENDHLSL
        }

        // DepthOnly For fullscreen Sky with Backplate
        Pass
        {
            Name "FragRenderBackplateDepth"
            ZWrite On
            ZTest LEqual
            Blend Off
            Cull Off

            HLSLPROGRAM
                #pragma fragment FragRenderBackplateDepth
            ENDHLSL
        }
    }
    Fallback Off
}
