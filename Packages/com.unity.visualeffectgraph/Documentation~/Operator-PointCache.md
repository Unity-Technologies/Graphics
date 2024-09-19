# Point Cache

> [!IMPORTANT]
> This feature is experimental. To use this feature, open the **Preferences** window, go to the **Visual Effects** tab, and enable **Experimental Operators/Blocks**.

Menu Path : **Operator > Utility > Point Cache**

The **Point Cache** Operator exposes the attribute maps and the point count stored into a [Point Cache asset](point-cache-asset.md).

## Operator settings

| **Setting** | **Type**          | **Description**                                 |
| ----------- | ----------------- | ----------------------------------------------- |
| **Asset**   | Point Cache asset | The point cache asset this Operator references. |

## Operator properties

Based on the **Asset**, the number of AttributeMap outputs changes to match the number of attributes stored inside the Point Cache asset

| **Output**                                 | **Type**  | **Description**                                          |
| ------------------------------------------ | --------- | -------------------------------------------------------- |
| **Point Count**                            | uint      | The number of values stored inside the Point ache asset. |
| **AttributeMap : \<attribute> (multiple)** | Texture2D | The Attribute Map (Texture2D) containing values.         |

## Remarks

If the attribute this Operator is trying to read from has not been written to, it returns the default standard value for its type.

You can use the [Point Cache Bake Tool](point-cache-bake-tool.md) provided by VFX Graph to generate point cache from meshes or textures.
