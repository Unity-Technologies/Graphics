# Create a local fog effect

You may want to have fog effects in your Scene that global fog can not produce by itself. In these cases you can use local fog. To add localized fog, use a Local Volumetric Fog component. Local Volumetric Fog is a volume of fog represented as an oriented bounding box. By default, fog is constant (homogeneous), but you can alter it by assigning a Density Mask 3D texture to the **Texture** field under the **Density Mask Texture** section.

## Create a Local Volumetric Fog component

You can create a Local Volumetric Fog component in one of the following ways:

1. In the menu bar, select **GameObject** > **Rendering** > **Local Volumetric Fog**.

2. Right-click in the Hierarchy window and select **Volume** > **Local Volumetric Fog**.

<a name="volumetric-fog-set-up"></a>

## Set up Volumetric fog
To use volumetric fog, enable it in the all of the following locations:
- [Project Settings](#enable-fog-project-settings)
- [HDRP Asset](#enable-fog-hdrp-asset)
- [Global Volume](#enable-fog-global-volume)

<a name="enable-fog-project-settings"></a>

### Enable Volumetric Fog in Project Settings

To enable Volumetric Fog in the Project Settings window, open the Project settings window (menu: **Edit > Project Settings)** and enable the following properties**:**

1. Go to **Quality** > **HDRP** > **Lighting** > **Volumetrics** and enable **Volumetric Fog**.
2. Go to **Graphics** > **Pipeline Specific Settings** > **HDRP**.
3. Under Frame Settings (Default Values), in the Camera section:
   - Select **Lighting**.
   - Enable **Fog**.
   - Enable **Volumetrics**.

<a name="enable-fog-hdrp-asset"></a>

### Enable Volumetric Fog in the HDRP Asset

To enable Volumetric Fog in the [HDRP Asset](HDRP-Asset.md):

1. In the **Project** window, select the **HDRenderPipelineAsset** to open it in the Inspector**.**
2. Go to **Lighting** > **Volumetrics**.
3. Enable **Volumetric Fog**.

<a name="enable-fog-global-volume"></a>

### Enable Volumetric Fog in a Global Volume

To use volumetric fog in your scene, create a [Global volume](understand-volumes.md) with a [**Fog** component](fog-volume-override-reference.md). To do this:

1. Create a new GameObject (menu: **GameObject** > **Create Empty**).
2. In the Inspector window, select **Add Component**.
3. In the search box that appears, enter “volume”.
4. Select the **Volume** Component.
5. Go to **Profile** and select **New** to create a volume profile.
6. Select **Add Override**.
7. In the search box that appears, select **Fog.**

To enable volumetric fog in the Fog component:

1. Select the **Fog** dropdown
2. Select the **Enable** toggle, and enable it.
3. Change the **State** setting to **Enabled**.
4. Select the **Volumetric Fog** toggle, and enable it.

To see more properties you can use to control Volumetric Fog, expose the hidden Fog settings:

1. In the Volume component, go to the **Fog** override and select the **More** (**⋮**) dropdown.
2. Select **Show All Additional Properties** to open the Preferences window.
3. In the Preferences window, under Additional Properties, open the **Visibility** dropdown and select **All Visible.**

Refer to the [Local Volumetric Fog Volume reference](local-volumetric-fog-volume-reference.md) for more information.

<a name="apply-fog-volume"></a>

## Apply a Fog Volume shader graph

To apply a [Fog Volume shader](fog-volume-master-stack-reference.md) in a scene, assign a Fog Volume material to a Local Volumetric Fog component:

1. [Create and set up a Local Volumetric Fog component.](#volumetric-fog-set-up).
2. In the Project window, right-click the Fog Volume shader graph asset and select **Create** > **Material.**
3. In the Hierarchy window, select the Local Volumetric Fog GameObject.
4. In the Local Volumetric Fog’s Inspector window, set the **Mask Mode** property to **Material**.
5. In the Mask Material section, select the material picker (circle).
6. Select the Fog Volume material.

If the Fog Volume doesn't appear in your scene, follow the [Local Volumetric Fog setup instructions](#volumetric-fog-set-up).

**Note**: The **Scale** transform doesn't affect the size of the fog in your scene. To do this, change the **Size** value in the Local Volumetric Fog component.

**Note**: If you use more than one Fog Volume shader in your scene, then the HDRP fog system applies lighting consistently across them all.

Refer to [Fog Volume shader](fog-volume-master-stack-reference.md) for more information.

### Mask textures

A mask texture is a 3D texture that controls the shape and appearance of the local volumetric fog in your scene. Unity does not limit the size of this 3D texture, and its size doesn't affect the amount of memory a local volumetric fog uses.

### Use a built-in Mask Texture

HDRP includes 3D Density Mask Textures with different noise values and shapes that you can use in your scene. To use these Textures, import them from the High Definition RP package samples:

1. Open the Package Manager window (menu: **Window** > **Package Manager**).
2. Find the **High Definition RP** package.
3. Expand the **Samples** drop-down.
4. Find the **Local Volumetric Fog 3D Texture Samples** and click on the **Import** button.

## Create a Density Mask Texture

 To create a 3D texture to apply to a local volumetric fog component:

1. In the image-editing software of your choice, create an RGBA flipbook texture and [import it as a 3D texture](https://docs.unity3d.com/2020.2/Documentation/Manual/class-Texture3D.html). For example, a texture of size 1024 x 32 describes a 3D Texture of size 32 x 32 x 32 with 32 slices laid out one after another.
2. Open a **Local Volumetric Fog** component and in its **Density Mask Texture** section assign the 3D Texture you imported to the **Texture** field .

## Limitations

HDRP voxelizes Local Volumetric Fog at 64 or 128 slices along the camera's focal axis to improve performance. This causes the following limitations:
- Local Volumetric Fog doesn't support volumetric shadowing. If you place Local Volumetric Fog between a Light and a surface, the Volume does not decrease the intensity of light that reaches the surface.
- Noticeable aliasing can appear at the boundary of the fog Volume. To hide aliasing artifacts, use Local Volumetric Fog with [global fog](create-a-global-fog-effect.md). You can also use a Density Mask and a Blend Distance value above 0 to decrease the hardness of the edge.
