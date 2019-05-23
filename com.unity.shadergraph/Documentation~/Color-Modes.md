# Shader Graph Color Modes

## Description

Shader Graph has the ability to display colors on nodes in your graph to aid in readability. This display uses **Color Modes** to change which colors are being displayed inside of the graph. **Color Modes** can be changed using the `Color Mode:` drop down menu in the top right hand corner of the [Shader Graph Window](Shader-Graph-Window.md).

<image>

## Modes
| Name | Description |
|:-----|:------------|
| None | No colors are displayed on the nodes. All nodes use default gray. |
| Category | Colors are displayed on the nodes based on their category designation. See **Category Colors** below. |
| User Defined | Colors displayed are set by the user on a per-node basis. These are custom colors for your graph. See **User Defined Colors** below. |

### Category Colors
This mode displays colors on the nodes based on their category, found in the [Node Library](Node-Library.md)

<image>

Current categories and their corrresponding colors are: 

| Name | Color |
|:-----|:------|
| Artistic | <hex value> |
| Channel | <hex value> |
| Input | <hex value> |
| Master | <hex value> |
| Math | <hex value> |
| Procedural | <hex value> |
| Utility | <hex value> |
| UV | <hex value> |

**Note:** [Sub Graph](Sub-Graph.md) nodes used inside of a main [Shader Graph](Shader-Graph.md) are in the `Utility` category. When using `Category` mode, all Sub Graphs will use the `Utility` color.

### User Defined Colors
This mode displays colors on the nodes based on user preferences. Colors in this mode are defined per-node. If a custom color has not been set, the node will display the defauly gray. 

To set a custom color for a node, right click on the desired node and find `Color` in the contextual menu. 
| Option | Description |
|:-------|:------------|
| Change... |Brings up a color picker menu and lets you set your own custom color on the node. |
| Reset  | Remove the currently selected color and set the color to default gray. |

<images>

## Overriding Default Colors
Preset colors set in `Category` mode can be overridden per-project. The colors are set using `.uss` Style Sheet and `HEX` color values. 
The default style sheet can be found in your project at `Packages/com.unity.shadergraph/Editor/Resources/Styles/ColorMode.uss` . It is recommended that you create a copy of this file in your project to override the presets. Create a new folder directory in your project: `Editor/Resources/Styles` and create a copy of `ColorMode.uss` in this folder. Change the hex colors values in this file to override the presets and use your own custom colors for `Category` mode. 

