using UnityEditor.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    internal class SerializedHDRaytracingEnvironment
    {
#if ENABLE_RAYTRACING

        public SerializedObject serializedObject;

        // Generic Attributes
        public SerializedProperty rayBias;

        // Ambient Occlusion
        public SerializedProperty raytracedAO;
        public SerializedProperty aoLayerMask;
        public SerializedProperty aoFilterMode;
        public SerializedProperty aoRayLength;
        public SerializedProperty aoNumSamples;
        public SerializedProperty aoBilateralRadius;
        public SerializedProperty maxFilterWidthInPixels;
        public SerializedProperty filterRadiusInMeters;
        public SerializedProperty normalSharpness;

        // Light Cluster Attributes
        public SerializedProperty maxNumLightsPercell;
        public SerializedProperty cameraClusterRange;

        // Reflections Attributes
        public SerializedProperty raytracedReflections;
        public SerializedProperty reflLayerMask;
        public SerializedProperty reflRayLength;
        public SerializedProperty reflBlendDistance;
        public SerializedProperty reflMinSmoothness;
        public SerializedProperty reflClampValue;
        public SerializedProperty reflQualityMode;
        public SerializedProperty reflFilterMode;
        public SerializedProperty reflTemporalAccumulationWeight;
        public SerializedProperty reflSpatialFilterRadius;
        public SerializedProperty reflNumMaxSamples;
        public SerializedProperty reflFilterRadius;

        // Primary visiblity raytracing
        public SerializedProperty raytracedObjects;
        public SerializedProperty raytracedLayerMask;
        public SerializedProperty rayMaxDepth;
        public SerializedProperty raytracingRayLength;

        // Area Shadow Properties
        public SerializedProperty raytracedShadows;
        public SerializedProperty shadowLayerMask;
        public SerializedProperty shadowNumSamples;
        public SerializedProperty numAreaLightShadows;
        public SerializedProperty shadowFilterRadius;
        public SerializedProperty splitIntegration;

        // Indirect diffuse Properties
        public SerializedProperty raytracedIndirectDiffuse;
        public SerializedProperty indirectDiffuseLayerMask;
        public SerializedProperty indirectDiffuseNumSamples;
        public SerializedProperty indirectDiffuseRayLength;
        public SerializedProperty indirectDiffuseClampValue;
        public SerializedProperty indirectDiffuseFilterMode;
        public SerializedProperty indirectDiffuseFilterRadius;

        public SerializedHDRaytracingEnvironment(HDRaytracingEnvironment rtEnv)
        {
            serializedObject = new SerializedObject(rtEnv);

            var o = new PropertyFetcher<HDRaytracingEnvironment>(serializedObject);

            // Ambient Occlusion
            rayBias = o.Find(x => x.rayBias);
            raytracedAO = o.Find(x => x.raytracedAO);
            aoLayerMask = o.Find(x => x.aoLayerMask);
            aoFilterMode = o.Find(x => x.aoFilterMode);
            aoRayLength = o.Find(x => x.aoRayLength);
            aoNumSamples = o.Find(x => x.aoNumSamples);
            aoBilateralRadius = o.Find(x => x.aoBilateralRadius);
            maxFilterWidthInPixels = o.Find(x => x.maxFilterWidthInPixels);
            filterRadiusInMeters = o.Find(x => x.filterRadiusInMeters);
            normalSharpness = o.Find(x => x.normalSharpness);

            // Reflections Attributes
            raytracedReflections = o.Find(x => x.raytracedReflections);
            reflLayerMask = o.Find(x => x.reflLayerMask);
            reflRayLength = o.Find(x => x.reflRayLength);
            reflBlendDistance = o.Find(x => x.reflBlendDistance);
            reflMinSmoothness = o.Find(x => x.reflMinSmoothness);
            reflClampValue = o.Find(x => x.reflClampValue);
            reflQualityMode = o.Find(x => x.reflQualityMode);
            reflFilterMode = o.Find(x => x.reflFilterMode);
            reflTemporalAccumulationWeight = o.Find(x => x.reflTemporalAccumulationWeight);
            reflSpatialFilterRadius = o.Find(x => x.reflSpatialFilterRadius);
            reflNumMaxSamples = o.Find(x => x.reflNumMaxSamples);
            reflFilterRadius = o.Find(x => x.reflFilterRadius);

            // Shadows Attributes
            raytracedShadows = o.Find(x => x.raytracedShadows);
            shadowLayerMask = o.Find(x => x.shadowLayerMask);
            shadowNumSamples = o.Find(x => x.shadowNumSamples);
            numAreaLightShadows = o.Find(x => x.numAreaLightShadows);
            shadowFilterRadius = o.Find(x => x.shadowFilterRadius);
            splitIntegration = o.Find(x => x.splitIntegration);

            // Light Cluster Attributes
            maxNumLightsPercell = o.Find(x => x.maxNumLightsPercell);
            cameraClusterRange = o.Find(x => x.cameraClusterRange);

            // Raytracing Attributes
            raytracedObjects = o.Find(x => x.raytracedObjects);
            raytracedLayerMask = o.Find(x => x.raytracedLayerMask);
            rayMaxDepth = o.Find(x => x.rayMaxDepth);
            raytracingRayLength = o.Find(x => x.raytracingRayLength);

            // Indirect diffuse Properties
            raytracedIndirectDiffuse = o.Find(x => x.raytracedIndirectDiffuse);
            indirectDiffuseLayerMask = o.Find(x => x.indirectDiffuseLayerMask);
            indirectDiffuseNumSamples = o.Find(x => x.indirectDiffuseNumSamples);
            indirectDiffuseRayLength = o.Find(x => x.indirectDiffuseRayLength);
            indirectDiffuseClampValue = o.Find(x => x.indirectDiffuseClampValue);
            indirectDiffuseFilterMode = o.Find(x => x.indirectDiffuseFilterMode);
            indirectDiffuseFilterRadius = o.Find(x => x.indirectDiffuseFilterRadius);
        }

        public void Update()
        {
            serializedObject.Update();
        }

        public void Apply()
        {
            serializedObject.ApplyModifiedProperties();
        }
#endif
    }
}
