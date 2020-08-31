# Graph Inspector

## Description

The **Graph Inspector** lets you interact with any selectable graph elements and graph-wide settings for a [Shader Graph Asset](Shader-Graph-Asset.md). You can use the **Graph Inspector** to edit attributes and default values.

When you open a Shader Graph, the **Graph Inspector** displays the **[Graph Settings](Graph-Settings-Menu.md)** tab by default. Graph-wide settings for that specific Shader Graph appear in this tab.

Select a node in the graph to display settings available for that node in the **Graph Inspector**. Settings available for that node appear in the **Node Settings** tab of the Graph Inspector. For example, if you select a Property node either in the graph or the [Blackboard](Blackboard.md), the **Node Settings** tab displays attributes of the Property that you can edit.

![](images/InternalInspectorBlackboardProperty.png) 

Graph elements that currently work with the Graph Inspector:

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


Graph elements that currently do not work with the Graph Inspector:

- Edges
- [Sticky Notes](Sticky-Notes.md)
- Groups