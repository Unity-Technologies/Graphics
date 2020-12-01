# Creating a new Shader Graph Asset

After you configure an SRP, you can create a new Shader Graph Asset. Right-click the Project window, locate **Create** > **Shader** in the context menu, then select your desired type of Shader Graph.

The type of Shader Graph available is dependent on the render pipelines present in your project. Some options may or may not be present based on the render pipelines.

The following options are always available:

|       |  |
|:------------|:----------------|:------------|
| Blank Shader Graph | A completely blank shader graph. No target is selected and no blocks are added to the Master Stack. |
| Sub Graph |  A blank sub graph asset. |

A sub menu for each installed render pipeline may be present containing template stacks for standard shading models ( Lit, Unlit, etc ).

For a full list of provided options, see the [Universal Render Pipeline]() and [High Definition Render Pipeline]() documentation.

For this example, Universal is installed so a Unversal Lit Shader Graph has been created.

Double-click your newly created Shader Graph Asset to open it in the Shader Graph window.

## Shader Graph window

The Shader Graph window consists of the Master Stack, the Preview Window, the Blackboard, and the Graph Inspector.

![](images/ShaderGraphWindow.png)

### Master Stack

The final connection that determines your shader output. See [Master Stack]() for more information.

![]()

### Preview window

An area to preview the current shader output. Here, you can rotate the object, and zoom in and out. You can also change the basic mesh on which the shader is previewed. See [Main Preview](Master-Preview) for more information.

![img](images/MainPreview.png)

### Blackboard

An area that contains all of the shader's properties in a single, collected view. Use the Blackboard to add, remove, rename, and reorder properties. See [Blackboard](Blackboard) for more information.

![](images/Blackboard.png)

After you've set up a project, and become familiar with the Shader Graph window, see [My first Shader Graph](First-Shader-Graph) for more information on how to get started.

### Internal Inspector

An area that contains information contextual to whatever the user is currently clicking on. It's a window that automatically is hidden by default and only appears when something is selected that can be edited by the user. Use the Internal Inspector to display and modify properties, node options, and the graph settings. See [Internal Inspector](Internal-Inspector.md) for more information.

![](images/Inspector.png)
