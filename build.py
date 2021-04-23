#!/usr/bin/python -B
import os
import logging

def packages_list():
    return [
        #ORDER IS IMPORTANT
        ("com.unity.shadergraph", os.path.join("com.unity.shadergraph")),
        ("com.unity.visualeffectgraph", os.path.join("com.unity.visualeffectgraph")),
        ("com.unity.render-pipelines.core", os.path.join("com.unity.render-pipelines.core")),
        ("com.unity.render-pipelines.universal", os.path.join("com.unity.render-pipelines.universal")),
        ("com.unity.render-pipelines.lightweight", os.path.join("com.unity.render-pipelines.lightweight")),
        ("com.unity.render-pipelines.high-definition", os.path.join("com.unity.render-pipelines.high-definition")),
        ("com.unity.render-pipelines.high-definition-config", os.path.join("com.unity.render-pipelines.high-definition-config")),
        ("com.unity.testing.visualeffectgraph", os.path.join("com.unity.testing.visualeffectgraph")),
        ("com.unity.testing.hdrp", os.path.join("com.unity.testing.hdrp")),
        ("com.unity.testframework.graphics", os.path.join("com.unity.testframework.graphics"))
    ]

def test_packages_list():
    return [
    ]

if __name__ == "__main__":
    import sys
    sys.path.insert(0, os.path.abspath(os.path.join("..", "automation-tools")))

    try:
        import unity_package_build
        build_log = unity_package_build.setup()
    except ImportError:
        print "No Automation Tools found."
