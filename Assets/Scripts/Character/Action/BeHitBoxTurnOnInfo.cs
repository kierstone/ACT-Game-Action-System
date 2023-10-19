using System;

/// <summary>
/// 开启一些受击盒子的信息
/// 可以理解为“一部分防御”
/// </summary>
[Serializable]
public struct BeHitBoxTurnOnInfo
{
    /// <summary>
    /// 开启的时间区域，可以分为多段时间开启
    /// </summary>
    public PercentageRange[] inPercentage;
    
    /// <summary>
    /// 要开启的盒子的tag
    /// </summary>
    /// <returns></returns>
    public string[] tag;

    /// <summary>
    /// 这样开启的盒子，优先级会发生怎样的临时变化
    /// </summary>
    public int priority;
    
    /// <summary>
    /// 如果命中了这里的受击框，就会临时开启一些tempBeCancelledTag，这里用id去索引
    /// </summary>
    public string[] tempBeCancelledTagTurnOn;
    
    /// <summary>
    /// 与攻击框不同（是因为这个demo的游戏设计精度所决定的），受击框本身会决定这次受到攻击的时候双方的动作。
    /// 因为我们完全可以开启一个受击框代表盾牌的同时，还有一个受击框代表屁股，屁股挨揍和盾牌挨揍效果完全不同
    /// </summary>
    public ActionChangeInfo attackerActionChange;
    
    /// <summary>
    /// 与攻击框不同（是因为这个demo的游戏设计精度所决定的），受击框本身会决定这次受到攻击的时候双方的动作。
    /// 因为我们完全可以开启一个受击框代表盾牌的同时，还有一个受击框代表屁股，屁股挨揍和盾牌挨揍效果完全不同
    /// </summary>
    public ActionChangeInfo selfActionChange;
}
