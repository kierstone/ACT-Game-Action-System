using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 游戏的静态数据，这里有一些是凑效果的
/// </summary>
public static class GameData
{
    private static bool _loaded = false; 
    public static Dictionary<string, ActionInfo> Actions = new Dictionary<string, ActionInfo>();

    public static void Load()
    {
        if (_loaded) return;
        _loaded = true;
        Actions.Clear();

        TextAsset ta = Resources.Load<TextAsset>("GameData/Action");
        if (ta)
        {
            ActionInfoContainer aic = JsonUtility.FromJson<ActionInfoContainer>(ta.text);
            foreach (ActionInfo info in aic.data)
            {
                if (info.id != "") Actions.Add(info.id, info);
            }
        }
    }

    public static List<ActionInfo> AllActions()
    {
        List<ActionInfo> res = new List<ActionInfo>();
        foreach (KeyValuePair<string,ActionInfo> actionInfo in Actions)
        {
            res.Add(actionInfo.Value);
        }

        return res;
    }
}