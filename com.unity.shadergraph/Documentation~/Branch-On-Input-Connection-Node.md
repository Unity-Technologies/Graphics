# Branch On Input Connection Node

## Description
This node supports conditional logic in a Sub Graph. It has two states: connected and not connected. Shader Graph determines the node's connection mode with the status of two ports. The first of these is the node's Input port itself. The second of these is the Property port of the [node representing this Sub Graph in a parent graph](Sub-graph-Node). The [Branch Node](Branch-Node) is the model for this functionality.

This functionality is not compatible with Virtual Streaming Texture properties.

### Adding branching logic
1. From your Blackboard, select the relevant **Property** and add it to your Sub Graph.
2. In the **Graph Inspector**, enable **Custom Binding** and designate a **Label**.
3. Wire the **Property** to the **Input** port of the Test Input Connection Node. The **Input** port only accepts Properties.
4. Wire one node to the **Connected** port and another to the **Not Connected** port.
5. Select an output node and connect it to the **Out** port.
6. Use the Sub Graph in your graph.

If you disable the **Custom Binding** setting, the **Property** disconnects from this node, and Unity displays a warning.
