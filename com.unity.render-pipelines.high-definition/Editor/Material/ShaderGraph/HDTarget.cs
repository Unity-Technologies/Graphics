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

    enum SpecularOcclusionMode
    {
        Off,
        FromAO,
        FromAOAndBentNormal,
        Custom
    }

    sealed class HDTarget : Target, IHasMetadata, ILegacyTarget
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

        [SerializeField]
        JsonData<SubTarget> m_ActiveSubTarget;

        [SerializeField]
        List<JsonData<HDTargetData>> m_Datas = new List<JsonData<HDTargetData>>();

        [SerializeField]
        string m_CustomEditorGUI;

        public override bool IsNodeAllowedByTarget(Type nodeType)
        {
            SRPFilterAttribute srpFilter = NodeClassCache.GetAttributeOnNodeType<SRPFilterAttribute>(nodeType);
            bool worksWithThisSrp = srpFilter == null || srpFilter.srpTypes.Contains(typeof(HDRenderPipeline));
            return worksWithThisSrp && base.IsNodeAllowedByTarget(nodeType);
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
            if(m_ActiveSubTarget.value == null)
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
            if(m_ActiveSubTarget.value == null)
                return;

            // Setup the active SubTarget
            ProcessSubTargetDatas(m_ActiveSubTarget.value);
            m_ActiveSubTarget.value.target = this;
            m_ActiveSubTarget.value.Setup(ref context);

            // Override EditorGUI
            if(!string.IsNullOrEmpty(m_CustomEditorGUI))
            {
                context.SetDefaultShaderGUI(m_CustomEditorGUI);
            }
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            var descs = context.blocks.Select(x => x.descriptor);
            // Stages
            context.AddField(Fields.GraphVertex,                    descs.Contains(BlockFields.VertexDescription.Position) ||
                                                                    descs.Contains(BlockFields.VertexDescription.Normal) ||
                                                                    descs.Contains(BlockFields.VertexDescription.Tangent));
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
            if(m_ActiveSubTarget.value == null)
                return;

            context.globalIndentLevel++;

            // Core properties
            m_SubTargetField = new PopupField<string>(m_SubTargetNames, activeSubTargetIndex);
            context.AddProperty("Material", m_SubTargetField, (evt) =>
            {
                if (Equals(activeSubTargetIndex, m_SubTargetField.index))
                    return;

                var systemData = m_Datas.SelectValue().FirstOrDefault(x => x is SystemData) as SystemData;
                if(systemData != null)
                {
                    // Force material update hash
                    systemData.materialNeedsUpdateHash = -1;
                }

                m_ActiveSubTarget = m_SubTargets[m_SubTargetField.index];
                ProcessSubTargetDatas(m_ActiveSubTarget.value);
                onChange();
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
            context.AddProperty("Custom Editor GUI", m_CustomGUIField, (evt) => {});

            context.globalIndentLevel--;
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
                if(m_ActiveSubTarget.value is IHasMetadata subTargetHasMetaData)
                    return subTargetHasMetaData.identifier;

                return null;
            }
        }

        public ScriptableObject GetMetadataObject()
        {
            if(m_ActiveSubTarget.value is IHasMetadata subTargetHasMetaData)
                return subTargetHasMetaData.GetMetadataObject();

            return null;
        }

        public bool TrySetActiveSubTarget(Type subTargetType)
        {
            if(!subTargetType.IsSubclassOf(typeof(SubTarget)))
                return false;
            
            foreach(var subTarget in m_SubTargets)
            {
                if(subTarget.GetType().Equals(subTargetType))
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
            var typeCollection = TypeCache.GetTypesDerivedFrom<HDTargetData>();
            foreach(var type in typeCollection)
            {
                // Data requirement interfaces need generic type arguments
                // Therefore we need to use reflections to call the method
                var methodInfo = typeof(HDTarget).GetMethod("SetDataOnSubTarget");
                var genericMethodInfo = methodInfo.MakeGenericMethod(type);
                genericMethodInfo.Invoke(this, new object[] { subTarget });
            }
        }

        void ClearUnusedData()
        {
            for(int i = 0; i < m_Datas.Count; i++)
            {
                var data = m_Datas[i];
                var type = data.value.GetType();

                // Data requirement interfaces need generic type arguments
                // Therefore we need to use reflections to call the method
                var methodInfo = typeof(HDTarget).GetMethod("ValidateDataForSubTarget");
                var genericMethodInfo = methodInfo.MakeGenericMethod(type);
                genericMethodInfo.Invoke(this, new object[] { m_ActiveSubTarget.value, data.value });
            }
        }

        public void SetDataOnSubTarget<T>(SubTarget subTarget) where T : HDTargetData
        {
            if(!(subTarget is IRequiresData<T> requiresData))
                return;
            
            // Ensure data object exists in list
            var data = m_Datas.SelectValue().FirstOrDefault(x => x.GetType().Equals(typeof(T))) as T;
            if(data == null)
            {
                data = Activator.CreateInstance(typeof(T)) as T;
                m_Datas.Add(data);
            }

            // Apply data object to SubTarget
            requiresData.data = data;
        }

        public void ValidateDataForSubTarget<T>(SubTarget subTarget, T data) where T : HDTargetData
        {
            if(!(subTarget is IRequiresData<T> requiresData))
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
            foreach(var type in typeCollection)
            {
                var data = Activator.CreateInstance(type) as HDTargetData;
                m_Datas.Add(data);
            }

            // Process SubTargets
            foreach(var subTarget in m_SubTargets)
            {
                if(!(subTarget is ILegacyTarget legacySubTarget))
                    continue;
                
                // Ensure all SubTargets have any required data to fill out during upgrade
                ProcessSubTargetDatas(subTarget);
                subTarget.target = this;
                
                if(legacySubTarget.TryUpgradeFromMasterNode(masterNode, out blockMap))
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
        public static StructCollection Default = new StructCollection
        {
            { HDStructs.AttributesMesh },
            { HDStructs.VaryingsMeshToPS },
            { Structs.SurfaceDescriptionInputs },
            { Structs.VertexDescriptionInputs },
        };
    }
#endregion

#region FieldDependencies
    static class CoreFieldDependencies
    {
        public static DependencyCollection Varying = new DependencyCollection
        {
            //Standard Varying Dependencies
            new FieldDependency(HDStructFields.VaryingsMeshToPS.positionRWS,                         HDStructFields.AttributesMesh.positionOS),
            new FieldDependency(HDStructFields.VaryingsMeshToPS.normalWS,                            HDStructFields.AttributesMesh.normalOS),
            new FieldDependency(HDStructFields.VaryingsMeshToPS.tangentWS,                           HDStructFields.AttributesMesh.tangentOS),
            new FieldDependency(HDStructFields.VaryingsMeshToPS.texCoord0,                           HDStructFields.AttributesMesh.uv0),
            new FieldDependency(HDStructFields.VaryingsMeshToPS.texCoord1,                           HDStructFields.AttributesMesh.uv1),
            new FieldDependency(HDStructFields.VaryingsMeshToPS.texCoord2,                           HDStructFields.AttributesMesh.uv2),
            new FieldDependency(HDStructFields.VaryingsMeshToPS.texCoord3,                           HDStructFields.AttributesMesh.uv3),
            new FieldDependency(HDStructFields.VaryingsMeshToPS.color,                               HDStructFields.AttributesMesh.color),
            new FieldDependency(HDStructFields.VaryingsMeshToPS.instanceID,                          HDStructFields.AttributesMesh.instanceID),
        };

        public static DependencyCollection Tessellation = new DependencyCollection
        {
            //Tessellation Varying Dependencies
            new FieldDependency(HDStructFields.VaryingsMeshToPS.positionRWS,                         HDStructFields.VaryingsMeshToDS.positionRWS),
            new FieldDependency(HDStructFields.VaryingsMeshToPS.normalWS,                            HDStructFields.VaryingsMeshToDS.normalWS),
            new FieldDependency(HDStructFields.VaryingsMeshToPS.tangentWS,                           HDStructFields.VaryingsMeshToDS.tangentWS),
            new FieldDependency(HDStructFields.VaryingsMeshToPS.texCoord0,                           HDStructFields.VaryingsMeshToDS.texCoord0),
            new FieldDependency(HDStructFields.VaryingsMeshToPS.texCoord1,                           HDStructFields.VaryingsMeshToDS.texCoord1),
            new FieldDependency(HDStructFields.VaryingsMeshToPS.texCoord2,                           HDStructFields.VaryingsMeshToDS.texCoord2),
            new FieldDependency(HDStructFields.VaryingsMeshToPS.texCoord3,                           HDStructFields.VaryingsMeshToDS.texCoord3),
            new FieldDependency(HDStructFields.VaryingsMeshToPS.color,                               HDStructFields.VaryingsMeshToDS.color),
            new FieldDependency(HDStructFields.VaryingsMeshToPS.instanceID,                          HDStructFields.VaryingsMeshToDS.instanceID),

            //Tessellation Varying Dependencies, TODO: Why is this loop created?
            new FieldDependency(HDStructFields.VaryingsMeshToDS.tangentWS,                           HDStructFields.VaryingsMeshToPS.tangentWS),
            new FieldDependency(HDStructFields.VaryingsMeshToDS.texCoord0,                           HDStructFields.VaryingsMeshToPS.texCoord0),
            new FieldDependency(HDStructFields.VaryingsMeshToDS.texCoord1,                           HDStructFields.VaryingsMeshToPS.texCoord1),
            new FieldDependency(HDStructFields.VaryingsMeshToDS.texCoord2,                           HDStructFields.VaryingsMeshToPS.texCoord2),
            new FieldDependency(HDStructFields.VaryingsMeshToDS.texCoord3,                           HDStructFields.VaryingsMeshToPS.texCoord3),
            new FieldDependency(HDStructFields.VaryingsMeshToDS.color,                               HDStructFields.VaryingsMeshToPS.color),
            new FieldDependency(HDStructFields.VaryingsMeshToDS.instanceID,                          HDStructFields.VaryingsMeshToPS.instanceID),
        };

        public static DependencyCollection FragInput = new DependencyCollection
        {
            //FragInput dependencies
            new FieldDependency(HDStructFields.FragInputs.positionRWS,                               HDStructFields.VaryingsMeshToPS.positionRWS),
            new FieldDependency(HDStructFields.FragInputs.tangentToWorld,                            HDStructFields.VaryingsMeshToPS.tangentWS),
            new FieldDependency(HDStructFields.FragInputs.tangentToWorld,                            HDStructFields.VaryingsMeshToPS.normalWS),
            new FieldDependency(HDStructFields.FragInputs.texCoord0,                                 HDStructFields.VaryingsMeshToPS.texCoord0),
            new FieldDependency(HDStructFields.FragInputs.texCoord1,                                 HDStructFields.VaryingsMeshToPS.texCoord1),
            new FieldDependency(HDStructFields.FragInputs.texCoord2,                                 HDStructFields.VaryingsMeshToPS.texCoord2),
            new FieldDependency(HDStructFields.FragInputs.texCoord3,                                 HDStructFields.VaryingsMeshToPS.texCoord3),
            new FieldDependency(HDStructFields.FragInputs.color,                                     HDStructFields.VaryingsMeshToPS.color),
        };

        public static DependencyCollection VertexDescription = new DependencyCollection
        {
            //Vertex Description Dependencies
            new FieldDependency(StructFields.VertexDescriptionInputs.ObjectSpaceNormal,              HDStructFields.AttributesMesh.normalOS),
            new FieldDependency(StructFields.VertexDescriptionInputs.WorldSpaceNormal,               HDStructFields.AttributesMesh.normalOS),
            new FieldDependency(StructFields.VertexDescriptionInputs.ViewSpaceNormal,                StructFields.VertexDescriptionInputs.WorldSpaceNormal),

            new FieldDependency(StructFields.VertexDescriptionInputs.ObjectSpaceTangent,             HDStructFields.AttributesMesh.tangentOS),
            new FieldDependency(StructFields.VertexDescriptionInputs.WorldSpaceTangent,              HDStructFields.AttributesMesh.tangentOS),
            new FieldDependency(StructFields.VertexDescriptionInputs.ViewSpaceTangent,               StructFields.VertexDescriptionInputs.WorldSpaceTangent),

            new FieldDependency(StructFields.VertexDescriptionInputs.ObjectSpaceBiTangent,           HDStructFields.AttributesMesh.normalOS),
            new FieldDependency(StructFields.VertexDescriptionInputs.ObjectSpaceBiTangent,           HDStructFields.AttributesMesh.tangentOS),
            new FieldDependency(StructFields.VertexDescriptionInputs.WorldSpaceBiTangent,            StructFields.VertexDescriptionInputs.ObjectSpaceBiTangent),
            new FieldDependency(StructFields.VertexDescriptionInputs.ViewSpaceBiTangent,             StructFields.VertexDescriptionInputs.WorldSpaceBiTangent),

            new FieldDependency(StructFields.VertexDescriptionInputs.ObjectSpacePosition,            HDStructFields.AttributesMesh.positionOS),
            new FieldDependency(StructFields.VertexDescriptionInputs.WorldSpacePosition,             HDStructFields.AttributesMesh.positionOS),
            new FieldDependency(StructFields.VertexDescriptionInputs.AbsoluteWorldSpacePosition,     HDStructFields.AttributesMesh.positionOS),
            new FieldDependency(StructFields.VertexDescriptionInputs.ViewSpacePosition,              StructFields.VertexDescriptionInputs.WorldSpacePosition),

            new FieldDependency(StructFields.VertexDescriptionInputs.WorldSpaceViewDirection,        StructFields.VertexDescriptionInputs.WorldSpacePosition),
            new FieldDependency(StructFields.VertexDescriptionInputs.ObjectSpaceViewDirection,       StructFields.VertexDescriptionInputs.WorldSpaceViewDirection),
            new FieldDependency(StructFields.VertexDescriptionInputs.ViewSpaceViewDirection,         StructFields.VertexDescriptionInputs.WorldSpaceViewDirection),
            new FieldDependency(StructFields.VertexDescriptionInputs.TangentSpaceViewDirection,      StructFields.VertexDescriptionInputs.WorldSpaceViewDirection),
            new FieldDependency(StructFields.VertexDescriptionInputs.TangentSpaceViewDirection,      StructFields.VertexDescriptionInputs.WorldSpaceTangent),
            new FieldDependency(StructFields.VertexDescriptionInputs.TangentSpaceViewDirection,      StructFields.VertexDescriptionInputs.WorldSpaceBiTangent),
            new FieldDependency(StructFields.VertexDescriptionInputs.TangentSpaceViewDirection,      StructFields.VertexDescriptionInputs.WorldSpaceNormal),

            new FieldDependency(StructFields.VertexDescriptionInputs.ScreenPosition,                 StructFields.VertexDescriptionInputs.WorldSpacePosition),
            new FieldDependency(StructFields.VertexDescriptionInputs.uv0,                            HDStructFields.AttributesMesh.uv0),
            new FieldDependency(StructFields.VertexDescriptionInputs.uv1,                            HDStructFields.AttributesMesh.uv1),
            new FieldDependency(StructFields.VertexDescriptionInputs.uv2,                            HDStructFields.AttributesMesh.uv2),
            new FieldDependency(StructFields.VertexDescriptionInputs.uv3,                            HDStructFields.AttributesMesh.uv3),
            new FieldDependency(StructFields.VertexDescriptionInputs.VertexColor,                    HDStructFields.AttributesMesh.color),

            new FieldDependency(StructFields.VertexDescriptionInputs.BoneWeights,                    HDStructFields.AttributesMesh.weights),
            new FieldDependency(StructFields.VertexDescriptionInputs.BoneIndices,                    HDStructFields.AttributesMesh.indices),
            new FieldDependency(StructFields.VertexDescriptionInputs.VertexID,                       HDStructFields.AttributesMesh.vertexID),
        };

        public static DependencyCollection SurfaceDescription = new DependencyCollection
        {
            //Surface Description Dependencies
            new FieldDependency(StructFields.SurfaceDescriptionInputs.WorldSpaceNormal,              HDStructFields.FragInputs.tangentToWorld),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.ObjectSpaceNormal,             StructFields.SurfaceDescriptionInputs.WorldSpaceNormal),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.ViewSpaceNormal,               StructFields.SurfaceDescriptionInputs.WorldSpaceNormal),

            new FieldDependency(StructFields.SurfaceDescriptionInputs.WorldSpaceTangent,             HDStructFields.FragInputs.tangentToWorld),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.ObjectSpaceTangent,            StructFields.SurfaceDescriptionInputs.WorldSpaceTangent),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.ViewSpaceTangent,              StructFields.SurfaceDescriptionInputs.WorldSpaceTangent),

            new FieldDependency(StructFields.SurfaceDescriptionInputs.WorldSpaceBiTangent,           HDStructFields.FragInputs.tangentToWorld),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.ObjectSpaceBiTangent,          StructFields.SurfaceDescriptionInputs.WorldSpaceBiTangent),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.ViewSpaceBiTangent,            StructFields.SurfaceDescriptionInputs.WorldSpaceBiTangent),

            new FieldDependency(StructFields.SurfaceDescriptionInputs.WorldSpacePosition,            HDStructFields.FragInputs.positionRWS),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.AbsoluteWorldSpacePosition,    HDStructFields.FragInputs.positionRWS),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.ObjectSpacePosition,           HDStructFields.FragInputs.positionRWS),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.ViewSpacePosition,             HDStructFields.FragInputs.positionRWS),

            new FieldDependency(StructFields.SurfaceDescriptionInputs.WorldSpaceViewDirection,       HDStructFields.FragInputs.positionRWS),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.ObjectSpaceViewDirection,      StructFields.SurfaceDescriptionInputs.WorldSpaceViewDirection),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.ViewSpaceViewDirection,        StructFields.SurfaceDescriptionInputs.WorldSpaceViewDirection),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.TangentSpaceViewDirection,     StructFields.SurfaceDescriptionInputs.WorldSpaceViewDirection),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.TangentSpaceViewDirection,     StructFields.SurfaceDescriptionInputs.WorldSpaceTangent),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.TangentSpaceViewDirection,     StructFields.SurfaceDescriptionInputs.WorldSpaceBiTangent),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.TangentSpaceViewDirection,     StructFields.SurfaceDescriptionInputs.WorldSpaceNormal),

            new FieldDependency(StructFields.SurfaceDescriptionInputs.ScreenPosition,                StructFields.SurfaceDescriptionInputs.WorldSpacePosition),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.uv0,                           HDStructFields.FragInputs.texCoord0),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.uv1,                           HDStructFields.FragInputs.texCoord1),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.uv2,                           HDStructFields.FragInputs.texCoord2),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.uv3,                           HDStructFields.FragInputs.texCoord3),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.VertexColor,                   HDStructFields.FragInputs.color),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.FaceSign,                      HDStructFields.FragInputs.IsFrontFace),
        };

        public static DependencyCollection Default = new DependencyCollection
        {
            { Varying },
            { Tessellation },
            { FragInput },
            { VertexDescription },
            { SurfaceDescription },
        };
    }
