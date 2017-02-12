namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public abstract class LightLoopProducer : ScriptableObject
    {
        public abstract BaseLightLoop CreateLightLoop();
    }
}
