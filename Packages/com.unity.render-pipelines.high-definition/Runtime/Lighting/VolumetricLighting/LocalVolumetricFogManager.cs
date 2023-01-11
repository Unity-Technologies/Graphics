using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;
using System;
using System.Runtime.InteropServices;

namespace UnityEngine.Rendering.HighDefinition
{
    class LocalVolumetricFogManager
    {
        // Allocate graphics buffers by chunk to avoid reallocating them too often
        private static readonly int k_IndirectBufferChunkSize = 50;

        static LocalVolumetricFogManager m_Manager;
        public static LocalVolumetricFogManager manager
        {
            get
            {
                if (m_Manager == null)
                    m_Manager = new LocalVolumetricFogManager();
                return m_Manager;
            }
        }

        List<LocalVolumetricFog> m_Volumes = null;

        /// <summary>Stores all the indirect arguments for the indirect draws of the fog</summary>
        internal GraphicsBuffer globalIndirectBuffer;
        /// <summary>Indirection buffer that transforms the an index in the global indirect buffer to an index for the volumetric material data buffer</summary>
        internal GraphicsBuffer globalIndirectionBuffer;

        internal GraphicsBuffer volumetricMaterialDataBuffer;
        internal GraphicsBuffer volumetricMaterialIndexBuffer;

        LocalVolumetricFogManager()
        {
            m_Volumes = new List<LocalVolumetricFog>();
        }


        public void RegisterVolume(LocalVolumetricFog volume)
        {
            m_Volumes.Add(volume);
            ResizeBuffersIfNeeded();
        }

        public void DeRegisterVolume(LocalVolumetricFog volume)
        {
            if (m_Volumes.Contains(volume))
            {
                m_Volumes.Remove(volume);
                ResizeBuffersIfNeeded();
            }
        }

        int GetNeededBufferCount()
            => Mathf.Max(k_IndirectBufferChunkSize, Mathf.CeilToInt(m_Volumes.Count / (float)k_IndirectBufferChunkSize) * k_IndirectBufferChunkSize);

        internal unsafe void InitializeGraphicsBuffers(int maxLocalVolumetricFogs)
        {
            int count = GetNeededBufferCount();
            if (count > 0)
            {
                globalIndirectBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, count, sizeof(GraphicsBuffer.IndirectDrawArgs));
                globalIndirectionBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, count, sizeof(uint));
            }

            int maxVolumeCountTimesViewCount = maxLocalVolumetricFogs * 2;
            volumetricMaterialDataBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, maxVolumeCountTimesViewCount, Marshal.SizeOf(typeof(VolumetricMaterialRenderingData)));
            volumetricMaterialIndexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Index, 3 * 4, sizeof(uint));
            // Index buffer for triangle fan with max 6 vertices
            volumetricMaterialIndexBuffer.SetData(new List<uint>{
                0, 1, 2,
                0, 2, 3,
                0, 3, 4,
                0, 4, 5
            });
            
            // Because SRP rendering happens too late, we need to force the draw calls to update
            RegisterLocalVolumetricFogLateUpdate.PrepareFogDrawCalls();
        }

        /// <summary>
        /// Resize buffers only if the total number of volumes is above the buffer size or if the number
        /// of volumes is below the buffer size minus the chunk size
        /// </summary>
        unsafe void ResizeBuffersIfNeeded()
        {
            if (globalIndirectBuffer == null || !globalIndirectBuffer.IsValid())
                return;

            int count = GetNeededBufferCount();

            if (count > globalIndirectBuffer.count)
                Resize(count);
            if (count < globalIndirectBuffer.count - k_IndirectBufferChunkSize)
                Resize(count + k_IndirectBufferChunkSize);

            void Resize(int bufferCount)
            {
                globalIndirectBuffer.Release();
                globalIndirectionBuffer.Release();
                globalIndirectBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, bufferCount, sizeof(GraphicsBuffer.IndirectDrawArgs));
                globalIndirectionBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, bufferCount, sizeof(uint));
            }
        }

        internal void CleanupGraphicsBuffers()
        {
            CoreUtils.SafeRelease(globalIndirectBuffer);
            CoreUtils.SafeRelease(globalIndirectionBuffer);
            CoreUtils.SafeRelease(volumetricMaterialIndexBuffer);
            CoreUtils.SafeRelease(volumetricMaterialDataBuffer);
        }

        public bool ContainsVolume(LocalVolumetricFog volume) => m_Volumes.Contains(volume);

        public List<LocalVolumetricFog> PrepareLocalVolumetricFogData(CommandBuffer cmd, HDCamera currentCam)
        {
            //Update volumes
            float time = currentCam.time;
            foreach (LocalVolumetricFog volume in m_Volumes)
                volume.PrepareParameters(time);

            return m_Volumes;
        }

        public bool IsInitialized() => globalIndirectBuffer != null && globalIndirectBuffer.IsValid();

        public static class RegisterLocalVolumetricFogLateUpdate
        {
#if UNITY_EDITOR
            [UnityEditor.InitializeOnLoadMethod]
#else
            [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#endif
            internal static void Init()
            {
                var currentLoopSystem = LowLevel.PlayerLoop.GetCurrentPlayerLoop();

                bool found = AppendToPlayerLoopList(typeof(RegisterLocalVolumetricFogLateUpdate), PrepareFogDrawCalls, ref currentLoopSystem, typeof(Update.ScriptRunBehaviourUpdate));
                LowLevel.PlayerLoop.SetPlayerLoop(currentLoopSystem);
            }

            internal static void PrepareFogDrawCalls()
            {
                if (!LocalVolumetricFogManager.manager?.IsInitialized() ?? true)
                    return;

                var volumes = LocalVolumetricFogManager.m_Manager.m_Volumes;
                for (int i = 0; i < volumes.Count; i++)
                    volumes[i].PrepareDrawCall(i);
            }

            internal static bool AppendToPlayerLoopList(Type updateType, PlayerLoopSystem.UpdateFunction updateFunction, ref PlayerLoopSystem playerLoop, Type playerLoopSystemType)
            {
                if (updateType == null || updateFunction == null || playerLoopSystemType == null)
                    return false;

                if (playerLoop.type == playerLoopSystemType)
                {
                    var oldListLength = playerLoop.subSystemList != null ? playerLoop.subSystemList.Length : 0;
                    var newSubsystemList = new PlayerLoopSystem[oldListLength + 1];
                    for (var i = 0; i < oldListLength; ++i)
                        newSubsystemList[i] = playerLoop.subSystemList[i];
                    newSubsystemList[oldListLength] = new PlayerLoopSystem
                    {
                        type = updateType,
                        updateDelegate = updateFunction
                    };
                    playerLoop.subSystemList = newSubsystemList;
                    return true;
                }

                if (playerLoop.subSystemList != null)
                {
                    for (var i = 0; i < playerLoop.subSystemList.Length; ++i)
                    {
                        if (AppendToPlayerLoopList(updateType, updateFunction, ref playerLoop.subSystemList[i], playerLoopSystemType))
                            return true;
                    }
                }
                return false;
            }
        }
    }
}
