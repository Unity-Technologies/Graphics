# Contributions

## If you are interested in contributing, here are some ground rules:
* If you have a bug you want to fix or a feature you want to add, use the channel [#gfx-test-framework](https://unity.slack.com/archives/CHSTN3FFX) to coordinate (you need to ensure that noone else is working on the same thing, and that it won't interfere with anyone's work).
* If you are adding a new kind of test, make sure you add a test for that test

## All contributions are subject to the [Unity Contribution Agreement(UCA)](https://unity3d.com/legal/licenses/Unity_Contribution_Agreement)
By making a pull request, you are confirming agreement to the terms and conditions of the UCA, including that your Contributions are your original creation and that you have complete right and authority to make your Contributions.

## Rules for PRs
* If a PR shall not be merged (yet), the author should create a draft PR instead. As soon as the PR is considered ready to land, it should be turned into a usual PR by the author.
* Do not run a promote job from your branch, all promotions happen on master

## Testing your PR
* Small changes can be tested locally in a graphics test project - state the steps you've taken in the PR description
    * More info on how ot test a package locally: https://docs.unity3d.com/Manual/upm-ui-local.html
    * If you created a new project for testing, make sure to add the graphics test framework package to testables in the project manifest: https://docs.unity3d.com/Manual/cus-tests.html
* Bigger changes should be tested against multiple branches/platforms:
    * Make a branch in the [Graphics](https://github.com/Unity-Technologies/Graphics) repo
    * Find the manifest.json of the projects you want to test on
    * Replace the graphics test framework version with an ssh git url pointing to your graphics test framework branch, eg
        * `"com.unity.testframework.graphics": "ssh://git@github.cds.internal.unity3d.com/unity/com.unity.testframework.graphics.git#your-branch-here"`
    * Run the jobs in the Graphics yamato project

## Publishing and promoting
The package is now on "Lifecycle V2" of Package Manager, more info here https://docs.unity3d.com/Packages/com.unity.package-validation-suite@0.23/manual/lifecycle_validation_error.html#lifecycle-v2-tagging-rules

* The package is in preview/experimental with no plans to leave. This is because the package needs to be publicly accessible so that it can be used outside of the VPN, but there is no way to have a non-experimental package that can circumvent Release Management, package standards, etc, even though it would only ever be used internally.
* If you need to publish a test version on your branch, add something like `-exp.99` to the end of the version, so that we don't accidentally use up a version number that we will need later
    * You can add `[Unreleased]` to your changelog instead of a version number while you are testing and iterating on your branch (otherwise you will fail package validation, or end up with a messy changelog)
* Publishing and promoting happens from the master branch, but you can run a "dry-run" job on your branch before merging.

## Package validation
Please see the Package Validation Suite docs for more information, including how to fix errors you might see https://docs.unity3d.com/Packages/com.unity.package-validation-suite@0.23/manual/index.html

## Once you have a change ready following these ground rules. Simply make a pull request in Github
