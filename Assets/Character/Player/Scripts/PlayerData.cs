﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * 0630 0100 - 강의헌
 * 모든 캐릭터 최대 체력 15개 통일, hpMax = 15로 일단 통일
 * 허나 시작할 때 체력 모두 다름.(= 시작 시 초반에 주워지는 현재 체력 캐릭터 마다 각각 다름)
 * 현재는 패시브로 최대 체력 늘어나는 패시브 아예 안 만듬.
 * 최대 체력이 15라는걸 알리려는게 따로 없는 것 같음.
 * 체력 최대체력일 때 회복 물약 안 먹어지게 처리
 */ 

 // armor 쉴드 개념 있는 아이템들 있어서 아예 player 데미지 입는 공식
 // 데미지 - 방어력으로 하고 저런 아이템 먹으면 방어력 수치 잠깐 올렸다가 내릴려고 함.

[System.Serializable]
[CreateAssetMenu(fileName = "PlayerData", menuName = "PlayerData")]
public class PlayerData : ScriptableObject
{
    #region variables

    [SerializeField]
    private Player.PlayerType playerType;
    [SerializeField]
    private Sprite playerSprite;

    [SerializeField]
    private float hp;
    [SerializeField]
    private float hpMax;
    [SerializeField]
    private float moveSpeed;
    [SerializeField]
    private int stamina;
    [SerializeField]
    private int staminaMax;
    [SerializeField]
    private int skillGauge;
    [SerializeField]
    private int skillGaugeMax;
    [SerializeField]
    private float criticalChance;
    [SerializeField]
    private WeaponInfo[] startingWeaponInfos;
    [SerializeField]
    private SkillData skillData;

    private int shield;

    // on / off bool;
    private bool canDrainHp;
    private bool cannotDamagedWhenFalling;
    #endregion

    #region get/set Property

    public float Hp
    {
        get { return hp; }
        set { hp = value; }
    }
    public float HpMax
    {
        get { return hpMax; }
        set { hpMax = value; }
    }
    public float MoveSpeed
    {
        get { return moveSpeed; }
        set { moveSpeed = value; }
    }
    public int Stamina
    {
        get { return stamina; }
        set { stamina = value; }
    }
    public int StaminaMax
    {
        get { return staminaMax; }
        set { staminaMax = value; }
    }

    public int SkillGauge
    {
        get { return skillGauge; }
        set { skillGauge = value; }
    }
    public int SkillGaugeMax
    {
        get { return skillGaugeMax; }
        set { skillGaugeMax = value; }
    }

    public int Shield
    {
        get { return shield; }
        set { shield = value; }
    }
    public float CriticalChance
    {
        get { return criticalChance; }
        set { criticalChance = value; }
    }
    public WeaponInfo[] StartingWeaponInfos
    {
        get { return startingWeaponInfos; }
    }
    public SkillData SkillData
    {
        get
        {
            return skillData;
        }
    }
    #endregion

    public PlayerData Clone()
    {
        PlayerData clonedInfo = CreateInstance<PlayerData>();

        clonedInfo.playerType = playerType;
        clonedInfo.playerSprite = playerSprite;
        clonedInfo.hp = hp;
        clonedInfo.hpMax = hpMax;
        clonedInfo.moveSpeed = moveSpeed;
        clonedInfo.stamina = stamina;
        clonedInfo.staminaMax = staminaMax;
        clonedInfo.skillGauge = skillGauge;
        clonedInfo.skillGaugeMax = skillGaugeMax;
        clonedInfo.shield = shield;
        clonedInfo.criticalChance = criticalChance;
        clonedInfo.startingWeaponInfos = startingWeaponInfos;

        clonedInfo.canDrainHp = canDrainHp;
        clonedInfo.cannotDamagedWhenFalling = cannotDamagedWhenFalling;

        clonedInfo.skillData = skillData;
        return clonedInfo;
    }
}