using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using BSCore;
using UnityEngine.UI;

public class ItemShop : MonoBehaviour
{
    [Inject] private StoreManager _storeManager = default;
    [Inject] private InventoryManager _inventoryManager = default;

    [SerializeField] private string _storeId = default;
    [SerializeField] private GameObject _loadingOverlay = default;
    [SerializeField] private PurchaseItemPopup _purchaseItemPopup = default;
    [SerializeField] private List<ItemShopItem> _shopItems = default;

    private void Start()
    {
        _loadingOverlay.SetActive(true);
        _inventoryManager.CurrencyFetched += UpdateStoreDisplay;
        _inventoryManager.InventoryFetched += UpdateStoreDisplay;
        _storeManager.Fetch(_storeId, PopulateStore, FailedToFetchStore);
    }

    private void OnDestroy()
    {
        _inventoryManager.CurrencyFetched -= UpdateStoreDisplay;
        _inventoryManager.InventoryFetched -= UpdateStoreDisplay;
    }

    private void FailedToFetchStore(FailureReasons failureReason)
    {
        //Show Error popup or something
        Debug.LogError($"[ItemShop] Failed to fetch store {failureReason}");
        _loadingOverlay.SetActive(false);
    }

    private void PopulateStore(StoreData storeData)
    {
        foreach(var shopItem in _shopItems)
        {
            shopItem.Clear();
            shopItem.OnClicked -= OnShopItemClick;
            shopItem.gameObject.SetActive(false);
        }

        int index = 0;
        foreach (var storeItem in storeData.Items)
        {
            var shopItem = _shopItems[index];
            if (shopItem == null)
            {
                Debug.LogError($"[ItemShop] Unable to get shop item for profile at Index {index}");
                return;
            }
            var ownsItem = _inventoryManager.OwnsItem(storeItem.Profile);
            shopItem.Populate(storeItem, ownsItem);
            shopItem.OnClicked += OnShopItemClick;
            shopItem.gameObject.SetActive(true);
            index++;
        }
        _loadingOverlay.SetActive(false);
    }

    private void UpdateStoreDisplay()
    {
        foreach(var shopItem in _shopItems)
        {
            if(shopItem.IsPopulated)
            {
                var ownsItem = _inventoryManager.OwnsItem(shopItem.StoreItem.Profile);
                shopItem.Populate(shopItem.StoreItem, ownsItem);
            }
        }
    }

    private void OnShopItemClick(ItemShopItem shopItem)
    {
        _purchaseItemPopup.Show(shopItem.StoreItem, _storeId);
    }
}
