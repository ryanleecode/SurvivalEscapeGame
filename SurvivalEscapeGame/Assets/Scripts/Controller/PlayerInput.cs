﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerInput : MonoBehaviour {
    public Vector3 Destination = new Vector3();
    [SerializeField]
    private PlayerData PlayerData;
    private int NeighbourIndex;
    [SerializeField]
    private GameObject InventoryPanel;

    private Vector3 previousPosition = Vector3.zero;

    [SerializeField]
    private GameObject ChannelingBarMask;

    private float LerpStep = 0.0f;
    private float AttackStep = 0.0f;

    public Text GUItext;

    private AudioSource DigSound;
    private AudioSource WalkSound;

    private AudioSource AttackSound;
    public AudioSource ItemPickup;

    private AudioSource Nourish;

    public Dictionary<string, Action> Actions = new Dictionary<string, Action>() {
    };

    // Use this for initialization
    private void Awake() {
        Actions.Add(Global.ItemNames[ItemList.Shovel], Dig);
        Actions.Add(Global.ItemNames[ItemList.Pickaxe], DigMountain);
        Actions.Add(Global.ItemNames[ItemList.Tent], BuildTent);
        Actions.Add(Global.ItemNames[ItemList.Coconut], EatCoconut);
        Actions.Add(Global.ItemNames[ItemList.Berry], EatBerry);
        Actions.Add(Global.ItemNames[ItemList.Cocoberry], EatCocoberry);
    }

    private void Start() {
        GUItext = GetPlayerData().GUIText.GetComponent<Text>();
        DigSound = this.GetComponents<AudioSource>()[0];
        WalkSound = this.GetComponents<AudioSource>()[1];
        AttackSound = this.GetComponents<AudioSource>()[2];
        ItemPickup = this.GetComponents<AudioSource>()[3];
        Nourish = this.GetComponents<AudioSource>()[4];
    }

    private void Update() {
        // Movement
        if (this.PlayerData == null)
            return;
        this.Movement(this.GetPlayerData().PerformingAction[PlayerActions.Move]);
        Digging();
        ToggleInventory();
        BuildingTent();
        EatingCoconut();
        EatingBerry();
        EatingCocoberry();
        if (Input.GetMouseButtonDown(1)) {
            Attack();
        }
        AttackCooldown();
    }

    private void Movement(bool exec) {
        if (!exec && !this.GetPlayerData().IsPerformingAction) {
            if (Input.GetAxisRaw("Vertical") > 0) {
                this.NeighbourIndex = (int)Sides.Top;
                this.Move(GetPlayerData().Direction);
            } else if (Input.GetAxisRaw("Vertical") < 0) {
                this.NeighbourIndex = (int)Sides.Bottom;
                this.Move(GetPlayerData().Direction);
            } else if (Input.GetAxisRaw("Horizontal") > 0) {
                this.NeighbourIndex = (int)Sides.Right;
                this.Move(1);
            } else if (Input.GetAxisRaw("Horizontal") < 0) {
                this.NeighbourIndex = (int)Sides.Left;
                this.Move(-1);
            }
        } else if (exec) {
            Tile enter = this.GetPlayerData().GetCurrentTile().GetNeighbours()[this.NeighbourIndex];
            float step = (this.GetPlayerData().MovementSpeed / enter.MovementCost) * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, this.Destination, step);
            if (transform.position.Equals(this.Destination)) {                                   
                this.GetPlayerData().SetCurrentTile(enter);
                this.GetPlayerData().position = GetPlayerData().CurrentTile.GetPosition() - Global.SmallOffset;
                GetPlayerData().CurrentTile.CurrentGameObject = this.gameObject;
                this.GetPlayerData().PerformingAction[PlayerActions.Move] = false;
                this.GetPlayerData().IsPerformingAction = false;
                this.GetPlayerData().UpdateTileVisibility();
                WalkSound.loop = false;
            }
        }
    }

    protected void Move(int direction) {
        Tile enter = this.GetPlayerData().GetCurrentTile().GetNeighbours()[this.NeighbourIndex];
        if (enter != null && enter.CurrentGameObject == null && enter.IsWalkable == true) {
            this.Destination = enter.GetPosition() - Global.SmallOffset;
            this.GetPlayerData().PerformingAction[PlayerActions.Move] = true;
            this.GetPlayerData().IsPerformingAction = true;
            this.GetPlayerData().CurrentTile.CurrentGameObject = null;
            enter.CurrentGameObject = this.gameObject;
            WalkSound.Play();
            WalkSound.loop = true;
        }
        Animator animCtrl = this.gameObject.GetComponent<Animator>();
            
            if (direction == 1) {
            GetPlayerData().Direction = 1;
            if (!(GetPlayerData().IsWeaponEquipped && GetPlayerData().IsShieldEquipped)) {
                animCtrl.ResetTrigger("MoveLeft");
                animCtrl.SetTrigger("MoveRight");
               
            }
        } else if (direction == -1) {
            GetPlayerData().Direction = -1;
            if (!(GetPlayerData().IsWeaponEquipped && GetPlayerData().IsShieldEquipped)) {
                animCtrl.ResetTrigger("MoveRight");
                animCtrl.SetTrigger("MoveLeft");
               
            }
        }
    }

    protected void Attack() {
        if (GetPlayerData().Stamina < GetPlayerData().AttackStaminaCost) {
            GUItext.text = "Not enough stamina! Need " + GetPlayerData().AttackStaminaCost + " stamina!!!";
            return;
        } else if (GetPlayerData().IsAttackOnCooldown == true) {
            float multi = Mathf.Lerp(0.0f, 1.0f, AttackStep);
            float asdf = AttackStep - (AttackStep * multi);
            GUItext.text = "Attack is on cooldown! " + asdf.ToString("F2") + " seconds remain.";
            return;
        } else if (GetPlayerData().IsPerformingAction) {
            GUItext.text = "Cannot attack while performing another action!";
            return;
        }
        AttackSound.Play();
        GetPlayerData().IsAttackOnCooldown = true;
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit, 100.0f)) {
            Animator animCtrl = this.gameObject.GetComponent<Animator>();
            Vector2 diff = ray.origin - GetPlayerData().position;
            if (!GetPlayerData().IsWeaponEquipped) {
                if (diff.x > 0) {
                    animCtrl.ResetTrigger("MoveRight");
                    animCtrl.SetTrigger("MoveRight");
                    animCtrl.SetTrigger("AttackNoWeaponRight");
                } else {
                    animCtrl.ResetTrigger("MoveLeft");
                    animCtrl.SetTrigger("MoveLeft");
                    animCtrl.SetTrigger("AttackNoWeaponLeft");
                }
            }
            GetPlayerData().Stamina -= GetPlayerData().AttackStaminaCost;
            Tile[] neigh = GetPlayerData().CurrentTile.GetNeighbours();
            if (Math.Abs(diff.x) > Math.Abs(diff.y)) {
                if (diff.x > 0 && neigh[2].CurrentGameObject != null) {
                    neigh[2].CurrentGameObject.GetComponent<EnemyData>().DamageEnemy(GetPlayerData().Damage);

                } else if (diff.x < 0 && neigh[1].CurrentGameObject != null) {
                    neigh[1].CurrentGameObject.GetComponent<EnemyData>().DamageEnemy(GetPlayerData().Damage);

                }
            } else {
                if (diff.y > 0 && neigh[0].CurrentGameObject != null) {
                    neigh[0].CurrentGameObject.GetComponent<EnemyData>().DamageEnemy(GetPlayerData().Damage);

                } else if (diff.y < 0 && neigh[3].CurrentGameObject != null) {
                    neigh[3].CurrentGameObject.GetComponent<EnemyData>().DamageEnemy(GetPlayerData().Damage);                   
                }
            }

        }
    }

    protected void AttackCooldown() {
        if (GetPlayerData().IsAttackOnCooldown) {
            float step = Mathf.Lerp(0.0f, 1.0f, AttackStep);
            AttackStep += Time.deltaTime / GetPlayerData().AttackCooldown;
            if (step >= 1.0f) {
                GetPlayerData().IsAttackOnCooldown = false;
                AttackStep = 0.0f;
            }
        }
    }

    private bool AItemFcns(PlayerActions pa, ItemList il) {
        ActionItem it = (ActionItem)this.GetPlayerData().GetInventory()[Global.ItemNames[il]];
        if (it == null)
            return false;
        bool exec = this.GetPlayerData().PerformingAction[pa];
        if (!exec && !this.GetPlayerData().IsPerformingAction && GetPlayerData().Stamina >= it.StaminaCost) {
            this.GetPlayerData().PerformingAction[pa] = true;
            this.GetPlayerData().IsPerformingAction = true;
            return true;
        }
        return false;
    }

    protected PlayerActions Channeling(ItemList il, PlayerActions pa) {
        if (this.GetPlayerData().InventoryContains(Global.ItemNames[il])
              && this.GetPlayerData().PerformingAction[pa]) {
            ActionItem ai = (ActionItem)(this.GetPlayerData().GetInventory()[Global.ItemNames[il]]);
            float step = Mathf.Lerp(0.0f, 1.0f, LerpStep);
            LerpStep += Time.deltaTime / ai.ChannelDuration;
            if (1.0f - LerpStep >= 0) {
                ChannelingBarMask.transform.GetChild(0).GetComponent<Image>().fillAmount = 1.0f - LerpStep;
                ChannelingBarMask.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "Channeling: "
                    + Math.Round((1.0f - LerpStep) * ai.ChannelDuration, 2);
            }
            if (step >= 1.0f) {
                this.GetPlayerData().PerformingAction[pa] = false;
                this.GetPlayerData().IsPerformingAction = false;
                LerpStep = 0.0f;
                ChannelingBarMask.SetActive(false);
                return pa;
            }
        }
        return PlayerActions.NotThisAction;
    }

    public void BuildTent() {
        if (AItemFcns(PlayerActions.BuildTent, ItemList.Tent)) {
            GUItext.text = "Building tent.";
            Debug.Log("Building Tent");
            ChannelingBarMask.SetActive(true);
            BuildingTent();
        }
    }

    protected void BuildingTent() {
        if (Channeling(ItemList.Tent, PlayerActions.BuildTent) == PlayerActions.BuildTent) {
            Tent ai = (Tent)(this.GetPlayerData().GetInventory()[Global.ItemNames[ItemList.Tent]]);
            ai.BuildTent(this.GetPlayerData());
        }
    }

    public void Dig() {
        if (GetPlayerData().CurrentTile.Type == TileType.Mountain) {
            GUItext.text = "Cannot use a shovel in a mountain!";
            return;
        }
        if (AItemFcns(PlayerActions.Dig, ItemList.Shovel)) {            
            GUItext.text = "Digging... This tile has been dug " + GetPlayerData().CurrentTile.DigCount + " times.";
            ChannelingBarMask.SetActive(true);
            DigSound.Play();
            Digging();            
        }
    }

    public void DigMountain() {
        if (GetPlayerData().CurrentTile.Type != TileType.Mountain) {
            GUItext.text = "Cannnot use a shovel when not in a mountain!";
            return;
        }
        if (AItemFcns(PlayerActions.Dig, ItemList.Shovel)) {
            GUItext.text = "Digging... This tile has been dug " + GetPlayerData().CurrentTile.DigCount + " times.";
            ChannelingBarMask.SetActive(true);
            DigSound.Play();
            Digging();
        }
    }

    protected void DiggingMountain() {
        if (Channeling(ItemList.Pickaxe, PlayerActions.Dig) == PlayerActions.Dig) {
            Pickaxe p = (Pickaxe)(this.GetPlayerData().GetInventory()[Global.ItemNames[ItemList.Pickaxe]]);
            p.Dig(this.GetPlayerData(), this);
        }
    }

    protected void Digging() {
        if (Channeling(ItemList.Shovel, PlayerActions.Dig) == PlayerActions.Dig) {
            Shovel s = (Shovel)(this.GetPlayerData().GetInventory()[Global.ItemNames[ItemList.Shovel]]);
            s.Dig(this.GetPlayerData(), this);
        }
    }

    public void EatCocoberry() {
        if (AItemFcns(PlayerActions.Eat, ItemList.Cocoberry)) {
            GUItext.text = "Consuming Cocobery.";
            ChannelingBarMask.SetActive(true);
            EatingCocoberry();
        }
    }

    protected void EatingCocoberry() {
        if (Channeling(ItemList.Cocoberry, PlayerActions.Eat) == PlayerActions.Eat) {
            Cocoberry cocoberry = (Cocoberry)(this.GetPlayerData().GetInventory()[Global.ItemNames[ItemList.Cocoberry]]);
            cocoberry.Eat(this.GetPlayerData());
            Nourish.Play();
        }
    }

    public void EatBerry() {
        if (AItemFcns(PlayerActions.Eat, ItemList.Berry)) {
            GUItext.text = "Consuming berry.";
            ChannelingBarMask.SetActive(true);
            EatingBerry();
        }
    }

    protected void EatingBerry() {
        if (Channeling(ItemList.Berry, PlayerActions.Eat) == PlayerActions.Eat) {
            Berry berry = (Berry)(this.GetPlayerData().GetInventory()[Global.ItemNames[ItemList.Berry]]);
            berry.Eat(this.GetPlayerData());
            Nourish.Play();
        }
    }

    public void EatCoconut() {
        if (AItemFcns(PlayerActions.Eat, ItemList.Coconut)) {
            GUItext.text = "Consuming coconut.";
            ChannelingBarMask.SetActive(true);
            EatingCoconut();
        }
    }

    protected void EatingCoconut() {
        if (Channeling(ItemList.Coconut, PlayerActions.Eat) == PlayerActions.Eat) {
            Coconut coco = (Coconut)(this.GetPlayerData().GetInventory()[Global.ItemNames[ItemList.Coconut]]);
            coco.Eat(this.GetPlayerData());
            Nourish.Play();
        }
    }

    public void SetPlayerData(PlayerData pd) {
        this.PlayerData = pd;
    }

    public PlayerData GetPlayerData() {
        return this.PlayerData;
    }

    public void ToggleInventory() {
        if (Input.GetKeyDown(KeyCode.I)) {
            InventoryPanel.SetActive(!InventoryPanel.activeSelf);
        }
    }


}


