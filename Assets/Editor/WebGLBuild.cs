// WebGLBuild — the build entry point used by .github/workflows/deploy-webgl.yml.
//
// WHY IT EXISTS
//   GitHub Pages cannot send the Content-Encoding headers Unity's compressed WebGL
//   files need. Instead of changing ProjectSettings by hand, this script sets the
//   WebGL options for the build only:
//     * compression format  = Gzip
//     * decompression fallback = ON  (the loader decompresses in JavaScript, so no
//                                      server headers are needed)
//     * maximum memory size    = 2048 MB (Unity 6's default; the project's 512 MB
//                                      cap crashed the forest scene with OOM once the
//                                      terrain data was in the build)
//     * texture format         = DXT (desktop) or ASTC (phones), see below
//   It then builds every scene that is enabled in File > Build Profiles / Build Settings.
//
// TWO TEXTURE FORMATS (desktop + phone)
//   Desktop GPUs read DXT textures; phone GPUs read ASTC. A browser that can't read a
//   build's format unpacks every texture to full size in memory, which is what kept
//   the game from loading on phones. So CI builds the game twice, same quality:
//     -textureSubtarget DXT   -> the normal build, served to desktops
//     -textureSubtarget ASTC  -> only its .data file is kept, served to phones
//   The code (.wasm/.framework.js) is the same in both; only the .data file differs
//   (Unity's documented dual-build pattern: Manual > WebGL texture compression).
//   The workflow adds the ASTC data file next to the desktop one and patches index.html
//   to pick it when the browser supports WEBGL_compressed_texture_astc.
//
// HOW TO USE
//   CI:     game-ci calls  WebGLBuild.BuildFromCommandLine  (buildMethod in the workflow),
//           with  -textureSubtarget DXT|ASTC  in customParameters.
//   Editor: MIHLab > Build WebGL (GitHub Pages)        builds DXT  to build/WebGL/WebGL
//           MIHLab > Build WebGL for Phones (ASTC)     builds ASTC to build/astc/WebGL/WebGL
//
// All settings changed here are restored after the build, so ProjectSettings and your
// Build Profile's texture setting are left as they were.
//
// Editor-only (lives in an Editor folder); never compiled into the game.

using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class WebGLBuild
{
    const string DefaultOutput = "build/WebGL/WebGL";
    const string DefaultAstcOutput = "build/astc/WebGL/WebGL";
    const int MaximumMemoryMB = 2048;

    [MenuItem("MIHLab/Build WebGL (GitHub Pages)")]
    public static void BuildFromMenu() => BuildFromMenu(WebGLTextureSubtarget.DXT, DefaultOutput);

    [MenuItem("MIHLab/Build WebGL for Phones (ASTC)")]
    public static void BuildAstcFromMenu() => BuildFromMenu(WebGLTextureSubtarget.ASTC, DefaultAstcOutput);

    static void BuildFromMenu(WebGLTextureSubtarget subtarget, string output)
    {
        bool ok = Build(output, subtarget);
        EditorUtility.DisplayDialog("WebGL build",
            ok ? $"{subtarget} build succeeded:\n{output}" : "Build failed — see the Console.", "OK");
    }

    // Called by game-ci. game-ci passes -customBuildPath <path>; the workflow passes
    // -textureSubtarget DXT|ASTC. Defaults: build/WebGL/WebGL, DXT.
    public static void BuildFromCommandLine()
    {
        string output = GetArg("-customBuildPath") ?? DefaultOutput;
        string subtargetArg = GetArg("-textureSubtarget") ?? "DXT";
        if (!Enum.TryParse(subtargetArg, true, out WebGLTextureSubtarget subtarget))
        {
            Debug.LogError($"[WebGLBuild] Unknown -textureSubtarget '{subtargetArg}'. Use DXT or ASTC.");
            EditorApplication.Exit(1);
            return;
        }
        bool ok = Build(output, subtarget);
        EditorApplication.Exit(ok ? 0 : 1);
    }

    static bool Build(string outputPath, WebGLTextureSubtarget subtarget)
    {
        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            Debug.LogError("[WebGLBuild] No scenes are enabled in Build Settings.");
            return false;
        }

        var oldCompression = PlayerSettings.WebGL.compressionFormat;
        var oldFallback = PlayerSettings.WebGL.decompressionFallback;
        var oldMaxMemory = PlayerSettings.WebGL.maximumMemorySize;
        var oldSubtarget = EditorUserBuildSettings.webGLBuildSubtarget;

        try
        {
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            if (PlayerSettings.WebGL.maximumMemorySize < MaximumMemoryMB)
                PlayerSettings.WebGL.maximumMemorySize = MaximumMemoryMB;
            EditorUserBuildSettings.webGLBuildSubtarget = subtarget;
            Debug.Log($"[WebGLBuild] Textures {subtarget}, maximum memory {PlayerSettings.WebGL.maximumMemorySize} MB");

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.WebGL,
                // Must be set here too: with target set and subtarget left at 0, Unity
                // builds the platform's default subtarget and ignores webGLBuildSubtarget
                // (the first dual build shipped DXT textures in both data files).
                subtarget = (int)subtarget,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            Debug.Log($"[WebGLBuild] {subtarget} {report.summary.result}: {report.summary.totalSize / (1024 * 1024)} MB, " +
                      $"{report.summary.totalErrors} errors, output {outputPath}");
            return report.summary.result == BuildResult.Succeeded;
        }
        finally
        {
            PlayerSettings.WebGL.compressionFormat = oldCompression;
            PlayerSettings.WebGL.decompressionFallback = oldFallback;
            PlayerSettings.WebGL.maximumMemorySize = oldMaxMemory;
            EditorUserBuildSettings.webGLBuildSubtarget = oldSubtarget;
        }
    }

    static string GetArg(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == name) return args[i + 1];
        return null;
    }
}
