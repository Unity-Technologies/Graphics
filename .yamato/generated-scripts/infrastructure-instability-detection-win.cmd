@echo on
rem This is an auto-generated script. Do not edit manually!

curl -fs "https://artifactory-slo.bf.unity3d.com/artifactory/automation-and-tooling/infrastructure-instability-detection/standalone/1.0.0/windows.zip" --output "infrastructure_instability_detection_standalone.zip" --retry 5
IF EXIST "infrastructure_instability_detection" rmdir /s /q infrastructure_instability_detection
powershell.exe -nologo -noprofile -command "& { Add-Type -A 'System.IO.Compression.FileSystem'; [IO.Compression.ZipFile]::ExtractToDirectory('infrastructure_instability_detection_standalone.zip', '.'); }" && DEL "infrastructure_instability_detection_standalone.zip"
curl -fs "https://artifactory-slo.bf.unity3d.com/artifactory/automation-and-tooling/infrastructure-instability-detection/patterns.zip" --output patterns.zip --retry 5
IF EXIST "patterns" rmdir /s /q patterns
powershell.exe -nologo -noprofile -command "& { Add-Type -A 'System.IO.Compression.FileSystem'; [IO.Compression.ZipFile]::ExtractToDirectory('patterns.zip', '.'); }" && DEL "patterns.zip"
infrastructure_instability_detection
exit /b 0
