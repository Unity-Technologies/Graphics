using System;
using UnityEngine;
using UnityEngine.PathTracing.Lightmapping;
using UnityEngine.Rendering;

namespace UnityEditor.PathTracing.Debugging
{
    internal static class AdaptiveSamplingDebugHelpers
    {
        internal static class ShaderIDs
        {
            public static readonly int TemporaryRT = Shader.PropertyToID("BakeLightmapDriverTemporaryRT");
            public static readonly int SecondTemporaryRT = Shader.PropertyToID("BakeLightmapDriverSecondTemporaryRT");
        }

        static internal void LogTotalSamplesTaken(CommandBuffer cmd, LightmapIntegrationHelpers.ComputeHelpers helpers, RenderTexture accumulatedOutput, uint maxSampleCount)
        {
            int width = accumulatedOutput.width;
            int height = accumulatedOutput.height;
            UInt64 totalSampleSum = GetSampleCountSum(cmd, helpers, accumulatedOutput);
            UInt64 maxSampleSum = (UInt64)width * (UInt64)height * (UInt64)maxSampleCount;
            Debug.Log($"total samples taken:\t{totalSampleSum}\tout of: \t{maxSampleSum}\tfraction:\t{(double)totalSampleSum / (double)maxSampleSum}");
        }

        static internal LightmapIntegrationHelpers.AdaptiveSample GetAdaptiveSample(CommandBuffer cmd, LightmapIntegrationHelpers.ComputeHelpers helpers, RenderTexture accumulatedOutput, RenderTexture adaptiveOutput, int sampleX, int sampleY)
        {
            Debug.Assert(accumulatedOutput.width == adaptiveOutput.width && accumulatedOutput.height == adaptiveOutput.height);
            var width = adaptiveOutput.width;
            var height = adaptiveOutput.height;

            Color accumulatedOutputValue = LightmapIntegrationHelpers.GetValue(cmd, helpers.ComputeHelperShader, LightmapIntegrationHelpers.ComputeHelpers.GetValueKernel, sampleX, sampleY, width, height, accumulatedOutput);
            float accumulatedLuminance = LightmapIntegrationHelpers.Luminance(accumulatedOutputValue);
            Color adaptiveOutputValue = LightmapIntegrationHelpers.GetValue(cmd, helpers.ComputeHelperShader, LightmapIntegrationHelpers.ComputeHelpers.GetValueKernel, sampleX, sampleY, width, height, adaptiveOutput);

            LightmapIntegrationHelpers.AdaptiveSample sample = new LightmapIntegrationHelpers.AdaptiveSample();
            sample.sampleCount = (uint)accumulatedOutputValue.a;
            sample.accumulatedLuminance = accumulatedLuminance;
            sample.mean = adaptiveOutputValue.r;
            sample.meanSqr = adaptiveOutputValue.g;
            sample.variance = adaptiveOutputValue.b;
            sample.standardError = adaptiveOutputValue.a;

            // missing
            sample.varianceFiltered = 0.0f;// LightmapIntegrationHelpers.GetValue(cmd, helpers.ComputeHelperShader, LightmapIntegrationHelpers.ComputeHelpers.GetValueKernel, sampleX, sampleY, width, height, lightmappingContext.AdaptiveOutput).b;
            sample.active = false;// LightmapIntegrationHelpers.GetValue(cmd, helpers.ComputeHelperShader, LightmapIntegrationHelpers.ComputeHelpers.GetValueKernel, sampleX, sampleY, width, height, secondTemporaryRT).r > 0.5f;

            return sample;
        }

