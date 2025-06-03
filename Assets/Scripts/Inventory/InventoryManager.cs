using System;
using System.Linq;
using System.Linq.Expressions;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager instance { get; private set; }

    [SerializeField] private Item itemSelected;

    [SerializeField] GameObject buttonsActionRoot;
    [SerializeField] GameObject saveButton;

    [SerializeField] private ShowInventory inventoryUI;

    [SerializeField] Player player;
    [SerializeField] public InventorySO inventory;
    [SerializeField] public InventorySO chestInventory;
    [SerializeField] NoteInventorySO noteInventory;
    [SerializeField] GameObject notesRoot;
    [SerializeField] GameObject notesDiaryPanel;
    [SerializeField] GameObject notesDiaryPanelScroll;
    [SerializeField] GameObject unequipButton;
    [SerializeField] GameObject itemDescriptionPanel;
    [SerializeField] GameObject notesPanel;
    [SerializeField] GameObject notesPanelScroll;
    [SerializeField] private GameObject imagePanel;
    [SerializeField] private GameObject bookPanel;
    [SerializeField] private Events events;
    [SerializeField] public bool glitchDone = false;
    public event Action<bool> OnNote;

    public Item equippedItem;

    public bool isCombining { get; private set; }
    public bool chestOpened { get; private set; }
    private Item targetItemToCombine;
    public InputSystem_Actions _inputActions { get; private set; }

    [Header("Audio")]
    AudioSource inventoryAudioSource;
    [SerializeField]
    AudioClip tapeAudio;
    
    private void Awake()
    {
        if (instance == null)
            instance = this;
        _inputActions = new InputSystem_Actions();
        _inputActions.Chest.OpenClose.performed += ToggleChest;
        inventoryAudioSource = this.GetComponent<AudioSource>();
    }

    private void Start()
    {
        InitState(ActionStates.NOACTION);
    }

    private void OnDestroy()
    {
        _inputActions.Chest.OpenClose.performed -= ToggleChest;
        instance = null;
    }

    public void ItemSelected(Item item)
    {
        itemSelected = item;
        ChangeSelectedItem();
        if (!this.chestOpened)
        {
            ChangeState(ActionStates.SELECT_ACTION);
        }
        else
        {
            ChangeState(ActionStates.ACTION_CHEST_SELECT);
        }
        
    }

    private void ToggleActionsButtons(bool show)
    {
        for (int i = 0; i < buttonsActionRoot.transform.childCount; i++)
        {
            buttonsActionRoot.transform.GetChild(i).gameObject.SetActive(show);
        }
        OptionsAvailable();
    }

    private void OptionsAvailable()
    {
        if (itemSelected != null)
        {
            buttonsActionRoot.transform.GetChild(0).GetComponent<Button>().interactable = itemSelected.isUsable ? true : false;
            buttonsActionRoot.transform.GetChild(1).GetComponent<Button>().interactable = itemSelected.isEquipable ? true : false;
            buttonsActionRoot.transform.GetChild(2).GetComponent<Button>().interactable = itemSelected.isCombinable ? true : false;
        }
    }
    private void FillClearItemDescriptionPanel(string itemName, string itemDescription, bool clear)
    {
        if (clear)
        {
            itemDescriptionPanel.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "";
            itemDescriptionPanel.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = "";
        }
        else
        {
            itemDescriptionPanel.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = itemName;
            itemDescriptionPanel.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = itemDescription;
        }
    }

    #region FSM
    enum ActionStates { NOACTION, SELECT_ACTION, ACTION_USE, ACTION_COMBINE, ACTION_EQUIP_ITEM, ACTION_COMBINE_SELECT, SELECT_EQUIPPED, ACTION_UNEQUIP, CHEST_OPENED, ACTION_CHEST_SELECT }
    [SerializeField] ActionStates actionState;
    private void ChangeState(ActionStates newState)
    {
        Debug.Log($"---------------------- Entering from {actionState} to {newState} ------------------------");
        ExitState(actionState);

        Debug.Log($"---------------------- Entering to {newState} ------------------------");
        InitState(newState);
    }

 

    private void InitState(ActionStates newState)
    {
        actionState = newState;
        switch (actionState)
        {
            case ActionStates.NOACTION:
                saveButton.SetActive(false);
                chestOpened = false;
                unequipButton.GetComponent<Button>().interactable = false;
                isCombining = false;
                ToggleActionsButtons(false);
                FillClearItemDescriptionPanel("", "", true);
                notesDiaryPanel.SetActive(false);
                notesDiaryPanelScroll.SetActive(false);
                break;
            case ActionStates.SELECT_ACTION:
                ToggleActionsButtons(true);
                FillClearItemDescriptionPanel(itemSelected.Name, itemSelected.Description, false);
                break;
            case ActionStates.ACTION_USE:
                itemSelected.Use();
                ChangeState(ActionStates.NOACTION);
                break;
            case ActionStates.ACTION_COMBINE:
                inventoryUI.ShowHideItemsToCombine(itemSelected.combinableItems, itemSelected);
                ChangeState(ActionStates.ACTION_COMBINE_SELECT);
                break;
            case ActionStates.ACTION_COMBINE_SELECT:
                isCombining = true;
                ToggleHabilitateButtons(false);
                break;
            case ActionStates.ACTION_EQUIP_ITEM:
                itemSelected.Equip();
                ChangeState(ActionStates.NOACTION);
                break;
            case ActionStates.SELECT_EQUIPPED:
                ToggleActionsButtons(false);
                unequipButton.SetActive(true);
                inventoryUI.SetEquippedItem(equippedItem, true);
                unequipButton.GetComponent<Button>().interactable = true;
                break;
            case ActionStates.ACTION_UNEQUIP:
                if (inventory.items.Count < 6)
                {
                    inventory.AddItem(equippedItem);
                    equippedItem = null;
                    inventoryUI.SetEquippedItem(null, true);
                    ChangeState(ActionStates.NOACTION);
                }
                else
                {
                    Debug.Log("Tienes mas objetos de los que puedes tener en el inventario");
                }
                break;
            case ActionStates.CHEST_OPENED:
                chestOpened = true;
                this.inventoryUI.Show();
                ToggleActionsButtons(false);
                saveButton.SetActive(true);
                saveButton.GetComponent<Button>().interactable = false;
                _inputActions.Chest.Enable();
                player.ToggleInputPlayer(false, false);
                player.ToggleChestAnimation(true);
                player.inventoryOpened = true;
                //player.ResumeInteract(false);
                break;
            case ActionStates.ACTION_CHEST_SELECT:
                saveButton.GetComponent<Button>().interactable = true;
                FillClearItemDescriptionPanel(itemSelected.Name, itemSelected.Description, false);
                break;

        }

    }
    private void ExitState(ActionStates currentState)
    {
        switch (currentState)
        {
            case ActionStates.NOACTION:
                break;
            case ActionStates.SELECT_ACTION:
                break;
            case ActionStates.ACTION_USE:
                break;
            case ActionStates.ACTION_COMBINE:
                break;
            case ActionStates.ACTION_COMBINE_SELECT:
                isCombining = false;
                ToggleHabilitateButtons(false);
                break;
            case ActionStates.ACTION_EQUIP_ITEM:
                ToggleActionsButtons(false);
                break;
            case ActionStates.ACTION_UNEQUIP:
                unequipButton.SetActive(false);
                player.UnequipItem();
                break;
        }
    }
    #endregion

    private void ToggleHabilitateButtons(bool interactable)
    {
        foreach (Transform child in buttonsActionRoot.transform)
        {
            child.GetComponent<Button>().interactable = interactable;
        }
    }

    public void SelectItemToCombine(Item item)
    {
        targetItemToCombine=item;
        itemSelected.Combine(targetItemToCombine);
    }

    public void AddNewItemAfterCombine(Item newItem)
    {
        inventoryAudioSource.PlayOneShot(tapeAudio);
        inventory.UseItem(itemSelected);
        inventory.UseItem(targetItemToCombine);
        inventory.AddItem(newItem);
        inventoryUI.Show();
        ChangeState(ActionStates.NOACTION);

    }

    public void SelectEquippedItem(Item item)
    {
        equippedItem=item;
        ChangeState(ActionStates.SELECT_EQUIPPED);

    }

    public void UnequipItem()
    {
        ChangeState(ActionStates.ACTION_UNEQUIP);
    }

    public void UseItem()
    {
        ChangeState(ActionStates.ACTION_USE);
    }

    public void CombineItem()
    {
        ChangeState(ActionStates.ACTION_COMBINE);
    }

    public void EquipItem()
    {
        ChangeState(ActionStates.ACTION_EQUIP_ITEM);
    }

    public void AddItem(Item item)
    {
        inventory.AddItem(item);
        Debug.Log("Afegeixo item " + item.name);
    }

    public void OpenInventory(GameObject target)
    {
        Cursor.visible = true;
        player.inventoryOpened = true;
        player.ToggleInputPlayer(false, true);
        inventoryUI.Show();
        ChangeState(ActionStates.NOACTION);
    }

    public void CloseInventory()
    {
        Cursor.visible = false;
        player.inventoryOpened = false;
        player.ToggleInputPlayer(true, true);
        inventoryUI.Hide();
    }

    public void UseHealingItem(int healing, Item item)
    {
        
        Debug.Log("Player usa item de curacion");
        inventory.UseItem(item);
        inventoryUI.Show();
        player.Heal(healing);
    }

    public void UseAmmo(int numAmmo, Item item)
    {
        if(player.gunAmmo < 12)
        {
            if(player.gunAmmo + numAmmo <= 12)
                player.ReloadAmmo(numAmmo);
            else
            {
                player.gunAmmo = 0;
                player.ReloadAmmo(12); //Nomes entrara aqui quan sobrepassi el limit de 12 bales
            }

            Debug.Log("Player recarrega les bales");
            inventory.UseItem(item);
            inventoryUI.Show();
        }
        else
        {
            //Avisar de que no pots carregar bales
        }
    }

    public void UseKeyItem(Item item)
    {
        inventory.UseItem(item);
        inventoryUI.Show();
    }

    public void UseSilencer(Item item)
    {
        if (!player.isSilencerEquipped)
        {
            inventory.UseItem(item);
            player.UseSilencer();
            inventoryUI.Show();
        }
    }

    public void EquipThrowableItem(Item item, GameObject equipableObject)
    {
        inventory.UseItem(item);
        equippedItem = item;
        player.EquipItem(equipableObject);
        ChangeState(ActionStates.SELECT_EQUIPPED);
    }

    public void ChangeSelectedItem()
    {
        inventoryUI.GetComponent<ShowInventory>().ChangeItemSelected();
    }

    public void OpenDiaryNote(NotesSO note)
    {
        notesDiaryPanel.SetActive(true);
        notesDiaryPanel.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text=note.name;
        notesDiaryPanel.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = note.noteContent;
    }  
    public void OpenDiaryNoteScroll(NotesSO note)
    {
        notesDiaryPanelScroll.SetActive(true);
        notesDiaryPanelScroll.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text=note.name;
        notesDiaryPanelScroll.transform.GetChild(1).GetChild(0).GetComponent<TextMeshProUGUI>().text = note.noteContent;
    }

    public void DiscoverNote(NotesSO note)
    {
        noteInventory.AddNote(note);
        events.IncreaseIntensityChAb(0.167f);
        events.IncreaseSampleChAb(3);
        events.IncreaseIntensityFilmGrain(0.167f);
    }

    public void ShowNoteScroll(NotesSO note)
    {
        notesPanelScroll.SetActive(true);
        notesPanelScroll.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = note.Name;
        notesPanelScroll.transform.GetChild(1).GetChild(0).GetComponent<TextMeshProUGUI>().text = note.noteContent;
        player.ToggleInputPlayer(false, false);
        Cursor.visible = true;
        OnNote?.Invoke(true);

    }
    public void ShowNote(NotesSO note)
    {   
        notesPanel.SetActive(true);
        notesPanel.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = note.Name;
        notesPanel.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = note.noteContent;
        player.ToggleInputPlayer(false, false);
        Cursor.visible = true;
        OnNote?.Invoke(true);

    }

    public void ShowImageNote(string content)
    {
        imagePanel.SetActive(true);
        imagePanel.transform.GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().text = content;
        player.ToggleInputPlayer(false, false);
        Cursor.visible = true;
        OnNote?.Invoke(true);
    }

    public void ShowBookNote(string content)
    {
        Cursor.visible = true;
        bookPanel.SetActive(true);
        bookPanel.transform.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text = content;
        player.ToggleInputPlayer(false, false);
        OnNote?.Invoke(true);
    }

    public void CloseNote()
    {
        notesPanel.SetActive(false);
        player.ToggleInputPlayer(true, true);
        OnNote?.Invoke(false);
        
    }    
    public void CloseNoteScroll()
    {
        Cursor.visible = false;
        notesPanelScroll.SetActive(false);
        player.ToggleInputPlayer(true, true);
        OnNote?.Invoke(false);
    }

    public void CloseImageNote()
    {
        imagePanel.SetActive(false);
        player.ToggleInputPlayer(true, true);
        OnNote?.Invoke(false);
    }

    public void CloseBookNote()
    {
        bookPanel.SetActive(false);
        player.ToggleInputPlayer(true, true);
        OnNote?.Invoke(false);
    }

    //pasar este metodo al ui.
    public void ShowDiscoveredNotes()
    {
        for (int i = 0; i < noteInventory.notes.Count; i++)
        {
            for (int j = 0; j < notesRoot.transform.childCount; j++)
            {
                if (noteInventory.notes.ElementAt(i).noteId == notesRoot.transform.GetChild(j).GetComponent<NotesButton>().id)
                {
                    Debug.Log("Entro");
                    Debug.Log("La nota actual es: "+noteInventory.notes.ElementAt(i).name);
                    notesRoot.transform.GetChild(j).GetComponent<Button>().interactable = true;
                    notesRoot.transform.GetChild(j).GetChild(0).GetComponent<TextMeshProUGUI>().text = noteInventory.notes.ElementAt(i).name;
                }
            }
        }
    }

    public void ToggleChest(InputAction.CallbackContext context)
    {
        chestOpened = !chestOpened;
        if (chestOpened) OpenChest();
        else CloseChest();
    }
    public void OpenChest()
    {
        ChangeState(ActionStates.CHEST_OPENED);
    }

    public void CloseChest()
    {
        player.ToggleInputPlayer(true, true);
        player.ResumeInteract(true);
        _inputActions.Chest.Disable();
        inventoryUI.Hide();
        player.ToggleChestAnimation(false);
        player.inventoryOpened = false;
        ChangeState(ActionStates.NOACTION);
    }

    public void StoreInChest()
    {
        if (chestInventory.items.Count < 12)
        {
            InventorySO.ItemSlot itemToStore = inventory.GetItem(itemSelected);
            inventory.items.Remove(itemToStore);
            if (itemToStore.item is AmmunitionItem)
            {
                if (chestInventory.items.Contains(itemToStore))
                {
                    InventorySO.ItemSlot itemInChest = chestInventory.GetItem(itemSelected);
                    itemInChest.amount += itemToStore.amount;
                }
                else
                {
                    chestInventory.AddItem(itemSelected);
                }
            }
            else
            {
                for (int i = 0; i < itemToStore.amount; i++)
                {
                    chestInventory.AddItem(itemSelected);
                }
            }
        }
        inventoryUI.Show();
        ChangeState(ActionStates.CHEST_OPENED);
    }

    public void SelectChestItem(Item item)
    {
        if (inventory.items.Count < 6)
        {
            InventorySO.ItemSlot itemToReturn= chestInventory.GetItem(item);
            chestInventory.items.Remove(itemToReturn);
            if (itemToReturn.item is AmmunitionItem)
            {
                if (inventory.items.Contains(itemToReturn))
                {
                    InventorySO.ItemSlot itemInInventory = inventory.GetItem(item);
                    itemInInventory.amount += itemToReturn.amount;
                }
                else
                {
                    inventory.AddItem(item);
                }
            }
            else
            {
                for (int i = 0; i < itemToReturn.amount; i++)
                {
                    inventory.AddItem(item);
                }
            }
        }
        inventoryUI.Show();
    }

    public void UseEquippedItem()
    {
        inventoryUI.SetEquippedItem(null, false);
        unequipButton.SetActive(false);
        player.equipedObject = null;
        player.itemSlotOccuped = false;
        equippedItem = null;
    }
}
