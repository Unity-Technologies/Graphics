using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Utility class to connect SRP to automated test framework.
    /// </summary>
    public static class XRGraphicsAutomatedTests
    {
        // XR tests can be enabled from the command line. Cache result to avoid GC.
        static bool activatedFromCommandLine
        {
#if UNITY_EDITOR
            get => Array.Exists(Environment.GetCommandLineArgs(), arg => arg == "-xr-reuse-tests");
#elif XR_REUSE_TESTS_STANDALONE
            get => true;
#else
            get => false;
#endif
        }

        /// <summary>
        /// Used by render pipelines to initialize XR tests.
        /// </summary>
        public static bool enabled { get; } = activatedFromCommandLine;

        /// <summary>
        /// Set by automated test framework and read by render pipelines.
        /// </summary>
        public static bool running = false;

        // Helper function to override the XR default layout using settings of new camera
        internal static void OverrideLayout(XRLayout layout, Camera camera)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            if (enabled && running)
            {
                var camProjMatrix = camera.projectionMatrix;
                var camViewMatrix = camera.worldToCameraMatrix;

                if (camera.TryGetCullingParameters(false, out var cullingParams))
                {
                    cullingParams.stereoProjectionMatrix = camProjMatrix;
                    cullingParams.stereoViewMatrix = camViewMatrix;
                    cullingParams.stereoSeparationDistance = 0.0f;

                    List<(Camera, XRPass)> xrPasses = layout.GetActivePasses();
                    for (int passId = 0; passId < xrPasses.Count; passId++)
                    {
                        var xrPass = xrPasses[passId].Item2;
                        xrPass.AssignCullingParams(xrPass.cullingPassId, cullingParams);

                        for (int viewId = 0; viewId < xrPass.viewCount; viewId++)
                        {
                            var projMatrix = camProjMatrix;
                            var viewMatrix = camViewMatrix;

                            bool isFirstViewMultiPass = xrPasses.Count == 2 && passId == 0;
                            bool isFirstViewSinglePass = xrPasses.Count == 1 && viewId == 0;

                            if (isFirstViewMultiPass || isFirstViewSinglePass)
                            {
                                // Modify the render viewpoint and frustum of the first view in order to
                                // distinguish it from the final view used for image comparison.
                                // This is a trick to help detect issues related to view indexing.
                                var planes = projMatrix.decomposeProjection;
                                planes.left *= 0.44f;
                                planes.right *= 0.88f;
                                planes.top *= 0.11f;
                                planes.bottom *= 0.33f;
                                projMatrix = Matrix4x4.Frustum(planes);
                                viewMatrix *= Matrix4x4.Translate(new Vector3(.34f, 0.25f, -0.08f));
                            }

                            XRView xrView = new XRView(projMatrix, viewMatrix, xrPass.GetViewport(viewId), null, xrPass.GetTextureArraySlice(viewId));
                            xrPass.AssignView(viewId, xrView);
                        }
                    }
                }
            }
#endif
        }
    }
}
