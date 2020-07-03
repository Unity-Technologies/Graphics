# Blackboard

## Description
The Blackboard lists [Properties](https://docs.unity3d.com/Manual/SL-Properties.html) and [Keywords](Keywords) available for use in the graph. Here, you can define a range of Property and Keyword types. 

To change the path of a [Shader Graph Asset](Shader-Graph-Asset), click on its path field in the Blackboard, and enter the new path. The Unity Editor uses the Shader Graph path to list the corresponding Shader Graph Asset in the Material Inspector's **Shader** drop-down menu. Similarly, you can change the path field of a [Sub Graph](Sub-graph). The Editor uses the Sub Graph path to list the corresponding Sub Graph Asset in the [Create Node Menu](Create-Node-Menu).

![](images/blackboard_shadergraph_path.png) ![](images/blackboard_subgraph_path.png)

You can move the Blackboard anywhere in the [Shader Graph Window](Shader-Graph-Window). It always maintains the same distance from the nearest corner, even if you resize the window.

To create a new Property or Keyword, click the **Add (+)** button on the Blackboard's title bar, and select a Property or Keyword type.

To reorder items listed on the Blackboard, drag and drop them. To delete items, use the Delete key on Windows, or the Command + Backspace key combination on OS X. To rename an item, double-click on its name, and enter a new name (note: quotation marks in display names will get stripped and replaced with underscores). Drag Properties and Keywords from the Blackboard to the graph to create a corresponding node.

For a full list of Property types, see [Property Types](Property-Types).
