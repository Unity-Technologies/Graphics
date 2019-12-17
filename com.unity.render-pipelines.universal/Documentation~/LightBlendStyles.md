# Light Blend Styles

![](images\image_38.png)

__Blend Styles__ determine the way a particular Light interacts with Sprites in the Scene. All Lights in the Scene must pick from one of the available Blend Styles. The Universal Render Pipeline (URP) 2D Asset can currently contain a total of four different Light Blend Styles, starting with the 'Default' Blend Style being available.

| **Property**             | **Function**                                                 |
| ------------------------ | ------------------------------------------------------------ |
| __Name__                 | The name that appears when choosing a Blend Style for a Light2D. |
| __Mask Texture Channel__ | Which mask channel to use when applying this Blend Style to a Sprite. |
| __Render Texture Scale__ | Scale for the internal render texture created for this Blend Style. |
| __Blend Mode__           | What blending mode the Light2D uses when this Blend Style is selected. |



## Blend Mode

![](images\image_39.png)

Blend Modes controls the way a Sprite is lit by light. The following examples show the four predefined Blend Modes:

| ![Original reference](images\image_40.png) | ![Multiply](images\image_41.png)    |
| ------------------------------------------ | ----------------------------------- |
| Original Sprite                            | Multiply                            |
| ![Additive](images\image_42.png)           | ![Subtractive](images\image_43.png) |
| Additive                                   | Subtractive                         |



## Custom Blend Modes

![](images\image_44.png)

All of the Blend Modes available can be expressed with Custom Blend Factors. The factors for the existing Blend Modes are as follows:

- Multiplicative

- - Modulate = 1
  - Additive = 0

- Additive

- - Modulate = 0
  - Additive = 1

- Subtractive

- - Modulate = 0
  - Additive = -1



## Mask Texture Channel

Masks control where Lights can affect a Sprite. There are 4 channels to select from as the mask channel - Red, Blue, Green, and Alpha. In a mask max value means full light, min value means no light. 

| ![Original rock color](images\image_45.png)     | ![Rock Mask](images\image_46.png)                      |
| ----------------------------------------------- | ------------------------------------------------------ |
| Original Rock Color                             | Rock with a mask                                       |
| ![Additive Light Blending](images\image_47.png) | ![Masked Additive Light Blending](images\image_48.png) |
| Additive Light Blending                         | Additive Light Blending with a mask                    |



## Render Texture Scale

Render Texture Scale adjusts the size of the internal render Texture used for a given Blend Mode. Lowering the Render Texture Scale can increase performance and decrease memory usage for Scenes that contain large Lights. Lowering the Texture Scale to too low a value may cause visual artefacts or a shimmering effect when there is motion in the Scene.