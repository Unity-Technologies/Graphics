using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    public interface IPreviewUpdateReceiver
    {
        void UpdatePreviewData(string listenerID, Texture newTexture);
    }
}
