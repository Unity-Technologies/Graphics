using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Rendering
{
    public partial class MaterialUpgrader
    {
        /// <summary>
        /// The priority of the upgrader.
        /// </summary>
        public virtual int priority => 0;

        /// <summary>
        /// Material Upgrader finalizer delegate.
        /// </summary>
        /// <param name="mat">Material</param>
        public delegate void MaterialFinalizer(Material mat);

        string m_OldShader;
        string m_NewShader;

        /// <summary>
        /// Retrieves path to new shader.
        /// </summary>
        public string NewShaderPath => m_NewShader;

        /// <summary>
        /// Retrieves path to old shader.
        /// </summary>
        public string OldShaderPath => m_OldShader;

        MaterialFinalizer m_Finalizer;

        Dictionary<string, string> m_TextureRename = new Dictionary<string, string>();
        Dictionary<string, string> m_FloatRename = new Dictionary<string, string>();
        Dictionary<string, string> m_ColorRename = new Dictionary<string, string>();

        Dictionary<string, float> m_FloatPropertiesToSet = new Dictionary<string, float>();
        Dictionary<string, Color> m_ColorPropertiesToSet = new Dictionary<string, Color>();
        List<string> m_TexturesToRemove = new List<string>();
        Dictionary<string, Texture> m_TexturesToSet = new Dictionary<string, Texture>();

        class KeywordFloatRename
        {
            public string keyword;
            public string property;
            public float setVal, unsetVal;
        }
        List<KeywordFloatRename> m_KeywordFloatRename = new List<KeywordFloatRename>();
        Dictionary<string, (string, System.Func<float, bool>)> m_ConditionalFloatRename;

        /// <summary>
        /// Type of property to rename.
        /// </summary>
        public enum MaterialPropertyType
        {
            /// <summary>Texture reference property.</summary>
            Texture,
            /// <summary>Float property.</summary>
            Float,
            /// <summary>Color property.</summary>
            Color
        }

        /// <summary>
        /// Retrieves a collection of renamed parameters of a specific MaterialPropertyType.
        /// </summary>
        /// <param name="type">Material Property Type</param>
        /// <returns>Dictionary of property names to their renamed values.</returns>
        /// <exception cref="ArgumentException">type is not valid.</exception>
        public IReadOnlyDictionary<string, string> GetPropertyRenameMap(MaterialPropertyType type)
        {
            switch (type)
            {
                case MaterialPropertyType.Texture: return m_TextureRename;
                case MaterialPropertyType.Float: return m_FloatRename;
                case MaterialPropertyType.Color: return m_ColorRename;
                default: throw new ArgumentException(nameof(type));
            }
        }

        /// <summary>
        /// Upgrade Flags
        /// </summary>
        [Flags]
        public enum UpgradeFlags
        {
            /// <summary>None.</summary>
            None = 0,
            /// <summary>LogErrorOnNonExistingProperty.</summary>
            LogErrorOnNonExistingProperty = 1,
            /// <summary>CleanupNonUpgradedProperties.</summary>
            CleanupNonUpgradedProperties = 2,
            /// <summary>LogMessageWhenNoUpgraderFound.</summary>
            LogMessageWhenNoUpgraderFound = 4
        }

        /// <summary>
        /// Upgrade method.
        /// </summary>
        /// <param name="material">Material to upgrade.</param>
        /// <param name="flags">Upgrade flag</param>
        public void Upgrade(Material material, UpgradeFlags flags)
        {
            Material newMaterial;
            if ((flags & UpgradeFlags.CleanupNonUpgradedProperties) != 0)
            {
                newMaterial = new Material(Shader.Find(m_NewShader));
            }
            else
            {
                newMaterial = UnityEngine.Object.Instantiate(material) as Material;
                newMaterial.shader = Shader.Find(m_NewShader);
            }

            Convert(material, newMaterial);

            material.shader = Shader.Find(m_NewShader);
            material.CopyPropertiesFromMaterial(newMaterial);
            UnityEngine.Object.DestroyImmediate(newMaterial);

            if (m_Finalizer != null)
                m_Finalizer(material);
        }

        // Overridable function to implement custom material upgrading functionality
        /// <summary>
        /// Custom material conversion method.
        /// </summary>
        /// <param name="srcMaterial">Source material.</param>
        /// <param name="dstMaterial">Destination material.</param>
        public virtual void Convert(Material srcMaterial, Material dstMaterial)
        {
            foreach (var t in m_TextureRename)
            {
                if (!srcMaterial.HasProperty(t.Key) || !dstMaterial.HasProperty(t.Value))
                    continue;

                dstMaterial.SetTextureScale(t.Value, srcMaterial.GetTextureScale(t.Key));
                dstMaterial.SetTextureOffset(t.Value, srcMaterial.GetTextureOffset(t.Key));
                dstMaterial.SetTexture(t.Value, srcMaterial.GetTexture(t.Key));
            }

            foreach (var t in m_FloatRename)
            {
                if (!srcMaterial.HasProperty(t.Key) || !dstMaterial.HasProperty(t.Value))
                    continue;

                dstMaterial.SetFloat(t.Value, srcMaterial.GetFloat(t.Key));
            }

            foreach (var t in m_ColorRename)
            {
                if (!srcMaterial.HasProperty(t.Key) || !dstMaterial.HasProperty(t.Value))
                    continue;

                dstMaterial.SetColor(t.Value, srcMaterial.GetColor(t.Key));
            }

            foreach (var prop in m_TexturesToRemove)
            {
                if (!dstMaterial.HasProperty(prop))
                    continue;

                dstMaterial.SetTexture(prop, null);
            }

            foreach (var prop in m_TexturesToSet)
            {
                if (!dstMaterial.HasProperty(prop.Key))
                    continue;

                dstMaterial.SetTexture(prop.Key, prop.Value);
            }

            foreach (var prop in m_FloatPropertiesToSet)
            {
                if (!dstMaterial.HasProperty(prop.Key))
                    continue;

                dstMaterial.SetFloat(prop.Key, prop.Value);
            }

            foreach (var prop in m_ColorPropertiesToSet)
            {
                if (!dstMaterial.HasProperty(prop.Key))
                    continue;

                dstMaterial.SetColor(prop.Key, prop.Value);
            }

            foreach (var t in m_KeywordFloatRename)
            {
                if (!dstMaterial.HasProperty(t.property))
                    continue;

                dstMaterial.SetFloat(t.property, srcMaterial.IsKeywordEnabled(t.keyword) ? t.setVal : t.unsetVal);
            }

            // Handle conditional float renaming
            if (m_ConditionalFloatRename != null)
            {
                foreach (var (oldName, (newName, condition)) in m_ConditionalFloatRename)
                {
                    if (srcMaterial.HasProperty(oldName) &&
                        condition(srcMaterial.GetFloat(oldName)) &&
                        dstMaterial.HasProperty(newName))
                    {
                        dstMaterial.SetFloat(newName, 1.0f);
                    }
                }
            }
        }

        /// <summary>
        /// Rename shader.
        /// </summary>
        /// <param name="oldName">Old name.</param>
        /// <param name="newName">New name.</param>
        /// <param name="finalizer">Finalizer delegate.</param>
        public void RenameShader(string oldName, string newName, MaterialFinalizer finalizer = null)
        {
            m_OldShader = oldName;
            m_NewShader = newName;
            m_Finalizer = finalizer;
        }

        /// <summary>
        /// Rename Texture Parameter.
        /// </summary>
        /// <param name="oldName">Old name.</param>
        /// <param name="newName">New name.</param>
        public void RenameTexture(string oldName, string newName)
        {
            m_TextureRename[oldName] = newName;
        }

        /// <summary>
        /// Rename Float Parameter.
        /// </summary>
        /// <param name="oldName">Old name.</param>
        /// <param name="newName">New name.</param>
        public void RenameFloat(string oldName, string newName)
        {
            m_FloatRename[oldName] = newName;
        }

        /// <summary>
        /// Rename Color Parameter.
        /// </summary>
        /// <param name="oldName">Old name.</param>
        /// <param name="newName">New name.</param>
        public void RenameColor(string oldName, string newName)
        {
            m_ColorRename[oldName] = newName;
        }

        /// <summary>
        /// Remove Texture Parameter.
        /// </summary>
        /// <param name="name">Parameter name.</param>
        public void RemoveTexture(string name)
        {
            m_TexturesToRemove.Add(name);
        }

        /// <summary>
        /// Set float property.
        /// </summary>
        /// <param name="propertyName">Property name.</param>
        /// <param name="value">Property value.</param>
        public void SetFloat(string propertyName, float value)
        {
            m_FloatPropertiesToSet[propertyName] = value;
        }

        /// <summary>
        /// Set color property.
        /// </summary>
        /// <param name="propertyName">Property name.</param>
        /// <param name="value">Property value.</param>
        public void SetColor(string propertyName, Color value)
        {
            m_ColorPropertiesToSet[propertyName] = value;
        }

        /// <summary>
        /// Set texture property.
        /// </summary>
        /// <param name="propertyName">Property name.</param>
        /// <param name="value">Property value.</param>
        public void SetTexture(string propertyName, Texture value)
        {
            m_TexturesToSet[propertyName] = value;
        }

        /// <summary>
        /// Rename a keyword to float.
        /// </summary>
        /// <param name="oldName">Old name.</param>
        /// <param name="newName">New name.</param>
        /// <param name="setVal">Value when set.</param>
        /// <param name="unsetVal">Value when unset.</param>
        public void RenameKeywordToFloat(string oldName, string newName, float setVal, float unsetVal)
        {
            m_KeywordFloatRename.Add(new KeywordFloatRename { keyword = oldName, property = newName, setVal = setVal, unsetVal = unsetVal });
        }

        /// <summary>
        /// Rename a float property conditionally based on its value
        /// </summary>
        /// <param name="oldName">Old property name</param>
        /// <param name="newName">New property name</param>
        /// <param name="condition">Condition function that takes the float value and returns true if renaming should occur</param>
        protected void RenameFloat(string oldName, string newName, System.Func<float, bool> condition)
        {
            (m_ConditionalFloatRename ??= new Dictionary<string, (string, System.Func<float, bool>)>())[oldName] = (newName, condition);
        }
    }
}
