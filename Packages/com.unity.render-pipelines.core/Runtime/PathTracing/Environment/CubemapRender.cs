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

        private Material _cubemapMaterial;
        private Shader _lastUsedCubemapShader;
        private LocalKeyword? _noSunKeyword;
        private Color[] _faceColors = new Color[6]
        {
            Color.black,
            Color.black,
            Color.black,
            Color.black,
            Color.black,
            Color.black
        };
        private readonly Mesh _skyboxMesh;
        private readonly Mesh _sixFaceSkyboxMesh;
        private readonly Shader _solidColorShader;
        private Material _colorMaterial;
        private MaterialPropertyBlock _colorProperties;
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

        private static readonly int s_ColorID = Shader.PropertyToID("_Color");

        public int Hash => _hash;

        public CubemapRender(Mesh skyboxMesh, Mesh sixFaceSkyboxMesh, Shader solidColorShader)
        {
            _skyboxMesh = skyboxMesh;
            _sixFaceSkyboxMesh = sixFaceSkyboxMesh;
            _solidColorShader = solidColorShader;
            _hash = 0;
        }

        public void Dispose()
        {
            ReleaseCubemapIfExists();
            if (_colorMaterial != null)
            {
                CoreUtils.Destroy(_colorMaterial);
                _colorMaterial = null;
            }
        }

        public void SetMaterial(Material mat)
        {
            _cubemapMaterial = mat;
        }

        public void SetColor(Color color)
        {
            for (int faceIndex = 0; faceIndex < 6; faceIndex++)
            {
                _faceColors[faceIndex] = color;
            }
        }

        public void SetFaceColor(CubemapFace face, Color color)
        {
            _faceColors[(int)face] = color;
        }

        public void SetMode(Mode mode)
        {
            _mode = mode;
        }

        public Material GetMaterial() => _cubemapMaterial;

        Color LightColorInRenderingSpace(Light light)
        {
            Color cct = light.useColorTemperature ? Mathf.CorrelatedColorTemperatureToRGB(light.colorTemperature) : new Color(1, 1, 1, 1);
            Color filter = light.color.linear;
            return cct * filter * light.intensity;
        }

        public void Update(CommandBuffer cmd, Light sun, int resolution, out bool viewAndProjectionMatricesChanged)
        {
            viewAndProjectionMatricesChanged = false;

            int newHash = ((int)_mode) + 1;
            if (_mode == Mode.Color)
            {
                for (int faceIndex = 0; faceIndex < 6; faceIndex++)
                {
                    newHash = HashCode.Combine(newHash, _faceColors[faceIndex].r, _faceColors[faceIndex].g, _faceColors[faceIndex].b);
                }

                if (newHash != _hash)
                {
                    _colorMaterial ??= CoreUtils.CreateEngineMaterial(_solidColorShader);
#if UNITY_EDITOR
                    // The editor skips procedural draws while the shader compiles async, so keep the old hash and retry next frame
                    if (RequestShaderCompilationIfNeeded(_colorMaterial, 1))
                        return;
#endif
                    RenderWithColor(cmd);
                }
            }
            else if (_mode == Mode.Material)
            {
                if (_cubemapMaterial)
                {
                    newHash = HashCode.Combine(newHash, _cubemapMaterial.ComputeCRC());
                    if (sun != null)
                    {
                        var color = LightColorInRenderingSpace(sun);
                        var dir = -sun.GetComponent<Transform>().forward;
                        newHash = HashCode.Combine(newHash, color.r, color.g, color.b);
                        newHash = HashCode.Combine(newHash, dir.x, dir.y, dir.z);
                    }

                    if (newHash != _hash)
                    {
                        if (_lastUsedCubemapShader != _cubemapMaterial.shader)
                        {
                            var keyword = _cubemapMaterial.shader.keywordSpace.FindKeyword("_SUNDISK_NONE");
                            _noSunKeyword = keyword.isValid ? keyword : null;
                            _lastUsedCubemapShader = _cubemapMaterial.shader;
                        }
#if UNITY_EDITOR
                        // Drawing while the shader compiles async would bake the placeholder shader into the cubemap
                        if (RequestCubemapMaterialCompilationIfNeeded(_cubemapMaterial, _noSunKeyword))
                            return;
#endif
                        RenderWithMaterial(cmd, sun, resolution, out viewAndProjectionMatricesChanged);
                    }
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

            // Draw the color rather than ClearRenderTarget it: on some platforms a fast clear
            // collapses a non-{0,1} HDR clear color to ~white, while a fullscreen draw is exact.
#if UNITY_EDITOR
            Debug.Assert(ShaderUtil.IsPassCompiled(_colorMaterial, 0) || !ShaderUtil.allowAsyncCompilation, "The solid color shader is expected to be compiled before drawing the environment cubemap.");
#endif
            _colorProperties ??= new MaterialPropertyBlock();

            for (int faceIndex = 0; faceIndex < 6; ++faceIndex)
            {
                cmd.SetRenderTarget(new RenderTargetIdentifier(_cubemap, 0, (CubemapFace) faceIndex));
                cmd.SetViewport(new Rect(0, 0, 1, 1));
                _colorProperties.SetVector(s_ColorID, _faceColors[faceIndex]);
                CoreUtils.DrawFullScreen(cmd, _colorMaterial, _colorProperties);
            }
        }

        private void RenderWithMaterial(CommandBuffer cmd, Light sun, int cubemapResolution, out bool viewAndProjectionMatricesChanged)
        {
            EnsureCubemapExistsWithParticularResolution(cubemapResolution);

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

            if (_noSunKeyword.HasValue)
                cmd.EnableKeyword(_cubemapMaterial, _noSunKeyword.Value);

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

                if (_cubemapMaterial.passCount == 6)
                {
                    int passIndex = _cubeFaceToSkyboxPass[faceIndex];
                    cmd.DrawMesh(_sixFaceSkyboxMesh, Matrix4x4.identity, _cubemapMaterial, passIndex, passIndex, properties);
                }
                else
                {
                    cmd.DrawMesh(_skyboxMesh, Matrix4x4.identity, _cubemapMaterial, 0, 0, properties);
                }
            }

            viewAndProjectionMatricesChanged = true;

            if (_noSunKeyword.HasValue)
                cmd.DisableKeyword(_cubemapMaterial, _noSunKeyword.Value);
        }

#if UNITY_EDITOR
        // Requests compilation of the passes that are not compiled yet and returns whether any pass is still compiling
        private static bool RequestShaderCompilationIfNeeded(Material material, int passCount)
        {
            if (!ShaderUtil.allowAsyncCompilation)
                return false;

            bool compiling = false;
            for (int passIndex = 0; passIndex < passCount; ++passIndex)
            {
                if (ShaderUtil.IsPassCompiled(material, passIndex))
                    continue;

                ShaderUtil.CompilePass(material, passIndex);
                compiling = true;
            }
            return compiling;
        }

        private static bool RequestCubemapMaterialCompilationIfNeeded(Material cubemapMaterial, LocalKeyword? noSunKeyword)
        {
            // Check the variant the draw uses, which has the no-sun keyword enabled
            bool toggleKeyword = noSunKeyword.HasValue && !cubemapMaterial.IsKeywordEnabled(noSunKeyword.Value);
            if (toggleKeyword)
                cubemapMaterial.SetKeyword(noSunKeyword.Value, true);

            bool compiling = RequestShaderCompilationIfNeeded(cubemapMaterial, cubemapMaterial.passCount == 6 ? 6 : 1);

            if (toggleKeyword)
                cubemapMaterial.SetKeyword(noSunKeyword.Value, false);
            return compiling;
        }
#endif

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
                graphicsFormat = GraphicsFormat.R32G32B32A32_SFloat,
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
