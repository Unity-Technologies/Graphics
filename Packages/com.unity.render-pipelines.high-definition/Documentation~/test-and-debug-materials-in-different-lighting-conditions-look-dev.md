# Compare materials in different lighting conditions with Look Dev

Use the [Look Dev](look-dev-reference.md) viewer to compare assets under different lighting conditions in high dynamic range images (HDRI). You can use the viewer to quickly manipulate and change between HDRIs to simulate different environments for the asset you're working on.

**Note**: Look Dev is only available in Edit mode. The Look Dev window closes when you enter Play mode.

To compare assets using Look Dev, do the following:

1. If it's the first time you're using Look Dev, [create an Environment Library](#create-environment-library).
2. [Open your Environment Library in Look Dev](#open-environment-library).
3. [Add and edit environments](#add-environment).
4. [Load assets into Look Dev](#load-assets).
5. [Compare different conditions for the same assets](#compare-assets).

<a name="create-environment-library"></a>
## Create an Environment Library

The first time you use Look Dev, you must create a new Environment Library or load an existing one. An Environment Library is an asset that contains a list of environments that you can use in Look Dev to simulate different lighting conditions. 

To create an Environment Library, do one of the following:

- Select **Assets** > **Create** > **Rendering Environment Library (Look Dev)**.  
- Open the Look Dev window and select **New Library**.

<a name="open-environment-library"></a>
## Open your Environment Library in Look Dev

Before you can create and edit environments, open your Environment Library in Look Dev. To do this, do one of the following:

- Go to the Look Dev window (**Window > Rendering > Look Dev**) and drag your Environment Library from your Project window into the sidebar.  
- In your Project window, select your Environment Library asset. Then, in the Inspector window, select **Open in LookDev window**.

<a name="add-environment"></a>
## Add and edit environments

To view different lighting conditions in Look Dev, add environments to the Environment Library. Each environment uses an HDRI texture for its skybox and includes properties that you can use to fine-tune the environment.

To add, remove, or duplicate environments, use the toolbar at the bottom of the Environment Library list. 

If you already have environments in the Environment Library, Unity displays a list of previews in the sidebar. When you select a preview for an environment in the sidebar, Unity displays Environment Settings so you can edit the [selected environment's properties](look-dev-reference#environment-settings).

To import an HDRI texture and add it to an environment, do the following:

1. Load an .hdr or .exr file into your project.
2. In the Texture Importer Inspector window, set **Texture Type** to **Default**, **Texture Shape** to **Cube**, and **Convolution Type** to **None**.
3. In the Look Dev window, select an environment.
4. Set **Sky with Sun** to your chosen HDRI texture using the picker.

<a name="load-assets"></a>
## Load assets into Look Dev

To load assets into Look Dev, do one of the following:

- Drag a prefab from the Project window into the Look Dev viewport.
- Drag a GameObject from the Hierarchy into the Look Dev viewport.

<a name="compare-assets"></a>
## Compare different lighting conditions for the same assets

To compare different lighting conditions for the same assets, navigate with the Look Dev camera and adjust your viewports.

### Navigate with the Look Dev Camera

Navigate around the environment with the Look Dev camera by doing the following:

- **Rotate around pivot**: Left-click and drag.  
- **Pan camera**: Middle-click and drag.  
- **Zoom**: Alt + right-click and drag.  
- **Forward/backward**: Mouse wheel.  
- **First Person mode**: Right-click + W, A, S, and D.

By default, Look Dev synchronizes the camera movement for both views. To decouple the cameras from one another and manipulate them independently, select the synchronized cameras button between the two numbered camera buttons.

### Adjust your viewports

Use multiple viewports to compare two different environments and settings for the same asset. By default, Look Dev displays a single viewport which contains the prefab or GameObject you're working with.

Choose one of the following viewports:

- Vertically side-by-side.   
- Horizontally side-by-side.   
- Split-screen. If you use this option, refer to [Use the manipulation gizmo](#manipulation-gizmo).

<a name="manipulation-gizmo"></a>
#### Use the manipulation gizmo

The manipulation gizmo represents the separation plane between the two viewports.

| **Action** | **Control** | **Description** |
| :---- | :---- | :---- |
| Move the separator | Left-click and drag the straight line of the gizmo to the desired location. | View different parts of the environment. |
| Change the orientation and length of the manipulator gizmo | Left-click and drag the circle at either end of the manipulator. | Set the orientation and blending values more precisely. |
| Change the split view in increments of 22.5° | Left-click and hold the circle on the end of the manipulation gizmo, then hold Shift as you move the mouse.  | View an exact horizontal, vertical or diagonal angle. |
| Blend between the two views | Left-click on the central white circle. Drag along the red line to blend the left-hand view with the right-hand view. Drag along the blue line to blend the right-hand view with the left-hand view.  |  Adjust the relative contribution of each viewport to the final image.<br/><br/>**Note**: The white circle automatically snaps back into the center when you drag it back. This helps you get back to the default blending value quickly. |