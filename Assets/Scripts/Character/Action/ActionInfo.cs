using System;

/// <summary>
/// 角色的一个动作的信息
/// 在这个demo里面我偷了一个懒：就是直接让所有的类似这个结构的结构都变成可以填表的数据了
/// 其实应该有个ActionData，通过这个ActionData转为ActionInfo来用，这也是大多团队都会漏掉做的一步
/// ActionData就是读取Json表的数据，里面只有string int等基本类型，然后通过转换函数：ActionInfo::FromData(ActionData data)
/// 来转换出来类似enum的结构，但是这个比较繁琐，可以在第一次重构的时候做，这个是必须要做的， 不然策划几乎没法做好
/// 尤其是enum现在用数字做，很魔法，最好换成string来做。
/// 但还是这句话，既然是demo……又不是不能用，对吧
///
/// 至于音效和镜头的Tween，也应该是这个结构里面提供的，只是这里就不给范例了，非常简单的
/// 比如镜头的Tween，其实是一个(float normalized)=>(Vector3 offset, float fieldOfView)
/// </summary>
[Serializable]
public struct ActionInfo
{
    /// <summary>
    /// 一个动作的id，每个动作都有一个id，必须是唯一的，同名的action会互相覆盖
    /// 也因为同名的action互相覆盖，所以才可以好好利用这个来做一些玩法，比如MHR中的替换技
    /// </summary>
    public string id;

    /// <summary>
    /// 在Animator中的一个动作的名称，用来叫Animator播放某个动作的
    /// </summary>
    public string animKey;

    /// <summary>
    /// 动作的分类，一个动作不一定要有分类，所以可以是空字符串
    /// 但是他会被用于一些动作切换的时候，比如我们可以定义它为“受伤动作”
    /// </summary>
    public string catalog;

    /// <summary>
    /// 这个动作的CancelTag，他可以Cancel掉哪些动作
    /// </summary>
    public CancelTag[] cancelTag;

    /// <summary>
    /// 这个动作可以被Cancel的信息
    /// 这里是长久存在的BeCancelledTag
    /// </summary>
    public BeCancelledTag[] beCancelledTag;

    /// <summary>
    /// 临时的被Cancel信息
    /// 这个是需要临时开启和关闭的信息
    /// </summary>
    public TempBeCancelledTag[] tempBeCancelledTag;

    /// <summary>
    /// 允许的操作，一个动作未必只有一个方式操作出来
    /// 比如街霸6的春丽百裂腿，236脚和连续按脚都能发
    /// </summary>
    public ActionCommand[] commands;

    /// <summary>
    /// 这里是保持移动方向的倍率，根据游戏不同、精度不同所需要的参数不同
    /// 大多横版游戏这个倍率会有多个值
    /// 这个值的作用是当做这个动作的时候，我继续前进或者后退按照什么倍率来
    /// 在动作游戏中，我们放有些技能，按住前进会向前更多的距离，按住后则会向前较短距离甚至不会向前
    /// 靠的就是这个参数，他的倍率x角色移动速度小于动作本身位移速度，就会导致按住后动作前进距离变短
    /// 大多动作这个值应该都是0，而移动类则是1
    /// 值得注意的是，一个动作的acceptance是阶段性的，比如起跳动作起跳的蹲伏阶段是不能移动的，但是跳起来之后却可以
    /// 如果美术把起跳到最高点做在一个动作了，也问题不大，靠这个来做
    /// 如果策划填表填错了，2段重叠了，那么就取速度慢的那段
    /// </summary>
    public MoveInputAcceptance[] inputAcceptance;

    /// <summary>
    /// 下一个动作的id
    /// 这个id是当动作自然播放完毕之后转向的那个动作，所以必须是一个严格的id
    /// 【注意1】
    /// 在标准的动作游戏中，所有的问题是“下一帧是什么”，所以应该是autoNextFrame，但是我们都被Unity和UE教化了
    /// 或者说不服从于Unity和UE的规范，我们要花更多的时间去搞定用帧而非Update的正确做法，在这个demo里面就不去做了
    /// 所以我们用类似的手法，设计这个autoNextActionId，也就是动作播放完毕之后自动换成什么动作，也更符合现代人的理解
    /// 【注意2】
    /// 由此，你应该发现，类似怪物猎人的斩击斧，是不需要一个状态记录剑形态和斧形态的
    /// 正如拔刀和非拔刀一样，利用好这个autoNextActionId就能做到，比如RT的拔刀动作autoNextActionId是剑形态站立
    /// 而三角的拔刀动作的autoNextActionId是斧形态站立，就能产生出这个效果。
    /// 【注意3】
    /// 类似街霸的格斗游戏中，蹲下这个动作的autoNextActionId应该等于站立动作的
    /// 之所以蹲着，是因为按住了下，导致下蹲动作自己cancel了自己，所以保持蹲着
    /// 因为同一个动作cancel自己，虽然会导致逻辑上这个动作从头开始了，但是由于播放动画走的是Update而非逻辑帧，所以播放动作是单独的，他可以继续播放下去
    /// 所以才会看起来蹲是保持的（因为动作是连贯的，而非重新开始播放），但实际上确实是“新动作”，当然动作游戏里面的核心问题还是“下一帧”。
    /// </summary>
    public string autoNextActionId;

