using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 受击盒
/// 正确的做法：每个动作帧每个角色可能都会有若干受击盒。
/// 但是受限于Unity和UE之类的对于游戏的理解非常外行，所以给的框架不能很好的实现动作游戏
/// 于是我们只能凑效果，就是在角色身上先绑定了受击盒，然后在合适的时候开启或者关闭他们
/// </summary>
public class BeHitBox : MonoBehaviour
{
    /// <summary>
    /// 标签，可以被当做一种分类方式
    /// 比如我们开启一些受击盒的时候可以byTag，只要tags里面有指定的tag就算是符合条件了。
    /// </summary>
    [Tooltip("盒子的tag")] public string[] tags;

    /// <summary>
    /// 肉质，就是MH里面的那个肉质，打在这个部位掉血的伤害值x这个数字等于伤害量，所以越大代表越软。
    /// 【关键1】
    /// 如果是MH这样复杂的游戏，肉质本身是一个struct，里面包含了斩击、打击等数据，这里就简化为一个float了
    /// 【关键2】
    /// 除了肉质之外，还可以有很多其他属性，根据游戏的具体需要去添加，这里只是一个范例。
    /// 比如我们需要某个受击盒对应一个角色部位，打中这个盒子，部位耐久度下降，最后部位还会被破坏，也应该在受击盒提供对应信息。
    /// </summary>
    [Tooltip("肉质"), Range(0, float.MaxValue)]public float meat = 1;
    
    /// <summary>
    /// 这个框的主人
    /// </summary>
    [HideInInspector] public CharacterObj master;
    
    /// <summary>
    /// 当同一帧有多个攻击盒命中了同一个角色的受击盒时，我们算是命中了，这点没错
    /// 但是有一个问题，到底应该算哪个攻击盒打中了对手呢？总得有个结论的，所以用Priority来进行冒泡
    /// </summary>
    [Tooltip("基础的优先级")] public int basePriority;
    /// <summary>
    /// 临时的优先级
    /// </summary>
    private int _tempPriority = 0;
    /// <summary>
    /// 受击盒当前的优先级
    /// </summary>
    public int Priority => _tempPriority + basePriority;
    
    /// <summary>
    /// 当前是否开启了
    /// </summary>
    public bool Active { get; private set; }

    private void Update()
    {
        Active = master && master.ShouldBeHitBoxActive(tags);
    }

    /// <summary>
    /// 是否有一个tag在checkTags里面
    /// </summary>
    /// <param name="checkTags"></param>
    /// <returns></returns>
    public bool TagHit(List<string> checkTags)
    {
        foreach (string t in tags)
        {
            if (checkTags.Contains(t)) return true;
        }

        return false;
    }
}
