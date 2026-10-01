# Change how Sub Graph inputs appear on a node

Choose how each Sub Graph input appears on the sub graph node, and which values it accepts.

By default, each input you add to a [Sub Graph](Sub-graphs.md) appears as a port on the Sub Graph node in the parent shader graph. To make a Sub Graph easier to reuse, you can change how an input displays, or remove the port so that the input accepts a constant value only.

These options apply inside a Sub Graph only. To control an input from the Inspector window of a material instead, refer to [Expose a Sub Graph property in the Inspector window](Sub-Graph-Promote-Property.md).

## Display a float input as a slider or dropdown

Use the **Mode** setting to select how a Float property displays on the Sub Graph node. For example, set **Mode** to **Enum** to display a list of named options instead of a number.

To display a float input as a slider or dropdown, follow these steps:

1. Open the Sub Graph asset.
2. In the Blackboard, select the Float property.
3. In the **Graph Inspector** window, select the **Node Settings** tab.
4. Set **Mode** to **Default**, **Slider**, **Integer**, or **Enum**.
5. Set the additional settings that appear for the mode you selected.
6. Save the sub graph.

When you add the Sub Graph to a shader graph, the Sub Graph node displays a box, a slider, or a dropdown next to the input port, to match the mode you selected. If you [remove the port for the input](#set-input-value-on-node), the node displays it in the body instead.

For a description of each mode and the settings it requires, refer to [Property Types](Property-Types.md).

<a name="set-input-value-on-node"></a>
## Set an input value on the node instead of a port

Use **Disable connector** to remove the input port for a property. The input then appears in the body of the Sub Graph node, where you set its value directly.

You can enable **Disable connector** for Boolean, Color, Float, and Gradient properties. It's not available if you enable **Promote to final Shader**, because a promoted property doesn't appear on the Sub Graph node. 

To remove the port for an input, follow these steps:

1. Open the Sub Graph asset.
2. In the Blackboard, select the property.
3. In the **Graph Inspector** window, select the **Node Settings** tab.
4. Enable **Disable connector**.
5. Save the Sub Graph.

When you add the Sub Graph to a shader graph, the input no longer has a port on the Sub Graph node. Instead, the node displays the input in its body, labeled with the name of the property.

> [!IMPORTANT]
> If a parent shader graph already connects a node to this input, enabling **Disable connector** removes that connection. Check every shader graph that uses the Sub Graph after you enable this option.

## Additional resources

- [Add inputs and outputs to a Sub Graph](Add-Inputs-Outputs-Sub-Graph.md)
- [Set default inputs for a Sub Graph](Sub-Graph-Default-Property-Values.md)
- [Change the behavior of a Sub Graph with a dropdown](Change-Behaviour-Sub-Graph-Dropdown.md)
- [Property Types](Property-Types.md)
- [Switch node](Switch-Node.md)
