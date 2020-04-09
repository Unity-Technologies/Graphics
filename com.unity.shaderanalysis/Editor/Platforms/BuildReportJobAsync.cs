using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor.ShaderAnalysis.Internal;
using UnityEngine;

namespace UnityEditor.ShaderAnalysis
{
    /// <summary>Derives from this class to make new report jobs.</summary>
    public abstract  class BuildReportJobAsync : AsyncBuildReportJob
    {
        bool m_HasReport;
        IEnumerator m_Enumerator;
        ShaderBuildReport m_BuildReport;
        string m_Name = "Build shader report";

        public override string name => m_Name;

        /// <summary>Whether the job was cancelled.</summary>
        protected bool isCancelled { get; private set; }
        /// <summary>The shader to process.</summary>
        protected Shader shader { get; private set; }
        /// <summary>The compute shader to process.</summary>
        protected ComputeShader compute { get; }
        /// <summary>The material process.</summary>
        protected Material material { get; }

        /// <inheritdoc cref="AsyncBuildReportJob"/>
        public override bool hasReport => m_HasReport;
        /// <inheritdoc cref="AsyncBuildReportJob"/>
        public override ShaderBuildReport builtReport
        {
            get
            {
                if (!hasReport)
                    throw new InvalidOperationException("Report is not available.");
                return m_BuildReport;
            }
        }

        /// <summary>Create an instance to process the <paramref name="shader"/>.</summary>
        /// <param name="target">Build target to use.</param>
        /// <param name="shader">Shader to process.</param>
        /// <exception cref="ArgumentNullException">when <paramref name="shader"/> is null.</exception>
        protected BuildReportJobAsync(ShaderAnalysisReport<Shader> args)
            : base(args.common)
        {
            if (args.asset != null && args.asset.Equals(null))
                throw new ArgumentNullException(nameof(args.asset));
            shader = args.asset;
            m_Name = $"Build Shader Report ({shader})";
        }

        /// <summary>Create an instance to process the <paramref name="compute"/>.</summary>
        /// <param name="target">Build target to use.</param>
        /// <param name="compute">Compute shader to process.</param>
        /// <exception cref="ArgumentNullException">when <paramref name="compute"/> is null.</exception>
        protected BuildReportJobAsync(ShaderAnalysisReport<ComputeShader> args)
            : base(args.common)
        {
            if (args.asset != null && args.asset.Equals(null))
                throw new ArgumentNullException(nameof(args.asset));
            compute = args.asset;
            m_Name = $"Build Compute Shader Report ({compute})";
        }

        /// <summary>Create an instance to process the <paramref name="material"/>.</summary>
        /// <param name="target">Build target to use.</param>
        /// <param name="material">Material to process.</param>
        /// <exception cref="ArgumentNullException">when <paramref name="material"/> is null.</exception>
        protected BuildReportJobAsync(ShaderAnalysisReport<Material> args)
            : base(args.common)
        {
            if (args.asset != null && args.asset.Equals(null))
                throw new ArgumentNullException(nameof(args.asset));
            material = args.asset;
            m_Name = $"Build Material Report ({material})";
        }

        /// <inheritdoc cref="AsyncBuildReportJob"/>
        protected override bool Internal_Tick()
        {
            if (isCancelled)
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

            var isCompleted = !hasNext || isCancelled;
            if (isCompleted)
                EditorUtility.ClearProgressBar();

            return isCompleted;
        }

        /// <inheritdoc cref="AsyncBuildReportJob"/>
        protected override void Internal_Cancel()
        {
            isCancelled = true;
            SetProgress(1, "Cancelled");
        }

        /// <summary>Set the report.</summary>
        /// <param name="report">The report to set.</param>
        /// <exception cref="ArgumentNullException">for <paramref name="report"/></exception>
        protected void SetReport(ShaderBuildReport report)
        {
            m_BuildReport = report ?? throw new ArgumentNullException(nameof(report));
            m_HasReport = true;
        }

        /// <summary>
        /// Implements this to process the <see cref="shader"/> property.
        ///
        /// This needs to call DoTick_Shader_Internal with the proper buildTarget
        /// </summary>
        protected abstract IEnumerator DoTick_Shader();

        /// <summary>
        /// Implements this to process the <see cref="material"/> property.
        ///
        /// This needs to call DoTick_Material_Internal with the proper buildTarget
        /// </summary>
        protected abstract IEnumerator DoTick_Material();

        /// <summary>Implements this to process the <see cref="compute"/> property.</summary>
        protected abstract IEnumerator DoTick_ComputeShader();

        protected abstract IEnumerator DoTick_Shader_Internal(string[] shaderKeywords, DirectoryInfo temporaryDirectory);

        protected IEnumerator DoTick_Material_Internal(BuildTarget buildTarget)
        {
            shader = material.shader;

            for (int i = 0, c = material.passCount; i < c; ++i)
            {
                // Exclude passes disabled by the material
                var passName = material.GetPassName(i);
                var isEnabled = material.GetShaderPassEnabled(passName);
                if (!isEnabled)
                    filter.excludedPassNames.Add(passName);
            }

            var temporaryDirectory = ShaderAnalysisUtils.GetTemporaryDirectory(material, buildTarget);

            var e = DoTick_Shader_Internal(material.shaderKeywords, temporaryDirectory);

            while (e.MoveNext()) yield return null;
            if (isCancelled) yield break;

            SetProgress(1, "Completed");
        }

        /// <summary>Helper struct to process stages.</summary>
        protected struct StageTick
        {
            /// <summary>The range of progress this operation contributes to. Must be in [0-1].</summary>
            public float stageRange;
            /// <summary>The operation to tick.</summary>
            public Func<IEnumerator> operation;
        }

        /// <summary>Process all stages in order and send progress accordingly.</summary>
        /// <param name="p">Progress notifier to use for progression.</param>
        /// <param name="stages">Stages to execute.</param>
        /// <returns>Return an enumerator to tick to process the stages. Last value is <c>true</c> when it succeeds.</returns>
        protected IEnumerator<bool> ProcessStages(ProgressWrapper p, StageTick[] stages)
        {
            var min = 0f;
            var max = stages[0].stageRange;
            for (var i = 0; i < stages.Length; i++)
            {
                p.SetProgressRange(min, max);
                var e = stages[i].operation();
                while (e.MoveNext())
                {
                    yield return false;
                    if (isCancelled)
                        yield break;
                }
                if (isCancelled)
                    yield break;

                min += stages[i].stageRange;
                max += stages[i].stageRange;
                if (max > 1)
                {
                    var d = max - 1;
                    max -= d;
                    min -= d;
                }
            }

            yield return true;
        }
    }
}
