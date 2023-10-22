using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 动作管理器，是一个核心组件
/// </summary>
public class ActionController : MonoBehaviour
{
    /// <summary>
    /// 角色的animator，要通过这个来播放角色动作的
    /// </summary>
    [Tooltip("角色的animator")] public Animator anim;

    /// <summary>
    /// 即使不是玩家控制，也可以有这个组件，ai也可以通过发送操作指令来驱动角色
    /// 尽管ai有aiCommand这个组件
    /// </summary>
    [Tooltip("指令输入的input")] public InputToCommand command;
    
    /// <summary>
    /// 当前正在做的动作的信息
    /// </summary>
    public ActionInfo CurrentAction { get; private set; }

    /// <summary>
    /// 当前激活的BeCancelledTag
    /// </summary>
    public List<BeCancelledTag> CurrentBeCancelledTag { get; private set; } = new List<BeCancelledTag>();

    /// <summary>
    /// 角色所有会的动作
    /// </summary>
    public List<ActionInfo> AllActions { get; private set; } = new List<ActionInfo>();

    /// <summary>
    /// 当前帧的动画切换请求，如果一个也没有，则会继续当前的动作
    /// </summary>
    private List<PreorderActionInfo> _preorderActions = new List<PreorderActionInfo>();

    /// <summary>
    /// 这个动作在上一个update经历了多少百分比了
    /// </summary>
    private float _wasPercentage = 0;

    /// <summary>
    /// 当前动作的RootMotion方法
    /// 参数float：当前动作进行到的百分比
    /// 参数string[]：配置在ActionInfo表里actionInfo.rootMotionTween的param部分
    /// 返回值：Vector3，偏移量，假设起始的时候坐标为zero，到normalized==参数float的时候，当时的偏移值
    /// </summary>
    private ScriptMethodInfo _rootMotion = new ScriptMethodInfo();

    /// <summary>
    /// 当前激活的攻击盒tag
    /// 这算是一个内存换芯片，就是我先每帧算好了储存下来哪些框开启
    /// </summary>
    public List<string> ActiveAttackBoxTag { get; private set; } = new List<string>();
    //同上，只是储存的是具体信息
    public List<AttackBoxTurnOnInfo> ActiveAttackBoxInfo { get; private set; } = new List<AttackBoxTurnOnInfo>();

    /// <summary>
    /// 当前帧的移动速度百分比
    /// </summary>
    public float MoveInputAcceptance { get; private set; } = 0;
    
    /// <summary>
    /// 当前激活的受击盒tag
    /// </summary>
    public List<string> ActiveBeHitBoxTag { get; private set; } = new List<string>();
    public List<BeHitBoxTurnOnInfo> ActiveBeHitBoxInfo { get; private set; } = new List<BeHitBoxTurnOnInfo>();
    
    /// <summary>
    /// 当前帧的RootMotion信息
    /// </summary>
    public Vector3 RootMotionMove { get; private set; } = Vector3.zero;

    /// <summary>
    /// 硬直（卡帧）时间
    /// </summary>
    private float _freezing = 0;
    /// <summary>
    /// 是否在硬直或者卡帧
    /// </summary>
    public bool Freezing => _freezing > 0;

    /// <summary>
    /// 更换动作的时候的回调函数
    /// 参数1：ActionInfo：更换之前的action
    /// 参数2：ActionInfo：更换之后的action
    /// 只有在ChangeAction时才会调用
    /// </summary>
    private Action<ActionInfo, ActionInfo> _onChangeAction = null;

    /// <summary>
    /// 当前动画百分比，放这里方便些，其实最好放一个函数里
    /// 但是因为要访问动画百分比的频率较高，不如就一次性了
    /// </summary>
    private float _pec = 0;
    
