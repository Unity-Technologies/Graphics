#ifndef __REFLECTIONTEMPLATE_H__
#define __REFLECTIONTEMPLATE_H__

#include "UnityCG.cginc"
#include "UnityStandardBRDF.cginc"
#include "UnityStandardUtils.cginc"
#include "UnityPBSLighting.cginc"


UNITY_DECLARE_ABSTRACT_CUBE_ARRAY(_reflCubeTextures);
UNITY_DECLARE_TEXCUBE(_reflRootCubeTexture);
//uniform int _reflRootSliceIndex;
uniform float _reflRootHdrDecodeMult;
uniform float _reflRootHdrDecodeExp;


half3 Unity_GlossyEnvironment (UNITY_ARGS_ABSTRACT_CUBE_ARRAY(tex), int sliceIndex, half4 hdr, Unity_GlossyEnvironmentData glossIn);

half3 distanceFromAABB(half3 p, half3 aabbMin, half3 aabbMax)
{
    return max(max(p - aabbMax, aabbMin - p), half3(0.0, 0.0, 0.0));
}


float3 ExecuteReflectionList(uint start, uint numReflProbes, float3 vP, float3 vNw, float3 Vworld, float smoothness)
{
    float3 worldNormalRefl = reflect(-Vworld, vNw);

    float3 vspaceRefl = mul((float3x3) g_mWorldToView, worldNormalRefl).xyz;

    float percRoughness = SmoothnessToPerceptualRoughness(smoothness);

    UnityLight light;
    light.color = 0;
    light.dir = 0;

    float3 ints = 0;

    // root ibl begin
    {
        Unity_GlossyEnvironmentData g;
        g.roughness = percRoughness;
        g.reflUVW = worldNormalRefl;

        half3 env0 = Unity_GlossyEnvironment(UNITY_PASS_TEXCUBE(_reflRootCubeTexture), float4(_reflRootHdrDecodeMult, _reflRootHdrDecodeExp, 0.0, 0.0), g);
        //half3 env0 = Unity_GlossyEnvironment(UNITY_PASS_TEXCUBEARRAY(_reflCubeTextures), _reflRootSliceIndex, float4(_reflRootHdrDecodeMult, _reflRootHdrDecodeExp, 0.0, 0.0), g);

        UnityIndirect ind;
        ind.diffuse = 0;
        ind.specular = env0;// * data.occlusion;
        ints = EvalIndirectSpecular(light, ind);
    }
    // root ibl end

    uint l=0;
    // don't need the outer loop since the probes are sorted by volume type (currently one type in fact)
    //while(l<numReflProbes)
    if(numReflProbes>0)
    {
        uint uIndex = l<numReflProbes ? FetchIndex(start, l) : 0;
        uint uLgtType = l<numReflProbes ? g_vLightData[uIndex].lightType : 0;

        // specialized loop for sphere lights
        while(l<numReflProbes && uLgtType==(uint) BOX_LIGHT)
        {
            SFiniteLightData lgtDat = g_vLightData[uIndex];
            float3 vLp = lgtDat.lightPos.xyz;
            float3 vecToSurfPos  = vP - vLp;        // vector from reflection volume to surface position in camera space
            float3 posInReflVolumeSpace = float3( dot(vecToSurfPos, lgtDat.lightAxisX), dot(vecToSurfPos, lgtDat.lightAxisY), dot(vecToSurfPos, lgtDat.lightAxisZ) );


            float blendDistance = lgtDat.probeBlendDistance;//unity_SpecCube1_ProbePosition.w; // will be set to blend distance for this probe

            float3 sampleDir;
            if((lgtDat.flags&IS_BOX_PROJECTED)!=0)
            {
                // For box projection, use expanded bounds as they are rendered; otherwise
                // box projection artifacts when outside of the box.
                //float4 boxMin = unity_SpecCube0_BoxMin - float4(blendDistance,blendDistance,blendDistance,0);
                //float4 boxMax = unity_SpecCube0_BoxMax + float4(blendDistance,blendDistance,blendDistance,0);
                //sampleDir = BoxProjectedCubemapDirection (worldNormalRefl, worldPos, unity_SpecCube0_ProbePosition, boxMin, boxMax);

                float4 boxOuterDistance = float4( lgtDat.boxInnerDist + float3(blendDistance, blendDistance, blendDistance), 0.0 );
#if 0
                // if rotation is NOT supported
                sampleDir = BoxProjectedCubemapDirection(worldNormalRefl, posInReflVolumeSpace, float4(lgtDat.localCubeCapturePoint, 1.0), -boxOuterDistance, boxOuterDistance);
#else
                float3 volumeSpaceRefl = float3( dot(vspaceRefl, lgtDat.lightAxisX), dot(vspaceRefl, lgtDat.lightAxisY), dot(vspaceRefl, lgtDat.lightAxisZ) );
                float3 vPR = BoxProjectedCubemapDirection(volumeSpaceRefl, posInReflVolumeSpace, float4(lgtDat.localCubeCapturePoint, 1.0), -boxOuterDistance, boxOuterDistance);    // Volume space corrected reflection vector
                sampleDir = mul( (float3x3) g_mViewToWorld, vPR.x*lgtDat.lightAxisX + vPR.y*lgtDat.lightAxisY + vPR.z*lgtDat.lightAxisZ );
#endif
            }
            else
                sampleDir = worldNormalRefl;

            Unity_GlossyEnvironmentData g;
            g.roughness = percRoughness;
            g.reflUVW       = sampleDir;

            half3 env0 = Unity_GlossyEnvironment(UNITY_PASS_ABSTRACT_CUBE_ARRAY(_reflCubeTextures), lgtDat.sliceIndex, float4(lgtDat.lightIntensity, lgtDat.decodeExp, 0.0, 0.0), g);


            UnityIndirect ind;
            ind.diffuse = 0;
            ind.specular = env0;// * data.occlusion;

            //half3 rgb = UNITY_BRDF_PBS(0, data.specularColor, oneMinusReflectivity, data.smoothness, data.normalWorld, vWSpaceVDir, light, ind).rgb;
            half3 rgb = EvalIndirectSpecular(light, ind);

            // Calculate falloff value, so reflections on the edges of the Volume would gradually blend to previous reflection.
            // Also this ensures that pixels not located in the reflection Volume AABB won't
            // accidentally pick up reflections from this Volume.
            //half3 distance = distanceFromAABB(worldPos, unity_SpecCube0_BoxMin.xyz, unity_SpecCube0_BoxMax.xyz);
            half3 distance = distanceFromAABB(posInReflVolumeSpace, -lgtDat.boxInnerDist, lgtDat.boxInnerDist);
            half falloff = saturate(1.0 - length(distance)/blendDistance);

            ints = lerp(ints, rgb, falloff);

            // next probe
            ++l; uIndex = l<numReflProbes ? FetchIndex(start, l) : 0;
            uLgtType = l<numReflProbes ? g_vLightData[uIndex].lightType : 0;
        }

        //if(uLgtType!=BOX_LIGHT) ++l;
    }

    return ints;
}


