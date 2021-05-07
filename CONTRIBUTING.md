# Contributions

## If you are interested in contributing, here are some ground rules:
* Talk to us before doing the work -- we love contributions, but we might already be working on the same thing, or we might have different opinions on how it should be implemented.
* Everything must have test coverage. PRs with insufficient test coverage will be rejected.
* A formatting job will run automatically on your PR, even while in draft. This gives you a chance to fix the formatting before marking your PR as "Ready for Review".

## All contributions are subject to the Unity Contribution Agreement

By making a pull request, you are confirming agreement to the terms and conditions of the [Unity Contribution Agreement](https://unity3d.com/legal/licenses/Unity_Contribution_Agreement), including that your Contributions are your original creation and that you have complete right and authority to make your Contributions.

## Rules for PRs

* If a PR shall not be merged (yet), the author should create a draft PR instead. As soon as the PR is considered ready to land, it should be turned into a usual PR by the author.
* Things to check while your PR is in draft:
    * Is your PR pointing at the correct branch?
    * Have you missed anything? Consider getting a review while the PR is still in draft.
    * Do you need to add any extra tests?

* Yamato pipelines will start once your PR is marked as "Ready For Review". The pipelines that start depend on what changed in your PR.
    * These jobs will start again on each push. Please keep your PR in draft if you have additional pushes to make to avoid starting too many jobs.
    * Please add `[cancel old ci]` to your commit message if you want the previous jobs to stop (ie if your new change overrides them)

* Please try to keep your branch as close to master as possible, and merge in master before running tests.

* If there were significant changes to the code after a reviewer approved, a re-review must be requested. For smaller changes (like fixing typos) this is not required.

* The PR author is expected to merge the PR. If the author wants the last approving reviewer to merge it instead, this should be explicitly communicated. There is no guideline on how that communication should happen (on GitHub, Slack or even verbally - whatever makes the most sense in the given situation and scope) but it must always be clear to all reviewers if they are expected to merge.

* A reviewer should not feel nitpicky about requesting (small) changes in a PR, this guideline explicitly encourages this -- also this kind of feedback adds to the quality of our product and it should not be taken personal. Here is a suggestion for a "fast path" though: the reviewer should suggest a solution (like a different phrasing, name or hinting a typo). If the author resolves those exactly as the reviewer suggested, he or she can assume the approval of the reviewer without awaiting another explicit re-review/approval.


### Adding Reviewers

* Each request needs **at least 1 reviewer**. Each reviewer needs to approve the PR before it is merged. People from the team can add themselves if they would also like to review the changes. Reviewers can be removed if they have not yet started a review, but do not remove people who have added themselves.

* For each reviewer added, **reach out to that person on Slack** so that they are made aware of the request.

* As a Reviewer, please **comment on or approve a PR in a timely manner**. If you’re unable to do so, reach out to the author and let them know.

* As the PR author you are responsible for landing your PR swiftly. Reach out to devs who do not review in a timely manner on slack and remind them.

## Stale PRs

PRs without activity for several days should be **updated with comments or closed**. If the PR is waiting on something (e.g. another merge), close the PR and reopen it later. If the PR has yet to be reviewed by the reviewer(s), double check that they are the best person to review this PR.

## Guidelines

* Aim for not more than 200 to 400 lines of changes in a PR. Beyond that, reviewers often stop trying to fully understand the code.
* Try to separate large automated-refactoring resulting in massive code changes from actual changes in separate PRs.
* Release Notes must be updated when behaviour is changed.

### Resolving comments guidelines
* Comments that were simply fixed exactly as the reviewer requested can be resolved by both author and reviewer
* Comments that the author has either disagreed with or solved in an unexpected way, should be resolved by the reviewer only (not the the author)

## Merging PRs

It’s preferred to **squash commits and merge** when completing a PR. To do so, click the dropdown arrow next to the merge button and select "Squash and merge". This will allow you to preview the new commit message and edit it.

Select another merge option if it’s required to preserve valuable information. For more information about the different merge options, see [GitHub’s documentation](https://help.github.com/articles/about-merge-methods-on-github/).

**Replace the merge commit message** with something meaning instead of using the default “merge pull request #123”.

Merged branches will automatically be deleted through GitHub (you can restore them if required).
