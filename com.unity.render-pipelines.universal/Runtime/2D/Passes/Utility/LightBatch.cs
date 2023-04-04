using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.Rendering.Universal
{

    // The idea is to avoid CPU cost when rendering meshes with the same shader (Consider this a light-weight SRP batcher). To identify the Mesh instance ID in the Light Buffer we utilize a Slot Index
    // identified from the Blue Channel of the Vertex Colors (Solely used for this purpose). This can batch a maximum of kLightMod meshes in best-case scenario. Simple but no optizations have been added yet
    internal class LightBatch
    {
        static readonly int kMax = 2048;
        static readonly int kLightMod = 64;
        static readonly int kBatchMax = 256;
        static readonly ProfilingSampler profilingDrawBatched = new ProfilingSampler("Light2D Batcher");
        static readonly int k_BufferOffset = Shader.PropertyToID("_BatchBufferOffset");
        static int sBatchIndexCounter = 0; // For LightMesh asset conditioning to facilitate batching.

        private static int batchLightMod => kLightMod;
        private static float batchRunningIndex => (sBatchIndexCounter++) % kLightMod / (float)kLightMod;
        // Should be in Sync with USE_STRUCTURED_BUFFER_FOR_LIGHT2D_DATA
        public static bool isBatchingSupported => (SystemInfo.graphicsDeviceType != GraphicsDeviceType.OpenGLES3 && SystemInfo.graphicsDeviceType != GraphicsDeviceType.OpenGLCore && SystemInfo.graphicsDeviceType != GraphicsDeviceType.Switch);

        private int[] subsets = new int[kMax];
        private Mesh[] lightMeshes = new Mesh[kMax];
        private Matrix4x4[] matrices = new Matrix4x4[kMax];
        private NativeArray<PerLight2D> lightNativeBuffer = new NativeArray<PerLight2D>(kMax, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        private NativeArray<int> lightMarkers = new NativeArray<int>(kBatchMax, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        private GraphicsBuffer lightGraphicsBuffer;

        private Light2D cachedLight;
        private Material cachedMaterial;
        private int hashCode = 0;
        private int lightCount = 0;
        private int maxIndex = 0;
        private int batchCount = 0;
        internal PerLight2D GetLight(int index) => lightNativeBuffer[index];
        internal static int batchSlotIndex => (int)(batchRunningIndex * kLightMod);
#if UNITY_EDITOR
        static bool kRegisterCallback = false;
#endif

        internal void SetLight(int index, PerLight2D light)
        {
            lightNativeBuffer[index] = light;
        }

        internal static float GetBatchColor(int batchSlotIndex)
        {
            return (float)batchSlotIndex / (float)batchLightMod;
        }

        internal static int GetBatchSlotIndex(float channelColor)
        {
            return (int)(channelColor * kLightMod);
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
            if (lightGraphicsBuffer == null)
                lightGraphicsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, kMax, UnsafeUtility.SizeOf<PerLight2D>());
        }

        void OnAssemblyReload()
        {
            lightGraphicsBuffer.Release();
        }

        void SetBuffer(CommandBuffer cmd)
        {
            Validate();
            lightGraphicsBuffer.SetData(lightNativeBuffer, lightCount, lightCount, kBatchMax);
            Shader.SetGlobalBuffer("_Light2DBuffer", lightGraphicsBuffer);
        }

        internal int SlotIndex(int x)
        {
            return lightCount + x;
        }

        internal void Reset()
        {
            unsafe { UnsafeUtility.MemClear(lightNativeBuffer.GetUnsafePtr(), UnsafeUtility.SizeOf<PerLight2D>() * kMax); }
            maxIndex = 0;
            hashCode = 0;
            batchCount = 0;
            lightCount = 0;
        }

        internal bool CanBatch(Light2D light, Material material, int index, out int lightHash)
        {
            Debug.Assert(lightCount < kMax);
            lightHash = Hash(light, material);
            hashCode = (hashCode == 0) ? lightHash : hashCode;
            if (batchCount == 0)
            {
                hashCode = lightHash;
            }
            else if (hashCode != lightHash || SlotIndex(index) >= kMax || lightMarkers[index] == 1)
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
            lightMarkers[index] = 1;
            return true;
        }

        internal void Flush(CommandBuffer cmd)
        {
            if (batchCount > 0)
            {
                using (new ProfilingScope(cmd, profilingDrawBatched))
                {
                    SetBuffer(cmd);
                    cmd.SetGlobalInt(k_BufferOffset, lightCount);
                    cmd.DrawMultipleMeshes(matrices, lightMeshes, subsets, batchCount, cachedMaterial, -1, null);
                }
                lightCount = lightCount + maxIndex + 1;
            }
            for (int i = 0; i < batchCount; ++i)
                lightMeshes[i] = null;
            unsafe { UnsafeUtility.MemClear(lightMarkers.GetUnsafePtr(), UnsafeUtility.SizeOf<int>() * kBatchMax); }
            batchCount = 0;
            maxIndex = 0;
        }
    }
}
