using System;
using System.IO;
using UnityEditor.Callbacks;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.ImportedGraph
{
    // PF TODO: reload graph when asset is reimported

    public class ImportedGraphAsset : GraphAsset
    {
        GraphWrapper m_WrappingAsset;
        string m_FilePath;

        [OnOpenAsset(1)]
        public static bool OpenGraphAsset(int instanceId, int line)
        {
            var obj = EditorUtility.InstanceIDToObject(instanceId);
            if (obj is ImportedGraphAsset graphAsset)
            {
                var window = GraphViewEditorWindow.FindOrCreateGraphWindow<ImportedGraphWindow>(graphAsset.FilePath);
                window.SetCurrentSelection(graphAsset, GraphViewEditorWindow.OpenMode.OpenAndFocus);
                return true;
            }

            return false;
        }

        protected override Type GraphModelType => typeof(ImportedGraphModel);

        /// <inheritdoc />
        public override void CreateFile(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            path = AssetDatabase.GenerateUniqueAssetPath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "");
            var fileStream = File.Create(path);
            fileStream.Close();

            m_WrappingAsset = CreateInstance<GraphWrapper>();
            m_FilePath = path;
        }

        /// <inheritdoc />
        public override void Save()
        {
            if (m_WrappingAsset == null)
            {
                m_WrappingAsset = CreateInstance<GraphWrapper>();
            }

            if (string.IsNullOrEmpty(m_FilePath))
            {
                m_FilePath = AssetDatabase.GetAssetPath(this);
            }

            m_WrappingAsset.Export(this, m_FilePath);
            Dirty = false;
        }

        /// <inheritdoc />
        public override ISerializedGraphAsset Import()
        {
            AssetDatabase.ImportAsset(m_FilePath);
            var asset = AssetDatabase.LoadAssetAtPath<ImportedGraphAsset>(m_FilePath);
            return asset;
        }
    }
}
