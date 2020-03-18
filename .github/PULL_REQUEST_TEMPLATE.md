**DONT FORGET TO ADD A CHANGELOG**

### Checklist for PR maker
- [ ] Have you added a backport label (if needed)? For example, the `need-backport-2019.3` label. After you backport the PR, the label changes to `backported-2019.3`.
- [ ] Have you updated the changelog? Each package has a `CHANGELOG.md` file.
- [ ] Have you updated or added the documentation for your PR? When you add a new feature, change a property name, or change the behavior of a feature, it's best practice to include related documentation changes in the same PR.
- [ ] Have you added a graphic test for your PR (if needed)? When you add a new feature, or discover a bug that tests don't cover, please add a graphic test.

---
### Purpose of this PR
Why is this PR needed, what hard problem is it solving/fixing?

---
### Testing status

**Manual Tests**: What did you do?
- [ ] Opened test project + Run graphic tests locally
- [ ] Built a player
- [ ] Checked new UI names with UX convention
- [ ] Tested UI multi-edition + Undo/Redo + Prefab overrides + Alignment in Preset
- [ ] C# and shader warnings (supress shader cache to see them)
- [ ] Checked new resources path for the reloader (in devloper mode, you have a button at end of resources that check the pathes)
- Other: 

**Automated Tests**: What did you setup? (Add a screenshot or the reference image of the test please)

**Yamato**: (Select your branch):
https://yamato.prd.cds.internal.unity3d.com/jobs/78-ScriptableRenderPipeline

Any test projects to go with this to help reviewers?

---
### Comments to reviewers
Notes for the reviewers you have assigned.
