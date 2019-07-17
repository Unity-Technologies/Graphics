namespace UnityEngine.Rendering.LWRP
{
    //[CreateAssetMenu()]
    public class CustomRenderGraphData : UnityEngine.Rendering.Universal.ScriptableRendererData
    {
        protected override UnityEngine.Rendering.Universal.ScriptableRenderer Create()
        {
            return new CustomLWPipe(this);
        }
    }
}

