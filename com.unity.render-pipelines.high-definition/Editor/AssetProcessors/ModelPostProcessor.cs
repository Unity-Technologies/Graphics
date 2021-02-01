using System;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class ModelPostprocessor : AssetPostprocessor
    {
        static void AddAdditionalData<T, AdditionalT>(GameObject go, Action<AdditionalT> initDefault = null)
            where T : Component
            where AdditionalT : Component
        {
            var components = go.GetComponentsInChildren(typeof(T), true);
            foreach (var c in components)
            {
                if (!c.TryGetComponent<AdditionalT>(out _))
                {
                    var hd = c.gameObject.AddComponent<AdditionalT>();
                    if (initDefault != null)
                        initDefault(hd);
                }
            }
        }

        void OnPostprocessModel(GameObject go)
        {
            AddAdditionalData<Camera, HDAdditionalCameraData>(go, HDAdditionalCameraData.InitDefaultHDAdditionalCameraData);
            AddAdditionalData<Light, HDAdditionalLightData>(go, HDAdditionalLightData.InitDefaultHDAdditionalLightData);
            AddAdditionalData<ReflectionProbe, HDAdditionalReflectionData>(go);
        }
    }
}