        static internal void WriteAdaptiveDebugImages(string path, string filenamePostfix, CommandBuffer cmd, LightmapIntegrationHelpers.ComputeHelpers helpers, RenderTexture accumulatedOutput, RenderTexture adaptiveOutput,
            uint totalSampleCount, float varianceScale, float errorScale, float adaptiveThreshold)
        {
            WriteIrradiance(cmd, helpers, accumulatedOutput, $"{path}/irradiance_{filenamePostfix}.r2d");
            WriteSampleCount(cmd, helpers, accumulatedOutput, totalSampleCount, $"{path}/sampleCount_{filenamePostfix}.r2d");
            WriteVariance(cmd, helpers, adaptiveOutput, varianceScale, $"{path}/variance_{filenamePostfix}.r2d");
            // this may be double filtered in converged parts of the lightmap
            WriteFilteredVariance(cmd, helpers, adaptiveOutput, varianceScale, $"{path}/varianceFiltered_{filenamePostfix}.r2d");
            WriteStandardError(cmd, helpers, adaptiveOutput, errorScale, $"{path}/standardError_{filenamePostfix}.r2d");
            WriteActiveTexels(cmd, helpers, adaptiveOutput, adaptiveThreshold, $"{path}/active_{filenamePostfix}.r2d");
            SerializationHelpers.WriteRenderTexture(cmd, $"{path}/adaptive_{filenamePostfix}.r2d", adaptiveOutput);
        }

        static void WriteIrradiance(CommandBuffer cmd, LightmapIntegrationHelpers.ComputeHelpers helpers, RenderTexture accumulatedOutput, string filename)
        {
            int width = accumulatedOutput.width;
            int height = accumulatedOutput.height;

            int tempRTID = ShaderIDs.TemporaryRT;
            cmd.GetTemporaryRT(tempRTID, width, height, 0, FilterMode.Point, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear, 1, true);

            cmd.CopyTexture(accumulatedOutput, tempRTID);
            LightmapIntegrationHelpers.NormalizeByAlpha(cmd, helpers.ComputeHelperShader, LightmapIntegrationHelpers.ComputeHelpers.NormalizeByAlphaKernel, width, height, tempRTID);
            LightmapIntegrationHelpers.SetChannelRenderTexture(cmd, helpers.ComputeHelperShader, LightmapIntegrationHelpers.ComputeHelpers.SetChannelKernel, tempRTID, width, height, 3, 1.0f);
            SerializationHelpers.WriteRenderTexture(cmd, tempRTID, TextureFormat.RGBAFloat, width, height, filename);

            cmd.ReleaseTemporaryRT(tempRTID);
        }

        static void WriteSampleCount(CommandBuffer cmd, LightmapIntegrationHelpers.ComputeHelpers helpers, RenderTexture accumulatedOutput, uint maxSampleCount, string filename)
        {
            int width = accumulatedOutput.width;
            int height = accumulatedOutput.height;

            int tempRTID = ShaderIDs.TemporaryRT;
            cmd.GetTemporaryRT(tempRTID, width, height, 0, FilterMode.Point, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear, 1, true);

            cmd.CopyTexture(accumulatedOutput, tempRTID);
            LightmapIntegrationHelpers.BroadcastChannelRenderTexture(cmd, helpers.ComputeHelperShader, LightmapIntegrationHelpers.ComputeHelpers.BroadcastChannelKernel, tempRTID, width, height, 3);
            LightmapIntegrationHelpers.SetChannelRenderTexture(cmd, helpers.ComputeHelperShader, LightmapIntegrationHelpers.ComputeHelpers.SetChannelKernel, tempRTID, width, height, 3, 1.0f);
            float samplesScale = 1.0f / maxSampleCount;
            LightmapIntegrationHelpers.MultiplyRenderTexture(cmd, helpers.ComputeHelperShader, LightmapIntegrationHelpers.ComputeHelpers.MultiplyKernel, tempRTID, width, height, new Vector4(samplesScale, samplesScale, samplesScale, 1.0f));
            SerializationHelpers.WriteRenderTexture(cmd, tempRTID, TextureFormat.RGBAFloat, width, height, filename);

            cmd.ReleaseTemporaryRT(tempRTID);
        }

