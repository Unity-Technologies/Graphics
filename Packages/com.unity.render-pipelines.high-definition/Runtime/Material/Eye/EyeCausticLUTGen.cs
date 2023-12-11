using Unity.Collections;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    internal class EyeCausticLUT
    {
        static class Uniforms
        {
            internal static int _OutputWidth = Shader.PropertyToID("_OutputWidth");
            internal static int _OutputHeight = Shader.PropertyToID("_OutputHeight");
            internal static int _OutputDepth = Shader.PropertyToID("_OutputDepth");
            internal static int _OutputSlice = Shader.PropertyToID("_OutputSlice");
            internal static int _UseCorneaSymmetryMirroring = Shader.PropertyToID("_UseCorneaSymmetryMirroring");
            internal static int _ExtraScleraMargin = Shader.PropertyToID("_ExtraScleraMargin");

            internal static int _LightWidth = Shader.PropertyToID("_LightWidth");
            internal static int _LightHeight = Shader.PropertyToID("_LightHeight");
            internal static int _LightDistance = Shader.PropertyToID("_LightDistance");

            internal static int _LightLuminousFlux = Shader.PropertyToID("_LightLuminousFlux");
            internal static int _LightGrazingAngleCos = Shader.PropertyToID("_LightGrazingAngleCos");

            internal static int _CorneaFlatteningFactor = Shader.PropertyToID("_CorneaFlatteningFactor");
            internal static int _CorneaApproxPrimitive = Shader.PropertyToID("_CorneaApproxPrimitive");
            internal static int _CorneaPowerFactor = Shader.PropertyToID("_CorneaPowerFactor");

            internal static int _RandomNumbers = Shader.PropertyToID("_RandomNumbers");

            internal static int _NumberOfSamplesAccumulated = Shader.PropertyToID("_NumberOfSamplesAccumulated");

            internal static int _GeneratedSamplesBuffer = Shader.PropertyToID("_GeneratedSamplesBuffer");

            internal static int _OutputLutTex = Shader.PropertyToID("_OutputLutTex");

            public static readonly int _PreIntegratedEyeCaustic = Shader.PropertyToID("_PreIntegratedEyeCaustic");
        }

        enum CorneaApproximationPrimitive
        {
            Sine = 0,
            Sphere = 1
        }

        private float lightSize = 10.0f;
        private float lightLuminousFlux = 12000;
        private float lightDipBelowHorizonDegrees = 30.0f;
        private float lightDistance = 10.0f;
        private float corneaFlatteningFactor = 0.7f;
        private float corneaPowerFactor = 1.5f;
        private CorneaApproximationPrimitive selectedCorneaApproxPrim = CorneaApproximationPrimitive.Sphere;

        private int lutWidth = 128;
        private int lutHeight = 32;
        private int lutDepth = 16;
        private bool useCorneaSymmetryMirroring = true;
        private float lutExtraScleraMargin = 0.15f;

        private RenderTexture generatedLUT;

        private ComputeBuffer generatedLutStagingBuffer;
        private ComputeBuffer randomSamplesBuffer;

        private const int LUT_GEN_KERNEL_SIZE = 64;
        private const int LUT_GEN_NUMBER_OF_DISPATCHES = 512 * 32;
        private const int LUT_GEN_SAMPLES_PER_SLICE = LUT_GEN_NUMBER_OF_DISPATCHES * LUT_GEN_KERNEL_SIZE;

        private static ComputeShader s_Shader;
        private static int[] s_Kernels = new int[3];

        private void CreateLUTGenResources()
        {
            if (s_Shader == null)
            {
                s_Shader = GraphicsSettings.GetRenderPipelineSettings<HDRenderPipelineRuntimeShaders>().eyeMaterialCS;
                s_Kernels[0] = s_Shader.FindKernel("SampleCaustic");
                s_Kernels[1] = s_Shader.FindKernel("CopyToLUT");
                s_Kernels[2] = s_Shader.FindKernel("ClearBuffer");
            }


            RenderTextureDescriptor volumeDesc = new RenderTextureDescriptor()
            {
                dimension = TextureDimension.Tex3D,
                width = lutWidth,
                height = lutHeight,
                volumeDepth = lutDepth,
                graphicsFormat = GraphicsFormat.R16_SFloat,
                enableRandomWrite = true,
                msaaSamples = 1,
            };

            if (generatedLUT != null)
            {
                generatedLUT.Release();
            }

            generatedLUT = new RenderTexture(volumeDesc)
            {
                wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave, name = "Caustic LUT"
            };
            generatedLUT.Create();

            generatedLutStagingBuffer = new ComputeBuffer(lutWidth * lutHeight, 4, ComputeBufferType.Raw);
            generatedLutStagingBuffer.name = "Caustic LUT Staging";
            randomSamplesBuffer = new ComputeBuffer(LUT_GEN_SAMPLES_PER_SLICE, sizeof(float) * 4, ComputeBufferType.Default);
        }

        private void FreeLUTGenResources()
        {
            randomSamplesBuffer.Release();
            generatedLutStagingBuffer.Release();
        }

        private void GenerateLUT()
        {
            for (int i = 0; i != lutDepth; ++i)
            {
                ClearStaging();
                GenerateLUTForSlice(i);
                CopyFromStagingToLUT(lutDepth - i - 1);
            }
        }

        private void GenerateLUTForSlice(int currentDepthSlice)
        {
            int sampleCount = LUT_GEN_SAMPLES_PER_SLICE;

            NativeArray<Vector4> samples = new NativeArray<Vector4>(sampleCount, Allocator.Temp,
                NativeArrayOptions.UninitializedMemory);


            for (int i = 0; i < sampleCount; ++i)
            {
                float a = HaltonSequence.Get(i, 2);
                float b = HaltonSequence.Get(i, 3);
                float c = HaltonSequence.Get(i, 5);
                float d = HaltonSequence.Get(i, 7);

                Vector4 s = new Vector4(a, b, c, d);
                samples[i] = s;
            }

            randomSamplesBuffer.SetData(samples);
            samples.Dispose();

            CommandBuffer cmd = CommandBufferPool.Get();
            int kernel = s_Kernels[0];

            cmd.SetComputeIntParam(s_Shader, Uniforms._OutputWidth, lutWidth);
            cmd.SetComputeIntParam(s_Shader, Uniforms._OutputHeight, lutHeight);
            cmd.SetComputeIntParam(s_Shader, Uniforms._OutputSlice, currentDepthSlice);
            cmd.SetComputeFloatParam(s_Shader, Uniforms._ExtraScleraMargin, lutExtraScleraMargin);
            cmd.SetComputeFloatParam(s_Shader, Uniforms._UseCorneaSymmetryMirroring, useCorneaSymmetryMirroring ? 1 : 0);

            cmd.SetComputeFloatParam(s_Shader, Uniforms._LightWidth, lightSize);
            cmd.SetComputeFloatParam(s_Shader, Uniforms._LightHeight, lightSize);
            cmd.SetComputeFloatParam(s_Shader, Uniforms._LightDistance, lightDistance);
            float cosGrazingAngle = Mathf.Cos(Mathf.PI * 0.5f + Mathf.Deg2Rad * lightDipBelowHorizonDegrees);
            cmd.SetComputeFloatParam(s_Shader, Uniforms._LightGrazingAngleCos, cosGrazingAngle );
            cmd.SetComputeFloatParam(s_Shader, Uniforms._LightLuminousFlux, lightLuminousFlux );

            cmd.SetComputeIntParam(s_Shader, Uniforms._CorneaApproxPrimitive, (int)selectedCorneaApproxPrim);
            cmd.SetComputeFloatParam(s_Shader, Uniforms._CorneaFlatteningFactor, corneaFlatteningFactor);
            cmd.SetComputeFloatParam(s_Shader, Uniforms._CorneaPowerFactor, corneaPowerFactor);

            cmd.SetComputeBufferParam(s_Shader, kernel, Uniforms._RandomNumbers, randomSamplesBuffer);

            cmd.SetComputeBufferParam(s_Shader, kernel, Uniforms._GeneratedSamplesBuffer, generatedLutStagingBuffer);

            cmd.DispatchCompute(s_Shader, kernel, LUT_GEN_NUMBER_OF_DISPATCHES, 1, 1);
            Graphics.ExecuteCommandBuffer(cmd);

            CommandBufferPool.Release(cmd);
        }

        void CopyFromStagingToLUT(int currentDepthSlice)
        {

            int sampleCount = LUT_GEN_SAMPLES_PER_SLICE;
            CommandBuffer cmd = CommandBufferPool.Get();
            int kernel = s_Kernels[1];

            cmd.SetComputeIntParam(s_Shader, Uniforms._OutputWidth, lutWidth);
            cmd.SetComputeIntParam(s_Shader, Uniforms._OutputHeight, lutHeight);
            cmd.SetComputeIntParam(s_Shader, Uniforms._OutputDepth, lutDepth);
            cmd.SetComputeIntParam(s_Shader, Uniforms._OutputSlice, currentDepthSlice);
            cmd.SetComputeIntParam(s_Shader, Uniforms._NumberOfSamplesAccumulated, sampleCount);


            cmd.SetComputeBufferParam(s_Shader, kernel, Uniforms._GeneratedSamplesBuffer, generatedLutStagingBuffer);
            cmd.SetComputeTextureParam(s_Shader, kernel, Uniforms._OutputLutTex, generatedLUT);

            cmd.DispatchCompute(s_Shader, kernel, (lutWidth + 7) / 8,
                (lutHeight + 7) / 8, 1);

            Graphics.ExecuteCommandBuffer(cmd);

            CommandBufferPool.Release(cmd);
        }

        void ClearStaging()
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            int kernel = s_Kernels[2];

            int entries = lutWidth * lutHeight;

            cmd.SetComputeIntParam(s_Shader, Uniforms._OutputWidth, lutWidth);
            cmd.SetComputeIntParam(s_Shader, Uniforms._OutputHeight, lutHeight);

            cmd.SetComputeBufferParam(s_Shader, kernel, Uniforms._GeneratedSamplesBuffer, generatedLutStagingBuffer);

            cmd.DispatchCompute(s_Shader, kernel, (entries + 63) / 64, 1, 1);

            Graphics.ExecuteCommandBuffer(cmd);

            CommandBufferPool.Release(cmd);
        }

        internal void Create()
        {
            CreateLUTGenResources();
            GenerateLUT();
            FreeLUTGenResources();
        }

        internal void Cleanup()
        {
            if (generatedLUT != null)
            {
                generatedLUT.Release();
                generatedLUT = null;
            }
        }

        internal void Bind(CommandBuffer cmd)
        {
            cmd.SetGlobalTexture(Uniforms._PreIntegratedEyeCaustic, generatedLUT);
        }
    }
}
