# Calculate Mass from Volume

Menu Path : **Attribute > Derived > Calculate Mass from Volume**

The **Calculate Mass from Volume** Block sets a particle’s **Mass** attribute based on its volume, derived from the **Scale** attribute and the Block’s **Density** property. This Block is useful for calculating the mass of particles with different scales so they behave believably during physics simulations.

## Block compatibility

This Block is compatible with the following Contexts:

- [Initialize](Context-Initialize.md)
- [Update](Context-Update.md)

##  Block properties

| **Input**   | **Type** | **Description**                                              |
| ----------- | -------- | ------------------------------------------------------------ |
| **Density** | Float    | The mass attribute of a particle based on its volume. The unit for this property is kg/dm<sup>3</sup>. |
