#ifndef FILE_ATMOSPHERICSCATTERING
#define FILE_ATMOSPHERICSCATTERING

#define ATMOSPHERICS_DBG_NONE                   0
#define ATMOSPHERICS_DBG_SCATTERING             1
#define ATMOSPHERICS_DBG_OCCLUSION              2
#define ATMOSPHERICS_DBG_OCCLUDEDSCATTERING     3
#define ATMOSPHERICS_DBG_RAYLEIGH               4
#define ATMOSPHERICS_DBG_MIE                    5
#define ATMOSPHERICS_DBG_HEIGHT                 6

uniform int         _AtmosphericsDebugMode;

uniform float3      _SunDirection;

uniform float       _ShadowBias;
uniform float       _ShadowBiasIndirect;
uniform float       _ShadowBiasClouds;
uniform float2      _ShadowBiasSkyRayleighMie;
uniform float       _OcclusionDepthThreshold;
uniform float4      _OcclusionTexture_TexelSize;

uniform float4      _DepthTextureScaledTexelSize;

uniform float       _WorldScaleExponent;
uniform float       _WorldNormalDistanceRcp;
uniform float       _WorldRayleighNearScatterPush;
uniform float       _WorldMieNearScatterPush;
uniform float       _WorldRayleighDensity;
uniform float       _WorldMieDensity;

uniform float3      _RayleighColorM20;
uniform float3      _RayleighColorM10;
uniform float3      _RayleighColorO00;
uniform float3      _RayleighColorP10;
uniform float3      _RayleighColorP20;
uniform float3      _RayleighColorP45;

uniform float3      _MieColorM20;
uniform float3      _MieColorO00;
uniform float3      _MieColorP20;
uniform float3      _MieColorP45;

uniform float       _HeightNormalDistanceRcp;
uniform float       _HeightMieNearScatterPush;
uniform float       _HeightRayleighNearScatterPush;
uniform float       _HeightRayleighDensity;
uniform float       _HeightMieDensity;
uniform float       _HeightSeaLevel;
uniform float3      _HeightPlaneShift;
uniform float       _HeightDistanceRcp;
uniform float4      _HeightRayleighColor;
uniform float       _HeightExtinctionFactor;

uniform float2      _RayleighInScatterPct;
uniform float       _RayleighExtinctionFactor;

uniform float       _MiePhaseAnisotropy;
uniform float       _MieExtinctionFactor;

SAMPLER2D(sampler_MainDepthTexture);
#define SRL_BilinearSampler sampler_MainDepthTexture // Used for all textures

TEXTURE2D_FLOAT(_MainDepthTexture);
TEXTURE2D(_OcclusionTexture);

float HenyeyGreensteinPhase(float g, float cosTheta) {
    float gSqr = g * g;
    float a1 = (1.f - gSqr);
    float a2 = (2.f + gSqr);
    float b1 = 1.f + cosTheta * cosTheta;
    float b2 = pow(abs(1.f + gSqr - 2.f * g * cosTheta), 1.5f);
    return (a1 / a2) * (b1 / b2);
}

float RayleighPhase(float cosTheta) {
    const float f = 3.f / (16.f * PI);
    return f + f * cosTheta * cosTheta;
}

float MiePhase(float cosTheta, float anisotropy) {
    const float f = 3.f / (8.f * PI);
    return f * HenyeyGreensteinPhase(anisotropy, cosTheta);
}

float HeightDensity(float h, float H) {
    return exp(-h/H);
}

float3 WorldScale(float3 p) {
    p.xz = sign(p.xz) * pow(abs(p.xz), _WorldScaleExponent);
    return p;
}

