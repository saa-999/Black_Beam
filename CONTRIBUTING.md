# Contributing to BlackBeam

First off, thank you for considering contributing to BlackBeam. It's people like you that make BlackBeam such a great tool.

## Where do I go from here?

If you've noticed a bug or have a feature request, make sure to check our [issue tracker](../../issues) to see if someone else in the community has already created a ticket. If not, go ahead and [make one](../../issues/new).

## Fork & create a branch

If this is something you think you can fix, then fork BlackBeam and create a branch with a descriptive name.

## Get the test suite running

Make sure your environment is set up according to the instructions in the `README.md`. Ensure that your code passes any existing tests and compiles cleanly.

## Implement your fix or feature

At this point, you're ready to make your changes. Feel free to ask for help; everyone is a beginner at first.

## Code Conventions

*   Follow standard C# and .NET coding conventions.
*   Keep your commit messages descriptive and clear.
*   Update any relevant documentation (like `.env.example` or API docs) if you change configuration or endpoints.

## Make a Pull Request

At this point, you should switch back to your master branch and make sure it's up to date with BlackBeam's master branch:

```bash
git remote add upstream git@github.com:YOUR_ORGANIZATION/BlackBeam.git
git checkout master
git pull upstream master
```

Then update your feature branch from your local copy of master, and push it!

```bash
git checkout 325-add-new-feature
git rebase master
git push --set-upstream origin 325-add-new-feature
```

Finally, go to GitHub and make a Pull Request.

## Keeping your Pull Request updated

If a maintainer asks you to "rebase" your PR, they're saying that a lot of code has changed, and that you need to update your branch so it's easier to merge.
