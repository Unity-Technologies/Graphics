using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.VFX;
using System.Text;

namespace UnityEditor.VFX
{
    class VFXDataMesh : VFXData
    {
        [SerializeField, FormerlySerializedAs("shader")]
        private Shader m_Shader;

        [SerializeField]
        private string m_ShaderName;

        public Shader shader
        {
            get {
                //This is needed for standard shaders ( for instance Unlit/Color ) that are not deserialized correctly during first import.
                if (m_Shader == null && !object.ReferenceEquals(m_Shader,null) && !string.IsNullOrEmpty(m_ShaderName))
                {
                    Shader newShader = Shader.Find(m_ShaderName);
                    if (newShader != null)
                        m_Shader = newShader;
                }
                return m_Shader;
            }
            set
            {
                m_Shader = value;
                DestroyCachedMaterial();
                if( m_Shader != null)
                    m_ShaderName = m_Shader.name;
            }
        }

        private Material m_CachedMaterial = null; // Transient material used to retrieve key words and properties

        public override VFXDataType type { get { return VFXDataType.Mesh; } }

        public override void OnEnable()
        {
            base.OnEnable();

            if (object.ReferenceEquals(shader,null)) shader = VFXResources.defaultResources.shader;

            if( m_Shader != null)
            {
                if(m_ShaderName != m_Shader.name )
                {
                    m_ShaderName = m_Shader.name;
                    EditorUtility.SetDirty(this);
                }
            }
        }

        public void RefreshShader()
        {
            DestroyCachedMaterial();
            Invalidate(InvalidationCause.kSettingChanged);
        }

        private void DestroyCachedMaterial()
        {
            Material.DestroyImmediate(m_CachedMaterial);
            m_CachedMaterial = null;
        }

        public void OnDisable()
        {
            DestroyCachedMaterial();
        }

        public override void CopySettings<T>(T dst)
        {
            VFXDataMesh other = dst as VFXDataMesh;
            if (other != null)
                other.shader = shader;
        }

        public override VFXDeviceTarget GetCompilationTarget(VFXContext context)
        {
            return VFXDeviceTarget.GPU;
        }

        public override bool CanBeCompiled()
        {
            return shader != null && m_Owners.Count == 1;
        }

        public Material GetOrCreateMaterial()
        {
            if (m_CachedMaterial == null && shader != null)
            {
                m_CachedMaterial = new Material(shader);
                m_CachedMaterial.hideFlags = HideFlags.HideAndDontSave;
                VFXLibrary.currentSRPBinder.SetupMaterial(m_CachedMaterial);
            }

            return m_CachedMaterial;
        }

        public override void FillDescs(
            List<VFXGPUBufferDesc> outBufferDescs,
            List<VFXTemporaryGPUBufferDesc> outTemporaryBufferDescs,
            List<VFXEditorSystemDesc> outSystemDescs,
            VFXExpressionGraph expressionGraph,
            Dictionary<VFXContext, VFXContextCompiledData> contextToCompiledData,
            Dictionary<VFXContext, int> contextSpawnToBufferIndex,
            VFXDependentBuffersData dependentBuffers,
            Dictionary<VFXContext, List<VFXContextLink>[]> effectiveFlowInputLinks,
            VFXSystemNames systemNames = null)
        {
            var context = m_Owners[0];
            var contextData = contextToCompiledData[context];

            var mappings = new List<VFXMapping>();
            foreach (var uniform in contextData.uniformMapper.uniforms.Concat(contextData.uniformMapper.textures))
            {
                int exprIndex = expressionGraph.GetFlattenedIndex(uniform);
                foreach (var name in contextData.uniformMapper.GetNames(uniform))
                    mappings.Add(new VFXMapping(name, exprIndex));
            }

            var paramList = new List<VFXMapping>(contextData.parameters);

            // TODO Remove once material are serialized
            {
                var mat = GetOrCreateMaterial();
                var keywordsStr = new StringBuilder();

                foreach (var k in mat.shaderKeywords)
                {
                    keywordsStr.Append(k);
                    keywordsStr.Append(' ');
                }

                const int kKeywordID = 0x5a93713b;
                paramList.Add(new VFXMapping(keywordsStr.ToString(), kKeywordID));

                // Add material properties mappings
                for (int i = 0; i < ShaderUtil.GetPropertyCount(shader); ++i)
                {
                    if (ShaderUtil.IsShaderPropertyHidden(shader, i))
                    {
                        var name = ShaderUtil.GetPropertyName(shader, i);
                        var propExp = contextData.cpuMapper.FromNameAndId(name, -1);
                        if (propExp != null)
                        {
                            int propIndex = expressionGraph.GetFlattenedIndex(propExp);
                            if (propIndex != -1)
                                paramList.Add(new VFXMapping(name, propIndex));
                        }
                    }
                }
            }

            var task = new VFXEditorTaskDesc()
            {
                externalProcessor = shader,
                values = mappings.ToArray(),
                parameters = paramList.ToArray(),
                type = (UnityEngine.VFX.VFXTaskType)VFXTaskType.Output
            };

            mappings.Clear();
            var mapper = contextData.cpuMapper;

            // TODO Factorize that
            var meshExp = mapper.FromNameAndId("mesh", -1);
            var transformExp = mapper.FromNameAndId("transform", -1);
            var subMaskExp = mapper.FromNameAndId("subMeshMask", -1);

            int meshIndex = meshExp != null ? expressionGraph.GetFlattenedIndex(meshExp) : -1;
            int transformIndex = transformExp != null ? expressionGraph.GetFlattenedIndex(transformExp) : -1;
            int subMaskIndex = subMaskExp != null ? expressionGraph.GetFlattenedIndex(subMaskExp) : -1;

            if (meshIndex != -1)
                mappings.Add(new VFXMapping("mesh", meshIndex));
            if (transformIndex != -1)
                mappings.Add(new VFXMapping("transform", transformIndex));
            if (subMaskIndex != -1)
                mappings.Add(new VFXMapping("subMeshMask", subMaskIndex));

            outSystemDescs.Add(new VFXEditorSystemDesc()
            {
                tasks = new VFXEditorTaskDesc[1] { task },
                values = mappings.ToArray(),
                type = VFXSystemType.Mesh,
                layer = uint.MaxValue,
            });
        }

        public override void GenerateAttributeLayout(Dictionary<VFXContext, List<VFXContextLink>[]> effectiveFlowInputLinks)
        {
        }

        public override string GetAttributeDataDeclaration(VFXAttributeMode mode)
        {
            throw new NotImplementedException();
        }

        public override string GetLoadAttributeCode(VFXAttribute attrib, VFXAttributeLocation location)
        {
            throw new NotImplementedException();
        }

        public override string GetStoreAttributeCode(VFXAttribute attrib, string value)
        {
            throw new NotImplementedException();
        }
    }
}
