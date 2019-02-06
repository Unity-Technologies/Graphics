namespace UnityEngine.Rendering.LWRP
{
    //[CreateAssetMenu()]
    public class CustomRenderGraphData : RendererData
    {
        public override RendererSetup Create()
        {
            return new CustomLWPipe(this);
        }
    }
}

