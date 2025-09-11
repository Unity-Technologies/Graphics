# Graph Settings Tab

## Description

The **Graph Settings** tab on the [Graph Inspector](Internal-Inspector.md) makes it possible to change settings that affect the Shader Graph as a whole.

![](images/GraphSettings_Menu.png)

### Graph Settings options

| Menu Item | Description |
|:----------|:------------|
| **Precision** | Select **Single** or **Half** from the [Precision](Precision-Modes.md) dropdown menu as the graph's default Precision Mode for the entire graph. |
| **Preview Mode** | Select your preferred preview mode for a node that has a preview from the following options: <ul><li>**Inherit**:  The Unity Editor automatically selects the preview mode to use.</li><li>**Preview 2D**: Renders the output of the Sub Graph as a flat two-dimensional preview.</li><li>**Preview 3D**: Renders the output of the Sub Graph on a three-dimensional object such as a sphere.</li></ul> This property is available only when you selected a [Sub Graph](Sub-graph.md).  |
| **Active Targets** | A list that contains selected targets. You can add or remove **Active Targets** by selecting the **Add (+)** and **Remove (&minus;)** buttons, respectively. <br/>Shader Graph supports three targets: <ul><li>**Built-in**: Shaders for Unity’s [Built-In Render Pipeline](xref:um-render-pipelines).</li><li>**Custom Render Texture**: Shaders for updating [Custom Render Textures](Custom-Render-Texture.md). </li><li>**Universal**: Shaders for the [Universal Render Pipeline](xref:um-shaders-in-universalrp-reference).</li></ul> The available properties displayed depend on the targets you have added to the list. Refer to the [Shader Material Inspector window properties](xref:um-shaders-in-universalrp-reference) for the respective **Materials** you select for the **Built-in** and **Universal** targets.|

## Additional resources
- [Precision Modes](Precision-Modes.md)
- [Example Custom Render Texture with Shader Graph](Custom-Render-Texture-Example.md)
- [Custom Editor block in ShaderLab reference](xref:um-sl-custom-editor)