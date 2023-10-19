using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameMain : MonoBehaviour
{
    /// <summary>
    /// 使用的主要相机
    /// </summary>
    public Camera usingCam;
    /// <summary>
    /// 玩家角色，其实应该是动态创建的，不过这个demo里面就偷个懒了
    /// </summary>
    public CharacterObj player;
    /// <summary>
    /// 敌人，也是偷懒的，正确的做法，应该是把所有人放在一个列表里统一处理
    /// 至少是走代码创建，而不是直接丢在场景里，然后拖到inspector
    /// </summary>
    public List<CharacterObj> enemy;
    
    void Awake()
    {
        //就暂时写在这里读取吧，demo自然就偷懒了
        GameData.Load();
        //同样是偷懒，把数据全塞给角色
        player.action.SetAllActions(GameData.AllActions(), "BoxingStand");
        foreach (CharacterObj e in enemy)
        {
            e.action.SetAllActions(GameData.AllActions(), "TurnBack");
        }
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        //处理攻击和碰撞
        DealWithAttacks();
        //先处理角色的移动
        player.transform.position += player.ThisTickMove(dt);
        foreach (CharacterObj ene in enemy)
        {
            ene.transform.position += ene.ThisTickMove(dt);
        }
    }

    private void LateUpdate()
    {
        Vector3 baseCamPos = new Vector3(2, 2.4f, -4);
        usingCam.transform.position = baseCamPos + player.transform.position;
    }

    /// <summary>
    /// 处理每个角色的本帧攻击
    /// </summary>
    private void DealWithAttacks()
    {
        foreach (CharacterObj ene in enemy)
        {
            //玩家对敌人
            if (player.CanAttackTargetNow(ene, out AttackInfo pAttackPhase, out BeHitBoxTurnOnInfo eDefenseInfo,
                    out AttackHitBox pAttackBox, out BeHitBox eneBox)) 
            {
                //先判断，如果hitRecord表示没法命中，那么这个框的信息无效
                HitRecord hRec = player.GetHitRecord(ene, pAttackPhase.phase);
                if (hRec == null || (hRec.Cooldown <= 0 && hRec.CanHitTimes > 0))
                {
                    DoAttack(player, ene, pAttackPhase, eDefenseInfo);
                }
            }
            //敌人对玩家
            if (ene.CanAttackTargetNow(player, out AttackInfo eAttackPhase, out BeHitBoxTurnOnInfo pDefenseInfo,
                    out AttackHitBox eAttackBox, out BeHitBox playerBox)) 
            {
                //先判断，如果hitRecord表示没法命中，那么这个框的信息无效
                HitRecord hRec = ene.GetHitRecord(player, eAttackPhase.phase);
                if (hRec == null || (hRec.Cooldown <= 0 && hRec.CanHitTimes > 0))
                {
                    DoAttack(ene, player, eAttackPhase, pDefenseInfo);
                }
            }
        }
    }

    /// <summary>
    /// 根据是否翻转获得方向
    /// </summary>
    /// <param name="dir"></param>
    /// <param name="inversed"></param>
    /// <returns></returns>
    private static ForceDirection GetForceDirection(ForceDirection dir, bool inversed)
    {
        if (!inversed) return dir;
        switch (dir)
        {
            case ForceDirection.Forward: return ForceDirection.Backward;
            case ForceDirection.Backward: return ForceDirection.Forward;
        }
        return dir;
    }
    
    /// <summary>
    /// 发动攻击
    /// </summary>
    /// <param name="attacker"></param>
    /// <param name="defender"></param>
    /// <param name="attackInfo"></param>
    /// <param name="defensePhase"></param>
    private void DoAttack(CharacterObj attacker, CharacterObj defender, AttackInfo attackInfo, BeHitBoxTurnOnInfo defensePhase)
    {
        //动作改变，各自动作各自占优，所以>=和>的区别就在这里了
        ActionChangeInfo attackerChange =
            attackInfo.selfActionChange.priority >= defensePhase.attackerActionChange.priority
                ? attackInfo.selfActionChange
                : defensePhase.attackerActionChange;
        ForceDirection attackerDir = GetForceDirection(attackInfo.forceDir, attacker.Inversed); 
        attacker.action.PreorderActionByActionChangeInfo(attackerChange, attackerDir);
        
        ActionChangeInfo defenderChange =
            attackInfo.targetActionChange.priority > defensePhase.selfActionChange.priority
                ? attackInfo.targetActionChange
                : defensePhase.selfActionChange;
        //两人相向则翻转，否则不翻转
        defender.action.PreorderActionByActionChangeInfo(defenderChange, attackerDir);

        // Debug.Log("defender preorder action : " + defenderChange.param + " > " + defenderChange.changeType + " > " +
        //           defenderChange.priority +
        //           defender.action.anim.GetCurrentAnimatorStateInfo(0).normalizedTime.ToString("P3"));
        
        //造成伤害
        //todo demo里就先不做了
        
        attacker.AddHitRecord(defender, attackInfo.phase);
    }
}
