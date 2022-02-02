using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    internal struct CapsuleShadowList
    {
        public List<OrientedBBox> bounds;
        public List<CapsuleOccluderData> occluders;
    }

    internal struct CapsuleOccluderList
    {
        public List<OrientedBBox> bounds;
        public List<CapsuleOccluderData> occluders;
        public bool directUsesSphereBounds;
        public int directCount;
        public int indirectCount;

        public void Clear()
        {
            bounds.Clear();
            occluders.Clear();
            directUsesSphereBounds = false;
            directCount = 0;
            indirectCount = 0;
        }

        public int TotalCount()
        {
            return directCount + indirectCount;
        }
    };

    public partial class HDRenderPipeline
    {
        private const int k_MaxVisibleCapsuleOccluders = 256;

        CapsuleOccluderList m_CapsuleOccluders;
        ComputeBuffer m_CapsuleOccluderDataBuffer;

        internal void InitializeCapsuleShadows()
        {
            m_CapsuleOccluders.bounds = new List<OrientedBBox>();
            m_CapsuleOccluders.occluders = new List<CapsuleOccluderData>();
            m_CapsuleOccluderDataBuffer = new ComputeBuffer(k_MaxVisibleCapsuleOccluders, Marshal.SizeOf(typeof(CapsuleOccluderData)));
        }

        internal void CleanupCapsuleShadows()
        {
            CoreUtils.SafeRelease(m_CapsuleOccluderDataBuffer);
            m_CapsuleOccluders.occluders = null;
            m_CapsuleOccluders.bounds = null;
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
                        lightHalfAngle = Mathf.Max(lightData.angularDiameter, lightData.capsuleShadowMinimumAngle) * Mathf.Deg2Rad * 0.5f;
                    }
                    else
                    {
                        // cannot optimise for a single directional light, disable and early out
                        optimiseBoundsForLight = false;
                        break;
                    }
                }
            }

            m_CapsuleOccluders.Clear();
            m_CapsuleOccluders.directUsesSphereBounds = !optimiseBoundsForLight;

            using (ListPool<OrientedBBox>.Get(out List<OrientedBBox> indirectBounds))
            using (ListPool<CapsuleOccluderData>.Get(out List<CapsuleOccluderData> indirectOccluders))
            {
                CapsuleShadowsVolumeComponent capsuleShadows = hdCamera.volumeStack.GetComponent<CapsuleShadowsVolumeComponent>();
                bool enableDirectShadows = capsuleShadows.enableDirectShadows.value;
                float indirectRangeFactor = capsuleShadows.indirectRangeFactor.value;
                bool enableIndirectShadows = capsuleShadows.enableIndirectShadows.value &&  indirectRangeFactor > 0.0f;
                if (enableDirectShadows || enableIndirectShadows)
                {
                    bool scalePenumbraAlongX = m_CurrentDebugDisplaySettings.data.lightingDebugSettings.capsuleShadowMethod == CapsuleShadowMethod.Ellipsoid;
                    var occluders = CapsuleOccluderManager.instance.occluders;
                    foreach (CapsuleOccluder occluder in occluders)
                    {
                        if (m_CapsuleOccluders.TotalCount() >= k_MaxVisibleCapsuleOccluders)
                            break;

                        CapsuleOccluderData data = occluder.GetOccluderData(originWS);

                        if (enableDirectShadows)
                        {
                            OrientedBBox bounds;
                            if (optimiseBoundsForLight)
                            {
                                // align local X with the capsule axis
                                Vector3 localZ = lightDirection;
                                Vector3 localY = Vector3.Cross(localZ, data.axisDirWS).normalized;
                                Vector3 localX = Vector3.Cross(localY, localZ);

                                // capsule bounds, extended along light direction
                                Vector3 centerRWS = data.centerRWS;
                                Vector3 halfExtentLS = new Vector3(
                                    Mathf.Abs(Vector3.Dot(data.axisDirWS, localX)) * data.offset + data.radius,
                                    Mathf.Abs(Vector3.Dot(data.axisDirWS, localY)) * data.offset + data.radius,
                                    Mathf.Abs(Vector3.Dot(data.axisDirWS, localZ)) * data.offset + data.radius);
                                halfExtentLS.z += 0.5f * maxRange;
                                centerRWS += (0.5f * maxRange) * localZ;

                                // expand by max penumbra
                                float penumbraSize = Mathf.Tan(lightHalfAngle) * maxRange;
                                halfExtentLS.x += scalePenumbraAlongX ? (penumbraSize*(data.offset + data.radius)/data.radius) : penumbraSize;
                                halfExtentLS.y += penumbraSize;

                                bounds = new OrientedBBox(new Matrix4x4(
                                    2.0f * halfExtentLS.x * localX,
                                    2.0f * halfExtentLS.y * localY,
                                    2.0f * halfExtentLS.z * localZ,
                                    centerRWS));
                            }
                            else
                            {
                                // max distance from *surface* of capsule
                                float length = 2.0f * (data.offset + data.radius + maxRange);
                                bounds = new OrientedBBox(
                                    Matrix4x4.TRS(data.centerRWS, Quaternion.identity, new Vector3(length, length, length)));
                            }

                            // Frustum cull on the CPU for now.
                            if (GeometryUtils.Overlap(bounds, hdCamera.frustum, 6, 8))
                            {
                                m_CapsuleOccluders.bounds.Add(bounds);
                                m_CapsuleOccluders.occluders.Add(data);
                                m_CapsuleOccluders.directCount += 1;
                            }
                        }

                        if (enableIndirectShadows && m_CapsuleOccluders.TotalCount() < k_MaxVisibleCapsuleOccluders)
                        {
                            float length = 2.0f * (data.offset + data.radius*(1.0f + indirectRangeFactor));
                            OrientedBBox bounds = new OrientedBBox(
                                 Matrix4x4.TRS(data.centerRWS, Quaternion.identity, new Vector3(length, length, length)));
                            if (GeometryUtils.Overlap(bounds, hdCamera.frustum, 6, 8))
                            {
                                indirectBounds.Add(bounds);
                                indirectOccluders.Add(data);
                                m_CapsuleOccluders.indirectCount += 1;
                            }    
                        }
                    }
                }

                m_CapsuleOccluders.bounds.AddRange(indirectBounds);
                m_CapsuleOccluders.occluders.AddRange(indirectOccluders);
            }

            m_CapsuleOccluderDataBuffer.SetData(m_CapsuleOccluders.occluders);

            return m_CapsuleOccluders;
        }

        internal void UpdateShaderVariablesGlobalCapsuleShadows(ref ShaderVariablesGlobal cb, HDCamera hdCamera)
        {
            CapsuleShadowsVolumeComponent capsuleShadows = hdCamera.volumeStack.GetComponent<CapsuleShadowsVolumeComponent>();

            uint indirectCountAndFlags = (uint)m_CapsuleOccluders.indirectCount | ((uint)capsuleShadows.indirectShadowMethod.value << 24);
            switch (capsuleShadows.indirectShadowMethod.value)
            {
                case CapsuleIndirectShadowMethod.AmbientOcclusion:
                    indirectCountAndFlags |= (uint)capsuleShadows.ambientOcclusionMethod.value << 28;
                    break;
                case CapsuleIndirectShadowMethod.Directional:
                    break;
            }

            cb._CapsuleDirectShadowCount = (uint)m_CapsuleOccluders.directCount;
            cb._CapsuleIndirectShadowCountAndFlags = indirectCountAndFlags;
            cb._CapsuleIndirectRangeFactor = capsuleShadows.indirectRangeFactor.value;

            var indirectShadowSettings = CapsuleIndirectShadowSettings.instance;
            if (indirectShadowSettings != null)
            {
                cb._CapsuleIndirectDirection = indirectShadowSettings.transform.up;
                cb._CapsuleIndirectCosAngle = Mathf.Cos(Mathf.Deg2Rad * indirectShadowSettings.angle);
            }
            else
            {
                cb._CapsuleIndirectDirection = Vector3.up;
                cb._CapsuleIndirectCosAngle = Mathf.Cos(Mathf.Deg2Rad * 45.0f);
            }
        }

        internal void BindGlobalCapsuleShadowBuffers(CommandBuffer cmd)
        {
            cmd.SetGlobalBuffer(HDShaderIDs._CapsuleOccluderDatas, m_CapsuleOccluderDataBuffer);
        }
    }
}
