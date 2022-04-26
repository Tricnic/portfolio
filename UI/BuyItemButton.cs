using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BuyItemButton : MonoBehaviour
{
    [SerializeField] private Button _button = default;
    [SerializeField] private CurrencyType _currencyType = default;
    [SerializeField] private TMPro.TextMeshProUGUI _currencyAmount = default;
    [SerializeField] private Color _canAffordColor = Color.black;
    [SerializeField] private Color _canNotAffordColor = Color.red;

    private event System.Action<BuyItemButton> _onClicked;
    public event System.Action<BuyItemButton> OnClicked { add { _onClicked += value; } remove { _onClicked -= value; } }

    public CurrencyType Currency => _currencyType;

    private void Awake()
    {
        _button.onClick.AddListener(() => { _onClicked?.Invoke(this); } );
    }

    public void SetCurrencyCost(int cost)
    {
        _currencyAmount.text = cost.ToString();
    }

    public void UpdateCanAfford(bool canAfford)
    {
        _currencyAmount.color = canAfford ? _canAffordColor : _canNotAffordColor;
    }
}
