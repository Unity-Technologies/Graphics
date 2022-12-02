namespace UnityEngine.Rendering.HighDefinition.Compositor
{
    internal interface ICompositionFilterComponent
    {
        CompositionFilter.FilterType compositionFilterType { get; }

        CompositionFilter currentCompositionFilter { get; set; }

        bool IsActiveForCamera(HDCamera hdCamera)
        {
            hdCamera.camera.gameObject.TryGetComponent<AdditionalCompositorData>(out var layerData);
            if (layerData == null || layerData.layerFilters == null)
                return false;

            int index = layerData.layerFilters.FindIndex(x => x.filterType == compositionFilterType);
            if (index < 0)
                return false;

            // Keep the current filter for the rendering avoiding to re-fetch it later on
            currentCompositionFilter = layerData.layerFilters[index];

            return true;
        }
    }
}
