#if SHADERPASS != SHADERPASS_CUSTOM_UI
#error SHADERPASS_CUSTOM_UI_is_not_correctly_defined
#endif

#ifdef UNITY_UV_STARTS_AT_TOP
    float4x4 unity_MatrixVP;
#endif

#define INCLUDE_ONLY_MV_FUNCTIONS
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl"

// Get Homogeneous normalized device coordinates
float4 GetVertexPositionNDC(float3 positionCS)
{
    float3 ndc = positionCS * 0.5f;
    float4 positionNDC;
    positionNDC.xy = float2(ndc.x, ndc.y * _ProjectionParams.x);// + ndc.w;
    positionNDC.zw = float2(positionCS.z, 1.0);
    return positionNDC;
}

// Transforms position from object space to homogenous space
// float4 TransformModelToHClip(float3 positionOS)
float4 TransformModelToHClip(float3 positionOS)
{
    return mul(unity_MatrixVP, mul(GetRawUnityObjectToWorld(), float4(positionOS, 1.0)));
}

Varyings BuildVaryings(Attributes input)
{
    Varyings output = (Varyings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);


    // Returns the camera relative position (if enabled)
    float3 positionWS = TransformObjectToWorld(input.positionOS);
    output.positionCS = TransformWorldToHClip(positionWS);
    // output.positionCS = TransformObjectToHClip(input.positionOS);

    #if UNITY_UV_STARTS_AT_TOP
    //When UI is set to render in Overlay it writes to GFXdevice
    //the same as if it was writing to a full screen back buffer
    //Work around to catch Matrices not supplied by HDCamera,
    //So Clipspace is recalculated, using raw  unity_ObjectToWorld & unity_MatrixVP
    output.positionCS =  TransformModelToHClip(input.positionOS);
    output.texCoord0.y = 1.0 - output.texCoord0.y;
    #endif


    #ifdef ATTRIBUTES_NEED_NORMAL
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
    #else
    // Required to compile ApplyVertexModification that doesn't use normal.
    float3 normalWS = float3(0.0, 0.0, 0.0);
    #endif

    #ifdef ATTRIBUTES_NEED_TANGENT
    float4 tangentWS = float4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w);
    #endif

    // TODO: Change to inline ifdef
    // Do vertex modification in camera relative space (if enabled)
    #if defined(HAVE_VERTEX_MODIFICATION)
    ApplyVertexModification(input, normalWS, positionWS, _TimeParameters.xyz);
    #endif

    #ifdef VARYINGS_NEED_POSITION_WS
        output.positionWS = positionWS;
    #endif

    #ifdef VARYINGS_NEED_NORMAL_WS
        output.normalWS = normalWS;         // normalized in TransformObjectToWorldNormal()
    #endif

    #ifdef VARYINGS_NEED_TANGENT_WS
        output.tangentWS = tangentWS;       // normalized in TransformObjectToWorldDir()
    #endif

    #if defined(VARYINGS_NEED_TEXCOORD0)
    output.texCoord0 = input.uv0;
    #endif

    //UI "Mask"
    #if defined(VARYINGS_NEED_TEXCOORD1)
        #ifdef UNITY_UI_CLIP_RECT
            float2 pixelSize =  output.positionCS.w;
            pixelSize /= float2(1, 1) * abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));

            float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
            float2 maskUV = (input.positionOS.xy - clampedRect.xy) / (clampedRect.zw - clampedRect.xy);
            output.texCoord1 = float4(input.positionOS.xy * 2 - clampedRect.xy - clampedRect.zw, 0.25 / (0.25 * half2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize.xy)));
        #endif
    #endif

    #if defined(VARYINGS_NEED_TEXCOORD2)
    // Store previous CS position for MV calculation
    float3 previousPositionRWS = TransformPreviousObjectToWorld(input.positionOS);
    output.texCoord2 = mul(UNITY_MATRIX_PREV_VP, float4(previousPositionRWS, 1.0));
    #endif

    #if defined(VARYINGS_NEED_TEXCOORD3)
        output.texCoord3 = input.uv3;
    #endif

    #if defined(VARYINGS_NEED_COLOR)
        output.color = input.color;
    #endif

    #ifdef VARYINGS_NEED_SCREENPOSITION
        output.screenPosition = GetVertexPositionNDC(output.positionCS); // vertexInput.positionNDC;
    #endif

    // Apply MV bias
    #if UNITY_REVERSED_Z
    output.positionCS.z -= unity_MotionVectorsParams.z * output.positionCS.w;
    #else
    output.positionCS.z += unity_MotionVectorsParams.z * output.positionCS.w;
    #endif

    return output;
}

