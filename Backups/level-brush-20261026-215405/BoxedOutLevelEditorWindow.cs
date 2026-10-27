using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Editor-only, read-only first stage. Never saves or changes scene objects.
public class BoxedOutLevelEditorWindow : EditorWindow
{
    public class CheckResult
    {
        public string SceneName;
        public bool IsLevel;
        public int Players, Boxes, Walls, Goals;
        public readonly List<string> Issues = new List<string>();
        public readonly List<string> Notes = new List<string>();
    }

    private CheckResult report;
    private Vector2 scroll;

    [MenuItem("Tools/BOXED-OUT/Level Editor")]
    public static void Open()
    {
        var window = GetWindow<BoxedOutLevelEditorWindow>("关卡编辑器");
        window.minSize = new Vector2(420, 380);
        window.RefreshReport();
        window.Show();
    }

    private void OnEnable()
    {
        EditorApplication.hierarchyChanged += RefreshReport;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        RefreshReport();
    }

    private void OnDisable()
    {
        EditorApplication.hierarchyChanged -= RefreshReport;
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
    }

    private void OnPlayModeChanged(PlayModeStateChange state) { RefreshReport(); }

    private void RefreshReport()
    {
        report = Inspect(SceneManager.GetActiveScene());
        Repaint();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("BOXED-OUT · 关卡编辑器", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("第一步：读取与基础检查。此窗口不会修改、保存或删除关卡。", MessageType.Info);
        if (EditorApplication.isPlaying)
            EditorGUILayout.HelpBox("正在显示运行中的状态；请退出 Play 后检查初始关卡。", MessageType.Warning);
        if (GUILayout.Button("读取当前场景 / 重新检查")) RefreshReport();
        if (report == null) return;
        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.LabelField("当前场景", report.SceneName);
        if (!report.IsLevel)
        {
            EditorGUILayout.HelpBox("当前场景没有 LevelManager，不是可检查的玩法关卡。请在 Project 中打开 Lvl1～Lvl4，再点击重新检查。", MessageType.Info);
        }
        else
        {
            EditorGUILayout.LabelField("玩家", report.Players.ToString());
            EditorGUILayout.LabelField("箱子", report.Boxes.ToString());
            EditorGUILayout.LabelField("墙", report.Walls.ToString());
            EditorGUILayout.LabelField("目标点", report.Goals.ToString());
            foreach (var issue in report.Issues) EditorGUILayout.HelpBox(issue, MessageType.Error);
            foreach (var note in report.Notes) EditorGUILayout.HelpBox(note, MessageType.Warning);
            if (report.Issues.Count == 0)
                EditorGUILayout.HelpBox("已通过本阶段的基础结构检查。", MessageType.Info);
        }
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("本阶段未检查：地图边界、格子重叠、关卡可解性。", EditorStyles.wordWrappedLabel);
        EditorGUILayout.LabelField("后续步骤：放置与擦除 → 另存关卡 → 试玩。", EditorStyles.wordWrappedLabel);
        EditorGUILayout.EndScrollView();
    }

    public static CheckResult Inspect(Scene scene)
    {
        var result = new CheckResult { SceneName = scene.IsValid() ? scene.name : "无场景" };
        if (!scene.IsValid() || !scene.isLoaded) return result;
        var roots = scene.GetRootGameObjects();
        var managers = roots.SelectMany(r => r.GetComponentsInChildren<LevelManager>(true))
            .Where(m => m.isActiveAndEnabled).ToArray();
        result.IsLevel = managers.Length > 0;
        if (!result.IsLevel) return result;
        if (managers.Length != 1) result.Issues.Add("必须且只能有一个启用的 LevelManager。");
        var manager = managers[0];
        result.Players = roots.SelectMany(r => r.GetComponentsInChildren<PlayerController>(true))
            .Count(p => p.isActiveAndEnabled);
        if (result.Players != 1) result.Issues.Add("必须且只能有一个启用的玩家。");
        var goals = roots.SelectMany(r => r.GetComponentsInChildren<Goal>(true))
            .Where(g => g.isActiveAndEnabled).ToArray();
        result.Goals = goals.Length;
        if (result.Goals == 0) result.Issues.Add("关卡没有启用的目标点。");
        if (manager.Moveable == null) result.Issues.Add("LevelManager 缺少箱子参考对象。");
        if (manager.Wall == null) result.Issues.Add("LevelManager 缺少墙参考对象。");
        if (manager.InteractablesTmap == null || manager.InteractablesTmap.gameObject.scene != scene)
            result.Issues.Add("交互 Tilemap 未设置，或不属于当前关卡。");
        else
        {
            foreach (Transform child in manager.InteractablesTmap.transform)
            {
                if (!child.gameObject.activeInHierarchy) continue;
                bool box = manager.Moveable != null && child.CompareTag(manager.Moveable.tag);
                bool wall = manager.Wall != null && child.CompareTag(manager.Wall.tag);
                if (box) result.Boxes++;
                if (wall) result.Walls++;
                if ((box || wall) && !child.GetComponents<Collider>().Any(c => c.enabled))
                    result.Issues.Add(child.name + " 缺少启用的 Collider，移动逻辑可能无法识别它。");
            }
            if (result.Boxes == 0) result.Issues.Add("交互 Tilemap 下没有启用的箱子。");
            if (result.Boxes != result.Goals) result.Notes.Add("箱子与目标点数量不同，请确认这是有意的关卡设计。");
        }
        var references = manager.Goals ?? new GameObject[0];
        if (references.Length == 0 || references.Any(g => g == null))
            result.Issues.Add("LevelManager 的 Goals 列表为空或包含丢失的引用。");
        var validReferences = references.Where(g => g != null).ToArray();
        if (validReferences.Distinct().Count() != validReferences.Length)
            result.Issues.Add("Goals 列表包含重复引用。");
        if (goals.Any(g => !validReferences.Contains(g.gameObject)) ||
            validReferences.Any(g => g.scene != scene || !goals.Any(goal => goal.gameObject == g)))
            result.Issues.Add("Goals 列表与场景中启用的目标点不一致，可能影响通关判定。");
        result.Notes.Add("直接试玩需要 GameManager；本阶段尚未加入独立试玩入口。");
        return result;
    }
}
