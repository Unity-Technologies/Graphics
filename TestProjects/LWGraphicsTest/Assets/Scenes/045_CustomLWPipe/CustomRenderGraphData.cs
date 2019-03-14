namespace UnityEngine.Rendering.LWRP
{
    //[CreateAssetMenu()]
    public class CustomRenderGraphData : ScriptableRendererData
    {
        protected override ScriptableRenderer Create()
        {
            return new CustomLWPipe(this);
        }
    }
}

