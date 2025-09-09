using System;
using Unity.Mathematics;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace UnityEngine.PathTracing.Core
{
    internal class CubemapRender : IDisposable
    {
        private Material _material;

        public CubemapRender(Mesh skyboxMesh, Mesh sixSixFaceSkyboxMesh)
        {
            _skyboxMesh = skyboxMesh;
            _sixFaceSkyboxMesh = sixSixFaceSkyboxMesh;
            _currentSkyboxHash = 0;
        }

        public void Dispose()
        {
            if (_skyboxTexture != null)
            {
                _skyboxTexture.Release();
                CoreUtils.Destroy(_skyboxTexture);
            }
        }

        public void SetMaterial(Material mat)
        {
            _material = mat;
        }

        public Material GetMaterial() => _material;

        Color LightColorInRenderingSpace(Light light)
        {
            Color cct = light.useColorTemperature ? Mathf.CorrelatedColorTemperatureToRGB(light.colorTemperature) : new Color(1, 1, 1, 1);
            Color filter = light.color.linear;
            return cct * filter * light.intensity;
        }

        public Texture GetCubemap(int cubemapResolution, out int hash)
        {
            if (!_material)
            {
                // Note: prefabs have a null skyboxMaterial
                hash = 42;
                return CoreUtils.blackCubeTexture;
            }

            hash = _material.ComputeCRC();
            var light = RenderSettings.sun;
            if (light != null)
            {
                var color = LightColorInRenderingSpace(light);
                var dir = -light.GetComponent<Transform>().forward;
                hash ^= HashCode.Combine(color.r, color.g, color.b);
                hash ^= HashCode.Combine(dir.x, dir.y, dir.z);
            }

            if (hash != _currentSkyboxHash)
            {
                Render(cubemapResolution);
                _currentSkyboxHash = hash;
            }

           return _skyboxTexture;
        }

        public Texture Render(int cubemapResolution)
        {
            if (_skyboxTexture == null || _skyboxTexture.width != cubemapResolution)
                _skyboxTexture = CreateSkyboxTexture(cubemapResolution);

            using var cmd = new CommandBuffer();

            // We don't want to render  a (procedural) sun in our skybox for path tracing as it's already sampled as direct light
            bool isSunDisabled = _material.IsKeywordEnabled("_SUNDISK_NONE");
            if (!isSunDisabled)
                _material.EnableKeyword("_SUNDISK_NONE");

            var light = RenderSettings.sun;
            var properties = new MaterialPropertyBlock();
            if (light != null)
            {
                properties.SetVector(Shader.PropertyToID("_LightColor0"), LightColorInRenderingSpace(light));
                properties.SetVector(Shader.PropertyToID("_WorldSpaceLightPos0"), -light.GetComponent<Transform>().forward);
            }

            for (int faceIndex = 0; faceIndex < 6; ++faceIndex)
            {
                var viewMatrix = CubemapFaceBases[faceIndex];
                var proj = Matrix4x4.Perspective(90.0f, 1.0f, 0.1f, 10.0f);
                if (IsOpenGLGfxDevice())
                    proj.SetColumn(1, -proj.GetColumn(1)); // flip Y axis

                cmd.SetViewProjectionMatrices(viewMatrix, proj);
                cmd.SetRenderTarget(new RenderTargetIdentifier(_skyboxTexture, 0, (CubemapFace) faceIndex));
                cmd.SetViewport(new Rect(0, 0, _skyboxTexture.width, _skyboxTexture.height));
                cmd.ClearRenderTarget(false, true, new Color(0, 0, 0, 1));

#if UNITY_EDITOR
                ShaderUtil.SetAsyncCompilation(cmd, false);
#endif
                if (_material.passCount == 6)
                {
                    int passIndex = CubeFaceToSkyboxPass[faceIndex];
                    cmd.DrawMesh(_sixFaceSkyboxMesh, Matrix4x4.identity, _material, passIndex, passIndex, properties);
                }
                else
                {
                    cmd.DrawMesh(_skyboxMesh, Matrix4x4.identity, _material, 0, 0, properties);
                }
#if UNITY_EDITOR
                ShaderUtil.RestoreAsyncCompilation(cmd);
#endif
            }

            Graphics.ExecuteCommandBuffer(cmd);

            if (!isSunDisabled)
                _material.DisableKeyword("_SUNDISK_NONE");

            return _skyboxTexture;
        }

        private static readonly Matrix4x4[] CubemapFaceBases = new Matrix4x4[6]
        {
            new(new float4(0, 0, -1, 0), new float4(0, 1, 0, 0),  new float4(-1, 0, 0, 0), new float4(0, 0, 0, 1)),
            new(new float4(0, 0, 1, 0),  new float4(0, 1, 0, 0),  new float4(1, 0, 0, 0),  new float4(0, 0, 0, 1)),
            new(new float4(1, 0, 0, 0),  new float4(0, 0, -1, 0), new float4(0, -1, 0, 0), new float4(0, 0, 0, 1)),
            new(new float4(1, 0, 0, 0),  new float4(0, 0, 1, 0),  new float4(0, 1, 0, 0),  new float4(0, 0, 0, 1)),
            new(new float4(1, 0, 0, 0),  new float4(0, 1, 0, 0),  new float4(0, 0, -1, 0), new float4(0, 0, 0, 1)),
            new(new float4(-1, 0, 0, 0), new float4(0, 1, 0, 0),  new float4(0, 0, 1, 0),  new float4(0, 0, 0, 1)),
        };

        private static readonly int[] CubeFaceToSkyboxPass = { 2, 3, 4, 5, 0, 1 };

        private readonly Mesh _skyboxMesh;
        private readonly Mesh _sixFaceSkyboxMesh;
        private RenderTexture _skyboxTexture;
        private int _currentSkyboxHash;

        private RenderTexture CreateSkyboxTexture(int width)
        {
            var texture = new RenderTexture(new RenderTextureDescriptor(){
                dimension = TextureDimension.Cube,
                width = width,
                height = width,
                depthBufferBits = 0,
                volumeDepth = 1,
                msaaSamples = 1,
                vrUsage = VRTextureUsage.OneEye,
                graphicsFormat = GraphicsFormat.R16G16B16A16_SFloat,
                enableRandomWrite = true
            });
            texture.Create();
            texture.name = "CreateSkyboxTexture";
            return texture;
        }

        private bool IsOpenGLGfxDevice()
        {
            return SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3 ||
                SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLCore;
        }
    }
}
