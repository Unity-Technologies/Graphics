# Create a Volume Profile

Refer to [Understand Volumes](understand-volumes.md) for more information about Volume Profiles.

## Create and customize a Volume Profile

There are multiple ways to create a Volume Profile.

Create a Scene Settings GameObject from the **GameObject**  >  **Volume** menu. You can select one of the following:

- [**Global Volume**](understand-volumes.md)
- [**Sky and Fog Global Volume**](visual-environment-volume-override-reference.md)
- **Box Volume**
- **Sphere Volume**
- **Convex Mesh Volume**
- [**Custom Pass Volume**](Custom-Pass-Creating.md)

Unity creates and links a Volume Profile automatically when you create one of these volumes.

You can also create a Volume Profile manually. To do this, navigate to **Assets** > **Create** > **Volume Profile**.

Open the Volume Profile in the Inspector to edit its properties.  To do this, you can either:

- Select the Volume Profile in the Assets folder.

![](Images/VolumeProfile1.png)

- Select a GameObject with a Volume component that has a Volume Profile set in its **Profile** field.

![](Images/VolumeProfile2.png)



When you view the Volume Profile in the Inspector, you can only see values from the Volume overrides that the Volume Profile includes; the Volume Profile hides all other values. You must add Volume override components in order to edit the default properties of the Volume Profile. Click on the **Add Override** button and select which Volume override you want to add to the Volume Profile. For example, click on the **Add Override** button and select the **Screen Space Reflection** Volume override. This exposes properties relating to the screen space reflection (SSR) effect in HDRP. Default values for SSR already exist and you can use SSR in your Unity Project with these default values, without adding the SSR Volume override to a Volume Profile.
