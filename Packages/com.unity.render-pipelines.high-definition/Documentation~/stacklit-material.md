# Stacklit material

The StackLit Master Stack can render materials that are more complex than the [Lit Master Stack](lit-master-stack-reference.md). It includes all the features available in the Lit shader and, sometimes, provides more advanced or higher quality versions. For example, it uses a more advanced form of specular occlusion and also calculates anisotropic reflections for area lights in the same way the Lit shader does for other light types. It also takes into account light interactions between two vertically stacked physical layers, along with a more complex looking general base layer.

![](Images/HDRPFeatures-StackLitShader.png)

## Creating a StackLit Shader Graph

To create a StackLit material in Shader Graph, you can either:

* Modify an existing Shader Graph.
    1. Open the Shader Graph in the Shader Editor.
    2. In **Graph Settings**, select the **HDRP** Target. If there isn't one, go to **Active Targets,** click the **Plus** button, and select **HDRP**.
    3. In the **Material** drop-down, select **StackLit**.

* Create a new Shader Graph. Go to **Assets** > **Create** > **Shader Graph** > **HDRP** and select **StackLit Shader Graph**.

Refer to [StackLit Master Stack reference](stacklit-master-stack-reference.md) for more information.
