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
        internal enum Mode
        {
            Material,
            Color
        }

        private Material _material;
        private Color _color = Color.black;
        private readonly Mesh _skyboxMesh;
        private readonly Mesh _sixFaceSkyboxMesh;
        private RenderTexture _cubemap;
        private int _hash;
        private Mode _mode = Mode.Color;

        private static readonly int[] _cubeFaceToSkyboxPass = { 2, 3, 4, 5, 0, 1 };
        private static readonly Matrix4x4[] _cubemapFaceBases = new Matrix4x4[6]
        {
            new(new float4(0, 0, -1, 0), new float4(0, 1, 0, 0),  new float4(-1, 0, 0, 0), new float4(0, 0, 0, 1)),
            new(new float4(0, 0, 1, 0),  new float4(0, 1, 0, 0),  new float4(1, 0, 0, 0),  new float4(0, 0, 0, 1)),
            new(new float4(1, 0, 0, 0),  new float4(0, 0, -1, 0), new float4(0, -1, 0, 0), new float4(0, 0, 0, 1)),
            new(new float4(1, 0, 0, 0),  new float4(0, 0, 1, 0),  new float4(0, 1, 0, 0),  new float4(0, 0, 0, 1)),
            new(new float4(1, 0, 0, 0),  new float4(0, 1, 0, 0),  new float4(0, 0, -1, 0), new float4(0, 0, 0, 1)),
            new(new float4(-1, 0, 0, 0), new float4(0, 1, 0, 0),  new float4(0, 0, 1, 0),  new float4(0, 0, 0, 1)),
        };

        public int Hash => _hash;

        public CubemapRender(Mesh skyboxMesh, Mesh sixFaceSkyboxMesh)
        {
            _skyboxMesh = skyboxMesh;
            _sixFaceSkyboxMesh = sixFaceSkyboxMesh;
            _hash = 0;
        }

        public void Dispose()
        {
            ReleaseCubemapIfExists();
        }

        public void SetMaterial(Material mat)
        {
            _material = mat;
        }

        public void SetColor(Color color)
        {
            _color = color;
        }

        public void SetMode(Mode mode)
        {
            _mode = mode;
        }

        public Material GetMaterial() => _material;

        Color LightColorInRenderingSpace(Light light)
        {
            Color cct = light.useColorTemperature ? Mathf.CorrelatedColorTemperatureToRGB(light.colorTemperature) : new Color(1, 1, 1, 1);
            Color filter = light.color.linear;
            return cct * filter * light.intensity;
        }

        public void Update(CommandBuffer cmd, Light sun, int resolution)
        {
            int newHash = ((int)_mode) + 1;
            if (_mode == Mode.Color)
            {
                newHash ^= HashCode.Combine(_color.r, _color.g, _color.b);

                if (newHash != _hash)
                    RenderWithColor(cmd);
            }
            else if (_mode == Mode.Material)
            {
                if (_material)
                {
                    newHash ^= _material.ComputeCRC();
                    if (sun != null)
                    {
                        var color = LightColorInRenderingSpace(sun);
                        var dir = -sun.GetComponent<Transform>().forward;
                        newHash ^= HashCode.Combine(color.r, color.g, color.b);
                        newHash ^= HashCode.Combine(dir.x, dir.y, dir.z);
                    }

                    if (newHash != _hash)
                        RenderWithMaterial(cmd, sun, resolution);
                }
                else
                {
                    newHash = 42;
                    ReleaseCubemapIfExists();
                }
            }
            _hash = newHash;
        }

        void ReleaseCubemapIfExists()
        {
            if (_cubemap != null)
            {
                _cubemap.Release();
                CoreUtils.Destroy(_cubemap);
                _cubemap = null;
            }
        }

        public Texture GetCubemap()
        {
            return _cubemap != null ? _cubemap : CoreUtils.blackCubeTexture;
        }

        private void RenderWithColor(CommandBuffer cmd)
        {
            EnsureCubemapExistsWithParticularResolution(1);

            for (int faceIndex = 0; faceIndex < 6; ++faceIndex)
            {
                cmd.SetRenderTarget(new RenderTargetIdentifier(_cubemap, 0, (CubemapFace) faceIndex));
                cmd.SetViewport(new Rect(0, 0, 1, 1));
                cmd.ClearRenderTarget(false, true, _color);
            }
        }

        private void RenderWithMaterial(CommandBuffer cmd, Light sun, int cubemapResolution)
        {
            EnsureCubemapExistsWithParticularResolution(cubemapResolution);

            // We don't want to render  a (procedural) sun in our skybox for path tracing as it's already sampled as direct light
            bool isSunDisabled = _material.IsKeywordEnabled("_SUNDISK_NONE");
            if (!isSunDisabled)
                _material.EnableKeyword("_SUNDISK_NONE");

            var properties = new MaterialPropertyBlock();
            if (sun != null)
            {
                properties.SetVector(Shader.PropertyToID("_LightColor0"), LightColorInRenderingSpace(sun));
                properties.SetVector(Shader.PropertyToID("_WorldSpaceLightPos0"), -sun.GetComponent<Transform>().forward);
            }
            else
            {
                properties.SetVector(Shader.PropertyToID("_LightColor0"), Color.black);
                properties.SetVector(Shader.PropertyToID("_WorldSpaceLightPos0"), new Vector4(0, 0, -1, 0));
            }

            for (int faceIndex = 0; faceIndex < 6; ++faceIndex)
            {
                var viewMatrix = _cubemapFaceBases[faceIndex];
                var proj = Matrix4x4.Perspective(90.0f, 1.0f, 0.1f, 10.0f);
                if (IsOpenGLGfxDevice())
                    proj.SetColumn(1, -proj.GetColumn(1)); // flip Y axis

                cmd.SetViewProjectionMatrices(viewMatrix, proj);
                cmd.SetRenderTarget(new RenderTargetIdentifier(_cubemap, 0, (CubemapFace) faceIndex));
                cmd.SetViewport(new Rect(0, 0, _cubemap.width, _cubemap.height));
                cmd.ClearRenderTarget(false, true, new Color(0, 0, 0, 1));

#if UNITY_EDITOR
                ShaderUtil.SetAsyncCompilation(cmd, false);
#endif
                if (_material.passCount == 6)
                {
                    int passIndex = _cubeFaceToSkyboxPass[faceIndex];
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

            if (!isSunDisabled)
                _material.DisableKeyword("_SUNDISK_NONE");
        }

        private void EnsureCubemapExistsWithParticularResolution(int resolution)
        {
            if (_cubemap == null || _cubemap.width != resolution)
            {
                ReleaseCubemapIfExists();
                CreateCubemap(resolution);
            }
        }

        private void CreateCubemap(int width)
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
            texture.name = "EnvironmentCubemap";
            _cubemap = texture;
        }

        private static bool IsOpenGLGfxDevice()
        {
            return SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3 ||
                SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLCore;
        }
    }
}
