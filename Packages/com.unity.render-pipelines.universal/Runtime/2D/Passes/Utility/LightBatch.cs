using UnityEngine.Experimental.Rendering;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.Rendering.Universal
{
    internal class LightBuffer
    {
        static readonly internal int kMax = 2048 * 8;
        static readonly internal int kCount = 1;
        static readonly internal int kLightMod = 64;
        static readonly internal int kBatchMax = 256;

        private GraphicsBuffer m_GraphicsBuffer;
        private NativeArray<int> m_Markers = new NativeArray<int>(kBatchMax, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        private NativeArray<PerLight2D> m_NativeBuffer = new NativeArray<PerLight2D>(kMax, Allocator.Persistent, NativeArrayOptions.ClearMemory);

        internal GraphicsBuffer graphicsBuffer
        {
            get
            {
                if (null == m_GraphicsBuffer)
                    m_GraphicsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, kMax, UnsafeUtility.SizeOf<PerLight2D>());
                return m_GraphicsBuffer;
            }
        }

        internal NativeArray<int> lightMarkers
        {
            get
            {
                return m_Markers;
            }
        }

        internal NativeArray<PerLight2D> nativeBuffer
        {
            get
            {
                return m_NativeBuffer;
            }
        }

        internal void Release()
        {
            m_GraphicsBuffer.Release();
            m_GraphicsBuffer = null;
        }

        internal void Reset()
        {
            unsafe { UnsafeUtility.MemClear(m_Markers.GetUnsafePtr(), UnsafeUtility.SizeOf<int>() * LightBuffer.kBatchMax); }
            unsafe { UnsafeUtility.MemClear(m_NativeBuffer.GetUnsafePtr(), UnsafeUtility.SizeOf<PerLight2D>() * LightBuffer.kMax); }
        }

    }

    // The idea is to avoid CPU cost when rendering meshes with the same shader (Consider this a light-weight SRP batcher). To identify the Mesh instance ID in the Light Buffer we utilize a Slot Index
    // identified from the Blue Channel of the Vertex Colors (Solely used for this purpose). This can batch a maximum of kLightMod meshes in best-case scenario. Simple but no optizations have been added yet
    internal class LightBatch
    {

        static readonly ProfilingSampler profilingDrawBatched = new ProfilingSampler("Light2D Batcher");
        static readonly int k_BufferOffset = Shader.PropertyToID("_BatchBufferOffset");
        static int sBatchIndexCounter = 0; // For LightMesh asset conditioning to facilitate batching.

        private static int batchLightMod => LightBuffer.kLightMod;
        private static float batchRunningIndex => (sBatchIndexCounter++) % LightBuffer.kLightMod / (float)LightBuffer.kLightMod;
        // Should be in Sync with USE_STRUCTURED_BUFFER_FOR_LIGHT2D_DATA
        public static bool isBatchingSupported => false;

        private int[] subsets = new int[LightBuffer.kMax];
        private Mesh[] lightMeshes = new Mesh[LightBuffer.kMax];
        private Matrix4x4[] matrices = new Matrix4x4[LightBuffer.kMax];
        private LightBuffer[] lightBuffer = new LightBuffer[LightBuffer.kCount];

        private Light2D cachedLight;
        private Material cachedMaterial;
        private int hashCode = 0;
        private int lightCount = 0;
        private int maxIndex = 0;
        private int batchCount = 0;
        private int activeCount = 0;

        internal NativeArray<PerLight2D> nativeBuffer
        {
            get
            {
                if (null == lightBuffer[activeCount])
                    lightBuffer[activeCount] = new LightBuffer();
                return lightBuffer[activeCount].nativeBuffer;
            }
        }

        internal GraphicsBuffer graphicsBuffer
        {
            get
            {
                if (null == lightBuffer[activeCount])
                    lightBuffer[activeCount] = new LightBuffer();
                return lightBuffer[activeCount].graphicsBuffer;
            }
        }

        internal NativeArray<int> lightMarker
        {
            get
            {
                if (null == lightBuffer[activeCount])
                    lightBuffer[activeCount] = new LightBuffer();
                return lightBuffer[activeCount].lightMarkers;
            }
        }

        internal PerLight2D GetLight(int index) => nativeBuffer[index];
        internal static int batchSlotIndex => (int)(batchRunningIndex * LightBuffer.kLightMod);
#if UNITY_EDITOR
        static bool kRegisterCallback = false;
#endif

        internal void SetLight(int index, PerLight2D light)
        {
            var buffer = nativeBuffer;
            buffer[index] = light;
        }

        internal static float GetBatchColor()
        {
            return (float)batchSlotIndex / (float)batchLightMod;
        }

        internal static int GetBatchSlotIndex(float channelColor)
        {
            return (int)(channelColor * LightBuffer.kLightMod);
        }

        static int Hash(Light2D light, Material material)
        {
            unchecked
            {
                int _hashCode = (int)2166136261;
                _hashCode = _hashCode * 16777619 ^ material.GetHashCode();
                _hashCode = _hashCode * 16777619 ^ (light.lightCookieSprite == null ? 0 : light.lightCookieSprite.GetHashCode());
                return _hashCode;
            }
        }

        void Validate()
        {
#if UNITY_EDITOR
            if (!kRegisterCallback)
                UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += OnAssemblyReload;
            kRegisterCallback = true;
#endif
        }

        void OnAssemblyReload()
        {
            for (int i = 0; i < LightBuffer.kCount; ++i)
                lightBuffer[activeCount].Release();
        }

        void ResetInternals()
        {
            for (int i = 0; i < LightBuffer.kCount; ++i)
                if (null != lightBuffer[i])
                    lightBuffer[i].Reset();
        }

        void SetBuffer()
        {
            Validate();
            graphicsBuffer.SetData(nativeBuffer, lightCount, lightCount, math.min(LightBuffer.kBatchMax, LightBuffer.kMax - lightCount));
        }

        internal int SlotIndex(int x)
        {
            return lightCount + x;
        }

        internal void Reset()
        {
            if (isBatchingSupported)
            {
                maxIndex = 0;
                hashCode = 0;
                batchCount = 0;
                lightCount = 0;
                activeCount = 0;
                Shader.SetGlobalBuffer("_Light2DBuffer", graphicsBuffer);
            }
        }

        internal bool CanBatch(Light2D light, Material material, int index, out int lightHash)
        {
            lightHash = Hash(light, material);
            hashCode = (hashCode == 0) ? lightHash : hashCode;
            if (batchCount == 0)
            {
                hashCode = lightHash;
            }
            else if (hashCode != lightHash || SlotIndex(index) >= LightBuffer.kMax || lightMarker[index] == 1)
            {
                hashCode = lightHash;
                return false;
            }
            return true;
        }

        internal bool AddBatch(Light2D light, Material material, Matrix4x4 mat, Mesh mesh, int subset, int lightHash, int index)
        {
            Debug.Assert(lightHash == hashCode);
            cachedLight = light;
            cachedMaterial = material;
            matrices[batchCount] = mat;
            lightMeshes[batchCount] = mesh;
            subsets[batchCount] = subset;
            batchCount++;
            maxIndex = math.max(maxIndex, index);
            var lightMark = lightMarker;
            lightMark[index] = 1;
            return true;
        }

        internal void Flush(RasterCommandBuffer cmd)
        {
            if (batchCount > 0)
            {
                using (new ProfilingScope(cmd, profilingDrawBatched))
                {
                    SetBuffer();
                    cmd.SetGlobalInt(k_BufferOffset, lightCount);
                    cmd.DrawMultipleMeshes(matrices, lightMeshes, subsets, batchCount, cachedMaterial, -1, null);
                }

                lightCount = lightCount + maxIndex + 1;
            }
            for (int i = 0; i < batchCount; ++i)
                lightMeshes[i] = null;
            ResetInternals();
            batchCount = 0;
            maxIndex = 0;
        }
    }
}
