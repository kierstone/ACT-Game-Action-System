using System;

/// <summary>
/// 根据命令得出的操作
/// </summary>
[Serializable]
public struct ActionCommand
{
    /// <summary>
    /// 按键顺序
    /// </summary>
    public KeyMap[] keySequence;

    /// <summary>
    /// 检查的按键最远的一次操作距离现在的最远时间（秒）
    /// </summary>
    public float validInSec;
}


