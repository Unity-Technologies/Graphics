using System;
using System.IO;
using Unity.GraphToolsFoundation.Editor;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class ShaderGraphAsset : GraphAsset
    {
        [NonSerialized]
        private string m_FilePath;
        public override string FilePath => m_FilePath;

        internal void SetFilePath(string filePath) => m_FilePath = filePath;

        protected override Type GraphModelType => typeof(SGGraphModel);
        public SGGraphModel SGGraphModel => GraphModel as SGGraphModel;

        protected override void OnEnable()
        {
            Name = Path.GetFileNameWithoutExtension(FilePath);
            base.OnEnable();
        }

        public override void Save()
        {
            if (string.IsNullOrEmpty(FilePath))
            {
                Debug.LogError("No file path");
                return;
            }
            Debug.Log($"ShaderGraphAsset.Save: {FilePath}");

            InternalEditorUtility.SaveToSerializedFileAndForget(new UnityEngine.Object[] { this }, FilePath, true);
            Dirty = false;
            AssetDatabase.ImportAsset(FilePath);
        }
    }
}
