using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PostProcessingRendererFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class PostProcessingRendererFeatureSettings
    {
        public string volumeComponentName;
        public int volumeComponentIndex;
        public Material effectMaterial;
    }
    
    class FullScreenEffectPass : ScriptableRenderPass
    {
        private readonly PostProcessingRendererFeatureSettings _settings;
        private List<string> _shaderPropRefs;
        private List<FieldInfo> _volumeComponentFields;
        private Type _volumeComponentType;

        public FullScreenEffectPass(PostProcessingRendererFeatureSettings settings)
        {
            _settings = settings;
        }

        public void Setup()
        {
            SetupShaderRefsAndVolumeFields();
        }
        

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(_settings.volumeComponentName);
            
            if (UpdateMaterialFromVolume())
            {
                cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
                cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, _settings.effectMaterial, 0, 0);
            }
            
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
        
        private bool UpdateMaterialFromVolume()
        {
            if (_volumeComponentType == null) return false;
            VolumeComponent volumeComponent = VolumeManager.instance.stack.GetComponent(_volumeComponentType);

            for (int i = 0; i < _volumeComponentFields.Count; i++)
            {
                FieldInfo currentField = _volumeComponentFields[i];
                string currentShaderPropRef = _shaderPropRefs[i];
                SetShaderPropFromField(currentField, currentShaderPropRef, _settings.effectMaterial, volumeComponent);
            }

            return true;
        }
        
        private void SetupShaderRefsAndVolumeFields()
        {
            _shaderPropRefs = new List<string>();
            _volumeComponentFields = new List<FieldInfo>();
            
            _volumeComponentType = Type.GetType(_settings.volumeComponentName);

            if (_volumeComponentType == null || !_volumeComponentType.IsSubclassOf(typeof(VolumeComponent)))
            {
                return;
            }

            foreach(var field in _volumeComponentType.GetFields())
            {
                field.GetCustomAttribute<ShaderReferenceAttribute>();
                ShaderReferenceAttribute shaderReferenceAttribute = field.GetCustomAttribute<ShaderReferenceAttribute>();
                if (shaderReferenceAttribute != null)
                {
                    _shaderPropRefs.Add(shaderReferenceAttribute.shaderReferenceName);
                    _volumeComponentFields.Add(field);
                }
            }
        }
        
        private static void SetShaderPropFromField(FieldInfo field, string shaderPropRef, Material material, VolumeComponent volumeComponent)
        {
            switch (field.GetValue(volumeComponent))
            {
                case ColorParameter c:
                    material.SetColor(shaderPropRef, c.value);
                    break;
                case FloatParameter f:
                    material.SetFloat(shaderPropRef, f.value);
                    break;
                case IntParameter n:
                    material.SetInt(shaderPropRef, n.value);
                    break;
                default:
                    Debug.Log("Unsupported shader property type");
                    break;
            }
        }

    }

    FullScreenEffectPass m_FullScreenEffectPass;
    public PostProcessingRendererFeatureSettings settings;

    
    public override void Create()
    {
        if (settings != null && settings.effectMaterial != null)
        {
            m_FullScreenEffectPass = new FullScreenEffectPass(settings);
            m_FullScreenEffectPass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        }
        else
        {
            m_FullScreenEffectPass = null;
        }
    }

    
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (m_FullScreenEffectPass != null)
        {
            m_FullScreenEffectPass.Setup();
            renderer.EnqueuePass(m_FullScreenEffectPass);
        }
    }
}


