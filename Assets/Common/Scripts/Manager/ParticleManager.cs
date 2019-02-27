﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ParticleManager : MonoBehaviourSingleton<ParticleManager> {
    Vector3 one;
    Transform bodyTransform;

    public Transform GetBodyTransform()
    {
        return bodyTransform;
    }

    private void Awake()
    {
        one = Vector3.one;
        bodyTransform = transform;
    }
    public void PlayParticle(string str,Vector2 pos)
    {
        ParticleSystem particle = ParticlePool.Instance.GetAvailabeParticle(str);
        if (particle == null)
            return;
        particle.gameObject.transform.position = pos;
        //particle.gameObject.transform.localScale = one;
        particle.Play();
        UtilityClass.Invoke(this, () => particle.gameObject.SetActive(false), particle.main.duration + 0.5f);
    }
    public void PlayParticle(string str, Vector2 pos, float scale)
    {
        ParticleSystem particle = ParticlePool.Instance.GetAvailabeParticle(str);
        if (particle == null)
            return;
        particle.gameObject.transform.position = pos;
        particle.gameObject.transform.localScale = one * scale;
        particle.Play();
        UtilityClass.Invoke(this, () => particle.gameObject.SetActive(false), particle.main.duration + 0.5f);
    }
    public void PlayParticle(string str, Vector2 pos,Vector3 scale)
    {
        ParticleSystem particle = ParticlePool.Instance.GetAvailabeParticle(str);
        if (particle == null)
            return;
        particle.gameObject.transform.position = pos;
        particle.gameObject.transform.localScale = scale;
        particle.Play();
        UtilityClass.Invoke(this, () => particle.gameObject.SetActive(false), particle.main.duration + 0.5f);
    }

    public void PlayParticle(string str, Vector2 pos, Transform parent)
    {
        ParticleSystem particle = ParticlePool.Instance.GetAvailabeParticle(str);
        if (particle == null)
            return;
        particle.gameObject.transform.position = pos;
        particle.gameObject.transform.localScale = one;
        particle.gameObject.transform.parent = parent;
        particle.Play();
        UtilityClass.Invoke(this, () => particle.transform.parent = this.bodyTransform, particle.main.duration + 0.5f);
        UtilityClass.Invoke(this, () => particle.gameObject.SetActive(false), particle.main.duration + 0.5f);
    }
    public void PlayParticle(string str, Vector2 pos, float scale, Transform parent)
    {
        ParticleSystem particle = ParticlePool.Instance.GetAvailabeParticle(str);
        if (particle == null)
            return;
        particle.gameObject.transform.position = pos;
        particle.gameObject.transform.localScale = one * scale;
        particle.gameObject.transform.parent = parent;  
        particle.Play();
        UtilityClass.Invoke(this, () => particle.transform.parent = this.bodyTransform, particle.main.duration + 0.5f);
        UtilityClass.Invoke(this, () => particle.gameObject.SetActive(false), particle.main.duration + 0.5f);
    }
    public void PlayParticle(string str, Vector2 pos, Vector3 scale, Transform parent)
    {
        ParticleSystem particle = ParticlePool.Instance.GetAvailabeParticle(str);
        if (particle == null)
            return;
        particle.gameObject.transform.position = pos;
        particle.gameObject.transform.localScale = scale;
        particle.gameObject.transform.parent = parent;
        particle.Play();
        UtilityClass.Invoke(this, () => particle.transform.parent = this.bodyTransform, particle.main.duration + 0.5f);
        UtilityClass.Invoke(this, () => particle.gameObject.SetActive(false), particle.main.duration + 0.5f);
    }


    // func for mo
    public ParticleSystem PlayBulletParticle(string str, Vector2 pos, Transform parent, Vector3 rotation)
    {
        ParticleSystem particle = ParticlePool.Instance.GetAvailabeParticle(str);
        if (particle == null)
            return null;
        Debug.Log(particle.gameObject.transform.localRotation);
        particle.gameObject.transform.position = pos;
        particle.gameObject.transform.parent = parent;
        particle.gameObject.transform.localScale = one;
        Debug.Log(particle.gameObject.transform.localRotation);
        particle.gameObject.transform.localRotation = Quaternion.Euler(rotation);
        particle.Play();
        return particle;   
    }
}
