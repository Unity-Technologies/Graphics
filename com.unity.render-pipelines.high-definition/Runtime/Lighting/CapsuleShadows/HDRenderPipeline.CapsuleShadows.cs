using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    internal struct CapsuleOccluderList
    {
        public bool useSphereBounds;
        public List<OrientedBBox> bounds;
        public List<CapsuleOccluderData> occluders;
    };

    public partial class HDRenderPipeline
    {
        private const int k_MaxVisibleCapsuleOccluders = 256;

        List<OrientedBBox> m_VisibleCapsuleOccluderBounds = null;
        List<CapsuleOccluderData> m_VisibleCapsuleOccluderData = null;
        ComputeBuffer m_VisibleCapsuleOccluderDataBuffer = null;

        internal void InitializeCapsuleShadows()
        {
            m_VisibleCapsuleOccluderBounds = new List<OrientedBBox>();
            m_VisibleCapsuleOccluderData = new List<CapsuleOccluderData>();
            m_VisibleCapsuleOccluderDataBuffer = new ComputeBuffer(k_MaxVisibleCapsuleOccluders, Marshal.SizeOf(typeof(CapsuleOccluderData)));
        }

        internal void CleanupCapsuleShadows()
        {
            CoreUtils.SafeRelease(m_VisibleCapsuleOccluderDataBuffer);
            m_VisibleCapsuleOccluderData = null;
            m_VisibleCapsuleOccluderBounds = null;
        }

        internal CapsuleOccluderList PrepareVisibleCapsuleOccludersList(HDCamera hdCamera, CommandBuffer cmd, CullingResults cullResults)
        {
            Vector3 originWS = Vector3.zero;
            if (ShaderConfig.s_CameraRelativeRendering != 0)
            {
                originWS = hdCamera.camera.transform.position;
            }

            // if there is a single light with capsule shadows, build optimised bounds
            HDLightRenderDatabase lightEntities = HDLightRenderDatabase.instance;
            bool optimiseBoundsForLight = false;
            Vector3 lightDirection = Vector3.zero;
            float lightHalfAngle = 0.0f;
            float maxRange = 0.0f;
            for (int i = 0; i < cullResults.visibleLights.Length; ++i)
            {
                VisibleLight visibleLight = cullResults.visibleLights[i];
                Light light = visibleLight.light;

                int dataIndex = lightEntities.FindEntityDataIndex(light);
                if (dataIndex == HDLightRenderDatabase.InvalidDataIndex)
                    continue;

                HDAdditionalLightData lightData = lightEntities.hdAdditionalLightData[dataIndex];
                if (lightData.enableCapsuleShadows)
                {
                    maxRange = Mathf.Max(maxRange, lightData.capsuleShadowRange);

                    if (light.type == LightType.Directional && !optimiseBoundsForLight)
                    {
                        // optimise for this light, continue checking through the visible list
                        optimiseBoundsForLight = true;
                        lightDirection = visibleLight.GetForward().normalized;
                        lightHalfAngle = lightData.angularDiameter * Mathf.Deg2Rad * 0.5f;
                    }
                    else
                    {
                        // cannot optimise for a single directional light, disable and early out
                        optimiseBoundsForLight = false;
                        break;
                    }
                }
            }

            m_VisibleCapsuleOccluderBounds.Clear();
            m_VisibleCapsuleOccluderData.Clear();

            CapsuleShadowsVolumeComponent capsuleShadows = hdCamera.volumeStack.GetComponent<CapsuleShadowsVolumeComponent>();
            if (capsuleShadows.enable.value)
            {
                var occluders = CapsuleOccluderManager.instance.occluders;
                foreach (CapsuleOccluder occluder in occluders)
                {
                    if (m_VisibleCapsuleOccluderData.Count >= k_MaxVisibleCapsuleOccluders)
                    {
                        break;
                    }

                    CapsuleOccluderData data = occluder.GetOccluderData(originWS);

                    OrientedBBox bbox;
                    if (optimiseBoundsForLight)
                    {
                        // align local X with the capsule axis
                        Vector3 localZ = lightDirection;
                        Vector3 localX = Vector3.Cross(localZ, data.axisDirWS).normalized;
                        Vector3 localY = Vector3.Cross(localZ, localX);

                        // capsule bounds, extended along light direction
                        Vector3 halfExtentLS = new Vector3(
                            Mathf.Abs(Vector3.Dot(data.axisDirWS, localX)) * data.offset + data.radius,
                            Mathf.Abs(Vector3.Dot(data.axisDirWS, localY)) * data.offset + data.radius,
                            Mathf.Abs(Vector3.Dot(data.axisDirWS, localZ)) * data.offset + data.radius);
                        halfExtentLS.z += maxRange;

                        // expand by max penumbra
                        float penumbraSize = Mathf.Tan(lightHalfAngle) * maxRange;
                        halfExtentLS.x += penumbraSize;
                        halfExtentLS.y += penumbraSize;

                        bbox = new OrientedBBox(new Matrix4x4(
                            2.0f * halfExtentLS.x * localX,
                            2.0f * halfExtentLS.y * localY,
                            2.0f * halfExtentLS.z * localZ,
                            data.centerRWS));
                    }
                    else
                    {
                        float length = 2.0f * maxRange;
                        bbox = new OrientedBBox(
                            Matrix4x4.TRS(data.centerRWS, Quaternion.identity, new Vector3(length, length, length)));
                    }

                    // Frustum cull on the CPU for now.
                    if (GeometryUtils.Overlap(bbox, hdCamera.frustum, 6, 8))
                    {
                        m_VisibleCapsuleOccluderBounds.Add(bbox);
                        m_VisibleCapsuleOccluderData.Add(data);
                    }
                }
            }

            m_VisibleCapsuleOccluderDataBuffer.SetData(m_VisibleCapsuleOccluderData);

            return new CapsuleOccluderList
            {
                useSphereBounds = !optimiseBoundsForLight,
                bounds = m_VisibleCapsuleOccluderBounds,
                occluders = m_VisibleCapsuleOccluderData,
            };
        }

        internal void UpdateShaderVariablesGlobalCapsuleShadows(ref ShaderVariablesGlobal cb)
        {
            cb._CapsuleOccluderCount = (uint)m_VisibleCapsuleOccluderData.Count;
        }

        internal void BindGlobalCapsuleShadowBuffers(CommandBuffer cmd)
        {
            cmd.SetGlobalBuffer(HDShaderIDs._CapsuleOccluderDatas, m_VisibleCapsuleOccluderDataBuffer);
        }
    }
}
