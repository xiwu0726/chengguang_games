#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 《时光之痕》构建入口。
///
/// 工程是「零场景资源」设计：运行时由 StoryBootstrap 的
/// [RuntimeInitializeOnLoadMethod] 自动拉起全部 UI，所以这里只需要
/// 在构建时现场生成一个空场景当载体，不去手写 .unity / .prefab。
///
/// 批处理用法（CI / 命令行）：
///   Unity.exe -batchmode -nographics -quit -projectPath &lt;repo&gt; \
///     -executeMethod BuildScript.BuildWindowsBatch -logFile -
/// </summary>
public static class BuildScript
{
    const string SceneDir = "Assets/Scenes";
    const string ScenePath = SceneDir + "/MainScene.unity";
    const string OutputDir = "Build/Windows";
    const string ExeName = "时光之痕.exe";

    [MenuItem("时光之痕/构建 Windows 可执行文件", false, 10)]
    public static void BuildWindows()
    {
        if (BuildWindowsCore(true)) Debug.Log("[BuildScript] 构建成功 -> " + Path.GetFullPath(OutputDir));
    }

    [MenuItem("时光之痕/构建并运行", false, 11)]
    public static void BuildAndRun()
    {
        if (BuildWindowsCore(true))
        {
            string exe = Path.Combine(Path.GetFullPath(OutputDir), ExeName);
            Debug.Log("[BuildScript] 启动 " + exe);
            System.Diagnostics.Process.Start(exe);
        }
    }

    [MenuItem("时光之痕/在编辑器里试玩（Play）", false, 20)]
    public static void PlayInEditor()
    {
        EditorApplication.isPlaying = true;
    }

    /// <summary>批处理入口：成功退出码 0，失败退出码 1。</summary>
    public static void BuildWindowsBatch()
    {
        bool ok = BuildWindowsCore(false);
        EditorApplication.Exit(ok ? 0 : 1);
    }

    static bool BuildWindowsCore(bool verbose)
    {
        try
        {
            EnsureCarrierScene();

            PlayerSettings.productName = "时光之痕";
            PlayerSettings.companyName = "chengguang_games";
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;

            Directory.CreateDirectory(OutputDir);
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = Path.Combine(OutputDir, ExeName),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            if (verbose)
            {
                Debug.Log(string.Format(
                    "[BuildScript] 结果={0} 大小={1:F1}MB 耗时={2:F1}s 警告={3} 错误={4}",
                    summary.result, summary.totalSize / 1048576f,
                    summary.totalTime.TotalSeconds, summary.totalWarnings, summary.totalErrors));
            }

            if (summary.result != BuildResult.Succeeded)
            {
                Debug.LogError("[BuildScript] 构建失败: " + summary.result);
                return false;
            }
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("[BuildScript] 构建异常: " + e);
            return false;
        }
    }

    /// <summary>没有场景资源时现场生成一个空场景，让构建有入口。</summary>
    static void EnsureCarrierScene()
    {
        if (File.Exists(ScenePath)) return;

        Directory.CreateDirectory(SceneDir);
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        bool saved = EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();
        Debug.Log("[BuildScript] 生成载体场景 " + ScenePath + " saved=" + saved);
    }
}
#endif