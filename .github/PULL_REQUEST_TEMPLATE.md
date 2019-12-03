**DONT FORGET TO ADD A CHANGELOG**

### Checklist for PR maker
- Have you added a Label? : HDRP, Universal, ShaderGraph etc...
- Have you added a label for backport (if needed)? : need-backport-2019.3  .  When the PR is backported the label will be change ton backported-2019.3
- Have you added a changelog? Each package have a changelog.
- Have you updated or added the documentation for you PR? When property name is changed, when a feature behavior is change, when adding a new features, think to update the documentation in the same PR.
- Have you added a graphic test for your PR (if needed)? When adding new feature or discovering a bug that isn't cover by a test, please add a graphic test

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
