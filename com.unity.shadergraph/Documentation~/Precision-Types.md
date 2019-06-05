# Precision Types

## Description

There are currently two **Precision Types** in [Shader Graph](Shader-Graph.md). Use the options listed in [Precision Modes](Precision-Modes.md) to define a **Precision Type** for each node.

## Precision Types

| Name        | Description     |
|:------------|:----------------|
| Half | Medium precision floating point value; generally 16 bits (range of –60000 to +60000, with about 3 decimal digits of precision).<br>`Half` precision is useful for short vectors, directions, object space positions, and high dynamic range colors. |
| Float | Highest precision floating point value; generally 32 bits (identical to `float` in regular programming languages).<br>Full `float` precision is useful for world space positions, texture coordinates, and scalar computations that involve complex functions such as trigonometry, power, and exponentiation. |
