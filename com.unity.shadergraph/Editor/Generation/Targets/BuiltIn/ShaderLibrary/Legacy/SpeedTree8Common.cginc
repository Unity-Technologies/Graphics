///////////////////////////////////////////////////////////////////////
//  SpeedTree8Common.cginc

#ifndef SPEEDTREE8_COMMON_INCLUDED
#define SPEEDTREE8_COMMON_INCLUDED

#include "UnityCG.cginc"
#include "UnityPBSLighting.cginc"

#if defined(ENABLE_WIND) && !defined(_WINDQUALITY_NONE)
    #define SPEEDTREE_Y_UP
    #include "SpeedTreeWind.cginc"
    float _WindEnabled;
    UNITY_INSTANCING_BUFFER_START(STWind)
        UNITY_DEFINE_INSTANCED_PROP(float, _GlobalWindTime)
    UNITY_INSTANCING_BUFFER_END(STWind)
#endif

struct Input
{
    half2   uv_MainTex  : TEXCOORD0;
    fixed4  color       : COLOR;

    #ifdef EFFECT_BACKSIDE_NORMALS
        fixed   facing      : VFACE;
    #endif
};

sampler2D _MainTex;
fixed4 _Color;
int _TwoSided;

#ifdef EFFECT_BUMP
    sampler2D _BumpMap;
#endif

#ifdef EFFECT_EXTRA_TEX
    sampler2D _ExtraTex;
#else
    half _Glossiness;
    half _Metallic;
#endif

#ifdef EFFECT_HUE_VARIATION
    half4 _HueVariationColor;
#endif

#ifdef EFFECT_BILLBOARD
    half _BillboardShadowFade;
#endif

#ifdef EFFECT_SUBSURFACE
    sampler2D _SubsurfaceTex;
    fixed4 _SubsurfaceColor;
    half _SubsurfaceIndirect;
#endif

#define GEOM_TYPE_BRANCH 0
#define GEOM_TYPE_FROND 1
#define GEOM_TYPE_LEAF 2
#define GEOM_TYPE_FACINGLEAF 3


///////////////////////////////////////////////////////////////////////
//  OffsetSpeedTreeVertex

