// WebGLBuild — the build entry point used by .github/workflows/deploy-webgl.yml.
//
// WHY IT EXISTS
//   GitHub Pages cannot send the Content-Encoding headers Unity's compressed WebGL
//   files need. Instead of changing ProjectSettings by hand, this script sets the
//   WebGL options for the build only:
//     * compression format  = Gzip
//     * decompression fallback = ON  (the loader decompresses in JavaScript, so no
//                                      server headers are needed)
//   It then builds every scene that is enabled in File > Build Profiles / Build Settings.
//
// HOW TO USE
//   CI:     game-ci calls  WebGLBuild.BuildFromCommandLine  (buildMethod in the workflow).
//   Editor: MIHLab > Build WebGL (GitHub Pages)  builds to  build/WebGL/WebGL  locally.
//
// The PlayerSettings changes are made in memory for the build and restored afterwards,
// so ProjectSettings/ProjectSettings.asset is left as it was.
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

    [MenuItem("MIHLab/Build WebGL (GitHub Pages)")]
    public static void BuildFromMenu()
    {
        bool ok = Build(DefaultOutput);
        EditorUtility.DisplayDialog("WebGL build",
            ok ? "Build succeeded:\n" + DefaultOutput : "Build failed — see the Console.", "OK");
    }

    // Called by game-ci. game-ci passes -customBuildPath <path>; fall back to the default.
    public static void BuildFromCommandLine()
    {
        string output = GetArg("-customBuildPath") ?? DefaultOutput;
        bool ok = Build(output);
        EditorApplication.Exit(ok ? 0 : 1);
    }

    static bool Build(string outputPath)
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

        try
        {
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            Debug.Log($"[WebGLBuild] {report.summary.result}: {report.summary.totalSize / (1024 * 1024)} MB, " +
                      $"{report.summary.totalErrors} errors, output {outputPath}");
            return report.summary.result == BuildResult.Succeeded;
        }
        finally
        {
            PlayerSettings.WebGL.compressionFormat = oldCompression;
            PlayerSettings.WebGL.decompressionFallback = oldFallback;
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
