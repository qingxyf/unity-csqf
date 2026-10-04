using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
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
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.decompressionFallback = false;

        BuildReport report = BuildPipeline.BuildPlayer(
            new[] { WebGlRoguelikeScene },
            outputPath,
            BuildTarget.WebGL,
            BuildOptions.StrictMode);

        if (report.summary.result != BuildResult.Succeeded)
            throw new BuildFailedException($"WebGL build failed: {report.summary.result}");

        ValidateUncompressedWebGlResources(Path.Combine(outputPath, "Build"));

        Debug.Log($"CI WebGL build succeeded: {outputPath} ({report.summary.totalSize} bytes)");
    }

    /// <summary>
    /// Builds an independent, playable visual acceptance fixture. The source
    /// scene and normal build scene list are never edited or replaced.
    /// </summary>
    public static void PerformRiceKeeperVisualWebGlBuild()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string outputPath = Path.GetFullPath(Path.Combine(projectRoot, "Builds", "RiceKeeperPreview"));
        if (!File.Exists(Path.Combine(projectRoot, WebGlRoguelikeScene)))
            throw new BuildFailedException($"Visual preview source scene is missing: {WebGlRoguelikeScene}");

        string fixturePath = AssetDatabase.GenerateUniqueAssetPath("Assets/Scenes/RiceKeeperVisualPreview.unity");
        try
        {
            var scene = EditorSceneManager.OpenScene(WebGlRoguelikeScene, OpenSceneMode.Single);
            new GameObject("RiceKeeper visual acceptance").AddComponent<RiceKeeperVisualPreview>();
            if (!EditorSceneManager.SaveScene(scene, fixturePath, true))
                throw new BuildFailedException("Could not save the separate visual preview scene.");

            Directory.CreateDirectory(outputPath);
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false;
            BuildReport report = BuildPipeline.BuildPlayer(new[] { fixturePath }, outputPath,
                BuildTarget.WebGL, BuildOptions.StrictMode | BuildOptions.Development);
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException($"RiceKeeper visual WebGL build failed: {report.summary.result}");
            ValidateUncompressedWebGlResources(Path.Combine(outputPath, "Build"));
            Debug.Log($"RiceKeeper visual WebGL build succeeded: {outputPath} ({report.summary.totalSize} bytes)");
        }
        finally
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AssetDatabase.DeleteAsset(fixturePath);
        }
    }

    private static void ValidateUncompressedWebGlResources(string resourceDirectory)
    {
        if (!Directory.Exists(resourceDirectory))
            throw new BuildFailedException($"WebGL build did not produce a Build resource directory: {resourceDirectory}");

        string[] resourceFiles = Directory.GetFiles(resourceDirectory, "*", SearchOption.AllDirectories);
        string[] expectedExtensions = { ".data", ".framework.js", ".wasm" };
        string[] compressedExtensions = { ".br", ".gz", ".unityweb" };

        foreach (string extension in expectedExtensions)
        {
            if (!resourceFiles.Any(path => path.EndsWith(extension, StringComparison.OrdinalIgnoreCase)))
                throw new BuildFailedException($"WebGL build is missing an uncompressed {extension} resource in {resourceDirectory}.");
        }

        string[] compressedResources = resourceFiles
            .Where(path => compressedExtensions.Any(extension => path.EndsWith(extension, StringComparison.OrdinalIgnoreCase)))
            .ToArray();
        if (compressedResources.Length > 0)
            throw new BuildFailedException($"WebGL build contains compressed resources: {string.Join(", ", compressedResources)}");
    }
}
