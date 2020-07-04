using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Legacy;
using UnityEditor.Rendering.HighDefinition.ShaderGraph.Legacy;
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;
using static UnityEditor.Rendering.HighDefinition.HDShaderUtils;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    abstract class HDSubTarget : SubTarget<HDTarget>, IHasMetadata,
        IRequiresData<SystemData>, IVersionable<ShaderGraphVersion>
    {
        SystemData m_SystemData;
        protected bool m_MigrateFromOldCrossPipelineSG; // Use only for the migration to shader stack architecture

        // Interface Properties
        SystemData IRequiresData<SystemData>.data
        {
            get => m_SystemData;
            set => m_SystemData = value;
        }

        // Public properties
        public SystemData systemData
        {
            get => m_SystemData;
            set => m_SystemData = value;
        }

        protected virtual int ComputeMaterialNeedsUpdateHash()
        {
            // Alpha test is currently the only property in system data to trigger the material upgrade script.
            int hash = systemData.alphaTest.GetHashCode();
            return hash;
        }

        public override bool IsActive() => true;

        protected abstract ShaderID shaderID { get; }
        protected abstract string customInspector { get; }
        protected abstract string subTargetAssetGuid { get; }
        protected abstract string renderType { get; }
        protected abstract string renderQueue { get; }
        protected abstract string templatePath { get; }
        protected abstract string[] templateMaterialDirectories { get; }

        public virtual string identifier => GetType().Name;

        public virtual ScriptableObject GetMetadataObject()
        {
            var hdMetadata = ScriptableObject.CreateInstance<HDMetadata>();
            hdMetadata.shaderID = shaderID;
            hdMetadata.migrateFromOldCrossPipelineSG = m_MigrateFromOldCrossPipelineSG;
            return hdMetadata;
        }

        ShaderGraphVersion IVersionable<ShaderGraphVersion>.version
        {
            get => systemData.version;
            set => systemData.version = value;
        }

        // Generate migration description steps to migrate HD shader targets
        internal static MigrationDescription<ShaderGraphVersion, HDSubTarget> migrationSteps => MigrationDescription.New(
            Enum.GetValues(typeof(ShaderGraphVersion)).Cast<ShaderGraphVersion>().Select(
                version => MigrationStep.New(version, (HDSubTarget t) => t.MigrateTo(version))
            ).ToArray()
        );

        /// <summary>
        /// Override this method to handle migration in inherited subtargets
        /// </summary>
        /// <param name="version">The current version of the migration</param>
        internal virtual void MigrateTo(ShaderGraphVersion version)
        {
        }

        public override void Setup(ref TargetSetupContext context)
        {
            context.AddAssetDependencyPath(AssetDatabase.GUIDToAssetPath("c09e6e9062cbd5a48900c48a0c2ed1c2")); // HDSubTarget.cs
            context.AddAssetDependencyPath(AssetDatabase.GUIDToAssetPath(subTargetAssetGuid));
            context.SetDefaultShaderGUI(customInspector);

            if (migrationSteps.Migrate(this))
                OnBeforeSerialize();

            foreach (var subShader in EnumerateSubShaders())
            {
                // patch render type and render queue from pass declaration:
                var patchedSubShader = subShader;
                patchedSubShader.renderType = renderType;
                patchedSubShader.renderQueue = renderQueue;
                context.AddSubShader(patchedSubShader);
            }
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            // Common properties between all HD master nodes
            // Dots
            context.AddField(HDFields.DotsInstancing,      systemData.dotsInstancing);
        }

        protected abstract IEnumerable<SubShaderDescriptor> EnumerateSubShaders();

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo)
        {
            var gui = new SubTargetPropertiesGUI(context, onChange, registerUndo, systemData, null, null);
            AddInspectorPropertyBlocks(gui);
            context.Add(gui);
        }

        protected abstract void AddInspectorPropertyBlocks(SubTargetPropertiesGUI blockList);

        public override object saveContext
        {
            get
            {
                int hash = ComputeMaterialNeedsUpdateHash();
                bool needsUpdate = hash != systemData.materialNeedsUpdateHash;
                if (needsUpdate)
                    systemData.materialNeedsUpdateHash = hash;

                return new HDSaveContext{ updateMaterials = needsUpdate };
            }
        } 
    }
}
