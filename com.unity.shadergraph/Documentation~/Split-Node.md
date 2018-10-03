## Description

Splits the input vector **In** into four **Vector 1** outputs **R**, **G**, **B** and **A**. These output vectors are defined by the individual channels of the input **In**; red, green, blue and alpha respectively. If the input vector **In**'s dimension is less than 4 (**Vector 4**) the output values not present in the input will use default values. These default values are (0, 0, 0, 1).

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| In      | Input | Dynamic Vector | None | Input value |
| R | Output      |    Vector 1 | None | Red channel from input |
| G | Output      |    Vector 1 | None | Green channel from input |
| B | Output      |    Vector 1 | None | Blue channel from input |
| A | Output      |    Vector 1 | None | Alpha channel from input |