void OffsetSpeedTreeVertex(inout appdata_full data, float lodValue)
{
    // smooth LOD
    #if defined(LOD_FADE_PERCENTAGE) && !defined(EFFECT_BILLBOARD)
        data.vertex.xyz = lerp(data.vertex.xyz, data.texcoord2.xyz, lodValue);
    #endif

    // wind
    #if defined(ENABLE_WIND) && !defined(_WINDQUALITY_NONE)
        if (_WindEnabled > 0)
        {
            float3 rotatedWindVector = mul(_ST_WindVector.xyz, (float3x3)unity_ObjectToWorld);
            float windLength = length(rotatedWindVector);
            if (windLength < 1e-5)
            {
                // sanity check that wind data is available
                return;
            }
            rotatedWindVector /= windLength;

            float3 treePos = float3(unity_ObjectToWorld[0].w, unity_ObjectToWorld[1].w, unity_ObjectToWorld[2].w);
            float3 windyPosition = data.vertex.xyz;

            #ifndef EFFECT_BILLBOARD
                // geometry type
                float geometryType = (int)(data.texcoord3.w + 0.25);
                bool leafTwo = false;
                if (geometryType > GEOM_TYPE_FACINGLEAF)
                {
                    geometryType -= 2;
                    leafTwo = true;
                }

                // leaves
                if (geometryType > GEOM_TYPE_FROND)
                {
                    // remove anchor position
                    float3 anchor = float3(data.texcoord1.zw, data.texcoord2.w);
                    windyPosition -= anchor;

                    if (geometryType == GEOM_TYPE_FACINGLEAF)
                    {
                        // face camera-facing leaf to camera
                        float offsetLen = length(windyPosition);
                        windyPosition = mul(windyPosition.xyz, (float3x3)UNITY_MATRIX_IT_MV); // inv(MV) * windyPosition
                        windyPosition = normalize(windyPosition) * offsetLen; // make sure the offset vector is still scaled
                    }

                    // leaf wind
                    #if defined(_WINDQUALITY_FAST) || defined(_WINDQUALITY_BETTER) || defined(_WINDQUALITY_BEST)
                        #ifdef _WINDQUALITY_BEST
                            bool bBestWind = true;
                        #else
                            bool bBestWind = false;
                        #endif
                        float leafWindTrigOffset = anchor.x + anchor.y;
                        windyPosition = LeafWind(bBestWind, leafTwo, windyPosition, data.normal, data.texcoord3.x, float3(0,0,0), data.texcoord3.y, data.texcoord3.z, leafWindTrigOffset, rotatedWindVector);
                    #endif

                    // move back out to anchor
                    windyPosition += anchor;
                }

                // frond wind
                bool bPalmWind = false;
                #ifdef _WINDQUALITY_PALM
                    bPalmWind = true;
                    if (geometryType == GEOM_TYPE_FROND)
                    {
                        windyPosition = RippleFrond(windyPosition, data.normal, data.texcoord.x, data.texcoord.y, data.texcoord3.x, data.texcoord3.y, data.texcoord3.z);
                    }
                #endif

                // branch wind (applies to all 3D geometry)
                #if defined(_WINDQUALITY_BETTER) || defined(_WINDQUALITY_BEST) || defined(_WINDQUALITY_PALM)
                    float3 rotatedBranchAnchor = normalize(mul(_ST_WindBranchAnchor.xyz, (float3x3)unity_ObjectToWorld)) * _ST_WindBranchAnchor.w;
                    windyPosition = BranchWind(bPalmWind, windyPosition, treePos, float4(data.texcoord.zw, 0, 0), rotatedWindVector, rotatedBranchAnchor);
                #endif

            #endif // !EFFECT_BILLBOARD

            // global wind
            float globalWindTime = _ST_WindGlobal.x;
            #if defined(EFFECT_BILLBOARD) && defined(UNITY_INSTANCING_ENABLED)
                globalWindTime += UNITY_ACCESS_INSTANCED_PROP(STWind, _GlobalWindTime);
            #endif
            windyPosition = GlobalWind(windyPosition, treePos, true, rotatedWindVector, globalWindTime);
            data.vertex.xyz = windyPosition;
        }
    #endif
}


///////////////////////////////////////////////////////////////////////
//  vertex program

void SpeedTreeVert(inout appdata_full v)
{
    // handle speedtree wind and lod
    OffsetSpeedTreeVertex(v, unity_LODFade.x);

    float3 treePos = float3(unity_ObjectToWorld[0].w, unity_ObjectToWorld[1].w, unity_ObjectToWorld[2].w);

    #if defined(EFFECT_BILLBOARD)

        // crossfade faces
        bool topDown = (v.texcoord.z > 0.5);
        float3 viewDir = UNITY_MATRIX_IT_MV[2].xyz;
        float3 cameraDir = normalize(mul((float3x3)unity_WorldToObject, _WorldSpaceCameraPos - treePos));
        float viewDot = max(dot(viewDir, v.normal), dot(cameraDir, v.normal));
        viewDot *= viewDot;
        viewDot *= viewDot;
        viewDot += topDown ? 0.38 : 0.18; // different scales for horz and vert billboards to fix transition zone
        v.color = float4(1, 1, 1, clamp(viewDot, 0, 1));

        // if invisible, avoid overdraw
        if (viewDot < 0.3333)
        {
            v.vertex.xyz = float3(0,0,0);
        }

        // adjust lighting on billboards to prevent seams between the different faces
        if (topDown)
        {
            v.normal += cameraDir;
        }
        else
        {
            half3 binormal = cross(v.normal, v.tangent.xyz) * v.tangent.w;
            float3 right = cross(cameraDir, binormal);
            v.normal = cross(binormal, right);
        }
        v.normal = normalize(v.normal);

    #endif

    // color already contains (ao, ao, ao, blend)
    // put hue variation amount in there
    #ifdef EFFECT_HUE_VARIATION
        float hueVariationAmount = frac(treePos.x + treePos.y + treePos.z);
        v.color.g = saturate(hueVariationAmount * _HueVariationColor.a);
    #endif
}


