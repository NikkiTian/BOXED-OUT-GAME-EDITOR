using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BoxedOutBrushVerification
{
    const string Request = "Library/BoxedOutBrushVerification.request";
    const string Report = "Library/BoxedOutBrushVerification.txt";
    [InitializeOnLoadMethod]
    static void Initialize() { EditorApplication.delayCall += CheckRequest; }
    static void CheckRequest()
    {
        if (!File.Exists(Request) || File.ReadAllText(Request) != "pending") return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
        { EditorApplication.delayCall += CheckRequest; return; }
        File.WriteAllText(Request, "running");
        Run();
        File.WriteAllText(Request, "finished");
    }
    [MenuItem("Tools/BOXED-OUT/Verify Brush")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var lines = new List<string>();
        var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Lvl1.unity");
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        try
        {
            var m = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<LevelManager>()).Single();
            var cell = new Vector3Int(80, 80, 0);
            Func<int> count = () => m.InteractablesTmap.transform.childCount;
            Action<bool, string> check = (pass, name) => { if (!pass) throw new Exception(name); lines.Add("PASS: " + name); };
            Action<BoxedOutLevelBrush.Kind, bool> apply = (kind, erase) => { lines.Add(BoxedOutLevelBrush.Apply(m, cell, kind, erase)); Undo.FlushUndoRecordObjects(); };
            int initial = count(), goals = m.Goals.Length;
            apply(BoxedOutLevelBrush.Kind.Box, false);
            check(count() == initial + 1, "place box");
            Undo.PerformUndo(); check(count() == initial, "undo box");
            Undo.PerformRedo(); check(count() == initial + 1, "redo box");
            apply(BoxedOutLevelBrush.Kind.Box, false); check(count() == initial + 1, "block duplicate box");
            apply(BoxedOutLevelBrush.Kind.Wall, false); check(count() == initial + 1, "block wall over box");
            apply(BoxedOutLevelBrush.Kind.Goal, false);
            check(count() == initial + 2 && m.Goals.Length == goals + 1 && m.Goals.Last() != null, "goal with box and reference");
            Undo.PerformUndo(); check(count() == initial + 1 && m.Goals.Length == goals, "undo goal and reference");
            Undo.PerformRedo(); check(count() == initial + 2 && m.Goals.Length == goals + 1 && m.Goals.Last() != null, "redo goal and reference");
            apply(BoxedOutLevelBrush.Kind.Goal, true); check(count() == initial + 1 && m.Goals.Length == goals, "erase only goal and reference");
            Undo.PerformUndo(); check(count() == initial + 2 && m.Goals.Length == goals + 1 && m.Goals.Last() != null, "undo goal erasure");
            cell = new Vector3Int(82, 80, 0);
            var player = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<PlayerController>()).Single();
            var old = player.transform.position;
            apply(BoxedOutLevelBrush.Kind.Player, false);
            check(player.transform.position == GridController.GridToWorldPos(m.InteractablesTmap, cell), "move player");
            Undo.PerformUndo(); check(player.transform.position == old, "undo player");
            apply(BoxedOutLevelBrush.Kind.Wall, false);
            var walls = count(); apply(BoxedOutLevelBrush.Kind.Box, false); check(count() == walls, "block box on wall");
            apply(BoxedOutLevelBrush.Kind.Wall, true); check(count() == walls - 1, "erase wall");
            Undo.PerformUndo(); check(count() == walls, "undo wall erasure");
            lines.Add("SUCCESS; tests used an unsaved preview scene only.");
        }
        catch (Exception ex) { lines.Add("FAILED: " + ex); }
        finally
        {
            Undo.RevertAllDownToGroup(group);
            EditorSceneManager.ClosePreviewScene(scene);
            File.WriteAllLines(Report, lines);
        }
    }
}
