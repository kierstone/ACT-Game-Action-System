
using System;
using System.Collections.Generic;
using UnityEngine;

public static class MoveTween
{
    /// <summary>
    /// 函数组，key是string对应的，value则是对应的函数，value的参数：
    /// "startPos" ：Vector3，移动开始时候的坐标;
    /// "moveInfo" ： MoveInfo 移动信息;
    /// "timeElapsed": float 这个移动信息运行了多久了（秒）
    /// </summary>
    public static Dictionary<string, Func<Vector3, MoveInfo, float, Vector3>> Methods =
        new Dictionary<string, Func<Vector3, MoveInfo, float, Vector3>>
        {
            //--------------------------越来越慢------------------------------------
            {
                "Slower",
                (Vector3 startPos, MoveInfo moveInfo, float timeElapsed) =>
                {
                    if (timeElapsed >= moveInfo.inSec) return moveInfo.moveDistance + startPos;
                    float p = Mathf.Clamp01(timeElapsed / moveInfo.inSec);
                    Vector3 moved = moveInfo.moveDistance * (1.00f - Mathf.Pow(1.00f - p, 3f));
                    return startPos + moved;
                }
            }
        };
    
}