#Enable execution rights via "chmod +x PackMac.sh"
PKG_VERSION="$1"
function createPackage() {
	mv $1 package
	tar -czvf Tools/$1-"$PKG_VERSION".tgz package
	mv package $1
}
cd ..
createPackage com.unity.render-pipelines.core
createPackage com.unity.render-pipelines.high-definition
createPackage com.unity.render-pipelines.high-definition-config
createPackage com.unity.shadergraph
createPackage com.unity.visualeffectgraph
createPackage com.unity.render-pipelines.universal
