using System;
using UnityEditor.ShaderAnalysis.Internal;
using UnityEngine;

namespace UnityEditor.ShaderAnalysis
{
    public static class CLI
    {
        struct BuildReportArg
        {
            public string assetPath;
            public string exporter;
            public BuildTarget targetPlatform;
            public string outputFile;
            public float timeout;
            public string variantFilter;
            public string shaderPassFilter;

            public static BuildReportArg Parse(string[] args)
            {
                BuildTarget? targetPlatform = null;
                var result = new BuildReportArg();
                result.timeout = 600; // 10 min timeout
                for (var i = 0; i < args.Length; ++i)
                {
                    switch (args[i])
                    {
                        case "-assetPath":
                            ++i;
                            if (i >= args.Length) throw new ArgumentException($"Missing value for '{nameof(assetPath)}");
                            result.assetPath = args[i];
                            break;
                        case "-exporter":
                            ++i;
                            if (i >= args.Length) throw new ArgumentException($"Missing value for '{nameof(exporter)}");
                            result.exporter = args[i];
                            break;
                        case "-targetPlatform":
                            ++i;
                            if (i >= args.Length) throw new ArgumentException($"Missing value for '{nameof(targetPlatform)}");
                            targetPlatform = (BuildTarget)Enum.Parse(typeof(BuildTarget), args[i]);
                            break;
                        case "-outputFile":
                            ++i;
                            if (i >= args.Length) throw new ArgumentException($"Missing value for '{nameof(outputFile)}");
                            result.outputFile = args[i];
                            break;
                        case "-variantFilter":
                            ++i;
                            if (i >= args.Length) throw new ArgumentException($"Missing value for '{nameof(variantFilter)}");
                            result.variantFilter = args[i];
                            break;
                        case "-shaderPassFilter":
                            ++i;
                            if (i >= args.Length) throw new ArgumentException($"Missing value for '{nameof(shaderPassFilter)}");
                            result.shaderPassFilter = args[i];
                            break;
                        case "-timeout":
                            ++i;
                            if (i >= args.Length) throw new ArgumentException($"Missing value for '{nameof(timeout)}");
                            result.timeout = float.Parse(args[i]);
                            break;
                    }
                }
                if (string.IsNullOrEmpty(result.assetPath))
                    throw new ArgumentException($"{nameof(assetPath)} is missing");
                if (string.IsNullOrEmpty(result.exporter))
                    throw new ArgumentException($"{nameof(exporter)} is missing");
                if (string.IsNullOrEmpty(result.outputFile))
                    throw new ArgumentException($"{nameof(outputFile)} is missing");
                if (!targetPlatform.HasValue)
                    throw new ArgumentException($"{nameof(targetPlatform)} is missing");
                result.targetPlatform = targetPlatform.Value;
                return result;
            }
        }

        /// <summary>Build a report by parsing command line arguments.</summary>
        /// <example>
        /// "$UNITY_EXE" -projectPath $PROJECT_PATH -batchMode -quit -executeMethod UnityEditor.ShaderAnalysis.CLI.BuildReport -assetPath Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Deferred.shader -exporter CSV -targetPlatform PS4 -outputFile $OUTPUT_FILE -shaderPassFilter $PASS_FILTER -variantFilter $VARIANT_FILTER
        /// </example>
        public static void BuildReport()
        {
            var args = Environment.GetCommandLineArgs();
            var buildReportArgs = BuildReportArg.Parse(args);
            InternalBuildReport(buildReportArgs.assetPath, buildReportArgs.exporter, buildReportArgs.targetPlatform, buildReportArgs.outputFile, buildReportArgs.timeout, buildReportArgs.shaderPassFilter, buildReportArgs.variantFilter);
        }

        static void InternalBuildReport(string assetPath, string exporter, BuildTarget targetPlatform, string outputFile, float timeout, string shaderPassFilter, string variantFilter)
        {
            if (!ExporterUtilities.GetExporterIndex(exporter, out var index))
                throw new ArgumentException($"Unknown exporter {exporter}");

            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid))
                throw new ArgumentException($"{assetPath} is not a valid asset path.");

            var filter = ShaderProgramFilter.Parse(shaderPassFilter, variantFilter);
            var assetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
            AsyncBuildReportJob buildReportJob = null;
            if (assetType == typeof(ComputeShader))
            {
                var castedAsset = AssetDatabase.LoadAssetAtPath<ComputeShader>(assetPath);
                buildReportJob = (AsyncBuildReportJob)EditorShaderTools.GenerateBuildReportAsync(castedAsset, targetPlatform, filter, (BuildReportFeature)(-1));
            }
            else if (assetType == typeof(Shader))
            {
                var castedAsset = AssetDatabase.LoadAssetAtPath<Shader>(assetPath);
                buildReportJob = (AsyncBuildReportJob)EditorShaderTools.GenerateBuildReportAsync(castedAsset, targetPlatform, filter, (BuildReportFeature)(-1));
            }
            else if (assetType == typeof(Material))
            {
                var castedAsset = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
                buildReportJob = (AsyncBuildReportJob)EditorShaderTools.GenerateBuildReportAsync(castedAsset, targetPlatform, filter, (BuildReportFeature)(-1));
            }
            else
                throw new ArgumentException($"Unsupported asset type: {assetType}");

            var time = Time.realtimeSinceStartup;
            var startTime = time;
            while (!buildReportJob.IsComplete())
            {
                if (Time.realtimeSinceStartup - time > 3)
                {
                    Debug.Log($"[Build Report] {assetPath} {buildReportJob.progress:P} {buildReportJob.message}");
                    time = Time.realtimeSinceStartup;
                }

                if (Time.realtimeSinceStartup - startTime > timeout)
                {
                    buildReportJob.Cancel();
                    throw new Exception($"Timeout {timeout} s");
                }
                buildReportJob.Tick();
                EditorUpdateManager.Tick();
            }

            var report = buildReportJob.builtReport;
            ExporterUtilities.Export(index, report, outputFile);
        }
    }
}
