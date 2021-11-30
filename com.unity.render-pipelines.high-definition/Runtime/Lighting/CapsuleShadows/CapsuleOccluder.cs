using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace UnityEngine.Rendering.HighDefinition
{
    [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
    internal struct ShaderVariablesCapsuleOccluders
    {
        public int _CapsuleOccluderCount;
        public int _CapsuleOccluderUseEllipsoid;
        public int _CapsuleOccluderPad0;
        public int _CapsuleOccluderPad1;
    }

    [GenerateHLSL(needAccessors = false)]
    internal struct CapsuleOccluderData
    {
        public Vector3 centerRWS;
        public float radius;
        public Vector3 axisDirWS;
        public float offset;
        public uint lightLayers;
        public float range;
        public float pad0;
        public float pad1;
    }

    [ExecuteAlways]
    public class CapsuleOccluder : MonoBehaviour
    {
        public Vector3 center = Vector3.zero;
        public Quaternion rotation = Quaternion.identity;
        public float radius = 0.1f;
        public float height = 1.0f;
        public float range = 5.0f;
        public LightLayerEnum lightLayersMask = LightLayerEnum.LightLayerDefault;

        internal Matrix4x4 capsuleToWorld
        {
            get
            {
                Transform tr = transform;
                Vector3 scale = tr.lossyScale;
                float xyScale = Mathf.Max(scale.x, scale.y);
                return Matrix4x4.TRS(
                    tr.TransformPoint(center),
                    tr.rotation * rotation,
                    new Vector3(xyScale, xyScale, scale.z));
            }
        }

        private void OnEnable()
        {
            CapsuleOccluderManager.instance.RegisterCapsule(this);
        }

        private void OnDisable()
        {
            CapsuleOccluderManager.instance.DeregisterCapsule(this);
        }

        internal CapsuleOccluderData GetOccluderData(Vector3 originWS)
        {
            Transform tr = transform;
            Matrix4x4 localToWorld = this.capsuleToWorld;

            float offset = Mathf.Max(0.0f, 0.5f * height - radius);

            Vector3 centerRWS = localToWorld.MultiplyPoint3x4(Vector3.zero) - originWS;
            Vector3 axisDirWS = localToWorld.MultiplyVector(Vector3.forward).normalized;
            float radiusWS = localToWorld.MultiplyVector(radius * Vector3.right).magnitude;

            return new CapsuleOccluderData
            {
                centerRWS = centerRWS,
                radius = radiusWS,
                axisDirWS = axisDirWS,
                offset = offset,
                range = range,
                lightLayers = (uint)lightLayersMask,
            };
        }
    }
}
