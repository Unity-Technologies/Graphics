# Implement ray tracing with Shader Graph

To use ray-traced effects with Shader Graph, use the **Raytracing Quality** Keyword node. This node exchanges accuracy for speed, except for [Path-Traced effects](Ray-Tracing-Path-Tracing.md) which aren't affected and use the default path.

## Add the Raytracing Quality Keyword node

The Raytracing Quality Keyword node is a [Built-in Keyword node ](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest/index.html?subfolder=/manual/Keywords.html#built-in-keywords).

To add the Raytracing Quality Keyword node to the graph:

1. In the [Blackboard](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest/index.html?subfolder=/manual/Blackboard.html), click plus (**+**).
2. Go to **Keyword** > **Raytracing Quality**. This creates the keyword and makes it visible on the Blackboard.

## Use the Raytracing Quality keyword node

To use this keyword in the graph, you need to create a [Keyword Node](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest/index.html?subfolder=/manual/Keyword-Node.html). To do this, drag the **Raytracing Quality** Keyword node  from the Blackboard to the graph or open the Create Node Menu and search for **Raytracing Quality** .

![](Images/RaytracingQualityNode.png)

### Available Ports

| Name          | Direction | Type           | Description                                                  |
| :------------ | :-------- | :------------- | :----------------------------------------------------------- |
| **default**   | Input     | Vector4        | The value to use for the normal Shader Graph. This is the default path Unity uses to render this Shader Graph. |
| **raytraced** | Input     | Vector4        | The value to use for the fast Shader Graph to use with the ray-traced effects excepth the path traced one.|
| **output**    | Output    | Vector4        | Outputs is the value which will be selected based on the context this shader graph is used. |
