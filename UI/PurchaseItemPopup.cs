using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BSCore;
using Zenject;

public class PurchaseItemPopup : MonoBehaviour
{
    [Inject] private InventoryManager _inventoryManager = default;
    [Inject] private PurchasingManager _purchasingManager = default;

    [SerializeField] protected TMPro.TextMeshProUGUI _displayNameText = default;
    [SerializeField] private TMPro.TextMeshProUGUI _descriptionText = default;
    [SerializeField] private Image _icon = default;
    [SerializeField] private BuyItemButton _softCurrencyBuyButton = default;
    [SerializeField] private BuyItemButton _hardCurrencyBuyButton = default;
    [SerializeField] private Button _closeButton = default;
    [SerializeField] private GameObject _purchasingOverlay = default;
    
    private StoreItem _storeItem;
    private string _storeId;

    private void Awake()
    {
        _closeButton.onClick.AddListener(Close);
        _softCurrencyBuyButton.OnClicked += TryBuyItem;
        _hardCurrencyBuyButton.OnClicked += TryBuyItem;
    }

    public void Show(StoreItem storeItem, string storeId)
    {
        _storeItem = storeItem;
        _storeId = storeId;
        _displayNameText.text = _storeItem.Profile.Name;
        _descriptionText.text = _storeItem.Profile.Description;
        _icon.overrideSprite = _storeItem.Profile.Icon;

        _inventoryManager.CurrencyFetched += OnCurrencyFetched;
        if (_storeItem.HasCurrencyTypeCost(CurrencyType.S1))
        {
            _softCurrencyBuyButton.SetCurrencyCost(_storeItem.OverrideCosts[CurrencyType.S1]);
            _softCurrencyBuyButton.UpdateCanAfford(_inventoryManager.CanAfford(CurrencyType.S1, _storeItem.OverrideCosts[CurrencyType.S1]));
            _softCurrencyBuyButton.gameObject.SetActive(true);
        }
        else
        {
            _softCurrencyBuyButton.gameObject.SetActive(false);
        }
        if (_storeItem.HasCurrencyTypeCost(CurrencyType.H1))
        {
            _hardCurrencyBuyButton.SetCurrencyCost(_storeItem.OverrideCosts[CurrencyType.H1]);
            _hardCurrencyBuyButton.UpdateCanAfford(_inventoryManager.CanAfford(CurrencyType.H1, _storeItem.OverrideCosts[CurrencyType.H1]));
            _hardCurrencyBuyButton.gameObject.SetActive(true);
        }
        else
        {
            _hardCurrencyBuyButton.gameObject.SetActive(false);
        }
        _purchasingOverlay.SetActive(false);
        gameObject.SetActive(true);
    }

    private void OnCurrencyFetched()
    {
        if (_storeItem.Profile == null)
            return;

        if (_storeItem.HasCurrencyTypeCost(CurrencyType.S1))
        {
            _softCurrencyBuyButton.UpdateCanAfford(_inventoryManager.CanAfford(CurrencyType.S1, _storeItem.OverrideCosts[CurrencyType.S1]));
        }
        if (_storeItem.HasCurrencyTypeCost(CurrencyType.H1))
        {
            _hardCurrencyBuyButton.UpdateCanAfford(_inventoryManager.CanAfford(CurrencyType.H1, _storeItem.OverrideCosts[CurrencyType.H1]));
        }
    }
    
    private void TryBuyItem(BuyItemButton buyButton)
    {
        if (_storeItem.Profile == null)
            return;

        int price = _storeItem.OverrideCosts[buyButton.Currency];
        if (!_inventoryManager.CanAfford(buyButton.Currency, price))
        {
            if (buyButton.Currency == CurrencyType.H1)
            {
                //Open IAP Menu
            }
            return;            
        }

        _purchasingOverlay.SetActive(true);

        _inventoryManager.InventoryFetched += Close;
        _inventoryManager.InventoryFetchFailed += Close;
        _purchasingManager.PurchaseItem(_storeItem.Profile, buyButton.Currency, price, _storeId, OnPurchaseSuccess, OnPurchaseFailure);
    }

    private void OnPurchaseSuccess()
    {
        Debug.Log($"[PurchaseItemPopup] Purchased {_storeItem.Profile.Id} Succesfully");
    }

    private void OnPurchaseFailure(FailureReasons reason)
    {
        //Show failure popup
        Debug.LogError($"[PurchaseItemPopup] Failed to purchase item {_storeItem.Profile.Id} - {reason}");
        _purchasingOverlay.SetActive(false);
    }

    private void Close()
    {
        _inventoryManager.InventoryFetched -= Close;
        _inventoryManager.InventoryFetchFailed -= Close;
        _inventoryManager.CurrencyFetched -= OnCurrencyFetched;
        gameObject.SetActive(false);
    }
}
