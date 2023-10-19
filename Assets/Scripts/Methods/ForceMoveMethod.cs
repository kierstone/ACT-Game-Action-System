using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 强制位移的方法
/// 参数1: ForceMove这条数据是什么
/// 返回值：当前移动量
/// </summary>
public static class ForceMoveMethod
{
    public static Dictionary<string, Func<ForceMove, Vector3>> Methods =
        new Dictionary<string, Func<ForceMove, Vector3>>()
        {
            {
                "Slowly",
                (forceMove) =>
                {
                    if (forceMove.Data.inSec <= 0) return Vector3.zero;
                    float wasPec = Mathf.Clamp01(forceMove.WasElapsed / forceMove.Data.inSec);
                    float curPec = Mathf.Clamp01(forceMove.TimeElapsed / forceMove.Data.inSec);
                    Vector3 was = forceMove.Data.moveDistance * (1.00f - Mathf.Pow(1.00f - wasPec, 3));
                    Vector3 cur = forceMove.Data.moveDistance * (1.00f - Mathf.Pow(1.00f - curPec, 3));
                    return cur - was;
                }
            }
        };
}