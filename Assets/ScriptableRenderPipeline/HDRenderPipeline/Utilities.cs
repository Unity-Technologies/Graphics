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

        public static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier buffer, ClearFlag clearFlag, Color clearColor, int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown)
        {
            cmd.SetRenderTarget(buffer, miplevel, cubemapFace);
            if (clearFlag != ClearFlag.ClearNone)
                cmd.ClearRenderTarget((clearFlag & ClearFlag.ClearDepth) != 0, (clearFlag & ClearFlag.ClearColor) != 0, clearColor);
        }

        public static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier buffer, ClearFlag clearFlag = ClearFlag.ClearNone, int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown)
        {
            SetRenderTarget(cmd, buffer, clearFlag, Color.black, miplevel, cubemapFace);
        }

        public static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier colorBuffer, RenderTargetIdentifier depthBuffer, int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown)
        {
            SetRenderTarget(cmd, colorBuffer, depthBuffer, ClearFlag.ClearNone, Color.black, miplevel, cubemapFace);
        }

        public static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier colorBuffer, RenderTargetIdentifier depthBuffer, ClearFlag clearFlag, int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown)
        {
            SetRenderTarget(cmd, colorBuffer, depthBuffer, clearFlag, Color.black, miplevel, cubemapFace);
        }

        public static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier colorBuffer, RenderTargetIdentifier depthBuffer, ClearFlag clearFlag, Color clearColor, int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown)
        {
            cmd.SetRenderTarget(colorBuffer, depthBuffer, miplevel, cubemapFace);
            if (clearFlag != ClearFlag.ClearNone)
                cmd.ClearRenderTarget((clearFlag & ClearFlag.ClearDepth) != 0, (clearFlag & ClearFlag.ClearColor) != 0, clearColor);
        }

        public static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier[] colorBuffers, RenderTargetIdentifier depthBuffer)
        {
            SetRenderTarget(cmd, colorBuffers, depthBuffer, ClearFlag.ClearNone, Color.black);
        }

        public static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier[] colorBuffers, RenderTargetIdentifier depthBuffer, ClearFlag clearFlag = ClearFlag.ClearNone)
        {
            SetRenderTarget(cmd, colorBuffers, depthBuffer, clearFlag, Color.black);
        }

        public static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier[] colorBuffers, RenderTargetIdentifier depthBuffer, ClearFlag clearFlag, Color clearColor)
        {
            cmd.SetRenderTarget(colorBuffers, depthBuffer);
            if (clearFlag != ClearFlag.ClearNone)
                cmd.ClearRenderTarget((clearFlag & ClearFlag.ClearDepth) != 0, (clearFlag & ClearFlag.ClearColor) != 0, clearColor);
        }

        public static void ClearCubemap(CommandBuffer cmd, RenderTargetIdentifier buffer, Color clearColor)
        {
            for(int i = 0 ; i < 6 ; ++i)
            {
                SetRenderTarget(cmd, buffer, ClearFlag.ClearColor, Color.black, 0, (CubemapFace)i);
            }
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

        public struct ProfilingSample
            : IDisposable
        {
            bool            disposed;
            CommandBuffer   cmd;
            string          name;

            public ProfilingSample(string _name, CommandBuffer _cmd)
            {
                cmd = _cmd;
                disposed = false;
                name = _name;
                cmd.BeginSample(name);
            }

            public void Dispose()
            {
                Dispose(true);
            }

            // Protected implementation of Dispose pattern.
            void Dispose(bool disposing)
            {
                if (disposed)
                    return;

                if (disposing)
                {
                    cmd.EndSample(name);
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
        public static void DrawFullScreen(CommandBuffer commandBuffer, Material material,
            RenderTargetIdentifier colorBuffer,
            MaterialPropertyBlock properties = null, int shaderPassID = 0)
        {
            commandBuffer.SetRenderTarget(colorBuffer);
            commandBuffer.DrawProcedural(Matrix4x4.identity, material, shaderPassID, MeshTopology.Triangles, 3, 1, properties);
        }

        // Draws a full screen triangle as a faster alternative to drawing a full screen quad.
        public static void DrawFullScreen(CommandBuffer commandBuffer, Material material,
            RenderTargetIdentifier colorBuffer, RenderTargetIdentifier depthStencilBuffer,
            MaterialPropertyBlock properties = null, int shaderPassID = 0)
        {
            commandBuffer.SetRenderTarget(colorBuffer, depthStencilBuffer);
            commandBuffer.DrawProcedural(Matrix4x4.identity, material, shaderPassID, MeshTopology.Triangles, 3, 1, properties);
        }

        // Draws a full screen triangle as a faster alternative to drawing a full screen quad.
        public static void DrawFullScreen(CommandBuffer commandBuffer, Material material,
            RenderTargetIdentifier[] colorBuffers, RenderTargetIdentifier depthStencilBuffer,
            MaterialPropertyBlock properties = null, int shaderPassID = 0)
        {
            commandBuffer.SetRenderTarget(colorBuffers, depthStencilBuffer);
            commandBuffer.DrawProcedural(Matrix4x4.identity, material, shaderPassID, MeshTopology.Triangles, 3, 1, properties);
        }

        // Draws a full screen triangle as a faster alternative to drawing a full screen quad.
        // Important: the first RenderTarget must be created with 0 depth bits!
        public static void DrawFullScreen(CommandBuffer commandBuffer, Material material,
            RenderTargetIdentifier[] colorBuffers,
            MaterialPropertyBlock properties = null, int shaderPassID = 0)
        {
            // It is currently not possible to have MRT without also setting a depth target.
            // To work around this deficiency of the CommandBuffer.SetRenderTarget() API,
            // we pass the first color target as the depth target. If it has 0 depth bits,
            // no depth target ends up being bound.
            DrawFullScreen(commandBuffer, material, colorBuffers, colorBuffers[0], properties, shaderPassID);
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

        // Just a sort function that doesn't allocate memory
        // Note: Shoud be repalc by a radix sort for positive integer
        static public int Partition(uint[] numbers, int left, int right)
        {
            uint pivot = numbers[left];
            while (true)
            {
                while (numbers[left] < pivot)
                    left++;

                while (numbers[right] > pivot)
                    right--;

                if (left < right)
                {
                    uint temp = numbers[right];
                    numbers[right] = numbers[left];
                    numbers[left] = temp;
                }
                else
                {
                    return right;
                }
            }
        }

        static public void QuickSort(uint[] arr, int left, int right)
        {
            // For Recusrion
            if (left < right)
            {
                int pivot = Partition(arr, left, right);

                if (pivot > 1)
                    QuickSort(arr, left, pivot - 1);

                if (pivot + 1 < right)
                    QuickSort(arr, pivot + 1, right);
            }
        }
    }
}
