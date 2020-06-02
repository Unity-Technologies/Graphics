# Color Luma

Menu Path : **Operator > Color > Color Luma**

This **Color Luma** Operator outputs the luma (perceived brightness) of the input color.

 ![](Images/Operator-ColorHSVLuma.png)

The luma value of a color is useful in many scenarios, for example:

- You can use it to recolor particles and still maintain their original brightness.
- You can utilize it in the simulation itself and make the brightest particles move faster.

Note: Color Operators work on a per-particle level. To recolor the particle's texture on a per-pixel level, use **Color Mapping** in the system's output Context or create your own shader via [Shader Graph](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest/index.html).

## Operator properties

| **Inputs** | **Type** | **Description**                      |
| ---------- | -------- | ------------------------------------ |
| **Color**  | Color    | The Color to calculate the luma for. |

| **Output** | **Type** | **Description**              |
| ---------- | -------- | ---------------------------- |
| **luma**   | Color    | The luma value of **Color**. |