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
        public SerializedProperty aoFilterMode;
        public SerializedProperty aoRayLength;
        public SerializedProperty aoNumSamples;
        public SerializedProperty aoBilateralRadius;
        public SerializedProperty aoBilateralSigma;
        public SerializedProperty maxFilterWidthInPixels;
        public SerializedProperty filterRadiusInMeters;
        public SerializedProperty normalSharpness;

        // Light Cluster Attributes
        public SerializedProperty maxNumLightsPercell;
        public SerializedProperty cameraClusterRange;

        // Reflections Attributes
        public SerializedProperty raytracedReflections;
        public SerializedProperty reflRayLength;
        public SerializedProperty reflNumMaxSamples;
        public SerializedProperty reflFilterMode;
        public SerializedProperty reflBilateralRadius;
        public SerializedProperty reflBilateralSigma;

        // Primary visiblity raytracing
        public SerializedProperty raytracedObjects;
        public SerializedProperty rayMaxDepth;
        public SerializedProperty raytracingRayLength;

        // Area Shadow Properties
        public SerializedProperty raytracedShadows;
        public SerializedProperty shadowNumSamples;
        public SerializedProperty numAreaLightShadows;
        public SerializedProperty shadowFilterRadius;
        public SerializedProperty shadowFilterSigma;

        public SerializedHDRaytracingEnvironment(HDRaytracingEnvironment rtEnv)
        {
            serializedObject = new SerializedObject(rtEnv);

            var o = new PropertyFetcher<HDRaytracingEnvironment>(serializedObject);

            // Ambient Occlusion
            rayBias = o.Find(x => x.rayBias);
            raytracedAO = o.Find(x => x.raytracedAO);
            aoFilterMode = o.Find(x => x.aoFilterMode);
            aoRayLength = o.Find(x => x.aoRayLength);
            aoNumSamples = o.Find(x => x.aoNumSamples);
            aoBilateralRadius = o.Find(x => x.aoBilateralRadius);
            aoBilateralSigma = o.Find(x => x.aoBilateralSigma);
            maxFilterWidthInPixels = o.Find(x => x.maxFilterWidthInPixels);
            filterRadiusInMeters = o.Find(x => x.filterRadiusInMeters);
            normalSharpness = o.Find(x => x.normalSharpness);

            // Reflections Attributes
            raytracedReflections = o.Find(x => x.raytracedReflections);
            reflRayLength = o.Find(x => x.reflRayLength);
            reflNumMaxSamples = o.Find(x => x.reflNumMaxSamples);
            reflFilterMode = o.Find(x => x.reflFilterMode);
            reflBilateralRadius = o.Find(x => x.reflBilateralRadius);
            reflBilateralSigma = o.Find(x => x.reflBilateralSigma);

            // Shadows Attributes
            raytracedShadows = o.Find(x => x.raytracedShadows);
            shadowNumSamples = o.Find(x => x.shadowNumSamples);
            numAreaLightShadows = o.Find(x => x.numAreaLightShadows);
            shadowFilterRadius = o.Find(x => x.shadowFilterRadius);
            shadowFilterSigma = o.Find(x => x.shadowFilterSigma);

            // Light Cluster Attributes
            maxNumLightsPercell = o.Find(x => x.maxNumLightsPercell);
            cameraClusterRange = o.Find(x => x.cameraClusterRange);

            // Raytracing Attributes
            raytracedObjects = o.Find(x => x.raytracedObjects);
            rayMaxDepth = o.Find(x => x.rayMaxDepth);
            raytracingRayLength = o.Find(x => x.raytracingRayLength);
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
