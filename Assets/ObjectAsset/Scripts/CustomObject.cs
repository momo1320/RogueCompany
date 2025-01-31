﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public enum ObjectType
{
    NONE, UNBREAKABLE, BREAKABLE, PUSHBOX, ITEMBOX,
    VENDINMACHINE, TRAPBOX, PORTAL, SNACKBOX, MEDKITBOX,
    SUBSTATION, STOREITEM, NPC, STATUE, SKILLBOX, PARTITION,
    DESTROYTRAPOBJ
}

public enum ObjectAbnormalType
{
    NONE, FREEZE, POISON, BURN, STUN, CHARM
}

public class CustomObject : MonoBehaviour
{

    public Vector3 objectPosition;
    public ObjectType objectType;
    public Sprite[] sprites;
    public object subParameter;
    public ObjectAbnormalType objectAbnormalType;

    protected Sprite sprite;
    protected bool isActive;
    protected bool isAvailable;
    protected bool isAnimate;
    protected int idx;

    protected Vector2[] nullPolygon;
    protected Vector2[] clickableBoxPolygon;
    #region components
    protected SpriteRenderer spriteRenderer;
    protected SpriteRenderer shadowRenderer;
    protected Animator animator;
    protected BoxCollider2D boxCollider;
    protected new Rigidbody2D rigidbody2D;
    protected TextMesh textMesh;
    protected TextMesh childTextMesh;
    protected PolygonCollider2D polygonCollider2D;
    #endregion

    protected void ShadowDrawing()
    {
        shadowRenderer.sprite = sprite;
    }
    protected void EraseShadow()
    {
        shadowRenderer.sprite = null;
    }
    public bool GetActive()
    {
        return isActive;
    }

    protected void DestroyAndDeactive()
    {
        Destroy(this);
        this.gameObject.SetActive(false);
    }

    protected void StartAni()
    {
        animator.enabled = true;
    }

    protected void StopAni()
    {
        animator.enabled = false;
        spriteRenderer.sprite = sprite;
    }

    public virtual void Init()
    {
        EraseShadow();

        idx = 0;
        textMesh.text = "";
        childTextMesh.text = textMesh.text;
        gameObject.layer = 1;
        SetNullPolygon();
        polygonCollider2D.enabled = true;
        spriteRenderer.sortingLayerName = "Default";
        spriteRenderer.color = Color.white;
        spriteRenderer.sortingOrder = -Mathf.RoundToInt(transform.position.y * 100);
        StopAni();
        tag = "Wall";
        objectPosition = transform.position;
    }

    private void SetNullPolygon()
    {
        polygonCollider2D.pathCount = 1;
        polygonCollider2D.SetPath(0, nullPolygon);
    }

    protected void SetSpriteAndCollider()
    {
        if (sprite)
        {
            spriteRenderer.sprite = sprite;
            List<Vector2> list = new List<Vector2>();
            int num = sprite.GetPhysicsShapeCount();
            polygonCollider2D.pathCount = num;
            for (int i = 0; i < num; i++)
            {
                sprite.GetPhysicsShape(i, list);
                polygonCollider2D.SetPath(i, list.ToArray());
            }
            polygonCollider2D.isTrigger = false;
        }
        else
        {
            EraseShadow();
        }
    }

    public void SetAnimate()
    {
        isAnimate = false;
        StopAni();
    }

    public virtual bool GetAvailable()
    {
        return isAvailable;
    }

    public virtual void SetAvailable()
    {
        isAvailable = !isAvailable;
    }

    public virtual bool Active()
    {
        if (!isAvailable)
            return false;
        isActive = true;
        return true;
    }

    public virtual void IndicateInfo() { }

    public virtual void DeIndicateInfo() { }

    public virtual void Delete() { DestroyAndDeactive(); }

    public void LoadAwake()
    {
        Awake();
    }
    #region UnityFunc
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        shadowRenderer = this.transform.GetComponentsInChildren<SpriteRenderer>()[1];
        animator = GetComponent<Animator>();
        boxCollider = GetComponent<BoxCollider2D>();
        rigidbody2D = GetComponent<Rigidbody2D>();
        rigidbody2D.bodyType = RigidbodyType2D.Kinematic;
        textMesh = GetComponentInChildren<TextMesh>();
        childTextMesh = textMesh.transform.GetChild(0).GetComponent<TextMesh>();
        polygonCollider2D = GetComponent<PolygonCollider2D>();
        nullPolygon = new Vector2[4] { new Vector2(-.1f, -.1f), new Vector2(.1f, -.1f), new Vector2(.1f, .1f), new Vector2(-.1f, .1f) };
        clickableBoxPolygon = new Vector2[4] { new Vector2(-.25f, 0), new Vector2(.25f, 0), new Vector2(.25f, .5f), new Vector2(-.25f, .5f) };
    }
    #endregion
}

