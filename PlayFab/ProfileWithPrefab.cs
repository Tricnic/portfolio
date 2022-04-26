using UnityEngine;

namespace BSCore
{
    public class ProfileWithPrefab : BaseProfile
    {
        public ProfileWithPrefab(GameItem gameItem) : base(gameItem) { }

        public string PrefabName { get; protected set; }

        protected override void DeserializeData(string json)
        {
            _profileData = BaseProfileData.FromJson<ProfileWithPrefabData>(json);
        }

        protected override void ParseCustomData()
        {
            base.ParseCustomData();
            ProfileWithPrefabData profileData = _profileData as ProfileWithPrefabData;
            PrefabName = profileData.PrefabName;
        }
    }

    [System.Serializable]
    public class ProfileWithPrefabData : BaseProfileData
    {
        public string PrefabName;
    }
}
