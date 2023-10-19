using System;
using UnityEngine;

/// <summary>
/// 百分比区域
/// </summary>
[Serializable]
public struct PercentageRange
{
    public float min;
    public float max;

    public PercentageRange(float minPercentage, float maxPercentage)
    {
        min = Mathf.Clamp01(minPercentage);
        max = Mathf.Clamp01(maxPercentage);
    }
}