using System;

namespace UnityEngine.Rendering.UIGen
{
    //Range is Min + Max. It only add a display as a slider
    public struct DisplayAsSlider : UIDefinition.IFeatureParameter // range is better
    {
        public bool Mutate(ref UIImplementationIntermediateDocuments result, out Exception error)
        {
            throw new NotImplementedException();
        }
    }
}
