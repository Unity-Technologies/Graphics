using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public class SerializedPlanarReflectionProbe
    {
        public SerializedObject serializedObject;

        public SerializedProperty proxyVolumeReference;
        public SerializedReflectionProxyVolumeComponent reflectionProxyVolume;

        public SerializedInfluenceVolume influenceVolume;

        public SerializedProperty captureLocalPosition;
        public SerializedProperty captureNearPlane;
        public SerializedProperty captureFarPlane;
        public SerializedProperty capturePositionMode;
        public SerializedProperty captureMirrorPlaneLocalPosition;
        public SerializedProperty captureMirrorPlaneLocalNormal;
        public SerializedProperty dimmer;
        public SerializedProperty mode;
        public SerializedProperty refreshMode;
        public SerializedProperty customTexture;

        public SerializedProperty overrideFieldOfView;
        public SerializedProperty fieldOfViewOverride;

        public SerializedFrameSettings frameSettings;

        public PlanarReflectionProbe target { get { return serializedObject.targetObject as PlanarReflectionProbe; } }

        public bool isMirrored
        {
            get
            {
                return refreshMode.intValue == (int)ReflectionProbeRefreshMode.EveryFrame
                    && mode.intValue == (int)ReflectionProbeMode.Realtime
                    && capturePositionMode.intValue == (int)PlanarReflectionProbe.CapturePositionMode.MirrorCamera;
            }
        }

        public SerializedPlanarReflectionProbe(SerializedObject serializedObject)
        {
            this.serializedObject = serializedObject;

            proxyVolumeReference = serializedObject.Find((PlanarReflectionProbe p) => p.proxyVolumeReference);
            influenceVolume = new SerializedInfluenceVolume(serializedObject.Find((PlanarReflectionProbe p) => p.influenceVolume));

            captureLocalPosition = serializedObject.Find((PlanarReflectionProbe p) => p.captureLocalPosition);
            captureNearPlane = serializedObject.Find((PlanarReflectionProbe p) => p.captureNearPlane);
            captureFarPlane = serializedObject.Find((PlanarReflectionProbe p) => p.captureFarPlane);
            capturePositionMode = serializedObject.Find((PlanarReflectionProbe p) => p.capturePositionMode);
            captureMirrorPlaneLocalPosition = serializedObject.Find((PlanarReflectionProbe p) => p.captureMirrorPlaneLocalPosition);
            captureMirrorPlaneLocalNormal = serializedObject.Find((PlanarReflectionProbe p) => p.captureMirrorPlaneLocalNormal);
            dimmer = serializedObject.Find((PlanarReflectionProbe p) => p.dimmer);
            mode = serializedObject.Find((PlanarReflectionProbe p) => p.mode);
            refreshMode = serializedObject.Find((PlanarReflectionProbe p) => p.refreshMode);
            customTexture = serializedObject.Find((PlanarReflectionProbe p) => p.customTexture);

            overrideFieldOfView = serializedObject.Find((PlanarReflectionProbe p) => p.overrideFieldOfView);
            fieldOfViewOverride = serializedObject.Find((PlanarReflectionProbe p) => p.fieldOfViewOverride);

            frameSettings = new SerializedFrameSettings(serializedObject.Find((PlanarReflectionProbe p) => p.frameSettings));

            InstantiateProxyVolume(serializedObject);
        }

        void InstantiateProxyVolume(SerializedObject serializedObject)
        {
            var objs = new List<Object>();
            for (var i = 0; i < serializedObject.targetObjects.Length; i++)
            {
                var p = ((PlanarReflectionProbe)serializedObject.targetObjects[i]).proxyVolumeReference;
                if (p != null)
                    objs.Add(p);
            }

            reflectionProxyVolume = objs.Count > 0
                    ? new SerializedReflectionProxyVolumeComponent(new SerializedObject(objs.ToArray()))
                    : null;
        }

        public void Update()
        {
            serializedObject.Update();

            var updateProxyVolume = reflectionProxyVolume != null
                && serializedObject.targetObjects.Length != reflectionProxyVolume.serializedObject.targetObjects.Length;
            if (!updateProxyVolume && reflectionProxyVolume != null)
            {
                var proxyVolumeTargets = reflectionProxyVolume.serializedObject.targetObjects;
                for (var i = 0; i < serializedObject.targetObjects.Length; i++)
                {
                    if (proxyVolumeTargets[i] != ((PlanarReflectionProbe)serializedObject.targetObjects[i]).proxyVolumeReference)
                    {
                        updateProxyVolume = true;
                        break;
                    }
                }
            }

            if (updateProxyVolume)
                InstantiateProxyVolume(serializedObject);
        }

        public void Apply()
        {
            serializedObject.ApplyModifiedProperties();
            if (reflectionProxyVolume != null)
                reflectionProxyVolume.Apply();
        }
    }
}
