using System;
using UnityEngine;

namespace UnityEditor.ShaderAnalysis
{
    public struct ShaderAnalysisReport
    {
        public static ShaderAnalysisReport<TAsset> New<TAsset>(
            TAsset asset,
            BuildTarget currentPlatform,
            ShaderProgramFilter filter = null,
            BuildReportFeature features = (BuildReportFeature)(-1),
            bool logCommandLines = false
        )
            => new ShaderAnalysisReport<TAsset>
            {
                asset = asset,
                common = new ShaderAnalysisReport
                {
                    targetPlatform = currentPlatform,
                    filter = filter,
                    features = features,
                    logCommandLines = logCommandLines
                }
            };

        public BuildTarget targetPlatform;
        public ShaderProgramFilter filter;
        public BuildReportFeature features;
        /// <summary>True to log in the Unity console the command line used.</summary>
        public bool logCommandLines;
    }

    public struct ShaderAnalysisReport<TAsset>
    {
        public TAsset asset;
        public ShaderAnalysisReport common;

        public ShaderAnalysisReport<TAsset2> Into<TAsset2>()
            => new ShaderAnalysisReport<TAsset2>
            {
                asset = (TAsset2)Convert.ChangeType(asset, typeof(TAsset2)),
                common = common
            };
    }
}
