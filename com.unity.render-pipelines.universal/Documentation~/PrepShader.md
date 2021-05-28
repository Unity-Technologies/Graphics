# Preparing Sprites For Lighting

To light __Sprites__ with __2D Lights__,  the [Sprite Renderer](https://docs.unity3d.com/Manual/class-SpriteRenderer.html) component of the Sprite is assigned a material with a Shader that reacts to 2D Lights. With the 2D Lights preview package installed, dragging Sprites onto the Scene automatically assigns the ‘Sprite-Lit-Default’ material to them which enables them to interact and appear lit by 2D Lights.

Alternatively, you can create a custom Shader that reacts to Lights with the [Shader Graph package](https://docs.unity3d.com/Packages/com.unity.shadergraph@5.6/manual/Getting-Started.html). The Shader Graph package is available for download via the Package Manager.

## Upgrading to a compatible Shader

If you are installing the 2D Lights package into a Project with pre-existing Prefabs, materials or Scenes, you will need to upgrade any materials used to a lighting compatible Shader. The following functions automatically upgrade a Scene or Project automatically in a one way process. Upgraded Scenes or Projects cannot be reverted to their previous state.

### Upgrading a Scene

To upgrade the currently opened Scene, go to __Edit> Render Pipeline > UniversalRP 2D Renderer > Upgrade Scene To 2D Renderer__

### Upgrading a Project

To upgrade all Prefabs and materials in your Project, go to __Edit > Render Pipeline > UniversalRP 2D Renderer > Upgrade Project To 2D Renderer__
