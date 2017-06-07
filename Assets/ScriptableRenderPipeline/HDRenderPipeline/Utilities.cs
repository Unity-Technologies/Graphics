using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using UnityEngine.Rendering;
using UnityObject = UnityEngine.Object;
using System.Reflection;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Flags]
    public enum ClearFlag
    {
        ClearNone  = 0,
        ClearColor = 1,
        ClearDepth = 2
    }

    public class Utilities
    {
        public static List<RenderPipelineMaterial> GetRenderPipelineMaterialList()
        {
            List<RenderPipelineMaterial> materialList = new List<RenderPipelineMaterial>();

            var baseType = typeof(RenderPipelineMaterial);
            var assembly = baseType.Assembly;

            System.Type[] types = assembly.GetTypes();
            foreach (System.Type type in types)
            {
                if (type.IsSubclassOf(baseType))
                {
                    // Create an instance object of the given type
                    var obj = (RenderPipelineMaterial)Activator.CreateInstance(type);
                    materialList.Add(obj);
                }
            }

            // Note: If there is a need for an optimization in the future of this function, user can simply fill the materialList manually by commenting the code abode and
            // adding to the list material they used in their game.
            //  materialList.Add(new Lit());
            //  materialList.Add(new Unlit());
            // ...

            return materialList;
        }

        public const RendererConfiguration kRendererConfigurationBakedLighting = RendererConfiguration.PerObjectLightProbe | RendererConfiguration.PerObjectLightmaps | RendererConfiguration.PerObjectLightProbeProxyVolume;


        // Render Target Management.
        public const ClearFlag kClearAll = ClearFlag.ClearDepth | ClearFlag.ClearColor;

        public static void SetRenderTarget(ScriptableRenderContext renderContext, RenderTargetIdentifier buffer, ClearFlag clearFlag, Color clearColor, int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown)
        {
            var cmd = new CommandBuffer();
            cmd.name = "";
            cmd.SetRenderTarget(buffer, miplevel, cubemapFace);
            if (clearFlag != ClearFlag.ClearNone)
                cmd.ClearRenderTarget((clearFlag & ClearFlag.ClearDepth) != 0, (clearFlag & ClearFlag.ClearColor) != 0, clearColor);
            renderContext.ExecuteCommandBuffer(cmd);
            cmd.Dispose();
        }

        public static void SetRenderTarget(ScriptableRenderContext renderContext, RenderTargetIdentifier buffer, ClearFlag clearFlag = ClearFlag.ClearNone, int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown)
        {
            SetRenderTarget(renderContext, buffer, clearFlag, Color.black, miplevel, cubemapFace);
        }

        public static void SetRenderTarget(ScriptableRenderContext renderContext, RenderTargetIdentifier colorBuffer, RenderTargetIdentifier depthBuffer, int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown)
        {
            SetRenderTarget(renderContext, colorBuffer, depthBuffer, ClearFlag.ClearNone, Color.black, miplevel, cubemapFace);
        }

        public static void SetRenderTarget(ScriptableRenderContext renderContext, RenderTargetIdentifier colorBuffer, RenderTargetIdentifier depthBuffer, ClearFlag clearFlag, int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown)
        {
            SetRenderTarget(renderContext, colorBuffer, depthBuffer, clearFlag, Color.black, miplevel, cubemapFace);
        }

        public static void SetRenderTarget(ScriptableRenderContext renderContext, RenderTargetIdentifier colorBuffer, RenderTargetIdentifier depthBuffer, ClearFlag clearFlag, Color clearColor, int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown)
        {
            var cmd = new CommandBuffer();
            cmd.name = "";
            cmd.SetRenderTarget(colorBuffer, depthBuffer, miplevel, cubemapFace);
            if (clearFlag != ClearFlag.ClearNone)
                cmd.ClearRenderTarget((clearFlag & ClearFlag.ClearDepth) != 0, (clearFlag & ClearFlag.ClearColor) != 0, clearColor);
            renderContext.ExecuteCommandBuffer(cmd);
            cmd.Dispose();
        }

        public static void SetRenderTarget(ScriptableRenderContext renderContext, RenderTargetIdentifier[] colorBuffers, RenderTargetIdentifier depthBuffer)
        {
            SetRenderTarget(renderContext, colorBuffers, depthBuffer, ClearFlag.ClearNone, Color.black);
        }

        public static void SetRenderTarget(ScriptableRenderContext renderContext, RenderTargetIdentifier[] colorBuffers, RenderTargetIdentifier depthBuffer, ClearFlag clearFlag = ClearFlag.ClearNone)
        {
            SetRenderTarget(renderContext, colorBuffers, depthBuffer, clearFlag, Color.black);
        }

        public static void SetRenderTarget(ScriptableRenderContext renderContext, RenderTargetIdentifier[] colorBuffers, RenderTargetIdentifier depthBuffer, ClearFlag clearFlag, Color clearColor)
        {
            var cmd = new CommandBuffer();
            cmd.name = "";
            cmd.SetRenderTarget(colorBuffers, depthBuffer);
            if (clearFlag != ClearFlag.ClearNone)
                cmd.ClearRenderTarget((clearFlag & ClearFlag.ClearDepth) != 0, (clearFlag & ClearFlag.ClearColor) != 0, clearColor);
            renderContext.ExecuteCommandBuffer(cmd);
            cmd.Dispose();
        }

        public static void ClearCubemap(ScriptableRenderContext renderContext, RenderTargetIdentifier buffer, Color clearColor)
        {
            var cmd = new CommandBuffer();
            cmd.name = "";

            for(int i = 0 ; i < 6 ; ++i)
            {
                SetRenderTarget(renderContext, buffer, ClearFlag.ClearColor, Color.black, 0, (CubemapFace)i);
            }

            renderContext.ExecuteCommandBuffer(cmd);
            cmd.Dispose();
        }

        // Miscellanous
        public static Material CreateEngineMaterial(string shaderPath)
        {
            var mat = new Material(Shader.Find(shaderPath))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            return mat;
        }

        public static Material CreateEngineMaterial(Shader shader)
        {
            var mat = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            return mat;
        }

        public static void Destroy(UnityObject obj)
        {
            if (obj != null)
            {
#if UNITY_EDITOR
                if (Application.isPlaying)
                    UnityObject.Destroy(obj);
                else
                    UnityObject.DestroyImmediate(obj);
#else
                UnityObject.Destroy(obj);
#endif
            }
        }

        public static void SafeRelease(ComputeBuffer buffer)
        {
            if (buffer != null)
                buffer.Release();
        }

        public static string GetFieldPath<TType, TValue>(Expression<Func<TType, TValue>> expr)
        {
            MemberExpression me;
            switch (expr.Body.NodeType)
            {
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                    var ue = expr.Body as UnaryExpression;
                    me = (ue != null ? ue.Operand : null) as MemberExpression;
                    break;
                default:
                    me = expr.Body as MemberExpression;
                    break;
            }

            var members = new List<string>();
            while (me != null)
            {
                members.Add(me.Member.Name);
                me = me.Expression as MemberExpression;
            }

            var sb = new StringBuilder();
            for (int i = members.Count - 1; i >= 0; i--)
            {
                sb.Append(members[i]);
                if (i > 0) sb.Append('.');
            }

            return sb.ToString();
        }

        public class ProfilingSample
            : IDisposable
        {
            bool        disposed = false;
            ScriptableRenderContext  renderContext;
            string      name;

            public ProfilingSample(string _name, ScriptableRenderContext _renderloop)
            {
                renderContext = _renderloop;
                name = _name;

                CommandBuffer cmd = new CommandBuffer();
                cmd.name = "";
                cmd.BeginSample(name);
                renderContext.ExecuteCommandBuffer(cmd);
                cmd.Dispose();
            }

            ~ProfilingSample()
            {
                Dispose(false);
            }

            public void Dispose()
            {
                Dispose(true);
            }

            // Protected implementation of Dispose pattern.
            protected virtual void Dispose(bool disposing)
            {
                if (disposed)
                    return;

                if (disposing)
                {
                    CommandBuffer cmd = new CommandBuffer();
                    cmd.name = "";
                    cmd.EndSample(name);
                    renderContext.ExecuteCommandBuffer(cmd);
                    cmd.Dispose();
                }

                disposed = true;
            }
        }

        public static Matrix4x4 GetViewProjectionMatrix(Matrix4x4 worldToViewMatrix, Matrix4x4 projectionMatrix)
        {
            // The actual projection matrix used in shaders is actually massaged a bit to work across all platforms
            // (different Z value ranges etc.)
            var gpuProj = GL.GetGPUProjectionMatrix(projectionMatrix, false);
            var gpuVP = gpuProj *  worldToViewMatrix * Matrix4x4.Scale(new Vector3(1.0f, 1.0f, -1.0f)); // Need to scale -1.0 on Z to match what is being done in the camera.wolrdToCameraMatrix API.

            return gpuVP;
        }

        public static HDCamera GetHDCamera(Camera camera)
        {
            HDCamera hdCamera = new HDCamera();
            hdCamera.camera = camera;
            hdCamera.screenSize = new Vector4(camera.pixelWidth, camera.pixelHeight, 1.0f / camera.pixelWidth, 1.0f / camera.pixelHeight);

            // The actual projection matrix used in shaders is actually massaged a bit to work across all platforms
            // (different Z value ranges etc.)
            var gpuProj = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false);
            var gpuVP = gpuProj * camera.worldToCameraMatrix;

            // Ref: An Efficient Depth Linearization Method for Oblique View Frustums, Eq. 6.
            Vector4 invProjectionParam = new Vector4(gpuProj.m20 / (gpuProj.m00 * gpuProj.m23),
                    gpuProj.m21 / (gpuProj.m11 * gpuProj.m23),
                    -1.0f / gpuProj.m23,
                    (-gpuProj.m22
                     + gpuProj.m20 * gpuProj.m02 / gpuProj.m00
                     + gpuProj.m21 * gpuProj.m12 / gpuProj.m11) / gpuProj.m23);

            hdCamera.viewProjectionMatrix    = gpuVP;
            hdCamera.invViewProjectionMatrix = gpuVP.inverse;
            hdCamera.invProjectionMatrix     = gpuProj.inverse;
            hdCamera.invProjectionParam      = invProjectionParam;

            return hdCamera;
        }

        public static void SetupMaterialHDCamera(HDCamera hdCamera, Material material)
        {
            material.SetVector("_ScreenSize",        hdCamera.screenSize);
            material.SetMatrix("_ViewProjMatrix",    hdCamera.viewProjectionMatrix);
            material.SetMatrix("_InvViewProjMatrix", hdCamera.invViewProjectionMatrix);
            material.SetMatrix("_InvProjMatrix",     hdCamera.invProjectionMatrix);
            material.SetVector("_InvProjParam",      hdCamera.invProjectionParam);
        }

        // TEMP: These functions should be implemented C++ side, for now do it in C#
        public static void SetMatrixCS(CommandBuffer cmd, ComputeShader shadercs, string name, Matrix4x4 mat)
        {
            var data = new float[16];

            for (int c = 0; c < 4; c++)
                for (int r = 0; r < 4; r++)
                    data[4 * c + r] = mat[r, c];

            cmd.SetComputeFloatParams(shadercs, name, data);
        }

        public static void SetMatrixArrayCS(CommandBuffer cmd, ComputeShader shadercs, string name, Matrix4x4[] matArray)
        {
            int numMatrices = matArray.Length;
            var data = new float[numMatrices * 16];

            for (int n = 0; n < numMatrices; n++)
                for (int c = 0; c < 4; c++)
                    for (int r = 0; r < 4; r++)
                        data[16 * n + 4 * c + r] = matArray[n][r, c];

            cmd.SetComputeFloatParams(shadercs, name, data);
        }

        public static void SetVectorArrayCS(CommandBuffer cmd, ComputeShader shadercs, string name, Vector4[] vecArray)
        {
            int numVectors = vecArray.Length;
            var data = new float[numVectors * 4];

            for (int n = 0; n < numVectors; n++)
                for (int i = 0; i < 4; i++)
                    data[4 * n + i] = vecArray[n][i];

            cmd.SetComputeFloatParams(shadercs, name, data);
        }

        public static void SetKeyword(Material m, string keyword, bool state)
        {
            if (state)
                m.EnableKeyword(keyword);
            else
                m.DisableKeyword(keyword);
        }

        public static void SelectKeyword(Material material, string keyword1, string keyword2, bool enableFirst)
        {
            material.EnableKeyword(enableFirst ? keyword1 : keyword2);
            material.DisableKeyword(enableFirst ? keyword2 : keyword1);
        }

        public static void SelectKeyword(Material material, string[] keywords, int enabledKeywordIndex)
        {
            material.EnableKeyword(keywords[enabledKeywordIndex]);

            for (int i = 0; i < keywords.Length; i++)
            {
                if (i != enabledKeywordIndex)
                {
                    material.DisableKeyword(keywords[i]);
                }
            }
        }

        // Draws a full screen triangle as a faster alternative to drawing a full screen quad.
        public static void DrawFullScreen(CommandBuffer commandBuffer, Material material, HDCamera camera,
            RenderTargetIdentifier colorBuffer,
            MaterialPropertyBlock properties = null, int shaderPassID = 0)
        {
            SetupMaterialHDCamera(camera, material);
            commandBuffer.SetRenderTarget(colorBuffer);
            commandBuffer.DrawProcedural(Matrix4x4.identity, material, shaderPassID, MeshTopology.Triangles, 3, 1, properties);
        }

        // Draws a full screen triangle as a faster alternative to drawing a full screen quad.
        public static void DrawFullScreen(CommandBuffer commandBuffer, Material material, HDCamera camera,
            RenderTargetIdentifier colorBuffer, RenderTargetIdentifier depthStencilBuffer,
            MaterialPropertyBlock properties = null, int shaderPassID = 0)
        {
            SetupMaterialHDCamera(camera, material);
            commandBuffer.SetRenderTarget(colorBuffer, depthStencilBuffer);
            commandBuffer.DrawProcedural(Matrix4x4.identity, material, shaderPassID, MeshTopology.Triangles, 3, 1, properties);
        }

        // Draws a full screen triangle as a faster alternative to drawing a full screen quad.
        public static void DrawFullScreen(CommandBuffer commandBuffer, Material material, HDCamera camera,
            RenderTargetIdentifier[] colorBuffers, RenderTargetIdentifier depthStencilBuffer,
            MaterialPropertyBlock properties = null, int shaderPassID = 0)
        {
            SetupMaterialHDCamera(camera, material);
            commandBuffer.SetRenderTarget(colorBuffers, depthStencilBuffer);
            commandBuffer.DrawProcedural(Matrix4x4.identity, material, shaderPassID, MeshTopology.Triangles, 3, 1, properties);
        }

        // Draws a full screen triangle as a faster alternative to drawing a full screen quad.
        // Important: the first RenderTarget must be created with 0 depth bits!
        public static void DrawFullScreen(CommandBuffer commandBuffer, Material material, HDCamera camera,
            RenderTargetIdentifier[] colorBuffers,
            MaterialPropertyBlock properties = null, int shaderPassID = 0)
        {
            // It is currently not possible to have MRT without also setting a depth target.
            // To work around this deficiency of the CommandBuffer.SetRenderTarget() API,
            // we pass the first color target as the depth target. If it has 0 depth bits,
            // no depth target ends up being bound.
            DrawFullScreen(commandBuffer, material, camera, colorBuffers, colorBuffers[0], properties, shaderPassID);
        }

        // Helper to help to display debug info on screen
        static float overlayLineHeight = -1.0f;
        public static void NextOverlayCoord(ref float x, ref float y, float overlayWidth, float overlayHeight, float width)
        {
            x += overlayWidth;
            overlayLineHeight = Mathf.Max(overlayHeight, overlayLineHeight);
            // Go to next line if it goes outside the screen.
            if (x + overlayWidth > width)
            {
                x = 0;
                y -= overlayLineHeight;
                overlayLineHeight = -1.0f;
            }
        }
    }
}