void VolundTransferScatter(float3 worldPos, out float4 coords1, out float4 coords2, out float4 coords3) {
    const float3 scaledWorldPos = WorldScale(worldPos);
    const float3 worldCamPos = WorldScale(_CameraPosWS.xyz);

    const float c_MieScaleHeight = 1200.f;
    const float worldRayleighDensity = 1.f;
    const float worldMieDensity = HeightDensity(scaledWorldPos.y, c_MieScaleHeight);

    const float3 worldVec = scaledWorldPos.xyz - worldCamPos.xyz;
    const float worldVecLen = length(worldVec);
    const float3 worldDir = worldVec / worldVecLen;

    const float3 worldDirUnscaled = normalize(worldPos - _CameraPosWS.xyz);

    const float viewSunCos = dot(worldDirUnscaled, _SunDirection);
    const float rayleighPh = min(1.f, RayleighPhase(viewSunCos) * 12.f);
    const float miePh = MiePhase(viewSunCos, _MiePhaseAnisotropy);

    const float angle20 = 0.324f / 1.5f;
    const float angle10 = 0.174f / 1.5f;
    const float angleY = worldDir.y * saturate(worldVecLen / 250.0);

    float3 rayleighColor;
    if(angleY >= angle10) rayleighColor = lerp(_RayleighColorP10, _RayleighColorP20, saturate((angleY - angle10) / (angle20 - angle10)));
    else if(angleY >= 0.f) rayleighColor = lerp(_RayleighColorO00, _RayleighColorP10, angleY / angle10);
    else if(angleY >= -angle10) rayleighColor = lerp(_RayleighColorM10, _RayleighColorO00, (angleY + angle10) / angle10);
    else rayleighColor = lerp(_RayleighColorM20, _RayleighColorM10, saturate((angleY + angle20) / (angle20 - angle10)));

    float3 mieColor;
    if(angleY >= 0.f) mieColor = lerp(_MieColorO00, _MieColorP20, saturate(angleY / angle20));
    else mieColor = lerp(_MieColorM20, _MieColorO00, saturate((angleY + angle20) / angle20));

    const float pushedMieDistance      = max(0.f, worldVecLen + _WorldMieNearScatterPush);
    const float pushedRayleighDistance = max(0.f, worldVecLen + _WorldRayleighNearScatterPush);
    const float pushedMieDensity       = /*HeightDensity **/ pushedMieDistance      /** exp(-scaledWorldPos.y / 8000.f)*/;
    const float pushedRayleighDensity  = /*HeightDensity **/ pushedRayleighDistance /** exp(-scaledWorldPos.y / 8000.f)*/;
    const float rayleighScatter = (1.f - exp(_WorldRayleighDensity * pushedRayleighDensity)) * rayleighPh;
#ifdef IS_RENDERING_SKY
    const float mieScatter = (1.f - exp(_WorldMieDensity * pushedMieDensity));
#else
    const float mieScatter = (1.f - exp(_WorldMieDensity * pushedMieDensity)) * miePh;
#endif

    const float heightShift = dot(worldVec, _HeightPlaneShift);
    const float heightScaledOffset = (scaledWorldPos.y - heightShift - _HeightSeaLevel) * _HeightDistanceRcp;
    const float HeightDensity = exp(-heightScaledOffset);
    const float pushedRayleighHeightDistance = max(0.f, worldVecLen + _HeightRayleighNearScatterPush);
    const float pushedMieHeightDistance      = max(0.f, worldVecLen + _HeightMieNearScatterPush);
    const float heightRayleighScatter = (1.f - exp(_HeightRayleighDensity * pushedRayleighHeightDistance)) * HeightDensity;
#ifdef IS_RENDERING_SKY
    const float heightMieScatter = (1.f - exp(_HeightMieDensity * pushedMieHeightDistance)) * HeightDensity;
#else
    const float heightMieScatter = (1.f - exp(_HeightMieDensity * pushedMieHeightDistance)) * HeightDensity * miePh;
#endif

    rayleighColor = lerp(Luminance(rayleighColor).rrr, rayleighColor, saturate(pushedRayleighDistance * _WorldNormalDistanceRcp));
    float3 heightRayleighColor = lerp(Luminance(_HeightRayleighColor.xyz).rrr, _HeightRayleighColor.xyz, saturate(pushedRayleighHeightDistance * _HeightNormalDistanceRcp));

    coords1.rgb = rayleighScatter * rayleighColor;
    coords1.a = rayleighScatter;

    coords3.rgb = saturate(heightRayleighScatter) * heightRayleighColor;
    coords3.a = heightRayleighScatter;

    coords2.rgb = mieScatter * mieColor + saturate(heightMieScatter) * mieColor;
    coords2.a = mieScatter;
}

