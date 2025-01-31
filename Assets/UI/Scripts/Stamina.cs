﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스테미나가 줄여들고 불어나는 걸 보여주는 UI
/// </summary>
public class Stamina : MonoBehaviourSingleton<Stamina>
{
    private const string RECOVERY_STAMINA = "RecoveryStamina";
    [SerializeField]
    private Text staminaText;
    private PlayerData playerData;
    private Player player;
    private Image staminaImage;
    [SerializeField]
    private Image delayStaminaImage;
    private int maxStamina;        // 스태미너의 Max치 
    private int playerStamina;       // player 의 스태미너
    private int stamina;
    private float currentTime = 0;
    #region pro
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
    #endregion

    void Awake() {
        currentTime = 0;
        staminaImage = GetComponent<Image>();
        if (playerData == null)
            playerData = ScriptableObject.CreateInstance<PlayerData>();
    }
    void Update()
    {
        if (maxStamina > stamina)
        {
            currentTime += Time.deltaTime;
            if (currentTime > 3)
            {
                Stamina.Instance.RecoverStamina();
                currentTime = 0;
            }
        }
    }


    #region function
    public void SetPlayer(Player player)
    {
        this.player = player;
    }

    public void SetStaminaBar(int _stamina)
    {
        stamina = _stamina;
        maxStamina = _stamina;
        staminaImage.fillAmount = stamina / (float)maxStamina;
        staminaText.text = stamina + "/" + maxStamina;
    }

    public void SetStaminaMax(int staminaMax)
    {
        maxStamina = staminaMax;
        staminaImage.fillAmount = stamina / (float)maxStamina;
        staminaText.text = stamina + "/" + maxStamina;
        delayStaminaImage.fillAmount = staminaImage.fillAmount;
    }


    /// <summary>
    /// 스테미너 회복 : 자연 회복, 몬스터 처치시 회복
    /// </summary>
    public void RecoverStamina(int recoveryAmount = 3)
    {
        ParticleManager.Instance.PlayParticle(RECOVERY_STAMINA, player.GetPosition(), 1f, player.GetbodyTransform());
        stamina += (int)(recoveryAmount * PlayerBuffManager.Instance.BuffManager.CharacterTargetEffectTotal.gettingStaminaMultiple);
        staminaImage.fillAmount += recoveryAmount / (float)maxStamina;

        if (stamina >= maxStamina)
        {
            stamina = maxStamina;
            staminaImage.fillAmount = 1;
        }
        playerData.Stamina = stamina;
        staminaImage.fillAmount = stamina / (float)maxStamina;
        staminaText.text = stamina + "/" + maxStamina;
    }

    public void RecoverFullStamina()
    {
        int recoveryAmount = maxStamina;
        RecoverStamina(recoveryAmount);
    }
    public void ConsumeStamina(int staminaConsumption)
    {
        float oldStamina = stamina;
        stamina -= staminaConsumption;
        staminaImage.fillAmount -= staminaConsumption / (float)maxStamina;

        if (staminaImage.fillAmount < 0)
        {
            stamina = 0;
            staminaImage.fillAmount = 0;
        }
        playerData.Stamina = stamina;
        staminaText.text = stamina + "/" + maxStamina;
        StartCoroutine(CoroutineStamina(oldStamina, stamina, maxStamina, delayStaminaImage));
    }

    public bool IsFullStamina()
    {
        if (staminaImage.fillAmount >= 1f)
            return true;
        else
            return false;
    }

    public bool IsConsumableStamina()
    {
        if (0 < staminaImage.fillAmount && staminaImage.fillAmount <= 1)
            return true;
        else
            return false;
    }

    IEnumerator CoroutineStamina(float _src, float _dest, float max, Image _image)
    {
        float temp;
        float t = 0;
        while (true)
        {
            yield return YieldInstructionCache.WaitForEndOfFrame;
            t += Time.deltaTime / 1f;

            temp = Mathf.Lerp(_src, _dest, t);
            _image.fillAmount = temp / max;
            if (temp == _dest)
                break;
        }
    }

    #endregion
}
