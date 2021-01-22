using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace UnityEditor.Rendering.ShaderGraph
{
    [Serializable]
    public class Metadata : ScriptableObject
    {
        [SerializeField]
        List<string> m_LockedProperties;

        public List<string> lockedProperties
        {
            get => m_LockedProperties;
            set => m_LockedProperties = value;
        }

        static public ReadOnlyCollection<string> GetLocksFromMetadata(Shader shader)
        {
            var path = AssetDatabase.GetAssetPath(shader);

            var allAssets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var asset in allAssets)
            {
                if (asset is Metadata metadataAsset)
                {
                    return metadataAsset.m_LockedProperties.AsReadOnly();
                }
            }

            return null;
        }
    }
}
