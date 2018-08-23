﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;    //UI 클릭시 터치 이벤트 발생 방지.

public class ControllerUI : MonoBehaviourSingleton<ControllerUI>, IDragHandler, IPointerUpHandler, IPointerDownHandler
{
    #region variable
    Vector2 outPos;
    Vector2 touchPos;
    float screenHalfWidth;
    #endregion
    #region controllComponents
    [SerializeField]
    private Joystick moveJoyStick;
    [SerializeField]
    private AttackJoyStick attackJoyStick;
    [SerializeField]
    private WeaponSwitchButton weaponSwitchButton;
    [SerializeField]
    private ActiveSkillButton activeSkillButton;
    #endregion
    #region components
    private RectTransform moveJouStickTransform;
    #endregion
    #region parameter
    public WeaponSwitchButton WeaponSwitchButton
    {
        get
        {
            return weaponSwitchButton;
        }
    }
    public ActiveSkillButton ActiveSkillButton
    {
        get
        {
            return activeSkillButton;
        }
    }
    #endregion
    #region func
    public void SetPlayer(Character player, ref PlayerController controller)
    {
        attackJoyStick.SetPlayer(player);
        activeSkillButton.SetPlayer(player);
        weaponSwitchButton.SetPlayer(player);
        controller = new PlayerController(moveJoyStick, attackJoyStick);
    }
    void DrawMoveJoyStick()
    {
        moveJouStickTransform.position = touchPos;
    }
    void HideMoveJoyStick()
    {
        moveJouStickTransform.position = outPos;
    }
    #endregion
    #region unityFunc
    private void Awake()
    {
        screenHalfWidth = Screen.width * 0.5f;
        outPos = new Vector2(-screenHalfWidth, 0);
        moveJouStickTransform = moveJoyStick.GetComponent<RectTransform>();
        moveJouStickTransform.position = outPos;
    }
    #endregion
    #region Handler

    public void OnDrag(PointerEventData eventData)
    {
        moveJoyStick.OnDrag(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        HideMoveJoyStick();
        moveJoyStick.OnPointerUp(eventData);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
#if UNITY_EDITOR
        if (eventData.position.x < screenHalfWidth)
        {
            touchPos = eventData.position;
            DrawMoveJoyStick();
        }
#else
        if (eventData.position.x < screenHalfWidth)
        {
            touchPos = eventData.position;
            DrawMoveJoyStick();
        }
#endif
    }
    #endregion
}