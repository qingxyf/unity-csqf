using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class CiBuild
{
    private const string WebGlRoguelikeScene = "Assets/Scenes/forth.unity";

    public static void PerformBuild()
    {
        string outputPath = Environment.GetEnvironmentVariable("UNITY_BUILD_PATH");
        if (string.IsNullOrEmpty(outputPath))
            outputPath = Path.Combine("Builds", "CI", "qingfeng.exe");

        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        outputPath = Path.GetFullPath(Path.Combine(projectRoot, outputPath));
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled && !string.IsNullOrEmpty(scene.path))
            .Select(scene => scene.path)
            .ToArray();

        if (scenes.Length == 0)
            throw new BuildFailedException("No enabled scenes are configured in EditorBuildSettings.");

        BuildReport report = BuildPipeline.BuildPlayer(
            scenes,
            outputPath,
            BuildTarget.StandaloneWindows64,
            BuildOptions.StrictMode);

        if (report.summary.result != BuildResult.Succeeded)
            throw new BuildFailedException($"Unity build failed: {report.summary.result}");

        Debug.Log($"CI build succeeded: {outputPath} ({report.summary.totalSize} bytes)");
    }

    public static void PerformWebGlBuild()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string outputPath = Path.GetFullPath(Path.Combine(projectRoot, "Builds", "WebGL"));

        if (!File.Exists(Path.Combine(projectRoot, WebGlRoguelikeScene)))
            throw new BuildFailedException($"WebGL roguelike scene is missing: {WebGlRoguelikeScene}");

        Directory.CreateDirectory(outputPath);
        BuildReport report = BuildPipeline.BuildPlayer(
            new[] { WebGlRoguelikeScene },
            outputPath,
            BuildTarget.WebGL,
            BuildOptions.StrictMode);

        if (report.summary.result != BuildResult.Succeeded)
            throw new BuildFailedException($"WebGL build failed: {report.summary.result}");

        Debug.Log($"CI WebGL build succeeded: {outputPath} ({report.summary.totalSize} bytes)");
    }
}
