# Prepare and upgrade sprites and projects for lighting

To light a sprite with [2D lights](2DLightProperties.md), first go to the [Sprite Renderer](xref:class-SpriteRenderer) component of the sprite and assign a material with a Shader that reacts to 2D lights. When you drag sprites onto the scene, Unity automatically assigns the `Sprite-Lit-Default` material to them which enables them to interact and appear lit by 2D lights.

You can also [create a custom Shader](ShaderGraph.md) that reacts to lights with the [Shader Graph package](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest).

## Upgrading existing materials

If you are installing the URP package into an existing project with preexisting prefabs, materials or scenes, you will need to upgrade any materials used to a lighting compatible Shader if you want to use the package's 2D lighting features. 

**Warning:** The following task automatically upgrades a scene or project in a one way process. Unity can't revert upgraded scenes or projects to their previous state. Before you start this task, back up any files you don't want to lose or converted.

To upgrade your project, go to **Window > Rendering > Render Pipeline Converter**. Enable **Material Upgrade** and then select **Convert Assets** to begin the upgrade.

For information on converting assets made for a Built-in Render Pipeline project to assets compatible with 2D URP, refer to [Render Pipeline Converter](features/rp-converter.md#converters).