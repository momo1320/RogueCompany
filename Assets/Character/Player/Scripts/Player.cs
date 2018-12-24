﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WeaponAsset;

public class Player : Character
{
    #region components
    [SerializeField]
    protected SpriteRenderer headRenderer;
    #endregion
    #region variables
    public enum PlayerType { SOCCER, MUSIC, FISH, ARMY }

    [SerializeField]
    private PlayerController controller;    // 플레이어 컨트롤 관련 클래스

    private Transform objTransform;

    private RaycastHit2D hit;
    private List<RaycasthitEnemy> raycastHitEnemies;
    private RaycasthitEnemy raycasthitEnemyInfo;
    private int layerMask;  // autoAim을 위한 layerMask
    private int killedEnemyCount;

    [SerializeField] private PlayerHpbarUI PlayerHPUi;
    private PlayerData playerData;
    private PlayerData originPlayerData;    // 아이템 효과 적용시 기준이 되는 정보

    private float skillGageMultiple;
    private float steminaGageMultiple;

    // 윤아 0802
    private Stamina stamina;

    private float floorSpeed;
    private int shieldCount;

    [SerializeField]
    private GameObject muzzlePosObj;
    [SerializeField]
    private Transform muzzlePosTransform;

    private SkillData skillData; 
    #endregion

    #region property
    public PlayerData PlayerData
    {
        get
        {
            return playerData;
        }
        set
        {
            playerData = value;
        }
    }
    public int KilledEnemyCount
    {
        get
        {
            return killedEnemyCount;
        }
    }
    public int ShieldCount
    {
        private set { shieldCount = value; }
        get { return shieldCount; }
    }

    public bool IsNotConsumeStamina
    {
        get; private set;
    }
    public bool IsNotConsumeAmmo
    {
        get; private set;
    }

    #endregion

    #region getter
    public PlayerController PlayerController
    {
        get
        {
            return controller;
        }
    }
    #endregion

    #region setter
    public void SetInFloor()
    {
        floorSpeed = .5f;
    }
    public void SetInRoom()
    {
        floorSpeed = 0;
    }
    #endregion

    #region UnityFunc
    void Awake()
    {
        objTransform = GetComponent<Transform>();
        scaleVector = new Vector3(1f, 1f, 1f);
        isRightDirection = true;
        raycastHitEnemies = new List<RaycasthitEnemy>();
        raycasthitEnemyInfo = new RaycasthitEnemy();
        layerMask = 1 << LayerMask.NameToLayer("TransparentFX");
        floorSpeed = 0;

        isAbnormalStatuses = new bool[(int)AbnormalStatusType.END];
        abnormalStatusCounts = new int[(int)AbnormalStatusType.END];
        overlappingCounts = new int[(int)AbnormalStatusType.END];
        abnormalStatusCoroutines = new Coroutine[(int)AbnormalStatusType.END];
        abnormalStatusTime = new float[(int)AbnormalStatusType.END];
        abnormalStatusDurationTime = new float[(int)AbnormalStatusType.END];
    }

    // Update is called once per frame
    void Update()
    {
        /*
        if(false == e && Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("무기 장착");
            e = true;
            // weaponManager 초기화, 바라보는 방향 각도, 방향 벡터함수 넘기기 위해서 해줘야됨
            weaponManager.Init(this, OwnerType.Player);
        }
        */

        // 총구 방향(각도)에 따른 player 우측 혹은 좌측 바라 볼 때 반전되어야 할 object(sprite는 여기서, weaponManager는 스스로 함) scale 조정
        if (Input.GetKeyDown(KeyCode.Q))
            ActiveSkill();
        if (Input.GetKeyDown(KeyCode.LeftShift))
            Evade();
        ChargeSkill();
        spriteRenderer.sortingOrder = -Mathf.RoundToInt(bodyTransform.position.y * 100);
        headRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;
        if (isEvade)
            return;
        if (-90 <= directionDegree && directionDegree < 90)
        {
            isRightDirection = true;
            scaleVector.x = 1f;
            spriteTransform.localScale = scaleVector;
        }
        else
        {
            isRightDirection = false;
            scaleVector.x = -1f;
            spriteTransform.localScale = scaleVector;
        }
    }

    void FixedUpdate()
    {
        Move();
    }
    #endregion

    #region initialzation

