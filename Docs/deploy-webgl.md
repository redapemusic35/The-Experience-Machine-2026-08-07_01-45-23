# Deploying the WebGL build to mihlab.org/play

Files involved:

- `.github/workflows/deploy-webgl.yml` — builds with GameCI and publishes to GitHub Pages.
- `Assets/Editor/WebGLBuild.cs` — the build entry point. Sets **Gzip + Decompression Fallback** for the build only, then restores your Player Settings. Also adds **MIHLab → Build WebGL (GitHub Pages)** for a local test build.

## Why the compression fallback

GitHub Pages serves files as-is and can't add the `Content-Encoding: gzip` header Unity's compressed builds expect, so a normal compressed build fails to load there. With Decompression Fallback on, Unity's loader unpacks the files in JavaScript instead. Load time is a little slower, but it works on any static host.

## One-time setup (Jim)

1. **Unity licence secrets.** Repo → Settings → Secrets and variables → Actions → add:
   - `UNITY_EMAIL` and `UNITY_PASSWORD` (your Unity ID), and
   - `UNITY_LICENSE` — the contents of your `.ulf` file (Personal licence). GameCI's "Activation" guide explains how to get it: <https://game.ci/docs/github/activation>.
2. **Turn on Pages.** Repo → Settings → Pages → *Source: GitHub Actions*.
3. **Custom domain.** In the same Pages screen, set `mihlab.org` and add the DNS records GitHub lists. Also commit a file named `CNAME` (containing `mihlab.org`) to the repo root; the workflow copies it into the site.
4. **Build scenes.** Make sure the forest scene is ticked in File → Build Profiles (the script builds every enabled scene, and stops if none are).

## Running it

Actions tab → **Deploy WebGL** → **Run workflow**. It only runs when started by hand. The first run takes the longest (no `Library` cache yet), so expect 30–90 minutes. When it finishes, the game is at `https://mihlab.org/play/`.

## Things to check before the first run

- **Is mihlab.org already a site?** `deploy-pages` replaces this repo's *whole* Pages site. If mihlab.org is served from a different repo (or another host), don't use the Pages steps as they are. Instead, copy the `build/WebGL/WebGL` folder into that site's `play/` folder. Ask a run to adapt the workflow once you know which repo hosts the site.
- **Unity 6000.5 image.** GameCI needs a Docker image for your exact editor version (`6000.5.7f1`). If the build step says the image doesn't exist, set `unityVersion:` in the workflow to the closest available 6000.5 version listed at <https://game.ci/docs/docker/versions>. Or upgrade the project to that version.
- **Git LFS quota.** The repo has about 340 LFS files. Every CI checkout downloads them, and GitHub's free LFS bandwidth is limited, so run the workflow only when you mean to publish.
- **Actions minutes.** Public repos build for free. Private repos use the monthly minutes allowance, and a Unity build uses a lot of them.
- **Pages size limit.** A Pages site should stay under 1 GB. The workflow prints the build size in the "Assemble site" step.

## Local test (optional)

Run MIHLab → Build WebGL (GitHub Pages), then serve the folder: `python -m http.server` inside `build/WebGL/WebGL`. Open `http://localhost:8000` in a browser. If it loads there, it will load on Pages.
