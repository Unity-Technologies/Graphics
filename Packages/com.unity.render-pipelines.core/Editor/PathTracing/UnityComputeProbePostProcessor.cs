using UnityEngine.PathTracing.Core;
using UnityEngine.LightTransport;
using UnityEngine.LightTransport.PostProcessing;
using UnityEngine.Rendering;

namespace UnityEngine.PathTracing.PostProcessing
{
    // UnityComputeProbePostProcessor was temporarily moved to the Editor assembly because of its unfortunate
    // dependency on RadeonRaysProbePostProcessor.DeringSphericalHarmonicsL2 which is editor-only.
    // We should move it back to Runtime: https://jira.unity3d.com/browse/GFXFEAT-1035.
    internal class UnityComputeProbePostProcessor : IProbePostProcessor
    {
        ProbePostProcessor _probePostProcessor;

        /// <summary>
        /// UnityComputeProbePostProcessor constructor.
        /// Expects to be given the "ProbePostProcessing.compute" shader.
        /// </summary>
        /// <param name="computeShader">The compute shader containing post processing kernels. Should be the "ProbePostProcessing.compute" shader.</param>
        public UnityComputeProbePostProcessor(ComputeShader computeShader)
        {
            _probePostProcessor = new ProbePostProcessor();
            _probePostProcessor.Prepare(computeShader);
        }

        public bool Initialize(IDeviceContext context)
        {
            return true;
        }

        public void Dispose()
        {
        }

        public bool AddSphericalHarmonicsL2(IDeviceContext context, BufferSlice<SphericalHarmonicsL2> a, BufferSlice<SphericalHarmonicsL2> b, BufferSlice<SphericalHarmonicsL2> sum, int probeCount)
        {
            var unityCtx = context as UnityComputeDeviceContext;
            Debug.Assert(unityCtx != null);

            _probePostProcessor.AddSphericalHarmonicsL2(
                unityCtx.GetCommandBuffer(),
                unityCtx.GetComputeBuffer(a.Id),
                unityCtx.GetComputeBuffer(b.Id),
                unityCtx.GetComputeBuffer(sum.Id),
                (uint)a.Offset,
                (uint)b.Offset,
                (uint)sum.Offset,
                (uint)probeCount);

            return true;
        }

        public bool ConvertToUnityFormat(IDeviceContext context, BufferSlice<SphericalHarmonicsL2> irradianceIn, BufferSlice<SphericalHarmonicsL2> irradianceOut, int probeCount)
        {
            var unityCtx = context as UnityComputeDeviceContext;
            Debug.Assert(unityCtx != null);

            _probePostProcessor.ConvertToUnityFormat(
                unityCtx.GetCommandBuffer(),
                unityCtx.GetComputeBuffer(irradianceIn.Id),
                unityCtx.GetComputeBuffer(irradianceOut.Id),
                (uint)irradianceIn.Offset,
                (uint)irradianceOut.Offset,
                (uint)probeCount);

            return true;
        }

        public bool ConvolveRadianceToIrradiance(IDeviceContext context, BufferSlice<SphericalHarmonicsL2> radianceIn, BufferSlice<SphericalHarmonicsL2> irradianceOut, int probeCount)
        {
            var unityCtx = context as UnityComputeDeviceContext;
            Debug.Assert(unityCtx != null);

            _probePostProcessor.ConvolveRadianceToIrradiance(
                unityCtx.GetCommandBuffer(),
                unityCtx.GetComputeBuffer(radianceIn.Id),
                unityCtx.GetComputeBuffer(irradianceOut.Id),
                (uint)radianceIn.Offset,
                (uint)irradianceOut.Offset,
                (uint)probeCount);

            return true;
        }

        public bool ScaleSphericalHarmonicsL2(IDeviceContext context, BufferSlice<SphericalHarmonicsL2> shIn, BufferSlice<SphericalHarmonicsL2> shOut, int probeCount, float scale)
        {
            var unityCtx = context as UnityComputeDeviceContext;
            Debug.Assert(unityCtx != null);

            _probePostProcessor.ScaleSphericalHarmonicsL2(
                unityCtx.GetCommandBuffer(),
                unityCtx.GetComputeBuffer(shIn.Id),
                unityCtx.GetComputeBuffer(shOut.Id),
                (uint)shIn.Offset,
                (uint)shOut.Offset,
                (uint)probeCount,
                scale);

            return true;
        }

        public bool WindowSphericalHarmonicsL2(IDeviceContext context, BufferSlice<SphericalHarmonicsL2> shIn, BufferSlice<SphericalHarmonicsL2> shOut, int probeCount)
        {
            var unityCtx = context as UnityComputeDeviceContext;
            Debug.Assert(unityCtx != null);

            _probePostProcessor.WindowSphericalHarmonicsL2(
                unityCtx.GetCommandBuffer(),
                unityCtx.GetComputeBuffer(shIn.Id),
                unityCtx.GetComputeBuffer(shOut.Id),
                (uint)shIn.Offset,
                (uint)shOut.Offset,
                (uint)probeCount);

            return true;
        }

        public bool DeringSphericalHarmonicsL2(IDeviceContext context, BufferSlice<SphericalHarmonicsL2> shIn, BufferSlice<SphericalHarmonicsL2> shOut, int probeCount)
        {
            using var radeonRaysProbePostProcessor = new RadeonRaysProbePostProcessor();
            return radeonRaysProbePostProcessor.DeringSphericalHarmonicsL2(context, shIn, shOut, probeCount);
        }
    }
}