void VolundTransferScatter(float3 worldPos, out float4 coords1) {
    float4 c1, c2, c3;
    VolundTransferScatter(worldPos, c1, c2, c3);

#ifdef IS_RENDERING_SKY
    coords1.rgb = c3.rgb;
    coords1.a = max(0.f, 1.f - c3.a * _HeightExtinctionFactor);
#else
    coords1.rgb = c1.rgb;
    coords1.rgb += c3.rgb;
    coords1.a = max(0.f, 1.f - c1.a * _RayleighExtinctionFactor - c3.a * _HeightExtinctionFactor);
#endif

    coords1.rgb += c2.rgb;
    coords1.a *= max(0.f, 1.f - c2.a * _MieExtinctionFactor);

#ifdef ATMOSPHERICS_DEBUG
    if(_AtmosphericsDebugMode == ATMOSPHERICS_DBG_RAYLEIGH)
        coords1.rgb = c1.rgb;
    else if(_AtmosphericsDebugMode == ATMOSPHERICS_DBG_MIE)
        coords1.rgb = c2.rgb;
    else if(_AtmosphericsDebugMode == ATMOSPHERICS_DBG_HEIGHT)
        coords1.rgb = c3.rgb;
#endif
}

float2 UVFromPos(float2 pos) {
#if defined(UNITY_PASS_FORWARDBASE)
    return pos;
#else
    return pos * _ScreenSize.zw;
#endif
}

float3 VolundApplyScatter(float4 coords1, float2 pos, float3 color) {
#ifdef ATMOSPHERICS_DEBUG
    if(_AtmosphericsDebugMode == ATMOSPHERICS_DBG_OCCLUSION)
        return 1;
    else if(_AtmosphericsDebugMode == ATMOSPHERICS_DBG_SCATTERING || _AtmosphericsDebugMode == ATMOSPHERICS_DBG_OCCLUDEDSCATTERING)
        return coords1.rgb;
    else if(_AtmosphericsDebugMode == ATMOSPHERICS_DBG_RAYLEIGH || _AtmosphericsDebugMode == ATMOSPHERICS_DBG_MIE || _AtmosphericsDebugMode == ATMOSPHERICS_DBG_HEIGHT)
        return coords1.rgb;
#endif

    return color * coords1.a + coords1.rgb;
}

float3 VolundApplyScatterAdd(float coords1, float3 color) {
    return color * coords1;
}

void VolundTransferScatterOcclusion(float3 worldPos, out float4 coords1, out float3 coords2) {
    float4 c1, c2, c3;
    VolundTransferScatter(worldPos, c1, c2, c3);

    coords1.rgb = c1.rgb * _RayleighInScatterPct.x;
    coords1.a = max(0.f, 1.f - c1.a * _RayleighExtinctionFactor - c3.a * _HeightExtinctionFactor);

    coords1.rgb += c2.rgb;
    coords1.a *= max(0.f, 1.f - c2.a * _MieExtinctionFactor);

    coords2.rgb = c3.rgb + c1.rgb * _RayleighInScatterPct.y;

#ifdef ATMOSPHERICS_DEBUG
    if(_AtmosphericsDebugMode == ATMOSPHERICS_DBG_RAYLEIGH)
        coords1.rgb = c1.rgb;
    else if(_AtmosphericsDebugMode == ATMOSPHERICS_DBG_MIE)
        coords1.rgb = c2.rgb;
    else if(_AtmosphericsDebugMode == ATMOSPHERICS_DBG_HEIGHT)
        coords1.rgb = c3.rgb;
#endif
}

