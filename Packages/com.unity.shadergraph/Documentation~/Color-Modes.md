# Color Modes

## Description

Shader Graph can display colors on nodes in your graph to improve readability. This feature uses **Color Modes** to change which colors to display in the graph. Use the **Color Mode:** drop-down menu in the top right corner of the [Shader Graph Window](Shader-Graph-Window.md) to change the **Color Modes**.

![](images/Shader-Graph-Toolbar.png)

## Modes

| Name | Description |
|:-----|:------------|
| None | Does not display colors on the nodes. All nodes use the default gray. |
| Category | Displays colors on the nodes based on their assigned category. See **Category Colors** below. |
| Precision | Displays colors on the nodes based on the current [Precision Mode](Precision-Modes) in use. |
| User Defined | Lets you set the display colors on a per-node basis. These are custom colors for your graph. See **User Defined Colors** below. |

### Category Colors

This mode displays colors on the nodes based on their category. See the [Node Library](Node-Library.md) to learn about the different categories available.

![](images/Color-Mode-Category.png)

The table below lists current categories and their corresponding colors.

| Name | Color | Hex Value |
|:-----|:------|:----------|
| Artistic | ![#DB773B](https://placehold.it/15/DB773B/000000?text=+) | #DB773B |
| Channel | ![#97D13D](https://placehold.it/15/97D13D/000000?text=+) | #97D13D |
| Input | ![#CB3022](https://placehold.it/15/CB3022/000000?text=+) | #CB3022 |
| Math | ![#4B92F3](https://placehold.it/15/4B92F3/000000?text=+) | #4B92F3 |
| Procedural | ![#9C4FFF](https://placehold.it/15/9C4FFF/000000?text=+) | #9C4FFF |
| Utility | ![#AEAEAE](https://placehold.it/15/AEAEAE/000000?text=+) | #AEAEAE |
| UV | ![#08D78B](https://placehold.it/15/08D78B/000000?text=+) | #08D78B |

**Note:** [Sub Graph](Sub-Graph.md) nodes in a main [Shader Graph](index.md) fall in the Utility category. If you select **Category** mode, all Sub Graphs use the Utility color.

### Precision Colors

This mode displays colors on the nodes based on their current precision. If you set a node to **Inherit Precision**, the display color reflects the currently active precision. See [Precision Modes](Precision-Modes.md) for more information about inheritance.

### User Defined Colors

This mode displays colors on the nodes based on user preferences. In this mode, the user defines colors for each node. If a custom color is not set, the node displays in the default gray.

To set a custom color for a node, right-click on the target node to bring up the the context menu, and select **Color**.

| Option | Description |
|:-------|:------------|
| Change... |Brings up a color picker menu and lets you set your own custom color on the node. |
| Reset  | Removes the currently selected color and sets it to the default gray. |

![](images/Color-Mode-User-Defined.png)

## Overriding Default Colors

For each project, you can override preset colors in the **Category** and **Precision** modes. Unity uses a `.uss` style sheet and Hex color codes to set colors. The default style sheet in your project is  `Packages/com.unity.shadergraph/Editor/Resources/Styles/ColorMode.uss`.

The best practice is to create a copy of this file to override the presets. Under your project's **Assets** folder, create a new `Editor/Resources/Styles` folder structure, and place a copy of `ColorMode.uss` in the `Styles` folder. Change the Hex color codes in this `.uss` file to override the presets and use your own custom colors for the **Category** and **Precision** modes.
