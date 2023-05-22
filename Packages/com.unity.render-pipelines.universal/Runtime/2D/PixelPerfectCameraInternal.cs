using System;

namespace UnityEngine.Rendering.Universal
{
    internal interface IPixelPerfectCamera
    {
        int assetsPPU { get; set; }
        int refResolutionX { get; set; }
        int refResolutionY { get; set; }
        bool upscaleRT { get; set; }
        bool pixelSnapping { get; set; }
        bool cropFrameX { get; set; }
        bool cropFrameY { get; set; }
        bool stretchFill { get; set; }
    }

    [Serializable]
    internal class PixelPerfectCameraInternal : ISerializationCallbackReceiver
    {
        // Case 1061634:
        // In order for this class to survive hot reloading, we need to make the fields serializable.
        // Unity can't serialize an interface object, but does properly serialize UnityEngine.Object.
        // So we cast the reference to PixelPerfectCamera (which inherits UnityEngine.Object)
        // before serialization happens, and restore the interface reference after deserialization.
        [NonSerialized]
        IPixelPerfectCamera m_Component;
        PixelPerfectCamera m_SerializableComponent;

        internal float originalOrthoSize;
        internal bool hasPostProcessLayer;
        internal bool cropFrameXAndY = false;
        internal bool cropFrameXOrY = false;
        internal bool useStretchFill = false;
        internal int zoom = 1;
        internal bool useOffscreenRT = false;
        internal int offscreenRTWidth = 0;
        internal int offscreenRTHeight = 0;
        internal Rect pixelRect = Rect.zero;
        internal float orthoSize = 1.0f;
        internal float unitsPerPixel = 0.0f;
        internal int cinemachineVCamZoom = 1;
        internal bool requiresUpscaling = false;

        internal PixelPerfectCameraInternal(IPixelPerfectCamera component)
        {
            m_Component = component;
        }

        public void OnBeforeSerialize()
        {
            m_SerializableComponent = m_Component as PixelPerfectCamera;
        }

        public void OnAfterDeserialize()
        {
            if (m_SerializableComponent != null)
                m_Component = m_SerializableComponent;
        }

        internal void CalculateCameraProperties(int screenWidth, int screenHeight)
        {
            int assetsPPU = m_Component.assetsPPU;
            int refResolutionX = m_Component.refResolutionX;
            int refResolutionY = m_Component.refResolutionY;
            bool upscaleRT = m_Component.upscaleRT;
            bool pixelSnapping = m_Component.pixelSnapping;
            bool cropFrameX = m_Component.cropFrameX;
            bool cropFrameY = m_Component.cropFrameY;
            bool stretchFill = m_Component.stretchFill;

            cropFrameXAndY = cropFrameY && cropFrameX;
            cropFrameXOrY = cropFrameY || cropFrameX;
            useStretchFill = cropFrameXAndY && stretchFill;
            requiresUpscaling = useStretchFill;

            // zoom level (PPU scale)
            int verticalZoom = screenHeight / refResolutionY;
            int horizontalZoom = screenWidth / refResolutionX;
            zoom = Math.Max(1, Math.Min(verticalZoom, horizontalZoom));

            // off-screen RT
            useOffscreenRT = false;
            offscreenRTWidth = 0;
            offscreenRTHeight = 0;

            if (cropFrameXOrY)
            {
                useOffscreenRT = true;

                if (!upscaleRT)
                {
                    if (cropFrameXAndY)
                    {
                        offscreenRTWidth = zoom * refResolutionX;
                        offscreenRTHeight = zoom * refResolutionY;
                    }
                    else if (cropFrameY)
                    {
                        offscreenRTWidth = screenWidth;
                        offscreenRTHeight = zoom * refResolutionY;
                    }
                    else // crop frame X
                    {
                        offscreenRTWidth = zoom * refResolutionX;
                        offscreenRTHeight = screenHeight;
                    }
                }
                else
                {
                    if (cropFrameXAndY)
                    {
                        offscreenRTWidth = refResolutionX;
                        offscreenRTHeight = refResolutionY;
                    }
                    else if (cropFrameY)
                    {
                        offscreenRTWidth = screenWidth / zoom / 2 * 2;   // Make sure it's an even number by / 2 * 2.
                        offscreenRTHeight = refResolutionY;
                    }
                    else    // crop frame X
                    {
                        offscreenRTWidth = refResolutionX;
                        offscreenRTHeight = screenHeight / zoom / 2 * 2;   // Make sure it's an even number by / 2 * 2.
                    }
                }
            }
            else if (upscaleRT && zoom > 1)
            {
                useOffscreenRT = true;
                offscreenRTWidth = screenWidth / zoom / 2 * 2;        // Make sure it's an even number by / 2 * 2.
                offscreenRTHeight = screenHeight / zoom / 2 * 2;
            }

            // viewport
            if (useOffscreenRT)
            {
                // When we ask the render pipeline to create the offscreen RT for us, the size of the RT is determined by VP size.
                // That's why we set the VP size to be (m_OffscreenRTWidth, m_OffscreenRTHeight) here.
                pixelRect = new Rect(0.0f, 0.0f, offscreenRTWidth, offscreenRTHeight);
            }
            else
                pixelRect = Rect.zero;

            // orthographic size
            if (cropFrameY)
                orthoSize = (refResolutionY * 0.5f) / assetsPPU;
            else if (cropFrameX)
            {
                float aspect = (pixelRect == Rect.zero) ? (float)screenWidth / screenHeight : pixelRect.width / pixelRect.height;
                orthoSize = ((refResolutionX / aspect) * 0.5f) / assetsPPU;
            }
            else if (upscaleRT && zoom > 1)
                orthoSize = (offscreenRTHeight * 0.5f) / assetsPPU;
            else
            {
                float pixelHeight = (pixelRect == Rect.zero) ? screenHeight : pixelRect.height;
                orthoSize = (pixelHeight * 0.5f) / (zoom * assetsPPU);
            }

            // Camera pixel grid spacing
            if (upscaleRT || (!upscaleRT && pixelSnapping))
                unitsPerPixel = 1.0f / assetsPPU;
            else
                unitsPerPixel = 1.0f / (zoom * assetsPPU);
        }