half3 Unity_GlossyEnvironment (UNITY_ARGS_ABSTRACT_CUBE_ARRAY(tex), int sliceIndex, half4 hdr, Unity_GlossyEnvironmentData glossIn)
{
#if UNITY_GLOSS_MATCHES_MARMOSET_TOOLBAG2 && (SHADER_TARGET >= 30)
    // TODO: remove pow, store cubemap mips differently
    half perceptualRoughness = pow(glossIn.roughness, 3.0/4.0);
#else
    half perceptualRoughness = glossIn.roughness;           // MM: switched to this
#endif
    //perceptualRoughness = sqrt(sqrt(2/(64.0+2)));     // spec power to the square root of real roughness

#if 0
    float m = perceptualRoughness*perceptualRoughness;              // m is the real roughness parameter
    const float fEps = 1.192092896e-07F;        // smallest such that 1.0+FLT_EPSILON != 1.0  (+1e-4h is NOT good here. is visibly very wrong)
    float n =  (2.0/max(fEps, m*m))-2.0;        // remap to spec power. See eq. 21 in --> https://dl.dropboxusercontent.com/u/55891920/papers/mm_brdf.pdf

    n /= 4;                                     // remap from n_dot_h formulatino to n_dot_r. See section "Pre-convolved Cube Maps vs Path Tracers" --> https://s3.amazonaws.com/docs.knaldtech.com/knald/1.0.0/lys_power_drops.html

    perceptualRoughness = pow( 2/(n+2), 0.25);          // remap back to square root of real roughness
#else
    // MM: came up with a surprisingly close approximation to what the #if 0'ed out code above does.
    perceptualRoughness = perceptualRoughness*(1.7 - 0.7*perceptualRoughness);
#endif



    half mip = perceptualRoughness * UNITY_SPECCUBE_LOD_STEPS;
    half4 rgbm = UNITY_SAMPLE_ABSTRACT_CUBE_ARRAY_LOD(tex, float4(glossIn.reflUVW.xyz, sliceIndex), mip);

    return DecodeHDR(rgbm, hdr);
}




#endif
