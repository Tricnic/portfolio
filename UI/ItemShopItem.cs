using BSCore;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ItemShopItem : MonoBehaviour
{
    [SerializeField] private TMPro.TextMeshProUGUI _displayNameText = default;
    [SerializeField] private Image _icon = default;
    [SerializeField] private Button _button = default;
    [SerializeField] private GameObject _softCurrencyCostDisplay = default;
    [SerializeField] private GameObject _hardCurrencyCostDisplay = default;
    [SerializeField] private TMPro.TextMeshProUGUI _softCurrencyCostText = default;
    [SerializeField] private TMPro.TextMeshProUGUI _hardCurrencyCostText = default;
    [SerializeField] private GameObject _ownsItemOverlay = default;

    private event System.Action<ItemShopItem> _onClicked;
    public event System.Action<ItemShopItem> OnClicked { add { _onClicked += value; } remove { _onClicked -= value; } }

    public StoreItem StoreItem { get; private set; }
    public bool IsPopulated { get; private set; }

    private void Awake()
    {
        _button.onClick.AddListener(() => { _onClicked?.Invoke(this); });
    }

    public void Populate(StoreItem storeItem, bool ownsItem)
    {
        StoreItem = storeItem;
        _displayNameText.text = StoreItem.Profile.Name;
        _icon.overrideSprite = StoreItem.Profile.Icon;
        if (storeItem.HasCurrencyTypeCost(CurrencyType.S1))
        {
            _softCurrencyCostText.text = storeItem.OverrideCosts[CurrencyType.S1].ToString();
            _softCurrencyCostDisplay.SetActive(true);
        }
        else
        {
            _softCurrencyCostDisplay.SetActive(false);
        }
        if (storeItem.HasCurrencyTypeCost(CurrencyType.H1))
        {
            _hardCurrencyCostText.text = storeItem.OverrideCosts[CurrencyType.H1].ToString();
            _hardCurrencyCostDisplay.SetActive(true);
        }
        else
        {
            _hardCurrencyCostDisplay.SetActive(false);
        }
        _ownsItemOverlay.SetActive(ownsItem);
        _button.interactable = !ownsItem;
        IsPopulated = true;
    }

    public void Clear()
    {
        IsPopulated = false;
    }
}