        internal Rect CalculateFinalBlitPixelRect(int screenWidth, int screenHeight)
        {
            // This VP is used when the internal temp RT is blitted back to screen.
            Rect pixelRect = new Rect();

            if (useStretchFill)
            {
                // stretch (fit either width or height)
                float screenAspect = (float)screenWidth / screenHeight;
                float cameraAspect = (float)m_Component.refResolutionX / m_Component.refResolutionY;

                if (screenAspect > cameraAspect)
                {
                    pixelRect.height = screenHeight;
                    pixelRect.width = screenHeight * cameraAspect;
                    pixelRect.x = (screenWidth - (int)pixelRect.width) / 2;
                    pixelRect.y = 0;
                }
                else
                {
                    pixelRect.width = screenWidth;
                    pixelRect.height = screenWidth / cameraAspect;
                    pixelRect.y = (screenHeight - (int)pixelRect.height) / 2;
                    pixelRect.x = 0;
                }

                // Doesn't require scaling if ref resolution is a multiple of target resolution
                if (screenWidth % m_Component.refResolutionX == 0)
                {
                    requiresUpscaling = cameraAspect < screenAspect;
                }
                else if (screenHeight % m_Component.refResolutionY == 0)
                {
                    requiresUpscaling = cameraAspect > screenAspect;
                }
            }
            else
            {
                // center
                if (m_Component.upscaleRT)
                {
                    pixelRect.height = zoom * offscreenRTHeight;
                    pixelRect.width = zoom * offscreenRTWidth;
                }
                else
                {
                    pixelRect.height = offscreenRTHeight;
                    pixelRect.width = offscreenRTWidth;
                }

                pixelRect.x = (screenWidth - (int)pixelRect.width) / 2;
                pixelRect.y = (screenHeight - (int)pixelRect.height) / 2;
            }

            return pixelRect;
        }

        // Find a pixel-perfect orthographic size as close to targetOrthoSize as possible.
        internal float CorrectCinemachineOrthoSize(float targetOrthoSize)
        {
            float correctedOrthoSize;

            if (m_Component.upscaleRT)
            {
                cinemachineVCamZoom = Math.Max(1, Mathf.RoundToInt(orthoSize / targetOrthoSize));
                correctedOrthoSize = orthoSize / cinemachineVCamZoom;
            }
            else
            {
                cinemachineVCamZoom = Math.Max(1, Mathf.RoundToInt(zoom * orthoSize / targetOrthoSize));
                correctedOrthoSize = zoom * orthoSize / cinemachineVCamZoom;
            }

            // In this case the actual zoom level is cinemachineVCamZoom instead of zoom.
            if (!m_Component.upscaleRT && !m_Component.pixelSnapping)
                unitsPerPixel = 1.0f / (cinemachineVCamZoom * m_Component.assetsPPU);

            return correctedOrthoSize;
        }
    }
}
