﻿using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AttackJoyStick : Joystick
{
    [SerializeField]
    private Sprite attackSprite;
    [SerializeField]
    private Sprite interactSprite;
    private CustomObject interactiveObject;
    private CustomObject olderInteractiveObject;

    private void Update()
    {
        if (isTouchDown)
        {
            if (UIManager.Instance.GetActived() || character.IsEvade())
                return;
            character.SetAim();
            character.GetWeaponManager().AttackButtonDown();
        }
        else
        {
            olderInteractiveObject = interactiveObject;
            interactiveObject = character.Interact();
            if (interactiveObject == null)
            {
                if (olderInteractiveObject != null)
                    olderInteractiveObject.DeIndicateInfo();
                joystickImage.sprite = attackSprite;
            }
            else
            {
                interactiveObject.IndicateInfo();
                joystickImage.sprite = interactSprite;
            }
        }
    }

    public override void OnPointerDown(PointerEventData ped)
    {
        if (interactiveObject != null)
        {
            interactiveObject.Active();
        }
        else
        {
            if (character.IsEvade())
                return;
            base.OnPointerDown(ped);
        }
    }

    public override void OnPointerUp(PointerEventData ped)
    {
        base.OnPointerUp(ped);
        character.GetWeaponManager().AttackButtonUP();
    }
}
