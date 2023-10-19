using System;

/// <summary>
/// 攻击盒开启的信息
/// 可以视为“一段攻击”
/// </summary>
[Serializable]
public struct AttackBoxTurnOnInfo
{
    /// <summary>
    /// 开启的时间段
    /// </summary>
    public PercentageRange[] inPercentage;

    /// <summary>
    /// 要开启的攻击盒的tag
    /// </summary>
    public string[] tag;

    /// <summary>
    /// 这段攻击的逻辑数据是ActionInfo中的哪个AttackInfo
    /// </summary>
    public int attackPhase;
    
    /// <summary>
    /// 这样开启的盒子，优先级会发生怎样的临时变化
    /// </summary>
    public int priority;
}