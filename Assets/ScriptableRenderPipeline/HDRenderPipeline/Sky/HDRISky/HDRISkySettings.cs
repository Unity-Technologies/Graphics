namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [DisallowMultipleComponent]
    public class HDRISkySettings
        : SkySettings
    {
        public Cubemap skyHDRI;


        public override SkyRenderer GetRenderer()
        {
            return new HDRISkyRenderer(this);
        }
    }
}
