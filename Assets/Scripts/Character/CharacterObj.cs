using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterObj : MonoBehaviour
{
    /// <summary>
    /// 动作管理
    /// </summary>
    public ActionController action;
    /// <summary>
    /// 输入系统，通过这个可以获得一部分移动信息
    /// </summary>
    public InputToCommand input;

    /// <summary>
    /// 重量，影响到下落速度，第一秒的时候下降weight，第1-2秒的时候下降就是2*weight了，以此类推
    /// 实际上动作游戏中是每帧增加一个重量，但是我们用的是Update，帧的概念是电影行业的，不是做游戏的
    /// 所以我们不得不拟合一个，按照每秒作为单位来用deltaTime做拟合
    /// </summary>
    public float weight = 4.5f;

    /// <summary>
    /// 是否正在下落
    /// </summary>
    private bool _falling = false;
    /// <summary>
    /// 下落的重量
    /// </summary>
    private float _curWeight = 0;

    /// <summary>
    /// 当前是否下降这个东西，并不是由角色自己说了算的，这是绝大多数游戏教程，也包括UE的游戏框架在内，完全做错的地方
    /// 因为是否下落取决于地形等游戏中的其他元素，不仅仅取决于角色自身，甚至几乎不取决于角色自己
    /// 而角色不应该依赖于其他这么多的元素，所以应该由“最小弟”的GameMain这一层来告诉我，我该不该下落
    /// </summary>
    public bool Falling
    {
        get => _falling;
        set
        {
            if (value && !_falling)
            {
                //得初始化一下当前重量         
                _curWeight = 0;
            }
            _falling = value;
        }
    }

    /// <summary>
    /// 碰撞盒的碰撞信息，现在这里缓存一下
    /// </summary>
    private Dictionary<AttackHitBox, List<BeHitBox>> _boxTouches = new Dictionary<AttackHitBox, List<BeHitBox>>();
    
    /// <summary>
    /// 当前动作的命中记录，更换动作就清空了这个列表了
    /// </summary>
    public List<HitRecord> HitRecords { get; private set; } = new List<HitRecord>();

    
    
    /// <summary>
    /// 被强制移动，最多只有一个被强制移动
    /// </summary>
    private ForceMove _forceMove = ForceMove.NoForceMove;

    private bool UnderForceMove => _forceMove.TimeElapsed <= _forceMove.Data.inSec;
    
    public bool Inversed
    {
        get => input.inversed;
        set => input.inversed = value;
    }

    /// <summary>
    /// 移动速度（米/秒）
    /// 这原本应该是角色属性中的一个，但是这个demo并不打算做数值部分，所以就暴露在外
    /// </summary>
    public float moveSpeed;

    private bool WishToMoveForward =>
        input.ActionOccur(new ActionCommand {keySequence = new KeyMap[] {KeyMap.Forward}});

    private bool WishToMoveBackward =>
        input.ActionOccur(new ActionCommand {keySequence = new KeyMap[] {KeyMap.Backward}});

    /// <summary>
    /// 自然的移动，而非ForceMoved
    /// </summary>
    public Vector3 NatureMove(float delta) => new Vector3(
        ((WishToMoveForward ? (moveSpeed * action.MoveInputAcceptance * delta) :
            WishToMoveBackward ? (-moveSpeed * action.MoveInputAcceptance * delta) : 0) + action.RootMotionMove.x) *
        (Inversed ? -1 : 1),
        action.RootMotionMove.y - _curWeight,   //unity的坐标系y是反过来的
        action.RootMotionMove.z
    );

    
    
    private void Awake()
    {
        //set一下components
        action.Set(((wasAction, toAction) =>
        {
            //更换动作时去掉所有命中记录
            HitRecords.Clear();
        }));
        
        //开始的时候初始化一下必要的Components
        Transform[] transforms = GetComponentsInChildren<Transform>();
        foreach (Transform trans in transforms)
        {
            AttackHitBox ahb = trans.GetComponent<AttackHitBox>();
            if (ahb) ahb.master = this;
            BeHitBox bhb = trans.GetComponent<BeHitBox>();
            if (bhb) bhb.master = this;
        }
    }

    private void Update()
    {
        float delta = Time.deltaTime;
        //以下内容只有不在硬直才会执行
        if (!action.Freezing)
        {
            //HitRecords
            foreach (HitRecord record in HitRecords)
                record.Update(delta);
        }
        
    }

    /// <summary>
    /// 攻击盒子当前是否激活？
    /// </summary>
    /// <param name="tags">攻击盒的tag</param>
    /// <returns></returns>
    public bool ShouldAttackBoxActive(string[] tags)
    {
        foreach (string s in tags)
            if (action.ActiveAttackBoxTag.Contains(s))
                return true;
        return false;
    }

    /// <summary>
    /// 挨打盒子当前是否激活？
    /// </summary>
    /// <param name="tags">受击盒的tag</param>
    /// <returns></returns>
    public bool ShouldBeHitBoxActive(string[] tags)
    {
        foreach (string s in tags)
            if (action.ActiveBeHitBoxTag.Contains(s))
                return true;
        return false;
    }

    /// <summary>
    /// 追加一条攻击框命中受击框的信息
    /// 这里可不管框active与否
    /// </summary>
    /// <param name="attackHitBox">攻击框</param>
    /// <param name="targetBox">受击框</param>
    public void OnAttackBoxHit(AttackHitBox attackHitBox, BeHitBox targetBox)
    {
        if (!_boxTouches.ContainsKey(attackHitBox))
            _boxTouches.Add(attackHitBox, new List<BeHitBox>());
        
        if (!_boxTouches[attackHitBox].Contains(targetBox))
            _boxTouches[attackHitBox].Add(targetBox);
    }

    /// <summary>
    /// 当攻击框脱离了受击框
    /// </summary>
    /// <param name="attackHitBox"></param>
    /// <param name="beHitBox"></param>
    /// <returns></returns>
    public void OnAttackBoxExit(AttackHitBox attackHitBox, BeHitBox beHitBox)
    {
        if (!_boxTouches.ContainsKey(attackHitBox)) return;
        _boxTouches[attackHitBox].Remove(beHitBox);
    }

    private BeHitBoxTurnOnInfo GetDefensePhaseByBeHitBox(BeHitBox box)
    {
        foreach (string boxTag in box.tags)
        {
            foreach (BeHitBoxTurnOnInfo info in action.ActiveBeHitBoxInfo)
            {
                foreach (string infoTag in info.tag)
                {
                    if (infoTag == boxTag)
                        return info;
                }
            }
        }
        return new BeHitBoxTurnOnInfo();
    }

    /// <summary>
    /// 现在是否能殴打某个人，这里不判断HitRecord
    /// </summary>
    /// <param name="target">谁挨打？</param>
    /// <param name="attackPhase">这是攻击阶段信息</param>
    /// <param name="defensePhase">受击的时候的阶段信息</param>
    /// <param name="attackBox">命中的是哪个攻击框</param>
    /// <param name="targetBox">目标的那个受击框被命中</param>
    /// <returns></returns>
    public bool CanAttackTargetNow(CharacterObj target, out AttackInfo attackPhase, out BeHitBoxTurnOnInfo defensePhase, out AttackHitBox attackBox, out BeHitBox targetBox)
    {
        attackBox = null;
        targetBox = null;
        attackPhase = new AttackInfo();
        
        defensePhase = new BeHitBoxTurnOnInfo();
        if (!target) return false;
        
        //目标开启的受击盒
        List<string> activeBoxTag = new List<string>();
        foreach (BeHitBoxTurnOnInfo info in target.action.ActiveBeHitBoxInfo)
        {
            foreach (string t in info.tag)
            {
                if (!activeBoxTag.Contains(t)) activeBoxTag.Add(t);
            }
        }
        
        //命中对方的所有攻击框
        foreach (AttackBoxTurnOnInfo boxInfo in action.ActiveAttackBoxInfo)
        {
            foreach (KeyValuePair<AttackHitBox,List<BeHitBox>> touch in _boxTouches)
            {
                //没有启动的攻击框不会判断命中
                if (!touch.Key.Active) continue;
                
                //命中的最有价值的受击框才行
                BeHitBox best = null;
                int bestPriority = 0;
                foreach (BeHitBox hitBox in touch.Value)
                {
                    if (!hitBox.Active || hitBox.master != target || !hitBox.TagHit(activeBoxTag)) continue;
                    BeHitBoxTurnOnInfo info = GetDefensePhaseByBeHitBox(hitBox);
                    int thisPriority = hitBox.Priority + info.priority;
                    if (!best || thisPriority > bestPriority)
                    {
                        best = hitBox;
                        bestPriority = thisPriority;
                        defensePhase = info;
                    }
                }
                //一个没找到，当然就……
                if (!best) continue;
                
                //就不管攻击框了，本来应该先判断攻击框的，其实也无所谓的
                attackBox = touch.Key;
                targetBox = best;
                if (boxInfo.attackPhase >= 0 && boxInfo.attackPhase < action.CurrentAction.attacks.Length)
                    attackPhase = action.CurrentAction.attacks[boxInfo.attackPhase];
                return true;
            }
        }

        return false;
    }
    

    /// <summary>
    /// 攻击框命中了对手哪些受击框，一个也没的话就会是空数组了
    /// </summary>
    /// <param name="attackBox">攻击框</param>
    /// <param name="target">对手</param>
    /// <returns></returns>
    private List<BeHitBox> AttackBoxHitTargetIn(AttackHitBox attackBox, CharacterObj target)
    {
        List<BeHitBox> res = new List<BeHitBox>();
        if (!_boxTouches.ContainsKey(attackBox)) return res;
        foreach (BeHitBox hitBox in _boxTouches[attackBox])
        {
            if (hitBox.master == target)
                res.Add(hitBox);
        }
        return res;
    }
    
    /// <summary>
    /// 添加命中记录
    /// </summary>
    /// <param name="target">谁被命中</param>
    /// <param name="attackPhase">算是第几阶段的攻击命中的</param>
    /// <returns></returns>
    public void AddHitRecord(CharacterObj target, int attackPhase)
    {
        int idx = action.IndexOfAttack(attackPhase);
        if (idx < 0) return;    //没有这个伤害阶段，结束
        
        HitRecord rec = GetHitRecord(target, attackPhase);
        if (rec == null)
        {
            HitRecords.Add(new HitRecord(target, attackPhase, action.CurrentAction.attacks[idx].canHitSameTarget - 1,
                action.CurrentAction.attacks[idx].hitSameTargetDelay));
        }
        else
        {
            rec.Cooldown = action.CurrentAction.attacks[idx].hitSameTargetDelay;
            rec.CanHitTimes -= 1;
        }
    }
    
    /// <summary>
    /// 找出关于目标的HitRecord，如果没有就是null
    /// </summary>
    /// <param name="target">谁被命中</param>
    /// <param name="phase">算是第几阶段的攻击命中的</param>
    /// <returns></returns>
    public HitRecord GetHitRecord(CharacterObj target, int phase)
    {
        foreach (HitRecord record in HitRecords)
        {
            if (record.UniqueId == target.gameObject.GetInstanceID() && record.Phase == phase)
                return record;
        }

        return null;
    }
    
    /// <summary>
    /// 这一帧的移动 todo 目前只有NatureMove，还没有Forced
    /// </summary>
    public Vector3 ThisTickMove(float delta)
    {
        //硬直时候不做移动
        if (action.Freezing) return Vector3.zero;
        //有强制位移有限强制位移
        if (UnderForceMove)
        {
            Vector3 fMove = _forceMove.MoveTween(_forceMove);
            _forceMove.Update(delta);
            return fMove;
        }
        //下落重量的增幅，不应该在Update做，而是外部调用update的地方去做
        if (_falling) _curWeight += weight * delta;
        else _curWeight = 0;
        //有强制位移的时候强制位移，没有的时候自然移动
        return  NatureMove(delta);
    }

    /// <summary>
    /// 设置强制移动，只接受最新的一个
    /// </summary>
    /// <param name="force"></param>
    public void SetForceMove(MoveInfo force)
    {
        _forceMove = ForceMove.FromData(force);
    }
}
