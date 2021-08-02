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
        protected bool m_MigrateFromOldSG; // Use only for the migration from early shader stack architecture to recent one

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

        protected virtual int ComputeMaterialNeedsUpdateHash() => 0;

        public override bool IsActive() => true;

        protected abstract ShaderID shaderID { get; }
        protected abstract string customInspector { get; }
        protected abstract GUID subTargetAssetGuid { get; }
        protected abstract string renderType { get; }
        protected abstract string renderQueue { get; }
        protected abstract string templatePath { get; }
        protected abstract string[] templateMaterialDirectories { get; }
        protected abstract FieldDescriptor subShaderField { get; }
        protected abstract string subShaderInclude { get; }

        protected virtual string postDecalsInclude => null;
        protected virtual string raytracingInclude => null;
        protected virtual string pathtracingInclude => null;
        protected virtual bool supportPathtracing => false;
        protected virtual bool supportRaytracing => false;

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

        static readonly GUID kSourceCodeGuid = new GUID("c09e6e9062cbd5a48900c48a0c2ed1c2");  // HDSubTarget.cs

        public override void Setup(ref TargetSetupContext context)
        {
            systemData.materialNeedsUpdateHash = ComputeMaterialNeedsUpdateHash();
            context.AddAssetDependency(kSourceCodeGuid, AssetCollection.Flags.SourceDependency);
            context.AddAssetDependency(subTargetAssetGuid, AssetCollection.Flags.SourceDependency);
            context.SetDefaultShaderGUI(customInspector);

            if (migrationSteps.Migrate(this))
                OnBeforeSerialize();

            // Migration hack to have the case where SG doesn't have version yet but is already upgraded to the stack system
            if (!systemData.firstTimeMigrationExecuted)
            {
                // Force the initial migration step
                MigrateTo(ShaderGraphVersion.FirstTimeMigration);
                systemData.firstTimeMigrationExecuted = true;
                OnBeforeSerialize();
                systemData.materialNeedsUpdateHash = ComputeMaterialNeedsUpdateHash();
            }

            foreach (var subShader in EnumerateSubShaders())
            {
                // patch render type and render queue from pass declaration:
                var patchedSubShader = subShader;
                patchedSubShader.renderType = renderType;
                patchedSubShader.renderQueue = renderQueue;
                context.AddSubShader(patchedSubShader);
            }
        }

        protected SubShaderDescriptor PostProcessSubShader(SubShaderDescriptor subShaderDescriptor)
        {
            if (String.IsNullOrEmpty(subShaderDescriptor.pipelineTag))
                subShaderDescriptor.pipelineTag = HDRenderPipeline.k_ShaderTagName;
            
            var passes = subShaderDescriptor.passes.ToArray();
            PassCollection finalPasses = new PassCollection();
            for (int i = 0; i < passes.Length; i++)
            {
                var passDescriptor = passes[i].descriptor;
                passDescriptor.passTemplatePath = templatePath;
                passDescriptor.sharedTemplateDirectories = templateMaterialDirectories;

                // Add the subShader to enable fields that depends on it
                var originalRequireFields = passDescriptor.requiredFields;
                // Duplicate require fields to avoid unwanted shared list modification
                passDescriptor.requiredFields = new FieldCollection();
                if (originalRequireFields != null)
                    foreach (var field in originalRequireFields)
                        passDescriptor.requiredFields.Add(field.field);
                passDescriptor.requiredFields.Add(subShaderField);

                IncludeCollection finalIncludes = new IncludeCollection();
                var includeList = passDescriptor.includes.Select(include => include.descriptor).ToList();

                // Replace include placeholders if necessary:
                foreach (var include in passDescriptor.includes)
                {
                    if (include.descriptor.value == CoreIncludes.kPassPlaceholder)
                        include.descriptor.value = subShaderInclude;
                    if (include.descriptor.value == CoreIncludes.kPostDecalsPlaceholder)
                        include.descriptor.value = postDecalsInclude;
                    if (include.descriptor.value == CoreIncludes.kRaytracingPlaceholder)
                        include.descriptor.value = raytracingInclude;
                    if (include.descriptor.value == CoreIncludes.kPathtracingPlaceholder)
                        include.descriptor.value = pathtracingInclude;

                    if (!String.IsNullOrEmpty(include.descriptor.value))
                        finalIncludes.Add(include.descriptor.value, include.descriptor.location, include.fieldConditions);
                }
                passDescriptor.includes = finalIncludes;

                // Replace valid pixel blocks by automatic thing so we don't have to write them
                var tmpCtx = new TargetActiveBlockContext(new List<BlockFieldDescriptor>(), passDescriptor);
                GetActiveBlocks(ref tmpCtx);
                if (passDescriptor.validPixelBlocks == null)
                    passDescriptor.validPixelBlocks = tmpCtx.activeBlocks.Where(b => b.shaderStage == ShaderStage.Fragment).ToArray();
                if (passDescriptor.validVertexBlocks == null)
                    passDescriptor.validVertexBlocks = tmpCtx.activeBlocks.Where(b => b.shaderStage == ShaderStage.Vertex).ToArray();

                // Add keywords from subshaders:
                passDescriptor.keywords = passDescriptor.keywords == null ? new KeywordCollection() : new KeywordCollection{ passDescriptor.keywords }; // Duplicate keywords to avoid side effects (static list modification)
                passDescriptor.defines = passDescriptor.defines == null ? new DefineCollection() : new DefineCollection{ passDescriptor.defines }; // Duplicate defines to avoid side effects (static list modification)
                CollectPassKeywords(ref passDescriptor);

                // Set default values for HDRP "surface" passes:
                if (passDescriptor.structs == null)
                    passDescriptor.structs = CoreStructCollections.Default;
                if (passDescriptor.fieldDependencies == null)
                    passDescriptor.fieldDependencies = CoreFieldDependencies.Default;

                finalPasses.Add(passDescriptor, passes[i].fieldConditions);
            }

            subShaderDescriptor.passes = finalPasses;

            return subShaderDescriptor;
        }

        protected virtual void CollectPassKeywords(ref PassDescriptor pass) {}

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
