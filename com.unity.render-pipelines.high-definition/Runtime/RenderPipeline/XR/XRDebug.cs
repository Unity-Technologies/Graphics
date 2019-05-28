using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public enum XRDebugMode
    {
        None,
        Composite,
    }

    public static class XRDebugMenu
    {
        public static XRDebugMode debugMode { get; set; }
        public static bool displayCompositeBorders;
        public static bool animateCompositeTiles;

        static GUIContent[] debugModeStrings = null;
        static int[] debugModeValues = null;

        public static void Init()
        {
            debugModeValues = (int[])Enum.GetValues(typeof(XRDebugMode));
            debugModeStrings = Enum.GetNames(typeof(XRDebugMode))
                .Select(t => new GUIContent(t))
                .ToArray();
        }

        public static void Reset()
        {
            debugMode = XRDebugMode.None;
            displayCompositeBorders = false;
            animateCompositeTiles = false;
        }

        public static void AddWidgets(List<DebugUI.Widget> widgetList, Action<DebugUI.Field<int>, int> RefreshCallback)
        {
            widgetList.AddRange(new DebugUI.Widget[]
            {
                new DebugUI.EnumField { displayName = "XR Debug Mode", getter = () => (int)debugMode, setter = value => debugMode = (XRDebugMode)value, enumNames = debugModeStrings, enumValues = debugModeValues, getIndex = () => (int)debugMode, setIndex = value => debugMode = (XRDebugMode)value, onValueChanged = RefreshCallback },
            });

            if (debugMode == XRDebugMode.Composite)
            {
                widgetList.Add(new DebugUI.Container
                {
                    children =
                    {
                        new DebugUI.BoolField { displayName = "Display borders", getter = () => displayCompositeBorders, setter = value => displayCompositeBorders = value },
                        new DebugUI.BoolField { displayName = "Animate tiles",   getter = () => animateCompositeTiles, setter = value => animateCompositeTiles = value }
                    }
                });
            }
        }
    }

    public partial class XRSystem
    {
        private readonly string debugVolumeName = "XRDebugVolume";

        private GameObject m_debugVolume = null;
        private GameObject debugVolume { get => m_debugVolume ?? GameObject.Find(debugVolumeName); set => m_debugVolume = value; }

        // Setup a vignette effect to make a thin red border around each view
        void CreateDebugVolume()
        {
            debugVolume =  new GameObject(debugVolumeName);
            debugVolume.hideFlags = HideFlags.HideInHierarchy;

            var volume = debugVolume.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = float.MaxValue;
            volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();

            var vignette = volume.profile.Add<Vignette>();
            vignette.active = false;
            vignette.intensity.Override(0.6f);
            vignette.smoothness.Override(0.2f);
            vignette.roundness.Override(0.1f);
            vignette.color.Override(Color.red);
        }

        void DestroyDebugVolume()
        {
            if (debugVolume != null)
            {
                Object.DestroyImmediate(debugVolume);
                debugVolume = null;
            }
        }

        bool ProcessDebugMode(bool xrEnabled, Camera camera)
        {
            if (XRDebugMenu.debugMode == XRDebugMode.None)
            {
                DestroyDebugVolume();
                return false;
            }

            if (camera.cameraType != CameraType.Game || xrEnabled)
                return false;

            if (debugVolume == null)
                CreateDebugVolume();

            Rect fullViewport = camera.pixelRect;

            // Split into 4 tiles covering the original viewport
            int tileCountX = 2;
            int tileCountY = 2;
            float splitRatio = 2.0f;

            if (XRDebugMenu.animateCompositeTiles)
                splitRatio = 2.0f + Mathf.Sin(Time.time);

            // Use frustum planes to split the projection into 4 parts
            var frustumPlanes = camera.projectionMatrix.decomposeProjection;

            for (int tileY = 0; tileY < tileCountY; ++tileY)
            {
                for (int tileX = 0; tileX < tileCountX; ++tileX)
                {
                    var xrPass = XRPass.Create(framePasses.Count, camera.targetTexture);

                    float spliRatioX1 = Mathf.Pow((tileX + 0.0f) / tileCountX, splitRatio);
                    float spliRatioX2 = Mathf.Pow((tileX + 1.0f) / tileCountX, splitRatio);
                    float spliRatioY1 = Mathf.Pow((tileY + 0.0f) / tileCountY, splitRatio);
                    float spliRatioY2 = Mathf.Pow((tileY + 1.0f) / tileCountY, splitRatio);

                    var planes = frustumPlanes;
                    planes.left   = Mathf.Lerp(frustumPlanes.left,   frustumPlanes.right, spliRatioX1);
                    planes.right  = Mathf.Lerp(frustumPlanes.left,   frustumPlanes.right, spliRatioX2);
                    planes.bottom = Mathf.Lerp(frustumPlanes.bottom, frustumPlanes.top,   spliRatioY1);
                    planes.top    = Mathf.Lerp(frustumPlanes.bottom, frustumPlanes.top,   spliRatioY2);

                    float tileOffsetX = spliRatioX1 * fullViewport.width;
                    float tileOffsetY = spliRatioY1 * fullViewport.height;
                    float tileSizeX = spliRatioX2 * fullViewport.width - tileOffsetX;
                    float tileSizeY = spliRatioY2 * fullViewport.height - tileOffsetY;

                    Rect viewport = new Rect(fullViewport.x + tileOffsetX, fullViewport.y + tileOffsetY, tileSizeX, tileSizeY);
                    Matrix4x4 proj = camera.orthographic ? Matrix4x4.Ortho(planes.left, planes.right, planes.bottom, planes.top, planes.zNear, planes.zFar) : Matrix4x4.Frustum(planes);

                    xrPass.AddView(proj, camera.worldToCameraMatrix, viewport);
                    AddPassToFrame(camera, xrPass);
                }
            }

            if (debugVolume.GetComponent<Volume>().profile.TryGet<Vignette>(out Vignette vignette))
                vignette.active = XRDebugMenu.displayCompositeBorders;

            return true;
        }
    }
}
