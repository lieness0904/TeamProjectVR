using UnityEditor;
using UnityEngine;
using System;
using UnityEditor.Build.Reporting;
using System.Linq;

public class BuildScript
{
    static string[] GetEnabledScenes()
    {
        return Array.FindAll(EditorBuildSettings.scenes, scene => scene.enabled).Select(s => s.path).ToArray();
    }

    [MenuItem("Build/Build Windows")]
    public static void BuildWindows()
    {
        string buildPath = "Builds/WindowsBuild/TeamProject.exe";
        string[] scenes = GetEnabledScenes();
        
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = buildPath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };
        
        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        
        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log("Build succeeded.");
        }
        else
        {
            Debug.LogError("Build failed.");
            throw new Exception("Build failed.");
        }
    }
}
