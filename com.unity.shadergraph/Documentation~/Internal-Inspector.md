# Graph Inspector

## Description
The **Graph Inspector** is used to edit settings of selectable graph elements as well as graph-wide settings for the [Shader Graph Asset](Shader-Graph-Asset.md). You can edit their attributes and default values.  

To inspect something in the graph, the user must select it. These settings are available in **Node Settings** tab of the **Graph Inspector**.

The **Graph Inspector** displays the **Graph Settings** tab by default when a Shader Graph is opened.

The **Node Settings** tab will display the attributes belonging to selected items that the user can edit. Multiple elements can be selected and edited at once.

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

