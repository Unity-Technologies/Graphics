# Shader Graph preferences reference

Define preferences for Shader Graph behaviors in your project and your shader creation workflows.

To open the preferences, go to **Edit** > **Preferences** > **Shader Graph** (macOS: **Unity** > **Settings** > **Shader Graph**).

| Property | Description |
| :--- | :--- |
| **Preview Variant Limit** | Sets the maximum number of variants allowed in local projects. This is a local version of the **Shader Variant Limit** in the project settings. If your graph exceeds this maximum value, Unity returns the following error:<br/> _Validation: Graph is generating too many variants. Either delete Keywords, reduce Keyword variants, or increase the **Shader Variant Limit** in Preferences > Shader Graph._ <br/>For more information about shader variants, refer to [Making multiple shader program variants](https://docs.unity3d.com/Manual/SL-MultipleProgramVariants.html). For more information about the Shader Variant Limit, refer to [Shader graph project settings](Shader-Graph-Project-Settings.md) |
| **Automatically Add and Remove Block Nodes** | When enabled, Shader Graph automatically adds the required [Block nodes](Block-Node.md) for the asset's Target or material type to the [Master Stack](Master-Stack.md), and removes any incompatible Block nodes that have no connections and default values. When disabled, you must manually add and remove all Block nodes. |
| **Enable Deprecated Nodes** | Disables warnings for deprecated nodes and properties. When enabled, Shader Graph doesn't display warnings if your graph contains deprecated nodes or properties. When disabled, Shader Graph displays warnings for deprecated nodes and properties, and the new nodes and properties you create use the latest version. |
| **Zoom Step Size** | Adjusts how much the Shader Graph camera zooms with each mouse wheel movement. This helps balance zoom speed, since touchpads can zoom much faster than regular mouse wheels.<br/>Only affects materials created automatically, such as when you make a new shader graph from a Decal Projector or Fullscreen Renderer Feature. |

## Additional resources

- [Making multiple shader program variants](https://docs.unity3d.com/Manual/SL-MultipleProgramVariants.html)
- [Master Stack](Master-Stack.md)
- [Shader graph project settings](Shader-Graph-Project-Settings.md)