    /// <summary>
    /// 之所以我们都用Update而不是Fixed，因为我们要依赖的核心是Input和Animator
    /// 用Update做动作游戏会有很多问题，比如跳过了可以Cancel的帧
    /// 但是无奈，毕竟用了unity
    /// </summary>
    private void Update()
    {
        float delta = Time.deltaTime;
        //没有动画就不会工作
        if (AllActions.Count <= 0) return;
        
        //根据硬直来调整倍率
        anim.speed = Freezing ? 0 : 1;
        if (_freezing > 0) _freezing -= delta;
        
        //因为动作融合，所以我们优先取下一个动作的normalized当做百分比进度
        AnimatorStateInfo asInfo = anim.GetCurrentAnimatorStateInfo(0);
        AnimatorStateInfo nextStateInfo = anim.GetNextAnimatorStateInfo(0);
        //获得现在的百分比时间，因为会大于等于100%（你敢信，这就是Unity这个愚蠢做法的无奈之处）所以要clamp一下
        _pec = Mathf.Clamp01(nextStateInfo.length > 0 ? nextStateInfo.normalizedTime : asInfo.normalizedTime);
        
        //算一下攻击盒跟受击盒
        CalculateBoxInfo(_wasPercentage, _pec);
        
        //移动输入接受
        CalculateInputAcceptance(_wasPercentage, _pec);
        
        //算一下2帧之间的RootMotion变化
        if (!String.IsNullOrEmpty(_rootMotion.method) && RootMotionMethod.Methods.ContainsKey(_rootMotion.method))
        {
            Vector3 rmThisTick = RootMotionMethod.Methods[_rootMotion.method](_pec, _rootMotion.param);
            Vector3 rmLastTick = RootMotionMethod.Methods[_rootMotion.method](_wasPercentage, _rootMotion.param);
            RootMotionMove = rmThisTick - rmLastTick;
            //Debug.Log("RootMotion distance " + RootMotionMove + "=>" + pec + " - " + _wasPercentage);
        }else RootMotionMove = Vector3.zero;
        
        //动作是否要更换了，最终我们为了偷懒，而妥协了引擎的依赖于动画的思路了
        //当然，这只是因为这是个demo，如果正式开发游戏，就要斟酌一下，毕竟自己写一个，可以手感提高不少
        //但是手感的提高，玩家是未必能体验出来的
        //开始观察每个动作，如果他们可以cancel当前动作，并且操作存在，那么就会添加到预约列表里面
        foreach (ActionInfo action in AllActions)
        {
            if (CanActionCancelCurrent(action, _pec, true, out BeCancelledTag bcTag, out CancelTag cancelTag))
            {
                _preorderActions.Add(new PreorderActionInfo(action.id, bcTag.priority + cancelTag.priority + action.priority,
                    Mathf.Min(bcTag.fadeOutPercentage, cancelTag.fadeInPercentage), cancelTag.startFromPercentage));
            }
        }
        
        //如果要更换了就预约下一个动作
        if (_preorderActions.Count <= 0 && (_pec >= 1 || CurrentAction.autoTerminate))
        {
            _preorderActions.Add(new PreorderActionInfo(CurrentAction.autoNextActionId));
        }
        
        //冒泡所有的候选动作，得出应该切换的动作
        _wasPercentage = _pec;   //先设置这个，之后可能会被ChangeAction所改变
        if (_preorderActions.Count > 0)
        {
            //有需要更换的动画就更换
            _preorderActions.Sort(
                (candidate1, candidate2) => candidate1.Priority > candidate2.Priority ? -1 : 1
                );
            if (_preorderActions[0].ActionId == CurrentAction.id && CurrentAction.keepPlayingAnim)
                KeepAction(_pec);
            else
                ChangeAction(_preorderActions[0].ActionId, _preorderActions[0].TransitionNormalized,
                    _preorderActions[0].FromNormalized, _preorderActions[0].FreezingAfterChangeAction);
        }
        
        //清理一下预约列表
        _preorderActions.Clear();
    }

    /// <summary>
    /// 更换动作的回调函数
    /// </summary>
    /// <param name="onActionChanged"></param>
    public void Set(Action<ActionInfo, ActionInfo> onActionChanged)
    {
        _onChangeAction = onActionChanged;
    }

    public void CalculateInputAcceptance(float wasPec, float pec)
    {
        MoveInputAcceptance = 0;
        if (CurrentAction.inputAcceptance == null) return;
        foreach (MoveInputAcceptance acceptance in CurrentAction.inputAcceptance)
        {
            if (acceptance.range.min <= pec && acceptance.range.max >= wasPec &&
                (MoveInputAcceptance <= 0 || acceptance.rate < MoveInputAcceptance))
                MoveInputAcceptance = acceptance.rate;
        }
    }

    /// <summary>
    /// 计算当前动画帧的信息
    /// </summary>
    /// <param name="wasPec">上一帧的百分比</param>
    /// <param name="pec">百分比进度</param>
    private void CalculateBoxInfo(float wasPec, float pec)
    {
        ActiveAttackBoxInfo.Clear();
        ActiveAttackBoxTag.Clear();
        foreach (AttackBoxTurnOnInfo aBox in CurrentAction.attackPhase)
        {
            bool open = false;
            foreach (PercentageRange range in aBox.inPercentage)
            {
                if (pec >= range.min && wasPec <= range.max)
                {
                    open = true;
                    break;
                }
            }

            if (open)
            {
                foreach (string aTag in aBox.tag)
                    if (!ActiveAttackBoxTag.Contains(aTag))
                        ActiveAttackBoxTag.Add(aTag);
                ActiveAttackBoxInfo.Add(aBox);
            }
        }
        
        ActiveBeHitBoxInfo.Clear();
        ActiveBeHitBoxTag.Clear();
        foreach (BeHitBoxTurnOnInfo bHitBox in CurrentAction.defensePhase)
        {
            bool open = false;
            foreach (PercentageRange range in bHitBox.inPercentage)
            {
                if (pec >= range.min && pec <= range.max)
                {
                    open = true;
                    break;
                }
            }

            if (open)
            {
                foreach (string bTag in bHitBox.tag)
                    if (!ActiveBeHitBoxTag.Contains(bTag))
                        ActiveBeHitBoxTag.Add(bTag);
                ActiveBeHitBoxInfo.Add(bHitBox);
            }
        }
    }

