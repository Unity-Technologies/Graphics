using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    internal class SerializedPlanarReflectionProbe
    {
        internal SerializedObject serializedObject;

        internal SerializedProperty proxyVolumeReference;
        internal SerializedReflectionProxyVolumeComponent reflectionProxyVolume;

        internal SerializedInfluenceVolume influenceVolume;

        internal SerializedProperty captureLocalPosition;
        internal SerializedProperty captureNearPlane;
        internal SerializedProperty captureFarPlane;
        internal SerializedProperty capturePositionMode;
        internal SerializedProperty captureMirrorPlaneLocalPosition;
        internal SerializedProperty captureMirrorPlaneLocalNormal;
        internal SerializedProperty weight;
        internal SerializedProperty multiplier;
        internal SerializedProperty mode;
        internal SerializedProperty refreshMode;
        internal SerializedProperty customTexture;

        internal SerializedProperty overrideFieldOfView;
        internal SerializedProperty fieldOfViewOverride;

        internal SerializedFrameSettings frameSettings;

        internal PlanarReflectionProbe target { get { return serializedObject.targetObject as PlanarReflectionProbe; } }

        internal bool isMirrored
        {
            get
            {
                return refreshMode.intValue == (int)ReflectionProbeRefreshMode.EveryFrame
                    && mode.intValue == (int)ReflectionProbeMode.Realtime
                    && capturePositionMode.intValue == (int)PlanarReflectionProbe.CapturePositionMode.MirrorCamera;
            }
        }

        internal SerializedPlanarReflectionProbe(SerializedObject serializedObject)
        {
            this.serializedObject = serializedObject;

            proxyVolumeReference = serializedObject.Find((PlanarReflectionProbe p) => p.proxyVolume);
            influenceVolume = new SerializedInfluenceVolume(serializedObject.Find((PlanarReflectionProbe p) => p.influenceVolume));

            captureLocalPosition = serializedObject.Find((PlanarReflectionProbe p) => p.captureLocalPosition);
            captureNearPlane = serializedObject.Find((PlanarReflectionProbe p) => p.captureNearPlane);
            captureFarPlane = serializedObject.Find((PlanarReflectionProbe p) => p.captureFarPlane);
            capturePositionMode = serializedObject.Find((PlanarReflectionProbe p) => p.capturePositionMode);
            captureMirrorPlaneLocalPosition = serializedObject.Find((PlanarReflectionProbe p) => p.captureMirrorPlaneLocalPosition);
            captureMirrorPlaneLocalNormal = serializedObject.Find((PlanarReflectionProbe p) => p.captureMirrorPlaneLocalNormal);
            weight = serializedObject.Find((PlanarReflectionProbe p) => p.weight);
            multiplier = serializedObject.Find((PlanarReflectionProbe p) => p.multiplier);
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
                var p = ((PlanarReflectionProbe)serializedObject.targetObjects[i]).proxyVolume;
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

            mode.enumValueIndex = (int)ReflectionProbeMode.Realtime;
            refreshMode.enumValueIndex = (int)ReflectionProbeRefreshMode.EveryFrame;
            capturePositionMode.enumValueIndex = (int)PlanarReflectionProbe.CapturePositionMode.MirrorCamera;

            var updateProxyVolume = reflectionProxyVolume != null
                && serializedObject.targetObjects.Length != reflectionProxyVolume.serializedObject.targetObjects.Length;
            if (!updateProxyVolume && reflectionProxyVolume != null)
            {
                var proxyVolumeTargets = reflectionProxyVolume.serializedObject.targetObjects;
                for (var i = 0; i < serializedObject.targetObjects.Length; i++)
                {
                    if (proxyVolumeTargets[i] != ((PlanarReflectionProbe)serializedObject.targetObjects[i]).proxyVolume)
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