PackedVaryings vert(Attributes input)
{
    Varyings output = BuildVaryings(input);
    PackedVaryings packedOutput = PackVaryings(output);
    return packedOutput;
}

//NOTE: some shaders set target1 to be
//   Blend 1 SrcAlpha OneMinusSrcAlpha
//The reason for this blend mode is to let virtual texturing alpha dither work.
//Anything using Target1 should write 1.0 or 0.0 in alpha to write / not write into the target.
#ifdef UNITY_VIRTUAL_TEXTURING
#define VT_BUFFER_TARGET SV_Target1
#define EXTRA_BUFFER_TARGET SV_Target2
#if defined(SHADER_API_PSSL)
//For exact packing on pssl, we want to write exact 16 bit unorm (respect exact bit packing).
//In some sony platforms, the default is FMT_16_ABGR, which would incur in loss of precision.
//Thus, when VT is enabled, we force FMT_32_ABGR
#pragma PSSL_target_output_format(target 1 FMT_32_ABGR)
#endif
#else
#define EXTRA_BUFFER_TARGET SV_Target1
#endif

void frag(PackedVaryings packedInput,
out float4 outColor : SV_Target0
#ifdef UNITY_VIRTUAL_TEXTURING
    , out float4 outVTFeedback : VT_BUFFER_TARGET
#endif
    , out float4 outMotionVec : EXTRA_BUFFER_TARGET
)
{
    Varyings unpacked = UnpackVaryings(packedInput);
    UNITY_SETUP_INSTANCE_ID(unpacked);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(unpacked);
    SurfaceDescriptionInputs surfaceDescriptionInputs = BuildSurfaceDescriptionInputs(unpacked);
    SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs);

    //Round up the alpha color coming from the interpolator (to 1.0/256.0 steps)
    //The incoming alpha could have numerical instability, which makes it very sensible to
    //HDR color transparency blend, when it blends with the world's texture.
    const half alphaPrecision = half(0xff);
    const half invAlphaPrecision = half(1.0/alphaPrecision);
    unpacked.color.a = round(unpacked.color.a * alphaPrecision)*invAlphaPrecision;


    half alpha = surfaceDescription.Alpha;
    outColor = half4(surfaceDescription.BaseColor + surfaceDescription.Emission, alpha) ;

    #if !defined(HAVE_VFX_MODIFICATION) && !defined(_DISABLE_COLOR_TINT)
    outColor *= unpacked.color;
    #endif

    #ifdef UNITY_UI_CLIP_RECT
        //mask = Uv2
        half2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(unpacked.texCoord1.xy)) * unpacked.texCoord1.zw);
        outColor.a *= m.x * m.y;
    #endif

    #ifdef _ALPHATEST_ON
        clip(alpha - surfaceDescription.AlphaClipThreshold);
    #endif

    outColor.rgb *= outColor.a;

    // We don't support VT feedback on UI
#ifdef UNITY_VIRTUAL_TEXTURING
    outVTFeedback = 0;
#endif

    #if defined(UI_MOTION_VECTORS)
        float4 previousPositionCS = unpacked.texCoord2;
        float2 motionVector = CalculateMotionVector(unpacked.positionCS, previousPositionCS);
        outMotionVec = float4(motionVector * 0.5, 1, 1);
    #endif
}
