# Shader Graph Preferences

Use the Shader Graph preferences to define shader graph settings for your system. To access the Shader Graph system preferences, do the following:

1. From the main menu select **Edit** > **Preferences** (macOS: **Unity** > **Settings**). The **Preferences** window is displayed. 
2. Select **Shader Graph**.

## Settings

| Name                      | Description                             |
|:--------------------------|:----------------------------------------|
| **Preview Variant Limit** | Set the maximum number of variants allowed in local projects. This is a local version of the **Shader Variant Limit** in the project settings. If your graph exceeds this maximum value, Unity returns the following error:</br> _Validation: Graph is generating too many variants. Either delete Keywords, reduce Keyword variants or increase the **Shader Variant Limit** in Preferences > Shader Graph._ </br>For more information about shader variants, refer to [Making multiple shader program variants](https://docs.unity3d.com/Manual/SL-MultipleProgramVariants.html). For more information about the Shader Variant Limit, refer to [Shader graph project settings](Shader-Graph-Project-Settings.md)|
| **Automatically Add and Remove Block Nodes** | Automatically add [Block nodes](Block-Node.md) to, or remove them from, the [Master Stack](Master-Stack.md) as needed. If you select this option, any [Block nodes](Block-Node.md) that your Shader graph needs are added to the [Master Stack](Master-Stack.md) automatically. Any incompatible [Block nodes](Block-Node.md) that have no incoming connections will be removed from the [Master Stack](Master-Stack.md). If you don't select this option, no [Block nodes](Block-Node.md) are added to, or removed from, the [Master Stack](Master-Stack.md) automatically. |
| **Enable Deprecated Nodes** | Disable warnings for deprecated nodes and properties. If you select this option, Shader Graph doesn't display warnings if your graph contains deprecated nodes or properties. If you don't select this option, Shader Graph displays warnings for deprecated nodes and properties, and any new nodes and properties you create use the latest version.  |
| **Zoom Step Size**        | Control how much the camera in Shader Graph zooms each time you roll the mouse wheel. This makes it easier to control the difference in zoom speed between the touchpad and mouse. A touchpad simulates hundreds of steps, which causes very fast zooms, whereas a mouse wheel steps once with each click. |

## Additional resources

- [Making multiple shader program variants](https://docs.unity3d.com/Manual/SL-MultipleProgramVariants.html)
- [Master Stack](Master-Stack.md)
- [Shader graph project settings](Shader-Graph-Project-Settings.md)
