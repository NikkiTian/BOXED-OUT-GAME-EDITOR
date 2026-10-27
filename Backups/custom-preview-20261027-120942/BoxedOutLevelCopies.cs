using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BoxedOutLevelCopies
{
    public static void SaveCurrentCopy()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || !scene.IsValid()) return;
        var path = EditorUtility.SaveFilePanelInProject("另存为新关卡", scene.name + "_Custom", "unity",
            "保存独立副本，不覆盖已有场景。", "Assets/Scenes");
        if (string.IsNullOrEmpty(path)) return;
        try
        {
            SaveCopy(scene, path);
            var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
            EditorUtility.DisplayDialog("关卡副本已保存", "位置：" + path +
                "\n\n当前仍在编辑原场景，原场景未被保存或替换。要编辑副本，请双击 Project 中已选中的新场景；如有未保存修改，Unity 会询问如何处理。\n\n副本尚未加入正式关卡顺序。", "知道了");
        }
        catch (Exception ex) { EditorUtility.DisplayDialog("未保存", ex.Message, "知道了"); }
    }

    public static void SaveCopy(Scene scene, string path)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("请先退出 Play 模式。");
        if (!scene.IsValid() || !scene.isLoaded || !BoxedOutLevelEditorWindow.Inspect(scene).IsLevel)
            throw new InvalidOperationException("请先打开玩法关卡。");
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("请填写场景路径。");
        path = path.Replace('\\', '/');
        var root = Path.GetFullPath(Application.dataPath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var full = Path.GetFullPath(path);
        if (!path.StartsWith("Assets/", StringComparison.Ordinal) || !full.StartsWith(root, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(Path.GetExtension(path), ".unity", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("请保存为 Assets 文件夹内的 .unity 场景。");
        if (!Directory.Exists(Path.GetDirectoryName(full))) throw new ArgumentException("请先选择已存在的文件夹。");
        if (File.Exists(full) || File.Exists(full + ".meta") || !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path)))
            throw new IOException("该路径已有文件，请换一个名称；不会覆盖已有关卡。");
        if (!EditorSceneManager.SaveScene(scene, path, true)) throw new IOException("Unity 未能保存副本。");
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
    }
}
