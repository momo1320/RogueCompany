﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WeaponAsset;

public class Player : Character
{
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

    // 윤아 0802
    private Stamina stamina;

    private float floorSpeed;
    private int shieldCount;

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
    public int GetStamina()
    {
        return playerData.Stamina;
    }
    public int GetSkillGauage()
    {
        return playerData.SkillGauge;
    }
    public void SetStamina(int stamina)
    {
        playerData.Stamina = stamina;
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
    }

    // bool e = false;
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
        if (Input.GetKeyDown(KeyCode.LeftShift))
            Evade();
        spriteRenderer.sortingOrder = -Mathf.RoundToInt(transform.position.y * 100);
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
        pState = CharacterInfo.State.ALIVE;
        ownerType = CharacterInfo.OwnerType.Player;
        damageImmune = CharacterInfo.DamageImmune.NONE;

        animationHandler.Init(this, PlayerManager.Instance.runtimeAnimator);

        IsNotConsumeStamina = false;
        IsNotConsumeAmmo = false;

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

        animationHandler.SetEndAction(EndEvade);
        TimeController.Instance.PlayStart();
    }

    public void InitPlayerData(PlayerData playerData)
    {
        Debug.Log("InitPlayerData hp : " + playerData.Hp);
        this.playerData = playerData;
        originPlayerData = playerData.Clone();
        ApplyItemEffect();
        PlayerHPUi.SetHpBar(playerData.Hp);
        stamina.SetStaminaBar(playerData.StaminaMax);
    }
    #endregion

    #region function

    public override bool Evade()
    {
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
        animationHandler.Skill(0);
        damageImmune = CharacterInfo.DamageImmune.DAMAGE;
        weaponManager.HideWeapon();
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
    /// 쉴드가 있을시 데미지 상관 없이 공격(공격 타입 상관 X) 방어
    /// </summary>
    public bool DefendAttack()
    {
        if (0 >= shieldCount)
            return false;

        shieldCount -= 1;
        // 버프매니저 쪽으로 쉴드 버프 없애는 명령 보내기
        return true;
    }

    public override float Attacked(TransferBulletInfo transferredBulletInfo)
    {
        // if (DefendAttack()) return 0;
        if (damageImmune == CharacterInfo.DamageImmune.DAMAGE)
            return 0;
        playerData.Hp -= transferredBulletInfo.damage;
        AttackedAction(1);
        PlayerHPUi.DecreaseHp(playerData.Hp);
        AttackedEffect();
        if (playerData.Hp <= 0) Die();
        return transferredBulletInfo.damage;
    }

    public override float Attacked(Vector2 _dir, Vector2 bulletPos, float damage, float knockBack, float criticalChance = 0, bool positionBasedKnockBack = false)
    {
        if (CharacterInfo.State.ALIVE != pState || damageImmune == CharacterInfo.DamageImmune.DAMAGE)
            return 0;
        float criticalCheck = Random.Range(0f, 1f);
        // 크리티컬 공격
        playerData.Hp -= damage;
        AttackedAction(damage);
        if (knockBack > 0)
            isKnockBack = true;

        // 넉백 총알 방향 : 총알 이동 방향 or 몬스터-총알 방향 벡터
        rgbody.velocity = Vector3.zero;

        // bullet과 충돌 Object 위치 차이 기반의 넉백  
        if (positionBasedKnockBack)
        {
            rgbody.AddForce(knockBack * ((Vector2)transform.position - bulletPos).normalized);
        }
        // bullet 방향 기반의 넉백
        else
        {
            rgbody.AddForce(knockBack * _dir);
        }
        PlayerHPUi.DecreaseHp(playerData.Hp);
        AttackedEffect();
        StopCoroutine(KnockBackCheck());
        StartCoroutine(KnockBackCheck());

        if (playerData.Hp <= 0) Die();

        return damage;
    }

    public override void ActiveSkill()
    {
        if (100 == playerData.SkillGauge)
        {
            Debug.Log("Player 스킬 활성화");
            //skillGauge = 0;
        }
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
        float bestDistance = interactiveCollider2D.radius * 10;
        Collider2D bestCollider = null;

        Collider2D[] collider2D = Physics2D.OverlapCircleAll(transform.position, interactiveCollider2D.radius, (1 << 1) | (1 << 9));

        for (int i = 0; i < collider2D.Length; i++)
        {
            if (!collider2D[i].GetComponent<CustomObject>().GetAvailable())
                continue;
            float distance = Vector2.Distance(transform.position, collider2D[i].transform.position);

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

    public bool AttackAble()
    {
        if (pState == CharacterInfo.State.ALIVE && !isEvade)
            return true;
        else return false;
    }

    public void AddKilledEnemyCount()
    {
        if (false == buffManager.CharacterTargetEffectTotal.canDrainHp) return;
        killedEnemyCount += 1;
        if (killedEnemyCount == 7)
        {
            Debug.Log("몬스터 7마리 처치 후 피 회복");
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

    public bool RecoverHp(float recoveryHp)
    {
        if (playerData.Hp + recoveryHp <= playerData.HpMax)
        {
            playerData.Hp += recoveryHp;
            return true;
        }
        else
            return false;
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
            List<CircleCollider2D> enemyColliderList = EnemyManager.Instance.GetEnemyColliderList;
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

            CircleCollider2D enemyColider = enemyColliderList[raycastHitEnemies[proximateEnemyIndex].index];
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
            List<CircleCollider2D> enemyColliderList = EnemyManager.Instance.GetEnemyColliderList;
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
            CircleCollider2D enemyColider = enemyColliderList[raycastHitEnemies[proximateEnemyIndex].index];
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
        // 조이스틱 방향으로 이동하되 입력 거리에 따른 이동속도 차이가 생김.
        //objTransform.Translate(controller.GetMoveInputVector() * (playerData.MoveSpeed + floorSpeed) * Time.fixedDeltaTime);
        if (controller.GetMoveInputVector().sqrMagnitude > 0.1f)
        {
            animationHandler.Walk();
        }
        else
        {
            animationHandler.Idle();
        }
        if (Input.GetKey(KeyCode.W))
        {
            transform.Translate(Vector2.up * playerData.MoveSpeed * Time.fixedDeltaTime);
        }
        else if (Input.GetKey(KeyCode.S))
        {
            transform.Translate(Vector2.down * playerData.MoveSpeed * Time.fixedDeltaTime);
        }

        if (Input.GetKey(KeyCode.D))
        {
            transform.Translate(Vector2.right * playerData.MoveSpeed * Time.fixedDeltaTime);
        }
        else if (Input.GetKey(KeyCode.A))
        {
            transform.Translate(Vector2.left * playerData.MoveSpeed * Time.fixedDeltaTime);
        }
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
            playerData.Hp += itemUseEffect.recoveryHp;
        }
        if (0 != itemUseEffect.recoveryStamina)
        {
            playerData.Stamina += itemUseEffect.recoveryStamina;
        }
    }

    public override void ApplyItemEffect()
    {
        CharacterTargetEffect itemUseEffect = buffManager.CharacterTargetEffectTotal;
        playerData.StaminaMax = (int)(originPlayerData.StaminaMax * itemUseEffect.staminaMaxIncrement);
        playerData.MoveSpeed = originPlayerData.MoveSpeed * itemUseEffect.moveSpeedIncrement;
        IsNotConsumeStamina = itemUseEffect.isNotConsumeStamina;
        IsNotConsumeAmmo = itemUseEffect.isNotConsumeAmmo;
    }

    public override void ApplyStatusEffect(StatusEffectInfo statusEffectInfo)
    {
    }

    protected override bool IsAbnormal()
    {
        return false;
    }
    #endregion

    #region coroutine
    private IEnumerator KnockBackCheck()
    {
        while (true)
        {
            yield return YieldInstructionCache.WaitForSeconds(Time.fixedDeltaTime);
            if (Vector2.zero != rgbody.velocity && rgbody.velocity.magnitude < 1f)
            {
                //isActiveAI = true;
                //aiController.PlayMove();
            }
        }
    }
    private IEnumerator Roll(Vector3 dir)
    {
        float doubling = 3;
        totalSpeed = playerData.MoveSpeed + floorSpeed;
        while (isEvade)
        {
            doubling -= Time.fixedDeltaTime * 5;
            if (doubling <= .5f)
                doubling = .5f;
            rgbody.MovePosition(objTransform.position + dir * (totalSpeed) * Time.fixedDeltaTime * doubling);
            yield return YieldInstructionCache.WaitForFixedUpdate;
        }
        yield return YieldInstructionCache.WaitForSeconds(0.05f);
        damageImmune = CharacterInfo.DamageImmune.NONE;
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
    #endregion

    public PlayerController(Joystick moveJoyStick, AttackJoyStick attackJoyStick)
    {
        this.moveJoyStick = moveJoyStick;
        this.attackJoyStick = attackJoyStick;
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
}