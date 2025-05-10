using UnityEditor;
using UnityEngine;
using System.IO;

public class Builder
{
    [MenuItem("Build/Build Game")]
    public static void BuildGame()
    {
        // Get the path to build to
        string buildPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Build");
        
        // Create the build directory if it doesn't exist
        System.IO.Directory.CreateDirectory(buildPath);

        // Build the game
        BuildPipeline.BuildPlayer(
            EditorBuildSettings.scenes,
            System.IO.Path.Combine(buildPath, "Fight4Ever.app"),
            BuildTarget.StandaloneOSX,
            BuildOptions.None
        );
    }

    public static void BuildMac()
    {
        string buildPath = Directory.GetCurrentDirectory();
        BuildPipeline.BuildPlayer(
            EditorBuildSettings.scenes,
            Path.Combine(buildPath, "Fight4Ever.app"),
            BuildTarget.StandaloneOSX,
            BuildOptions.None
        );
    }

    public static void BuildLinux()
    {
        string buildPath = Directory.GetCurrentDirectory();
        BuildPipeline.BuildPlayer(
            EditorBuildSettings.scenes,
            Path.Combine(buildPath, "Fight4Ever.x86_64"),
            BuildTarget.StandaloneLinux64,
            BuildOptions.None
        );
    }
} 