using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PostProcessingRendererFeature : ScriptableRendererFeature
{
    [Serializable]
    public class PostProcessingRendererFeatureSettings
    {
        public string volumeComponentName;
        public int volumeComponentIndex;
    }
    class CustomPostProcessingPass : ScriptableRenderPass
    {
        private readonly PostProcessingRendererFeatureSettings _settings;
        private Material _effectMaterial;
        private List<string> _shaderPropRefs;
        private List<FieldInfo> _volumeComponentFields;
        private Type _volumeComponentType;

        public CustomPostProcessingPass(PostProcessingRendererFeatureSettings settings)
        {
            this._settings = settings;
        }

        public void Setup()
        {
            SetupShaderRefsAndVolumeFields();
        }
        
        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in an performance manner.
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(_settings.volumeComponentName);

            if (UpdateMaterialFromVolume())
            {
                Blit(cmd, "null", "_CameraColorTexture", _effectMaterial);
            }
            
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        /// Cleanup any allocated resources that were created during the execution of this render pass.
        public override void FrameCleanup(CommandBuffer cmd)
        {
        }

        private void SetupShaderRefsAndVolumeFields()
        {
            _shaderPropRefs = new List<string>();
            _volumeComponentFields = new List<FieldInfo>();
            
            _volumeComponentType = Type.GetType(_settings.volumeComponentName);

            if (_volumeComponentType == null || !_volumeComponentType.IsSubclassOf(typeof(VolumeComponent)))
            {
                //Debug.Log("Could not find volume component type " + _settings.volumeComponent);
                return;
            }
            
            ControlsShaderAttribute controlsShaderAttribute = _volumeComponentType.GetCustomAttribute<ControlsShaderAttribute>();
            if (controlsShaderAttribute == null)
            {
                //Debug.Log("Volume component '" + _settings.volumeComponent + "' missing 'ControlsShader' attribute");
                return;
            }
            
            Shader effectShader = Shader.Find(controlsShaderAttribute.shaderPath);
            if (effectShader == null)
            {
                //Debug.Log("Could not find shader " + controlsShaderAttribute.shaderPath);
                return;
            }
        
            _effectMaterial = new Material(effectShader);
            
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

        private bool UpdateMaterialFromVolume()
        {
            if (_volumeComponentType == null) return false;
            VolumeComponent volumeComponent = VolumeManager.instance.stack.GetComponent(_volumeComponentType);

            for (int i = 0; i < _volumeComponentFields.Count; i++)
            {
                FieldInfo currentField = _volumeComponentFields[i];
                string currentShaderPropRef = _shaderPropRefs[i];
                SetShaderPropFromField(currentField, currentShaderPropRef, _effectMaterial, volumeComponent);
            }

            return true;
        }
        
        private void SetShaderPropFromField(FieldInfo field, string shaderPropRef, Material material, VolumeComponent volumeComponent)
        {
            switch (field.GetValue(volumeComponent))
            {
                case ColorParameter c:
                    _effectMaterial.SetColor(shaderPropRef, c.value);
                    break;
                case FloatParameter f:
                    _effectMaterial.SetFloat(shaderPropRef, f.value);
                    break;
                case IntParameter n:
                    _effectMaterial.SetInt(shaderPropRef, n.value);
                    break;
                default:
                    Debug.Log("Unsupported shader property type");
                    break;
            }
        }
    }

    CustomPostProcessingPass m_CustomPostProcessingPass;
    public PostProcessingRendererFeatureSettings settings;

    public override void Create()
    {
        m_CustomPostProcessingPass = new CustomPostProcessingPass(settings);

        // Configures where the render pass should be injected.
        m_CustomPostProcessingPass.renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        m_CustomPostProcessingPass.Setup();
        renderer.EnqueuePass(m_CustomPostProcessingPass);
    }
}


