using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor.Experimental.ShaderTools.Internal;

namespace UnityEditor.Experimental.ShaderTools.PSSLInternal
{
    using Utils = EditorShaderPerformanceReportUtil;

    partial class BuildReportPS4JobAsync : AsyncBuildReportJobBase
    {
        class ShaderJob<TOperation>
        {
#pragma warning disable 0649
            internal TOperation operation;
            internal ShaderBuildReport.CompileUnit log;
            internal int variantIndex;
            internal int multiCompileIndex;
#pragma warning restore 0649
        }

        class ProgressWrapper
        {
            float m_Min;
            float m_Max;

            BuildReportPS4JobAsync m_Job;

            public ProgressWrapper(BuildReportPS4JobAsync owner)
            {
                m_Job = owner;
            }

            // Set current range for normalized progress
            // Use this in front of any asynchronous operation
            // So within your method, you don't have to care about the progress range, but only [0..1]
            public void SetProgressRange(float min, float max)
            {
                m_Min = min;
                m_Max = max;
            }

            public void SetNormalizedProgress(float progress, string message)
            {
                m_Job.SetProgress(progress * (m_Max - m_Min) + m_Min, message);
            }

            public void SetNormalizedProgress(float progress, string format, params object[] args)
            {
                SetNormalizedProgress(progress, string.Format(format, args));
            }
        }

        static readonly Regex k_ShaderMicroCodeSize = new Regex(@"Shader microcode size: (\d+) bytes");
        static readonly Regex k_VGPRCount = new Regex(@"VGPR count: (\d+) \((\d+) used\)");
        static readonly Regex k_SGPRCount = new Regex(@"SGPR count: (\d+) \+ (\d+) [\s\w]+ \((\d+) used\)");
        static readonly Regex k_UserSGPRCount = new Regex(@"User SGPR count: (\d+)");
        static readonly Regex k_LDSSize = new Regex(@"LDS size: (\d+) bytes");
        static readonly Regex k_ThreadGroupWaves = new Regex(@"Threadgroup waves: (\d+)");
        static readonly Regex k_CUOccupancy = new Regex(@"CU occupancy: (\d+)/(\d+)");
        static readonly Regex k_SIMDOccupancy = new Regex(@"SIMD occupancy: (\d+)/(\d+)");

        IEnumerator m_Enumerator = null;
        bool m_Cancelled = false;

        ShaderBuildReport m_BuildReport = new ShaderBuildReport();

        PSSLShaderCompiler m_Compiler = new PSSLShaderCompiler();

        public override bool Tick()
        {
            if (m_Cancelled)
                return true;

            if (m_Enumerator == null)
            {
                if (shader != null)
                    m_Enumerator = DoTick_Shader();
                else if (compute != null)
                    m_Enumerator = DoTick_ComputeShader();
                else if (material != null)
                    m_Enumerator = DoTick_Material();
                else
                    throw new Exception("Invalid state");
            }

            var hasNext = m_Enumerator.MoveNext();

            var isCompleted = !hasNext || m_Cancelled;
            if (isCompleted)
                EditorUtility.ClearProgressBar();

            return isCompleted;
        }

        public override void Cancel()
        {
            m_Cancelled = true;
            SetProgress(1, "Cancelled");
        }

        public override ShaderBuildReport GetBuildReport()
        {
            return m_BuildReport;
        }
    }
}
