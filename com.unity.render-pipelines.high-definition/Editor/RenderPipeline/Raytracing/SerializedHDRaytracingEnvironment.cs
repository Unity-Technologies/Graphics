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
        public SerializedProperty aoLayerMask;

        // Light Cluster Attributes
        public SerializedProperty maxNumLightsPercell;
        public SerializedProperty cameraClusterRange;

        // Reflections Attributes
        public SerializedProperty reflLayerMask;

        // Primary visiblity raytracing
        public SerializedProperty raytracedObjects;
        public SerializedProperty raytracedLayerMask;
        public SerializedProperty rayMaxDepth;
        public SerializedProperty raytracingRayLength;

        // Area Shadow Properties
        public SerializedProperty shadowLayerMask;

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
            aoLayerMask = o.Find(x => x.aoLayerMask);

            // Reflections Attributes
            reflLayerMask = o.Find(x => x.reflLayerMask);

            // Shadows Attributes
            shadowLayerMask = o.Find(x => x.shadowLayerMask);

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
