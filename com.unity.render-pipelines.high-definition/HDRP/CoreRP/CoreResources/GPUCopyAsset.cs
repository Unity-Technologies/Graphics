using System;
using System.Collections.Generic;
using System.Text;

namespace UnityEngine.Experimental.Rendering
{
    /// <summary>
    /// Declares what should be generated in utility code.
    /// It will generate a compute shader and a C# class to use the compute shader with a ComputeBuffer
    ///
    /// Exemple:
    ///  - I add a CopyOperation { sourceChannel = 4, targetChannel = 2, subscript = "zx" }
    ///  => a Kernel will be generated to copy from a TextureRGBA the AR channels into a TextureRG
    ///  => A method will be added to call the kernel in the C# class GPUCopy (SampleCopy_xyzw2zx)
    ///
    /// C# Exemple:
    ///  // Initialize the gpucopy
    ///  var gpuCopy = new CPUCopy(generatedComputeShaderAsset);
    ///
    ///  CommandBuffer cmb = ...
    ///   gpuCopy.SampleCopyChannel_xyzw2x(cmb, _SourceTexture, _TargetTexture, new Vector2(targetWidth, targetHeight));
    ///
    /// Initialization:
    ///  - You must set the generated ComputeShader as argument of the constructor of the generated GPUCopy C# class
    /// </summary>
    public class GPUCopyAsset : ScriptableObject
    {
        static string[] k_ChannelIDS = { "x", "xy", "xyz", "xyzw" };
        const int k_KernelSize = 8;

        [Serializable]
        public struct CopyOperation
        {
            public string subscript;
            public int sourceChannel;
            public int targetChannel;
        }

        [SerializeField]
        CopyOperation[] m_CopyOperation = new CopyOperation[0];

