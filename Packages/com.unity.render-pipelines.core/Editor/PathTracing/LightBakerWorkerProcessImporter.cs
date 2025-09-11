using System.IO;
using System.Threading;
using UnityEditor.AssetImporters;
using UnityEditor.Experimental;
using UnityEditor.LightBaking;
using UnityEngine;
using UnityEngine.LightTransport;
using static UnityEditor.LightBaking.LightBaker;

namespace UnityEditor.PathTracing.LightBakerBridge
{
    [ScriptedImporter(version: 1, ext: "ext_unused_lightbaker")]
    internal class LightBakerWorkerProcessImporter : ScriptedImporter
    {
        private const string LightBakerWorkerProcess = "[LightBaker worker process] ";
        private const string BakeAssetFolder = "Temp/LightBakerAssetImport";

        internal static bool BakeWithWorkerProcess(string bakeInputPath, string lightmapRequestsPath,
            string lightProbeRequestsPath, string bakeOutputFolderPath, int progressPort)
        {
            // Setup hidden import folder.
            if (!AssetDatabase.AssetPathExists(BakeAssetFolder))
                Directory.CreateDirectory(BakeAssetFolder);

            var assetPath = $"{BakeAssetFolder}/{nameof(BakeImport)}.asset";

            // Write the 'recipe' asset.
            var importAsset = ScriptableObject.CreateInstance<BakeImport>();
            importAsset.name = nameof(BakeImport);
            importAsset.BakeInputPath = bakeInputPath;
            importAsset.LightmapRequestsPath = lightmapRequestsPath;
            importAsset.LightProbeRequestsPath = lightProbeRequestsPath;
            importAsset.BakeOutputFolderPath = bakeOutputFolderPath;
            importAsset.ProgressPort = progressPort;

            // Import it.
            var guid = GUID.Generate();
            UnityEditorInternal.InternalEditorUtility.SaveToSerializedFileAndForget(new Object[] { importAsset }, assetPath, true);
            File.WriteAllText(assetPath + ".meta", $"fileFormatVersion: 2\nguid: {guid}\nDefaultImporter:\n  externalObjects: {{}}\n  assetBundleName:\n  assetBundleVariant:\n");
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            // Generate an artifact asynchronously, kicking off the bake.
            var headKey = new ArtifactKey(guid, typeof(LightBakerWorkerProcessImporter));
            AssetDatabaseExperimental.ProduceArtifactAsync(headKey);

            return true;
        }

        public override void OnImportAsset(AssetImportContext ctx)
        {
            CancellationTokenSource bakeIsFinished = new();
            ExternalProcessConnection progressConnection = new();
            BakeProgressState progressState = new();

            using (progressConnection)
            using (bakeIsFinished)
            using (progressState)
            {
                try
                {
                    var bakeImport = AssetDatabase.LoadAssetAtPath<BakeImport>(ctx.assetPath);
                    if (bakeImport == null)
                    {
                        Debug.LogError($"{LightBakerWorkerProcess}Failed to load asset at path: {ctx.assetPath}.");
                        return;
                    }

                    // If the Editor is waiting for us to connect, it will time out if we cannot connect.
                    if (!progressConnection.Connect(bakeImport.ProgressPort))
                    {
                        Debug.LogError($"{LightBakerWorkerProcess}Failed to connect to the parent process on port '{bakeImport.ProgressPort}'.");
                        return;
                    }

                    // Bake while reporting progress.
                    Thread progressReporterThread = new(() => ProgressReporterThreadFunction(progressConnection, bakeIsFinished.Token, progressState));
                    progressReporterThread.Start();

                    bool bakeOk = LightBakerStrangler.Bake(bakeImport.BakeInputPath, bakeImport.LightmapRequestsPath, bakeImport.LightProbeRequestsPath, bakeImport.BakeOutputFolderPath, progressState);

                    // Stop the progress thread and wait for the thread.
                    bakeIsFinished.Cancel();
                    progressReporterThread.Join();

                    ReportResultToParentProcess(
                        bakeOk
                            ? new Result { type = ResultType.Success, message = "Success." }
                            : new Result { type = ResultType.JobFailed, message = "LightBakerStrangler.Bake failed." }, progressConnection);
                }
                finally
                {
                    // Stop the progress thread.
                    bakeIsFinished.Cancel();
                }
            }
        }

        private static void ProgressReporterThreadFunction(ExternalProcessConnection connection, CancellationToken bakeIsFinished, BakeProgressState progressState)
        {
            if (connection == null)
                return;
            const int waitBetweenProgressReportsMs = 100;
            while (!progressState.WasCancelled() && !bakeIsFinished.IsCancellationRequested)
            {
                bool cancellationRequested = LightBaker.GetCancellationRequested(connection);
                if (cancellationRequested)
                    progressState.Cancel();

                float progress = progressState.Progress();
                ReportProgressToParentProcess(progress, connection);

                Thread.Sleep(waitBetweenProgressReportsMs);
            }
        }
    }
}
