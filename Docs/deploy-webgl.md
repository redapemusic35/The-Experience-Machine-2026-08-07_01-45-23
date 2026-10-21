# Deploying the WebGL build to mihlab.org/play

mihlab.org is served from **MIHLab-Org/mihlab-org.github.io**. The workflow builds the game here, copies the build into that repo's `play/` folder, and pushes. Nothing else on the site is touched.

Files involved:

- `.github/workflows/deploy-webgl.yml` — builds with GameCI and pushes the result to the site repo.
- `Assets/Editor/WebGLBuild.cs` — the build entry point. Sets **Gzip + Decompression Fallback** for the build only, then restores your Player Settings. Also adds **MIHLab → Build WebGL (GitHub Pages)** for a local test build.

## Why the compression fallback

GitHub Pages serves files as they are and can't add the `Content-Encoding: gzip` header that Unity's compressed builds expect, so a normal compressed build won't load there. With Decompression Fallback on, Unity's loader unpacks the files in JavaScript instead. Load time is a little slower, but it works on any static host.

## One-time setup (Jim)

All of these go in **this** repo (The-Experience-Machine) → Settings → Secrets and variables → Actions:

1. **Unity licence:** `UNITY_EMAIL`, `UNITY_PASSWORD` (your Unity ID), and `UNITY_LICENSE` (the contents of your `.ulf` file). GameCI's activation guide explains how to get the file: <https://game.ci/docs/github/activation>.
2. **Site push token:** `SITE_DEPLOY_TOKEN`. Create a *fine-grained* personal access token at GitHub → Settings → Developer settings:
   - Resource owner: **MIHLab-Org** (the org may need to allow fine-grained tokens first)
   - Repository access: only `mihlab-org.github.io`
   - Permissions: **Contents: Read and write**. Nothing else.
   - Pick an expiry date and note it, because the workflow stops working when the token expires.
3. **Build scenes:** make sure the forest scene is ticked in File → Build Profiles. The script builds every enabled scene and stops if none are.

You don't need to change any Pages settings, and this repo doesn't need Pages turned on.

## Running it

Actions tab → **Deploy WebGL** → **Run workflow**. Leave `site_branch` as `main` unless the site repo publishes from a different branch (check its Settings → Pages). The workflow only runs when started by hand. The first run takes the longest because there's no `Library` cache yet; expect 30–90 minutes. A few minutes after it finishes, the game is at `https://mihlab.org/play/`.

## Things to check before the first run

- **Existing `play/` folder.** If the site repo already has a `play/` folder, the workflow replaces its contents with the build.
- **100 MB file limit.** The build goes into the site repo as normal git files, and GitHub rejects any single file over 100 MB. The workflow checks for this and stops with a clear message. If it happens, cut texture sizes or the terrain resolution for WebGL.
- **Site repo growth.** Each publish adds a full build to the site repo's history, so publish when you mean to, not on every change.
- **Unity 6000.5 image.** GameCI needs a Docker image for your exact editor version (`6000.5.7f1`). If the build step says the image doesn't exist, set `unityVersion:` in the workflow to the closest 6000.5 version listed at <https://game.ci/docs/docker/versions>, or upgrade the project to that version.
- **Git LFS quota.** This repo has about 340 LFS files, and every CI checkout downloads them. GitHub's free LFS bandwidth is limited.
- **Actions minutes.** Public repos build for free. Private repos use the monthly minutes allowance, and a Unity build uses a lot of them.

## Local test (optional)

Run MIHLab → Build WebGL (GitHub Pages). Then, inside `build/WebGL/WebGL`, run `python -m http.server` and open `http://localhost:8000`. If it loads there, it will load on Pages.
