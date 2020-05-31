# Graph Target

A Target determines the end point compatibility of the generated shader from a Shader Graph. Targets are selected for each Shader Graph asset. Targets can be changed via the Graph Settings Menu. 

The Target holds information regarding the required generation format and variables for compatibility with different render pipelines or integration features like Visual Effects Graph. You can select as many or as few active targets as desired for each Shader Graph asset. Some targets may not be compatible with other selected targets, in which case an error with an explanation will display. 

Target Settings are specified by the selected target. Universal Target Settings and High Definition Target Settings may change. 

Typically, each target selected will generate a valid subshader from the graph. For example, a Shader Graph with both Universal and High Definition render pipelines selected will generate two subshaders. 
