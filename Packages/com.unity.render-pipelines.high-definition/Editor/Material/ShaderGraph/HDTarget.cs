using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UIElements;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.UIElements;
using UnityEditor.ShaderGraph.Serialization;
using UnityEditor.ShaderGraph.Legacy;
using UnityEditor.VFX;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    enum DistortionMode
    {
        Add,
        Multiply,
        Replace
    }

    enum DoubleSidedMode
    {
        Disabled,
        Enabled,
        FlippedNormals,
        MirroredNormals,
    }

    enum DoubleSidedGIMode
    {
        MatchMaterial,
        ForceOn,
        ForceOff,
    }

    enum SpecularOcclusionMode
    {
        Off,
        FromAO,
        FromAOAndBentNormal,
        Custom
    }

    sealed class HDTarget : Target, IHasMetadata, ILegacyTarget, IMaySupportVFX, IRequireVFXContext
    {
        // Constants
        static readonly GUID kSourceCodeGuid = new GUID("61d9843d4027e3e4a924953135f76f3c"); // HDTarget.cs

        // SubTarget
        List<SubTarget> m_SubTargets;
        List<string> m_SubTargetNames;
        int activeSubTargetIndex => m_SubTargets.IndexOf(m_ActiveSubTarget);

        // View
        PopupField<string> m_SubTargetField;
        TextField m_CustomGUIField;
        Toggle m_SupportVFXToggle;
        Toggle m_SupportLineRenderingToggle;

        [SerializeField]
        JsonData<SubTarget> m_ActiveSubTarget;

        public SubTarget activeSubTarget
        {
            get => m_ActiveSubTarget.value;
            set => m_ActiveSubTarget = value;
        }

        public bool supportLineRendering
        {
            get => m_SupportLineRendering;
        }

        [SerializeField]
        List<JsonData<JsonObject>> m_Datas = new List<JsonData<JsonObject>>();

        [SerializeField]
        string m_CustomEditorGUI;

        [SerializeField]
        bool m_SupportVFX;

        [SerializeField]
        bool m_SupportLineRendering;

        private static readonly List<Type> m_IncompatibleVFXSubTargets = new()
        {
            // Currently there is not support for VFX decals via HDRP master node.
            typeof(DecalSubTarget),
            typeof(HDCanvasSubTarget),
            typeof(HDFullscreenSubTarget),
            typeof(WaterSubTarget),
            typeof(WaterDecalSubTarget),
            typeof(FogVolumeSubTarget),
            typeof(PBRSkySubTarget),
        };

        private static readonly List<Type> m_IncompatibleHQLineRenderingSubTargets = new()
        {
            typeof(DecalSubTarget),
            typeof(HDCanvasSubTarget),
            typeof(HDFullscreenSubTarget),
            typeof(WaterSubTarget),
            typeof(WaterDecalSubTarget),
            typeof(FogVolumeSubTarget),
            typeof(PBRSkySubTarget),
        };

        internal override bool ignoreCustomInterpolators => m_ActiveSubTarget.value is HDCanvasSubTarget;
        internal override int padCustomInterpolatorLimit => 8;

        public override bool IsNodeAllowedByTarget(Type nodeType)
        {
            SRPFilterAttribute srpFilter = NodeClassCache.GetAttributeOnNodeType<SRPFilterAttribute>(nodeType);
            bool worksWithThisSrp = srpFilter == null || srpFilter.srpTypes.Contains(typeof(HDRenderPipeline));

            SubTargetFilterAttribute subTargetFilter = NodeClassCache.GetAttributeOnNodeType<SubTargetFilterAttribute>(nodeType);
            bool worksWithThisSubTarget = subTargetFilter == null || subTargetFilter.subTargetTypes.Contains(activeSubTarget.GetType());

            if (activeSubTarget.IsActive())
                worksWithThisSubTarget &= activeSubTarget.IsNodeAllowedBySubTarget(nodeType);

            return worksWithThisSrp && worksWithThisSubTarget && base.IsNodeAllowedByTarget(nodeType);
        }

        public HDTarget()
        {
            displayName = "HDRP";
            m_SubTargets = TargetUtils.GetSubTargets(this);
            m_SubTargetNames = m_SubTargets.Select(x => x.displayName).ToList();

            TargetUtils.ProcessSubTargetList(ref m_ActiveSubTarget, ref m_SubTargets);
            ProcessSubTargetDatas(m_ActiveSubTarget.value);
        }

        public string customEditorGUI
        {
            get => m_CustomEditorGUI;
            set => m_CustomEditorGUI = value;
        }

        public override bool IsActive()
        {
            if (m_ActiveSubTarget.value == null)
                return false;

            bool isHDRenderPipeline = GraphicsSettings.currentRenderPipeline is HDRenderPipelineAsset;
            return isHDRenderPipeline && m_ActiveSubTarget.value.IsActive();
        }

        public override void Setup(ref TargetSetupContext context)
        {
            // Setup the Target
            context.AddAssetDependency(kSourceCodeGuid, AssetCollection.Flags.SourceDependency);

            // Process SubTargets
            TargetUtils.ProcessSubTargetList(ref m_ActiveSubTarget, ref m_SubTargets);
            if (m_ActiveSubTarget.value == null)
                return;

            // Override EditorGUI (replaces the HDRP material editor by a custom one)
            if (!string.IsNullOrEmpty(m_CustomEditorGUI))
                context.AddCustomEditorForRenderPipeline(m_CustomEditorGUI, typeof(HDRenderPipelineAsset));

            // Setup the active SubTarget
            ProcessSubTargetDatas(m_ActiveSubTarget.value);
            m_ActiveSubTarget.value.target = this;
            m_ActiveSubTarget.value.Setup(ref context);
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            var descs = context.blocks.Select(x => x.descriptor);
            // Stages
            if (!context.pass.IsRaytracing()) // Don't handle vertex shader when using raytracing
            {
                context.AddField(Fields.GraphVertex, descs.Contains(BlockFields.VertexDescription.Position) ||
                    descs.Contains(BlockFields.VertexDescription.Normal) ||
                    descs.Contains(BlockFields.VertexDescription.Tangent));
            }

            context.AddField(Fields.GraphPixel);

            // SubTarget
            m_ActiveSubTarget.value.GetFields(ref context);
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            // SubTarget
            m_ActiveSubTarget.value.GetActiveBlocks(ref context);
        }

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo)
        {
            if (m_ActiveSubTarget.value == null)
                return;

            // Core properties
            var graphValidation = context.graphValidation;
            m_SubTargetField = new PopupField<string>(m_SubTargetNames, activeSubTargetIndex);
            context.AddProperty("Material", m_SubTargetField, (evt) =>
            {
                if (Equals(activeSubTargetIndex, m_SubTargetField.index))
                    return;

                var systemData = m_Datas.SelectValue().FirstOrDefault(x => x is SystemData) as SystemData;
                if (systemData != null)
                {
                    // Force material update hash
                    systemData.materialNeedsUpdateHash = -1;
                }

                m_ActiveSubTarget = m_SubTargets[m_SubTargetField.index];
                ProcessSubTargetDatas(m_ActiveSubTarget.value);
                onChange();
                graphValidation();
            });

            // SubTarget properties
            m_ActiveSubTarget.value.GetPropertiesGUI(ref context, onChange, registerUndo);

            // Custom Editor GUI
            m_CustomGUIField = new TextField("") { value = m_CustomEditorGUI };
            m_CustomGUIField.RegisterCallback<FocusOutEvent>(s =>
            {
                if (Equals(m_CustomEditorGUI, m_CustomGUIField.value))
                    return;

                m_CustomEditorGUI = m_CustomGUIField.value;
                onChange();
            });
            context.AddProperty("Custom Editor GUI", m_CustomGUIField, (evt) => { });

            // VFX Support
            if (!m_IncompatibleVFXSubTargets.Contains(m_ActiveSubTarget.value.GetType()))
            {
                m_SupportVFXToggle = new Toggle("") { value = m_SupportVFX };
                const string k_VFXToggleTooltip = "When enabled, this shader can be assigned to a compatible Visual Effect Graph output.";
                context.AddProperty("Support VFX Graph", k_VFXToggleTooltip, 0, m_SupportVFXToggle, (evt) =>
                {
					registerUndo("Change Support VFX Graph");
                    m_SupportVFX = m_SupportVFXToggle.value;
                    onChange();
                });
            }

            if (!m_IncompatibleHQLineRenderingSubTargets.Contains(m_ActiveSubTarget.value.GetType()))
            {
                m_SupportLineRenderingToggle = new Toggle("") { value = m_SupportLineRendering };
                context.AddProperty("Support High Quality Line Rendering", "", 0, m_SupportLineRenderingToggle, (evt) =>
                {
					registerUndo("Change Support High Quality Line Rendering");
                    m_SupportLineRendering = m_SupportLineRenderingToggle.value;
                    onChange();
                });
            }
        }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            // SubTarget
            m_ActiveSubTarget.value.CollectShaderProperties(collector, generationMode);

            collector.AddShaderProperty(LightmappingShaderProperties.kLightmapsArray);
            collector.AddShaderProperty(LightmappingShaderProperties.kLightmapsIndirectionArray);
            collector.AddShaderProperty(LightmappingShaderProperties.kShadowMasksArray);
        }

        public override void ProcessPreviewMaterial(Material material)
        {
            // SubTarget
            m_ActiveSubTarget.value.ProcessPreviewMaterial(material);
        }

        public override object saveContext => m_ActiveSubTarget.value?.saveContext;

        // IHasMetaData
        public string identifier
        {
            get
            {
                if (m_ActiveSubTarget.value is IHasMetadata subTargetHasMetaData)
                    return subTargetHasMetaData.identifier;

                return null;
            }
        }

        public ScriptableObject GetMetadataObject(GraphDataReadOnly graph)
        {
            if (m_ActiveSubTarget.value is IHasMetadata subTargetHasMetaData)
                return subTargetHasMetaData.GetMetadataObject(graph);

            return null;
        }

        public bool TrySetActiveSubTarget(Type subTargetType)
        {
            if (!subTargetType.IsSubclassOf(typeof(SubTarget)))
                return false;

            foreach (var subTarget in m_SubTargets)
            {
                if (subTarget.GetType().Equals(subTargetType))
                {
                    m_ActiveSubTarget = subTarget;
                    ProcessSubTargetDatas(m_ActiveSubTarget);
                    return true;
                }
            }

            return false;
        }

        void ProcessSubTargetDatas(SubTarget subTarget)
        {
            var typeCollection = TypeCache.GetTypesDerivedFrom<JsonObject>();
            foreach (var type in typeCollection)
            {
                if (type.IsGenericType)
                    continue;

                // Data requirement interfaces need generic type arguments
                // Therefore we need to use reflections to call the method
                var methodInfo = typeof(HDTarget).GetMethod(nameof(SetDataOnSubTarget));
                var genericMethodInfo = methodInfo.MakeGenericMethod(type);
                genericMethodInfo.Invoke(this, new object[] { subTarget });
            }
        }

        void ClearUnusedData()
        {
            for (int i = 0; i < m_Datas.Count; i++)
            {
                var data = m_Datas[i];
                var type = data.value.GetType();

                // Data requirement interfaces need generic type arguments
                // Therefore we need to use reflections to call the method
                var methodInfo = typeof(HDTarget).GetMethod(nameof(ValidateDataForSubTarget));
                var genericMethodInfo = methodInfo.MakeGenericMethod(type);
                genericMethodInfo.Invoke(this, new object[] { m_ActiveSubTarget.value, data.value });
            }
        }

        public void SetDataOnSubTarget<T>(SubTarget subTarget) where T : JsonObject
        {
            if (!(subTarget is IRequiresData<T> requiresData))
                return;

            // Ensure data object exists in list
            var data = m_Datas.SelectValue().FirstOrDefault(x => x.GetType().Equals(typeof(T))) as T;
            if (data == null)
            {
                data = Activator.CreateInstance(typeof(T)) as T;
                m_Datas.Add(data);
            }

            // Apply data object to SubTarget
            requiresData.data = data;
        }

        public void ValidateDataForSubTarget<T>(SubTarget subTarget, T data) where T : JsonObject
        {
            if (!(subTarget is IRequiresData<T> requiresData))
            {
                m_Datas.Remove(data);
            }
        }

        public override void OnBeforeSerialize()
        {
            ClearUnusedData();
        }

        public bool TryUpgradeFromMasterNode(IMasterNode1 masterNode, out Dictionary<BlockFieldDescriptor, int> blockMap)
        {
            blockMap = null;

            // We need to guarantee any required data object exists
            // as we fill out the datas in the same method as determining which SubTarget is valid
            // When the graph is serialized any unused data is removed anyway
            var typeCollection = TypeCache.GetTypesDerivedFrom<HDTargetData>();
            foreach (var type in typeCollection)
            {
                var data = Activator.CreateInstance(type) as HDTargetData;
                m_Datas.Add(data);
            }

            // Process SubTargets
            foreach (var subTarget in m_SubTargets)
            {
                if (!(subTarget is ILegacyTarget legacySubTarget))
                    continue;

                // Ensure all SubTargets have any required data to fill out during upgrade
                ProcessSubTargetDatas(subTarget);
                subTarget.target = this;

                if (legacySubTarget.TryUpgradeFromMasterNode(masterNode, out blockMap))
                {
                    m_ActiveSubTarget = subTarget;
                    return true;
                }
            }

            return false;
        }

        public override bool WorksWithSRP(RenderPipelineAsset scriptableRenderPipeline)
        {
            return scriptableRenderPipeline?.GetType() == typeof(HDRenderPipelineAsset);
        }

        public bool CanSupportVFX()
        {
            if (m_ActiveSubTarget.value == null)
                return false;

            if (m_IncompatibleVFXSubTargets.Contains(m_ActiveSubTarget.value.GetType()))
                return false;

            return true;
        }

        public bool SupportsVFX()
        {
            if (CanSupportVFX())
                return m_SupportVFX;
            return false;
        }

        public void ConfigureContextData(VFXContext context, VFXTaskCompiledData data)
        {
            if (!(m_ActiveSubTarget.value is IRequireVFXContext vfxSubtarget))
                return;

            vfxSubtarget.ConfigureContextData(context, data);
        }

        public override bool DerivativeModificationCallback(
                out string dstGraphFunctions,
                out string dstGraphPixel,
                out bool[] adjustedUvDerivs,
                string primaryShaderName,
                string passName,
                string propStr,
                string surfaceDescStr,
                string graphFuncStr,
                string graphPixelStr,
                List<string> customFuncs,
                bool applyEmulatedDerivatives)
        {
            //List<string> ignoredFuncs = new List<string>();
            //ignoredFuncs.Add("Unity_Checkerboard_float");
            //ignoredFuncs.Add("Unity_Checkerboard_half");

            bool success = false;

            dstGraphFunctions = "";
            dstGraphPixel = "";
            adjustedUvDerivs = new bool[4];

            HlslProcessor hlslProc = new HlslProcessor(4);

            try
            {
                hlslProc.ProcessFunctions(propStr, surfaceDescStr, graphFuncStr, graphPixelStr, applyEmulatedDerivatives, customFuncs, primaryShaderName);

                if (hlslProc.isValid)
                {
                    dstGraphFunctions = hlslProc.dstGraphFunctions;
                    dstGraphPixel = hlslProc.dstGraphPixel;

                    System.Array.Copy(hlslProc.adjustedUvDerivs, adjustedUvDerivs, adjustedUvDerivs.Length);

                    success = true;
                }
                else
                {
                    string warnText = "derivative parsing failed: " + primaryShaderName;
                    Debug.LogWarning(warnText);
                    hlslProc.debugLog += warnText + "\n";
                }
            }
            catch (Exception e)
            {
                string warnText = e.Message + " (derivative exception caught: " + primaryShaderName + ")";
                Debug.LogWarning(warnText);
                hlslProc.debugLog += warnText + "\n";
                hlslProc.debugLog += e.StackTrace;

                // also, add the generated node stack if we have one
                hlslProc.debugLog += "\n";
                hlslProc.debugLog += "NodeStack: " + hlslProc.debugNodeStack.Count.ToString() + "\n";
                for (int i = 0; i < hlslProc.debugNodeStack.Count; i++)
                {
                    int nodeId = hlslProc.debugNodeStack[i];
                    hlslProc.debugLog += "    " + nodeId.ToString() + "\n";
                }

                success = false;
            }
            var isDebugOutputNeed =
                GraphicsSettings.TryGetRenderPipelineSettings<AnalyticDerivativeSettings>(
                    out var analyticDerivativeSettings) && analyticDerivativeSettings.debugOutput;

            if (isDebugOutputNeed)
            {
                string cleanName = primaryShaderName.Replace("/", "_").Replace("\\", "_"); ;
                string dstDebugBasePath = "Temp/ShaderDerivative_" + cleanName + "__" + passName + "__";

                string origName = dstDebugBasePath + "00_original.txt";
                string tokenizedName = dstDebugBasePath + "01_tokenized.txt";
                string parsedName = dstDebugBasePath + "02_parsed.txt";
                string reconstructName = dstDebugBasePath + "03_reconstruct.txt";
                string nodeName = dstDebugBasePath + "04_node.txt";
                string genName = dstDebugBasePath + "05_generated.txt";
                string logName = dstDebugBasePath + "06_log.txt";

                System.IO.File.WriteAllText(origName, hlslProc.debugTextInput);
                System.IO.File.WriteAllText(tokenizedName, hlslProc.debugTokenizerInfo);
                System.IO.File.WriteAllText(parsedName, hlslProc.debugParserTree);
                System.IO.File.WriteAllText(reconstructName, hlslProc.debugDirectReconstruction);
                System.IO.File.WriteAllText(nodeName, hlslProc.debugNodeInfo);
                System.IO.File.WriteAllText(genName, hlslProc.debugTextOutput);
                System.IO.File.WriteAllText(logName, hlslProc.debugLog);
            }

            return success;
        }

    }





    #region BlockMasks
    static class CoreBlockMasks
    {
        public static BlockFieldDescriptor[] Vertex = new BlockFieldDescriptor[]
        {
            BlockFields.VertexDescription.Position,
            BlockFields.VertexDescription.Normal,
            BlockFields.VertexDescription.Tangent,
        };
    }
    #endregion

    #region StructCollections
    static class CoreStructCollections
    {
        public static StructCollection BasicProcedural = new StructCollection
        {
            { HDStructs.AttributesMeshProcedural },
            { HDStructs.VaryingsMeshToPS },
            { HDStructs.VertexDescriptionInputsProcedural },
            { Structs.SurfaceDescriptionInputs },
        };

        public static StructCollection Basic = new StructCollection
        {
            { HDStructs.AttributesMesh },
            { HDStructs.VaryingsMeshToPS },
            { Structs.VertexDescriptionInputs },
            { Structs.SurfaceDescriptionInputs },
        };

        // VFX have its own structure define in PostProcessSubShader that replace existing one

        // Will be append on top of Default if tessellation is enabled
        public static StructCollection BasicTessellation = new StructCollection
        {
            { HDStructs.AttributesMesh },
            { HDStructs.VaryingsMeshToDS },
            { HDStructs.VaryingsMeshToPS },
            { Structs.VertexDescriptionInputs },
            { Structs.SurfaceDescriptionInputs },
        };

        public static StructCollection BasicProceduralTessellation = new StructCollection
        {
            { HDStructs.AttributesMeshProcedural },
            { HDStructs.VaryingsMeshToDS },
            { HDStructs.VaryingsMeshToPS },
            { HDStructs.VertexDescriptionInputsProcedural },
            { Structs.SurfaceDescriptionInputs },
        };

        public static StructCollection BasicRaytracing = new StructCollection
        {
            { Structs.SurfaceDescriptionInputs },
        };
    }
    #endregion

    #region FieldDependencies
    static class CoreFieldDependencies
    {
        public static DependencyCollection Varying = new DependencyCollection
        {
            //Standard Varying Dependencies
            new FieldDependency(HDStructFields.VaryingsMeshToPS.positionRWS,                                        HDStructFields.AttributesMesh.positionOS),
            new FieldDependency(HDStructFields.VaryingsMeshToPS.positionPredisplacementRWS,                         HDStructFields.AttributesMesh.positionOS),
            new FieldDependency(HDStructFields.VaryingsMeshToPS.normalWS,                                           HDStructFields.AttributesMesh.normalOS),
            new FieldDependency(HDStructFields.VaryingsMeshToPS.tangentWS,                                          HDStructFields.AttributesMesh.tangentOS),
            new FieldDependency(HDStructFields.VaryingsMeshToPS.texCoord0,                                          HDStructFields.AttributesMesh.uv0),
            new FieldDependency(HDStructFields.VaryingsMeshToPS.texCoord1,                                          HDStructFields.AttributesMesh.uv1),
            new FieldDependency(HDStructFields.VaryingsMeshToPS.texCoord2,                                          HDStructFields.AttributesMesh.uv2),
            new FieldDependency(HDStructFields.VaryingsMeshToPS.texCoord3,                                          HDStructFields.AttributesMesh.uv3),
            new FieldDependency(HDStructFields.VaryingsMeshToPS.color,                                              HDStructFields.AttributesMesh.color),
            new FieldDependency(HDStructFields.VaryingsMeshToPS.instanceID,                                         HDStructFields.AttributesMesh.instanceID),
        };

        public static DependencyCollection Tessellation = new DependencyCollection
        {
            //Tessellation Varying Dependencies
            new FieldDependency(HDStructFields.VaryingsMeshToPS.positionRWS,                                        HDStructFields.VaryingsMeshToDS.positionRWS),
            new FieldDependency(HDStructFields.VaryingsMeshToPS.positionPredisplacementRWS,                         HDStructFields.VaryingsMeshToDS.positionPredisplacementRWS),
            new FieldDependency(HDStructFields.VaryingsMeshToPS.normalWS,                                           HDStructFields.VaryingsMeshToDS.normalWS),
            new FieldDependency(HDStructFields.VaryingsMeshToPS.tangentWS,                                          HDStructFields.VaryingsMeshToDS.tangentWS),
            new FieldDependency(HDStructFields.VaryingsMeshToPS.texCoord0,                                          HDStructFields.VaryingsMeshToDS.texCoord0),
            new FieldDependency(HDStructFields.VaryingsMeshToPS.texCoord1,                                          HDStructFields.VaryingsMeshToDS.texCoord1),
            new FieldDependency(HDStructFields.VaryingsMeshToPS.texCoord2,                                          HDStructFields.VaryingsMeshToDS.texCoord2),
            new FieldDependency(HDStructFields.VaryingsMeshToPS.texCoord3,                                          HDStructFields.VaryingsMeshToDS.texCoord3),
            new FieldDependency(HDStructFields.VaryingsMeshToPS.color,                                              HDStructFields.VaryingsMeshToDS.color),
            new FieldDependency(HDStructFields.VaryingsMeshToPS.instanceID,                                         HDStructFields.VaryingsMeshToDS.instanceID),

            new FieldDependency(HDStructFields.VaryingsMeshToDS.positionRWS,                                        HDStructFields.AttributesMesh.positionOS),
            new FieldDependency(HDStructFields.VaryingsMeshToDS.positionPredisplacementRWS,                         HDStructFields.AttributesMesh.positionOS),
            new FieldDependency(HDStructFields.VaryingsMeshToDS.normalWS,                                           HDStructFields.AttributesMesh.normalOS),
            new FieldDependency(HDStructFields.VaryingsMeshToDS.tangentWS,                                          HDStructFields.AttributesMesh.tangentOS),
            new FieldDependency(HDStructFields.VaryingsMeshToDS.texCoord0,                                          HDStructFields.AttributesMesh.uv0),
            new FieldDependency(HDStructFields.VaryingsMeshToDS.texCoord1,                                          HDStructFields.AttributesMesh.uv1),
            new FieldDependency(HDStructFields.VaryingsMeshToDS.texCoord2,                                          HDStructFields.AttributesMesh.uv2),
            new FieldDependency(HDStructFields.VaryingsMeshToDS.texCoord3,                                          HDStructFields.AttributesMesh.uv3),
            new FieldDependency(HDStructFields.VaryingsMeshToDS.color,                                              HDStructFields.AttributesMesh.color),
            new FieldDependency(HDStructFields.VaryingsMeshToDS.instanceID,                                         HDStructFields.AttributesMesh.instanceID),
        };

        public static DependencyCollection FragInput = new DependencyCollection
        {
            //FragInput dependencies
            new FieldDependency(HDStructFields.FragInputs.positionRWS,                                              HDStructFields.VaryingsMeshToPS.positionRWS),
            new FieldDependency(HDStructFields.FragInputs.positionPredisplacementRWS,                               HDStructFields.VaryingsMeshToPS.positionPredisplacementRWS),
            new FieldDependency(HDStructFields.FragInputs.tangentToWorld,                                           HDStructFields.VaryingsMeshToPS.tangentWS),
            new FieldDependency(HDStructFields.FragInputs.tangentToWorld,                                           HDStructFields.VaryingsMeshToPS.normalWS),
            new FieldDependency(HDStructFields.FragInputs.texCoord0,                                                HDStructFields.VaryingsMeshToPS.texCoord0),
            new FieldDependency(HDStructFields.FragInputs.texCoord1,                                                HDStructFields.VaryingsMeshToPS.texCoord1),
            new FieldDependency(HDStructFields.FragInputs.texCoord2,                                                HDStructFields.VaryingsMeshToPS.texCoord2),
            new FieldDependency(HDStructFields.FragInputs.texCoord3,                                                HDStructFields.VaryingsMeshToPS.texCoord3),
            new FieldDependency(HDStructFields.FragInputs.color,                                                    HDStructFields.VaryingsMeshToPS.color),
            new FieldDependency(HDStructFields.FragInputs.instanceID,                                               HDStructFields.VaryingsMeshToPS.instanceID),
        };

        public static DependencyCollection VertexDescription = new DependencyCollection
        {
            //Vertex Description Dependencies
            new FieldDependency(StructFields.VertexDescriptionInputs.ObjectSpaceNormal,                             HDStructFields.AttributesMesh.normalOS),
            new FieldDependency(StructFields.VertexDescriptionInputs.WorldSpaceNormal,                              HDStructFields.AttributesMesh.normalOS),
            new FieldDependency(StructFields.VertexDescriptionInputs.ViewSpaceNormal,                               StructFields.VertexDescriptionInputs.WorldSpaceNormal),

            new FieldDependency(StructFields.VertexDescriptionInputs.ObjectSpaceTangent,                            HDStructFields.AttributesMesh.tangentOS),
            new FieldDependency(StructFields.VertexDescriptionInputs.WorldSpaceTangent,                             HDStructFields.AttributesMesh.tangentOS),
            new FieldDependency(StructFields.VertexDescriptionInputs.ViewSpaceTangent,                              StructFields.VertexDescriptionInputs.WorldSpaceTangent),

            new FieldDependency(StructFields.VertexDescriptionInputs.ObjectSpaceBiTangent,                          HDStructFields.AttributesMesh.normalOS),
            new FieldDependency(StructFields.VertexDescriptionInputs.ObjectSpaceBiTangent,                          HDStructFields.AttributesMesh.tangentOS),
            new FieldDependency(StructFields.VertexDescriptionInputs.WorldSpaceBiTangent,                           StructFields.VertexDescriptionInputs.ObjectSpaceBiTangent),
            new FieldDependency(StructFields.VertexDescriptionInputs.ViewSpaceBiTangent,                            StructFields.VertexDescriptionInputs.WorldSpaceBiTangent),

            new FieldDependency(StructFields.VertexDescriptionInputs.ObjectSpacePosition,                           HDStructFields.AttributesMesh.positionOS),
            new FieldDependency(StructFields.VertexDescriptionInputs.WorldSpacePosition,                            HDStructFields.AttributesMesh.positionOS),
            new FieldDependency(StructFields.VertexDescriptionInputs.AbsoluteWorldSpacePosition,                    HDStructFields.AttributesMesh.positionOS),
            new FieldDependency(StructFields.VertexDescriptionInputs.ViewSpacePosition,                             StructFields.VertexDescriptionInputs.WorldSpacePosition),

            new FieldDependency(StructFields.VertexDescriptionInputs.ObjectSpacePositionPredisplacement,            HDStructFields.AttributesMesh.positionOS),
            new FieldDependency(StructFields.VertexDescriptionInputs.WorldSpacePositionPredisplacement,             HDStructFields.AttributesMesh.positionOS),
            new FieldDependency(StructFields.VertexDescriptionInputs.AbsoluteWorldSpacePositionPredisplacement,     HDStructFields.AttributesMesh.positionOS),
            new FieldDependency(StructFields.VertexDescriptionInputs.ViewSpacePositionPredisplacement,              StructFields.VertexDescriptionInputs.WorldSpacePosition),

            new FieldDependency(StructFields.VertexDescriptionInputs.WorldSpaceViewDirection,                       StructFields.VertexDescriptionInputs.WorldSpacePosition),
            new FieldDependency(StructFields.VertexDescriptionInputs.ObjectSpaceViewDirection,                      StructFields.VertexDescriptionInputs.WorldSpaceViewDirection),
            new FieldDependency(StructFields.VertexDescriptionInputs.ViewSpaceViewDirection,                        StructFields.VertexDescriptionInputs.WorldSpaceViewDirection),
            new FieldDependency(StructFields.VertexDescriptionInputs.TangentSpaceViewDirection,                     StructFields.VertexDescriptionInputs.WorldSpaceViewDirection),
            new FieldDependency(StructFields.VertexDescriptionInputs.TangentSpaceViewDirection,                     StructFields.VertexDescriptionInputs.WorldSpaceTangent),
            new FieldDependency(StructFields.VertexDescriptionInputs.TangentSpaceViewDirection,                     StructFields.VertexDescriptionInputs.WorldSpaceBiTangent),
            new FieldDependency(StructFields.VertexDescriptionInputs.TangentSpaceViewDirection,                     StructFields.VertexDescriptionInputs.WorldSpaceNormal),

            // vertex shader: screen position reads from world space position, then used to calculate NDC and Pixel position
            new FieldDependency(StructFields.VertexDescriptionInputs.ScreenPosition,                                StructFields.VertexDescriptionInputs.WorldSpacePosition),
            new FieldDependency(StructFields.VertexDescriptionInputs.NDCPosition,                                   StructFields.VertexDescriptionInputs.ScreenPosition),
            new FieldDependency(StructFields.VertexDescriptionInputs.PixelPosition,                                 StructFields.VertexDescriptionInputs.NDCPosition),

            new FieldDependency(StructFields.VertexDescriptionInputs.uv0,                                           HDStructFields.AttributesMesh.uv0),
            new FieldDependency(StructFields.VertexDescriptionInputs.uv1,                                           HDStructFields.AttributesMesh.uv1),
            new FieldDependency(StructFields.VertexDescriptionInputs.uv2,                                           HDStructFields.AttributesMesh.uv2),
            new FieldDependency(StructFields.VertexDescriptionInputs.uv3,                                           HDStructFields.AttributesMesh.uv3),
            new FieldDependency(StructFields.VertexDescriptionInputs.VertexColor,                                   HDStructFields.AttributesMesh.color),

            new FieldDependency(StructFields.VertexDescriptionInputs.BoneWeights,                                   HDStructFields.AttributesMesh.weights),
            new FieldDependency(StructFields.VertexDescriptionInputs.BoneIndices,                                   HDStructFields.AttributesMesh.indices),
            new FieldDependency(StructFields.VertexDescriptionInputs.VertexID,                                      HDStructFields.AttributesMesh.vertexID),
            new FieldDependency(StructFields.VertexDescriptionInputs.InstanceID,                                    HDStructFields.AttributesMesh.instanceID),
        };

        public static DependencyCollection VertexDescriptionTessellation = new DependencyCollection
        {
            //Vertex Description Dependencies
            new FieldDependency(StructFields.VertexDescriptionInputs.ObjectSpaceTangent,                            HDStructFields.VaryingsMeshToDS.tangentWS),
            new FieldDependency(StructFields.VertexDescriptionInputs.WorldSpaceTangent,                             HDStructFields.VaryingsMeshToDS.tangentWS),
            new FieldDependency(StructFields.VertexDescriptionInputs.ObjectSpaceBiTangent,                          HDStructFields.VaryingsMeshToDS.tangentWS),

            new FieldDependency(StructFields.VertexDescriptionInputs.uv0,                                           HDStructFields.VaryingsMeshToDS.texCoord0),
            new FieldDependency(StructFields.VertexDescriptionInputs.uv1,                                           HDStructFields.VaryingsMeshToDS.texCoord1),
            new FieldDependency(StructFields.VertexDescriptionInputs.uv2,                                           HDStructFields.VaryingsMeshToDS.texCoord2),
            new FieldDependency(StructFields.VertexDescriptionInputs.uv3,                                           HDStructFields.VaryingsMeshToDS.texCoord3),
            new FieldDependency(StructFields.VertexDescriptionInputs.VertexColor,                                   HDStructFields.VaryingsMeshToDS.color),
            new FieldDependency(StructFields.VertexDescriptionInputs.InstanceID,                                    HDStructFields.VaryingsMeshToDS.instanceID),
        };

        public static DependencyCollection SurfaceDescription = new DependencyCollection
        {
            //Surface Description Dependencies
            new FieldDependency(StructFields.SurfaceDescriptionInputs.WorldSpaceNormal,                             HDStructFields.FragInputs.tangentToWorld),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.ObjectSpaceNormal,                            StructFields.SurfaceDescriptionInputs.WorldSpaceNormal),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.ViewSpaceNormal,                              StructFields.SurfaceDescriptionInputs.WorldSpaceNormal),

            new FieldDependency(StructFields.SurfaceDescriptionInputs.WorldSpaceTangent,                            HDStructFields.FragInputs.tangentToWorld),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.ObjectSpaceTangent,                           StructFields.SurfaceDescriptionInputs.WorldSpaceTangent),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.ViewSpaceTangent,                             StructFields.SurfaceDescriptionInputs.WorldSpaceTangent),

            new FieldDependency(StructFields.SurfaceDescriptionInputs.WorldSpaceBiTangent,                          HDStructFields.FragInputs.tangentToWorld),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.ObjectSpaceBiTangent,                         StructFields.SurfaceDescriptionInputs.WorldSpaceBiTangent),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.ViewSpaceBiTangent,                           StructFields.SurfaceDescriptionInputs.WorldSpaceBiTangent),

            new FieldDependency(StructFields.SurfaceDescriptionInputs.WorldSpacePosition,                           HDStructFields.FragInputs.positionRWS),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.AbsoluteWorldSpacePosition,                   HDStructFields.FragInputs.positionRWS),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.ObjectSpacePosition,                          HDStructFields.FragInputs.positionRWS),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.ViewSpacePosition,                            HDStructFields.FragInputs.positionRWS),

            new FieldDependency(StructFields.SurfaceDescriptionInputs.WorldSpacePositionPredisplacement,            HDStructFields.FragInputs.positionPredisplacementRWS),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.AbsoluteWorldSpacePositionPredisplacement,    HDStructFields.FragInputs.positionPredisplacementRWS),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.ObjectSpacePositionPredisplacement,           HDStructFields.FragInputs.positionPredisplacementRWS),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.ViewSpacePositionPredisplacement,             HDStructFields.FragInputs.positionPredisplacementRWS),

            new FieldDependency(StructFields.SurfaceDescriptionInputs.WorldSpaceViewDirection,                      HDStructFields.FragInputs.positionRWS),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.ObjectSpaceViewDirection,                     StructFields.SurfaceDescriptionInputs.WorldSpaceViewDirection),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.ViewSpaceViewDirection,                       StructFields.SurfaceDescriptionInputs.WorldSpaceViewDirection),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.TangentSpaceViewDirection,                    StructFields.SurfaceDescriptionInputs.WorldSpaceViewDirection),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.TangentSpaceViewDirection,                    StructFields.SurfaceDescriptionInputs.WorldSpaceTangent),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.TangentSpaceViewDirection,                    StructFields.SurfaceDescriptionInputs.WorldSpaceBiTangent),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.TangentSpaceViewDirection,                    StructFields.SurfaceDescriptionInputs.WorldSpaceNormal),

            // pixel shader: pixel position read from vpos, then used to calculate NDC position.  Screen position calculated separately from world space position
            new FieldDependency(StructFields.SurfaceDescriptionInputs.PixelPosition,                                HDStructFields.FragInputs.positionPixel),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.NDCPosition,                                  StructFields.SurfaceDescriptionInputs.PixelPosition),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.ScreenPosition,                               StructFields.SurfaceDescriptionInputs.WorldSpacePosition),

            new FieldDependency(StructFields.SurfaceDescriptionInputs.uv0,                                          HDStructFields.FragInputs.texCoord0),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.uv1,                                          HDStructFields.FragInputs.texCoord1),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.uv2,                                          HDStructFields.FragInputs.texCoord2),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.uv3,                                          HDStructFields.FragInputs.texCoord3),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.VertexColor,                                  HDStructFields.FragInputs.color),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.InstanceID,                                   HDStructFields.FragInputs.instanceID),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.FaceSign,                                     HDStructFields.FragInputs.IsFrontFace),
        };

        public static DependencyCollection Default = new DependencyCollection
        {
            { Varying },
            { Tessellation },
            { FragInput },
            { VertexDescription },
            { VertexDescriptionTessellation },
            { SurfaceDescription },
        };
    }
    #endregion

    #region RequiredFields
    static class CoreRequiredFields
    {
        public static FieldCollection Meta = new FieldCollection()
        {
            HDStructFields.AttributesMesh.positionOS,
            HDStructFields.AttributesMesh.normalOS,
            HDStructFields.AttributesMesh.uv0,
            HDStructFields.AttributesMesh.uv1,
            HDStructFields.AttributesMesh.uv2,
            HDStructFields.AttributesMesh.uv3,
            HDStructFields.FragInputs.positionRWS,
            HDStructFields.FragInputs.positionPredisplacementRWS,
            HDStructFields.FragInputs.texCoord0,
            HDStructFields.FragInputs.texCoord1,
            HDStructFields.FragInputs.texCoord2,
            HDStructFields.FragInputs.texCoord3,
        };

        public static FieldCollection Basic = new FieldCollection()
        {
        };

        // positionRWS required by GetSurfaceAndBuiltinData.
        public static FieldCollection BasicSurfaceData = new FieldCollection()
        {
            Basic,
            HDStructFields.FragInputs.positionRWS,
        };

        public static FieldCollection BasicLighting = new FieldCollection()
        {
            // We need positionRWS to calculate the view vector per pixel for some hardcoded effect like Specular occlusion
            // We also need it to calculate motion vector
            HDStructFields.FragInputs.positionRWS,
            // We need to have tangent because if a lighting model have anisotropy and require tangent, it need to be present
            // This works for all lighting shader type including raytracing other fields are included due to DependencyCollection
            HDStructFields.FragInputs.tangentToWorld,
            // UV1 / 2 are always included for lightmaps (static and dynamic) sampling
            HDStructFields.FragInputs.texCoord1,
            HDStructFields.FragInputs.texCoord2,
        };

        // Note: this can result in duplicate with BasicLighting but shouldn't be an issue
        public static FieldCollection AddWriteNormalBuffer = new FieldCollection()
        {
            HDStructFields.FragInputs.tangentToWorld, // Required for WRITE_NORMAL_BUFFER case (to access to vertex normal)
        };
    }
    #endregion

    #region RenderStates
    static class CoreRenderStates
    {
        public static class Uniforms
        {
            public static readonly string srcBlend = "[_SrcBlend]";
            public static readonly string dstBlend = "[_DstBlend]";
            public static readonly string alphaSrcBlend = "[_AlphaSrcBlend]";
            public static readonly string alphaDstBlend = "[_AlphaDstBlend]";
            public static readonly string alphaCutoffEnable = "[_AlphaCutoffEnable]";
            public static readonly string cullMode = "[_CullMode]";
            public static readonly string cullModeForward = "[_CullModeForward]";
            public static readonly string zTestDepthEqualForOpaque = "[_ZTestDepthEqualForOpaque]";
            public static readonly string zTestTransparent = "[_ZTestTransparent]";
            public static readonly string zTestGBuffer = "[_ZTestGBuffer]";
            public static readonly string zWrite = "[_ZWrite]";
            public static readonly string zClip = "[_ZClip]";
            public static readonly string stencilWriteMaskDepth = "[_StencilWriteMaskDepth]";
            public static readonly string stencilRefDepth = "[_StencilRefDepth]";
            public static readonly string stencilWriteMaskMV = "[_StencilWriteMaskMV]";
            public static readonly string stencilRefMV = "[_StencilRefMV]";
            public static readonly string stencilWriteMask = "[_StencilWriteMask]";
            public static readonly string stencilRef = "[_StencilRef]";
            public static readonly string stencilWriteMaskGBuffer = "[_StencilWriteMaskGBuffer]";
            public static readonly string stencilRefGBuffer = "[_StencilRefGBuffer]";
            public static readonly string stencilRefDistortionVec = "[_StencilRefDistortionVec]";
            public static readonly string stencilWriteMaskDistortionVec = "[_StencilWriteMaskDistortionVec]";
        }

        public static string PremultipliedTransparency(int target) => $"Blend {target} One OneMinusSrcAlpha";

        public static RenderStateCollection Meta = new RenderStateCollection
        {
            { RenderState.Cull(Cull.Off) },
        };

        public static RenderStateCollection ShadowCaster = new RenderStateCollection
        {
            { RenderState.Cull(Uniforms.cullMode) },
            { RenderState.ZWrite(ZWrite.On) },
            { RenderState.ZClip(Uniforms.zClip) },
            { RenderState.ColorMask("ColorMask 0") },
        };

        public static RenderStateCollection BlendShadowCaster = new RenderStateCollection
        {
            { RenderState.Blend(Blend.One, Blend.Zero) },
            { RenderState.Cull(Uniforms.cullMode) },
            { RenderState.ZWrite(ZWrite.On) },
            { RenderState.ZClip(Uniforms.zClip) },
            { RenderState.ColorMask("ColorMask 0") },
        };

        public static RenderStateCollection ScenePicking = new RenderStateCollection
        {
            { RenderState.Cull(Uniforms.cullMode) },
        };

        public static RenderStateCollection SceneSelection = new RenderStateCollection
        {
            { RenderState.Cull(Cull.Off) },
        };

        public static RenderStateCollection DepthOnly = new RenderStateCollection
        {
            { RenderState.Cull(Uniforms.cullMode) },
            { RenderState.ZWrite(ZWrite.On) },
            { RenderState.AlphaToMask(Uniforms.alphaCutoffEnable), new FieldCondition(Fields.AlphaToMask, true) },
            { RenderState.Stencil(new StencilDescriptor()
            {
                WriteMask = Uniforms.stencilWriteMaskDepth,
                Ref = Uniforms.stencilRefDepth,
                Comp = "Always",
                Pass = "Replace",
            }) },
        };

        public static RenderStateCollection MotionVectors = new RenderStateCollection
        {
            { RenderState.Cull(Uniforms.cullMode) },
            { RenderState.ZWrite(ZWrite.On) },
            { RenderState.AlphaToMask(Uniforms.alphaCutoffEnable), new FieldCondition(Fields.AlphaToMask, true) },
            { RenderState.Stencil(new StencilDescriptor()
            {
                WriteMask = Uniforms.stencilWriteMaskMV,
                Ref = Uniforms.stencilRefMV,
                Comp = "Always",
                Pass = "Replace",
            }) },
        };

        public static RenderStateCollection TransparentBackface = new RenderStateCollection
        {
            { RenderState.Blend(Uniforms.srcBlend, Uniforms.dstBlend, Uniforms.alphaSrcBlend, Uniforms.alphaDstBlend) },
            { RenderState.Blend(PremultipliedTransparency(1)) },
            { RenderState.Blend(PremultipliedTransparency(2)) },
            { RenderState.Blend(PremultipliedTransparency(3)) },
            { RenderState.Blend(PremultipliedTransparency(4)) },
            { RenderState.Cull(Cull.Front) },
            { RenderState.ZWrite(Uniforms.zWrite) },
            { RenderState.ZTest(Uniforms.zTestTransparent) },
            { RenderState.ColorMask("ColorMask [_ColorMaskTransparentVelOne] 1") },
            { RenderState.ColorMask("ColorMask [_ColorMaskTransparentVelTwo] 2") },
        };


        public static RenderStateCollection TransparentDepthPrePass = new RenderStateCollection
        {
            { RenderState.Blend(Blend.One, Blend.Zero) },
            { RenderState.Cull(Uniforms.cullMode) },
            { RenderState.ZWrite(ZWrite.On) },
            { RenderState.Stencil(new StencilDescriptor()
            {
                WriteMask = CoreRenderStates.Uniforms.stencilWriteMaskDepth,
                Ref = CoreRenderStates.Uniforms.stencilRefDepth,
                Comp = "Always",
                Pass = "Replace",
            }) },
        };

        public static RenderStateCollection TransparentDepthPostPass = new RenderStateCollection
        {
            { RenderState.Blend(Blend.One, Blend.Zero) },
            { RenderState.Cull(Uniforms.cullMode) },
            { RenderState.ZWrite(ZWrite.On) },
            { RenderState.ColorMask("ColorMask 0") },
        };

        public static RenderStateCollection Forward = new RenderStateCollection
        {
            { RenderState.Blend(Uniforms.srcBlend, Uniforms.dstBlend, Uniforms.alphaSrcBlend, Uniforms.alphaDstBlend) },
            { RenderState.Blend(PremultipliedTransparency(1)) },
            { RenderState.Blend("Blend 2 One [_DstBlend2]") },
            { RenderState.Blend("Blend 3 One [_DstBlend2]") },
            { RenderState.Blend(PremultipliedTransparency(4)) },
            { RenderState.Cull(Uniforms.cullModeForward) },
            { RenderState.ZWrite(Uniforms.zWrite) },
            { RenderState.ZTest(Uniforms.zTestDepthEqualForOpaque) },
            { RenderState.ColorMask("ColorMask [_ColorMaskTransparentVelOne] 1") },
            { RenderState.ColorMask("ColorMask [_ColorMaskTransparentVelTwo] 2") },
            { RenderState.Stencil(new StencilDescriptor()
            {
                WriteMask = Uniforms.stencilWriteMask,
                Ref = Uniforms.stencilRef,
                Comp = "Always",
                Pass = "Replace",
            }) },
        };

        public static RenderStateCollection LineRendering = new RenderStateCollection
        {
            { RenderState.Blend(Blend.One, Blend.Zero) },
            { RenderState.Cull("Off") },
            { RenderState.ZWrite(ZWrite.On) },
            { RenderState.ZTest("Always") },
            { RenderState.Stencil(new StencilDescriptor()
            {
                WriteMask = Uniforms.stencilWriteMask,
                Ref = Uniforms.stencilRef,
                Comp = "Always",
                Pass = "Replace",
            }) },
        };
    }
    #endregion

    #region Pragmas
    static class CorePragmas
    {
        // We will always select Basic, BasicVFX or BasicTessellation - added in PostProcessSubShader
        public static PragmaCollection Basic = new PragmaCollection
        {
            { Pragma.Target(ShaderModel.Target45) },
            { Pragma.Vertex("Vert") },
            { Pragma.Fragment("Frag") },
            { Pragma.OnlyRenderers(PragmaRenderers.GetHighEndPlatformArray()) },
        };

        public static PragmaCollection BasicVFX = new PragmaCollection
        {
            { Pragma.Target(ShaderModel.Target45) },
            { Pragma.Vertex("VertVFX") },
            { Pragma.Fragment("Frag") },
            { Pragma.OnlyRenderers(PragmaRenderers.GetHighEndPlatformArray()) },
        };

        public static PragmaCollection BasicTessellation = new PragmaCollection
        {
            { Pragma.Target(ShaderModel.Target50) },
            { Pragma.Vertex("Vert") },
            { Pragma.Fragment("Frag") },
            { Pragma.Hull("Hull") },
            { Pragma.Domain("Domain") },
            { Pragma.OnlyRenderers(PragmaRenderers.GetHighEndPlatformArray()) },
        };

        public static PragmaCollection BasicRaytracing = new PragmaCollection
        {
            { Pragma.Target(ShaderModel.Target50) },
            { Pragma.Raytracing("surface_shader") },
            { Pragma.OnlyRenderers(new Platform[] {Platform.D3D11, Platform.GameCoreXboxSeries, Platform.PS5}) },
        };

        public static PragmaCollection BasicKernel = new PragmaCollection
        {
            { Pragma.Kernel("Kernel") },
        };

        // Here are the Pragma Collection we can add on top of the Basic one
        public static PragmaCollection DotsInstanced = new PragmaCollection
        {
            { Pragma.DOTSInstancing },
            { Pragma.InstancingOptions(InstancingOptions.RenderingLayer) },
        };

        public static PragmaCollection DotsInstancedEditorSync = new PragmaCollection
        {
            { Pragma.DOTSInstancing },
            { Pragma.EditorSyncCompilation },
            { Pragma.InstancingOptions(InstancingOptions.RenderingLayer) },
        };
    }
    #endregion

    #region Keywords
    static class CoreKeywords
    {
        public static KeywordCollection RaytracingGBuffer = new KeywordCollection
        {
            { CoreKeywordDescriptors.RaytraceMinimalGBuffer },
        };

        public static KeywordCollection RaytracingVisiblity = new KeywordCollection
        {
            { CoreKeywordDescriptors.TransparentColorShadow },
        };
    }
    #endregion

    #region Defines
    static class CoreDefines
    {
        public static DefineCollection SupportGlobalMipBias = new DefineCollection
        {
            { CoreKeywordDescriptors.SupportGlobalMipBias, 1 },
        };

        public static DefineCollection Tessellation = new DefineCollection
        {
            { CoreKeywordDescriptors.Tessellation, 1 },
            { CoreKeywordDescriptors.TessellationModification, 1 },
        };

        public static DefineCollection ScenePicking = new DefineCollection
        {
            { CoreKeywordDescriptors.ScenePickingPass, 1 },
        };

        public static DefineCollection Meta = new DefineCollection
        {
            { RayTracingQualityNode.GetRayTracingQualityKeyword(), 0 },
            // Use Unity's built-in matrices for meta pass rendering
            { CoreKeywordDescriptors.ScenePickingPass, 1 },
        };

        public static DefineCollection SceneSelection = new DefineCollection
        {
            { RayTracingQualityNode.GetRayTracingQualityKeyword(), 0 },
            { CoreKeywordDescriptors.SceneSelectionPass, 1 },
        };

        public static DefineCollection DepthForwardOnly = new DefineCollection
        {
            { RayTracingQualityNode.GetRayTracingQualityKeyword(), 0 },
            { CoreKeywordDescriptors.WriteNormalBuffer, 1 },
        };

        public static DefineCollection DepthForwardOnlyUnlit = new DefineCollection
        {
            // When using Shadow matte, we need to output a normal buffer even for unlit so it is compatible with ambient occlusion
            { CoreKeywordDescriptors.WriteNormalBuffer, 1, new FieldCondition(HDUnlitSubTarget.EnableShadowMatte, true)},
        };

        public static DefineCollection MotionVectorUnlit = new DefineCollection
        {
            // When using Shadow matte, we need to output a normal buffer even for unlit so it is compatible with ambient occlusion
            { CoreKeywordDescriptors.WriteNormalBuffer, 1, new FieldCondition(HDUnlitSubTarget.EnableShadowMatte, true)},
        };

        public static DefineCollection ShaderGraphRaytracingDefault = new DefineCollection
        {
            { RayTracingQualityNode.GetRayTracingQualityKeyword(), 0 },
        };

        public static DefineCollection TransparentDepthPrepass = new DefineCollection
        {
            { RayTracingQualityNode.GetRayTracingQualityKeyword(), 0 },
        };

        public static DefineCollection TransparentDepthPostpass = new DefineCollection
        {
            { RayTracingQualityNode.GetRayTracingQualityKeyword(), 0 },
        };

        public static DefineCollection Forward = new DefineCollection
        {
            { CoreKeywordDescriptors.SupportBlendModePreserveSpecularLighting, 1 },
            { CoreKeywordDescriptors.HasLightloop, 1 },
            { RayTracingQualityNode.GetRayTracingQualityKeyword(), 0 },
        };

        public static DefineCollection ForwardLit = new DefineCollection
        {
            { CoreKeywordDescriptors.SupportBlendModePreserveSpecularLighting, 1 },
            { CoreKeywordDescriptors.HasLightloop, 1 },
            { RayTracingQualityNode.GetRayTracingQualityKeyword(), 0 },
            { CoreKeywordDescriptors.ShaderLit, 1 },
        };

        public static DefineCollection ForwardUnlit = new DefineCollection
        {
            { RayTracingQualityNode.GetRayTracingQualityKeyword(), 0 },
        };

        public static DefineCollection BackThenFront = new DefineCollection
        {
            { CoreKeywordDescriptors.SupportBlendModePreserveSpecularLighting, 1 },
            { CoreKeywordDescriptors.HasLightloop, 1 },
            { RayTracingQualityNode.GetRayTracingQualityKeyword(), 0 },
            // { CoreKeywordDescriptors.LightList, 1 }, // BackThenFront Transparent use #define USE_CLUSTERED_LIGHTLIST
        };
    }
    #endregion

    #region Includes
    static class CoreIncludes
    {
        // CorePregraph
        public const string kShaderVariables = "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl";
        public const string kFragInputs = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl";
        public const string kMaterial = "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl";
        public const string kDebugDisplay = "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplay.hlsl";
        public const string kPickingSpaceTransforms = "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/PickingSpaceTransforms.hlsl";

        // CoreUtility
        public const string kBuiltInUtilities = "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl";
        public const string kMaterialUtilities = "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl";

        // Pregraph Raytracing
        public const string kRaytracingMacros = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingMacros.hlsl";
        public const string kShaderVariablesRaytracing = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracing.hlsl";
        public const string kShaderVariablesRaytracingLightLoop = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracingLightLoop.hlsl";
        public const string kRaytracingIntersection = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingIntersection.hlsl";
        public const string kRaytracingIntersectionGBuffer = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/Deferred/RaytracingIntersectonGBuffer.hlsl";
        public const string kRaytracingIntersectionSubSurface = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/SubSurface/RayTracingIntersectionSubSurface.hlsl";
        public const string kLitRaytracing = "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitRaytracing.hlsl";
        public const string kLitPathtracing = "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitPathTracing.hlsl";
        public const string kUnlitRaytracing = "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Unlit/UnlitRaytracing.hlsl";
        public const string kFabricRaytracing = "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Fabric/FabricRaytracing.hlsl";
        public const string kFabricPathtracing = "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Fabric/FabricPathTracing.hlsl";
        public const string kEyeRaytracing = "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Eye/EyeRaytracing.hlsl";
        public const string kStackLitRaytracing = "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/StackLit/StackLitRaytracing.hlsl";
        public const string kStackLitPathtracing = "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/StackLit/StackLitPathTracing.hlsl";
        public const string kHairRaytracing = "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Hair/HairRaytracing.hlsl";
        public const string kHairPathtracing = "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Hair/HairPathTracing.hlsl";
        public const string kRaytracingLightLoop = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingLightLoop.hlsl";
        public const string kRaytracingCommon = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingCommon.hlsl";
        public const string kNormalBuffer = "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/NormalBuffer.hlsl";
        public const string kRaytracingLightCluster = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RayTracingLightCluster.hlsl";

        // Postgraph Raytracing
        public const string kPassRaytracingIndirect = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassRaytracingIndirect.hlsl";
        public const string kPassRaytracingVisbility = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassRaytracingVisibility.hlsl";
        public const string kPassRaytracingForward = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassRaytracingForward.hlsl";
        public const string kPassRaytracingGBuffer = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderpassRaytracingGBuffer.hlsl";
        public const string kPassRaytracingDebug = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassRaytracingDebug.hlsl";
        public const string kPassPathTracing = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassPathTracing.hlsl";
        public const string kPassRaytracingSubSurface = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderpassRaytracingSubSurface.hlsl";

        // Public Pregraph Function
        public const string kCommonLighting = "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonLighting.hlsl";
        public const string kHDShadow = "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/HDShadow.hlsl";
        public const string kLightLoopDef = "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl";
        public const string kPunctualLightCommon = "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/PunctualLightCommon.hlsl";
        public const string kHDShadowLoop = "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/HDShadowLoop.hlsl";
        public const string kHDRaytracingShadowLoop = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RayTracing/Shaders/HDRaytracingShadowLoop.hlsl";
        public const string kNormalSurfaceGradient = "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl";
        public const string kLighting = "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl";
        public const string kLightLoop = "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoop.hlsl";

        // Public Pregraph Material
        public const string kUnlit = "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Unlit/Unlit.hlsl";
        public const string kLit = "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl";
        public const string kFabric = "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Fabric/Fabric.hlsl";
        public const string kHair = "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Hair/Hair.hlsl";
        public const string kStackLit = "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/StackLit/StackLit.hlsl";
        public const string kSixWayLit = "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/SixWayLit/SixWaySmokeLit.hlsl";

        // Public Pregraph Misc
        public const string kShaderGraphFunctions = "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl";
        public const string kDecalUtilities = "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl";
        public const string kPassPlaceholder = "Pass Include Placeholder, replace me !";
        public const string kPostDecalsPlaceholder = "After Decal Include Placeholder, replace me !";
        public const string kRaytracingPlaceholder = "Raytracing Include Placeholder, replace me !";
        public const string kPathtracingPlaceholder = "Pathtracing Include Placeholder, replace me !";

        // Public Postgraph Pass
        public const string kPassLightTransport = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassLightTransport.hlsl";
        public const string kPassDepthOnly = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl";
        public const string kPassGBuffer = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassGBuffer.hlsl";
        public const string kPassMotionVectors = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassMotionVectors.hlsl";
        public const string kDisortionVectors = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDistortion.hlsl";
        public const string kPassForward = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForward.hlsl";
        public const string kStandardLit = "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/StandardLit/StandardLit.hlsl";
        public const string kPassForwardUnlit = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForwardUnlit.hlsl";
        public const string kPassConstant = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassConstant.hlsl";
        public const string kPassFullScreenDebug = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassFullScreenDebug.hlsl";

        public static IncludeCollection MinimalCorePregraph = new IncludeCollection
        {
            { kShaderVariables, IncludeLocation.Pregraph },
            { kFragInputs, IncludeLocation.Pregraph },
        };

        public static IncludeCollection CorePregraph = new IncludeCollection
        {
            { kDebugDisplay, IncludeLocation.Pregraph },
            { kMaterial, IncludeLocation.Pregraph },
        };

        public static IncludeCollection RaytracingCorePregraph = new IncludeCollection
        {
            // Pregraph includes
            { CoreIncludes.kRaytracingMacros, IncludeLocation.Pregraph },
            { CoreIncludes.kShaderVariablesRaytracing, IncludeLocation.Pregraph },
            { CoreIncludes.kMaterial, IncludeLocation.Pregraph },
            { CoreIncludes.kShaderVariablesRaytracingLightLoop, IncludeLocation.Pregraph },
        };

        public static IncludeCollection CoreUtility = new IncludeCollection
        {
            { kBuiltInUtilities, IncludeLocation.Pregraph },
            { kMaterialUtilities, IncludeLocation.Pregraph },
        };
    }
    #endregion

    #region KeywordDescriptors
    static class CoreKeywordDescriptors
    {
        public static KeywordDescriptor WriteNormalBuffer = new KeywordDescriptor()
        {
            displayName = "Write Normal Buffer",
            referenceName = "WRITE_NORMAL_BUFFER",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };

        public static KeywordDescriptor WriteMsaaDepth = new KeywordDescriptor()
        {
            displayName = "Write MSAA Depth",
            referenceName = "WRITE_MSAA_DEPTH",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
            stages = KeywordShaderStage.Fragment,
        };

        public static KeywordDescriptor WriteDecalBufferDepthOnly = new KeywordDescriptor()
        {
            displayName = "Write Decal Buffer (Depth Only)",
            referenceName = "WRITE",
            type = KeywordType.Enum,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
            entries = new KeywordEntry[]
            {
                new KeywordEntry() { displayName = "Off", referenceName = "" },
                new KeywordEntry() { displayName = "Decal Buffer", referenceName = "DECAL_BUFFER" },
                new KeywordEntry() { displayName = "Rendering Layer", referenceName = "RENDERING_LAYER" },
            },
            stages = KeywordShaderStage.Fragment,
        };

        public static KeywordDescriptor WriteDecalBufferMotionVector = new KeywordDescriptor()
        {
            displayName = "Write Decal Buffer (Motion Vector)",
            referenceName = "WRITE_DECAL_BUFFER_AND_RENDERING_LAYER",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
            stages = KeywordShaderStage.Fragment,
        };

        public static KeywordDescriptor DebugDisplay = new KeywordDescriptor()
        {
            displayName = "Debug Display",
            referenceName = "DEBUG_DISPLAY",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };

        public static KeywordDescriptor ProceduralInstancing = new KeywordDescriptor()
        {
            displayName = "Procedural Instancing",
            referenceName = "PROCEDURAL_INSTANCING_ON",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };

        public static KeywordDescriptor StereoInstancing = new KeywordDescriptor()
        {
            displayName = "Stereo Instancing",
            referenceName = "STEREO_INSTANCING_ON",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };

        public static KeywordDescriptor Lightmap = new KeywordDescriptor()
        {
            displayName = "Lightmap",
            referenceName = "LIGHTMAP_ON",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global
            // Caution: 'Optimize Mesh Data' strip away attributes uv1/uv2 without the keyword set on the vertex stage. - so don't define stage frequency here.
        };

        public static KeywordDescriptor DirectionalLightmapCombined = new KeywordDescriptor()
        {
            displayName = "Directional Lightmap Combined",
            referenceName = "DIRLIGHTMAP_COMBINED",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
            // Don't define shader stage frequency
        };

        public static KeywordDescriptor DynamicLightmap = new KeywordDescriptor()
        {
            displayName = "Dynamic Lightmap",
            referenceName = "DYNAMICLIGHTMAP_ON",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
            // Don't define shader stage frequency
        };

        public static KeywordDescriptor ShadowsShadowmask = new KeywordDescriptor()
        {
            displayName = "Shadows Shadowmask",
            referenceName = "SHADOWS_SHADOWMASK",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
            stages = KeywordShaderStage.FragmentAndRaytracing
        };

        public static KeywordDescriptor ScreenSpaceShadow = new KeywordDescriptor()
        {
            displayName = "ScreenSpaceShadow",
            referenceName = "SCREEN_SPACE_SHADOWS",
            type = KeywordType.Enum,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
            entries = new KeywordEntry[]
            {
                new KeywordEntry() { displayName = "Off", referenceName = "OFF" },
                new KeywordEntry() { displayName = "On", referenceName = "ON" },
            },
            stages = KeywordShaderStage.Fragment,
        };

        public static KeywordDescriptor RenderingLayers = new KeywordDescriptor()
        {
            displayName = "Rendering Layers",
            referenceName = "RENDERING_LAYERS",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
            stages = KeywordShaderStage.FragmentAndRaytracing,
        };

        public static KeywordDescriptor Decals = new KeywordDescriptor()
        {
            displayName = "Decals",
            referenceName = "DECALS",
            type = KeywordType.Enum,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
            entries = new KeywordEntry[]
            {
                new KeywordEntry() { displayName = "Off", referenceName = "OFF" },
                new KeywordEntry() { displayName = "3RT", referenceName = "3RT" },
                new KeywordEntry() { displayName = "4RT", referenceName = "4RT" },
            },
            stages = KeywordShaderStage.Fragment,
        };

        public static KeywordDescriptor DecalsRayTracing = new KeywordDescriptor()
        {
            displayName = "Decals",
            referenceName = "DECALS",
            type = KeywordType.Enum,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
            entries = new KeywordEntry[]
            {
                new KeywordEntry() { displayName = "Off", referenceName = "OFF" },
                new KeywordEntry() { displayName = "3RT", referenceName = "3RT" },
                new KeywordEntry() { displayName = "4RT", referenceName = "4RT" },
            },
            stages = KeywordShaderStage.RayTracing,
        };

        public static KeywordDescriptor ProbeVolumes = new KeywordDescriptor()
        {
            displayName = "ProbeVolumes",
            referenceName = "PROBE_VOLUMES",
            type = KeywordType.Enum,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
            entries = new KeywordEntry[]
            {
                new KeywordEntry() { displayName = "Off", referenceName = "" },
                new KeywordEntry() { displayName = "L1", referenceName = "L1" },
                new KeywordEntry() { displayName = "L2", referenceName = "L2" },
            },
            stages = KeywordShaderStage.FragmentAndRaytracing,
        };

        public static KeywordDescriptor LodFadeCrossfade = new KeywordDescriptor()
        {
            displayName = "LOD Fade Crossfade",
            referenceName = "LOD_FADE_CROSSFADE",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };

        public static KeywordDescriptor HasLightloop = new KeywordDescriptor()
        {
            displayName = "Has Lightloop",
            referenceName = "HAS_LIGHTLOOP",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.Predefined,
            scope = KeywordScope.Global,
        };

        public static KeywordDescriptor LightList = new KeywordDescriptor()
        {
            displayName = "Light List",
            referenceName = "USE",
            type = KeywordType.Enum,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
            entries = new KeywordEntry[]
            {
                new KeywordEntry() { displayName = "FPTL", referenceName = "FPTL_LIGHTLIST" },
                new KeywordEntry() { displayName = "Clustered", referenceName = "CLUSTERED_LIGHTLIST" },
            },
            stages = KeywordShaderStage.Fragment,
        };

        public static KeywordDescriptor DisableLightloopTileAndCluster = new KeywordDescriptor()
        {
            displayName = "Disable Lightloop Tile and Cluster",
            referenceName = "LIGHTLOOP_DISABLE_TILE_AND_CLUSTER",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.Predefined,
            scope = KeywordScope.Global,
        };

        public static KeywordDescriptor PathTracingclusteredDecals = new KeywordDescriptor()
        {
            displayName = "Cluster decals in the HDRP Path Tracer",
            referenceName = "PATH_TRACING_CLUSTERED_DECALS",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.Predefined,
            scope = KeywordScope.Global,
        };

        public static KeywordDescriptor PunctualShadow = new KeywordDescriptor()
        {
            displayName = "PunctualShadow",
            referenceName = "PUNCTUAL_SHADOW",
            type = KeywordType.Enum,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
            entries = new KeywordEntry[]
            {
                new KeywordEntry() { displayName = "Low", referenceName = "LOW" },
                new KeywordEntry() { displayName = "Medium", referenceName = "MEDIUM" },
                new KeywordEntry() { displayName = "High", referenceName = "HIGH" }
            },
            stages = KeywordShaderStage.Fragment,
        };

        public static KeywordDescriptor DirectionalShadow = new KeywordDescriptor()
        {
            displayName = "DirectionalShadow",
            referenceName = "DIRECTIONAL_SHADOW",
            type = KeywordType.Enum,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
            entries = new KeywordEntry[]
            {
                new KeywordEntry() { displayName = "Low", referenceName = "LOW" },
                new KeywordEntry() { displayName = "Medium", referenceName = "MEDIUM" },
                new KeywordEntry() { displayName = "High", referenceName = "HIGH" }
            },
            stages = KeywordShaderStage.Fragment,
        };

        public static KeywordDescriptor AreaShadow = new KeywordDescriptor()
        {
            displayName = "AreaShadow",
            referenceName = "AREA_SHADOW",
            type = KeywordType.Enum,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
            entries = new KeywordEntry[]
            {
                new KeywordEntry() { displayName = "Medium", referenceName = "MEDIUM" },
                new KeywordEntry() { displayName = "High", referenceName = "HIGH" }
            },
            stages = KeywordShaderStage.Fragment,
        };

        public static KeywordDescriptor SurfaceTypeTransparent = new KeywordDescriptor()
        {
            displayName = "Surface Type Transparent",
            referenceName = "_SURFACE_TYPE_TRANSPARENT",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.ShaderFeature,
            scope = KeywordScope.Global,
        };

        public static KeywordDescriptor ShaderLit = new KeywordDescriptor()
        {
            displayName = "Lit shader",
            referenceName = "SHADER_LIT",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.ShaderFeature,
            scope = KeywordScope.Global,
        };

        public static KeywordDescriptor DoubleSided = new KeywordDescriptor()
        {
            displayName = "Double Sided",
            referenceName = "_DOUBLESIDED_ON",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.ShaderFeature,
            scope = KeywordScope.Local,
        };

        public static KeywordDescriptor FogOnTransparent = new KeywordDescriptor()
        {
            displayName = "Enable Fog On Transparent",
            referenceName = "_ENABLE_FOG_ON_TRANSPARENT",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.ShaderFeature,
            scope = KeywordScope.Local,
            stages = KeywordShaderStage.Fragment,
        };

        public static KeywordDescriptor ScenePickingPass = new KeywordDescriptor()
        {
            displayName = "Scene Picking Pass",
            referenceName = "SCENEPICKINGPASS",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.ShaderFeature,
            scope = KeywordScope.Local,
        };

        public static KeywordDescriptor SceneSelectionPass = new KeywordDescriptor()
        {
            displayName = "Scene Selection Pass",
            referenceName = "SCENESELECTIONPASS",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.ShaderFeature,
            scope = KeywordScope.Local,
        };

        public static KeywordDescriptor Tessellation = new KeywordDescriptor()
        {
            displayName = "Tessellation",
            referenceName = "TESSELLATION_ON",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.ShaderFeature,
            scope = KeywordScope.Local,
        };

        public static KeywordDescriptor SupportGlobalMipBias = new KeywordDescriptor()
        {
            displayName = "Support Global Mip Bias",
            referenceName = "SUPPORT_GLOBAL_MIP_BIAS",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.ShaderFeature,
            scope = KeywordScope.Local,
        };

        public static KeywordDescriptor TessellationModification = new KeywordDescriptor()
        {
            displayName = "Tessellation Modification",
            referenceName = "HAVE_TESSELLATION_MODIFICATION",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.ShaderFeature,
            scope = KeywordScope.Local,
        };

        public static KeywordDescriptor TessellationMode = new KeywordDescriptor()
        {
            displayName = "Tessellation Mode",
            referenceName = "_TESSELLATION_PHONG",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.ShaderFeature,
            scope = KeywordScope.Local,
            stages = KeywordShaderStage.Domain,
        };

        public static KeywordDescriptor TransparentDepthPrepass = new KeywordDescriptor()
        {
            displayName = "Transparent Depth Prepass",
            referenceName = "CUTOFF_TRANSPARENT_DEPTH_PREPASS",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.ShaderFeature,
            scope = KeywordScope.Local,
        };

        public static KeywordDescriptor TransparentDepthPostpass = new KeywordDescriptor()
        {
            displayName = "Transparent Depth Postpass",
            referenceName = "CUTOFF_TRANSPARENT_DEPTH_POSTPASS",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.ShaderFeature,
            scope = KeywordScope.Local,
        };

        public static KeywordDescriptor SkipRasterizedShadows = new KeywordDescriptor()
        {
            displayName = "Skip Rasterized Shadows",
            referenceName = "SKIP_RASTERIZED_SHADOWS",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
            stages = KeywordShaderStage.Fragment,
        };

        public static KeywordDescriptor AlphaTest = new KeywordDescriptor()
        {
            displayName = "Alpha Test",
            referenceName = "_ALPHATEST_ON",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.ShaderFeature,
            scope = KeywordScope.Local
        };

        public static KeywordDescriptor TransparentColorShadow = new KeywordDescriptor()
        {
            displayName = "Transparent Color Shadow",
            referenceName = "TRANSPARENT_COLOR_SHADOW",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };

        public static KeywordDescriptor RaytraceMinimalGBuffer = new KeywordDescriptor()
        {
            displayName = "Minimal GBuffer",
            referenceName = "MINIMAL_GBUFFER",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };

        public static KeywordDescriptor DisableDecals = new KeywordDescriptor
        {
            displayName = "Disable Decals",
            referenceName = "_DISABLE_DECALS",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.ShaderFeature,
            scope = KeywordScope.Local,
            stages = KeywordShaderStage.FragmentAndRaytracing,
        };

        public static KeywordDescriptor DecalSurfaceGradient = new KeywordDescriptor
        {
            displayName = "Additive normal blending",
            referenceName = "DECAL_SURFACE_GRADIENT",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
            stages = KeywordShaderStage.Fragment,
        };

        public static KeywordDescriptor DecalSurfaceGradientRayTracing = new KeywordDescriptor
        {
            displayName = "Additive normal blending",
            referenceName = "DECAL_SURFACE_GRADIENT",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
            stages = KeywordShaderStage.RayTracing,
        };

        public static KeywordDescriptor DisableSSR = new KeywordDescriptor
        {
            displayName = "Disable SSR",
            referenceName = "_DISABLE_SSR",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.ShaderFeature,
            scope = KeywordScope.Local,
            stages = KeywordShaderStage.FragmentAndRaytracing,
        };

        public static KeywordDescriptor DisableSSRTransparent = new KeywordDescriptor
        {
            displayName = "Disable SSR Transparent",
            referenceName = "_DISABLE_SSR_TRANSPARENT",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.ShaderFeature,
            scope = KeywordScope.Local,
            stages = KeywordShaderStage.FragmentAndRaytracing,
        };

        public static KeywordDescriptor EnableGeometricSpecularAA = new KeywordDescriptor
        {
            displayName = "Enable Geometric Specular AA",
            referenceName = "_ENABLE_GEOMETRIC_SPECULAR_AA",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.ShaderFeature,
            scope = KeywordScope.Local,
            stages = KeywordShaderStage.Fragment,
        };

        public static KeywordDescriptor SupportBlendModePreserveSpecularLighting = new KeywordDescriptor
        {
            displayName = "BlendMode Preserve Specular Lighting",
            referenceName = "SUPPORT_BLENDMODE_PRESERVE_SPECULAR_LIGHTING",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.Predefined,
            scope = KeywordScope.Global,
        };

        public static KeywordDescriptor AddPrecomputedVelocity = new KeywordDescriptor
        {
            displayName = "Add Precomputed Velocity",
            referenceName = "_ADD_PRECOMPUTED_VELOCITY",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.ShaderFeature,
            scope = KeywordScope.Local,
        };

        public static KeywordDescriptor TransparentWritesMotionVector = new KeywordDescriptor
        {
            displayName = "Transparent Writes Motion Vector",
            referenceName = "_TRANSPARENT",
            type = KeywordType.Enum,
            definition = KeywordDefinition.ShaderFeature,
            scope = KeywordScope.Local,
            entries = new KeywordEntry[]
            {
                new KeywordEntry() { displayName = "Off", referenceName = "" },
                new KeywordEntry() { displayName = "Writes Motion Vector", referenceName = "WRITES_MOTION_VEC" },
                new KeywordEntry() { displayName = "Per Pixel Sorting", referenceName = "REFRACTIVE_SORT" },
            }
        };

        public static KeywordDescriptor DepthOffset = new KeywordDescriptor
        {
            displayName = "Depth Offset",
            referenceName = "_DEPTHOFFSET_ON",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.ShaderFeature,
            scope = KeywordScope.Local,
            stages = KeywordShaderStage.Fragment
        };

        public static KeywordDescriptor ConservativeDepthOffset = new KeywordDescriptor
        {
            displayName = "Conservative Depth Offset",
            referenceName = "_CONSERVATIVE_DEPTH_OFFSET",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.ShaderFeature,
            scope = KeywordScope.Local,
            stages = KeywordShaderStage.Fragment
        };

        public static KeywordDescriptor multiBounceIndirect = new KeywordDescriptor
        {
            displayName = "Multi Bounce Indirect",
            referenceName = "MULTI_BOUNCE_INDIRECT",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };

        public static KeywordDescriptor EditorVisualization = new KeywordDescriptor
        {
            displayName = "Editor Visualization",
            referenceName = "EDITOR_VISUALIZATION",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.ShaderFeature,
            scope = KeywordScope.Global,
        };

        public static KeywordDescriptor ForceEnableTransparent = new KeywordDescriptor
        {
            displayName = "Force Enable Transparent",
            referenceName = "_SURFACE_TYPE_TRANSPARENT",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.Predefined,
            scope = KeywordScope.Global,
        };

        public static KeywordDescriptor LineRenderingOffscreenShading = new KeywordDescriptor
        {
            displayName = "Line Rendering Offscreen Shading",
            referenceName = "LINE_RENDERING_OFFSCREEN_SHADING",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.Predefined,
            scope = KeywordScope.Global,
        };

        public static KeywordDescriptor Native16Bit = new KeywordDescriptor()
        {
            displayName = "Native 16 Bit",
            referenceName = "UNITY_DEVICE_SUPPORTS_NATIVE_16BIT",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };

        public static  KeywordDescriptor UseLegacyLightmaps = new KeywordDescriptor()
        {
            displayName = "Use Legacy Lightmaps",
            referenceName = "USE_LEGACY_LIGHTMAPS",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global
        };

        public static KeywordDescriptor LightmapBicubicSampling = new KeywordDescriptor()
        {
            displayName = "Lightmap Bicubic Sampling",
            referenceName = "LIGHTMAP_BICUBIC_SAMPLING",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global
        };
    }
    #endregion

    #region CustomInterpolators
    static class CoreCustomInterpolators
    {
        public static readonly CustomInterpSubGen.Collection Common = new CustomInterpSubGen.Collection
        {
            CustomInterpSubGen.Descriptor.MakeStruct(CustomInterpSubGen.Splice.k_splicePreInclude, "CustomInterpolators", "USE_CUSTOMINTERP_SUBSTRUCT"),
            CustomInterpSubGen.Descriptor.MakeBlock("CustomInterpolatorVertMeshCustomInterpolation", "varyings", "vertexDescription"),
            CustomInterpSubGen.Descriptor.MakeMacroBlock("CustomInterpolatorInterpolateWithBaryCoordsMeshToDS", "TESSELLATION_INTERPOLATE_BARY(", ", baryCoords)"),
            CustomInterpSubGen.Descriptor.MakeBlock("CustomInterpolatorVertMeshTesselationCustomInterpolation", "output", "input"),
            CustomInterpSubGen.Descriptor.MakeBlock("CustomInterpolatorVaryingsToFragInputs", "output.customInterpolators", "input"),
            CustomInterpSubGen.Descriptor.MakeBlock(CustomInterpSubGen.Splice.k_spliceCopyToSDI, "output", "input.customInterpolators")
        };
    }

    #endregion



}
