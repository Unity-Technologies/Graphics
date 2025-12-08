using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Rendering.Universal;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Class containing utility methods for upgrading assets affected by render pipeline migration.
    /// </summary>
    static class UpgradeUtility
    {
        /// <summary>
        /// Stable, unique identifier for some asset.
        /// </summary>
        internal struct UID
        {
            public string Value;
            public static implicit operator string(UID uid) => uid.Value;
            public static implicit operator UID(string id) => new UID { Value = id };
        }

        internal interface IMaterial
        {
            UID ID { get; }
            string ShaderName { get; }
        }

        internal struct MaterialProxy : IMaterial
        {
            public MaterialProxy(Material material, UnityObject[] allAssetsAtPath)
            {
                m_ID = $"{allAssetsAtPath}{Array.IndexOf(allAssetsAtPath, material)}";
                m_Material = material;
            }

            UID m_ID;
            Material m_Material;
            public UID ID => m_ID;
            public string ShaderName => m_Material.shader.name;
            public static implicit operator Material(MaterialProxy proxy) => proxy.m_Material;
            public static implicit operator MaterialProxy(Material material) =>
                new MaterialProxy(material, AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(material)));
            public override string ToString() => m_Material.ToString();
        }
    }
}