    void InitilizeController()
    {
        ControllerUI.Instance.SetPlayer(this, ref controller);
    }

    public override void Init()
    {
        base.Init();
        baseColor = Color.white;
        pState = CharacterInfo.State.ALIVE;
        ownerType = CharacterInfo.OwnerType.Player;
        damageImmune = CharacterInfo.DamageImmune.NONE;
        abnormalImmune = CharacterInfo.AbnormalImmune.NONE;

        animationHandler.Init(this, PlayerManager.Instance.runtimeAnimator);

        IsNotConsumeStamina = false;
        IsNotConsumeAmmo = false;
        isActiveMove = true;
        isActiveAttack = true;

        DeactivateAbnormalComponents();
        directionVector = new Vector3(1, 0, 0);
        shieldCount = 0;
        evadeCoolTime = 0.05f;
        battleSpeed = 0.5f;
        InitilizeController();

        PlayerHPUi = GameObject.Find("HPbar").GetComponent<PlayerHpbarUI>();
        buffManager = PlayerBuffManager.Instance.BuffManager;
        buffManager.SetOwner(this);
        stamina = Stamina.Instance;
        stamina.SetPlayer(this);

        // weaponManager 초기화, 바라보는 방향 각도, 방향 벡터함수 넘기기 위해서 해줘야됨
        weaponManager.Init(this, CharacterInfo.OwnerType.Player);

        TimeController.Instance.PlayStart();

        DeactivateAbnormalComponents();
        InitStatusEffects();
    }

    public void InitPlayerData(PlayerData playerData)
    {
        skillGageMultiple = 1;
        steminaGageMultiple = 1;
        Debug.Log("InitPlayerData hp : " + playerData.Hp);
        this.playerData = playerData;
        originPlayerData = playerData.Clone();
        ApplyItemEffect();
        PlayerHPUi.SetHpBar(playerData.Hp);
        stamina.SetStaminaBar(playerData.StaminaMax);
        playerData.SkillGauge = 100;
    }
    #endregion

    #region function
    private void ScaleChange(float scale)
    {
        if(scale != this.bodyTransform.localScale.x)
        {
            ParticleManager.Instance.PlayParticle("Smoke", this.bodyTransform.position);
        }
        this.bodyTransform.localScale = Vector3.one * scale;
        this.GetComponentInChildren<Camera>().transform.localScale = Vector3.one / scale;
    }
    public void UpdateMuzzlePosition(Vector3 pos, bool visible)
    {
        muzzlePosObj.SetActive(visible);
        muzzlePosTransform.position = pos;
    }

    public override bool Evade()
    {
        if (!isActiveMove)
            return false;
        if (isEvade || !canEvade)
            return false;
        controller.AttackJoyStickUp();
        canEvade = false;
        isEvade = true;
        gameObject.layer = 0;
        directionVector = controller.GetMoveRecentNormalInputVector();
        directionVector.Normalize();
        directionDegree = directionVector.GetDegFromVector();
        if (-90 <= directionDegree && directionDegree < 90)
        {
            isRightDirection = true;
            scaleVector.x = 1f;
            spriteTransform.localScale = scaleVector;
        }
        else
        {
            isRightDirection = false;
            scaleVector.x = -1f;
            spriteTransform.localScale = scaleVector;
        }
        ParticleManager.Instance.PlayParticle("WalkSmoke", bodyTransform.position, spriteRenderer.transform.localScale);
        StartCoroutine(Roll(directionVector));
        return true;
    }

    protected override void Die()
    {
        GameDataManager.Instance.SetTime(TimeController.Instance.GetPlayTime);
        UIManager.Instance.GameOverUI();
        GameStateManager.Instance.GameOver();
    }

    /// <summary>
    /// 쉴드가 있을시 데미지 상관 없이 공격(공격 타입 상관 X) 방어, 아직 미사용
    /// </summary>
    public bool DefendAttack()
    {
        if (0 >= shieldCount)
            return false;

        shieldCount -= 1;
        // 버프매니저 쪽으로 쉴드 버프 없애는 명령 보내기
        return true;
    }
    private void ChargeSkill()
    {
        playerData.SkillGauge += Time.deltaTime;
        controller.ActiveSkill(playerData.SkillGauge, playerData.SkillData.CoolTime);
    }
    public override float Attacked(TransferBulletInfo transferredBulletInfo)
    {
        if(damageImmune == CharacterInfo.DamageImmune.ALL)
        {
            ParticleManager.Instance.PlayParticle("Guard", interactiveCollider2D.transform.position);
            return 0;
        }
        if (CharacterInfo.State.ALIVE != pState || isEvade)
            return 0;
        playerData.Hp -= transferredBulletInfo.damage;
        AttackedAction(1);
        PlayerHPUi.ChangeHp(playerData.Hp);
        AttackedEffect();
        if (playerData.Hp <= 0) Die();
        return transferredBulletInfo.damage;
    }

