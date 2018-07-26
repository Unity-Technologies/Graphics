using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    internal abstract class SerializedHDProbe
    {
        internal SerializedObject serializedObject;

        internal SerializedReflectionProxyVolumeComponent proxyVolumeComponent;
        internal SerializedProperty proxyVolumeReference;

        internal SerializedInfluenceVolume influenceVolume;

        internal SerializedFrameSettings frameSettings;

        internal SerializedProperty weight;
        internal SerializedProperty multiplier;
        internal SerializedProperty mode;
        internal SerializedProperty refreshMode;

        internal SerializedProperty resolution;
        internal SerializedProperty shadowDistance;
        internal SerializedProperty cullingMask;
        internal SerializedProperty useOcclusionCulling;
        internal SerializedProperty nearClip;
        internal SerializedProperty farClip;

        internal HDProbe target { get { return serializedObject.targetObject as HDProbe; } }

        internal SerializedHDProbe(SerializedObject serializedObject)
        {
            this.serializedObject = serializedObject;

            proxyVolumeReference = serializedObject.Find((HDProbe p) => p.proxyVolume);
            influenceVolume = new SerializedInfluenceVolume(serializedObject.Find((HDProbe p) => p.influenceVolume));

            frameSettings = new SerializedFrameSettings(serializedObject.Find((HDProbe p) => p.frameSettings));

            weight = serializedObject.Find((HDProbe p) => p.weight);
            multiplier = serializedObject.Find((HDProbe p) => p.multiplier);
            mode = serializedObject.Find((HDProbe p) => p.mode);
            refreshMode = serializedObject.Find((HDProbe p) => p.refreshMode);
        }

        //void InstantiateProxyVolume(SerializedObject serializedObject)
        //{
        //    var objs = new List<Object>();
        //    for (var i = 0; i < serializedObject.targetObjects.Length; i++)
        //    {
        //        var p = ((HDProbe)serializedObject.targetObjects[i]).proxyVolume;
        //        if (p != null)
        //            objs.Add(p);
        //    }

        //    proxyVolumeComponent = objs.Count > 0
        //        ? new SerializedReflectionProxyVolumeComponent(new SerializedObject(objs.ToArray()))
        //        : null;
        //}

        internal virtual void Update()
        {
            serializedObject.Update();
            //InfluenceVolume does not have Update. Add it here if it have in the future.

            ////Force SerializedReflectionProxyVolumeComponent to refresh
            //var updateProxyVolume = proxyVolumeComponent != null
            //    && serializedObject.targetObjects.Length != proxyVolumeComponent.serializedObject.targetObjects.Length;
            //if (!updateProxyVolume && proxyVolumeComponent != null)
            //{
            //    var proxyVolumeTargets = proxyVolumeComponent.serializedObject.targetObjects;
            //    for (var i = 0; i < serializedObject.targetObjects.Length; i++)
            //    {
            //        if (proxyVolumeTargets[i] != ((PlanarReflectionProbe)serializedObject.targetObjects[i]).proxyVolume)
            //        {
            //            updateProxyVolume = true;
            //            break;
            //        }
            //    }
            //}

            //if (updateProxyVolume)
            //    InstantiateProxyVolume(serializedObject);
        }

        internal virtual void Apply()
        {
            serializedObject.ApplyModifiedProperties();
        }
    }
}
