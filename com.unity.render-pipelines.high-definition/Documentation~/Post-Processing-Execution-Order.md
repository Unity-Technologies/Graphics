# Post-processing effect execution order

The post-processing system in the High Definition Render Pipeline applies post-processing effects in a specific order. The system also combines some effects into the same Compute Shader to minimize the number of passes.

## Execution order and effect grouping

![](Images/Post-processingExecutionOrder1.png))