float VolundSampleScatterOcclusion(float2 pos) {
#if defined(ATMOSPHERICS_OCCLUSION)
    float2 uv = UVFromPos(pos);
#if defined(ATMOSPHERICS_OCCLUSION_EDGE_FIXUP)
    float4 baseUV = float4(uv.x, uv.y, 0.f, 0.f);

    float cDepth = SAMPLE_TEXTURE2D_LOD(_CMainDepthTexture, SRL_BilinearSampler, baseUV, 0.f).r;
    cDepth = LinearEyeDepth(cDepth, _ZBufferParams);

    float4 xDepth;
    baseUV.xy = uv + _DepthTextureScaledTexelSize.zy; xDepth.x = SAMPLE_TEXTURE2D_LOD(_MainDepthTexture, SRL_BilinearSampler, baseUV);
    baseUV.xy = uv + _DepthTextureScaledTexelSize.xy; xDepth.y = SAMPLE_TEXTURE2D_LOD(_MainDepthTexture, SRL_BilinearSampler, baseUV);
    baseUV.xy = uv + _DepthTextureScaledTexelSize.xw; xDepth.z = SAMPLE_TEXTURE2D_LOD(_MainDepthTexture, SRL_BilinearSampler, baseUV);
    baseUV.xy = uv + _DepthTextureScaledTexelSize.zw; xDepth.w = SAMPLE_TEXTURE2D_LOD(_MainDepthTexture, SRL_BilinearSampler, baseUV);

    xDepth.x = LinearEyeDepth(xDepth.x, _ZBufferParams);
    xDepth.y = LinearEyeDepth(xDepth.y, _ZBufferParams);
    xDepth.z = LinearEyeDepth(xDepth.z, _ZBufferParams);
    xDepth.w = LinearEyeDepth(xDepth.w, _ZBufferParams);

    float4 diffDepth = xDepth - cDepth.rrrr;
    float4 maskDepth = abs(diffDepth) < _OcclusionDepthThreshold;
    float maskWeight = dot(maskDepth, maskDepth);

    UNITY_BRANCH
    if(maskWeight == 4.f || maskWeight == 0.f) {
        return SAMPLE_TEXTURE2D_LOD(_OcclusionTexture, SRL_BilinearSampler, uv, 0.f).r;
    } else {
        float4 occ = GATHER_TEXTURE2D(_OcclusionTexture, SRL_BilinearSampler, uv);

        float4 fWeights;
        fWeights.xy = frac(uv * _OcclusionTexture_TexelSize.zw - 0.5f);
        fWeights.zw = float2(1.f, 1.f) - fWeights.xy;

        float4 mfWeights = float4(fWeights.z * fWeights.y, fWeights.x * fWeights.y, fWeights.x * fWeights.w, fWeights.z * fWeights.w);
        return dot(occ, mfWeights * maskDepth) / dot(mfWeights, maskDepth);
    }
#else
    return SAMPLE_TEXTURE2D(_OcclusionTexture, SRL_BilinearSampler, uv).r;
#endif
#else //defined(ATMOSPHERICS_OCCLUSION)
    return 1.f;
#endif //defined(ATMOSPHERICS_OCCLUSION)
}

float3 VolundApplyScatterOcclusion(float4 coords1, float3 coords2, float2 pos, float3 color) {
    float occlusion = VolundSampleScatterOcclusion(pos);

#ifdef ATMOSPHERICS_DEBUG
    if(_AtmosphericsDebugMode == ATMOSPHERICS_DBG_SCATTERING)
        return coords1.rgb + coords2.rgb;
    else if(_AtmosphericsDebugMode == ATMOSPHERICS_DBG_OCCLUSION)
        return occlusion;
    else if(_AtmosphericsDebugMode == ATMOSPHERICS_DBG_OCCLUDEDSCATTERING)
        return coords1.rgb * min(1.f, occlusion + _ShadowBias)  + coords2.rgb * min(1.f, occlusion + _ShadowBiasIndirect);
    else if(_AtmosphericsDebugMode == ATMOSPHERICS_DBG_RAYLEIGH || _AtmosphericsDebugMode == ATMOSPHERICS_DBG_MIE || _AtmosphericsDebugMode == ATMOSPHERICS_DBG_HEIGHT)
        return coords1.rgb;
#endif

    return
        color * coords1.a
        + coords1.rgb * min(1.f, occlusion + _ShadowBias) + coords2.rgb * min(1.f, occlusion + _ShadowBiasIndirect);
    ;
}

float VolundCloudOcclusion(float2 pos) {
#if defined(ATMOSPHERICS_OCCLUSION)
    return min(1.f, VolundSampleScatterOcclusion(pos) + _ShadowBiasClouds);
#else
    return 1.f;
#endif
}


float4 VolundApplyCloudScatter(float4 coords1, float4 color) {
#if defined(DBG_ATMOSPHERICS_SCATTERING) || defined(DBG_ATMOSPHERICS_OCCLUDEDSCATTERING)
    return float4(coords1.rgb, color.a);
#elif defined(DBG_ATMOSPHERICS_OCCLUSION)
    return 1;
#endif

    color.rgb = color.rgb * coords1.a + coords1.rgb;
    return color;
}

