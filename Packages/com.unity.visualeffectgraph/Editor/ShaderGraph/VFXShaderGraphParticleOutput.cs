using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

namespace UnityEditor.VFX
{
    class VFXShaderGraphParticleOutput : VFXAbstractParticleOutput, IVFXShaderGraphOutput
    {
        //"protected" is only to be listed by VFXModel.GetSettings, we should always use GetOrRefreshShaderGraphObject
        [SerializeField, VFXSetting]
        protected ShaderGraphVfxAsset shaderGraph;

        [SerializeField, VFXSetting]
        internal VFXMaterialSerializedSettings materialSettings = new VFXMaterialSerializedSettings();

        private bool m_IsShaderGraphMissing;

        protected override IEnumerable<string> untransferableSettings
        {
            get
            {
                //In case of convert from VFXComposedParticleOutput, we won't retrieve shaderGraph anymore, this settings is now hidden
                yield return nameof(shaderGraph);
            }
        }

        public ShaderGraphVfxAsset GetOrRefreshShaderGraphObject(bool refreshErrors = true)
        {
            var wasShaderGraphMissing = m_IsShaderGraphMissing;
            //This is the only place where shaderGraph property is updated or read
            if (shaderGraph == null && !object.ReferenceEquals(shaderGraph, null))
            {
                var assetPath = AssetDatabase.GetAssetPath(shaderGraph.GetInstanceID());

                var newShaderGraph = AssetDatabase.LoadAssetAtPath<ShaderGraphVfxAsset>(assetPath);
                m_IsShaderGraphMissing = newShaderGraph == null;

                if (!m_IsShaderGraphMissing)
                {
                    shaderGraph = newShaderGraph;
                }
            }
            else
            {
                m_IsShaderGraphMissing = false;
            }

            if (refreshErrors && wasShaderGraphMissing != m_IsShaderGraphMissing)
            {
                RefreshErrors();
            }

            return shaderGraph;
        }

        public override void Sanitize(int version)
        {
            base.Sanitize(version);
            if (version < 14)
            {
                var shaderGraph = GetOrRefreshShaderGraphObject();
                if (shaderGraph && shaderGraph.generatesWithShaderGraph)
                {
                    var path = AssetDatabase.GetAssetPath(shaderGraph);
                    var referenceMaterial = AssetDatabase.LoadAssetAtPath<Material>(path);
                    materialSettings.UpgradeToMaterialWorkflowVersion(referenceMaterial);
                }
            }

            if (version < 15)
            {
                SanitizeHelper.MigrateSGOutputToComposed(this);
            }
        }

        public override bool CanBeCompiled()
        {
            if (m_IsShaderGraphMissing)
                return false;

            var sg = GetOrRefreshShaderGraphObject();
            if (sg != null && sg.generatesWithShaderGraph)
                return false;

            return base.CanBeCompiled();
        }

        public override void OnSettingModified(VFXSetting setting)
        {
            if (setting.name == nameof(shaderGraph))
            {
                VFXAnalytics.GetInstance().OnSpecificSettingChanged($"{GetType().Name}.{setting.name}");
            }
        }

        public override void GetImportDependentAssets(HashSet<int> dependencies)
        {
            base.GetImportDependentAssets(dependencies);
            if (!object.ReferenceEquals(shaderGraph, null))
            {
                dependencies.Add(shaderGraph.GetInstanceID());
            }
        }

        protected VFXShaderGraphParticleOutput(bool strip = false) : base(strip) { }

        // Here we maintain a list of settings that we do not need if we are using the ShaderGraph generation path (it will be in the material inspector).
        static IEnumerable<string> FilterOutBuiltinSettings()
        {
            yield return "blendMode";
            yield return "cullMode";
            yield return "zWriteMode";
            yield return "zTestMode";
            yield return "excludeFromTUAndAA";
            yield return "preserveSpecularLighting";
            yield return "doubleSided";
            yield return "onlyAmbientLighting";
            yield return "useExposureWeight";
            yield return "alphaThreshold";
            yield return "normalBending";
        }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                foreach (var setting in base.filteredOutSettings)
                    yield return setting;

