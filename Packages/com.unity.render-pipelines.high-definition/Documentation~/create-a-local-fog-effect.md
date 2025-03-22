# Create a local fog effect

To create fog effects that [global fog](create-a-global-fog-effect.md) can't produce, use a Local Volumetric Fog component, which is a fog volume represented as an oriented bounding box.

## Limitations

HDRP voxelizes Local Volumetric Fog at 64 or 128 slices along the camera's focal axis for better performance. This introduces the following limitations:

- No volumetric shadowing: Local Volumetric Fog does not reduce light intensity between a Light and a surface.
- Aliasing artifacts: Noticeable aliasing can appear at the fog volume boundary. Use Local Volumetric Fog with [global fog](create-a-global-fog-effect.md) and a Blend Distance above 0 to reduce edge hardness.

## Create a Local Volumetric Fog component

Create a **Local Volumetric Fog** component in one of the following ways:

- From the main menu, select **GameObject** > **Rendering** > **Local Volumetric Fog**.

- Right-click in the **Hierarchy** window and select **Volume** > **Local Volumetric Fog**.

<a name="volumetric-fog-set-up"></a>

## Set up volumetric fog

To use volumetric fog, enable it in the following locations:

- [Project Settings](#enable-fog-project-settings)
- [High Definition Render Pipeline (HDRP) Asset](#enable-fog-hdrp-asset)
- [Global volume](#enable-fog-global-volume)

<a name="enable-fog-project-settings"></a>

### Enable volumetric fog in Project Settings

To enable **Volumetric Fog** in the **Project Settings** window:

1. Open the **Project Settings** window (menu: **Edit** > **Project Settings**).
2. Go to **Quality** > **HDRP** > **Lighting** > **Volumetrics** and enable **Volumetric Fog**.
3. Go to **Graphics** > **Pipeline Specific Settings** > **HDRP**.
4. Under **Frame Settings (Default Values)** in the **Camera** section:
   - Select **Lighting**.
   - Enable **Fog**.
   - Enable **Volumetrics**.

<a name="enable-fog-hdrp-asset"></a>

### Enable volumetric fog in the HDRP asset

To enable **Volumetric Fog** in the [HDRP Asset](HDRP-Asset.md):

1. In the **Project** window, select the **HDRenderPipelineAsset** to open it in the **Inspector** window.
2. Go to **Lighting** > **Volumetrics**.
3. Enable **Volumetric Fog**.

<a name="enable-fog-global-volume"></a>

### Enable Volumetric Fog in a global volume

To use **Volumetric Fog** in your scene, create a [Global volume](understand-volumes.md) with a [Fog Volume Override](fog-volume-override-reference.md):

1. Create a new **GameObject** (menu: **GameObject** > **Create Empty**).
2. In the **Inspector** window, select **Add Component**.
3. Enter “volume” in the search box.
4. Select the **Volume** component.
5. In **Profile**, select **New** to create a volume profile.
6. Select **Add Override**.
7. In the search box, select **Fog**.

To enable **Volumetric Fog** in the Fog Volume Override:

1. Select the **Fog** dropdown.
2. Enable **State** and **Volumetric Fog**.

<a name="apply-fog-volume"></a>

## Configure the fog with a mask

To configure fog color and density with a mask, use one of the following methods:

- Apply a 3D mask texture.
- Apply a Fog Volume shader graph.

### Apply a 3D mask texture

A mask texture is a 3D texture that defines the shape and appearance of the local volumetric fog. Unity does not limit the size of this texture, and its size does not affect memory usage.

#### Create a mask texture

To create a 3D texture for a local volumetric fog component:

1. Create an RGBA flipbook texture in image-editing software. 

    For example, a texture size of 1024 x 32 represents a 3D texture size of 32 x 32 x 32, with 32 slices laid out sequentially.

2. [Import the texture as a 3D texture](https://docs.unity3d.com/Manual/class-Texture3D.html).

3. Open the **Local Volumetric Fog** component and assign the 3D texture to the **Texture** field in the **Mask Texture** section.

**Note**: The **Scale** transform does not affect the fog size. To change the size, modify the **Size** value in the Local Volumetric Fog component.

#### Use a built-in mask texture

HDRP includes built-in 3D mask textures that set the density of a fog volume to different shapes and noise patterns. To use these textures:

1. Open the **Package Manager** window (menu: **Window** > **Package Management** > **Package Manager**).
2. Find the **High Definition RP** package.
3. Open the **Samples** dropdown.
4. Find **Volumetric Samples** and select **Import**.

### Apply a Fog Volume shader graph

A Fog Volume shader graph is a custom shader graph designed to render volumetric fog effects in a 3D scene.

To apply a [Fog Volume shader graph](fog-volume-master-stack-reference.md) in a scene:

1. [Create and set up a Local Volumetric Fog component.](#volumetric-fog-set-up)
2. In the **Project** window, right-click the Fog Volume shader graph asset and select **Create** > **Material**.
3. In the **Hierarchy** window, select the **Local Volumetric Fog** GameObject.
4. In the **Inspector**, set the **Mask Mode** to **Material**.
5. In the **Mask Material** section, select the **Material** picker (⊙) and choose the Fog Volume material.


