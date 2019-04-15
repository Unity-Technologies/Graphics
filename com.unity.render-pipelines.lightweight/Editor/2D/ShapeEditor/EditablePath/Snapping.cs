using UnityEngine;
using UnityEditor;

namespace UnityEditor.Experimental.Rendering.LWRP.Path2D
{
    internal class Snapping : ISnapping<Vector3>
    {
        public Vector3 Snap(Vector3 position)
        {
            return new Vector3(
                Snap(position.x, EditorPrefs.GetFloat("MoveSnapX", 1f)),
                Snap(position.y, EditorPrefs.GetFloat("MoveSnapY", 1f)),
                position.z);
        }

        private float Snap(float value, float snap)
        {
            return Mathf.Round(value / snap) * snap;
        }
    }
}