                var sg = GetOrRefreshShaderGraphObject();
                if (sg != null || m_IsShaderGraphMissing)
                {
                    yield return "colorMapping";
                    yield return "useAlphaClipping";

                    if (m_IsShaderGraphMissing)
                    {
                        yield return "useSoftParticle";
                        yield return "uvMode";

                        foreach (var builtinSetting in FilterOutBuiltinSettings())
                            yield return builtinSetting;
                    }
                }
                else
                {
                    yield return nameof(shaderGraph);
                }

                yield return nameof(materialSettings);
            }
        }

        public override bool supportsUV => base.supportsUV && GetOrRefreshShaderGraphObject() == null;
        public override bool exposeAlphaThreshold
        {
            get
            {
                var shaderGraph = GetOrRefreshShaderGraphObject();
                if (shaderGraph == null)
                {
                    if (base.exposeAlphaThreshold)
                        return true;
                }
                else
                {
                    if (!shaderGraph.alphaClipping)
                    {
                        //alpha clipping isn't enabled in shaderGraph, we implicitly still allows clipping for shadow & motion vector passes.
                        if (!isBlendModeOpaque && (hasMotionVector || hasShadowCasting))
                            return true;
                    }
                }
                return false;
            }
        }
        public override bool supportSoftParticles => base.supportSoftParticles && GetOrRefreshShaderGraphObject() == null;
        public override bool hasAlphaClipping
        {
            get
            {
                var shaderGraph = GetOrRefreshShaderGraphObject();
                bool noShaderGraphAlphaThreshold = shaderGraph == null && useAlphaClipping;
                bool ShaderGraphAlphaThreshold = shaderGraph != null && shaderGraph.alphaClipping;
                return noShaderGraphAlphaThreshold || ShaderGraphAlphaThreshold;
            }
        }

        // Do not resync slots when shader graph is missing to keep potential links to the shader properties
        public override bool ResyncSlots(bool notify) => !m_IsShaderGraphMissing && base.ResyncSlots(notify);

        public override void CheckGraphBeforeImport()
        {
            base.CheckGraphBeforeImport();
            var currentShaderGraph = GetOrRefreshShaderGraphObject();

            // If the graph is reimported it can be because one of its dependency such as the shadergraphs, has been changed.
            if (!VFXGraph.explicitCompile)
            {
                ResyncSlots(true);

                // Ensure that the output context name is in sync with the shader graph shader enum name.
                if (currentShaderGraph != null && currentShaderGraph.generatesWithShaderGraph)
                    Invalidate(InvalidationCause.kUIChangedTransient);

                else if (m_IsShaderGraphMissing)
                {
                    var vfxName = GetGraph().visualEffectResource.name;
                    Debug.LogError($"The VFX Graph '{vfxName}'" + VFXShaderGraphHelpers.GetMissingShaderGraphErrorMessage(currentShaderGraph));
                }
            }
        }

        internal override void GenerateErrors(VFXErrorReporter report)
        {
            base.GenerateErrors(report);

            var currentShaderGraph = GetOrRefreshShaderGraphObject(false);
            if (m_IsShaderGraphMissing)
            {
                var message = VFXShaderGraphHelpers.GetMissingShaderGraphErrorMessage(currentShaderGraph);
                report.RegisterError("ErrorMissingShaderGraph", VFXErrorType.Error, "The VFX Graph" + message, this);
            }

            if (currentShaderGraph != null)
            {
                if (!currentShaderGraph.generatesWithShaderGraph)
                {
                    report.RegisterError("DeprecatedOldShaderGraph", VFXErrorType.Error, ParticleShadingShaderGraph.kErrorOldSG, this);
                }
                else
                {
                    //There isn't automatic sanitize if the SG change its status from old to new SG integration
                    report.RegisterError("WrongOutputShaderGraph", VFXErrorType.Error, "Please convert this context to dedicated ShaderGraph Output.", this);
                }
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                IEnumerable<VFXPropertyWithValue> properties = base.inputProperties;
                var sg = GetOrRefreshShaderGraphObject();
                if (sg != null)
                {
                    //This path is only used with old shader graph integration. It doesn't support keyword.
                    var shaderGraphProperties = VFXShaderGraphHelpers.GetProperties(sg).Where(o => o.keywordsMapping == null).Select(o => o.property);
                    properties = properties.Concat(shaderGraphProperties);
                }
                return properties;
            }
        }

        protected override IEnumerable<VFXNamedExpression> CollectGPUExpressions(IEnumerable<VFXNamedExpression> slotExpressions)
        {
            foreach (var exp in base.CollectGPUExpressions(slotExpressions))
                yield return exp;

            var shaderGraph = GetOrRefreshShaderGraphObject();
            if (shaderGraph != null)
            {
                foreach (var sgProperty in shaderGraph.properties)
                {
                    if (inputSlots.Any(t => t.property.name == sgProperty.referenceName))
                        yield return slotExpressions.First(o => o.name == sgProperty.referenceName);
                }
            }
        }

        protected virtual VFXOldShaderGraphHelpers.RPInfo currentRP
        {
            get { return VFXOldShaderGraphHelpers.hdrpInfo; }
        }

        public override IEnumerable<string> additionalDefines
        {
            get
            {
                foreach (var def in base.additionalDefines)
                    yield return def;

                var shaderGraph = GetOrRefreshShaderGraphObject();
                if (shaderGraph != null && !shaderGraph.generatesWithShaderGraph)
                {
                    foreach (var def in VFXOldShaderGraphHelpers.GetAdditionalDefinesGetAdditionalReplacement(shaderGraph, currentRP, graphCodes, taskType == VFXTaskType.ParticleMeshOutput))
                        yield return def;
                }
            }
        }

        public override VFXExpressionMapper GetExpressionMapper(VFXDeviceTarget target)
        {
            var mapper = base.GetExpressionMapper(target);
            var shaderGraph = GetOrRefreshShaderGraphObject();

            switch (target)
            {
                case VFXDeviceTarget.CPU:
                {
                }
                break;
                case VFXDeviceTarget.GPU:
                    if (shaderGraph != null)
                    {
                        foreach (var texture in VFXShaderGraphHelpers.GetTextureConstant(shaderGraph))
                            mapper.AddExpression(texture, -1);
                    }
                    break;
            }

            return mapper;
        }

        public virtual bool isLitShader { get => false; }

        Dictionary<string, GraphCode> graphCodes;

        public override bool SetupCompilation()
        {
            if (!base.SetupCompilation())
                return false;

            var shaderGraph = GetOrRefreshShaderGraphObject();
            if (shaderGraph != null && !shaderGraph.generatesWithShaderGraph)
            {
                graphCodes = VFXOldShaderGraphHelpers.BuildGraphCode(shaderGraph, currentRP, isLitShader);
                return graphCodes != null;
            }

            return true;
        }

        public override void EndCompilation()
        {
            graphCodes = null;
        }

        public override IEnumerable<KeyValuePair<string, VFXShaderWriter>> additionalReplacements
        {
            get
            {
                foreach (var rep in base.additionalReplacements)
                    yield return rep;

                var shaderGraph = GetOrRefreshShaderGraphObject();
                if (shaderGraph != null && !shaderGraph.generatesWithShaderGraph)
                {
                    foreach (var def in VFXOldShaderGraphHelpers.GetAdditionalReplacement(shaderGraph, currentRP, graphCodes, taskType == VFXTaskType.ParticleMeshOutput))
                        yield return def;
                }
            }
        }

        public ShaderGraphVfxAsset GetShaderGraph()
        {
            var shaderGraph = GetOrRefreshShaderGraphObject();
            return shaderGraph;
        }
    }
}