    /// <summary>
    /// 在当前的情况下，是否能Cancel掉CurrentAction
    /// </summary>
    /// <param name="action">动画</param>
    /// <param name="currentPercentage">当前动画播放到了百分之多少</param>
    /// <param name="checkCommand">是否检查输入，true代表要输入也合法才行</param>
    /// <param name="beCancelledTag">符合条件的BeCancelledTag</param>
    /// <param name="foundTag">符合条件的CancelTag</param>
    /// <returns></returns>
    private bool CanActionCancelCurrent(ActionInfo action, float currentPercentage, bool checkCommand, out BeCancelledTag beCancelledTag, out CancelTag foundTag)
    {
        foundTag = new CancelTag();
        beCancelledTag = new BeCancelledTag();
        foreach (BeCancelledTag bcTag in CurrentBeCancelledTag)
        {
            //百分比时间符合的情况下，才可能有效
            if (!(bcTag.percentageRange.max >= _wasPercentage && bcTag.percentageRange.min <= currentPercentage)) continue;
            
            //判断CancelTag是否有交集，没有交集，说明也不能cancel
            bool tagFit = false;
            foreach (string cTag in bcTag.cancelTag)
            {
                foreach (CancelTag cancelTag in action.cancelTag)
                {
                    if (cancelTag.tag == cTag)
                    {
                        tagFit = true;
                        beCancelledTag = bcTag;
                        foundTag = cancelTag;
                        break;
                    }
                }

                if (tagFit) break;
            }
            if (!tagFit) continue;
            
            //检查输入
            if (checkCommand)
            {
                foreach (ActionCommand ac in action.commands)
                {
                    //任何一条操作符合，就算符合
                    if (command.ActionOccur(ac)) return true;
                }
            }
            else return true;   //不检查输入，到这里就直接符合了
        }

        return false;   //很遗憾，找不到
    }

    /// <summary>
    /// 更换到某个action
    /// </summary>
    /// <param name="actionId">目标actionId</param>
    /// <param name="transitionNormalized">融合百分比时间</param>
    /// <param name="fromNormalized">从百分之多少开始播放新的动画</param>
    /// <param name="freezingAfterChange">切换动作后，硬直多少秒</param>
    private void ChangeAction(string actionId, float transitionNormalized, float fromNormalized, float freezingAfterChange)
    {
        ActionInfo aInfo = GetActionById(actionId, out bool foundAction);
        if (foundAction)
        {
            //清除掉非方向操作，连招手感得这么保障，当然刻意为了更容易连招，可以去掉这个
            command.CleanNonDirectionInputs();
            
            _onChangeAction?.Invoke(CurrentAction, aInfo);
            anim.CrossFade(aInfo.animKey, transitionNormalized, 0, fromNormalized);
            CurrentAction = aInfo;
            //默认的cancelTag都可以加上
            CurrentBeCancelledTag.Clear();
            foreach (BeCancelledTag beCancelledTag in aInfo.beCancelledTag)
            {
                CurrentBeCancelledTag.Add(beCancelledTag);
            }

            _freezing = freezingAfterChange;
            
            ActiveBeHitBoxInfo.Clear();
            ActiveBeHitBoxTag.Clear();
            ActiveAttackBoxTag.Clear();
            ActiveAttackBoxInfo.Clear();
            
            _rootMotion = aInfo.rootMotionTween;
            
            _wasPercentage = fromNormalized;
            //顺便修一下面向
            transform.eulerAngles = new Vector3(0, command.inversed ? 270 : 90, 0);
            //修正完毕才接受新的是否要转向，因为可能这个动作本身自带转向
            if (aInfo.flip) command.inversed = !command.inversed;
            
        }
    }

    /// <summary>
    /// 保持继续播放动作
    /// 为什么这也需要一个专门的函数？因为要处理循环问题
    /// </summary>
    /// <param name="currentNormalized"></param>
    private void KeepAction(float currentNormalized)
    {
        if (currentNormalized >= 1)
        {
            anim.CrossFade(CurrentAction.animKey, 0, 0, 0);
        }
            
    }

