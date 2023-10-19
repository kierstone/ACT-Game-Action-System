using System;

/// <summary>
/// CancelTag和BeCancelledTag是一对用于动作切换的核心数据
/// 在Unity或者UE的Montage中，我们都用指定一段时间，在这段时间内
/// 开启BeCancelledTag，让CancelTag对应的动作有可能在这段时间内可以Cancel当前动作
/// </summary>
[Serializable]
public struct CancelTag
{
    /// <summary>
    /// 这个tag的字符串，可以理解为id
    /// </summary>
    public string tag;

    /// <summary>
    /// 这个动作会从normalized多少的地方开始播放
    /// </summary>
    public float startFromPercentage;

    /// <summary>
    /// 动画融合进来的百分比时间长度
    /// </summary>
    public float fadeInPercentage;
    
    /// <summary>
    /// 当从这里Cancel动作时，优先级变化
    /// </summary>
    public int priority;
}

[Serializable]
public struct BeCancelledTag
{
    /// <summary>
    /// 时间段
    /// </summary>
    public PercentageRange percentageRange;

    /// <summary>
    /// 可以Cancel的CancelTag
    /// </summary>
    public string[] cancelTag;

    /// <summary>
    /// 动画融合出去的时间
    /// Unity推荐用normalized作为一个标尺，因为用second对于做动画本身有点要求
    /// 当然也可能是我对CrossFadeInFixedTime理解有误
    /// </summary>
    public float fadeOutPercentage;
    
    /// <summary>
    /// 当从这里被Cancel，动作会增加多少优先级
    /// </summary>
    public int priority;

    /// <summary>
    /// 根据TempBeCancelledTag和产生这个Tag的百分比时间点，算出一个新的BeCancelledTag
    /// </summary>
    /// <param name="tempTag"></param>
    /// <param name="fromPercentage"></param>
    /// <returns></returns>
    public static BeCancelledTag FromTemp(TempBeCancelledTag tempTag, float fromPercentage) => new BeCancelledTag
    {
        percentageRange = new PercentageRange(fromPercentage, fromPercentage + tempTag.percentage),
        cancelTag = tempTag.cancelTag,
        fadeOutPercentage = tempTag.fadeOutPercentage,
        priority = tempTag.priority
    };
}

[Serializable]
public struct TempBeCancelledTag
{
    /// <summary>
    /// 因为需要被索引，所以需要一个id
    /// </summary>
    public string id;
    
    /// <summary>
    /// 在当前动作中，有百分之多少的时间是开启的
    /// 从开启的时间往后算
    /// </summary>
    public float percentage;
    
    /// <summary>
    /// 可以Cancel的CancelTag
    /// </summary>
    public string[] cancelTag;

    /// <summary>
    /// 动画融合出去的时间
    /// Unity推荐用normalized作为一个标尺，因为用second对于做动画本身有点要求
    /// 当然也可能是我对CrossFadeInFixedTime理解有误
    /// </summary>
    public float fadeOutPercentage;
    
    /// <summary>
    /// 当从这里被Cancel，动作会增加多少优先级
    /// </summary>
    public int priority;
}