#ifndef PROBEVOLUMEDEBUG_FUNCTIONS_HLSL
#define PROBEVOLUMEDEBUG_FUNCTIONS_HLSL

#ifdef PROBE_VOLUME_DEBUG_FUNCTION_MAIN
    v2f vert(appdata v)
    {
        v2f o;

        UNITY_SETUP_INSTANCE_ID(v);
        UNITY_TRANSFER_INSTANCE_ID(v, o);

        if (!ShouldCull(o))
        {
            float4 wsPos = mul(UNITY_MATRIX_M, float4(v.vertex.xyz * _ProbeSize, 1.0));
            o.vertex = mul(UNITY_MATRIX_VP, wsPos);
            o.normal = normalize(mul(v.normal, (float3x3)UNITY_MATRIX_M));
        }

        return o;
    }

    float4 frag(v2f i) : SV_Target
    {
        UNITY_SETUP_INSTANCE_ID(i);

        if (_ShadingMode >= DEBUGPROBESHADINGMODE_SH && _ShadingMode <= DEBUGPROBESHADINGMODE_SHL0L1)
        {
            return float4(CalculateDiffuseLighting(i) * exp2(_ExposureCompensation) * GetCurrentExposureMultiplier(), 1);
        }
        else if (_ShadingMode == DEBUGPROBESHADINGMODE_INVALIDATED_BY_TOUCHUP_VOLUMES)
        {
            float4 defaultCol = float4(CalculateDiffuseLighting(i) * exp2(_ExposureCompensation) * GetCurrentExposureMultiplier(), 1);
            float touchupAction = UNITY_ACCESS_INSTANCED_PROP(Props, _TouchupedByVolume);
            if (touchupAction > 0 && touchupAction < 1)
            {
                return float4(1, 0, 0, 1);
            }
            return defaultCol;
        }
        else if (_ShadingMode == DEBUGPROBESHADINGMODE_VALIDITY)
        {
            float validity = UNITY_ACCESS_INSTANCED_PROP(Props, _Validity);
            return lerp(float4(0, 1, 0, 1), float4(1, 0, 0, 1), validity);
        }
        else if (_ShadingMode == DEBUGPROBESHADINGMODE_VALIDITY_OVER_DILATION_THRESHOLD)
        {
            float validity = UNITY_ACCESS_INSTANCED_PROP(Props, _Validity);
            float threshold = UNITY_ACCESS_INSTANCED_PROP(Props, _DilationThreshold);
            if (validity > threshold)
            {
                return float4(1, 0, 0, 1);
            }
            else
            {
                return float4(0, 1, 0, 1);
            }
        }
        else if (_ShadingMode == DEBUGPROBESHADINGMODE_SIZE)
        {
            float4 col = lerp(float4(0, 1, 0, 1), float4(1, 0, 0, 1), UNITY_ACCESS_INSTANCED_PROP(Props, _RelativeSize));
            return col;
        }

        return _Color;
    }
#endif

#ifdef PROBE_VOLUME_DEBUG_FUNCTION_FRAGMENTATION
    int _ChunkCount;
    StructuredBuffer<int> _DebugFragmentation;

    struct Attributes
    {
        uint vertexID : SV_VertexID;
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        float2 texcoord : TEXCOORD0;
    };

    Varyings Vert(Attributes input)
    {
        Varyings output;
        output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
        output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID);

        return output;
    }

    float4 Frag(Varyings input) : SV_Target
    {
        int lineSize = (int)ceil(sqrt(_ChunkCount));
        int2 coord = (int2)(input.texcoord * lineSize);

        int index = coord.y * lineSize + coord.x;

        float4 color = 0.0;
        if (index < _ChunkCount && _DebugFragmentation[index] != -1)
            color = float4(0.0, 1.0, 0.0, 1.0);

        return color;
    }
#endif

#ifdef PROBE_VOLUME_DEBUG_FUNCTION_OFFSET
    v2f vert(appdata v)
    {
        v2f o;

        UNITY_SETUP_INSTANCE_ID(v);
        UNITY_TRANSFER_INSTANCE_ID(v, o);

        float3 offset = UNITY_ACCESS_INSTANCED_PROP(Props, _Offset).xyz;
        float offsetLenSqr = dot(offset, offset);
        if (offsetLenSqr <= 1e-6f)
        {
            DoCull(o);
        }
        else if (!ShouldCull(o))
        {
            float4 wsPos = mul(UNITY_MATRIX_M, float4(v.vertex.x * _OffsetSize, v.vertex.y * _OffsetSize, v.vertex.z, 1.f));
            o.vertex = mul(UNITY_MATRIX_VP, wsPos);
            o.normal = normalize(mul(v.normal, (float3x3)UNITY_MATRIX_M));
        }

        return o;
    }

    float4 frag(v2f i) : SV_Target
    {
        return float4(0, 0, 1, 1);
    }
#endif

#endif // PROBEVOLUMEDEBUG_FUNCTIONS_HLSL
