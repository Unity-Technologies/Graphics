# Custom nodes

The sample contains a few custom shader graph nodes to facilitate the setup.
The custom nodes are broken into two categories:
- **Inline Properties** - These nodes automatically add a hidden property to the shader graph and fetch its value.
- **Branch** - These nodes enable branching on the value of specific inputs such as a Button state.

## Inline Property Nodes
These nodes automatically add a hidden property to the shader graph and fetch its value.
You can also just manually add the property to the shader graph, but these custom nodes let you nest them in subgraphs.

| Node           | Description        |
|:---------------|:-------------------|
| **RectTransform Size** | Adds a _RectTransformSize Vector2 hidden property to the graph and outputs its value. This is fed by the RectTransformSize component required by some subgraphs to perform aspect ratio based math.
| **Selectable State** | Adds a _State float hidden property to the graph and outputs its value. This is fed by the Selectable components (CustomButton, CustomToggle and Slider). The value represents the mutually exclusive state such as: **0 - Normal**, **1 - Highlighted**, **2 - Pressed**, **3 - Selected**, **4 - Disabled** |
| **Toggle State** | Adds a _isOn Boolean hidden property to the graph and outputs its value. This is fed by the CustomToggle component. |
| **Meter Value** | Adds a _MeterValue float hidden property to the graph and outputs its value. This is fed by the Meter component and allows creating progress, health or other indicators. |
| **Slider Value** | Adds a _SliderValue Vector3 hidden property to the graph and outputs its value as: **Value** - The normalized Slider Value as float. **Direction** - The normalized Slider Direction as Vector2. This is fed by the Slider component and allows creating sliders of which the direction and value can be set from the component. |
| **Range Bar** | Adds a _RangeBar Vector4 hidden property to the graph and outputs its value as: **Min** - The normalized Slider Min Value as float. **Max** - The normalized Slider Max Value as float. **Direction** - The normalized Slider Direction as Vector2. This is fed by the RangeBar component and allows creating range bars of which the direction and min/max values can be set from the component. |

## Branch Nodes
These nodes let you branch on the value of specific inputs such as a Button state.
The motivation behind making them custom nodes rather than subgraphs lies in the ability to feature dynamic ports.

### SelectableBranch
This node lets you branch depending on the state of a Selectable element such as a CustomButton.