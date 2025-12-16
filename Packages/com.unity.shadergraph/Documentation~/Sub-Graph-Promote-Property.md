# Expose a Sub Graph property in the Inspector window

To control a [Sub Graph](Sub-graphs.md) property or keyword from the Inspector window of a material, set the property or keyword as belonging to the main shader instead of the Sub Graph. This process is known as promoting the property, or creating a nested property.

Promoting a property means the Sub Graph automatically exposes the property in the Inspector window of a material, and you don't need to create a duplicate property in the blackboard of the main shader graph. 

**Note:** When you promote a property, it no longer appears as an input port on the Sub Graph node in the parent shader graph.

## Promote a Sub Graph property or keyword

Follow these steps:

1. Open the Sub Graph in the Shader Graph editor.
2. In the Blackboard, select the property or keyword you want to promote.
3. In the **Graph Inspector** window, select the **Node Settings** tab, then enable **Promote to final Shader**.
4. Save the Sub Graph.

In the compiled shader code, the property or keyword is now promoted out of the Sub Graph and into the main shader.

**Show In Inspector** is enabled by default, so the property or keyword appears in the Inspector window of any material that uses the Sub Graph. If the property or keyword is at the top level of the Blackboard, it appears under a foldout (triangle) that has the name of the Sub Graph.

**Note**: You can't promote Gradient, Virtual Texture, or Sampler State property types.

Enabling **Promote to final Shader** also means the property or keyword inherits the same parameters as a property or keyword in a regular shader graph. For example, you can set the scope as **Global** to control the property or keyword from C# instead of the Inspector window. For more information, refer to [Property types](Property-Types.md) and [Keyword parameter reference](Keywords-reference.md).

## Expose a single property or keyword for multiple Sub Graphs

To use the Unity Editor to control a single property or keyword across multiple Sub Graphs, for example to share a single property across Sub Graphs for rain, snow, and mud, follow these steps:

1. In each Sub Graph, use the same name and type for the property or keyword.

    **Note:** The best practice is to use the same category name for each property or keyword. Otherwise Unity exposes multiple copies of the property or keyword, even though editing one still changes them all. For more information about categories, refer to [Using Blackboard categories](Blackboard.md#using-blackboard-categories).

2. To promote the property or keyword from each Sub Graph, follow the steps in the previous section.
3. Add the Sub Graphs to a shader graph. Unity exposes a single instance of the promoted property or keyword.

For an example of a promoted property, open the [template browser](template-browser.md) and select the **Terrain Standard 4 Layers** template. The template uses a nested property in the **Height Based Splat Modify** Sub Graph.

## Additional resources

- [Sub Graphs](Sub-graphs.md)
- [Property types](Property-Types.md)
- [Keywords](Keywords.md)
