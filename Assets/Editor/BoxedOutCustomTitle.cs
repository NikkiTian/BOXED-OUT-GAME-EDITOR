using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class BoxedOutCustomTitle
{
    public const string Path = "Assets/Sprites/Prefabs/Level Title Card_Custom.prefab";
    public static GameObject Validate()
    {
        var p = AssetDatabase.LoadAssetAtPath<GameObject>(Path);
        if (p == null || p.GetComponent<LevelTitleCard>() == null || p.GetComponent<CanvasGroup>() == null || p.GetComponentInChildren<TMPro.TMP_Text>(true) == null)
            throw new InvalidOperationException("Custom 标题缺少必要组件。");
        return p;
    }
    public static void Replace(Scene scene)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("请退出 Play 模式。");
        if (new[] { "Assets/Scenes/Lvl1.unity", "Assets/Scenes/Lvl2.unity", "Assets/Scenes/Lvl3.unity", "Assets/Scenes/Lvl4.unity" }.Contains(scene.path))
            throw new InvalidOperationException("请先另存为自定义关卡，原四关保留原版标题。");
        var prefab = Validate();
        var cards = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<LevelTitleCard>(true)).ToArray();
        if (cards.Length != 1) throw new InvalidOperationException("关卡必须只有一个 LevelTitleCard。");
        var old = cards[0].gameObject;
        if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(old) == Path) return;
        Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("使用 Custom 标题");
        try
        {
            var obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            Undo.RegisterCreatedObjectUndo(obj, "创建 Custom 标题");
            Undo.SetTransformParent(obj.transform, old.transform.parent, "标题位置");
            Undo.RecordObject(obj.transform, "保留标题布局");
            var src = old.GetComponent<RectTransform>(); var dst = obj.GetComponent<RectTransform>();
            if (src != null && dst != null)
            { dst.anchorMin = src.anchorMin; dst.anchorMax = src.anchorMax; dst.pivot = src.pivot; dst.sizeDelta = src.sizeDelta; dst.anchoredPosition3D = src.anchoredPosition3D; }
            obj.transform.localRotation = old.transform.localRotation; obj.transform.localScale = old.transform.localScale;
            obj.transform.SetSiblingIndex(old.transform.GetSiblingIndex());
            PrefabUtility.RecordPrefabInstancePropertyModifications(obj.transform);
            Undo.DestroyObjectImmediate(old);
            EditorSceneManager.MarkSceneDirty(scene);
        }
        finally { Undo.CollapseUndoOperations(group); }
    }
}
