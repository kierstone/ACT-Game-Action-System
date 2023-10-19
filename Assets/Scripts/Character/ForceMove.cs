using System;
using UnityEngine;

/// <summary>
/// 强制位移信息
/// </summary>
public class ForceMove
{
    public MoveInfo Data;
    public float TimeElapsed = 0;
    public float WasElapsed = 0;
    public Func<ForceMove, Vector3> MoveTween = _ => Vector3.zero;

    public static ForceMove FromData(MoveInfo data) => new ForceMove
    {
        Data = data,
        TimeElapsed = 0,
        WasElapsed = 0,
        MoveTween = ForceMoveMethod.Methods.ContainsKey(data.tweenMethod)
            ? ForceMoveMethod.Methods[data.tweenMethod]
            : _ => Vector3.zero
    };

    public static ForceMove NoForceMove => new ForceMove
    {
        Data = new MoveInfo(),
        WasElapsed = float.MaxValue,
        TimeElapsed = float.MaxValue,
        MoveTween = _ => Vector3.zero
    };

    public void Update(float delta)
    {
        WasElapsed = TimeElapsed;
        TimeElapsed += delta;
    }
}