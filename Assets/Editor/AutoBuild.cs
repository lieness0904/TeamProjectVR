using System.Collections;
using System.Collections.Generic;
using UnityEditor.Build.Reporting;
using UnityEditor;
using UnityEngine;

public class AutoBuild : MonoBehaviour
{
    [MenuItem("Build/Build AOS")]
    public static void MyBuild_AOS()
    {
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();

        buildPlayerOptions.scenes = new[] {
            "Assets/01_Scenes/Lobby.unity",
            "Assets/01_Scenes/MainGame.unity",
            "Assets/01_Scenes/TitleScene.unity"
            };

        buildPlayerOptions.locationPathName = $"Builds/AOS_{PlayerSettings.bundleVersion}.apk";
        buildPlayerOptions.target = BuildTarget.Android;
        buildPlayerOptions.options = BuildOptions.None;

        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log("Build succeeded: " + summary.totalSize + " bytes");
        }

        if (summary.result == BuildResult.Failed)
        {
            Debug.Log("Build failed");
        }
    }
}
