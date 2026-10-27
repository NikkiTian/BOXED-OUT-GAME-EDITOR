using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
[InitializeOnLoad]
public static class BoxedOutLevelPreview
{
    const string Pending = "BoxedOut.Preview.Pending", Previous = "BoxedOut.Preview.PreviousStart", Waiting = "BoxedOut.Preview.Waiting";
    static double deadline;
    static BoxedOutLevelPreview() { EditorApplication.playModeStateChanged += OnState; EditorApplication.update += Tick; }
    public static void Start()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (Prepare()) EditorApplication.isPlaying = true;
    }
    static bool Prepare()
    {
        var scene = SceneManager.GetActiveScene(); var check = BoxedOutLevelEditorWindow.Inspect(scene);
        if (!check.IsLevel || check.Issues.Count > 0)
        { EditorUtility.DisplayDialog("无法试玩", "请先修复关卡基础检查中的错误。", "知道了"); return false; }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() || scene.isDirty || string.IsNullOrEmpty(scene.path))
        { EditorUtility.DisplayDialog("未开始试玩", "请先保存关卡，试玩读取已保存内容。", "知道了"); return false; }
        var menu = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Menu.unity");
        if (menu == null) { EditorUtility.DisplayDialog("无法试玩", "缺少 Menu 场景。", "知道了"); return false; }
        SessionState.SetString(Previous, AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
        SessionState.SetString(Pending, scene.path); EditorSceneManager.playModeStartScene = menu;
        return true;
    }
    static void OnState(PlayModeStateChange state)
    {
        // The toolbar Play button must bootstrap the same services as preview.
        if (state == PlayModeStateChange.ExitingEditMode &&
            string.IsNullOrEmpty(SessionState.GetString(Pending, "")) &&
            BoxedOutLevelEditorWindow.Inspect(SceneManager.GetActiveScene()).IsLevel)
        {
            if (!Prepare()) EditorApplication.isPlaying = false;
        }
        if (state == PlayModeStateChange.EnteredPlayMode && !string.IsNullOrEmpty(SessionState.GetString(Pending, "")))
        { SessionState.SetBool(Waiting, true); deadline = EditorApplication.timeSinceStartup + 30; }
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            GameManager.EditorPreviewPath = null;
            if (string.IsNullOrEmpty(SessionState.GetString(Pending, ""))) return;
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(SessionState.GetString(Previous, ""));
            SessionState.EraseString(Pending); SessionState.EraseString(Previous); SessionState.SetBool(Waiting, false);
        }
    }
    static void FocusGame()
    {
        if (!EditorApplication.isPlaying) return;
        var gameView = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        if (gameView != null) EditorWindow.GetWindow(gameView).Focus();
    }
    static void Tick()
    {
        if (!EditorApplication.isPlaying || !SessionState.GetBool(Waiting, false)) return;
        if (GameManager.Instance != null)
        { SessionState.SetBool(Waiting, false); GameManager.Instance.BeginEditorPreview(SessionState.GetString(Pending, "")); EditorApplication.delayCall += FocusGame; }
        else if (EditorApplication.timeSinceStartup > deadline)
        { Debug.LogError("试玩启动失败：Menu 没有初始化 GameManager。"); SessionState.SetBool(Waiting, false); EditorApplication.isPlaying = false; }
    }
}

