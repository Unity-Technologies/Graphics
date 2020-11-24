# Emission Node

The Emission Node allows you to apply emission in your Shader Graph.

## Render pipeline compatibility

| **Node** | **Universal Render Pipeline (URP)** | **High Definition Render Pipeline (HDRP)** |
| -------- | ----------------------------------- | ------------------------------------------ |
| Emission | No                                  | Yes                                        |

## Ports

| Name          | Direction | Type           | Description                                                  |
| :------------ | :-------- | :------------- | :----------------------------------------------------------- |
| **color**     | Input     | LDR Color(RGB) | Sets the low dynamic range (LDR) color of the emission.      |
| **intensity** | Input     | Float          | Sets the intensity of the emission color.                    |
| **output**    | Output    | HDR Color(RGB) | Outputs the high dynamic range (HDR) color that this Node produces. |

## Notes

### Emission Unit

You can use two [physical light units](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/Physical-Light-Units.html) to control the strength of the emission:

* [Nits](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/Physical-Light-Units.html%23Nits).
* [EV<sub>100</sub>](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/Physical-Light-Units.html%23EV).


### Exposure Weight
You can use Exposure Weight to determine how exposure affects emission. It is a value between **0** and **1** where. A value of **0** means that exposure does not effect this part of the emission. A value of **1** means that exposure fully affects this part of the emission.