public class NoneRandomSpriteObject : CustomObject
{
    public override void Init()
    {
        base.Init();
        isAnimate = false;
        if (sprites != null)
            sprite = sprites[0];
        else
            sprite = null;
        SetSpriteAndCollider();
        ShadowDrawing();
    }

}

public class RandomSpriteObject : CustomObject
{
    public override void Init()
    {
        base.Init();
        isAnimate = false;
        if (sprites != null && sprites.Length != 0)
        { 
            idx = Random.Range(0, sprites.Length);
            sprite = sprites[idx];
        }
        else
            sprite = null;
        SetSpriteAndCollider();
        ShadowDrawing();
    }

}

public class PushBox : RandomSpriteObject
{
    Vector2 oldPosition;
    Vector2 dir;
    Vector3 offset;
    public override void Init()
    {
        base.Init();
        isActive = false;
        isAvailable = true;
        objectType = ObjectType.PUSHBOX;
        offset = new Vector3(0, sprites[0].bounds.size.y * 0.5f, 0);
    }
    //public override void SetAvailable()
    //{
    //    isAvailable = true;
    //}
    //public override bool Active()
    //{
    //    oldPosition = transform.position;
    //    isActive = true;
    //    dir = offset + transform.position - PlayerManager.Instance.GetPlayerPosition();
    //    dir.Normalize();
    //    rigidbody2D.bodyType = RigidbodyType2D.Dynamic;
    //    StartCoroutine(CoroutinePushed(dir));

    //    return true;
    //}
    //TODO : 캐릭터가 밀리는 문제가 발생
    IEnumerator CoroutinePushed(Vector2 direction)
    {
        float speed = 20;
        rigidbody2D.velocity = speed * direction;
        float time = 0.1f;
        Vector2 start = rigidbody2D.velocity;
        while (rigidbody2D.velocity.sqrMagnitude > 1)
        {
            rigidbody2D.velocity = Vector2.Lerp(rigidbody2D.velocity, Vector2.zero, Time.deltaTime / time);
            yield return YieldInstructionCache.WaitForSeconds(Time.fixedDeltaTime);
        }
        StopMove();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy") && isActive)
            Attack(collision);
    }

    void Attack(Collision2D collision)
    {
        collision.gameObject.GetComponent<Enemy>().Attacked(dir, transform.position, 1, 100, 0);
        StopMove();
    }

    void StopMove()
    {
        AStar.TileGrid.Instance.Bake(spriteRenderer.sprite,oldPosition);
        AStar.TileGrid.Instance.Bake(spriteRenderer);
        rigidbody2D.bodyType = RigidbodyType2D.Kinematic;
        spriteRenderer.sortingOrder = -Mathf.RoundToInt(transform.position.y * 100);
        isActive = false;
    }
}

public class Spawner : RandomSpriteObject
{
    int spawnCount;
    int gage;
    int eachGage;
    public override void Init()
    {
        base.Init();
        isActive = false;
        isAvailable = false;
        polygonCollider2D.enabled = false;

        gameObject.layer = 0;
        spawnCount = Random.Range(2, 4);
    }
    public override bool Active()
    {
        if(!isActive)
        {
            gage = RoomManager.Instance.GetGage();
            eachGage = gage / spawnCount;
            isActive = true;
        }
        spawnCount--;
        if (spawnCount < 0)
            return false;
        StartCoroutine(SpawnProcess());
        return true;
    }
    IEnumerator SpawnProcess()
    {
        int count = eachGage;
        if(spawnCount == 0)
        {
            count = RoomManager.Instance.GetGage();
        }
        while (count > 0)
        {
            Spawn(ref count);
            yield return YieldInstructionCache.WaitForSeconds(.5f);
        }
    }
    void Spawn(ref int count)
    {
        EnemyData enemyData = EnemyManager.Instance.GetEnemy(false);
        count -= enemyData.Price;
        RoomManager.Instance.SpawndWithGage(enemyData.Price);
        EnemyManager.Instance.Generate(enemyData);
    }
}