    /// <summary>
    /// 档切换到自己这个动作的时候，是否保持继续播放
    /// 这个意思是：比如移动会被移动自己cancel，这时候移动动作应该继续播放，而不是重置，所以要有这个true
    /// 原本在帧为单位的时候，cancel关系都是nextFrame所以可以通过frame之间的连接关系来，现在要以动作为单位，就靠这个凑出这个效果了
    /// </summary>
    public bool keepPlayingAnim;

    /// <summary>
    /// 是否当没有收到命令的时候，就自动走向autoNext了
    /// 这并不是一个好的做法，正确的做法，应该是在动作过程中设置某些帧
    /// 在这些帧去判断是否还有对应的command，如果没有了就终止了
    /// UE的Montage里面可以用多个NotifyState分布在动画过程
    /// Unity的话得自己做个编辑工具，所以我这个demo就先偷懒了——如果true，就是每一帧都做这个检查
    /// 其实从逻辑结构来说，我可以写成一个float[]，每个float代表一个percentage检查一下
    /// 但是这个填表………不是地球人可以轻易做到的（烦得很）………所以就先偷懒了，但是意思是一样的
    /// </summary>
    public bool autoTerminate;
    
    /// <summary>
    /// 在这个动作期间存在的攻击信息
    /// </summary>
    public AttackInfo[] attacks;

    /// <summary>
    /// 每一段攻击的信息
    /// </summary>
    public AttackBoxTurnOnInfo[] attackPhase;

    /// <summary>
    /// 受击框开关信息，
    /// 若有2个阶段重叠，那么战斗中的信息取哪个？
    /// 首先会看受击框属于哪个信息的，如果受击框同时被多个重叠的阶段开启
    /// 那就真的听天由命了（这就是策划配表问题了，只能通过编辑器ui解决）
    /// </summary>
    public BeHitBoxTurnOnInfo[] defensePhase;

    /// <summary>
    /// 指向Methods/RootMotionMethods下的RootMotion函数
    /// 如果这个函数找不到或者这个值为空，则会返回停留在原地(Vector3.zero)
    /// </summary>
    public ScriptMethodInfo rootMotionTween;

    /// <summary>
    /// 优先级
    /// 一个动作的基础优先级，优先级越高的动作越可能被选中
    /// </summary>
    public int priority;

    /// <summary>
    /// 是否翻转角色的朝向，在这个demo里面，角色面向是一个严肃的属性
    /// 并不是每个游戏的角色面向都是如此严肃的，具体看游戏设计
    /// 所以有些动作会改变角色的面向，他未必是转身动画，而是可能动作中转身、再转身
    /// 而转身到再转身之间的那段的cancel你输入招式就要反手搓，愚蠢的欧美人认为这就叫操作
    /// 在这个游戏里面，角色是会后退一段路然后转身的，这是一种动作游戏的风格
    /// 如果采用这种风格，最好有一些攻击动作是有转身版的，虽然玩家理解是同一个动作，比如kick
    /// 但实际上按住后再按kick和按kick本来就是两个动作了对吧，他们只是“大部分相似”而已
    /// </summary>
    public bool flip;
}

/// <summary>
/// 读取json用的
/// </summary>
[Serializable]
public struct ActionInfoContainer
{
    public ActionInfo[] data;
}

/// <summary>
/// 阶段性输入百分比
/// </summary>
[Serializable]
public struct MoveInputAcceptance
{
    /// <summary>
    /// 在百分比多少的阶段
    /// </summary>
    public PercentageRange range;
    /// <summary>
    /// 允许百分比
    /// </summary>
    public float rate;
}