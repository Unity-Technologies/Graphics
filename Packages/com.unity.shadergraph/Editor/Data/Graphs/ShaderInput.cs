using System;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing;
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
            set
            {
                // this is a raw set of the display name
                // if you want to a fully graph-connected set-and-sanitize-and-update,
                // call SetDisplayNameAndSanitizeForGraph() instead
                m_Name = value;
            }
        }

        internal void AddObserver(IShaderInputObserver observer)
        {
            m_InputObservers.Add(observer);
        }

        internal void RemoveObserver(IShaderInputObserver observer)
        {
            m_InputObservers.Remove(observer);
        }

        internal void ClearObservers()
        {
            m_InputObservers.Clear();
        }

        internal HashSet<IShaderInputObserver> InputObservers => m_InputObservers;

        HashSet<IShaderInputObserver> m_InputObservers = new();

        // sanitizes the desired name according to the current graph, and assigns it as the display name
        // also calls the update trigger to update other bits of the graph UI that use the name
        internal void SetDisplayNameAndSanitizeForGraph(GraphData graphData, string desiredName = null)
        {
            string originalDisplayName = displayName;

            // if no desired name passed in, sanitize the current name
            if (desiredName == null)
                desiredName = originalDisplayName;

            var sanitizedName = graphData.SanitizeGraphInputName(this, desiredName);
            bool changed = (originalDisplayName != sanitizedName);

            // only assign if it was changed
            if (changed)
                m_Name = sanitizedName;

            // update the default reference name
            UpdateDefaultReferenceName(graphData);
        }

        internal void SetReferenceNameAndSanitizeForGraph(GraphData graphData, string desiredRefName = null)
        {
            string originalRefName = referenceName;

            // if no desired ref name, use the current name
            if (string.IsNullOrEmpty(desiredRefName))
                desiredRefName = originalRefName;

            // sanitize and deduplicate the desired name
            var sanitizedRefName = graphData.SanitizeGraphInputReferenceName(this, desiredRefName);

            // check if the final result is different from the current name
            bool changed = (originalRefName != sanitizedRefName);

            // if changed, then set the new name up as an override
            if (changed)
                overrideReferenceName = sanitizedRefName;
        }

        // resets the reference name to a "default" value (deduplicated against existing reference names)
        // returns the new default reference name
        internal string ResetReferenceName(GraphData graphData)
        {
            overrideReferenceName = null;

            // because we are clearing an override, we must force a sanitization pass on the default ref name
            // as there may now be collisions that didn't previously exist
            UpdateDefaultReferenceName(graphData, true);

            return referenceName;
        }

        internal void UpdateDefaultReferenceName(GraphData graphData, bool forceSanitize = false)
        {
            if (m_DefaultRefNameVersion <= 0)
                return; // old version is updated in the getter

            if (forceSanitize ||
                string.IsNullOrEmpty(m_DefaultReferenceName) ||
                (m_RefNameGeneratedByDisplayName != displayName))
            {
                // Make sure all reference names are consistently auto-generated with a pre-pended underscore (if they can be renamed)
                var targetRefName = displayName;
                if (this.isReferenceRenamable && !targetRefName.StartsWith("_"))
                    targetRefName = "_" + targetRefName;

                m_DefaultReferenceName = graphData.SanitizeGraphInputReferenceName(this, targetRefName);
                m_RefNameGeneratedByDisplayName = displayName;
            }
        }

        const int k_LatestDefaultRefNameVersion = 1;

        // this is used to know whether this shader input is using:
        // 0) the "old" default reference naming scheme (type + GUID)
        // 1) the new default reference naming scheme (make it similar to the display name)
        [SerializeField]
        int m_DefaultRefNameVersion = k_LatestDefaultRefNameVersion;

        [SerializeField]
        string m_RefNameGeneratedByDisplayName; // used to tell what was the display name used to generate the default reference name

        [SerializeField]
        string m_DefaultReferenceName;          // NOTE: this can be NULL for old graphs, or newly created properties

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
                        // default reference name is updated elsewhere in the new naming scheme
                        return m_DefaultReferenceName;
                    }
                }
                return overrideReferenceName;
            }
        }

        public virtual string referenceNameForEditing => referenceName;

        public override void OnBeforeDeserialize()
        {
            // if serialization doesn't write to m_DefaultRefNameVersion, then it is an old shader input, and should use the old default naming scheme
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

        // returns true if this shader input is CURRENTLY using the new default reference name
        public bool IsUsingNewDefaultRefName()
        {
            return string.IsNullOrEmpty(overrideReferenceName) && (m_DefaultRefNameVersion >= 1);
        }

        // upgrades the default reference name to use the new naming scheme
        internal string UpgradeDefaultReferenceName(GraphData graphData)
        {
            m_DefaultRefNameVersion = k_LatestDefaultRefNameVersion;
            m_DefaultReferenceName = null;
            m_RefNameGeneratedByDisplayName = null;
            UpdateDefaultReferenceName(graphData, true);  // make sure to sanitize the new default
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

        internal bool generatePropertyBlock     // this is basically the "exposed" toggle
        {
            get => m_GeneratePropertyBlock;
            set => m_GeneratePropertyBlock = value;
        }

        internal bool isExposed => isExposable && generatePropertyBlock;

        public virtual bool allowedInSubGraph
        {
            get { return true; }
        }

        public virtual bool allowedInMainGraph
        {
            get { return true; }
        }

        internal abstract ConcreteSlotValueType concreteShaderValueType { get; }

        internal abstract bool isExposable { get; }
        internal virtual bool isAlwaysExposed => false;

        // this controls whether the UI allows the user to rename the display and reference names
        internal abstract bool isRenamable { get; }
        internal virtual bool isReferenceRenamable => isRenamable;

        internal virtual bool isCustomSlotAllowed => true;

        [SerializeField]
        bool m_UseCustomSlotLabel = false;

        [SerializeField]
        string m_CustomSlotLabel;

        internal bool useCustomSlotLabel
        {
            get => m_UseCustomSlotLabel;
            set => m_UseCustomSlotLabel = value;
        }

        internal string customSlotLabel
        {
            get => m_CustomSlotLabel;
            set => m_CustomSlotLabel = value;
        }

        [SerializeField]
        protected int m_DismissedVersion = 0;
        public int dismissedUpdateVersion { get => m_DismissedVersion; set => m_DismissedVersion = value; }

        internal bool isConnectionTestable
        {
            get => m_UseCustomSlotLabel;
        }

        static internal string GetConnectionStateVariableName(string variableName)
        {
            return variableName + "_IsConnected";
        }

        internal abstract ShaderInput Copy();

        internal virtual void OnBeforePasteIntoGraph(GraphData graph) { }
    }
}
