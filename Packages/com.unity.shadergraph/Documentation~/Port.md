# Port

## Description

A **Port** defines an input or output on a [Node](Node.md). Connecting [Edges](Edge.md) to a **Port** allows data to flow through the Shader Graph node network.

Each **Port** has a [Data Type](Data-Types.md) which defines what edges can be connected to it. Each data type has an associated color for identifying its type.

Only one edge can be connected to any input **Port** but multiple edges can be connected to an output **Port**.

You can open a contextual [Create Node Menu](Create-Node-Menu.md) by dragging an edge from a **Port** with left mouse button and releasing it in an empty area of the workspace.

### Default Inputs

Each **Input Port**, a **Port** on the left side of a node implying that it is for inputting data into the node, has a **Default Input**. This appears as a small field connected to the **Port** when there is no edge connected. This field will display an input for the ports data type unless the **Port** has a [Port Binding](Port-Bindings.md). If a **Port** does have a port binding the default input field might display a special field, such as a dropdown for selecting UV channels, or just a label to help you understand the intended input, such as coordinate space labels for geometry data.