#endregion

#region RequiredFields
    static class CoreRequiredFields
    {
        public static FieldCollection Meta = new FieldCollection()
        {
            HDStructFields.AttributesMesh.normalOS,
            HDStructFields.AttributesMesh.tangentOS,
            HDStructFields.AttributesMesh.uv0,
            HDStructFields.AttributesMesh.uv1,
            HDStructFields.AttributesMesh.color,
            HDStructFields.AttributesMesh.uv2,
        };

        public static FieldCollection PositionRWS = new FieldCollection()
        {
            HDStructFields.VaryingsMeshToPS.positionRWS,
        };

        public static FieldCollection LitMinimal = new FieldCollection()
        {
            HDStructFields.FragInputs.tangentToWorld,
            HDStructFields.FragInputs.positionRWS,
            HDStructFields.FragInputs.texCoord1,
            HDStructFields.FragInputs.texCoord2,
        };

        public static FieldCollection LitFull = new FieldCollection()
        {
            HDStructFields.AttributesMesh.normalOS,
            HDStructFields.AttributesMesh.tangentOS,
            HDStructFields.AttributesMesh.uv0,
            HDStructFields.AttributesMesh.uv1,
            HDStructFields.AttributesMesh.color,
            HDStructFields.AttributesMesh.uv2,
            HDStructFields.AttributesMesh.uv3,
            HDStructFields.FragInputs.tangentToWorld,
            HDStructFields.FragInputs.positionRWS,
            HDStructFields.FragInputs.texCoord1,
            HDStructFields.FragInputs.texCoord2,
            HDStructFields.FragInputs.texCoord3,
            HDStructFields.FragInputs.color,
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
            public static readonly string alphaToMask = "[_AlphaToMask]";
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
            { RenderState.ColorMask("ColorMask 0") },
        };

        public static RenderStateCollection DepthOnly = new RenderStateCollection
        {
            { RenderState.Cull(Uniforms.cullMode) },
            { RenderState.ZWrite(ZWrite.On) },
            { RenderState.AlphaToMask(Uniforms.alphaToMask), new FieldCondition(Fields.AlphaToMask, true) },
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
            { RenderState.AlphaToMask(Uniforms.alphaToMask), new FieldCondition(Fields.AlphaToMask, true) },
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
            { RenderState.Cull(Cull.Front) },
            { RenderState.ZWrite(Uniforms.zWrite) },
            { RenderState.ZTest(Uniforms.zTestTransparent) },
            { RenderState.ColorMask("ColorMask [_ColorMaskTransparentVel] 1") },
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
            { RenderState.Cull(Uniforms.cullModeForward) },
            { RenderState.ZWrite(Uniforms.zWrite) },
            { RenderState.ZTest(Uniforms.zTestDepthEqualForOpaque) },
            { RenderState.ColorMask("ColorMask [_ColorMaskTransparentVel] 1") },
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
        public static PragmaCollection Basic = new PragmaCollection
        {
            { Pragma.Target(ShaderModel.Target45) },
            { Pragma.Vertex("Vert") },
            { Pragma.Fragment("Frag") },
            { Pragma.OnlyRenderers(PragmaRenderers.GetHighEndPlatformArray()) },
        };

        public static PragmaCollection InstancedRenderingLayer = new PragmaCollection
        {
            { Basic },
            { Pragma.MultiCompileInstancing },
            { Pragma.InstancingOptions(InstancingOptions.RenderingLayer) },
        };

        public static PragmaCollection InstancedRenderingLayerEditorSync = new PragmaCollection
        {
            { Basic },
            { Pragma.MultiCompileInstancing },
            { Pragma.InstancingOptions(InstancingOptions.RenderingLayer) },
            { Pragma.EditorSyncCompilation },
        };

        public static PragmaCollection DotsInstancedInV2Only = new PragmaCollection
        {
            { Basic },
            { Pragma.MultiCompileInstancing },
            { Pragma.InstancingOptions(InstancingOptions.RenderingLayer) },
            #if ENABLE_HYBRID_RENDERER_V2
            { Pragma.DOTSInstancing },
            { Pragma.InstancingOptions(InstancingOptions.NoLodFade) },
            #endif
        };

        public static PragmaCollection DotsInstancedInV2OnlyEditorSync = new PragmaCollection
        {
            { Basic },
            { Pragma.MultiCompileInstancing },
            { Pragma.EditorSyncCompilation },
            { Pragma.InstancingOptions(InstancingOptions.RenderingLayer) },
            #if ENABLE_HYBRID_RENDERER_V2
            { Pragma.DOTSInstancing },
            { Pragma.InstancingOptions(InstancingOptions.NoLodFade) },
            #endif
        };

        public static PragmaCollection DotsInstancedInV1AndV2 = new PragmaCollection
        {
            { Basic },
            { Pragma.MultiCompileInstancing },
            // Hybrid Renderer V2 requires a completely different set of pragmas from Hybrid V1
            #if ENABLE_HYBRID_RENDERER_V2
            { Pragma.DOTSInstancing },
            { Pragma.InstancingOptions(InstancingOptions.NoLodFade) },
            { Pragma.InstancingOptions(InstancingOptions.RenderingLayer) },
            #else
            { Pragma.InstancingOptions(InstancingOptions.NoLightProbe), new FieldCondition(HDFields.DotsInstancing, true) },
            { Pragma.InstancingOptions(InstancingOptions.NoLightProbe), new FieldCondition(HDFields.DotsProperties, true) },
            { Pragma.InstancingOptions(InstancingOptions.NoLodFade),    new FieldCondition(HDFields.DotsInstancing, true) },
            { Pragma.InstancingOptions(InstancingOptions.NoLodFade),    new FieldCondition(HDFields.DotsProperties, true) },
            { Pragma.InstancingOptions(InstancingOptions.RenderingLayer), new FieldCondition[]
            {
                new FieldCondition(HDFields.DotsInstancing, false),
                new FieldCondition(HDFields.DotsProperties, false),
            } },
            #endif
        };

        public static PragmaCollection DotsInstancedInV1AndV2EditorSync = new PragmaCollection
        {
            { Basic },
            { Pragma.MultiCompileInstancing },
            { Pragma.EditorSyncCompilation },
            // Hybrid Renderer V2 requires a completely different set of pragmas from Hybrid V1
            #if ENABLE_HYBRID_RENDERER_V2
            { Pragma.DOTSInstancing },
            { Pragma.InstancingOptions(InstancingOptions.NoLodFade) },
            { Pragma.InstancingOptions(InstancingOptions.RenderingLayer) },
            #else
            { Pragma.InstancingOptions(InstancingOptions.NoLightProbe), new FieldCondition(HDFields.DotsInstancing, true) },
            { Pragma.InstancingOptions(InstancingOptions.NoLightProbe), new FieldCondition(HDFields.DotsProperties, true) },
            { Pragma.InstancingOptions(InstancingOptions.NoLodFade),    new FieldCondition(HDFields.DotsInstancing, true) },
            { Pragma.InstancingOptions(InstancingOptions.NoLodFade),    new FieldCondition(HDFields.DotsProperties, true) },
            { Pragma.InstancingOptions(InstancingOptions.RenderingLayer), new FieldCondition[]
            {
                new FieldCondition(HDFields.DotsInstancing, false),
                new FieldCondition(HDFields.DotsProperties, false),
            } },
            #endif
        };

        public static PragmaCollection RaytracingBasic = new PragmaCollection
        {
            { Pragma.Target(ShaderModel.Target50) },
            { Pragma.Raytracing("surface_shader") },
            { Pragma.OnlyRenderers(new Platform[] {Platform.D3D11}) },
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
        public static DefineCollection ScenePicking = new DefineCollection
        {
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
        public const string kEyeRaytracing = "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Eye/EyeRaytracing.hlsl";
        public const string kStackLitRaytracing = "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/StackLit/StackLitRaytracing.hlsl";
        public const string kHairRaytracing = "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Hair/HairRaytracing.hlsl";
        public const string kRaytracingLightLoop = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingLightLoop.hlsl";
        public const string kRaytracingCommon = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingCommon.hlsl";
        public const string kNormalBuffer = "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/NormalBuffer.hlsl";

        // Postgraph Raytracing
        public const string kPassRaytracingIndirect = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassRaytracingIndirect.hlsl";
        public const string kPassRaytracingVisbility = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassRaytracingVisibility.hlsl";
        public const string kPassRaytracingForward = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassRaytracingForward.hlsl";
        public const string kPassRaytracingGBuffer = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderpassRaytracingGBuffer.hlsl";
        public const string kPassPathTracing = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassPathTracing.hlsl";
        public const string kPassRaytracingSubSurface = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderpassRaytracingSubSurface.hlsl";

        // Public Pregraph Function
        public const string kCommonLighting = "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonLighting.hlsl";
        public const string kHDShadow = "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/HDShadow.hlsl";
        public const string kLightLoopDef = "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl";
        public const string kPunctualLightCommon = "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/PunctualLightCommon.hlsl";
        public const string kHDShadowLoop = "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/HDShadowLoop.hlsl";
        public const string kNormalSurfaceGradient = "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl";
        public const string kLighting = "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl";
        public const string kLightLoop = "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoop.hlsl";
        
        // Public Pregraph Material
        public const string kUnlit = "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Unlit/Unlit.hlsl";
        public const string kLit = "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl";
        public const string kFabric = "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Fabric/Fabric.hlsl";
        public const string kHair = "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Hair/Hair.hlsl";
        public const string kStackLit = "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/StackLit/StackLit.hlsl";

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
            { CoreIncludes.kMaterial, IncludeLocation.Pregraph },
            { CoreIncludes.kShaderVariablesRaytracing, IncludeLocation.Pregraph },
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
        };

        public static KeywordDescriptor WriteDecalBuffer = new KeywordDescriptor()
        {
            displayName = "Write Decal Buffer",
            referenceName = "WRITE_DECAL_BUFFER",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };

        public static KeywordDescriptor DebugDisplay = new KeywordDescriptor()
        {
            displayName = "Debug Display",
            referenceName = "DEBUG_DISPLAY",
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
            scope = KeywordScope.Global,
        };

        public static KeywordDescriptor DirectionalLightmapCombined = new KeywordDescriptor()
        {
            displayName = "Directional Lightmap Combined",
            referenceName = "DIRLIGHTMAP_COMBINED",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };

        public static KeywordDescriptor DynamicLightmap = new KeywordDescriptor()
        {
            displayName = "Dynamic Lightmap",
            referenceName = "DYNAMICLIGHTMAP_ON",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };

        public static KeywordDescriptor ShadowsShadowmask = new KeywordDescriptor()
        {
            displayName = "Shadows Shadowmask",
            referenceName = "SHADOWS_SHADOWMASK",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
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
            }
        };

        public static KeywordDescriptor LightLayers = new KeywordDescriptor()
        {
            displayName = "Light Layers",
            referenceName = "LIGHT_LAYERS",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
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
            }
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
            }
        };

        public static KeywordDescriptor Shadow = new KeywordDescriptor()
        {
            displayName = "Shadow",
            referenceName = "SHADOW",
            type = KeywordType.Enum,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
            entries = new KeywordEntry[]
            {
                new KeywordEntry() { displayName = "Low", referenceName = "LOW" },
                new KeywordEntry() { displayName = "Medium", referenceName = "MEDIUM" },
                new KeywordEntry() { displayName = "High", referenceName = "HIGH" },
            }
        };

        public static KeywordDescriptor SurfaceTypeTransparent = new KeywordDescriptor()
        {
            displayName = "Surface Type Transparent",
            referenceName = "_SURFACE_TYPE_TRANSPARENT",
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

        public static KeywordDescriptor BlendMode = new KeywordDescriptor()
        {
            displayName = "Blend Mode",
            referenceName = "_BLENDMODE",
            type = KeywordType.Enum,
            definition = KeywordDefinition.ShaderFeature,
            scope = KeywordScope.Local,
            entries = new KeywordEntry[]
            {
                new KeywordEntry() { displayName = "Off", referenceName = "OFF" },
                new KeywordEntry() { displayName = "Alpha", referenceName = "ALPHA" },
                new KeywordEntry() { displayName = "Add", referenceName = "ADD" },
                new KeywordEntry() { displayName = "PreMultiply", referenceName = "PRE_MULTIPLY" },
            }
        };

        public static KeywordDescriptor FogOnTransparent = new KeywordDescriptor()
        {
            displayName = "Enable Fog On Transparent",
            referenceName = "_ENABLE_FOG_ON_TRANSPARENT",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.ShaderFeature,
            scope = KeywordScope.Local,
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
        };

        public static KeywordDescriptor AlphaTest = new KeywordDescriptor()
        {
            displayName = "Alpha Test",
            referenceName = "_ALPHATEST_ON",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.ShaderFeature,
            scope = KeywordScope.Local
        };

        public static KeywordDescriptor AlphaToMask = new KeywordDescriptor()
        {
            displayName = "Alpha To Mask",
            referenceName = "_ALPHATOMASK_ON",
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
            scope = KeywordScope.Global
        };

        public static KeywordDescriptor RaytraceMinimalGBuffer = new KeywordDescriptor()
        {
            displayName = "Minimal GBuffer",
            referenceName = "MINIMAL_GBUFFER",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global
        };

        public static KeywordDescriptor DisableDecals = new KeywordDescriptor
        {
            displayName = "Disable Decals",
            referenceName = "_DISABLE_DECALS",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.ShaderFeature,
            scope = KeywordScope.Local,
        };

        public static KeywordDescriptor DisableSSR = new KeywordDescriptor
        {
            displayName = "Disable SSR",
            referenceName = "_DISABLE_SSR",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.ShaderFeature,
            scope = KeywordScope.Local,
        };

        public static KeywordDescriptor DisableSSRTransparent = new KeywordDescriptor
        {
            displayName = "Disable SSR Transparent",
            referenceName = "_DISABLE_SSR_TRANSPARENT",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.ShaderFeature,
            scope = KeywordScope.Local,
        };

        public static KeywordDescriptor EnableGeometricSpecularAA = new KeywordDescriptor
        {
            displayName = "Enable Geometric Specular AA",
            referenceName = "_ENABLE_GEOMETRIC_SPECULAR_AA",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.ShaderFeature,
            scope = KeywordScope.Local,
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
            referenceName = "_TRANSPARENT_WRITES_MOTION_VEC",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.ShaderFeature,
            scope = KeywordScope.Local,
        };

        public static KeywordDescriptor DepthOffset = new KeywordDescriptor
        {
            displayName = "Depth Offset",
            referenceName = "_DEPTHOFFSET_ON",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.ShaderFeature,
            scope = KeywordScope.Local,
        };

        public static KeywordDescriptor multiBounceIndirect = new KeywordDescriptor
        {
            displayName = "Multi Bounce Indirect",
            referenceName = "MULTI_BOUNCE_INDIRECT",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };
    }
#endregion
}
