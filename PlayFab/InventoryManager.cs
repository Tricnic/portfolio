using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace BSCore
{
    public class InventoryManager
    {
        [Zenject.Inject] public InventoryManager(IInventoryService inventoryService, ProfileManager profileManager)
         {
            _inventoryService = inventoryService;
        }

        public InventoryManager(string serviceId, IInventoryService inventoryService, ProfileManager profileManager) : this(inventoryService, profileManager)
        {
            _serviceId = serviceId;
        }

        private event System.Action _inventoryFetched;
        public event System.Action InventoryFetched { add { _inventoryFetched += value; } remove { _inventoryFetched -= value; } }
        private event System.Action _inventoryFetchFailed;
        public event System.Action InventoryFetchFailed { add { _inventoryFetchFailed += value; } remove { _inventoryFetchFailed -= value; } }
        private event System.Action _currencyFetched;
        public event System.Action CurrencyFetched { add { _currencyFetched += value; } remove { _currencyFetched -= value; } }

        private IInventoryService _inventoryService;
        private Dictionary<string, InventoryItem> _inventoryById = new Dictionary<string, InventoryItem>();
        private Dictionary<CurrencyType, int> _currencyByType = new Dictionary<CurrencyType, int>();
        private string _serviceId;

        public List<InventoryItem> Inventory { get { return _inventoryById.Values.ToList(); } }
        public IDictionary<CurrencyType, int> Currencies { get { return new Dictionary<CurrencyType, int>(_currencyByType); } }

        public void Fetch(System.Action<List<InventoryItem>, IDictionary<CurrencyType, int>> onSuccess, System.Action<FailureReasons> onFailure)
        {
            System.Action<Dictionary<string, InventoryItem>, IDictionary<CurrencyType, int>> onSuccessWrapper = (Dictionary<string, InventoryItem> inventory, IDictionary<CurrencyType, int> currency) =>
            {
                OnInventoryFetched(inventory);
                OnCurrencyFetched(currency);
                onSuccess?.Invoke(Inventory, currency);
            };

            System.Action<FailureReasons> onFailureWrapper = (FailureReasons reason) =>
            {
                _inventoryFetchFailed?.Invoke();
                onFailure?.Invoke(reason);
            };

            _inventoryService.Fetch(_serviceId, onSuccessWrapper, onFailureWrapper);
        }

        public bool OwnsItem(string itemId)
        {
            return _inventoryById.ContainsKey(itemId);
        }

        public bool OwnsItem(BaseProfile profile)
        {
            return OwnsItem(profile.Id);
        }

        public bool CanAfford(BaseProfile profile, CurrencyType currencyType)
        {
            if(!profile.HasCurrencyTypeCost(currencyType))
            {
                return false;
            }
            return CanAfford(currencyType, profile.Cost[currencyType]);
        }

        public bool CanAfford(CurrencyType type, int amount)
        {
            int currency = 0;
            return _currencyByType.TryGetValue(type, out currency) && currency >= amount;
        }

        public uint GetUsageCount(BaseProfile profile)
        {
            return GetUsageCount(profile.Id);
        }

        public uint GetUsageCount(string itemId)
        {
            uint usageCount = 0;
            InventoryItem item;
            if (_inventoryById.TryGetValue(itemId, out item))
            {
                usageCount = item.Count;
            }
            return usageCount;
        }

        public void GrantItem(BaseProfile profile, System.Action onSuccess, System.Action<FailureReasons> onFailure)
        {
            _inventoryService.GrantItem(profile, onSuccess, onFailure);
        }

        public void GrantCurrency(string playfabId, CurrencyType currencyType, int amount, System.Action onSuccess = null, System.Action<FailureReasons> onFailure = null)
        {
            _inventoryService.GrantCurrency(playfabId, currencyType, amount, onSuccess, onFailure);
        }

        public void ConsumeItem(string itemId, int amount, System.Action onSuccess, System.Action<FailureReasons> onFailure)
        {
            InventoryItem item;
            if (!_inventoryById.TryGetValue(itemId, out item) || item.Count <= 0)
            {
                onFailure(FailureReasons.NotEnoughUsesRemaining);
                return;
            }

            item.Count -= (uint)amount;

            System.Action<FailureReasons> onFailureWrapper = reason =>
            {
                item.Count += (uint)amount;
                onFailure(reason);
            };
            _inventoryService.ConsumeItem(item.InstanceId, amount, onSuccess, onFailureWrapper);
        }

        private void OnInventoryFetched(Dictionary<string, InventoryItem> items)
        {
            _inventoryById = items;
            _inventoryFetched?.Invoke();
        }

        private void OnCurrencyFetched(IDictionary<CurrencyType, int> currencies)
        {
            _currencyByType = new Dictionary<CurrencyType, int>(currencies);
            _currencyFetched?.Invoke();
        }

        public class Factory : Zenject.PlaceholderFactory<string, InventoryManager> { }
    }

    public class InventoryManagerFactory : Zenject.IFactory<string, InventoryManager>
    {
        public InventoryManagerFactory(Zenject.DiContainer container)
        {
            _container = container;
        }

        private Zenject.DiContainer _container;

        public InventoryManager Create(string serviceId)
        {
            return _container.Instantiate<InventoryManager>(new object[] { serviceId });
        }
    }
}
