using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShowItem : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    [SerializeField] TextMeshProUGUI textQuantitat;
    public event Action OnUseItem;
    PickItem selectedItem;

    public void Load(InventorySO.ItemSlot item)
    {
        this.GetComponent<Image>().sprite=item.item.item.Sprite;
        this.textQuantitat.text=item.amount.ToString();
        selectedItem=item.item;
    }

    public void ClickItem()
    {
        if (!InventoryManager.instance.isCombining)
        {
            Debug.Log("Clico Item");
            InventoryManager.instance.ChangeSelectedItem();
            InventoryManager.instance.ItemSelected(selectedItem.item);
            this.GetComponent<Image>().color = Color.magenta;
        }
        else
        {
            InventoryManager.instance.SelectItemToCombine(selectedItem);
        }
       
    }


}
