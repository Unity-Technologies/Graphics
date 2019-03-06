namespace UnityEngine.Experimental.Rendering.LWRP
{
    //[CreateAssetMenu()]
    public class CustomRenderGraphData : IRendererData
    {
        public override IRendererSetup Create()
        {
            return new CustomLWPipe(this);
        }
    }
}