    public override float Attacked(Vector2 _dir, Vector2 bulletPos, float damage, float knockBack, float criticalChance = 0, bool positionBasedKnockBack = false)
    {
        if (damageImmune == CharacterInfo.DamageImmune.ALL)
        {
            ParticleManager.Instance.PlayParticle("Guard", interactiveCollider2D.transform.position);
            return 0;
        }
        if (CharacterInfo.State.ALIVE != pState || isEvade)
            return 0;
        float criticalCheck = Random.Range(0f, 1f);
        // 크리티컬 공격
        playerData.Hp -= damage;
        AttackedAction(damage);

        if (knockBack > 0)
            KnockBack(knockBack, _dir, bulletPos, positionBasedKnockBack);

        PlayerHPUi.ChangeHp(playerData.Hp);
        AttackedEffect();
 

        if (playerData.Hp <= 0) Die();

        return damage;
    }

    public override void ActiveSkill()
    {
        float lapsedTime = playerData.SkillGauge;
        playerData.SkillData.Run(this, ActiveSkillManager.nullVector, ref lapsedTime);
        playerData.SkillGauge = lapsedTime;
        controller.ActiveSkill(lapsedTime, playerData.SkillData.CoolTime);
    }

    public override void SetAim()
    {
        switch (autoAimType)
        {
            default:
            case CharacterInfo.AutoAimType.REACTANCE:
            case CharacterInfo.AutoAimType.AUTO:
                AutoAim();
                break;
            case CharacterInfo.AutoAimType.SEMIAUTO:
                SemiAutoAim();
                break;
            case CharacterInfo.AutoAimType.MANUAL:
                ManualAim();
                break;
        }
    }

    public override CustomObject Interact()
    {
        float bestDistance = interactiveCollider2D.radius;
        Collider2D bestCollider = null;

        Collider2D[] collider2D = Physics2D.OverlapCircleAll(bodyTransform.position, interactiveCollider2D.radius, (1 << 1) | (1 << 9));

        for (int i = 0; i < collider2D.Length; i++)
        {
            if (!collider2D[i].GetComponent<CustomObject>().GetAvailable())
                continue;
            float distance = Vector2.Distance(bodyTransform.position, collider2D[i].transform.position);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestCollider = collider2D[i];
            }
        }

        if (null == bestCollider)
            return null;