float4 VolundApplyCloudScatterOcclusion(float4 coords1, float3 coords2, float2 pos, float4 color) {
    float occlusion = VolundSampleScatterOcclusion(pos);
#ifdef ATMOSPHERICS_OCCLUSION_DEBUG2
    color.rgb = coords1.rgb * min(1.f, occlusion + _ShadowBias) + coords2.rgb * min(1.f, occlusion + _ShadowBiasIndirect);
    return color;
#endif
#ifdef ATMOSPHERICS_OCCLUSION_DEBUG
    return occlusion;
#endif

    color.rgb = color.rgb * coords1.a + coords1.rgb * min(1.f, occlusion + _ShadowBias) + coords2.rgb * min(1.f, occlusion + _ShadowBiasIndirect);

    float cloudOcclusion = min(1.f, occlusion + _ShadowBiasClouds);
    color.a *= cloudOcclusion;

    return color;
}

// Original vert/frag macros
#if defined(ATMOSPHERICS_OCCLUSION)
    #define VOLUND_SCATTER_COORDS(idx1, idx2) float4 scatterCoords1 : TEXCOORD##idx1; float3 scatterCoords2 : TEXCOORD##idx2;
    #if defined(ATMOSPHERICS_PER_PIXEL)
        #define VOLUND_TRANSFER_SCATTER(pos, o) o.scatterCoords1 = pos.xyzz; o.scatterCoords2 = pos.xyz;
        #define VOLUND_APPLY_SCATTER(i, color) VolundTransferScatterOcclusion(i.scatterCoords1.xyz, i.scatterCoords1, i.scatterCoords2); color = VolundApplyScatterOcclusion(i.scatterCoords1, i.scatterCoords2, i.pos.xy, color)
        #define VOLUND_CLOUD_SCATTER(i, color) VolundTransferScatterOcclusion(i.scatterCoords1.xyz, i.scatterCoords1, i.scatterCoords2); color = VolundApplyCloudScatterOcclusion(i.scatterCoords1, i.scatterCoords2, i.pos.xy, color)
    #else
        #define VOLUND_TRANSFER_SCATTER(pos, o) VolundTransferScatterOcclusion(pos, o.scatterCoords1, o.scatterCoords2)
        #define VOLUND_APPLY_SCATTER(i, color) color = VolundApplyScatterOcclusion(i.scatterCoords1, i.scatterCoords2, i.pos.xy, color)
        #define VOLUND_CLOUD_SCATTER(i, color) color = VolundApplyCloudScatterOcclusion(i.scatterCoords1, i.scatterCoords2, i.pos.xy, color)
    #endif
#else
    #define VOLUND_SCATTER_COORDS(idx1, idx2) float4 scatterCoords1 : TEXCOORD##idx1;
    #if defined(ATMOSPHERICS_PER_PIXEL)
        #define VOLUND_TRANSFER_SCATTER(pos, o) o.scatterCoords1 = pos.xyzz;
        #define VOLUND_APPLY_SCATTER(i, color) VolundTransferScatter(i.scatterCoords1.xyz, i.scatterCoords1); color = VolundApplyScatter(i.scatterCoords1, i.pos.xy, color);
        #define VOLUND_CLOUD_SCATTER(i, color) VolundTransferScatter(i.scatterCoords1.xyz, i.scatterCoords1); color = VolundApplyCloudScatter(i.scatterCoords1, color);
    #else
        #define VOLUND_TRANSFER_SCATTER(pos, o) VolundTransferScatter(pos, o.scatterCoords1)
        #define VOLUND_APPLY_SCATTER(i, color) color = VolundApplyScatter(i.scatterCoords1, i.pos.xy, color)
        #define VOLUND_CLOUD_SCATTER(i, color) color = VolundApplyCloudScatter(i.scatterCoords1, color)
    #endif
#endif

#if !defined(SURFACE_SCATTER_COORDS)
                                                /* surface shader analysis currently forces us to include stuff even when unused */
                                                /* we also have to convince the analyzer to not optimize out stuff we need */
    #define SURFACE_SCATTER_COORDS              float3 worldPos; float4 scatterCoords1; float3 scatterCoords2;
    #define SURFACE_SCATTER_TRANSFER(pos, o)    o.scatterCoords1.r = o.scatterCoords2.r = pos.x;
    #define SURFACE_SCATTER_APPLY(i, color)     color += (i.worldPos + i.scatterCoords1.xyz + i.scatterCoords2.xyz) * 0.000001f
#endif

#endif //FILE_ATMOSPHERICSCATTERING
