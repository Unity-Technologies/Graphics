using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    // Defines a sub set of world lights using indices into
    // world light datas and world light volumes
    class WorldLightSubSet
    {
        List<uint> m_lightIndicies = new List<uint>();
        GraphicsBuffer m_lightSubsetBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 1, System.Runtime.InteropServices.Marshal.SizeOf(typeof(uint)));

        internal void ResizeLightSubsetBuffer(int numLights)
        {
            int numLightsGpu = Math.Max(numLights, 1);

            // If it is not null and it has already the right size, we are pretty much done
            if (m_lightSubsetBuffer.count == numLightsGpu)
                return;

            // It is not the right size, free it to be reallocated
            CoreUtils.SafeRelease(m_lightSubsetBuffer);

            // Allocate the next buffer buffer
            m_lightSubsetBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, numLightsGpu, System.Runtime.InteropServices.Marshal.SizeOf(typeof(uint)));
        }

        internal void ClearLightSubsetList()
        {
            m_lightIndicies.Clear();
            bounds.SetMinMax(WorldLightManager.minBounds, WorldLightManager.maxBounds);
        }

        public void Add(uint index)
        {
            m_lightIndicies.Add(index);
        }

        internal void PushToGpu()
        {
            if (m_lightIndicies.Count > 0)
                m_lightSubsetBuffer.SetData(m_lightIndicies);
        }

        public Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);

        public void Bind(CommandBuffer cmd, int lightSubSetShaderID)
        {
            cmd.SetGlobalBuffer(lightSubSetShaderID, m_lightSubsetBuffer);
        }

        public int GetCount()
        {
            return m_lightIndicies.Count;
        }

        public GraphicsBuffer GetBuffer()
        {
            return m_lightSubsetBuffer;
        }

        internal void Release()
        {
            CoreUtils.SafeRelease(m_lightSubsetBuffer);
            m_lightSubsetBuffer = null;
        }
    }

    // Represents multiple subsets contained in the same buffer
    class WorldLightSubSetList
    {
        List<uint> m_lightIndicies = new List<uint>();
        GraphicsBuffer m_lightSubsetBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 1, System.Runtime.InteropServices.Marshal.SizeOf(typeof(uint)));

        uint currentOffset = 0;
        List<uint> m_lightOffsetAndCounts = new List<uint>();

        internal void ClearLightSubsetList()
        {
            m_lightIndicies.Clear();
            m_lightOffsetAndCounts.Clear();
            currentOffset = 0;
        }

        internal void ResizeLightSubsetBuffer()
        {
            int numLightsGpu = Math.Max((int) currentOffset, 1);

            // If it is not null and it has already the right size, we are pretty much done
            if (m_lightSubsetBuffer.count == numLightsGpu)
                return;

            // It is not the right size, free it to be reallocated
            CoreUtils.SafeRelease(m_lightSubsetBuffer);

            // Allocate the next buffer buffer
            m_lightSubsetBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, numLightsGpu, System.Runtime.InteropServices.Marshal.SizeOf(typeof(uint)));
        }

        public void Add(uint index)
        {
            m_lightIndicies.Add(index);
        }

        public void AddRange(uint count)
        {
            m_lightOffsetAndCounts.Add(currentOffset);
            m_lightOffsetAndCounts.Add(count);
            currentOffset += count;
        }

        internal void PushToGpu()
        {
            if (m_lightIndicies.Count > 0)
                m_lightSubsetBuffer.SetData(m_lightIndicies);
        }

        public void Bind(CommandBuffer cmd, int lightSubSetShaderID)
        {
            cmd.SetGlobalBuffer(lightSubSetShaderID, m_lightSubsetBuffer);
        }

        public List<uint> GetLightOffsetAndCountsList()
        {
            return m_lightOffsetAndCounts;
        }

        public GraphicsBuffer GetBuffer()
        {
            return m_lightSubsetBuffer;
        }

        public void Release()
        {
            CoreUtils.SafeRelease(m_lightSubsetBuffer);
            m_lightSubsetBuffer = null;
        }
    };

    class WorldLightCulling
    {
        public static void GetLightSubSetUsingFlags(in WorldLightsVolumes worldLightsVolumes, uint flagFilter, WorldLightSubSet worldLightSubSet)
        {
            int totalLights = worldLightsVolumes.GetCount();
            worldLightSubSet.ResizeLightSubsetBuffer(totalLights);
            worldLightSubSet.ClearLightSubsetList();
            worldLightSubSet.bounds.SetMinMax(WorldLightManager.minBounds, WorldLightManager.maxBounds);

            for (uint lightIdx = 0; lightIdx < totalLights; ++lightIdx)
            {
                ref uint flags = ref worldLightsVolumes.GetFlagsRef((int)lightIdx);
                if ((flags & flagFilter) == flagFilter)
                {
                    worldLightSubSet.Add(lightIdx);

                    ref WorldLightVolume volume = ref worldLightsVolumes.GetRef((int)lightIdx);
                    worldLightSubSet.bounds.Encapsulate(volume.position - volume.range);
                    worldLightSubSet.bounds.Encapsulate(volume.position + volume.range);
                }
            }
            worldLightSubSet.PushToGpu();
        }

        internal static bool IntersectSphereAABB(Vector3 position, float radius, Vector3 aabbMin, Vector3 aabbMax)
        {
            float x = Mathf.Max(aabbMin.x, Mathf.Min(position.x, aabbMax.x));
            float y = Mathf.Max(aabbMin.y, Mathf.Min(position.y, aabbMax.y));
            float z = Mathf.Max(aabbMin.z, Mathf.Min(position.z, aabbMax.z));
            float distance2 = ((x - position.x) * (x - position.x) + (y - position.y) * (y - position.y) + (z - position.z) * (z - position.z));
            return distance2 < radius * radius;
        }

        public static void GetLightSubSetUsingFlagsAndBounds(in WorldLightsVolumes worldLightsVolumes, uint flagFilter, in Bounds bounds, WorldLightSubSet worldLightSubSet)
        {
            int totalLights = worldLightsVolumes.GetCount();
            worldLightSubSet.ResizeLightSubsetBuffer(totalLights);
            worldLightSubSet.ClearLightSubsetList();
            worldLightSubSet.bounds.SetMinMax(WorldLightManager.minBounds, WorldLightManager.maxBounds);

            for (uint lightIdx = 0; lightIdx < totalLights; ++lightIdx)
            {
                ref uint flags = ref worldLightsVolumes.GetFlagsRef((int)lightIdx);
                if ((flags & flagFilter) == flagFilter)
                {
                    bool intersects = false;
                    ref WorldLightVolume volume = ref worldLightsVolumes.GetRef((int)lightIdx);
                    if (volume.shape == 0)
                    {
                        intersects = IntersectSphereAABB(volume.position, volume.range.x, bounds.min, bounds.max);
                    }
                    else
                    {
                        intersects = bounds.Intersects(new Bounds(volume.position, volume.range));
                    }

                    if (intersects)
                    {
                        worldLightSubSet.Add(lightIdx);

                        worldLightSubSet.bounds.Encapsulate(volume.position - volume.range);
                        worldLightSubSet.bounds.Encapsulate(volume.position + volume.range);
                    }
                }
            }
            worldLightSubSet.PushToGpu();
        }

        public static void GetLightSubSetUsingFlagsAndBounds(in WorldLightsVolumes worldLightsVolumes, uint flagFilter, in List<Bounds> boundsList, WorldLightSubSetList worldLightSubSetList, in Vector3 cameraPos)
        {
            int totalLights = worldLightsVolumes.GetCount();
            worldLightSubSetList.ClearLightSubsetList();

            foreach (Bounds bounds in boundsList)
            {
                uint count = 0;

                for (uint lightIdx = 0; lightIdx < totalLights; ++lightIdx)
                {
                    ref uint flags = ref worldLightsVolumes.GetFlagsRef((int)lightIdx);
                    if ((flags & flagFilter) == flagFilter)
                    {
                        bool intersects = false;
                        ref WorldLightVolume volume = ref worldLightsVolumes.GetRef((int)lightIdx);

                        Vector3 lightPositionWS = volume.position;
                        if (ShaderConfig.s_CameraRelativeRendering != 0)
                        {
                            lightPositionWS += cameraPos;
                        }

                        if (volume.shape == 0)
                        {
                            intersects = IntersectSphereAABB(lightPositionWS, volume.range.x, bounds.min, bounds.max);
                        }
                        else
                        {
                            intersects = bounds.Intersects(new Bounds(lightPositionWS, volume.range));
                        }

                        if (intersects)
                        {
                            worldLightSubSetList.Add(lightIdx);
                            count++;
                        }
                    }
                }

                worldLightSubSetList.AddRange(count);
            }

            worldLightSubSetList.ResizeLightSubsetBuffer();

            worldLightSubSetList.PushToGpu();
        }
    }
} // namespace UnityEngine.Rendering.HighDefinition