    /// <summary>
    /// 从allActions(已经学会的动作)中抽出第一个id符合条件的动作，如果没有，就会返回当前的动作
    /// </summary>
    /// <param name="actionId"></param>
    /// <param name="found">是否找到了合适的</param>
    /// <returns></returns>
    private ActionInfo GetActionById(string actionId, out bool found)
    {
        found = false;
        foreach (ActionInfo action in AllActions)
        {
            if (action.id == actionId)
            {
                found = true;
                return action;
            }
        }

        return CurrentAction;
    }

    public int IndexOfAttack(int attackPhase)
    {
        for (int i = 0; i < CurrentAction.attacks.Length; i++)
        {
            if (CurrentAction.attacks[i].phase == attackPhase)
                return i;
        }

        return -1;
    }

    /// <summary>
    /// 初始化：设置所有的动作
    /// </summary>
    /// <param name="actions"></param>
    /// <param name="defaultActionId"></param>
    public void SetAllActions(List<ActionInfo> actions, string defaultActionId)
    {
        AllActions.Clear();
        if (actions != null) AllActions = actions;
        ChangeAction(defaultActionId, 0, 0, 0);
    }

    /// <summary>
    /// 预约一个动作
    /// </summary>
    /// <param name="acInfo">变换动作信息</param>
    /// <param name="forceDir">如有必要（其实就是byCatalog）得给个动作受力方向</param>
    /// <param name="freezing">如果切换到这个动作，硬直多少秒</param>
    public void PreorderActionByActionChangeInfo(ActionChangeInfo acInfo, ForceDirection forceDir, float freezing = 0)
    {
        switch (acInfo.changeType)
        {
            case ActionChangeType.Keep: 
                //既然保持，就啥也不做了
                break;
            case ActionChangeType.ChangeByCatalog:
                List<ActionInfo> actions = new List<ActionInfo>();
                foreach (ActionInfo info in AllActions)
                    if (info.catalog == acInfo.param)
                        actions.Add(info);
                if (actions.Count > 0)
                {
                    ActionInfo picked = actions[0];
                    //如果有策划设计的脚本，那就走脚本拿到数据
                    if (PickActionMethod.Methods.ContainsKey(acInfo.param))
                    {
                        picked = PickActionMethod.Methods[acInfo.param](actions, forceDir);
                    }
                    _preorderActions.Add(new PreorderActionInfo
                    {
                        ActionId = picked.id,
                        FromNormalized = acInfo.fromNormalized,
                        Priority = acInfo.priority + picked.priority,
                        TransitionNormalized = acInfo.transNormalized,
                        FreezingAfterChangeAction = freezing
                    });
                }
                break;
            case ActionChangeType.ChangeToActionId:
                //找到对应id的动作，如果有的话
                ActionInfo aInfo = GetActionById(acInfo.param, out bool found);
                if (found)
                {
                    _preorderActions.Add(new PreorderActionInfo
                    {
                        ActionId = aInfo.id,
                        FromNormalized = acInfo.fromNormalized,
                        Priority = acInfo.priority + aInfo.priority,
                        TransitionNormalized = acInfo.transNormalized,
                        FreezingAfterChangeAction = freezing
                    });
                }
                break;
        }
    }

    /// <summary>
    /// 加入卡帧，卡帧会叠加，但是最多不会超过一个值，并且越接近的时候增加量越少
    /// 注意，这只能是卡帧freezing，因为他会立即暂停角色动作，而受击的hitStun是在切换动作之后，切勿走这里
    /// </summary>
    /// <param name="freezingSec"></param>
    public void SetFreezing(float freezingSec)
    {
        if (_freezing < 0) _freezing = 0;   //清理一下
        float maxFreezing = 0.5f;   //卡帧、硬直上限
        float addRate = Mathf.Clamp(maxFreezing - _freezing, 0, maxFreezing) / maxFreezing;
        _freezing += freezingSec * addRate;
        anim.speed = Freezing ? 0 : 1;
    }

    /// <summary>
    /// 开启临时的CancelTag
    /// </summary>
    /// <param name="beCancelledTag"></param>
    public void AddTempBeCancelledTag(TempBeCancelledTag beCancelledTag)
    {
        CurrentBeCancelledTag.Add(BeCancelledTag.FromTemp(beCancelledTag, _pec));
    }

    /// <summary>
    /// 根据TempBeCancelledTag的id来开启
    /// </summary>
    /// <param name="tempTagId"></param>
    public void AddTempBeCancelledTag(string tempTagId)
    {
        foreach (TempBeCancelledTag beCancelledTag in CurrentAction.tempBeCancelledTag)
        {
            if (beCancelledTag.id == tempTagId)
            {
                AddTempBeCancelledTag(beCancelledTag);
                return;
            }
        }
    }
    
    
}
