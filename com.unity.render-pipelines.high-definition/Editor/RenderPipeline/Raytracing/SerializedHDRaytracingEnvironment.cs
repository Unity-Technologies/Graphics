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

        // Reflections Attributes
        public SerializedProperty reflLayerMask;

        // Primary visiblity raytracing
        public SerializedProperty raytracedLayerMask;

        // Area Shadow Properties
        public SerializedProperty shadowLayerMask;

        // Indirect diffuse Properties
        public SerializedProperty indirectDiffuseLayerMask;

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

            // Raytracing Attributes
            raytracedLayerMask = o.Find(x => x.raytracedLayerMask);

            // Indirect diffuse Properties
            indirectDiffuseLayerMask = o.Find(x => x.indirectDiffuseLayerMask);
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
