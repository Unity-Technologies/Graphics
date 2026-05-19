using System;
using System.Collections.Generic;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Core;
using UnityEngine;

namespace UnityEditor.Rendering.Universal.ProjectAuditor
{
    internal struct RenderingSettingsIssue
    {
        public string id { get; set; }
        public int? qualityLevel { get; set; }
        public string assetName { get; set; }

        public RenderingSettingsIssue(string id, int? qualityLevel = null, string assetName = null)
        {
            this.id = id;
            this.qualityLevel = qualityLevel;
            this.assetName = assetName;
        }
    }

    internal interface IRenderingSettingsAnalyzer
    {
        public Descriptor Descriptor { get; }
        public IEnumerable<RenderingSettingsIssue> EnumerateIssues();
    }

    partial class SettingsAnalyzer : SettingsModuleAnalyzer
    {
        List<IRenderingSettingsAnalyzer> m_Entries = new List<IRenderingSettingsAnalyzer>();

        public SettingsAnalyzer()
        {
            foreach (var types in TypeCache.GetTypesDerivedFrom<IRenderingSettingsAnalyzer>())
            {
                m_Entries.Add(Activator.CreateInstance(types) as IRenderingSettingsAnalyzer);
            }

            // Sort by Descriptor.Id (alphabetically)
            m_Entries.Sort((a, b) => string.Compare(a.Descriptor.Id, b.Descriptor.Id, StringComparison.Ordinal));
        }

        public override void Initialize(Action<Descriptor> registerDescriptor)
        {
            foreach (var entry in m_Entries)
            {
                registerDescriptor(entry.Descriptor);
            }
        }

        static ReportItem CreateAssetSettingIssue(AnalysisContext context, int? qualityLevel, string name, string id)
        {
            string assetLocation = qualityLevel == null ? "Default Rendering Pipeline Asset" :
                $"Rendering Pipeline Asset on Quality Level: '{QualitySettings.names[qualityLevel.Value]}'";
            return context.CreateIssue(IssueCategory.ProjectSetting, id, name, assetLocation)
                .WithCustomProperties(new object[] { qualityLevel })
                .WithLocation(qualityLevel == null ? "Project/Graphics" : "Project/Quality");
        }

        static ReportItem CreateIssue(AnalysisContext context, string id)
        {
            return context.CreateIssue(IssueCategory.ProjectSetting, id)
                        .WithLocation("Project/Graphics");
        }

        public override IEnumerable<ReportItem> Analyze(SettingsAnalysisContext context)
        {
            foreach (var entry in m_Entries)
            {
                if (!context.IsDescriptorEnabled(entry.Descriptor))
                    continue;

                foreach (var issue in entry.EnumerateIssues())
                {
                    if (string.IsNullOrEmpty(issue.id))
                        throw new InvalidOperationException($"Issue id cannot be null or empty for {entry.Descriptor.Id}");

                    if (issue.qualityLevel == null && string.IsNullOrEmpty(issue.assetName))
                        yield return CreateIssue(context, issue.id);
                    else
                        yield return CreateAssetSettingIssue(context, issue.qualityLevel, issue.assetName, issue.id);
                }
            }
        }
    }
}
