# Additional Properties

The High Definition Render Pipeline (HDRP) components expose standard properties by default that are suitable for most use-cases. However, some HDRP components include **additional properties** which you can use to fine-tune the behavior of the component.

## Exposing additional properties

Not every component includes additional properties. If one does, it has a contextual menu to the right of each property section header that includes additional properties. Click this contextual menu and toggle "Show Additional Properties" to expose additional properties for that property section. For example, the [Light component’s](Light-Component.md) **General** section includes additional properties:

![](Images/MoreOptions1.png)

When you toggle "Show Additional Properties", Unity exposes additional properties for the **General** section. In this example,  the **Light Layer** property appears:

![](Images/MoreOptions2.png)

For Volume Components the already existing contextual menu will have a "Show Additional Properties" toggle as well.
Remember that this contextual menu is also available through right clicking on the header.

## Exposing all additional properties

If you want to toggle additional properties for all components, you can do so through the Preference window under **Core Render Pipeline**.

![](Images/MoreOptions3.png)

When toggling additional properties through this menu, the state of all components will be changed once. After that, you can still choose to show or hide additional properties for each component individually.
A shortcut to this preference menu is also available from the components contextual menu with "Show All Additional Properties...".
