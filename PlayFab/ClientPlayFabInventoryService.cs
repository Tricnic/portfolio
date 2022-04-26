using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;
using UnityEngine;

namespace BSCore
{
    public class ClientPlayFabInventoryService : BasePlayFabInventoryService
    {
        [Zenject.Inject]
        public ClientPlayFabInventoryService(ProfileManager profileManager) : base(profileManager) { }

        public override void Fetch(string serviceId, System.Action<Dictionary<string, InventoryItem>, IDictionary<CurrencyType, int>> onSuccess, System.Action<FailureReasons> onFailure)
        {
            if (!_profileManager.HasFetched)
            {
                DelayedAction.RunWhen(() => _profileManager.HasFetched, () => Fetch(serviceId, onSuccess, onFailure));
                return;
            }

            GetUserInventoryRequest request = new GetUserInventoryRequest();

            System.Action<GetUserInventoryResult> onSuccessWrapper = result =>
            {
                Dictionary<string, InventoryItem> inventory = ParseInventory(serviceId, result.Inventory);
                Dictionary<CurrencyType, int> currencies = ParseCurrency(result.VirtualCurrency);
                onSuccess(inventory, currencies);
            };

            var onFailureWrapper = OnFailureCallback(
                () => Fetch(serviceId, onSuccess, onFailure),
                reason => onFailure?.Invoke(reason)
            );
            PlayFabClientAPI.GetUserInventory(request, onSuccessWrapper, onFailureWrapper);
        }

        public override void ConsumeItem(string instanceId, int usesToConsume, System.Action onSuccess, System.Action<FailureReasons> onFailure)
        {
            ConsumeItemRequest request = new ConsumeItemRequest();
            request.ItemInstanceId = instanceId;
            request.ConsumeCount = usesToConsume;

            System.Action<ConsumeItemResult> onSuccessWrapper = result =>
            {
                onSuccess();
            };

            var onFailureWrapper = OnFailureCallback(
                () => ConsumeItem(instanceId, usesToConsume, onSuccess, onFailure),
                onFailure
            );
            PlayFabClientAPI.ConsumeItem(request, onSuccessWrapper, onFailureWrapper);
        }

        private Dictionary<string, InventoryItem> ParseInventory(string serviceId, List<ItemInstance> itemInstances)
        {
            Dictionary<string, InventoryItem> inventory = new Dictionary<string, InventoryItem>();
            foreach (var itemInstance in itemInstances)
            {
                InventoryItem item;
                if (!inventory.TryGetValue(itemInstance.ItemId, out item))
                {
                    var profile = _profileManager.GetById(itemInstance.ItemId);
                    if (profile == null)
                    {
                        Debug.LogWarningFormat("[InventoryService] {0} has {1} in their inventory, but could not find a profile for it", serviceId, itemInstance.ItemId);
                        continue;
                    }

                    InventoryItem.Data data = new InventoryItem.Data()
                    {
                        Id = itemInstance.ItemId,
                        InstanceId = itemInstance.ItemInstanceId,
                        Profile = _profileManager.GetById(itemInstance.ItemId),
                        Count = itemInstance.RemainingUses.HasValue ? (uint)itemInstance.RemainingUses.Value : 0
                    };
                    item = new InventoryItem(data);
                    inventory.Add(item.Id, item);
                }
            }
            return inventory;
        }
    }
}
