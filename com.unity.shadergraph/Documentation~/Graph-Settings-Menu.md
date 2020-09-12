# Graph Settings Menu

## Description

The **Graph Settings** tab on the **[Graph Inspector](Internal-Inspector.md)** lets you change settings that affect the Shader Graph as a whole. Click the **Graph Settings** tab in the **Graph Inspector** to display the settings. If you don't see the **Graph Inspector**, click the **Graph Inspector** button on the [Shader Graph title bar](Shader-Graph-Window.md) to make it appear.

![](images/GraphSettings_Menu.png)

### Graph Settings Menu

| Menu Item | Description |
|:----------|:------------|
| Precision | A [Precision Type](Precision-Types.md) drop-down menu that lets you set the precision for the entire graph. |
| Targets | A drop-down menu that lists the available [Targets](Graph-Target.md) you can select for the graph. By default, **Nothing** and **Everything** are always available. |
| Reorder data | A reorderable list that contains the Targets you've selected. Lets you change the order in which the final data appears in the generated shader file.|

Target-specific settings appear below the standard setting options. The displayed Target-specific settings change according to which Targets you select.