        static void WriteVariance(CommandBuffer cmd, LightmapIntegrationHelpers.ComputeHelpers helpers, RenderTexture adaptiveOutput, float scale, string filename)
        {
            int width = adaptiveOutput.width;
            int height = adaptiveOutput.height;

            int tempRTID = ShaderIDs.TemporaryRT;
            cmd.GetTemporaryRT(tempRTID, width, height, 0, FilterMode.Point, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear, 1, true);

            cmd.CopyTexture(adaptiveOutput, tempRTID);
            LightmapIntegrationHelpers.BroadcastChannelRenderTexture(cmd, helpers.ComputeHelperShader, LightmapIntegrationHelpers.ComputeHelpers.BroadcastChannelKernel, tempRTID, width, height, 2);
            LightmapIntegrationHelpers.SetChannelRenderTexture(cmd, helpers.ComputeHelperShader, LightmapIntegrationHelpers.ComputeHelpers.SetChannelKernel, tempRTID, width, height, 3, 1.0f);
            LightmapIntegrationHelpers.MultiplyRenderTexture(cmd, helpers.ComputeHelperShader, LightmapIntegrationHelpers.ComputeHelpers.MultiplyKernel, tempRTID, width, height, new Vector4(scale, scale, scale, 1.0f));
            SerializationHelpers.WriteRenderTexture(cmd, tempRTID, TextureFormat.RGBAFloat, width, height, filename);

            cmd.ReleaseTemporaryRT(tempRTID);
        }

        static void WriteFilteredVariance(CommandBuffer cmd, LightmapIntegrationHelpers.ComputeHelpers helpers, RenderTexture adaptiveOutput, float scale, string filename)
        {
            int width = adaptiveOutput.width;
            int height = adaptiveOutput.height;

            int tempRTID = ShaderIDs.TemporaryRT;
            cmd.GetTemporaryRT(tempRTID, width, height, 0, FilterMode.Point, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear, 1, true);

            cmd.CopyTexture(adaptiveOutput, tempRTID);
            LightmapIntegrationHelpers.ReferenceBoxFilterRenderTexture(cmd, helpers.ComputeHelperShader, LightmapIntegrationHelpers.ComputeHelpers.ReferenceBoxFilterBlueChannelKernel, tempRTID, width, height, 2);
            LightmapIntegrationHelpers.BroadcastChannelRenderTexture(cmd, helpers.ComputeHelperShader, LightmapIntegrationHelpers.ComputeHelpers.BroadcastChannelKernel, tempRTID, width, height, 2);
            LightmapIntegrationHelpers.SetChannelRenderTexture(cmd, helpers.ComputeHelperShader, LightmapIntegrationHelpers.ComputeHelpers.SetChannelKernel, tempRTID, width, height, 3, 1.0f);
            LightmapIntegrationHelpers.MultiplyRenderTexture(cmd, helpers.ComputeHelperShader, LightmapIntegrationHelpers.ComputeHelpers.MultiplyKernel, tempRTID, width, height, new Vector4(scale, scale, scale, 1.0f));
            SerializationHelpers.WriteRenderTexture(cmd, tempRTID, TextureFormat.RGBAFloat, width, height, filename);

            cmd.ReleaseTemporaryRT(tempRTID);
        }

