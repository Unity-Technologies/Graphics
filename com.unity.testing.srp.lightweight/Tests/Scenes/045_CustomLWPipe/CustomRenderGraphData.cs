namespace UnityEngine.Rendering.LWRP
{
    //[CreateAssetMenu()]
    public class CustomRenderGraphData : ScriptableRendererData
    {
        public override ScriptableRenderer Create()
        {
            return new CustomLWPipe(this);
        }
    }
}

