# Shader Graph Color Modes

## Description

Shader Graph has the ability to display colors on nodes in your graph to aid in readability. This display uses **Color Modes** to change which colors are being displayed inside of the graph. **Color Modes** can be changed using the `Color Mode:` drop down menu in the top right hand corner of the [Shader Graph Window](Shader-Graph-Window.md).

![](images/Shader-Graph-Toolbar.png)

## Modes
| Name | Description |
|:-----|:------------|
| None | No colors are displayed on the nodes. All nodes use default gray. |
| Category | Colors are displayed on the nodes based on their category designation. See **Category Colors** below. |
| Precision | Colors are displayed on the nodes based on the current [Precision](Precision-Modes.md) type being used. |
| User Defined | Colors displayed are set by the user on a per-node basis. These are custom colors for your graph. See **User Defined Colors** below. |

### Category Colors
This mode displays colors on the nodes based on their category, found in the [Node Library](Node-Library.md)

![](images/Color-Mode-Category.png)

Current categories and their corrresponding colors are: 

| Name | Color | Hex Value |
|:-----|:------|:----------|
| Artistic | ![#DB773B](https://placehold.it/15/DB773B/000000?text=+) | #DB773B |
| Channel | ![#97D13D](https://placehold.it/15/97D13D/000000?text=+) | #97D13D |
| Input | ![#CB3022](https://placehold.it/15/CB3022/000000?text=+) | #CB3022 |
| Math | ![#4B92F3](https://placehold.it/15/4B92F3/000000?text=+) | #4B92F3 |
| Procedural | ![#9C4FFF](https://placehold.it/15/9C4FFF/000000?text=+) | #9C4FFF |
| Utility | ![#AEAEAE](https://placehold.it/15/AEAEAE/000000?text=+) | #AEAEAE |
| UV | ![#08D78B](https://placehold.it/15/08D78B/000000?text=+) | #08D78B |

**Note:** [Sub Graph](Sub-Graph.md) nodes used inside of a main [Shader Graph](Shader-Graph.md) are in the `Utility` category. When using `Category` mode, all Sub Graphs will use the `Utility` color.

### Precision Colors
This mode displays colors on the nodes based on their current precision. If a node is set to `Inherit Precision`, then the color displayed will reflect the currently active precision. See [Precision Modes](Precision-Modes.md) for more information on inheritance. 

![](images/Color-Mode-Precision.png)

Current precision types and their corresponding colors are: 
| Name | Color | Hex Value |
|:-----|:------|:----------|
| Half | ![#CB3022](https://placehold.it/15/CB3022/000000?text=+) | #CB3022 |
| Float | ![#4B92F3](https://placehold.it/15/4B92F3/000000?text=+) | #4B92F3 |

### User Defined Colors
This mode displays colors on the nodes based on user preferences. Colors in this mode are defined per-node. If a custom color has not been set, the node will display the defauly gray. 

To set a custom color for a node, right click on the desired node and find `Color` in the contextual menu. 
| Option | Description |
|:-------|:------------|
| Change... |Brings up a color picker menu and lets you set your own custom color on the node. |
| Reset  | Remove the currently selected color and set the color to default gray. |

<images>

## Overriding Default Colors
Preset colors set in `Category` and `Precision` mode can be overridden per-project. The colors are set using `.uss` Style Sheet and `HEX` color values. The default style sheet can be found in your project at `Packages/com.unity.shadergraph/Editor/Resources/Styles/ColorMode.uss` . <br> It is recommended that you create a copy of this file in your project to override the presets. Create a new folder directory in your project: `Editor/Resources/Styles` and create a copy of `ColorMode.uss` in this folder. Change the hex colors values in this file to override the presets and use your own custom colors for `Category` and `Precision` mode. 

