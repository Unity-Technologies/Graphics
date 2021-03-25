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

        /// <summary>
        /// Initializes and returns an instance of LightAnchorHandles
        /// </summary>
        public LightAnchorHandles()
        {
        }

        /// <summary>
        /// On GUI
        /// </summary>
        public void OnGUI()
        {
            Handles.color = Color.yellow;
            Handles.DrawDottedLine(lightPosition, anchorPosition, 2f);

            anchorPosition = Handles.PositionHandle(anchorPosition, Quaternion.identity);
        }
    }
}
