# Please read
## PR workflow guidelines
- Consider making use of **draft PRs** if you are not 100% sure that your PR is ready for review
- Adding [skip ci] (case insensitive) to the title of PRs will stop any jobs being triggered automatically - you will need to open Yamato and find your branch to run ABV
- You can also add [skip ci] to commit messages to prevent CI from running on that push
- Add [cancel old ci] to your commit message if you've made changes you want to test and no longer need the previous jobs

## Reminders
- Have you added a backport label (if needed)?
  
  > *For example, the `need-backport-*` label. After you backport the PR, the label changes to `backported-*`.*
- Have you updated the changelog?
  
  > *Each package has a `CHANGELOG.md` file.*
- Have you updated or added the documentation for your PR?
  
  > *When you add a new feature, change a property name, or change the behavior of a feature, it's best practice to include related documentation changes in the same PR.*
- Have you added a graphic test for your PR (if needed)?
  
  > *When you add a new feature, or discover a bug that tests don't cover, please add a graphic test.*
