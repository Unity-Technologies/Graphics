#!/bin/sh

unity=$1
project=$(cd "$(dirname "$0")"; pwd)

echo "Unity: $unity"
echo "Project: $project"
echo

echo $unity -batchMode -projectPath "$project" -forgetProjectPath -automated -testResults "$project/TestResults.xml" -logFile "$project/Log.log" -runTests -testPlatform playmode
$unity -batchMode -projectPath "$project" -forgetProjectPath -automated -testResults "$project/TestResults.xml" -logFile "$project/Log.log" -runTests -testPlatform playmode
