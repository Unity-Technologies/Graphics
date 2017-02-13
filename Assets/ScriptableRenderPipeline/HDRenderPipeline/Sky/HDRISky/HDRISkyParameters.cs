namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [DisallowMultipleComponent]
    public class HDRISkyParameters
        : SkyParameters
    {
        public Cubemap skyHDRI;


        public override SkyRenderer GetRenderer()
        {
            return new HDRISkyRenderer(this);
        }
    }
}