        static void WriteStandardError(CommandBuffer cmd, LightmapIntegrationHelpers.ComputeHelpers helpers, RenderTexture adaptiveOutput, float scale, string filename)
        {
            int width = adaptiveOutput.width;
            int height = adaptiveOutput.height;

            int tempRTID = ShaderIDs.TemporaryRT;
            cmd.GetTemporaryRT(tempRTID, width, height, 0, FilterMode.Point, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear, 1, true);

            cmd.CopyTexture(adaptiveOutput, tempRTID);
            LightmapIntegrationHelpers.BroadcastChannelRenderTexture(cmd, helpers.ComputeHelperShader, LightmapIntegrationHelpers.ComputeHelpers.BroadcastChannelKernel, tempRTID, width, height, 3);
            LightmapIntegrationHelpers.SetChannelRenderTexture(cmd, helpers.ComputeHelperShader, LightmapIntegrationHelpers.ComputeHelpers.SetChannelKernel, tempRTID, width, height, 3, 1.0f);
            LightmapIntegrationHelpers.MultiplyRenderTexture(cmd, helpers.ComputeHelperShader, LightmapIntegrationHelpers.ComputeHelpers.MultiplyKernel, tempRTID, width, height, new Vector4(scale, scale, scale, 1.0f));
            SerializationHelpers.WriteRenderTexture(cmd, tempRTID, TextureFormat.RGBAFloat, width, height, filename);

            cmd.ReleaseTemporaryRT(tempRTID);
        }

        static int GetActiveTexelImage(CommandBuffer cmd, LightmapIntegrationHelpers.ComputeHelpers helpers, RenderTexture adaptiveOutput, float adaptiveThreshold)
        {
            int width = adaptiveOutput.width;
            int height = adaptiveOutput.height;

            int tempRTID = ShaderIDs.TemporaryRT;
            int secondTempRTID = ShaderIDs.SecondTemporaryRT;
            cmd.GetTemporaryRT(secondTempRTID, width, height, 0, FilterMode.Point, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear, 1, true);
            cmd.GetTemporaryRT(tempRTID, width, height, 0, FilterMode.Point, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear, 1, true);

            cmd.CopyTexture(adaptiveOutput, tempRTID);
            LightmapIntegrationHelpers.BroadcastChannelRenderTexture(cmd, helpers.ComputeHelperShader, LightmapIntegrationHelpers.ComputeHelpers.BroadcastChannelKernel, tempRTID, width, height, 3);
            LightmapIntegrationHelpers.StandardErrorThresholdRenderTexture(cmd, helpers.ComputeHelperShader, LightmapIntegrationHelpers.ComputeHelpers.StandardErrorThresholdKernel, tempRTID, adaptiveOutput, adaptiveThreshold, secondTempRTID, width, height);
            cmd.ReleaseTemporaryRT(tempRTID);

            return secondTempRTID;
        }

        static void WriteActiveTexels(CommandBuffer cmd, LightmapIntegrationHelpers.ComputeHelpers helpers, RenderTexture adaptiveOutput, float adaptiveThreshold, string filename)
        {
            int width = adaptiveOutput.width;
            int height = adaptiveOutput.height;
            int tempRTID = GetActiveTexelImage(cmd, helpers, adaptiveOutput, adaptiveThreshold);
            SerializationHelpers.WriteRenderTexture(cmd, tempRTID, TextureFormat.RGBAFloat, width, height, filename);
            cmd.ReleaseTemporaryRT(tempRTID);
        }

        static UInt64 GetSampleCountSum(CommandBuffer cmd, LightmapIntegrationHelpers.ComputeHelpers helpers, RenderTexture accumulatedOutput)
        {
            int width = accumulatedOutput.width;
            int height = accumulatedOutput.height;

            var temporaryRT = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear)
            { name = "temporaryRT (GetSampleCountSum)", enableRandomWrite = true, hideFlags = HideFlags.HideAndDontSave };
            temporaryRT.Create();

            cmd.CopyTexture(accumulatedOutput, temporaryRT);
            LightmapIntegrationHelpers.BroadcastChannelRenderTexture(cmd, helpers.ComputeHelperShader, LightmapIntegrationHelpers.ComputeHelpers.BroadcastChannelKernel, temporaryRT, width, height, 3);
            Graphics.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            double totalSampleSum = LightmapIntegrationHelpers.GetSum(width, height, temporaryRT).x;

            temporaryRT.Release();
            CoreUtils.Destroy(temporaryRT);
            return (UInt64)totalSampleSum;
        }
    }
}
