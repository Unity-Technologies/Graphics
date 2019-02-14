namespace UnityEngine.Rendering.LWRP
{
    //[CreateAssetMenu()]
    public class CustomRenderGraphData : RendererData
    {
        public override ScriptableRenderer Create()
        {
            return new CustomLWPipe(this);
        }
    }
}

