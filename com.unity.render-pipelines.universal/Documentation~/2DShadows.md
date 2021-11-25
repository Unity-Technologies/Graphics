## Shadow Caster 2D

The __Shadow Caster 2D__ component defines the shape and properties that a Light uses to determine its cast shadows. Add the __Shadow Caster 2D__ component to a GameObject by going to menu: __Component > Rendering > 2D > Shadow Caster 2D__.



| __Property__                | __Function__                                                 |
| --------------------------- | ------------------------------------------------------------ |
| __Use Renderer Silhouette__ | Enable this and __Self Shadows__ to include the GameObject Renderer's silhouette as part of the shadow. Enable this and disable Self Shadows to exclude the Renderer's silhouette from the shadow. This option is only available when a valid Renderer is present. |
| __Casts Shadows__           | Enable this to have the Renderer cast shadows.               |
| __Self Shadows__            | Enable this to have the Renderer cast shadows on itself.     |

| ![](Images/2D/RendSilhou_disabled_SS_false.png)                 | ![](Images/2D/RendSilhou_enabled_SS_false.png)                  |
| ------------------------------------------------------------ | ------------------------------------------------------------ |
| __Use Renderer Silhouette__ disabled, __Self Shadow__ disabled | __Use Renderer Silhouette__ enabled, __Self Shadow__ disabled |
| ![](Images/2D/RendSilhou_disabled_SS_true_.png)                 | ![](Images/2D/RendSilhou_enabled_SS_true.png)                   |
| __Use Renderer Silhouette__ disabled, __Self Shadows__ enabled | __Use Renderer Silhouette__ enabled, __Self Shadows__ enabled |



## Composite Shadow Caster 2D

The __Composite Shadow Caster 2D__ merges the shape of multiple __Shadow Caster 2Ds__ together as a single __Shadow Caster 2D__. Add the __Composite Shadow Caster 2D__ component to a GameObject by going to menu: __Component > Rendering > 2D > Composite Shadow Caster 2D__, then parent GameObjects with the __Shadow Caster 2D__ component to it. The Composite component merges all Shadow Caster 2Ds within this hierarchy, including any Shadow Caster 2Ds on the parent as well.

| ![](Images/2D/wo_composite_shadow.png)    | ![](Images/2D/w_composite_shadow.png)  |
| -------------------------------------- | ----------------------------------- |
| Without __Composite Shadow Caster 2D__ | With __Composite Shadow Caster 2D__ |
