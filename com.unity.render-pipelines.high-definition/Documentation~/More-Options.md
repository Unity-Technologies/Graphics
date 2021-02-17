# Additional Properties

The High Definition Render Pipeline (HDRP) components expose standard properties by default that are suitable for most use-cases. However, some HDRP components and [Volume Overrides](Volume-Components.md) include **additional properties** which you can use to fine-tune the behavior of the component.

## Exposing additional properties

Not every component or Volume Override includes additional properties. If one does, it has a contextual menu to the right of each property section header that includes additional properties. To expose additional properties for that section, open the contextual menu and click **Show Additional Properties**. For example, the [Light component’s](Light-Component.md) **General** section includes additional properties:

![](Images/MoreOptions1.png)

When you select **Show Additional Properties**, Unity exposes additional properties for the **General** section. In this example,  the **Light Layer** property appears:

![](Images/MoreOptions2.png)

For Volume Overrides, the already existing contextual menu has a **Show Additional Properties** toggle as well.

Note that you can also open the contextual menu by right-clicking on the property section header.

## Exposing all additional properties

If you want to toggle additional properties for all components and Volume Overrides, you can do so through the **Preference** window under **Core Render Pipeline**. To do this:

1. Open the **Core Render Pipeline** tab in the **Preferences** window (menu: **Edit > Preferences > Core Render Pipeline**).
2. Set **Additional Properties** to **All Visible**.

![](Images/MoreOptions3.png)

When toggling additional properties through this menu, the state of all components changes once. After that, you can still choose to show or hide additional properties for each component individually.
A shortcut to this preference menu is also available from the component and Volume Override's contextual menu with **Show All Additional Properties...**.
