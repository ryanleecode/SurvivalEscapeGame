﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class PlayerData : MonoBehaviour {
    public Tile CurrentTile;
    public Vector3 position;
    public bool IsPerformingAction;
    public int NourishmentLevel;
    public float NourishmentStatus;
    public float NourishmentDecayRate;
    public float HealthRegeneration;
    public float MaximumHealth;
    public float Health;
    public float MaximumStamina;
    public float Stamina;
    public float MovementSpeed;
    public float StaminaRegeneration;
    public bool Alive;
    public bool IsWeaponEquipped;
    public bool IsShieldEquipped;
    public int Direction = 1;
    public float Damage;
    public float AttackStaminaCost;
    public bool IsAttackOnCooldown;
    public float AttackCooldown = 0.33f;

    private float MaximumNourishmentStatus;

    public Dictionary<PlayerActions, bool> PerformingAction;

    [SerializeField]
    private GameObject Model;

    [SerializeField]
    private GameObject HealthBar;
    [SerializeField]
    private GameObject NourishmentBar;
    [SerializeField]
    private GameObject StaminaBar;
    [SerializeField]
    private GameObject NourishmentText;

    public const int NumItemSlots = 16;
    public const int NumCraftingSlots = 6;
    [SerializeField]
    private GameObject SlotPanel;
    [SerializeField]
    private GameObject InventorySlot;
    [SerializeField]
    private GameObject InventoryItem;
    [SerializeField]
    private GameObject ActivePanel;
    [SerializeField]
    private GameObject CraftingPanel;
    [SerializeField]
    public GameObject GUIText;

    private Dictionary<string, Item> Inventory;
    public static Dictionary<string, Item> CraftingInventory = new Dictionary<string, Item>();
    public static List<GameObject> Slots = new List<GameObject>();
    public static List<GameObject> CraftingSlots = new List<GameObject>();
    public static List<GameObject> Items = new List<GameObject>();
    public static List<GameObject> CraftingItems = new List<GameObject>();

    // Use this for initialization
    private void Start() {
    }

    public void LateStart() {
        this.NourishmentLevel = 1;
        this.MaximumHealth = NourishmentLevels.BaseMaximumHealth[this.NourishmentLevel];
        this.Health = NourishmentLevels.BaseMaximumHealth[this.NourishmentLevel];
        this.MaximumStamina = NourishmentLevels.BaseMaximumStamina[this.NourishmentLevel];
        this.Stamina = NourishmentLevels.BaseMaximumStamina[this.NourishmentLevel];
        this.StaminaRegeneration = NourishmentLevels.BaseStaminaRegeneration[this.NourishmentLevel];
        this.MovementSpeed = NourishmentLevels.BaseMovementSpeed[this.NourishmentLevel];
        this.HealthRegeneration = NourishmentLevels.BaseHealthRegeneration[this.NourishmentLevel];
        this.NourishmentStatus = NourishmentLevels.NourishmentThreshold[this.NourishmentLevel] / 2;
        this.NourishmentDecayRate = NourishmentLevels.NourishmentDecayRate[this.NourishmentLevel];
        this.position = this.GetComponent<Transform>().position;
        this.Alive = true;
        this.IsPerformingAction = false;
        IsAttackOnCooldown = false;
        Damage = 20.0f;
        AttackStaminaCost = 10.0f;
        this.PerformingAction = new Dictionary<PlayerActions, bool>() {
            {PlayerActions.Move, false },
            {PlayerActions.Dig, false },
            {PlayerActions.BuildTent, false },
            {PlayerActions.Attack, false },
            {PlayerActions.Eat, false }
        };
        this.Inventory = new Dictionary<string, Item>();
        this.UpdateTileVisibility();
        this.HealthBar.GetComponent<Image>().fillAmount = this.Health / this.MaximumHealth;
        this.MaximumNourishmentStatus = 0f;
        for (int i = -2; i <= 2; i++) {
            this.MaximumNourishmentStatus += NourishmentLevels.NourishmentThreshold[i];
        }
        SlotInput.Pd = this;
        for (int i = 0; i < PlayerData.NumItemSlots + PlayerData.NumCraftingSlots; i++) {
            GameObject Is = Instantiate(InventorySlot);
            PlayerData.Slots.Add(Is);
            PlayerData.Slots[i].AddComponent<SlotInput>();
            PlayerData.Slots[i].GetComponent<SlotInput>().SlotID = i;
            if (i >= PlayerData.NumItemSlots) {
                PlayerData.CraftingSlots.Add(Is);
                PlayerData.CraftingSlots[i - PlayerData.NumItemSlots] = PlayerData.Slots[i];
                PlayerData.Slots[i].transform.SetParent(CraftingPanel.transform, false);
                PlayerData.CraftingSlots[i - PlayerData.NumItemSlots].GetComponent<SlotInput>().CraftingSlot = true;
            } else {
                PlayerData.Slots[i].transform.SetParent(this.SlotPanel.transform, false);
                PlayerData.Slots[i].GetComponent<SlotInput>().CraftingSlot = false;
            }
        }
        this.AddItem(new Shovel(++Item.IdCounter, true), this.GetInventory(), NumItemSlots, Slots, Items);
        this.AddItem(new Tent(++Item.IdCounter, true), this.GetInventory(), NumItemSlots, Slots, Items);
        this.AddItem(new Coconut(++Item.IdCounter, true), this.GetInventory(), NumItemSlots, Slots, Items);
    }

    // Update is called once per frame
    private void Update() {
        this.MaximumStamina = NourishmentLevels.BaseMaximumStamina[this.NourishmentLevel];
        this.MovementSpeed = NourishmentLevels.BaseMovementSpeed[this.NourishmentLevel];
        this.HealthRegeneration = NourishmentLevels.BaseHealthRegeneration[this.NourishmentLevel];
        this.UpdateHealth();
        this.ApplyNourishmentDecay();
        this.UpdateNourishmentStatus();
        this.UpdateStamina();
    }

    public void UpdateTileVisibility() {

        if (this.GetComponentInParent<Model>().IsDay()) {
            this.UpdateTileVisibilityDay();
        } else {
            this.UpdateTileVisibilityNight();
        }
        Model m = this.GetComponentInParent<Model>();
        m.GetGameGrid().SetNormals(m.GetMeshBuilder().Norms.ToList());
    }

    private void UpdateTileVisibilityDay() {
        foreach (Tile t in this.GetCurrentTile().GetExtendedNeighbours(4)) {
            if (t.CurrentGameObject != null && t.CurrentGameObject != this.gameObject) {
                SpriteRenderer sr = t.CurrentGameObject.GetComponent<SpriteRenderer>();
                sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 0);
            }
        }
        foreach (Tile t in this.GetCurrentTile().GetExtendedNeighbours(3)) {
            for (int j = 0; j < t.NormIdx.Length; j++) {
                t.Norms[t.NormIdx[j]].Set(0f, 0f, 0f);
                t.SetActive(false);
                foreach (KeyValuePair<ItemList, GameObject> entry in t.Structures) {
                    entry.Value.SetActive(false);
                }
                if (t.CurrentGameObject != null && t.CurrentGameObject != this.gameObject) {
                    SpriteRenderer sr = t.CurrentGameObject.GetComponent<SpriteRenderer>();
                    sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 255);
                }                
            }
        }
        foreach (Tile t in this.GetCurrentTile().GetExtendedNeighbours(2)) {
            for (int j = 0; j < t.NormIdx.Length; j++) {
                t.Norms[t.NormIdx[j]].Set(0f, 0f, -1f);
                this.GetCurrentTile().SetActive(true);
                foreach (KeyValuePair<ItemList, GameObject> entry in t.Structures) {
                    entry.Value.SetActive(true);
                }
            }
            if (t.CurrentGameObject != null && t.CurrentGameObject != this.gameObject) {
                SpriteRenderer sr = t.CurrentGameObject.GetComponent<SpriteRenderer>();
                sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 255);
            }
        }
    }

    private void UpdateTileVisibilityNight() {
        foreach (Tile t in this.GetCurrentTile().GetExtendedNeighbours(2)) {
            for (int j = 0; j < t.NormIdx.Length; j++) {
                t.Norms[t.NormIdx[j]].Set(0f, 0f, 0f);
                t.SetActive(false);
            }
        }
        foreach (Tile t in this.GetCurrentTile().GetExtendedNeighbours(1)) {
            for (int j = 0; j < t.NormIdx.Length; j++) {
                t.Norms[t.NormIdx[j]].Set(0f, 0f, -1f);
                this.GetCurrentTile().SetActive(true);
            }
        }
    }

    public void UpdateStamina() {
        this.StaminaRegeneration = NourishmentLevels.BaseStaminaRegeneration[this.NourishmentLevel];
        if (CurrentTile.Structures.ContainsKey(ItemList.Tent)) {
            this.StaminaRegeneration += 5.0f * Time.deltaTime;
        }
        if (this.Stamina < this.MaximumStamina) {
            this.Stamina = System.Math.Min(MaximumStamina, this.Stamina + this.StaminaRegeneration);
        }
        this.StaminaBar.GetComponent<Image>().fillAmount = this.Stamina / this.MaximumStamina;
    }

    public void UpdateHealth() {
        if (this.Health < MaximumHealth) {
            this.Health = System.Math.Min(MaximumHealth, this.Health + this.HealthRegeneration);
        }
        this.HealthBar.GetComponent<Image>().fillAmount = this.Health / MaximumHealth;
    }

    public void ApplyNourishmentDecay() {
        if (this.NourishmentStatus > 0) {
            this.NourishmentStatus = this.NourishmentStatus - this.NourishmentDecayRate;
        } else if (this.NourishmentLevel == -2) {
            this.NourishmentStatus = 0;
        }
        NourishmentText.GetComponent<Text>().text = this.NourishmentLevel.ToString();
    }

    public void UpdateNourishmentStatus() {
        if (this.NourishmentLevel == 2 && this.NourishmentStatus > NourishmentLevels.NourishmentThreshold[2]) {
            this.NourishmentStatus = NourishmentLevels.NourishmentThreshold[2];
        }
        if (this.NourishmentLevel > -2 && this.NourishmentStatus <= 0) {
            this.NourishmentLevel--;
            this.NourishmentStatus = NourishmentLevels.NourishmentThreshold[this.NourishmentLevel] + this.NourishmentStatus;
        } else if (this.NourishmentLevel < 2 && this.NourishmentStatus > NourishmentLevels.NourishmentThreshold[this.NourishmentLevel]) {
            this.NourishmentLevel++;
            this.NourishmentStatus = NourishmentLevels.NourishmentThreshold[this.NourishmentLevel] - this.NourishmentStatus;
        }
        this.NourishmentBar.GetComponent<Image>().fillAmount = this.NourishmentStatus / NourishmentLevels.NourishmentThreshold[this.NourishmentLevel];
    }

    public void DamagePlayer(float damage) {
        float oldHealth = Health;
        this.Health = this.Health - damage;
        GUIText.GetComponent<Text>().text = "You took " + damage + " damage! Health: " + oldHealth.ToString("F2") + "->" + Health.ToString("F2");
        if (this.Health <= 0) {
            Transform camera = this.gameObject.transform.GetChild(0);
            camera.SetParent(this.gameObject.transform.parent.gameObject.transform);
            UpdateHealth();
            Destroy(this.gameObject);
            this.Alive = false;
            GUIText.GetComponent<Text>().text = "You are dead!!!";
        }
    }

    public bool AddItem(Item it) {
        return AddItem(it, this.GetInventory(), NumItemSlots, Slots, Items);
    }

    public bool AddItem(Item it, Dictionary<string, Item> inventory, int maximumSlotNumber, List<GameObject> SlotContainer,
        List<GameObject> ItemContainer) {
        return AddItem(it, inventory, maximumSlotNumber, SlotContainer, ItemContainer, false);
    }

    public bool AddItem(Item it, Dictionary<string, Item> inventory, int maximumSlotNumber, List<GameObject> SlotContainer, 
        List<GameObject> ItemContainer, bool ignoreContains) {
        if (inventory.Count > maximumSlotNumber) {
            return false;
        }
        if (!inventory.ContainsKey(it.GetName())) {
            inventory.Add(it.GetName(), it);
            for (int i = 0; i < maximumSlotNumber; i++) {
                if (SlotContainer[i].transform.childCount == 0) {
                    GameObject item = Instantiate(this.InventoryItem);
                    ItemContainer.Add(item);
                    inventory[it.GetName()].ItemObject = item;
                    item.transform.SetParent(SlotContainer[i].transform, false);
                    item.transform.localPosition = Vector3.zero;
                    item.GetComponent<Image>().sprite = inventory[it.GetName()].Icon;
                    item.transform.GetChild(0).GetComponent<Text>().text = it.GetQuantity().ToString();
                    item.GetComponent<ItemInput>().Item = it;
                    if (SlotContainer[i].GetComponent<SlotInput>().CraftingSlot) {
                        it.Slot = i + NumItemSlots;
                    } else {
                        it.Slot = i;
                    }
                    SlotContainer[i].GetComponent<SlotInput>().StoredItem = it;
                    if (inventory == this.GetInventory()) {
                        FillActiveSlot(it, item);
                    }
                    return true;
                }
            }
        } else if ((PlayerData.CraftingInventory.ContainsKey(it.GetName()) && this.GetInventory()[it.GetName()].GetQuantity() 
                + PlayerData.CraftingInventory[it.GetName()].GetQuantity() < inventory[it.GetName()].MaximumQuantity) 
            || (PlayerData.CraftingInventory.ContainsKey(it.GetName()) == false && this.GetInventory()[it.GetName()].GetQuantity()
                < inventory[it.GetName()].MaximumQuantity) || ignoreContains) {
            Item tmp = inventory[it.GetName()];
            tmp.SetQuantity(tmp.GetQuantity() + 1);
            tmp.ItemObject.transform.GetChild(0).GetComponent<Text>().text = tmp.GetQuantity().ToString();
            return true;
        } else {
            GUIText.GetComponent<Text>().text = "Maximum quantity reached!";
            Debug.Log("You've reached the maximum quantity of this item!");
        }
        return false;
    }

    public void FillActiveSlot(Item it, GameObject item) {
        int numActive = ActivePanel.transform.childCount;
        for (int j = 0; j < numActive; j++) {
            Transform activeSlot = ActivePanel.transform.GetChild(j);
            if (it.GetType().IsSubclassOf(typeof(ActionItem)) && activeSlot.GetComponent<ActiveInput>().Item == null) {
                activeSlot.GetComponent<ActiveInput>().Item = it;
                activeSlot.GetComponent<ActiveInput>().Slot = j;
                activeSlot.GetChild(0).GetComponent<Image>().sprite = Item.BorderSprite;
                activeSlot.GetChild(0).GetComponent<Image>().color = new Color(255, 255, 255, 255);
                activeSlot.GetChild(1).GetComponent<Image>().sprite = item.GetComponent<Image>().sprite;
                activeSlot.GetChild(1).GetComponent<Image>().color = new Color(255, 255, 255, 255);
                activeSlot.GetChild(2).GetComponent<Text>().text = ActiveInput.Hotkeys[activeSlot.GetComponent<ActiveInput>().Slot].ToString();
                ActionItem ai = (ActionItem)it;
                activeSlot.GetChild(3).GetComponent<Text>().text = ai.StaminaCost.ToString();
                it.ActiveContainer = activeSlot.gameObject;
                break;
            }
        }
    }

    public void RemoveItem(Item it, int quantity, Dictionary<string, Item> inventory) {
        if (inventory.ContainsKey(it.GetName())) {
            Item itInvent = inventory[it.GetName()];
            itInvent.SetQuantity(itInvent.GetQuantity() - quantity);
            itInvent.ItemObject.transform.GetChild(0).GetComponent<Text>().text = itInvent.GetQuantity().ToString();
            if (itInvent.GetQuantity() <= 0) {
                inventory.Remove(it.GetName());
                Destroy(it.ItemObject);
                if (it.GetType().IsSubclassOf(typeof(ActionItem)) && it.ActiveContainer != null) {
                    Destroy(it.ActiveContainer);
                    Model.GetComponent<Model>().CreateActivePanel();
                }
                
            }
        } else {
            Debug.Log("Item does not exist in player's invetory!");
        }
    }

    public void RemoveItem(Item it, Dictionary<string, Item> inventory) {
        RemoveItem(it, it.GetQuantity(), inventory);
    }

    public bool InventoryContains(string key) {
        if (this.Inventory.ContainsKey(key)) {
            return true;
        }
        return false;
    }

    public void SetCurrentTile(Tile t) {
        this.CurrentTile = t;
    }

    public Tile GetCurrentTile() {
        return this.CurrentTile;
    }

    public Dictionary<string, Item> GetInventory() {
        return this.Inventory;
    }
}
