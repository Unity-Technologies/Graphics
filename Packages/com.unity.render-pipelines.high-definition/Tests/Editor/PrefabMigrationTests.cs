using System;
using System.IO;
using UnityEditor;

namespace UnityEngine.Rendering.HighDefinition.Tests
{
    struct PrefabMigrationTests : IDisposable
    {
        string m_GeneratedPrefabFileName;

        public PrefabMigrationTests(string id, string YAML, out GameObject instance)
        {
            m_GeneratedPrefabFileName = $"Assets/Temporary/{id}.prefab";

            var fileInfo = new FileInfo(m_GeneratedPrefabFileName);
            if (!fileInfo.Directory.Exists)
                fileInfo.Directory.Create();

            File.WriteAllText(m_GeneratedPrefabFileName, YAML);

            AssetDatabase.ImportAsset(m_GeneratedPrefabFileName);

            instance = AssetDatabase.LoadAssetAtPath<GameObject>(m_GeneratedPrefabFileName);
            instance.hideFlags = HideFlags.None;
        }

        public void Dispose() => AssetDatabase.DeleteAsset(m_GeneratedPrefabFileName);
    }
}
