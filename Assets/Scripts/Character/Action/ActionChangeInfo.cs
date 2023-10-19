using System;
using UnityEngine;

/// <summary>
/// 动作变化信息，预约角色动作变化的一种信息
/// 通常在动作命中对方的时候，会得出双方的ActionChangeInfo，用来调整双方动作走向
/// </summary>
[Serializable]
public struct ActionChangeInfo
{
    [Tooltip("动作变化方式")] public ActionChangeType changeType;

    [Tooltip("变化方式的参数，如果是ToActionId则是要指向的Action的id；如果是ByCatalog则指向Action的Catalog属性")]
    public string param;
    
    [Tooltip("优先级，因为有多个变化信息会要求角色变化动作，但是角色最后只能变到一个，所以得冒泡")]
    public int priority;

    [Tooltip("从百分之多少开始这个动作"), Range(0.00f, 1.00f)]
    public float fromNormalized;

    [Tooltip("融合长度"), Range(0.00f, 1.00f)]
    public float transNormalized;
}

[Serializable]
public enum ActionChangeType
{
    /// <summary>
    /// 不发生变化
    /// </summary>
    Keep,
    /// <summary>
    /// 预约某个指定的ActionId的动画
    /// </summary>
    ChangeToActionId,
    /// <summary>
    /// 根据Catalog来预约Action
    /// </summary>
    ChangeByCatalog,
}