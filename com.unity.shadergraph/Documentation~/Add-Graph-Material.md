# Add a Shader Graph to a material

After you [create a Shader Graph Asset](Create-Shader-Graph.md), you can assign your Shader Graph to a material to start using it in your project.

For more information about materials, see [the Materials section](https://docs.unity3d.com/Documentation/Manual/Materials.html) in the Unity User Manual.

To add a Shader Graph to a material:

1. [!include[open-project-window](./snippets/sg-open-project-window.md)]

2. [!include[open-create-menu-project](./snippets/sg-open-create-menu-project.md)] **Material**. Enter a name for your new material and press Enter.

3. [!include[open-inspector-window](./snippets/sg-open-inspector-window.md)]

4. Select your new material to display it in your Inspector window.

5. In the **Shader** dropdown, select **Shader Graphs**, and select the Shader Graph you want to assign to your material.

    > [!TIP]
    > You can also quickly create a new material directly from a Shader Graph. In your Project window, right-click a Shader Graph Asset and go to **Create** &gt; **Material**. Enter a name for your new material and press Enter. The Editor automatically assigns your Shader Graph to the material.

    ![](images/)
    <!-- Add an image showing assigning a Shader Graph to a material -->

## Next steps

After you've added a Shader Graph to a material, you can apply your material to a GameObject in a scene. For more information on how to assign a material to a GameObject, see the [Materials introduction](https://docs.unity3d.com/Documentation/Manual/materials-introduction.html) in the Unity User Manual.
