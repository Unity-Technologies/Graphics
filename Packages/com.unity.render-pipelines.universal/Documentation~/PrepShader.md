# Preparing Sprites For Lighting

To light **Sprites** with **2D Lights**,  the [Sprite Renderer](https://docs.unity3d.com/Manual/class-SpriteRenderer.html) component of the Sprite is assigned a material with a Shader that reacts to 2D Lights. With the 2D Lights preview package installed, dragging Sprites onto the Scene automatically assigns the ‘Sprite-Lit-Default’ material to them which enables them to interact and appear lit by 2D Lights.

Alternatively, you can create a custom Shader that reacts to Lights with the [Shader Graph package](https://docs.unity3d.com/Packages/com.unity.shadergraph@5.6/manual/Getting-Started.html). The Shader Graph package is available for download via the Package Manager.

## Upgrading to a compatible Shader

If you are installing the 2D Lights package into a Project with pre-existing Prefabs, materials or Scenes, you will need to upgrade any materials used to a lighting compatible Shader. The following functions automatically upgrade a Scene or Project automatically in a one way process. Upgraded Scenes or Projects cannot be reverted to their previous state.

### Upgrading a Project

To upgrade all Prefabs, Scenes and Materials in your Project, go to **Window > Rendering > Render Pipeline Converter**. For information on converting assets made for a Built-in Render Pipeline project to assets compatible with 2D URP, see the page [Render Pipeline Converter](features/rp-converter.md#converters).
