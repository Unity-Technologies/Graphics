# Shader Graph Preferences

To access the Shader Graph Project-wide settings, click **Edit** > **Preferences**, and then select **Shader Graph**.

## Settings

| Name | Description |
|:------- |:----------- |
|Shader Variant Limit| Enter a value to set the maximum number of shader variants. If your graph exceeds this maximum value, Unity throws the following error: _Validation: Graph is generating too many variants. Either delete Keywords, reduce Keyword variants or increase the Shader Variant Limit in Preferences > Shader Graph._ For more information about shader variants, see [Making multiple shader program variants](https://docs.unity3d.com/Manual/SL-MultipleProgramVariants.html). |
| Automatically Add or Remove Block Nodes | Toggle either on or off. If this option is on, when changing Graph Settings any needed Block nodes will be added to the Master Stack. Any incompatible Block nodes that have no incoming connections will be removed from the Master Stack. If this option is off, no Block nodes will be added to or removed from the Master Stack. |
Enable Deprecated Nodes | Enable this setting to turn off warnings for deprecated nodes and properties, which also allows you to create older versions of nodes and properties. If you don't enable this setting, Shader Graph displays warnings for deprecated nodes and properties, and any new nodes and properties you create use the latest version. |