        public void Generate(out string computeShader, out string csharp)
        {
            var operations = m_CopyOperation;

            var sources = new HashSet<int>();
            var targets = new HashSet<int>();

            var cc = new StringBuilder(); // Compute Shader out
            var ccp = new StringBuilder(); // Compute properties
            var cck = new StringBuilder(); // Compute kernel
            var cs = new StringBuilder(); // CSharp out
            var csm = new StringBuilder(); // CSharp methods
            var csc = new StringBuilder(); // CSharp constructor
            var csp = new StringBuilder(); // CSharp properties

            for (var i = 0; i < operations.Length; i++)
            {
                var o = operations[i];
                sources.Add(o.sourceChannel);
                targets.Add(o.targetChannel);
            }

            ccp.AppendLine();
            ccp.AppendLine("CBUFFER_START (UnityCBuffer)");
            ccp.AppendLine("  uint2 _RectOffset;");
            ccp.AppendLine("CBUFFER_END");
            ccp.AppendLine();
            csm.AppendLine("        static readonly int _RectOffset = Shader.PropertyToID(\"_RectOffset\");");
            foreach (var target in targets)
            {
                ccp.AppendLine(string.Format("RWTexture2D<float{0}> _Result{0};", target.ToString()));
                csm.AppendLine(string.Format("        static readonly int _Result{0} = Shader.PropertyToID(\"_Result{0}\");", target.ToString()));
            }
            foreach (var source in sources)
            {
                ccp.AppendLine(string.Format("Texture2D<float{0}> _Source{0};", source.ToString()));
                csm.AppendLine(string.Format("        static readonly int _Source{0} = Shader.PropertyToID(\"_Source{0}\");", source.ToString()));
            }

            csm.AppendLine(@"        void SampleCopyChannel(
            CommandBuffer cmd,
            RectInt rect,
            int _source,
            RenderTargetIdentifier source,
            int _target,
            RenderTargetIdentifier target,
            int kernel8,
            int kernel1)
        {
            RectInt main, topRow, rightCol, topRight;
            unsafe
            {
                RectInt* dispatch1Rects = stackalloc RectInt[3];
                int dispatch1RectCount = 0;
                RectInt dispatch8Rect = RectInt.zero;

                if (TileLayoutUtils.TryLayoutByTiles(
                    rect,
                    8,
                    out main,
                    out topRow,
                    out rightCol,
                    out topRight))
                {
                    if (topRow.width > 0 && topRow.height > 0)
                    {
                        dispatch1Rects[dispatch1RectCount] = topRow;
                        ++dispatch1RectCount;
                    }
                    if (rightCol.width > 0 && rightCol.height > 0)
                    {
                        dispatch1Rects[dispatch1RectCount] = rightCol;
                        ++dispatch1RectCount;
                    }
                    if (topRight.width > 0 && topRight.height > 0)
                    {
                        dispatch1Rects[dispatch1RectCount] = topRight;
                        ++dispatch1RectCount;
                    }
                    dispatch8Rect = main;
                }
                else if (rect.width > 0 && rect.height > 0)
                {
                    dispatch1Rects[dispatch1RectCount] = rect;
                    ++dispatch1RectCount;
                }

                cmd.SetComputeTextureParam(m_Shader, kernel8, _source, source);
                cmd.SetComputeTextureParam(m_Shader, kernel1, _source, source);
                cmd.SetComputeTextureParam(m_Shader, kernel8, _target, target);
                cmd.SetComputeTextureParam(m_Shader, kernel1, _target, target);

                if (dispatch8Rect.width > 0 && dispatch8Rect.height > 0)
                {
                    var r = dispatch8Rect;
                    cmd.SetComputeIntParams(m_Shader, _RectOffset, (int)r.x, (int)r.y);
                    cmd.DispatchCompute(m_Shader, kernel8, (int)Mathf.Max(r.width / 8, 1), (int)Mathf.Max(r.height / 8, 1), 1);
                }

                for (int i = 0, c = dispatch1RectCount; i < c; ++i)
                {
                    var r = dispatch1Rects[i];
                    cmd.SetComputeIntParams(m_Shader, _RectOffset, (int)r.x, (int)r.y);
                    cmd.DispatchCompute(m_Shader, kernel1, (int)Mathf.Max(r.width, 1), (int)Mathf.Max(r.height, 1), 1);
                }
            }
        }");

            csc.AppendLine("        public GPUCopy(ComputeShader shader)");
            csc.AppendLine("        {");
            csc.AppendLine("            m_Shader = shader;");
            for (var i = 0; i < operations.Length; i++)
            {
                var o = operations[i];

                // Compute kernel
                var kernelName8 = string.Format("KSampleCopy{0}_{1}_{2}_8", o.sourceChannel.ToString(), o.targetChannel.ToString(), o.subscript);
                var kernelName1 = string.Format("KSampleCopy{0}_{1}_{2}_1", o.sourceChannel.ToString(), o.targetChannel.ToString(), o.subscript);
                cck.AppendLine(string.Format("#pragma kernel {0}   KERNEL_NAME={0}  KERNEL_SIZE=8", kernelName8));
                cck.AppendLine(string.Format("#pragma kernel {0}   KERNEL_NAME={0}  KERNEL_SIZE=1", kernelName1));
                cck.AppendLine(@"[numthreads(KERNEL_SIZE, KERNEL_SIZE, 1)]");
                cck.AppendLine(@"void KERNEL_NAME(uint2 dispatchThreadId : SV_DispatchThreadID)");
                cck.AppendLine("{");
                cck.AppendLine(string.Format("    _Result{0}[_RectOffset + dispatchThreadId] = LOAD_TEXTURE2D(_Source{1}, _RectOffset + dispatchThreadId).{2};",
                        o.targetChannel.ToString(), o.sourceChannel.ToString(), o.subscript));
                cck.AppendLine("}");
                cck.AppendLine();

                // CSharp kernel index
                var channelName = k_ChannelIDS[o.sourceChannel - 1];
                var kernelIndexName8 = string.Format("k_SampleKernel_{0}2{1}_8", channelName, o.subscript);
                var kernelIndexName1 = string.Format("k_SampleKernel_{0}2{1}_1", channelName, o.subscript);
                csp.AppendLine(string.Format("        int {0};", kernelIndexName8));
                csp.AppendLine(string.Format("        int {0};", kernelIndexName1));

                // CSharp constructor
                csc.AppendLine(string.Format("            {0} = m_Shader.FindKernel(\"{1}\");", kernelIndexName8, kernelName8));
                csc.AppendLine(string.Format("            {0} = m_Shader.FindKernel(\"{1}\");", kernelIndexName1, kernelName1));

                // CSharp method
                csm.AppendLine(string.Format(@"        public void SampleCopyChannel_{0}2{1}(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier target, RectInt rect)", channelName, o.subscript));
                csm.AppendLine("          {");
                csm.AppendLine(string.Format("                 SampleCopyChannel(cmd, rect, _Source{0}, source, _Result{1}, target, {2}, {3});", o.sourceChannel.ToString(), o.targetChannel.ToString(), kernelIndexName8, kernelIndexName1));
                csm.AppendLine("          }");
            }
            csc.AppendLine("        }");

            // Compute Shader
            cc.AppendLine(@"// Autogenerated file. Do not edit by hand");
            cc.AppendLine();
            cc.AppendLine("#pragma only_renderers d3d11 ps4 xboxone vulkan metal");
            cc.AppendLine(@"#include ""../ShaderLibrary/Common.hlsl""");
            cc.AppendLine(ccp.ToString()); // Properties
            cc.AppendLine(cck.ToString()); // Kernels

            // CSharp
            cs.AppendLine(@"// Autogenerated file. Do not edit by hand");
            cs.AppendLine(@"using System;");
            cs.AppendLine(@"using UnityEngine.Rendering;");
            cs.AppendLine();
            cs.AppendLine(@"namespace UnityEngine.Experimental.Rendering");
            cs.AppendLine("{");
            cs.AppendLine("    public class GPUCopy");
            cs.AppendLine("    {");
            cs.AppendLine("        ComputeShader m_Shader;");
            cs.AppendLine(csp.ToString()); // Properties
            cs.AppendLine(csc.ToString()); // Constructor
            cs.AppendLine(csm.ToString()); // methods
            cs.AppendLine("    }");
            cs.AppendLine("}");

            computeShader = cc.ToString();
            csharp = cs.ToString();
        }

        void OnValidate()
        {
            for (var i = 0; i < m_CopyOperation.Length; i++)
            {
                var o = m_CopyOperation[i];
                o.sourceChannel = Mathf.Clamp(o.sourceChannel, 1, k_ChannelIDS.Length);
                o.targetChannel = Mathf.Clamp(o.targetChannel, 1, k_ChannelIDS.Length);
                m_CopyOperation[i] = o;
            }
        }
    }
}
