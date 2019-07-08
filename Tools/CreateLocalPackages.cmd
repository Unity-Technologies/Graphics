set version_preview=6.8.1-preview
set version=6.8.1
call CreateOneLocalPackage.bat com.unity.render-pipelines.core %version%
call CreateOneLocalPackage.bat com.unity.render-pipelines.high-definition %version_preview%
call CreateOneLocalPackage.bat com.unity.render-pipelines.lightweight %version%
call CreateOneLocalPackage.bat com.unity.shadergraph %version%
call CreateOneLocalPackage.bat com.unity.visualeffectgraph %version_preview%
pause