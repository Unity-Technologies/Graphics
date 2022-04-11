using UnityEngine;

namespace UnityEditor
{
    /// <summary>
    /// LightAnchorHandles describes the Handles for the LightAnchorEditorTool
    /// </summary>
    public class LightAnchorHandles
    {
        /// <summary>
        /// The light position
        /// </summary>
        public Vector3 lightPosition { get; set; }
        /// <summary>
        /// The anchor position
        /// </summary>
        public Vector3 anchorPosition { get; set; }

        LightAnchor target;

        /// <summary>
        /// Initializes and returns an instance of LightAnchorHandles
        /// </summary>
        /// <param name="target">Target object</param>
        public LightAnchorHandles(LightAnchor target)
        {
            this.target = target;
        }

        /// <summary>
        /// On GUI
        /// </summary>
        public void OnGUI()
        {
            Handles.color = Color.yellow;
            Handles.DrawDottedLine(lightPosition, anchorPosition, 2f);

            // Orient the handle rotation depending on the editor pivot rotation mode
            var handleRotation = Quaternion.identity;
            if (Tools.pivotRotation == PivotRotation.Local && target != null)
                handleRotation = target.transform.rotation;

            anchorPosition = Handles.PositionHandle(anchorPosition, handleRotation);
        }
    }
}
