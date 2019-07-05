set version_preview=6.9.0-preview
set version=6.9.0
call CreateOneLocalPackage.bat com.unity.render-pipelines.core %version%
call CreateOneLocalPackage.bat com.unity.render-pipelines.high-definition %version_preview%
call CreateOneLocalPackage.bat com.unity.render-pipelines.lightweight %version%
call CreateOneLocalPackage.bat com.unity.shadergraph %version%
call CreateOneLocalPackage.bat com.unity.visualeffectgraph %version_preview%
pause