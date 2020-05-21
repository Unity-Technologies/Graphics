# Internal Inspector

## Description
The Inspector is used to interact with any graph elements that the user can select. You can edit their attributes and default values.  

To inspect something in the graph, the user must select it. Whenever the user selects something that can be interacted with through the Inspector, the Inspector window will become visible.

The inspector by default is hidden when the graph window is opened, and the user must select something before it will show up.

When the user selects something, for instance, a Property Node from the graph or the [Blackboard](Blackboard.md), then the Inspector will appear and display the attributes belonging to that Property that the user can edit.

![](images/InternalInspectorBlackboardProperty.png) 

Graph elements that can be inspected:

- [Properties](https://docs.unity3d.com/Manual/SL-Properties.html)

    ![](images/InternalInspectorGraphProperty.png)

- [Keywords](Keywords.md)

    ![](images/keywords_enum.png)

- [Custom Function nodes](Custom-Function-Node.md)

    ![](images/Custom-Function-Node-File.png)

- [Subgraph Output nodes](Subgraph-Output-Node.md)

    ![](images/Inspector-SubgraphOutput.png)

- [Per-node precision](Precision-Types.md)

    ![](images/Inspector-PerNodePrecision.png)


Graph elements that cannot (currently) be inspected:
- Edges
- [Sticky Notes](Sticky-Notes.md)
- Groups

