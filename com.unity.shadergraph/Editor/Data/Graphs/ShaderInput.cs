using System;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Internal
{
    public abstract class ShaderInput : JsonObject
    {
        [SerializeField]
        SerializableGuid m_Guid = new SerializableGuid();

        internal Guid guid => m_Guid.guid;

        internal void OverrideGuid(string namespaceId, string name) { m_Guid.guid = GenerateNamespaceUUID(namespaceId, name); }

        [SerializeField]
        string m_Name;

        public string displayName
        {
            get
            {
                if (string.IsNullOrEmpty(m_Name))
                    return $"{concreteShaderValueType}_{objectId}";
                return m_Name;
            }
            set => m_Name = value;
        }

        internal void SetDisplayNameAndSanitize(string desiredName, GraphData graphData)
        {
            m_Name = desiredName;
            graphData.SanitizeGraphInputName(this);
            if (String.IsNullOrEmpty(overrideReferenceName))
                UpdateDefaultReferenceName(graphData);
        }

        internal void SetOverrideReferenceNameAndSanitize(string desiredRefName, GraphData graphData)
        {
            graphData.SanitizeGraphInputReferenceName(this, desiredRefName);
        }

        // resets the reference name to a "default" value (deduplicated against existing reference names)
        // returns the new default reference name
        internal string ResetReferenceName(GraphData graphData)
        {
            overrideReferenceName = null;
            UpdateDefaultReferenceName(graphData);
            return referenceName;
        }

        internal void UpdateDefaultReferenceName(GraphData graphData)
        {
            if (m_DefaultRefNameVersion <= 0)
                return; // old version is updated in the getter

            var dispName = displayName;
            if (string.IsNullOrEmpty(m_DefaultReferenceName) ||
                (m_RefNameGeneratedByDisplayName != dispName))
            {
                var hlslName = NodeUtils.ConvertToValidHLSLIdentifier(dispName, NodeUtils.IsShaderLabKeyWord);
                m_DefaultReferenceName = graphData.DeduplicateGraphInputReferenceName(this, hlslName);
                m_RefNameGeneratedByDisplayName = dispName;
            }
        }

        const int k_LatestDefaultRefNameVersion = 1;

        [SerializeField]
        int m_DefaultRefNameVersion = k_LatestDefaultRefNameVersion;

        [SerializeField]
        string m_RefNameGeneratedByDisplayName; // used to tell what was the display name used to generate the default reference name

        [SerializeField]
        string m_DefaultReferenceName;      // NOTE: this can be NULL for old graphs, or newly created properties

        public string referenceName
        {
            get
            {
                if (string.IsNullOrEmpty(overrideReferenceName))
                {
                    if (m_DefaultRefNameVersion == 0)
                    {
                        if (string.IsNullOrEmpty(m_DefaultReferenceName))
                            m_DefaultReferenceName = GetOldDefaultReferenceName();
                        return m_DefaultReferenceName;
                    }
                    else // version 1
                    {
                        if (m_DefaultReferenceName == null)
                            Debug.Log("Default Reference Name is NULL for " + displayName);
                        return m_DefaultReferenceName;
                    }
                }
                return overrideReferenceName;
            }
        }

        public override void OnBeforeDeserialize()
        {
            m_DefaultRefNameVersion = 0;
            base.OnBeforeDeserialize();
        }

        // This is required to handle Material data serialized with "_Color_GUID" reference names
        // m_DefaultReferenceName expects to match the material data and previously used PropertyType
        // ColorShaderProperty is the only case where PropertyType doesn't match ConcreteSlotValueType
        public virtual string GetOldDefaultReferenceName()
        {
            return $"{concreteShaderValueType.ToString()}_{objectId}";
        }

        // returns true if this shader input is CURRENTLY using the old default reference name
        public bool IsUsingOldDefaultRefName()
        {
            return string.IsNullOrEmpty(overrideReferenceName) && (m_DefaultRefNameVersion == 0);
        }

        // upgrades the default reference name to use the new naming scheme
        internal string UpgradeDefaultReferenceName(GraphData graphData)
        {
            m_DefaultRefNameVersion = k_LatestDefaultRefNameVersion;
            m_DefaultReferenceName = null;
            m_RefNameGeneratedByDisplayName = null;
            UpdateDefaultReferenceName(graphData);
            return referenceName;
        }

        [SerializeField]
        string m_OverrideReferenceName;

        internal string overrideReferenceName
        {
            get => m_OverrideReferenceName;
            set => m_OverrideReferenceName = value;
        }

        [SerializeField]
        bool m_GeneratePropertyBlock = true;

        internal bool generatePropertyBlock
        {
            get => m_GeneratePropertyBlock;
            set => m_GeneratePropertyBlock = value;
        }

        internal abstract ConcreteSlotValueType concreteShaderValueType { get; }
        internal abstract bool isExposable { get; }
        internal virtual bool isAlwaysExposed => false;
        internal abstract bool isRenamable { get; }

        internal abstract ShaderInput Copy();
    }
}
