using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using com.unity.test.performance.runtimesettings;
using UnityEngine;

namespace com.unity.test.metadatamanager
{
    public class CustomMetadataManager : ICustomMetadataManager
    {
        private readonly StringBuilder metadata = new StringBuilder();
        private readonly List<string> dependencies;
        public static string MetadataFileName = "metadata.txt";

        public CustomMetadataManager(List<string> dependencies)
        {
            this.dependencies = dependencies;
        }

        public string GetCustomMetadata()
        {
            var settings = Resources.Load<CurrentSettings>("settings");

            var keyValuePairs = new[]
            {
                new KeyValuePair<string, string>("username", settings.Username),
                new KeyValuePair<string, string>("burstenabled", settings.EnableBurst.ToString()),
                new KeyValuePair<string, string>("PackageUnderTestName", settings.PackageUnderTestName),
                new KeyValuePair<string, string>("PackageUnderTestVersion", settings.PackageUnderTestVersion),
                new KeyValuePair<string, string>("PackageUnderTestRevision", settings.PackageUnderTestRevision),
                new KeyValuePair<string, string>("PackageUnderTestRevisionDate", settings.PackageUnderTestRevisionDate),
                new KeyValuePair<string, string>("PackageUnderTestPackageBranch", settings.PackageUnderTestPackageBranch),
                new KeyValuePair<string, string>("renderpipeline", settings.RenderPipeline),
                new KeyValuePair<string, string>("testsbranch", settings.TestsBranch),
                new KeyValuePair<string, string>("testsrev", settings.TestsRevision),
                new KeyValuePair<string, string>("testsrevdate", settings.TestsRevisionDate),
                new KeyValuePair<string, string>("dependencies", string.Join(",", dependencies)),
                new KeyValuePair<string, string>("MtRendering", string.Join(",", settings.MtRendering.ToString())),
                new KeyValuePair<string, string>("GraphicsJobs", string.Join(",", settings.GraphicsJobs.ToString())),
                new KeyValuePair<string, string>("joblink", string.Join(",", settings.JobLink)),
                new KeyValuePair<string, string>("jobworkercount", string.Join(",", settings.JobWorkerCount)),
                new KeyValuePair<string, string>("renderpipeline", string.Join(",", settings.RenderPipeline)),            };
            AppendMetadata(keyValuePairs);

            ReadAndAppendCustomMetadataFromFile();

            return metadata.Remove(0, 1).ToString();
        }

        private void ReadAndAppendCustomMetadataFromFile()
        {
            var tempFilePath = Path.Combine(Directory.GetCurrentDirectory(), MetadataFileName);
            if (File.Exists(tempFilePath))
            {
                using (StreamReader sr = new StreamReader(tempFilePath))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        metadata.Append(line);
                    }
                }
            }
        }

        private void AppendMetadata(IEnumerable<KeyValuePair<string, string>> keyValuePairs)
        {
            foreach (var keyValuePair in keyValuePairs)
            {
                metadata.Append(string.Format("|{0}={1}", keyValuePair.Key, keyValuePair.Value));
            }
        }
    }
}