public class Door : RandomSpriteObject
{
    int isLock;
    bool isHorizon;
    Sprite openSprite;
    Sprite closeSprite;
    GameObject[] doorArrows;
    public bool objectAssigned;

    public override void Init()
    {
        base.Init();
        objectType = ObjectType.NONE;
        objectAssigned = false;
    }
    public void Init(Sprite openSprite, Sprite closeSprite, GameObject[] doorArrows)
    {
        Init();
        this.isLock = 0;
        this.openSprite = openSprite;
        this.closeSprite = closeSprite;
        this.doorArrows = doorArrows;
        sprite = openSprite;
        SetCollision();
        EraseShadow();
    }
    public void SetCollision()
    {
        spriteRenderer.sprite = sprite;
        List<Vector2> list = new List<Vector2>();
        int num = sprite.GetPhysicsShapeCount();
        polygonCollider2D.pathCount = num;
        for (int i = 0; i < num; i++)
        {
            sprite.GetPhysicsShape(i, list);
            polygonCollider2D.SetPath(i, list.ToArray());
        }
        polygonCollider2D.isTrigger = false;
    }
    public void Lock()
    {
        isLock++;
        sprite = closeSprite;
        isAvailable = true;
        StopAni();
        SetCollision();
    }
    public void UnLock()
    {
        isLock--;
        if (isLock <= 0)
        {
            sprite = openSprite;
            isAvailable = false;
        }
    }
    public override void SetAvailable()
    {
        isAvailable = false;
    }
    public override bool Active()
    {
        if (isLock <= 0)
            return false;
        UIManager.Instance.OpenWarningUI();
        isLock--;
        isAvailable = false;
        sprite = openSprite;

        SetCollision();
        return true;
    }
    public void Open()
    {
        if (isLock > 0)
        {
            isAvailable = true;
            return;
        }
        doorArrows[0].SetActive(true);
        doorArrows[1].SetActive(true);
        sprite = openSprite;
        SetCollision();
    }
    public void Close()
    {
        isAnimate = true;
        StartAni();
        doorArrows[0].SetActive(false);
        doorArrows[1].SetActive(false);
        if (!isHorizon)
        {
            animator.SetTrigger("door_horizon");
        }
        else
        {
            animator.SetTrigger("door_vertical");
        }
        sprite = closeSprite;
        SetCollision();
    }

    public void SetAxis(bool _isHorizon)
    {
        isHorizon = _isHorizon;
    }
    public bool GetHorizon()
    {
        return isHorizon;
    }
    public void DestroySelf()
    {
        for(int i=0;i< doorArrows.Length;i++)
        {
            doorArrows[i].SetActive(false);
        }
        doorArrows = null;
    }
}

public class Alert : RandomSpriteObject
{
    public delegate void Del(Vector3 _position, object obj, float amount, Character owner);
    Del callback;
    object temporary;
    Character owner;
    float amount;
    int type = 0;
    bool tutorial;
    public override void Init()
    {
        base.Init();
        isAvailable = false;
        polygonCollider2D.SetPath(0, null);
        objectType = ObjectType.NONE;
        gameObject.layer = 0;
    }
    public void Init(Del _call, object temporary, float amount, int type,Character owner)
    {
        Init();
        callback += _call;
        this.temporary = temporary;
        this.type = type;
        this.amount = amount;
        this.owner = owner;
    }
    public override bool Active()
    {
        base.Active();
        StartAni();
        isAnimate = true;
        if (type == 0)
        {
            animator.SetTrigger("skull_alert");
        }
        else
        {
            animator.SetTrigger("circle_alert");
        }
        StartCoroutine(CheckAnimate());

        return true;
    }
    IEnumerator CheckAnimate()
    {
        while (true)
        {
            if (!isAnimate)
                break;
            yield return YieldInstructionCache.WaitForEndOfFrame;
        }

        callback(transform.position, temporary, amount, owner);
        DestroyAndDeactive();
    }
}

public class PortalTutorial : RandomSpriteObject
{
    GameObject obj;
    public override void Init()
    {
        base.Init();
        isActive = false;
        isAvailable = true;
        objectType = ObjectType.PORTAL;
        gameObject.layer = 9;
    }

    public override void SetAvailable()
    {
    }
    public void Possible()
    {
        isAvailable = true;
    }

    public override bool Active()
    {
        if (!isAvailable)
            return false;
        base.Active();
        isAvailable = false;
        SceneDataManager.SetNextScene("InGameScene");
        SceneManager.LoadScene("LoadingScene");

        return true;
    }

}

