using UnityEngine;

/// <summary>
/// 命中信息记录
/// </summary>
public class HitRecord
{
    /// <summary>
    /// 打中的目标是谁
    /// </summary>
    public int UniqueId;
    /// <summary>
    /// 这是第几段伤害
    /// </summary>
    public int Phase = 0;
    /// <summary>
    /// 还能打中他几次
    /// </summary>
    public int CanHitTimes = 0;
    /// <summary>
    /// 冷却多久以后才能继续打他
    /// </summary>
    public float Cooldown = 0;

    public HitRecord(CharacterObj cha, int phase, int canHitTimes, float cooldown)
    {
        UniqueId = cha.gameObject.GetInstanceID();
        Phase = phase;
        CanHitTimes = canHitTimes;
        Cooldown = cooldown;
    }

    public void Update(float delta)
    {
        if (Cooldown > 0)
            Cooldown = Mathf.Max(0, Cooldown - delta);
    }
}