using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class BoxedOutLevelOrderEditor
{
    const string ConfigPath = "Assets/Resources/BoxedOutLevelOrder.json";
    readonly List<SceneAsset> levels = new List<SceneAsset>();
    SceneAsset candidate;
    bool loaded, changed;
    string message;

    public void Draw()
    {
        if (!loaded) Reload();
        EditorGUILayout.HelpBox("正式流程：首页 → 列表中的关卡 → 结尾。新关卡做好后手动添加；单关试玩不受顺序影响。", MessageType.Info);
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
        {
            EditorGUILayout.LabelField("首页（固定）", "Menu");
            for (int i = 0; i < levels.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField((i + 1).ToString(), GUILayout.Width(24));
                EditorGUI.BeginChangeCheck();
                var scene = (SceneAsset)EditorGUILayout.ObjectField(levels[i], typeof(SceneAsset), false);
                if (EditorGUI.EndChangeCheck()) { levels[i] = scene; changed = true; }
                if (GUILayout.Button("打开", GUILayout.Width(42)) && scene != null)
                {
                    if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(scene));
                }
                using (new EditorGUI.DisabledScope(i == 0))
                    if (GUILayout.Button("↑", GUILayout.Width(25))) { Swap(i, i - 1); GUIUtility.ExitGUI(); }
                using (new EditorGUI.DisabledScope(i == levels.Count - 1))
                    if (GUILayout.Button("↓", GUILayout.Width(25))) { Swap(i, i + 1); GUIUtility.ExitGUI(); }
                if (GUILayout.Button("移除", GUILayout.Width(42)))
                { levels.RemoveAt(i); changed = true; GUIUtility.ExitGUI(); }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.LabelField("结尾（固定）", "Ending");
            EditorGUILayout.Space();
            candidate = (SceneAsset)EditorGUILayout.ObjectField("添加关卡", candidate, typeof(SceneAsset), false);
            using (new EditorGUI.DisabledScope(candidate == null))
                if (GUILayout.Button("加入列表"))
                {
                    var path = AssetDatabase.GetAssetPath(candidate);
                    if (path == BoxedOutLevelOrder.Menu || path == BoxedOutLevelOrder.Ending)
                        message = "首页和结尾已固定，无需添加。";
                    else if (levels.Contains(candidate)) message = "该关卡已经在列表中。";
                    else { levels.Add(candidate); candidate = null; changed = true; message = null; }
                }
            EditorGUILayout.HelpBox("移除只会移出游戏流程，不删除关卡文件。调整后点击保存生效。", MessageType.None);
            if (changed) EditorGUILayout.LabelField("有未保存的顺序更改", EditorStyles.boldLabel);
            if (GUILayout.Button("保存关卡顺序")) Save();
            if (GUILayout.Button("重新读取已保存顺序") &&
                (!changed || EditorUtility.DisplayDialog("重新读取", "放弃本窗口尚未保存的顺序更改？", "重新读取", "取消"))) Reload();
            if (!string.IsNullOrEmpty(message)) EditorGUILayout.HelpBox(message, MessageType.Info);
        }
    }

    void Swap(int a, int b)
    { var value = levels[a]; levels[a] = levels[b]; levels[b] = value; changed = true; }

    void Reload()
    {
        levels.Clear();
        if (File.Exists(ConfigPath))
        {
            var order = JsonUtility.FromJson<BoxedOutLevelOrder>(File.ReadAllText(ConfigPath));
            for (int i = 0; i < (order.levels ?? new string[0]).Length; i++)
            {
                string path = order.guids != null && i < order.guids.Length
                    ? AssetDatabase.GUIDToAssetPath(order.guids[i]) : order.levels[i];
                levels.Add(AssetDatabase.LoadAssetAtPath<SceneAsset>(path));
            }
        }
        else
        {
            foreach (var scene in EditorBuildSettings.scenes.Where(s => s.enabled &&
                s.path != BoxedOutLevelOrder.Menu && s.path != BoxedOutLevelOrder.Ending))
                levels.Add(AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path));
        }
        loaded = true; changed = false; message = null;
    }

    void Save()
    {
        try
        {
            var paths = levels.Select(s => AssetDatabase.GetAssetPath(s)).ToArray();
            if (paths.Length == 0 || paths.Any(string.IsNullOrEmpty))
                throw new InvalidOperationException("至少添加一个关卡，且不能有空白或丢失的场景。");
            if (paths.Distinct().Count() != paths.Length || paths.Contains(BoxedOutLevelOrder.Menu) || paths.Contains(BoxedOutLevelOrder.Ending))
                throw new InvalidOperationException("关卡不能重复，也不能包含首页或结尾。");
            var all = new[] { BoxedOutLevelOrder.Menu }.Concat(paths).Concat(new[] { BoxedOutLevelOrder.Ending }).ToArray();
            if (all.Any(p => AssetDatabase.LoadAssetAtPath<SceneAsset>(p) == null))
                throw new InvalidOperationException("场景文件不存在，请检查首页、关卡和结尾。");

            // Validate without changing the user's open scene or its unsaved edits.
            foreach (var path in paths)
            {
                var scene = EditorSceneManager.OpenPreviewScene(path);
                try
                {
                    var report = BoxedOutLevelEditorWindow.Inspect(scene);
                    if (!report.IsLevel || report.Issues.Count != 0)
                        throw new InvalidOperationException(Path.GetFileNameWithoutExtension(path) + " 未通过关卡检查：" +
                            (report.IsLevel ? string.Join("；", report.Issues) : "缺少启用的 LevelManager。"));
                }
                finally { EditorSceneManager.ClosePreviewScene(scene); }
            }
            string backup = "Backups/level-order-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");
            Directory.CreateDirectory(backup);
            File.Copy("ProjectSettings/EditorBuildSettings.asset", backup + "/EditorBuildSettings.asset");
            if (File.Exists(ConfigPath)) File.Copy(ConfigPath, backup + "/BoxedOutLevelOrder.json");
            Directory.CreateDirectory("Assets/Resources");
            var data = new BoxedOutLevelOrder { levels = paths, guids = paths.Select(p => AssetDatabase.AssetPathToGUID(p)).ToArray() };
            File.WriteAllText(ConfigPath, JsonUtility.ToJson(data, true));
            AssetDatabase.ImportAsset(ConfigPath);
            // Keep other registered scenes, but make the campaign and startup order explicit.
            EditorBuildSettings.scenes = all.Select(p => new EditorBuildSettingsScene(p, true))
                .Concat(EditorBuildSettings.scenes.Where(s => !all.Contains(s.path))).ToArray();
            changed = false;
            message = "已保存。下次从首页开始游戏时生效。备份：" + backup;
        }
        catch (Exception ex) { EditorUtility.DisplayDialog("未保存关卡顺序", ex.Message, "知道了"); }
    }
}

