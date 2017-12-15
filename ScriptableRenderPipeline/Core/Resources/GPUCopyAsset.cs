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
            foreach (var target in targets)
            {
                ccp.AppendLine(string.Format("RWTexture2D<float{0}> _Result{0};", target.ToString()));
                csm.AppendLine(string.Format("        static readonly int _Result{0} = Shader.PropertyToID(\"_Result{0}\");", target.ToString()));
            }
            ccp.AppendLine();
            foreach (var source in sources)
            {
                ccp.AppendLine(string.Format("Texture2D<float{0}> _Source{0};", source.ToString()));
                csm.AppendLine(string.Format("        static readonly int _Source{0} = Shader.PropertyToID(\"_Source{0}\");", source.ToString()));
            }
            ccp.AppendLine();

            csc.AppendLine("        public GPUCopy(ComputeShader shader)");
            csc.AppendLine("        {");
            csc.AppendLine("            m_Shader = shader;");
            csm.AppendLine("        static readonly int _Size = Shader.PropertyToID(\"_Size\");");
            for (var i = 0; i < operations.Length; i++)
            {
                var o = operations[i];

                // Compute kernel
                var kernelName = string.Format("KSampleCopy{0}_{1}_{2}", o.sourceChannel.ToString(), o.targetChannel.ToString(), o.subscript);
                cck.AppendLine(string.Format("#pragma kernel {0}", kernelName));
                cck.AppendLine(string.Format(@"[numthreads({0}, {0}, 1)]",
                    k_KernelSize.ToString(), k_KernelSize.ToString()));
                cck.AppendLine(string.Format(@"void {0}(uint2 dispatchThreadId : SV_DispatchThreadID)", kernelName));
                cck.AppendLine("{");
                cck.AppendLine(string.Format("    _Result{0}[dispatchThreadId] = SAMPLE_TEXTURE2D_LOD(_Source{1}, sampler_LinearClamp, float2(dispatchThreadId) * _Size.zw, 0.0).{2};",
                    o.targetChannel.ToString(), o.sourceChannel.ToString(), o.subscript));
                cck.AppendLine("}");
                cck.AppendLine();

                // CSharp kernel index
                var channelName = k_ChannelIDS[o.sourceChannel - 1];
                var kernelIndexName = string.Format("k_SampleKernel_{0}2{1}", channelName, o.subscript);
                csp.AppendLine(string.Format("        int {0};", kernelIndexName));

                // CSharp constructor
                csc.AppendLine(string.Format("            {0} = m_Shader.FindKernel(\"{1}\");", kernelIndexName, kernelName));

                // CSharp method
                csm.AppendLine(string.Format(@"        public void SampleCopyChannel_{0}2{1}(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier target, Vector2 size)", channelName, o.subscript));
                csm.AppendLine("            {");
                csm.AppendLine(string.Format("                if (size.x < {0} || size.y < {0})", k_KernelSize.ToString()));
                csm.AppendLine("                    Debug.LogWarning(\"Trying to copy a channel from a texture smaller than 8x* or *x8. ComputeShader cannot perform it.\");");
                csm.AppendLine("                var s = new Vector4(size.x, size.y, 1f / size.x, 1f / size.y);");
                csm.AppendLine("                cmd.SetComputeVectorParam(m_Shader, _Size, s);");
                csm.AppendLine(string.Format("                cmd.SetComputeTextureParam(m_Shader, {0}, _Source{1}, source);", kernelIndexName, o.sourceChannel.ToString()));
                csm.AppendLine(string.Format("                cmd.SetComputeTextureParam(m_Shader, {0}, _Result{1}, target);", kernelIndexName, o.targetChannel.ToString()));
                csm.AppendLine(string.Format("                cmd.DispatchCompute(m_Shader, {0}, (int)(size.x) / {1}, (int)(size.y) / {1}, 1);", kernelIndexName, k_KernelSize.ToString()));
                csm.AppendLine("            }");
            }
            csc.AppendLine("        }");

            // Compute Shader
            cc.AppendLine(@"// Autogenerated file. Do not edit by hand");
            cc.AppendLine();
            cc.AppendLine(@"#include ""../ShaderLibrary/Common.hlsl""");
            cc.AppendLine();
            cc.AppendLine(@"SamplerState sampler_LinearClamp;");
            cc.AppendLine();
            cc.AppendLine(@"CBUFFER_START(cb)");
            cc.AppendLine(@"    float4 _Size;");
            cc.AppendLine(@"CBUFFER_END");
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
