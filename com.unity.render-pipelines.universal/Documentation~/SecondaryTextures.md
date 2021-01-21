# Setting up normal map and mask Textures

2D Lights can interact with __normal map__ and __mask__ Textures linked to Sprites to create advanced lighting effects, such as [normal mapping](https://en.wikipedia.org/wiki/Normal_mapping). Assign these additional Textures to Sprites by using the [Sprite Editor](https://docs.unity3d.com/Manual/SpriteEditor.html)’s [Secondary Textures](https://docs.unity3d.com/Manual/SpriteEditor-SecondaryTextures.html) module. First select a Sprite, and open the [Sprite Editor](https://docs.unity3d.com/Manual/SpriteEditor.html) from its Inspector window. Then select the __Secondary Textures__ module from the drop-down menu at the top left of the editor window.

![](Images/2D/ST_ModuleSelect.png)

## Adding a Secondary Texture

In the Secondary Textures editor, select a Sprite to add Secondary Textures to. With a Sprite selected, the __Secondary Textures__ panel appears at the bottom right of the editor window. The panel displays the list of Secondary Textures currently assigned to the selected Sprite. To add a new Secondary Texture to the Sprite, select + at the bottom right of the list.

![](Images/2D/ST_ListField.png)

This adds a new entry to the list with the ‘Name’ and ‘Texture’ boxes. Enter a custom name into the Name box, or select the arrow to the right of the Name box to open the drop-down list of suggested names. These suggested names can include suggestions from installed Unity packages, as the Secondary Textures may need to have specific names to interact correctly with the Shaders in these packages to produce their effects.

The 2D Lights package suggests the names ‘MaskTex’ and ‘NormalMap’. Select the name that matches the function of the selected Texture - select ‘MaskTex’ for a masking Texture, or ‘NormalMap’ for a normal map Texture.  Naming these Textures correctly allow them to interact with the 2D Lights Shaders to properly produce the various lighting effects.

![](Images/2D/ST_Names.png)

To select the Texture Asset for this Secondary Texture entry, drag the Texture Asset directly onto the Texture field, or open the __Object Picker__ window by selecting the circle to the right of the Texture box.

![](Images/2D/ST_ObjectDrag.png)

Secondary Textures are sampled with the same UV coordinates as the Texture of the selected Sprite. Align the Secondary Textures with the main Sprite Texture to ensure that additional Texture effects are displayed correctly.

![](Images/2D/ST_Align.png)

To preview the Secondary Texture in the __Sprite Editor__ window, select an entry in the list. This automatically hides the Sprite’s main Texture. Click outside of the Secondary Textures list to deselect the entry, and the main Sprite Texture becomes visible again.

![](Images/2D/ST_Preview.png)

## Deleting a Secondary Texture

To delete a Secondary Texture, select it from the list and then select - at the bottom right of the window. This automatically removes the entry.

![](Images/2D/ST_Delete.png)

## Applying

Select __Apply__ at the top of the editor to save your entries. Invalid entries without a Name or an assigned Texture are automatically removed when changes are applied.

![](Images/2D/ST_Apply.png)
