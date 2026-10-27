using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Editor-only inspection and explicit scene-view painting; never auto-saves.
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

    private int page;
    private BoxedOutLevelOrderEditor levelOrder;
    private CheckResult report;
    private Vector2 scroll;
    private bool painting;
    private bool erasing;
    private int selectedKind;
    private string brushMessage = "";

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
        SceneView.duringSceneGui += DrawBrush;
        Undo.undoRedoPerformed += RefreshReport;
        RefreshReport();
    }

    private void OnDisable()
    {
        EditorApplication.hierarchyChanged -= RefreshReport;
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        SceneView.duringSceneGui -= DrawBrush;
        Undo.undoRedoPerformed -= RefreshReport;
    }

    private void OnPlayModeChanged(PlayModeStateChange state) { painting = false; RefreshReport(); }

    private void DrawBrush(SceneView view)
    {
        if (!painting || EditorApplication.isPlayingOrWillChangePlaymode) return;
        var manager = BoxedOutLevelBrush.Current();
        if (manager == null || manager.InteractablesTmap == null) return;
        var e = Event.current;
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        { painting = false; e.Use(); Repaint(); return; }
        if (e.alt || Tools.current == Tool.View) return;
        var map = manager.InteractablesTmap;
        bool floorBrush = (BoxedOutLevelBrush.Kind)selectedKind == BoxedOutLevelBrush.Kind.Floor;
        var plane = new Plane(Vector3.up, map.transform.position + (floorBrush ? Vector3.down : Vector3.zero));
        var ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        float distance;
        if (!plane.Raycast(ray, out distance)) return;
        var cell = map.WorldToCell(ray.GetPoint(distance));
        cell.z = 0;
        var center = floorBrush ? BoxedOutLevelBrush.FloorPosition(manager, cell) : GridController.GridToWorldPos(map, cell);
        Handles.color = erasing ? Color.red : Color.green;
        Handles.DrawWireCube(center, new Vector3(map.cellSize.x, .06f, map.cellSize.y));
        Handles.Label(center, string.Format("{0} ({1}, {2})", erasing ? "擦除" : "放置", cell.x, cell.y));
        if (e.type == EventType.Layout) HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            brushMessage = BoxedOutLevelBrush.Apply(manager, cell, (BoxedOutLevelBrush.Kind)selectedKind, erasing);
            e.Use();
            RefreshReport();
        }
        if (e.type == EventType.MouseMove) view.Repaint();
    }

    private void RefreshReport()
    {
        report = Inspect(SceneManager.GetActiveScene());
        Repaint();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("BOXED-OUT · 关卡编辑器", EditorStyles.boldLabel);
        page = GUILayout.Toolbar(page, new[] { "关卡编辑", "关卡顺序" });
        if (page == 1)
        {
            painting = false;
            if (levelOrder == null) levelOrder = new BoxedOutLevelOrderEditor();
            scroll = EditorGUILayout.BeginScrollView(scroll);
            levelOrder.Draw();
            EditorGUILayout.EndScrollView();
            return;
        }
        EditorGUILayout.HelpBox("启用画笔后，在 Scene 视图单击网格编辑。Ctrl+Z 撤销，Esc 关闭画笔；不会自动保存。", MessageType.Info);
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
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if (GUILayout.Button("另存为新关卡…"))
                {
                    painting = false;
                    BoxedOutLevelCopies.SaveCurrentCopy();
                }
                if (GUILayout.Button("试玩当前关卡")) { painting = false; BoxedOutLevelPreview.Start(); }
                EditorGUILayout.HelpBox("试玩：R 重开；HOME/MENU 或通关后返回编辑器；也可按顶部 Play 停止。", MessageType.None);
                selectedKind = GUILayout.Toolbar(selectedKind, new[] { "墙", "箱子", "目标点", "玩家", "地板" });
                erasing = GUILayout.Toolbar(erasing ? 1 : 0, new[] { "放置", "擦除所选类型" }) == 1;
                painting = EditorGUILayout.ToggleLeft("启用 Scene 网格画笔", painting);
                EditorGUILayout.HelpBox("地板使用 Floor 预制体，高度比物件低 1 格（Y - 1）。擦除地板不会删除上层物件。玩家工具只移动已有出生点。", MessageType.None);
                if (!string.IsNullOrEmpty(brushMessage)) EditorGUILayout.HelpBox(brushMessage, MessageType.Info);
            }
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
        return result;
    }
}

