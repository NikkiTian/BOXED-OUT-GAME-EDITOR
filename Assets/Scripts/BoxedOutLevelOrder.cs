using System;
using UnityEngine;

// Saved by the level editor; scene paths also work when names are duplicated.
[Serializable]
public class BoxedOutLevelOrder
{
    public string[] levels;
    public string[] guids;
    public const string ResourceName = "BoxedOutLevelOrder";
    public const string Menu = "Assets/Scenes/Menu.unity";
    public const string Ending = "Assets/Scenes/Ending.unity";

    public static string[] LoadScenes()
    {
        var asset = Resources.Load<TextAsset>(ResourceName);
        if (asset == null) return null;
        var order = JsonUtility.FromJson<BoxedOutLevelOrder>(asset.text);
        if (order == null || order.levels == null || order.levels.Length == 0)
            throw new InvalidOperationException("关卡顺序为空，请在关卡编辑器中保存有效的关卡顺序。");
        var scenes = new string[order.levels.Length + 2];
        scenes[0] = Menu;
        Array.Copy(order.levels, 0, scenes, 1, order.levels.Length);
        scenes[scenes.Length - 1] = Ending;
        return scenes;
    }
}
