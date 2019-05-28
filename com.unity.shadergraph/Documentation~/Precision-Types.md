# Precision Types

## Description

There are currently three **Precision Types** in [Shader Graph](Shader-Graph.md). Each [Node](Node.md) can define a **Precision Type** using the [Precision Modes](Precision-Modes.md) options. 

## Precision Types

| Name        | Description     |
|:------------|:----------------|
| Half | Medium precision floating point value; generally 16 bits (range of –60000 to +60000, with about 3 decimal digits of precision).<br> `Half` precision is useful for short vectors, directions, object space positions, high dynamic range colors. |
| Float | Highest precision floating point value; generally 32 bits (just like `float` from regular programming languages).<br> Full `float` precision is generally used for world space positions, texture coordinates, or scalar computations involving complex functions such as trigonometry or power/exponentiation. |
