#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 一键搭建最小可运行的 StartMenu 场景（Camera + Canvas + EventSystem + StartMenuController），
/// 并把构建场景顺序设为 [StartMenu, MainScene]。之后在该场景内搭建具体 UI 即可。
/// </summary>
public static class StartMenuSceneSetup
{
    const string StartMenuPath = "Assets/Scenes/StartMenu.unity";
    const string MainScenePath = "Assets/Scenes/MainScene.unity";

    [MenuItem("Tools/创建 StartMenu 场景")]
    public static void CreateStartMenuScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 主摄像机
        GameObject camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camGo.tag = "MainCamera";
        camGo.transform.position = new Vector3(0, 0, -10);

        // Canvas
        GameObject canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // EventSystem
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        // 菜单主控
        new GameObject("StartMenuController", typeof(StartMenuController));

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, StartMenuPath);

        SetBuildScenes();

        Debug.Log($"[StartMenu] 场景已创建：{StartMenuPath}，并已设置构建场景顺序。请在该场景中搭建 UI。");
    }

    [MenuItem("Tools/设置构建场景顺序 (StartMenu, MainScene)")]
    public static void SetBuildScenes()
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(StartMenuPath, true),
            new EditorBuildSettingsScene(MainScenePath, true),
        };
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
#endif
