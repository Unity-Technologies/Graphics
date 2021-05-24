namespace UnityEngine.Rendering.Universal
{
    //[CreateAssetMenu()]
    public class CustomRenderGraphData : ScriptableRendererData
    {
        protected override ScriptableRenderer Create()
        {
            return new CustomRenderer(this);
        }
    }
}
