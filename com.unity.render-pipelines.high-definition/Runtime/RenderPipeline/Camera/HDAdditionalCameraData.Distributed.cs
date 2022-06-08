using System;
using System.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDAdditionalCameraData
    {
        /// <summary>
        /// The exposure data that will be passed over network when in distributed mode.
        /// Does nothing when in non-distributed mode.
        /// Will be set and used automatically when in distributed mode.
        /// Do not modify manually.
        /// </summary>
        public Vector4 m_ExposureData = Vector4.zero;

        /// <summary>
        /// Convert the exposure data to bytes (native array).
        /// </summary>
        /// <returns>Converted bytes</returns>
        public NativeArray<byte> ExposureAsBytes()
        {
            unsafe
            {
                NativeArray<byte> bytes =
                    new NativeArray<byte>(sizeof(Vector4), Allocator.Temp, NativeArrayOptions.UninitializedMemory);

                var ptr = bytes.GetUnsafePtr();

                UnsafeUtility.CopyStructureToPtr(ref m_ExposureData, ptr);

                return bytes;
            }
        }

        /// <summary>
        /// Convert the byte array to exposure data.
        /// </summary>
        /// <param name="bytes">Source byte native array</param>
        public void ExposureFromBytes(NativeArray<byte> bytes)
        {
            unsafe
            {
                var ptr = bytes.GetUnsafePtr();
                UnsafeUtility.CopyPtrToStructure(ptr, out m_ExposureData);
            }
        }
    }
}