        return bestCollider.GetComponent<CustomObject>();
    }

    public override void SelfDestruction()
    {
        Attacked(Vector2.zero, bodyTransform.position, hp * 0.5f, 0);
    }

    public bool AttackAble()
    {
        if (pState == CharacterInfo.State.ALIVE && !isEvade)
            return true;
        else return false;
    }

    public void AddKilledEnemyCount()
    {
        
        playerData.SkillGauge += (int)(10 * skillGageMultiple);
        if (playerData.SkillGauge >= 100)
            playerData.SkillGauge = 100;
        controller.ActiveSkill(playerData.SkillGauge, 100);
        if (false == buffManager.CharacterTargetEffectTotal.canDrainHp) return;
        killedEnemyCount += 1;
        if (killedEnemyCount == 7)
        {
            RecoverHp(1f);
            killedEnemyCount = 0;
        }
    }

    public bool ConsumeStamina(int staminaConsumption)
    {
        if (0 <= playerData.Stamina)
            return false;
        playerData.Stamina -= staminaConsumption;
        if (0 <= playerData.Stamina)
            playerData.Stamina = 0;

        return true;
    }

    private void ChangeHP()
    {
        PlayerHPUi.ChangeHp(playerData.Hp);
    }
    public void RecoverHp(float recoveryHp)
    {
        ParticleManager.Instance.PlayParticle("Hp", this.bodyTransform.position);
        playerData.Hp += recoveryHp;
        if(playerData.Hp >= playerData.HpMax)
        {
            playerData.Hp = playerData.HpMax;
        }
        PlayerHPUi.ChangeHp(playerData.Hp);
    }

    private void AttackedAction(float power)
    {
        CameraController.Instance.Shake(0.2f, 0.2f);
        LayerController.Instance.FlashAttackedLayer(0.2f);
    }
    private void AutoAim()
    {
        int enemyTotal = EnemyManager.Instance.GetAliveEnemyTotal();

        if (0 == enemyTotal)
        {
            directionVector = controller.GetMoveAttackInputVector();
            directionVector.Normalize();
            directionDegree = directionVector.GetDegFromVector();
            return;
        }
        else
        {
            List<Enemy> enemyList = EnemyManager.Instance.GetEnemyList;
            List<BoxCollider2D> enemyColliderList = EnemyManager.Instance.GetEnemyColliderList;
            raycastHitEnemies.Clear();
            int raycasthitEnemyNum = 0;
            float minDistance = 10000f;
            int proximateEnemyIndex = -1;

            Vector3 enemyPos = new Vector3(0, 0, 0);
            for (int i = 0; i < enemyTotal; i++)
            {
                raycasthitEnemyInfo.index = i;
                enemyPos = enemyColliderList[i].transform.position + new Vector3(enemyColliderList[i].offset.x + enemyColliderList[i].offset.y, 0);
                raycasthitEnemyInfo.distance = Vector2.Distance(enemyPos, objTransform.position);
                hit = Physics2D.Raycast(objTransform.position, enemyPos - objTransform.position, raycasthitEnemyInfo.distance, layerMask);
                if (hit.collider == null)
                {
                    raycastHitEnemies.Add(raycasthitEnemyInfo);
                    raycasthitEnemyNum += 1;
                }
            }

            if (raycasthitEnemyNum == 0)
            {
                isBattle = false;
                directionVector = controller.GetMoveAttackInputVector();
                directionDegree = directionVector.GetDegFromVector();
                return;
            }

            for (int j = 0; j < raycasthitEnemyNum; j++)
            {
                if (raycastHitEnemies[j].distance <= minDistance)
                {
                    minDistance = raycastHitEnemies[j].distance;
                    proximateEnemyIndex = j;
                }
            }

            BoxCollider2D enemyColider = enemyColliderList[raycastHitEnemies[proximateEnemyIndex].index];
            enemyPos = enemyColider.transform.position + new Vector3(enemyColider.offset.x + enemyColider.offset.y, 0);
            directionVector = (enemyPos - objTransform.position);
            directionVector.z = 0;
            directionVector.Normalize();
            directionDegree = directionVector.GetDegFromVector();
            isBattle = true;
        }
    }
    private void SemiAutoAim()
    {
        Vector2 enemyVector;

        int enemyTotal = EnemyManager.Instance.GetAliveEnemyTotal();

        if (0 == enemyTotal)
        {
            directionVector = controller.GetMoveAttackInputVector();
            directionVector.Normalize();
            directionDegree = directionVector.GetDegFromVector();
            isBattle = false;
            return;
        }
        else
        {
            directionVector = controller.GetAttackRecentNormalInputVector();
            directionVector.Normalize();
            List<Enemy> enemyList = EnemyManager.Instance.GetEnemyList;
            List<BoxCollider2D> enemyColliderList = EnemyManager.Instance.GetEnemyColliderList;
            raycastHitEnemies.Clear();
            int raycasthitEnemyNum = 0;
            float minDistance = 10000f;
            int proximateEnemyIndex = -1;

            Vector3 enemyPos;
            for (int i = 0; i < enemyTotal; i++)
            {
                raycasthitEnemyInfo.index = i;
                enemyPos = enemyColliderList[i].transform.position;
                raycasthitEnemyInfo.distance = Vector2.Distance(enemyPos, objTransform.position);
                enemyVector = enemyPos - objTransform.position;
                hit = Physics2D.Raycast(objTransform.position, enemyVector, raycasthitEnemyInfo.distance, layerMask);
                if (hit.collider == null && Vector2.Dot(directionVector, enemyVector.normalized) >.25f)
                {
                    raycastHitEnemies.Add(raycasthitEnemyInfo);
                    raycasthitEnemyNum += 1;
                }
            }

            if (raycasthitEnemyNum == 0)
            {
                directionDegree = directionVector.GetDegFromVector();
                return;
            }

            for (int j = 0; j < raycasthitEnemyNum; j++)
            {
                if (raycastHitEnemies[j].distance <= minDistance)
                {
                    minDistance = raycastHitEnemies[j].distance;
                    proximateEnemyIndex = j;
                }
            }
            BoxCollider2D enemyColider = enemyColliderList[raycastHitEnemies[proximateEnemyIndex].index];
            enemyPos = enemyColider.transform.position + new Vector3(enemyColider.offset.x + enemyColider.offset.y, 0);
            directionVector = (enemyPos - objTransform.position);
            directionVector.Normalize();
            directionDegree = directionVector.GetDegFromVector();
        }
        isBattle = true;
    }
    private void ManualAim()
    {
        directionVector = controller.GetMoveAttackInputVector();
        directionVector.Normalize();
        directionDegree = directionVector.GetDegFromVector();
        isBattle = true;
    }
    private void Move()
    {
        if (isEvade)
            return;

        if (!isActiveMove)
            return;
        if(isBattle)
        {
            totalSpeed = playerData.MoveSpeed + floorSpeed - battleSpeed;
        }
        else
        {
            totalSpeed = playerData.MoveSpeed + floorSpeed;
        }
        rgbody.MovePosition(objTransform.position 
            + controller.GetMoveInputVector() * (totalSpeed) * Time.fixedDeltaTime);

        if (controller.GetMoveInputVector().sqrMagnitude > 0.1f)
        {
            animationHandler.Walk();
        }
        else
        {
            animationHandler.Idle();
        }
#if UNITY_EDITOR
        if (Input.GetKey(KeyCode.W))
        {
            bodyTransform.Translate(Vector2.up * playerData.MoveSpeed * Time.fixedDeltaTime);
        }
        else if (Input.GetKey(KeyCode.S))
        {
            bodyTransform.Translate(Vector2.down * playerData.MoveSpeed * Time.fixedDeltaTime);
        }

        if (Input.GetKey(KeyCode.D))
        {
            bodyTransform.Translate(Vector2.right * playerData.MoveSpeed * Time.fixedDeltaTime);
        }
        else if (Input.GetKey(KeyCode.A))
        {
            bodyTransform.Translate(Vector2.left * playerData.MoveSpeed * Time.fixedDeltaTime);
        }
#endif
    }
    private void EndEvade()
    {
        isEvade = false;
        gameObject.layer = 16;
        weaponManager.RevealWeapon();
    }

    // total을 안 거치고 바로 효과 적용하기 위해 구분함, 소모형 아이템 용 함수

    public void ApplyConsumableItem(CharacterTargetEffect itemUseEffect)
    {
        Debug.Log("소모품 아이템 플레이어 대상 효과 적용");
        if (0 != itemUseEffect.recoveryHp)
        {
            RecoverHp(itemUseEffect.recoveryHp);
        }
        if (0 != itemUseEffect.recoveryStamina)
        {
            stamina.RecoverStamina(playerData.Stamina);
        }
    }

    public override void ApplyItemEffect()
    {
        CharacterTargetEffect itemUseEffect = buffManager.CharacterTargetEffectTotal;
        playerData.StaminaMax = (int)(originPlayerData.StaminaMax * itemUseEffect.steminaGage);
        if(playerData.MoveSpeed != originPlayerData.MoveSpeed * itemUseEffect.moveSpeedIncrement)
        {
            if(playerData.MoveSpeed > originPlayerData.MoveSpeed * itemUseEffect.moveSpeedIncrement)
            {
                ParticleManager.Instance.PlayParticle("SpeedDown", this.bodyTransform.position  );
            }
            else
            {
                ParticleManager.Instance.PlayParticle("SpeedUp", this.bodyTransform.position);
            }
        }
        playerData.MoveSpeed = originPlayerData.MoveSpeed * itemUseEffect.moveSpeedIncrement;
        IsNotConsumeStamina = itemUseEffect.isNotConsumeStamina;
        IsNotConsumeAmmo = itemUseEffect.isNotConsumeAmmo;
        damageImmune = itemUseEffect.isImmuneDamage;
        abnormalImmune = itemUseEffect.isImmuneAbnormal;

        if (itemUseEffect.hpRatio > 0)
            playerData.Hp = originPlayerData.Hp * itemUseEffect.hpRatio;
        if (itemUseEffect.hpMaxRatio > 0)
            playerData.HpMax = originPlayerData.HpMax * itemUseEffect.hpMaxRatio;
        if (itemUseEffect.skillGage > 0)
            skillGageMultiple = itemUseEffect.skillGage;
        if (itemUseEffect.steminaGage > 0)
            steminaGageMultiple = itemUseEffect.steminaGage;

        if (itemUseEffect.charScale > 0 && itemUseEffect.charScale <= 2f)
            ScaleChange(itemUseEffect.charScale);
    }
    #endregion
    #region abnormal
    protected override bool IsAbnormal()
    {
        return isAbnormalStatuses[(int)AbnormalStatusType.STUN] || isAbnormalStatuses[(int)AbnormalStatusType.FREEZE] ||
            isAbnormalStatuses[(int)AbnormalStatusType.CHARM];
    }
    protected override void StopAbnormalStatus(AbnormalStatusType abnormalStatusType)
    {
        int type = (int)abnormalStatusType;
        if (false == isAbnormalStatuses[type])
            return;
        isAbnormalStatuses[type] = false;
        if (null != abnormalStatusCoroutines[type])
            StopCoroutine(abnormalStatusCoroutines[type]);
        abnormalStatusCoroutines[type] = null;
        switch (abnormalStatusType)
        {
            case AbnormalStatusType.FREEZE:
                abnormalComponents.FreezeEffect.SetActive(false);
                SubRetrictsMovingCount();
                SubRetrictsAttackingCount();
                break;
            case AbnormalStatusType.STUN:
                abnormalComponents.StunEffect.SetActive(false);
                SubRetrictsMovingCount();
                SubRetrictsAttackingCount();
                break;
            case AbnormalStatusType.CHARM:
                abnormalComponents.CharmEffect.SetActive(false);
                SubRetrictsMovingCount();
                SubRetrictsAttackingCount();
                break;
            default:
                break;
        }
    }
    // 여러 상태이상, 단일 상태이상 중첩 시 공격, 이동 제한을 한 곳에서 관리하기 위해서
    /// <summary> 이동 방해 상태 이상 갯수 증가 및 이동 AI OFF Check </summary>
    protected override void AddRetrictsMovingCount()
    {
        restrictMovingCount += 1;
        if (1 <= restrictMovingCount)
        {
            isActiveMove = false;
        }
    }
    /// <summary> 이동 방해 상태 이상 갯수 감소 및 이동 AI ON Check </summary>
    protected override void SubRetrictsMovingCount()
    {
        restrictMovingCount -= 1;
        if (0 >= restrictMovingCount)
        {
            restrictMovingCount = 0;
            isActiveMove = true;
        }
    }
    /// <summary> 공격 방해 상태 이상 갯수 증가 및 공격 AI OFF Check </summary>
    protected override void AddRetrictsAttackingCount()
    {
        restrictAttackingCount += 1;
        if (1 >= restrictAttackingCount)
        {
            isActiveAttack = false;
        }
    }
    /// <summary> 공격 방해 상태 이상 갯수 감소 및 공격 AI ON Check </summary>
    protected override void SubRetrictsAttackingCount()
    {
        restrictAttackingCount -= 1;
        if (0 <= restrictAttackingCount)
        {
            isActiveAttack = true;
        }
    }

    #endregion
    #region coroutine
    protected override IEnumerator PoisonCoroutine()
    {
        isPoisoning = true;
        abnormalComponents.PoisonEffect.SetActive(true);
        float damage = .3f;
        while (poisonOverlappingCount > 0)
        {
            yield return YieldInstructionCache.WaitForSeconds(StatusConstants.Instance.GraduallyDamageCycle);
            ColorChange(poisonColor);
            headRenderer.color = poisonColor;

            playerData.Hp -= damage;
            ChangeHP();
            for (int i = 0; i < StatusConstants.Instance.PoisonInfo.overlapCountMax; i++)
            {
                if (poisonCount[i] > 0)
                {
                    poisonCount[i] -= 1;
                    if (0 == poisonCount[i])
                    {
                        poisonOverlappingCount -= 1;
                    }
                }
            }
        }
        abnormalComponents.PoisonEffect.SetActive(false);
        isPoisoning = false;
        ColorChange(baseColor);
        headRenderer.color = baseColor;

    }

    protected override IEnumerator BurnCoroutine()
    {
        isBurning = true;
        abnormalComponents.BurnEffect.SetActive(true);
        float damage = .3f;
        while (burnOverlappingCount > 0)
        {
            yield return YieldInstructionCache.WaitForSeconds(StatusConstants.Instance.GraduallyDamageCycle);
            ColorChange(burnColor);
            headRenderer.color = burnColor;

            playerData.Hp -= damage;
            ChangeHP();
            for (int i = 0; i < StatusConstants.Instance.BurnInfo.overlapCountMax; i++)
            {
                if (burnCount[i] > 0)
                {
                    burnCount[i] -= 1;
                    if (0 == burnCount[i])
                    {
                        burnOverlappingCount -= 1;
                    }
                }
            }
        }
        abnormalComponents.BurnEffect.SetActive(false);
        isBurning = false;
        ColorChange(baseColor);
        headRenderer.color = baseColor;

    }

    protected override IEnumerator DelayStateCoroutine()
    {
        isDelayingState = true;
        while (delayStateOverlappingCount > 0)
        {
            CharacterTargetEffect characterTargetEffect = new CharacterTargetEffect();
            WeaponTargetEffect weaponTargetEffect = new WeaponTargetEffect();
            characterTargetEffect.moveSpeedIncrement = -StatusConstants.Instance.DelayStateInfo.value;
            weaponTargetEffect.bulletSpeedIncrement = -StatusConstants.Instance.DelayStateInfo.value;
            buffManager.RegisterItemEffect(characterTargetEffect, BuffManager.EffectApplyType.BUFF, -1, StatusConstants.Instance.DelayStateInfo.effectiveTime);
            buffManager.RegisterItemEffect(weaponTargetEffect, BuffManager.EffectApplyType.BUFF, -1, StatusConstants.Instance.DelayStateInfo.effectiveTime);
            yield return YieldInstructionCache.WaitForSeconds(StatusConstants.Instance.DelayStateInfo.effectiveTime);
            delayStateOverlappingCount -= 1;
        }
        delayStateCount = 0;
        isDelayingState = false;
    }

    protected override IEnumerator FreezeCoroutine()
    {
        int type = (int)AbnormalStatusType.FREEZE;
        animationHandler.Idle();
        AddRetrictsMovingCount();
        AddRetrictsAttackingCount();
        abnormalComponents.FreezeEffect.SetActive(true);
        isAbnormalStatuses[type] = true;
        abnormalStatusTime[type] = 0;
        abnormalStatusDurationTime[type] = StatusConstants.Instance.FreezeInfo.effectiveTime;

        while (abnormalStatusTime[type] <= abnormalStatusDurationTime[type])
        {
            ColorChange(freezeColor);
            headRenderer.color = freezeColor;

            abnormalStatusTime[type] += Time.fixedDeltaTime;
            yield return YieldInstructionCache.WaitForSeconds(Time.fixedDeltaTime);
        }

        StopAbnormalStatus(AbnormalStatusType.FREEZE);
        ColorChange(baseColor);
        headRenderer.color = baseColor;

    }

    protected override IEnumerator StunCoroutine(float effectiveTime)
    {
        int type = (int)AbnormalStatusType.STUN;
        abnormalComponents.StunEffect.SetActive(true);
        AddRetrictsMovingCount();
        AddRetrictsAttackingCount();
        animationHandler.Idle();
        isAbnormalStatuses[type] = true;
        abnormalStatusTime[type] = 0;
        abnormalStatusDurationTime[type] = effectiveTime;
        while (abnormalStatusTime[type] <= abnormalStatusDurationTime[type])
        {
            abnormalStatusTime[type] += Time.fixedDeltaTime;
            yield return YieldInstructionCache.WaitForSeconds(Time.fixedDeltaTime);
        }

        StopAbnormalStatus(AbnormalStatusType.STUN);
    }

    protected override IEnumerator CharmCoroutine(float effectiveTime)
    {
        int type = (int)AbnormalStatusType.CHARM;
        abnormalComponents.CharmEffect.SetActive(true);
        AddRetrictsMovingCount();
        AddRetrictsAttackingCount();
        isAbnormalStatuses[type] = true;
        abnormalStatusTime[type] = 0;
        abnormalStatusDurationTime[type] = effectiveTime;

        while (abnormalStatusTime[type] <= abnormalStatusDurationTime[type])
        {
            abnormalStatusTime[type] += Time.fixedDeltaTime;
            bodyTransform.Translate(moveSpeed * 0.5f * (PlayerManager.Instance.GetPlayerPosition() - bodyTransform.position).normalized * Time.fixedDeltaTime);
            yield return YieldInstructionCache.WaitForSeconds(Time.fixedDeltaTime);
        }

        StopAbnormalStatus(AbnormalStatusType.CHARM);
    }

    protected override IEnumerator KnockBackCheck()
    {
        while (true)
        {
            if (Vector2.zero != rgbody.velocity && rgbody.velocity.magnitude < 1f)
            {
                SubRetrictsMovingCount();
                knockBackCheck = null;
                break;
            }
            yield return YieldInstructionCache.WaitForSeconds(Time.fixedDeltaTime);
        }
    }

    private IEnumerator Roll(Vector3 dir)
    {
        animationHandler.Skill(0);
        weaponManager.HideWeapon();
        totalSpeed = playerData.MoveSpeed + floorSpeed;

        float doubling = 2f;
        float animTime = .63f;
         while (animTime > 0)
        {
            animTime -= Time.fixedDeltaTime;

            yield return YieldInstructionCache.WaitForFixedUpdate;

            doubling -= Time.fixedDeltaTime * 3;
            if (doubling <= 1f)
                doubling = 1f;
            rgbody.MovePosition(objTransform.position + dir * (totalSpeed) * Time.fixedDeltaTime * doubling);
        }
        animationHandler.Skill(-1);
        EndEvade();
        yield return YieldInstructionCache.WaitForSeconds(0.05f);
        yield return YieldInstructionCache.WaitForSeconds(evadeCoolTime);

        canEvade = true;
    }

    #endregion
}

