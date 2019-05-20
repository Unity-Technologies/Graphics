using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering
{
    public class EncodeBC6H
    {
        public static EncodeBC6H DefaultInstance;

        static readonly int _SourceCube = Shader.PropertyToID("_SourceCube");
        static readonly int _SourceTex2D = Shader.PropertyToID("_SourceTex2D");
        static readonly int _Target = Shader.PropertyToID("_Target");
        static readonly int _MipIndex = Shader.PropertyToID("_MipIndex");
        static readonly int[] __Tmp_RT =
        {
            Shader.PropertyToID("__Tmp_RT0"),
            Shader.PropertyToID("__Tmp_RT1"),
            Shader.PropertyToID("__Tmp_RT2"),
            Shader.PropertyToID("__Tmp_RT3"),
            Shader.PropertyToID("__Tmp_RT4"),
            Shader.PropertyToID("__Tmp_RT5"),
            Shader.PropertyToID("__Tmp_RT6"),
            Shader.PropertyToID("__Tmp_RT7"),
            Shader.PropertyToID("__Tmp_RT8"),
            Shader.PropertyToID("__Tmp_RT9"),
            Shader.PropertyToID("__Tmp_RT10"),
            Shader.PropertyToID("__Tmp_RT11"),
            Shader.PropertyToID("__Tmp_RT12"),
            Shader.PropertyToID("__Tmp_RT13")
        };

        readonly ComputeShader m_Shader;
        readonly int m_KEncodeFastCubemapMip;
        readonly int m_KEncodeFastTex2DMip;

        public EncodeBC6H(ComputeShader shader)
        {
            Assert.IsNotNull(shader);

            m_Shader = shader;
            m_KEncodeFastCubemapMip = m_Shader.FindKernel("KEncodeFastCubemapMip");
            m_KEncodeFastTex2DMip = m_Shader.FindKernel("KEncodeFastTex2DMip");
        }

        // Only use mode11 of BC6H encoding
        /// <summary>
        /// Encode a Cubemap in BC6H.
        ///
        /// It will encode all faces and selected mips of the Cubemap.
        ///
        /// It uses only mode 11 of BC6H.
        /// </summary>
        /// <param name="cmb">Command buffer for execution</param>
        /// <param name="source">The source Cubemap</param>
        /// <param name="sourceSize">The size of the source Cubemap</param>
        /// <param name="target">The compressed texture.
        /// It must be a BC6H Cubemap or Cubemap array with the same size as the source Cubemap</param>
        /// <param name="fromMip">Starting mip to encode</param>
        /// <param name="toMip">Last mip to encode</param>
        /// <param name="targetArrayIndex">The index of the cubemap to store the compressed texture.
        ///
        /// Only relevant when target is a CubemapArray</param>
        public void EncodeFastCubemap(CommandBuffer cmb, RenderTargetIdentifier source, int sourceSize, RenderTargetIdentifier target, int fromMip, int toMip, int targetArrayIndex = 0)
        {
            var maxMip = Mathf.Max(0, (int)(Mathf.Log(sourceSize) / Mathf.Log(2)) - 2);
            var actualFromMip = (int)Mathf.Clamp(fromMip, 0, maxMip);
            var actualToMip = (int)Mathf.Min(maxMip, Mathf.Max(toMip, actualFromMip));

            // Convert TextureCube source to Texture2DArray
            var d = new RenderTextureDescriptor
            {
                autoGenerateMips = false,
                bindMS = false,
                colorFormat = RenderTextureFormat.ARGBInt,
                depthBufferBits = 0,
                dimension = TextureDimension.Tex2DArray,
                enableRandomWrite = true,
                msaaSamples = 1,
                volumeDepth = 6,
                sRGB = false,
                useMipMap = false,
            };

            cmb.SetComputeTextureParam(m_Shader, m_KEncodeFastCubemapMip, _SourceCube, source);

            for (var mip = actualFromMip; mip <= actualToMip; ++mip)
            {
                var size = (sourceSize >> mip) >> 2;
                d.width = size;
                d.height = size;
                cmb.GetTemporaryRT(__Tmp_RT[mip], d);
            }

            for (var mip = actualFromMip; mip <= actualToMip; ++mip)
            {
                var size = (sourceSize >> mip) >> 2;
                cmb.SetComputeTextureParam(m_Shader, m_KEncodeFastCubemapMip, _Target, __Tmp_RT[mip]);
                cmb.SetComputeIntParam(m_Shader, _MipIndex, mip);
                cmb.DispatchCompute(m_Shader, m_KEncodeFastCubemapMip, size, size, 6);
            }

            var startSlice = 6 * targetArrayIndex;
            for (var mip = actualFromMip; mip <= actualToMip; ++mip)
            {
                var rtMip = Mathf.Clamp(mip, actualFromMip, actualToMip);
                for (var faceId = 0; faceId < 6; ++faceId)
                    cmb.CopyTexture(__Tmp_RT[rtMip], faceId, 0, target, startSlice + faceId, mip);
            }

            for (var mip = actualFromMip; mip <= actualToMip; ++mip)
                cmb.ReleaseTemporaryRT(__Tmp_RT[mip]);
        }

        // Only use mode11 of BC6H encoding
        /// <summary>
        /// Encode a Texture2D in BC6H.
        ///
        /// It uses only mode 11 of BC6H.
        /// </summary>
        /// <param name="cmb">Command buffer for execution</param>
        /// <param name="source">The source Texture</param>
        /// <param name="sourceSize">The size of the source Texture3D</param>
        /// <param name="target">The compressed texture.
        /// It must be a BC6H Texture2D or Texture2D array with the same size as the source Texture2D</param>
        /// <param name="fromMip">Starting mip to encode</param>
        /// <param name="toMip">Last mip to encode</param>
        /// <param name="sliceIndex">The index of the slice to store the compressed texture.
        ///
        /// Only relevant when target is a CubemapArray</param>
        public void EncodeFastTex2D(
            CommandBuffer cmb,
            RenderTargetIdentifier source,
            Vector2Int sourceSize,
            RenderTargetIdentifier target,
            int fromMip,
            int toMip,
            int sliceIndex = 0
        )
        {
            var maxMip = Mathf.Max(0, (int)(Mathf.Log(Mathf.Max(sourceSize.x, sourceSize.y)) / Mathf.Log(2)) - 2);
            var actualFromMip = (int)Mathf.Clamp(fromMip, 0, maxMip);
            var actualToMip = (int)Mathf.Min(maxMip, Mathf.Max(toMip, actualFromMip));

            // Convert TextureCube source to Texture2DArray
            var d = new RenderTextureDescriptor
            {
                autoGenerateMips = false,
                bindMS = false,
                colorFormat = RenderTextureFormat.ARGBInt,
                depthBufferBits = 0,
                dimension = TextureDimension.Tex2DArray,
                enableRandomWrite = true,
                msaaSamples = 1,
                volumeDepth = 6,
                sRGB = false,
                useMipMap = false,
            };

            cmb.SetComputeTextureParam(m_Shader, m_KEncodeFastTex2DMip, _SourceTex2D, source);

            for (var mip = actualFromMip; mip <= actualToMip; ++mip)
            {
                d.width = (sourceSize.x >> mip) >> 2;
                d.height = (sourceSize.y >> mip) >> 2;
                cmb.GetTemporaryRT(__Tmp_RT[mip], d);
            }

            for (var mip = actualFromMip; mip <= actualToMip; ++mip)
            {
                var sizeX = (sourceSize.x >> mip) >> 2;
                var sizeY = (sourceSize.y >> mip) >> 2;
                cmb.SetComputeTextureParam(m_Shader, m_KEncodeFastTex2DMip, _Target, __Tmp_RT[mip]);
                cmb.SetComputeIntParam(m_Shader, _MipIndex, mip);
                cmb.DispatchCompute(m_Shader, m_KEncodeFastTex2DMip, sizeX, sizeY, 6);
            }

            for (var mip = actualFromMip; mip <= actualToMip; ++mip)
            {
                var rtMip = Mathf.Clamp(mip, actualFromMip, actualToMip);
                cmb.CopyTexture(__Tmp_RT[rtMip], 0, 0, target, sliceIndex, mip);
            }

            for (var mip = actualFromMip; mip <= actualToMip; ++mip)
                cmb.ReleaseTemporaryRT(__Tmp_RT[mip]);
        }
    }

    public static class BC6HExtensions
    {
        public static void BC6HEncodeFastCubemap(
            this CommandBuffer cmb,
            RenderTargetIdentifier source,
            int sourceSize,
            RenderTargetIdentifier target,
            int fromMip,
            int toMip,
            int targetArrayIndex = 0
        )
            => EncodeBC6H.DefaultInstance.EncodeFastCubemap(
                    cmb,
                    source,
                    sourceSize,
                    target,
                    fromMip,
                    toMip,
                    targetArrayIndex
                );

        public static void BC6HEncodeFastTex2D(
            this CommandBuffer cmb,
            RenderTargetIdentifier source,
            Vector2Int sourceSize,
            RenderTargetIdentifier target,
            int fromMip,
            int toMip,
            int targetArrayIndex = 0
        )
            => EncodeBC6H.DefaultInstance.EncodeFastTex2D(
                    cmb,
                    source,
                    sourceSize,
                    target,
                    fromMip,
                    toMip,
                    targetArrayIndex
                );
    }
}
