using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Rendering.HighDefinition.Compositor
{
    internal enum BackgroundFitMode
    {
        Stretch,
        FitHorizontally,
        FitVertically
    };

    // This class store some additional per-camera data (filters, custom clear modes, etc) that are used by the compositor.
    internal class AdditionalCompositorData : MonoBehaviour
    {
        public Texture clearColorTexture = null;
        public bool clearAlpha = true;    // Clearing the alpha allows the post process to run only on the pixels covered by a stacked camera (and not the previous ones).
        public BackgroundFitMode imageFitMode = BackgroundFitMode.Stretch;
        public List<CompositionFilter> layerFilters;

        public void Init(List<CompositionFilter> layerFilters, bool clearAlpha)
        {
            this.layerFilters = new List<CompositionFilter>(layerFilters);
            this.clearAlpha = clearAlpha;
        }

        public void ResetData()
        {
            clearColorTexture = null;
            clearAlpha = true;
            imageFitMode = BackgroundFitMode.Stretch;

            if (layerFilters !=null)
            {
                layerFilters.Clear();
                layerFilters = null;
            }

        }
    }
}
