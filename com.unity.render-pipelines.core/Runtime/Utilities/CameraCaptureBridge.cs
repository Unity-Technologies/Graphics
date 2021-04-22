using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Bridge class for camera captures.
    /// </summary>
    public static class CameraCaptureBridge
    {
        private static Dictionary<Camera, HashSet<Action<RenderTargetIdentifier, CommandBuffer>>> actionDict =
            new Dictionary<Camera, HashSet<Action<RenderTargetIdentifier, CommandBuffer>>>();

        private static bool _enabled;

        /// <summary>
        /// Enable camera capture.
        /// </summary>
        public static bool enabled
        {
            get
            {
                return _enabled;
            }
            set
            {
                _enabled = value;
            }
        }

        /// <summary>
        /// Provides the set actions to the renderer to be triggered at the end of the render loop for camera capture
        /// </summary>
        /// <param name="camera">The camera to get actions for</param>
        /// <returns>Enumeration of actions</returns>
        public static IEnumerator<Action<RenderTargetIdentifier, CommandBuffer>> GetCaptureActions(Camera camera)
        {
            if (!actionDict.TryGetValue(camera, out var actions))
                return null;

            return actions.GetEnumerator();
        }

        /// <summary>
        /// Adds actions for camera capture
        /// </summary>
        /// <param name="camera">The camera to add actions for</param>
        /// <param name="action">The action to add</param>
        public static void AddCaptureAction(Camera camera, Action<RenderTargetIdentifier, CommandBuffer> action)
        {
            actionDict.TryGetValue(camera, out var actions);
            if (actions == null)
            {
                actions = new HashSet<Action<RenderTargetIdentifier, CommandBuffer>>();
                actionDict.Add(camera, actions);
            }

            actions.Add(action);
        }

        /// <summary>
        /// Removes actions for camera capture
        /// </summary>
        /// <param name="camera">The camera to remove actions for</param>
        /// <param name="action">The action to remove</param>
        public static void RemoveCaptureAction(Camera camera, Action<RenderTargetIdentifier, CommandBuffer> action)
        {
            if (camera == null)
                return;

            if (actionDict.TryGetValue(camera, out var actions))
                actions.Remove(action);
        }
    }
}
