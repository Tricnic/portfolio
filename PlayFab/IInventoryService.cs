using System.Collections.Generic;
using UnityEngine;

namespace BSCore
{
    public interface IInventoryService
    {
        void Fetch(string serviceId, System.Action<Dictionary<string, InventoryItem>, IDictionary<CurrencyType, int>> onSuccess, System.Action<FailureReasons> onFailure);
        void ConsumeItem(string instanceId, int usesToConsume, System.Action onSuccess, System.Action<FailureReasons> onFailure);
        void GrantItem(BaseProfile profile, System.Action onSuccess, System.Action<FailureReasons> onFailure);
        void GrantCurrency(string playfabId, CurrencyType currencyType, int amount, System.Action onSuccess, System.Action<FailureReasons> onFailure);
    }
}