public class PlayerController
{
    #region components
    private Joystick moveJoyStick;
    private AttackJoyStick attackJoyStick;
    private ActiveSkillButton activeSkillButton;
    #endregion

    public PlayerController(Joystick moveJoyStick, AttackJoyStick attackJoyStick,ActiveSkillButton activeSkillButton)
    {
        this.moveJoyStick = moveJoyStick;
        this.attackJoyStick = attackJoyStick;
        this.activeSkillButton = activeSkillButton;
        attackJoyStick.StartRoutine();
    }
    public Vector2 GetMoveAttackInputVector()
    {
        if(attackJoyStick.GetButtonDown())
        {
            return attackJoyStick.GetRecentNormalInputVector();
        }
        return moveJoyStick.GetRecentNormalInputVector();
    }
    #region move
    /// <summary>
    /// 조이스틱이 현재 바라보는 방향의 벡터  
    /// </summary> 
    public Vector3 GetMoveInputVector()
    {
        float h = moveJoyStick.GetHorizontalValue();
        float v = moveJoyStick.GetVerticalValue();

        return new Vector3(h, v, 0).normalized;
    }

    /// <summary>
    /// 입력한 조이스틱의 가장 최근 Input vector의 normal vector 반환 
    /// </summary>
    public Vector3 GetMoveRecentNormalInputVector()
    {
        return moveJoyStick.GetRecentNormalInputVector();
    }
    #endregion
    #region attack
    /// <summary>
    /// 조이스틱이 현재 바라보는 방향의 벡터  
    /// </summary> 
    public Vector3 GetAttackInputVector()
    {
        float h = attackJoyStick.GetHorizontalValue();
        float v = attackJoyStick.GetVerticalValue();

        return new Vector3(h, v, 0).normalized; 
    }

    /// <summary>
    /// 입력한 조이스틱의 가장 최근 Input vector의 normal vector 반환 
    /// </summary>
    public Vector3 GetAttackRecentNormalInputVector()
    {
        return attackJoyStick.GetRecentNormalInputVector();
    }

    public void AttackJoyStickUp()
    {
        attackJoyStick.OnPointerUp(null);
    }
    #endregion
    #region skill
    public void ActiveSkill(float sum, float total)
    {
        if (!PlayerManager.Instance.GetPlayer().GetIsAcitveAttack())
            return;
        activeSkillButton.ChargeFill(sum / total);
    }
    #endregion
}