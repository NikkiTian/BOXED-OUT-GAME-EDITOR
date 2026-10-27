using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BoxedOutLevelBrush
{
    public enum Kind { Wall, Box, Goal, Player, Floor }
    public const string FloorPrefabPath = "Assets/3D Models/Prefabs/Floor.prefab";

    public static Vector3 FloorPosition(LevelManager m, Vector3Int cell)
    {
        // Existing XZY levels use one-unit cells; floor roots are Y=-1, actors Y=0.
        return GridController.GridToWorldPos(m.InteractablesTmap, cell) + Vector3.down;
    }

    private static string ApplyFloor(LevelManager m, Vector3Int cell, bool erase)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FloorPrefabPath);
        if (prefab == null) return "找不到 Floor 预制体，未修改关卡。";
        var position = FloorPosition(m, cell);
        var floors = m.gameObject.scene.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
            .Where(t => PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject) &&
                PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject) == FloorPrefabPath).ToArray();
        var targets = floors.Where(t => (t.position - position).sqrMagnitude < .0001f).ToArray();
        if (!erase && targets.Length > 0) return "此格已有地板。";
        if (erase && targets.Length == 0) return "此格没有 Floor 地板。";
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName(erase ? "擦除地板" : "放置地板");
        try
        {
            if (erase)
                foreach (var target in targets) Undo.DestroyObjectImmediate(target.gameObject);
            else
            {
                var parent = floors.FirstOrDefault(t => t.parent != null && t.parent != m.InteractablesTmap.transform)?.parent;
                if (parent == null) parent = m.InteractablesTmap.transform.parent;
                var obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, m.gameObject.scene);
                Undo.RegisterCreatedObjectUndo(obj, "放置地板");
                if (parent != null) Undo.SetTransformParent(obj.transform, parent, "地板分组");
                Undo.RecordObject(obj.transform, "地板高度对齐");
                obj.transform.position = position;
                PrefabUtility.RecordPrefabInstancePropertyModifications(obj.transform);
            }
            EditorSceneManager.MarkSceneDirty(m.gameObject.scene);
        }
        finally { Undo.CollapseUndoOperations(group); }
        return erase ? "已擦除地板，上层物件保留；可 Ctrl+Z 撤销。" : "已放置地板（低于物件 1 格）；可 Ctrl+Z 撤销。";
    }

    public static LevelManager Current()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var managers = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<LevelManager>())
            .Where(m => m.isActiveAndEnabled).ToArray();
        return managers.Length == 1 ? managers[0] : null;
    }

    // One invocation is one undo operation, including the goal-reference update.
    public static string Apply(LevelManager m, Vector3Int cell, Kind kind, bool erase)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return "请先退出 Play 模式。";
        if (m == null || m.InteractablesTmap == null) return "需要一个有效的关卡管理器和交互 Tilemap。";
        var map = m.InteractablesTmap;
        if (map.gameObject.scene != m.gameObject.scene) return "Tilemap 必须属于当前关卡。";
        if (map.layoutGrid.cellSwizzle != GridLayout.CellSwizzle.XZY ||
            map.transform.rotation != Quaternion.identity || map.transform.lossyScale != Vector3.one)
            return "当前画笔仅支持现有关卡的 XZY、无旋转、单位缩放网格。";
        cell.z = 0;
        if (kind == Kind.Floor) return ApplyFloor(m, cell, erase);
        var players = m.gameObject.scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<PlayerController>()).ToArray();
        if (kind == Kind.Player && players.Length != 1) return "移动出生点前，场景必须有且仅有一个玩家。";
        if (kind == Kind.Player && erase) return "玩家不能擦除；选择玩家工具点击空格移动出生点。";
        var occupants = map.transform.Cast<Transform>().Where(t => t.gameObject.activeInHierarchy && map.WorldToCell(t.position) == cell).ToArray();
        Func<Transform, bool> isWall = t => m.Wall != null && t.CompareTag(m.Wall.tag);
        Func<Transform, bool> isBox = t => m.Moveable != null && t.CompareTag(m.Moveable.tag);
        Func<Transform, bool> matches = t => kind == Kind.Goal ? t.GetComponent<Goal>() != null : kind == Kind.Wall ? isWall(t) : isBox(t);
        var targets = occupants.Where(matches).ToArray();
        if (erase && targets.Length == 0) return "此格没有所选物件。";
        if (!erase)
        {
            bool playerHere = players.Any(p => map.WorldToCell(p.transform.position) == cell);
            if (kind == Kind.Wall && (occupants.Any(t => isWall(t) || isBox(t) || t.GetComponent<Goal>() != null) || playerHere)) return "墙不能覆盖玩家、箱子或目标点。";
            if (kind != Kind.Wall && occupants.Any(isWall)) return "此格有墙。";
            if ((kind == Kind.Box || kind == Kind.Player) && occupants.Any(isBox)) return "此格已有箱子。";
            if (kind == Kind.Box && playerHere) return "不能把箱子放在玩家所在格。";
            if (kind == Kind.Goal && targets.Length > 0) return "此格已有目标点。";
        }
        GameObject prefab = null;
        if (!erase && kind != Kind.Player)
        {
            var source = kind == Kind.Wall ? m.Wall : kind == Kind.Box ? m.Moveable :
                (m.Goals ?? new GameObject[0]).FirstOrDefault(g => g != null);
            if (source != null)
                prefab = EditorUtility.IsPersistent(source) ? source : PrefabUtility.GetCorrespondingObjectFromSource(source);
            if (prefab == null && kind == Kind.Goal)
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/3D Models/Prefabs/Goal.prefab");
            if (prefab == null) return "找不到该物件的预制体，未修改关卡。";
        }
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName(erase ? "擦除关卡物件" : "放置关卡物件");
        try
        {
            if (erase)
            {
                if (kind == Kind.Goal)
                {
                    Undo.RecordObject(m, "更新目标点引用");
                    m.Goals = (m.Goals ?? new GameObject[0]).Where(g => g != null && !targets.Any(t => t.gameObject == g)).ToArray();
                    PrefabUtility.RecordPrefabInstancePropertyModifications(m);
                }
                foreach (var t in targets) Undo.DestroyObjectImmediate(t.gameObject);
            }
            else if (kind == Kind.Player)
            {
                Undo.RecordObject(players[0].transform, "移动玩家出生点");
                players[0].transform.position = GridController.GridToWorldPos(map, cell);
                PrefabUtility.RecordPrefabInstancePropertyModifications(players[0].transform);
            }
            else
            {
                var obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, map.transform);
                Undo.RegisterCreatedObjectUndo(obj, "放置关卡物件");
                Undo.RecordObject(obj.transform, "对齐网格");
                obj.transform.position = GridController.GridToWorldPos(map, cell);
                PrefabUtility.RecordPrefabInstancePropertyModifications(obj.transform);
                if (kind == Kind.Goal)
                {
                    Undo.RecordObject(m, "更新目标点引用");
                    m.Goals = (m.Goals ?? new GameObject[0]).Concat(new[] { obj }).ToArray();
                    PrefabUtility.RecordPrefabInstancePropertyModifications(m);
                }
            }
            EditorSceneManager.MarkSceneDirty(m.gameObject.scene);
        }
        finally { Undo.CollapseUndoOperations(group); }
        return erase ? "已擦除，可 Ctrl+Z 撤销。" : "已放置，可 Ctrl+Z 撤销。";
    }
}
