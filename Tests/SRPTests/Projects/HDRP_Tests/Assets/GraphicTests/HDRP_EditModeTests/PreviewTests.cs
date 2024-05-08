using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Rendering;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.IO;
using Object = UnityEngine.Object;
using System.Linq;
using System.Text.RegularExpressions;

namespace UnityEditor.Previews
{
    class PreviewTests
    {
        public class WaitForPreview : CustomYieldInstruction
        {
            Object m_Asset;
            bool m_Done;
            public Texture2D Texture { get; private set; }

            public float WaitTime { get; set; }
            float m_WaitUntilTime = -1;
            float m_NextCheckTime = -1;
            float m_CheckInterval = 0.5f;

            public override bool keepWaiting
            {
                get
                {
                    if (m_Asset == null)
                        return false;

                    var now = Time.realtimeSinceStartup;
                    if (m_WaitUntilTime < 0)
                    {
                        m_WaitUntilTime = now + WaitTime;
                    }

                    bool wait =  now < m_WaitUntilTime;
                    bool performCheck = !wait || m_NextCheckTime < now;

                    if (performCheck)
                    {
                        Texture = AssetPreview.GetAssetPreview(m_Asset);
                        m_NextCheckTime = now + m_CheckInterval;
                    }

                    if (!wait)
                    {
                        // Reset so it can be reused.
                        Reset();
                    }

                    return wait && (Texture == null || !string.IsNullOrEmpty(Texture.name));
                }
            }

            public WaitForPreview(Object asset, float time, float checkInterval = 0.1f)
            {
                m_Asset = asset;
                m_CheckInterval = checkInterval;
                WaitTime = time;
                Texture = null;
            }

            public override void Reset()
            {
                m_WaitUntilTime = -1;
                m_NextCheckTime = -1;
                Texture = null;
            }
        }

        [UnityTest]
        [Ignore("Case https://jira.unity3d.com/browse/UUM-18048")]
        public IEnumerator CreatePreviewDoesNotLeakMemoryInWorkers()
        {
            var logEntryString = "There are remaining Allocations on the JobTempAlloc.";
            int prevLogEntryCount = GetLogEntryCount(logEntryString);

            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            var folderName = "Create_Preview_Test";
            try
            {
                AssetDatabase.CreateFolder("Assets", folderName);

                var prefabPath = Path.Combine($"Assets/{folderName}", "sphere_prefab.prefab");
                var prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(sphere, prefabPath, InteractionMode.UserAction,
                    out var prefabSuccess);

                Assert.IsTrue(prefabSuccess, $"The prefab at {prefabPath} could not be created");

                //Get the preview
                var w = new WaitForPreview(prefab, 90.0f);
                yield return w;

                //Here we catch any unexpected memory leak messages
                int logEntryCountAfterTest = GetLogEntryCount(logEntryString);
                Assert.AreEqual(prevLogEntryCount, logEntryCountAfterTest, "There were leaks in the test");
            }
            finally
            {
                AssetDatabase.DeleteAsset($"Assets/{folderName}");
            }
        }

        // Test case for UUM-63257
        [UnityTest]
        public IEnumerator CreateMeshPreviewDoesNotLeakMemoryInWorkers()
        {
            var logEntryString = "There are remaining Allocations on the JobTempAlloc.";
            int prevLogEntryCount = GetLogEntryCount(logEntryString);

            var folderName = "Create_Mesh_Preview_Test";
            try
            {
                AssetDatabase.CreateFolder("Assets", folderName);

                for (int i = 0; i < 2; ++i)
                {
                    var meshPath = Path.Combine($"Assets/{folderName}", $"m{i}.mesh");
                    var mesh = CreateUniqueMiniMesh();
                    AssetDatabase.CreateAsset(mesh, meshPath);
                    yield return new WaitForPreview(mesh, 90.0f);
                }

                //Here we catch any unexpected memory leak messages
                int logEntryCountAfterTest = GetLogEntryCount(logEntryString);
                Assert.AreEqual(prevLogEntryCount, logEntryCountAfterTest, "There were leaks in the test");
            }
            finally
            {
                AssetDatabase.DeleteAsset($"Assets/{folderName}");
            }
        }

        Mesh CreateUniqueMiniMesh()
        {
            // Use random value because the preview won't be regenerated if the mesh is already present in the library cache
            float randomValue = (float)new System.Random(Guid.NewGuid().GetHashCode()).NextDouble();

            Mesh mesh = new Mesh();
            mesh.vertices = new Vector3[]
            {
                new Vector3(randomValue, 0, 0),
                new Vector3(3.0f, 0, 0),
                new Vector3(0, 1.0f, 0)
            };

            return mesh;
        }

        private int GetLogEntryCount(string entry)
        {
            int entryCount = 0;
            FileMode fm = FileMode.Open;
            FileStream fileStream = new FileStream(Application.consoleLogPath, fm, FileAccess.ReadWrite, FileShare.ReadWrite);
            StreamReader streamReader = new StreamReader(fileStream);
            if (fileStream != null && streamReader != null)
            {
                var contents = streamReader.ReadToEnd();
                var lines = contents.Split('\n');
                foreach (var line in lines)
                {
                    if (line.Contains(entry))
                    {
                        entryCount++;
                    }
                }
            }
            fileStream.Close();
            return entryCount;
        }
    }
}