public class ItemBox : RandomSpriteObject
{
    Item innerObject;

    public override void Init()
    {
        base.Init();
        isActive = false;
        isAvailable = true;
        objectType = ObjectType.ITEMBOX;
    }
    public void Init(Item _item)
    {
        Init();
        innerObject = _item;
        innerObject.gameObject.SetActive(false);
    }

    public override void SetAvailable()
    {
        if(innerObject==null)
        {
            ItemManager.Instance.SetItemBox(this);
        }
    }
    public override bool Active()
    {
        if (!base.Active())
            return false;
        if (GameDataManager.Instance.GetKey() <= 0)
            return false;
        GameDataManager.Instance.UseKey();
        isAvailable = false;
        innerObject.gameObject.SetActive(true);
        ItemManager.Instance.CreateItem(innerObject, this.transform.position);
        UtilityClass.Invoke(this, DestroyAndDeactive, 3);
        return true;
    }

    public void DestroySelf()
    {
        if (typeof(Weapon) != innerObject.GetType())
        {
            Delete();
        }
        DestroyAndDeactive();
    }

    private void OnMouseDown()
    {
        bool success = Active();
        if(success)
        {
            ControllerUI.Instance.IsTouched();
        }
    }

    public override void Delete()
    {
        base.Delete();
        if (innerObject == null)
            return;
        innerObject.transform.parent = null;
        innerObject.gameObject.SetActive(false);
    }

}

public class ItemContainer : RandomSpriteObject
{
    Item innerObject;
    bool isCoin;
    public override void Init()
    {
        base.Init();
        polygonCollider2D.enabled = false;
        polygonCollider2D.isTrigger = true;
        isActive = false;
        isAvailable = true;
        isAnimate = true;
        isCoin = false;
        objectType = ObjectType.NONE;
        tag = "Untagged";
        textMesh.text = "";
        childTextMesh.text = textMesh.text;
        gameObject.layer = 9;
    }

    public void Init(Item _item)
    {
        Init();
        innerObject = _item;
        sprite = innerObject.GetSprite();
        ReAlign();
        polygonCollider2D.SetPath(0, clickableBoxPolygon);
    }

    public void IsCoin()
    {
        isCoin = true;
    }

    void ReAlign()
    {
        innerObject.transform.parent = transform;
        innerObject.transform.localPosition = Vector3.zero;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Interactor"))
        {
            if (innerObject.GetType() != typeof(Coin) && innerObject.GetType() != typeof(Key) || !isAvailable)
                return;
            DettachDestroy();
            if (this && innerObject)
                innerObject.Active();
        }
    }

    public override bool Active()
    {
        if (!isAvailable)
            return false;
        if (innerObject.GetType() == typeof(Weapon))
        {
            isAvailable = false;

            bool check = PlayerManager.Instance.GetPlayer().GetWeaponManager().PickAndDropWeapon(innerObject);
            if (check)
            {
                DestroyAndDeactive();
                return true;
            }
            isAvailable = true;
        }
        else if (innerObject.GetType() == typeof(UsableItem))
        {
            isAvailable = false;
            innerObject.Active();
            DestroyAndDeactive();
            return true;
        }
        else if(innerObject.GetType() == typeof(Ammo))
        {
            if (!(innerObject as Ammo).isCanFill())
                return false;
            innerObject.Active();
            isAvailable = false ;
            DestroyAndDeactive();
            return true;
        }

        return false;
    }

    public void SubAcitve()
    {
        if (innerObject != null)
        {
            innerObject.SubActive();
        }
    }

    public override void IndicateInfo()
    {
        textMesh.text = innerObject.GetName();
        childTextMesh.text = textMesh.text;
        if (innerObject.GetType() == typeof(Weapon))
        {
            WeaponInfo info = ((Weapon)innerObject).info;
            IngameNoticeManager.Instance.ShowInfoNotice(info);
            textMesh.color = CommonConstants.Instance.RATING_TXT_COLOR[(int)info.rating - 1];
            //childTextMesh.color = CommonConstants.Instance.RATING_TXT_COLOR[(int)info.rating - 1];
        }
        else if (innerObject.GetType() == typeof(UsableItem))
        {
            if(typeof(MiscItemInfo) == ((UsableItem)innerObject).GetInfoType())
            {
                IngameNoticeManager.Instance.ShowInfoNotice(((UsableItem)innerObject).GetUsableItemInfo());
                textMesh.color = CommonConstants.Instance.RATING_TXT_COLOR[(int)innerObject.GetRating() - 1];
                //childTextMesh.color = CommonConstants.Instance.RATING_TXT_COLOR[(int)innerObject.GetRating() - 1];
            }
        }
    }

