# Bake multiple scenes together with Baking Sets

If you [load multiple scenes simultaneously](https://docs.unity3d.com/Documentation/Manual/MultiSceneEditing.html) in your project, for example if you load multiples scenes at the same time in an open world game, you can add the scenes to a single Baking Set so you can bake the lighting for all the scenes together.

Refer to [Understanding probe volumes](probevolumes-concept.md#baking-sets) for more information about Baking Sets.

## Create a Baking Set

To place multiple scenes in a single Baking Set and bake them together, follow these steps:

1. From the main menu, select **Window** > **Rendering** > **Lighting**.
2. Set **Baking Mode** to **Baking Set**.
2. In **Current Baking Set**, select an existing Baking Set asset, or select **New** to create a new Baking Set.
4. Use the **Add** (**+**) button to add scenes. 

You can only add each scene to a single Baking Set.

To remove a scene from a Baking Set, select the scene in the **Scenes in Baking Set** list, then select the **Remove** (**-**) button.

## Bake a Baking Set

Select **Generate Lighting** to bake the lighting in all the scenes in a baking set.

The Universal Render Pipeline (URP) uses the settings from the Baking Set, and serializes the results in the `Assets` folder, in a subfolder with the same name as the active scene. You can move or rename the folder.

For faster iteration times, disable **Bake** next to a scene name. This stops Unity baking lighting data for this scene. This might result in incomplete data, but it can help reduce baking time when you're iterating on parts of a large world.

### Load a scene

Unity doesn't automatically load the scenes in a Baking Set when you select the scene in the **Scenes** list. To load a scene, select **Load Baking Set**.

When you load multiple scenes together, the lighting might be too bright because URP combines light from all the scenes. Refer to [Set up multiple Scenes](https://docs.unity3d.com/Manual/setupmultiplescenes.html) for more information on loading and unloading Scenes.

You can load multiple scenes together only if they belong to the same Baking Set.
