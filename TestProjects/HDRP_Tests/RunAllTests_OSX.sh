#!/bin/sh

pipelines=deferred
#pipelines="$pipelines deferred-depth-prepass"
#pipelines="$pipelines deferred-depth-prepass-alpha-only"
#pipelines="$pipelines forward"

#platforms=StandaloneOSX
platforms=playmode

unity=$1
project=$(cd "$(dirname "$0")"; pwd)

echo "Unity: $unity"
echo "Project: $project"
echo

for a in $platforms; do
	for b in $pipelines; do
		echo $unity -projectPath "$project" -forgetProjectPath -automated -testResults "$project/TestResults-$a-$b.xml" -logFile "$project/Log-$a-$b.log" -runTests -testPlatform $a
		$unity -projectPath "$project" -forgetProjectPath -automated -testResults "$project/TestResults-$a-$b.xml" -logFile "$project/Log-$a-$b.log" -runTests -testPlatform $a
    done
done