///////////////////////////////////////////////////////////////////////
//  lighting function to add subsurface

half4 LightingSpeedTreeSubsurface(inout SurfaceOutputStandard s, half3 viewDir, UnityGI gi)
{
    #ifdef EFFECT_SUBSURFACE
        half fSubsurfaceRough = 0.7 - s.Smoothness * 0.5;
        half fSubsurface = GGXTerm(clamp(-dot(gi.light.dir, viewDir), 0, 1), fSubsurfaceRough);

        // put modulated subsurface back into emission
        s.Emission *= (gi.indirect.diffuse * _SubsurfaceIndirect + gi.light.color * fSubsurface);
    #endif

    return LightingStandard(s, viewDir, gi);
}

void LightingSpeedTreeSubsurface_GI(inout SurfaceOutputStandard s, UnityGIInput data, inout UnityGI gi)
{
    #ifdef EFFECT_BILLBOARD
        // fade off the shadows on billboards to avoid artifacts
        data.atten = lerp(data.atten, 1.0, _BillboardShadowFade);
    #endif

    LightingStandard_GI(s, data, gi);
}

half4 LightingSpeedTreeSubsurface_Deferred(SurfaceOutputStandard s, half3 viewDir, UnityGI gi, out half4 outGBuffer0, out half4 outGBuffer1, out half4 outGBuffer2)
{
    // no light/shadow info in deferred, so stop subsurface
    s.Emission = half3(0,0,0);

    return LightingStandard_Deferred(s, viewDir, gi, outGBuffer0, outGBuffer1, outGBuffer2);
}


///////////////////////////////////////////////////////////////////////
//  surface shader

void SpeedTreeSurf(Input IN, inout SurfaceOutputStandard OUT)
{
    fixed4 color = tex2D(_MainTex, IN.uv_MainTex) * _Color;

    // transparency
    OUT.Alpha = color.a * IN.color.a;
    clip(OUT.Alpha - 0.3333);

    // color
    OUT.Albedo = color.rgb;

    // hue variation
    #ifdef EFFECT_HUE_VARIATION
        half3 shiftedColor = lerp(OUT.Albedo, _HueVariationColor.rgb, IN.color.g);

        // preserve vibrance
        half maxBase = max(OUT.Albedo.r, max(OUT.Albedo.g, OUT.Albedo.b));
        half newMaxBase = max(shiftedColor.r, max(shiftedColor.g, shiftedColor.b));
        maxBase /= newMaxBase;
        maxBase = maxBase * 0.5f + 0.5f;
        shiftedColor.rgb *= maxBase;

        OUT.Albedo = saturate(shiftedColor);
    #endif

    // normal
    #ifdef EFFECT_BUMP
        OUT.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
    #elif defined(EFFECT_BACKSIDE_NORMALS) || defined(EFFECT_BILLBOARD)
        OUT.Normal = float3(0, 0, 1);
    #endif

    // flip normal on backsides
    #ifdef EFFECT_BACKSIDE_NORMALS
        if (IN.facing < 0.5)
        {
            OUT.Normal.z = -OUT.Normal.z;
        }
    #endif

    // adjust billboard normals to improve GI and matching
    #ifdef EFFECT_BILLBOARD
        OUT.Normal.z *= 0.5;
        OUT.Normal = normalize(OUT.Normal);
    #endif

    // extra
    #ifdef EFFECT_EXTRA_TEX
        fixed4 extra = tex2D(_ExtraTex, IN.uv_MainTex);
        OUT.Smoothness = extra.r;
        OUT.Metallic = extra.g;
        OUT.Occlusion = extra.b * IN.color.r;
    #else
        OUT.Smoothness = _Glossiness;
        OUT.Metallic = _Metallic;
        OUT.Occlusion = IN.color.r;
    #endif

    // subsurface (hijack emissive)
    #ifdef EFFECT_SUBSURFACE
        OUT.Emission = tex2D(_SubsurfaceTex, IN.uv_MainTex) * _SubsurfaceColor;
    #endif
}


#endif // SPEEDTREE8_COMMON_INCLUDED
