# Custom UI components

The sample contains a few custom UI components to reproduce the behavior of some elements such as buttons, toggles and sliders, passing data to the material assigned.
You can add these components from the main menu **Component** > **UI** > **Shader Graph Samples**.

| Component        | Description   |
|:------------|:-------------------|
| **RectTransform Size** | Passes the gameObject's RectTransform's size to the graphic's material, as a  _RectTransformSize Vector2 property. This is then fetched in a Shader Graph or Subgraph by using the RectTransform Size subgraph.|
| **Button** | Reproduces the behavior of a UI Button component. The button state is fetched in a Shader Graph or Subgraph by using the Selectable State subgraph. | 
| **Toggle** | Reproduces the behavior of a UI Toggle component. The toggle 'on' state is fetched in a Shader Graph or Subgraph by using the Toggle State subgraph. Its state as a selectable is fetched in a Shader Graph or Subgraph by using the Selectable State subgraph.|
| **Meter** | A passive meter to be used as a progress indicator or gauge. It passes a normalized value to the Graphics material as a float "_MeterValue" property. Use the MeterValue node to fetch the value in a Shader Graph or Subgraph.|
| **RangeBar** | A passive range bar to be used as a progress bar, or in combination with a Range Slider.|
| **Slider** | A custom slider, handling drag events. Its value is fetched in a Shader Graph or Subgraph by using the Slider Value subgraph. Its state as a selectable is fetched in a Shader Graph or Subgraph by using the Selectable State subgraph.|