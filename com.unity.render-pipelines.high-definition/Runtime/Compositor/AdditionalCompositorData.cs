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
        public Texture m_clearColorTexture = null;
        public bool m_clearAlpha = true;    // Clearing the alpha allows the post process to run only on the pixels covered by a stacked camera (and not the previous ones).
        public BackgroundFitMode m_imageFitMode = BackgroundFitMode.Stretch;
        public List<CompositionFilter> m_layerFilters;

        public void Init(List<CompositionFilter> layerFilters, bool clearAlpha)
        {
            m_layerFilters = new List<CompositionFilter>(layerFilters);
            m_clearAlpha = clearAlpha;
        }

        public void Reset()
        {
            m_clearColorTexture = null;
            m_clearAlpha = true;
            m_imageFitMode = BackgroundFitMode.Stretch;

            if (m_layerFilters !=null )
            {
                m_layerFilters.Clear();
            }

        }
    }
}