    public override void DeIndicateInfo()
    {
        textMesh.text = "";
        childTextMesh.text = textMesh.text;
    }

    public override bool GetAvailable()
    {
        if (isCoin)
            return false;
        return base.GetAvailable();
    }

    public void DettachDestroy()
    {
        if (innerObject != null)
        {
            innerObject.transform.parent = null;
        }
        DestroyAndDeactive();
    }

    public void DestroySelf()
    {
        // Debug.Log("inner Type : " + innerObject.GetType());
        // destroy말고 오브젝트 풀에서 회수 처리 일괄적으로 하는 것 더 생길 수 있겠지만
        // 일단 무기만 적용, 무기가 아닌 거(코인)이면 destroy
        if (typeof(Weapon) != innerObject.GetType())
        {
            innerObject.transform.parent = null;
            innerObject.gameObject.SetActive(false);
        }
        DettachDestroy();
    }

    private void OnMouseDown()
    {
        bool success = Active();
        if (success)
        {
            ControllerUI.Instance.IsTouched();
        }
    }

    public override void Delete()
    {
        base.Delete();
        if (innerObject == null)
            return;
        innerObject.transform.parent = null;
        innerObject.gameObject.SetActive(false);
    }
}

public class SubStation : NoneRandomSpriteObject
{
    public override bool Active()
    {
        return base.Active();
    }
}

public class NPC : NoneRandomSpriteObject
{
    public override void Init()
    {
        base.Init();
        isAnimate = true;
        isAvailable = true;
        isActive = false;
    }
}

public class Statue : RandomSpriteObject
{
    public override void Init()
    {
        base.Init();
        objectType = ObjectType.STATUE;
        isAvailable = true;
    }
    public override bool Active()
    {
        if (!base.Active())
            return false;
        isAvailable = false;
        PlayerBuffManager.Instance.ApplyStatueBuff(idx);
        return true;
    }
}

public class Astrologer : NPC
{
    public override void Init()
    {
        base.Init();
        isAvailable = true;
        StartAni();
        objectType = ObjectType.NPC;
    }
    public override void SetAvailable()
    {
        animator.SetTrigger("Astrologer");
    }
    public override bool Active()
    {
        if(base.Active())
        {
            if (GameDataManager.Instance.GetCoin() < 10)
                return false;
            GameDataManager.Instance.ReduceCoin(10);
            ParticleManager.Instance.PlayParticle("Twinkle", transform.position);
            isAvailable = false;
            PlayerBuffManager.Instance.ApplyAstrologerBuff();
            return true;
        }
        return false;
    }
}

public class TrapBox : RandomSpriteObject
{
    Item innerObject;

    public override void Init()
    {
        base.Init();
        isActive = false;
        isAvailable = true;
        objectType = ObjectType.TRAPBOX;
    }
    public void Init(Item _item)
    {
        Init();
        innerObject = _item;
        innerObject.gameObject.SetActive(false);
    }

    public override void SetAvailable()
    {
        if (innerObject == null)
        {
            ItemManager.Instance.SetTrapBox(this);
        }
    }
    public override bool Active()
    {
        if (!base.Active())
            return false;

        isAvailable = false;
        innerObject.gameObject.SetActive(true);
        ItemManager.Instance.CreateItem(innerObject, this.transform.position);
        UtilityClass.Invoke(this, DestroyAndDeactive, 3);
        RoomManager.Instance.Trap();
        return true;
    }

    public void DestroySelf()
    {
        if (typeof(Weapon) != innerObject.GetType())
        {
            Delete();
        }
        DestroyAndDeactive();
    }

    private void OnMouseDown()
    {
        bool success = Active();
        if (success)
        {
            ControllerUI.Instance.IsTouched();
        }
    }

    public override void Delete()
    {
        base.Delete();
        if (innerObject == null)
            return;
        innerObject.transform.parent = null;
        innerObject.gameObject.SetActive(false);
    }
}

public class NoneBox : NoneRandomSpriteObject
{
    public override void Init()
    {
        base.Init();
        polygonCollider2D.enabled = false;
        polygonCollider2D.isTrigger = false;
        isActive = false;
        isAvailable = false;
        isAnimate = true;
        objectType = ObjectType.NONE;
        textMesh.text = "";
    }
}
