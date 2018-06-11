﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
class GameData
{
    float hp;
    int hungry;
    int m_floor;
    int m_coin;
    int[] weaponIds;
    int[] weaponAmmos;
    Player.PlayerType m_playerType;

    public GameData()
    {
        m_floor = 1;
        m_coin = 0;
        m_playerType = Player.PlayerType.SOCCER;
    }
    #region getter
    public float GetHp() { return hp; }
    public int GetFloor() { return m_floor; }
    public int GetCoin() { return m_coin; }
    public int[] GetWeaponIds() { return weaponIds; }
    public int[] GetWeaponAmmos() { return weaponAmmos; }
    public Player.PlayerType GetPlayerType() { return m_playerType; }
    #endregion
    #region setter
    public void SetHp(float _hp) { hp = _hp; }
    public void SetFloor() { m_floor++; }
    public void SetCoin(int _coin) { m_coin = _coin; }
    public void SetWeaponIds(int[] _weaponIds) { weaponIds = _weaponIds; }
    public void SetWeaponAmmos(int[] _weaponAmmos) { weaponAmmos = _weaponAmmos; }
    #endregion
}