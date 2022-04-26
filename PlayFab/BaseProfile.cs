using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace BSCore
{
    public class BaseProfile
    {
        private static List<string> _noItemNames;

        public static bool ItemIdIsNoItem(string itemId)
        {
            if (_noItemNames == null)
                _noItemNames = System.Enum.GetNames(typeof(ItemType)).Select(itn => "No" + itn).ToList();

            return _noItemNames.Contains(itemId);
        }

        public BaseProfile(GameItem gameItem)
        {
            CatalogVersion = gameItem.CatalogVersion;
            Id = gameItem.Id;
            ItemType = gameItem.ItemType;
            Tags = gameItem.Tags;
            Description = gameItem.Description;
            Name = gameItem.Name;
            Icon = Resources.Load<Sprite>(gameItem.Id + "Icon");
            Cost = gameItem.Cost;
            UsageCount = gameItem.UsageCount;
            UsageLifespan = gameItem.UsageLifespan;
            IsConsumable = UsageCount > 0 || UsageLifespan > 0;
            DeserializeData(gameItem.CustomData);
            ParseCustomData();
        }

        public bool IsNoItem { get { return ItemIdIsNoItem(Id); } }
        public string Id { get; protected set; }
        public ItemType ItemType { get; protected set; }
        public IList<string> Tags { get; protected set; }
        public string CatalogVersion { get; protected set; }
        public string Description { get; protected set; }
        public string Name { get; protected set; }
        public Sprite Icon { get; protected set; }
        public Sprite ItemTypeIcon { get; protected set; }
        public int ReleaseVersion { get; protected set; }
        public IDictionary<CurrencyType, int> Cost { get; protected set; }
        public bool IsConsumable { get; private set; }
        public int UsageCount { get; private set; }
        public int UsageLifespan { get; private set; }

        public CurrencyType FirstCostType { get { return Cost.Keys.First(); } }
        public int FirstCostAmount { get { return Cost[FirstCostType]; } }
        public Rarity Rarity { get; protected set; }
        public Color RarityColor { get; protected set; }

        protected BaseProfileData _profileData;

        protected virtual void DeserializeData(string json)
        {
            _profileData = BaseProfileData.FromJson<BaseProfileData>(json);
        }

        protected virtual void ParseCustomData()
        {
            ReleaseVersion = _profileData.ReleaseVersion;
            Rarity = Rarity.Common;
            Rarity rarity;
            if(Enum<Rarity>.TryParse(_profileData.Rarity, out rarity))
            {
                Rarity = rarity;
            }
        }

        public bool HasCurrencyTypeCost(CurrencyType currencyType)
        {
            return Cost.ContainsKey(currencyType) && Cost[currencyType] > 0;
        }

        public override string ToString()
        {
            return "[BaseProfile] Id: " + Id + " Type " + ItemType;
        }
    }

    [System.Serializable]
    public class BaseProfileData
    {
        public static T FromJson<T>(string json) where T : BaseProfileData
        {
            return JsonUtility.FromJson<T>(json);
        }

        public int ReleaseVersion;
        public string Rarity = "Common";
    }
}
