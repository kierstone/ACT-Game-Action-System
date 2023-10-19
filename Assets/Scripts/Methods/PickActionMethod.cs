
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 根据Catalog选择技能的函数
/// key是Catalog
/// value是(List of ActionInfo, ForceDirection)=>ActionInfo
/// 执行value获得一个结果
/// </summary>
public static class PickActionMethod
{
    public static Dictionary<string, Func<List<ActionInfo>, ForceDirection, ActionInfo>> Methods =
        new Dictionary<string, Func<List<ActionInfo>, ForceDirection, ActionInfo>>()
        {
            //受伤
            {
               "Hurt",
               (candidates, dir) =>
               {
                   foreach (ActionInfo info in candidates)
                   {
                       if (
                           (info.id == "HurtFromForward" && dir == ForceDirection.Forward) ||
                           (info.id == "HurtFromBackward" && dir == ForceDirection.Backward)
                       ) return info;
                   }
                   return new ActionInfo();
               }
            },
            //受伤
            {
                "Beaten",
                (candidates, dir) =>
                {
                    foreach (ActionInfo info in candidates)
                    {
                        if (
                            (info.id == "BeatenFromForward" && dir == ForceDirection.Forward) ||
                            (info.id == "BeatenFromBackward" && dir == ForceDirection.Backward)
                        ) return info;
                    }
                    return new ActionInfo();
                }
            },
        };
}