using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    public class HDRPColorDepthCapture : MonoBehaviour
    {
        public static HDRPColorDepthCapture instance;
        public Camera m_Camera;
        public bool Enabled = false;

        public RenderTexture ColorBuffer;
        public RenderTexture DepthBuffer;

        void OnEnable()
        {
            if (instance != null)
                throw new Exception("Singleton already exists");
            instance = this;
            m_Camera = GetComponent<Camera>();
        }

        public void CaptureColorAndDepth(Camera camera, CommandBuffer cmd, RenderTargetIdentifier color, RenderTargetIdentifier depth)
        {
            if (!Enabled) return;
            if (camera != m_Camera) return;

            if (ColorBuffer != null)
            {
                CheckRTSize(m_Camera, ColorBuffer);
                cmd.CopyTexture(color, 0, 0, ColorBuffer, 0, 0);
            }

            if (DepthBuffer != null)
            {
                CheckRTSize(m_Camera, DepthBuffer);
                cmd.CopyTexture(depth, 0, 0, DepthBuffer, 0, 0);
            }

        }

         private void CheckRTSize(Camera c,RenderTexture texture)
        {
            if (!texture.IsCreated()) texture.Create();

            int width = c.pixelWidth;
            int height = c.pixelHeight;

            if (texture.width != c.pixelWidth || texture.height != c.pixelHeight)
            {
                texture.width = c.pixelWidth;
                texture.height = c.pixelHeight;
            }
        }
    }
}

