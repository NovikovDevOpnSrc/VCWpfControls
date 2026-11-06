---
name: vcwpf-release
description: >
  Cut and publish a VCWpfControls release (nuget.org via Trusted Publishing). Use when the
  user says "релиз", "release", "опубликуй", "делай версию", "катай/режь релиз", "cut a
  release", or when asking what is pending for the next version. Encodes the full runbook:
  version bump, changelog, tag, push, verification — and the failure modes that already
  happened once in this project.
---

# VCWpfControls release runbook

Everything is automated by `.github/workflows/release.yml` (tag push → build/test →
version guard → GitHub Release → nuget.org Trusted Publishing). Your job is to feed it a
correct tag and then VERIFY. Work in `Z:\Work\VibeCoding\VCWpfControls`.

## Steps

1. **Preconditions**: `gh run list --limit 1` shows the last `ci` run on main `success`;
   working tree has no unexpected changes (`git status`); no user commits left unpushed.

2. **Bump the version** in `Directory.Build.props`:
   `<VersionPrefix>X.Y.Z</VersionPrefix>`.
   This step is THE critical one: in v0.2.5 the bump was forgotten, the workflow built the
   old version, nuget.org answered Conflict, and `--skip-duplicate` made the step green —
   the release silently published NOTHING.

3. **CHANGELOG**: replace `## [Unreleased]` block structure: add
   `## [X.Y.Z] — <date>` heading right below a fresh empty `## [Unreleased]`, with a
   one-line summary of the release contents.

4. **Commit and tag** (never touch `.github/workflows` in this commit):

   ```bash
   git add Directory.Build.props CHANGELOG.md
   git commit -m "chore(release): vX.Y.Z - <summary>"
   git tag vX.Y.Z
   git -c credential.helper= -c "credential.helper=!gh auth git-credential" push origin main vX.Y.Z
   ```

5. **Watch**: `gh run watch <id> --exit-status` (or `gh run list --workflow=release.yml`).
   The run must pass the **"Verify package version matches tag"** guard (added after the
   v0.2.5 incident) and then push `VCWpfControls.X.Y.Z.nupkg` + `.snupkg`.

6. **Verify** (trust but check):
   - `gh release view vX.Y.Z --json assets -q '.assets[].name'` — assets carry the NEW version;
   - run log contains `Pushing VCWpfControls.X.Y.Z.nupkg`;
   - `https://api.nuget.org/v3-flatcontainer/vcwpfcontrols/index.json` lists the version —
     indexing takes ~10 minutes; 404/absence right after the push is normal, don't panic.

## Failure modes seen in this project

- **Conflict / assets named with the OLD version** → the bump was missed. Fix:
  `gh release delete vX.Y.Z --yes --cleanup-tag`, bump, re-commit, re-tag, push.
- **`refusing to allow an OAuth App to create or update workflow ... without workflow scope`**
  → the push carries a commit touching `.github/workflows`. The gh credential helper can't
  push those (the user's interactive git can — their account has the workflow scope).
  Split the workflow change out of the release commit.
- **`gh run watch` printed CI FAILED but the run later shows success** → the WATCHER hit
  its own timeout. Always re-check with `gh run view <id>` before believing it.

After publishing: update `ROADMAP.md` if items shipped, refresh project memory, and
rebuild the Release demo (`dotnet build src/VCWpfControls.Demo -c Release`) so the user
tests the shipped bits.
