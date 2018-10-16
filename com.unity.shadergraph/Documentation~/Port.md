# Port

## Description

A **Port** defines an input or output on a [Node](Node.md). Connecting [Edges](Edge.md) to a **Port** allows data to flow through the [Shader Graph](Shader-Graph.md) node network.

Each **Port** has a [Data Type](Data-Types.md) which defines what [Edges](Edge.md) can be connected to it. Each [Data Type](Data-Types.md) has an associated color for identifying its type.

Only one [Edge](Edge.md) can be connected to any input **Port** but multiple [Edges](Edge.md) can be connected to an output **Port**.

You can open a contextual [Create Node Menu](Create-Node-Menu.md) by dragging an [Edge](Edge.md) from a **Port** with left mouse button and releasing it in an empty area of the workspace.

### Default Inputs

Each **Input Port**, a **Port** on the left side of a [Node](Node.md) implying that it is for inputting data into the [Node](Node.md), has a **Default Input**. This appears as a small field connected to the **Port** when there is no [Edge](Edge.md) connected. This field will display an input for the ports [Data Type](Data-Types.md) unless the **Port** has a [Port Binding](Port-Bindings.md). If a **Port** does have a [Port Binding](Port-Bindings.md) the default input field may display a special field, such as a dropdown for selecting UV channels, or just a label to help you undestand the intended input, such as coordindate space labels for geometry data.