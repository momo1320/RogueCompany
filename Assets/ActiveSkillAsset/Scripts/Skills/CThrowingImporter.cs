﻿using System.Collections;
using System.Collections.Generic;
using BT;
using UnityEngine;

[CreateAssetMenu(fileName = "CThrowingImporter", menuName = "SkillData/CThrowingImporter")]
public class CThrowingImporter : SkillData
{
    [Space(30)]

    [SerializeField]
    float speed, acceleration;
    [SerializeField]
    string animName;
    [SerializeField]
    StatusEffectInfo statusEffectInfo;
    [SerializeField]
    List<SkillData> skillData;
    [Header("오브젝트에 붙어서 호출되는 파티클")]
    [SerializeField]
    string attachedParticleName;
    [Header("계속해서 글로벌 호출되는 파티클")]
    [SerializeField]
    string particleName;
    [SerializeField]
    float particleTerm;
    [SerializeField]
    bool isDestroy;
    [SerializeField]
    bool randomDest;


    public override State Run(CustomObject customObject, Vector3 pos, ref float lapsedTime)
    {
        if (State.FAILURE == base.Run(customObject, pos, ref lapsedTime))
            return State.FAILURE;
        return Run(customObject.objectPosition, customObject.objectPosition + Random.insideUnitSphere * speed);
    }
    public override State Run(Character caster, Vector3 pos, ref float lapsedTime)
    {
        if (State.FAILURE == base.Run(caster, pos, ref lapsedTime))
            return State.FAILURE;
        return Run(caster.GetPosition(), caster.GetPosition() + caster.GetDirVector() * speed);
    }
    public override State Run(Character caster, Character other, Vector3 pos, ref float lapsedTime)
    {
        if (State.FAILURE == base.Run(caster, other, pos, ref lapsedTime))
            return State.FAILURE;
        return Run(caster.GetPosition(), other.GetPosition());
    }

    private BT.State Run(Vector3 srcPos, Vector3 destPos)
    {
        if (!(caster || other || customObject) || amount < 0)
        {
            return BT.State.FAILURE;
        }
        float nspeed = speed;
        if (randomDest)
        {
            destPos = RoomManager.Instance.GetCurrentRoomAvailableArea();
            nspeed += speed *.1f * Random.Range(-5, 5);
        }
        GameObject gameObject = ResourceManager.Instance.skillPool.GetPooledObject();
        gameObject.transform.position = srcPos;
        ProjectileSkillObject skillObject = gameObject.AddComponent<ProjectileSkillObject>();
        skillObject.SetSkillData(null, skillData);
        if (other)
            skillObject.Init(other);
        skillObject.Init(ref caster, this, time);
        skillObject.Set(statusEffectInfo);
        skillObject.Set(animName, nspeed, acceleration, destPos - srcPos);
        skillObject.Set(attachedParticleName);
        skillObject.Set(particleName, particleTerm);
        skillObject.Set(isDestroy, true);
        return BT.State.SUCCESS;
    }

}
