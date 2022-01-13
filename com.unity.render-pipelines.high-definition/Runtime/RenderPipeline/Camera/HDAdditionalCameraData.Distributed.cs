using System;
using System.Linq;
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
        /// Convert the exposure data to bytes.
        /// </summary>
        /// <returns>Converted bytes</returns>
        public byte[] ExposureAsBytes()
        {
            byte[] bytes = Array.Empty<byte>();
            bytes = bytes.Concat(BitConverter.GetBytes(m_ExposureData.x))
                .Concat(BitConverter.GetBytes(m_ExposureData.y))
                .Concat(BitConverter.GetBytes(m_ExposureData.z))
                .Concat(BitConverter.GetBytes(m_ExposureData.w))
                .ToArray();
            return bytes;
        }

        /// <summary>
        /// Convert the byte array to exposure data.
        /// </summary>
        /// <param name="bytes">Source byte array</param>
        public void ExposureFromBytes(byte[] bytes)
        {
            m_ExposureData.x = BitConverter.ToSingle(bytes, 0);
            m_ExposureData.y = BitConverter.ToSingle(bytes, 4);
            m_ExposureData.z = BitConverter.ToSingle(bytes, 8);
            m_ExposureData.w = BitConverter.ToSingle(bytes, 12);
        }
    }
}
