Shader "Hidden/Universal Render Pipeline/TileDeferred"
{
    Properties {
        _StencilRef ("StencilRef", Int) = 0

        _LitStencilRef ("LitStencilWriteMask", Int) = 0
        _LitStencilReadMask ("LitStencilReadMask", Int) = 0
        _LitStencilWriteMask ("LitStencilWriteMask", Int) = 0

        _SimpleLitStencilRef ("SimpleLitStencilWriteMask", Int) = 0
        _SimpleLitStencilReadMask ("SimpleLitStencilReadMask", Int) = 0
        _SimpleLitStencilWriteMask ("SimpleLitStencilWriteMask", Int) = 0
    }

    HLSLINCLUDE

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/Deferred.hlsl"

    struct TileData
    {
        uint tileID;                 // 2 ushorts
        uint listBitMask;            // 1 uint
        uint relLightOffsetAndCount; // 2 ushorts
        uint unused;
    };

    #if USE_CBUFFER_FOR_TILELIST
        CBUFFER_START(UTileList)
        uint4 _TileList[MAX_TILES_PER_CBUFFER_PATCH * SIZEOF_VEC4_TILEDATA];
        CBUFFER_END

        TileData LoadTileData(int i)
        {
            i *= SIZEOF_VEC4_TILEDATA;
            TileData tileData;
            tileData.tileID                 = _TileList[i][0];
            tileData.listBitMask            = _TileList[i][1];
            tileData.relLightOffsetAndCount = _TileList[i][2];
            return tileData;
        }

    #else
        StructuredBuffer<TileData> _TileList;

        TileData LoadTileData(int i) { return _TileList[i]; }

    #endif

    // Keep in sync with PackTileID().
    uint2 UnpackTileID(uint tileID)
    {
        return uint2(tileID & 0xFFFF, (tileID >> 16) & 0xFFFF);
    }

    uint _TilePixelWidth;
    uint _TilePixelHeight;
    uint _InstanceOffset;

    Texture2D<uint> _TileDepthInfoTexture;

    struct Attributes
    {
        uint vertexID   : SV_VertexID;
        uint instanceID : SV_InstanceID;
    };

    struct Varyings
    {
        noperspective float4 positionCS : SV_POSITION;
        noperspective float4 posCS : TEXCOORD0;
        nointerpolation int2 relLightOffsets : TEXCOORD1;
    };

    #if USE_CBUFFER_FOR_LIGHTLIST
        CBUFFER_START(URelLightList)
        uint4 _RelLightList[MAX_REL_LIGHT_INDICES_PER_CBUFFER_BATCH/4];
        CBUFFER_END

        uint LoadRelLightIndex(uint i) { return _RelLightList[i >> 2][i & 3]; }

    #else
        StructuredBuffer<uint> _RelLightList;

        uint LoadRelLightIndex(uint i) { return _RelLightList[i]; }

    #endif

    Varyings Vertex(Attributes input)
    {
        uint instanceID = _InstanceOffset + input.instanceID;
        TileData tileData = LoadTileData(instanceID);
        uint2 tileCoord = UnpackTileID(tileData.tileID);
        uint geoDepthBitmask = _TileDepthInfoTexture.Load(int3(tileCoord, 0)).x;
        bool shouldDiscard = (geoDepthBitmask & tileData.listBitMask) == 0;

        Varyings output;

        [branch] if (shouldDiscard)
        {
            output.positionCS = float4(-2, -2, -2, 1);
            output.posCS = output.positionCS;
            output.relLightOffsets = 0;
            return output;
        }

        // This handles both "real quad" and "2 triangles" cases: remaps {0, 1, 2, 3, 4, 5} into {0, 1, 2, 3, 0, 2}.
        uint quadIndex = (input.vertexID & 0x03) + (input.vertexID >> 2) * (input.vertexID & 0x01);
        float2 pp = GetQuadVertexPosition(quadIndex).xy;
        uint2 pixelCoord  = tileCoord * uint2(_TilePixelWidth, _TilePixelHeight);
        pixelCoord += uint2(pp.xy * uint2(_TilePixelWidth, _TilePixelHeight));
        float2 clipCoord = (pixelCoord * _ScreenSize.zw) * 2.0 - 1.0;

        output.positionCS = float4(clipCoord, 0, 1);
        // Screen is already y flipped (different from HDRP)?
        // Tiles coordinates always start at upper-left corner of the screen (y axis down).
        // Clip-space coordinatea always have y axis up. Hence, we must always flip y.
        // output.positionCS.y *= -1.0;
        output.posCS = output.positionCS;

        // "nointerpolation" interpolators are calculated by the provoking vertex of the triangles or quad.
        // Provoking vertex convention is different per platform.
        #if SHADER_API_SWITCH
		[branch] if (input.vertexID == 3)
        #else
        [branch] if (input.vertexID == 0 || input.vertexID == 3)
        #endif
        {
            int relLightOffset = tileData.relLightOffsetAndCount & 0xFFFF;
            int relLightOffsetEnd = relLightOffset + (tileData.relLightOffsetAndCount >> 16);

            // Trim beginning of the light list.
            [loop] for (; relLightOffset < relLightOffsetEnd; ++relLightOffset)
            {
                uint lightIndexAndRange = LoadRelLightIndex(relLightOffset);
                uint firstBit = (lightIndexAndRange >> 16) & 0xFF;
                uint bitCount = lightIndexAndRange >> 24;
                uint lightBitmask = (0xFFFFFFFF >> (32 - bitCount)) << firstBit;

                [branch] if ((geoDepthBitmask & lightBitmask) != 0)
                    break;
            }

            // Trim end of the light list.
            [loop] for (; relLightOffsetEnd >= relLightOffset; --relLightOffsetEnd)
            {
                uint lightIndexAndRange = LoadRelLightIndex(relLightOffsetEnd - 1);
                uint firstBit = (lightIndexAndRange >> 16) & 0xFF;
                uint bitCount = lightIndexAndRange >> 24;
                uint lightBitmask = (0xFFFFFFFF >> (32 - bitCount)) << firstBit;

                [branch] if ((geoDepthBitmask & lightBitmask) != 0)
                    break;
            }

            output.relLightOffsets.x = relLightOffset;
            output.relLightOffsets.y = relLightOffsetEnd;
        }
        else
        {
            output.relLightOffsets.x = 0;
            output.relLightOffsets.y = 0;
        }

        return output;
    }

    #if USE_CBUFFER_FOR_LIGHTDATA
        CBUFFER_START(UPunctualLightBuffer)
        // Unity does not support structure inside cbuffer unless for instancing case (not safe to use here).
        uint4 _PunctualLightBuffer[MAX_PUNCTUALLIGHT_PER_CBUFFER_BATCH * SIZEOF_VEC4_PUNCTUALLIGHTDATA];
        CBUFFER_END

        PunctualLightData LoadPunctualLightData(int relLightIndex)
        {
            uint i = relLightIndex * SIZEOF_VEC4_PUNCTUALLIGHTDATA;
            PunctualLightData pl;
            pl.posWS  = asfloat(_PunctualLightBuffer[i + 0].xyz);
            pl.radius2 = asfloat(_PunctualLightBuffer[i + 0].w);
            pl.color.rgb = asfloat(_PunctualLightBuffer[i + 1].rgb);
            pl.attenuation.xyzw = asfloat(_PunctualLightBuffer[i + 2].xyzw);
            pl.spotDirection.xyz = asfloat(_PunctualLightBuffer[i + 3].xyz);
            pl.shadowLightIndex = _PunctualLightBuffer[i + 3].w;

            return pl;
        }

    #else
        StructuredBuffer<PunctualLightData> _PunctualLightBuffer;

        PunctualLightData LoadPunctualLightData(int relLightIndex) { return _PunctualLightBuffer[relLightIndex]; }

    #endif

    TEXTURE2D_X(_CameraDepthTexture);
    TEXTURE2D_X_HALF(_GBuffer0);
    TEXTURE2D_X_HALF(_GBuffer1);
    TEXTURE2D_X_HALF(_GBuffer2);
    float4x4 _ScreenToWorld[2];

    half4 PunctualLightShading(Varyings input) : SV_Target
    {
        float d        = LOAD_TEXTURE2D_X(_CameraDepthTexture, input.positionCS.xy).x; // raw depth value has UNITY_REVERSED_Z applied on most platforms.
        half4 gbuffer0 = LOAD_TEXTURE2D_X(_GBuffer0, input.positionCS.xy);
        half4 gbuffer1 = LOAD_TEXTURE2D_X(_GBuffer1, input.positionCS.xy);
        half4 gbuffer2 = LOAD_TEXTURE2D_X(_GBuffer2, input.positionCS.xy);

        #if defined(USING_STEREO_MATRICES)
        int eyeIndex = unity_StereoEyeIndex;
        #else
        int eyeIndex = 0;
        #endif
        float4 posWS = mul(_ScreenToWorld[eyeIndex], float4(input.positionCS.xy, d, 1.0));
        posWS.xyz *= rcp(posWS.w);

        InputData inputData = InputDataFromGbufferAndWorldPosition(gbuffer2, posWS.xyz);
        uint materialFlags = UnpackMaterialFlags(gbuffer0.a);
        bool materialReceiveShadowsOff = (materialFlags & kMaterialFlagReceiveShadowsOff) != 0;
        #if SHADER_API_MOBILE || SHADER_API_SWITCH
        // Specular highlights are still silenced by setting specular to 0.0 during gbuffer pass and GPU timing is still reduced.
        bool materialSpecularHighlightsOff = false;
        #else
        bool materialSpecularHighlightsOff = (materialFlags & kMaterialFlagSpecularHighlightsOff) != 0;
        #endif

        #if defined(_LIT)
            BRDFData brdfData = BRDFDataFromGbuffer(gbuffer0, gbuffer1, gbuffer2);
        #elif defined(_SIMPLELIT)
            SurfaceData surfaceData = SurfaceDataFromGbuffer(gbuffer0, gbuffer1, gbuffer2, kLightingSimpleLit);
        #endif

        half3 color = 0.0.xxx;

        #if defined(_LIT)
            //[loop] for (int li = input.relLightOffsets.x; li < input.relLightOffsets.y; ++li)
            int li = input.relLightOffsets.x;
            [loop] do
            {
                uint relLightIndex = LoadRelLightIndex(li);
                PunctualLightData light = LoadPunctualLightData(relLightIndex & 0xFFFF);

                float3 L = light.posWS - posWS.xyz;
                [branch] if (dot(L, L) < light.radius2)
                {
                    Light unityLight = UnityLightFromPunctualLightDataAndWorldSpacePosition(light, posWS.xyz, materialReceiveShadowsOff);
                    color += LightingPhysicallyBased(brdfData, unityLight, inputData.normalWS, inputData.viewDirectionWS, materialSpecularHighlightsOff);
                }
            }
            while(++li < input.relLightOffsets.y);

        #elif defined(_SIMPLELIT)
            //[loop] for (int li = input.relLightOffsets.x; li < input.relLightOffsets.y; ++li)
            int li = input.relLightOffsets.x;
            [loop] do
            {
                uint relLightIndex = LoadRelLightIndex(li);
                PunctualLightData light = LoadPunctualLightData(relLightIndex & 0xFFFF);

                float3 L = light.posWS - posWS.xyz;
                [branch] if (dot(L, L) < light.radius2)
                {
                    Light unityLight = UnityLightFromPunctualLightDataAndWorldSpacePosition(light, posWS.xyz, materialReceiveShadowsOff);

                    half3 attenuatedLightColor = unityLight.color * (unityLight.distanceAttenuation * unityLight.shadowAttenuation);
                    half3 diffuseColor = LightingLambert(attenuatedLightColor, unityLight.direction, inputData.normalWS);
                    half3 specularColor = LightingSpecular(attenuatedLightColor, unityLight.direction, inputData.normalWS, inputData.viewDirectionWS, half4(surfaceData.specular, surfaceData.smoothness), surfaceData.smoothness);
                    // TODO: if !defined(_SPECGLOSSMAP) && !defined(_SPECULAR_COLOR), force specularColor to 0 in gbuffer code
                    color += diffuseColor * surfaceData.albedo + specularColor;
                }
            }
            while(++li < input.relLightOffsets.y);

        #endif

        return half4(color, 0.0);
    }

    ENDHLSL

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}

        // 0 - Tiled Deferred Punctual Light (Lit)
        Pass
        {
            Name "Tiled Deferred Punctual Light (Lit)"

            ZTest Always
            ZWrite Off
            Cull Off
            Blend One One, Zero One
            BlendOp Add, Add

            // [Stencil] Bit 5-6 material type. 00 = unlit/bakedLit, 01 = Lit, 10 = SimpleLit
            Stencil {
                Ref [_LitStencilRef]
                ReadMask [_LitReadMask]
                WriteMask [_LitWriteMask]
                Comp Equal
                Pass Zero
                Fail Zero
                ZFail Zero
            }

            HLSLPROGRAM

            #pragma multi_compile_fragment _LIT
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT

            #pragma vertex Vertex
            #pragma fragment PunctualLightShading
            //#pragma enable_d3d11_debug_symbols

            ENDHLSL
        }

        // 1 - Tiled Deferred Punctual Light (SimpleLit)
        Pass
        {
            Name "Tiled Deferred Punctual Light (SimpleLit)"

            ZTest Always
            ZWrite Off
            Cull Off
            Blend One One, Zero One
            BlendOp Add, Add

            Stencil {
                Ref [_SimpleLitStencilRef]
                ReadMask [_SimpleLitReadMask]
                WriteMask [_SimpleLitWriteMask]
                Comp Equal
                Pass Keep
                Fail Keep
                ZFail Keep
            }

            HLSLPROGRAM

            #pragma multi_compile_fragment _SIMPLELIT
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT

            #pragma vertex Vertex
            #pragma fragment PunctualLightShading
            //#pragma enable_d3d11_debug_symbols

            ENDHLSL
        }
    }
}
