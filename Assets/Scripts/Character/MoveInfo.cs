using System;
using UnityEngine;

/// <summary>
/// 角色移动的信息
/// </summary>
[Serializable]
public struct MoveInfo
{
    /// <summary>
    /// 最终移动这么多距离
    /// </summary>
    public Vector3 moveDistance;

    /// <summary>
    /// 在这么多秒内完成移动
    /// </summary>
    public float inSec;

    /// <summary>
    /// 移动函数，会从Methods/MoveTween中找到对应的函数，如果没有这个函数，就会按照匀速的来
    /// 空字符串时直接视为没有这个函数
    /// </summary>
    public string tweenMethod;

    public static MoveInfo Zero => new MoveInfo
    {
        moveDistance = Vector3.zero,
        inSec = 0,
        tweenMethod = ""
    };
}
