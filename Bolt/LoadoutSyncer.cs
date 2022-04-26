using Bolt;
using UnityEngine;
using Zenject;

[RequireComponent(typeof(LoadoutController))]
public class LoadoutSyncer : EntityEventListener<IPlayerState>
{
    [Inject] private SignalBus _signalBus = null;

    private LoadoutController _loadoutController;
    private StatusEffectController _statusEffectController;
    private PlayerAnimationController _animationController;
    private PlayerController _playerController;
    private WeaponHandler _weaponHandler;

    protected virtual void Awake()
    {
        _playerController = GetComponent<PlayerController>();
        _loadoutController = GetComponent<LoadoutController>();
        _statusEffectController = GetComponent<StatusEffectController>();
        _animationController = GetComponent<PlayerAnimationController>();
        _weaponHandler = GetComponent<WeaponHandler>();
    }

    public override void Attached()
    {
        state.AddCallback("Loadouts[].MeleeWeapon.Id", EquipMeleeWeapon);
        state.AddCallback("Loadouts[].Weapons[].Id", EquipWeapon);
        state.AddCallback("Loadouts[].Backpack", EquipBackpack);
        state.AddCallback("Loadouts[].Hat", EquipHat);
        state.AddCallback("Loadouts[].Outfit", EquipOutfit);
    }

    private void EquipOutfit()
    {
        Debug.Log($"[LoadoutSyncer] Equipping Outfit - {state.Loadouts[0].Outfit} on player ({gameObject.name}). Is Local? {entity.isControlled}");
        _loadoutController.EquipOutfit(state.Loadouts[0].Outfit);
        _loadoutController.Outfit.AimPointHandler.IsLocalPlayer = entity.isControlled && !entity.isOwner;
        var animator = _loadoutController.Outfit.GetComponent<Animator>();
        if (animator != null)
        {
            state.SetAnimator(animator);
        }
        else
        {
            Debug.LogError($"[LoadoutSyncer] Failed to get animator from outfit - {_loadoutController.Outfit.Profile.Id}. Unable to set animator on bolt state");
        }
        _statusEffectController.Setup(_loadoutController.Outfit);
        if (entity.isOwner)
            _animationController.CullingMode = AnimatorCullingMode.AlwaysAnimate;

        if (entity.isControllerOrOwner)
        {
            _playerController.DefaultPlayerMotor.SetProfileProperties(_loadoutController.OutfitProfile.HeroClassProfile, _weaponHandler.ActiveWeaponProfile);
        }
        _weaponHandler.DeployWeapon(state.Loadouts[0].ActiveWeapon);
    }

    private void EquipMeleeWeapon()
    {
        WeaponProfile profile = _loadoutController.EquipMeleeWeapon(state.Loadouts[0].MeleeWeapon.Id);

        if (entity.isControlled && !entity.isOwner)
        {
            Debug.Log($"[LoadoutSyncer] Sending Weapon Loadout update signal. index {-1} | profile {profile}");
            _signalBus.Fire(new LoadoutUpdatedSignal(-1, profile));
        }
    }

    private void EquipWeapon(IState iState, string propertyPath, ArrayIndices arrayIndices)
    {
        //if (_weaponHandler.HasActiveWeapon)
        //{
        //    _weaponHandler.StowWeapon(() => EquipWeapon(iState, propertyPath, arrayIndices));
        //}
        //else
        //{ // Commented out until we need in-match weapon pickup again
            int index = arrayIndices[1];
            Weapon weapon = state.Loadouts[0].Weapons[index];
            bool setActiveOnEquip = index == state.Loadouts[0].ActiveWeapon;

            WeaponProfile profile = _loadoutController.EquipWeapon(index, weapon.Id);
            if (setActiveOnEquip)
            {
                _weaponHandler.DeployWeapon(index);
            }

            if (entity.isControlled && !entity.isOwner)
            {
                Debug.Log($"[LoadoutSyncer] Sending Weapon Loadout update signal. index {index} | profile {profile}");
                _signalBus.Fire(new LoadoutUpdatedSignal(index, profile));
            }
        //}
    }

    private void EquipBackpack()
    {
        if (string.IsNullOrEmpty(state.Loadouts[0].Backpack))
        {
            return;
        }
        _loadoutController.EquipBackpack(state.Loadouts[0].Backpack);
    }

    private void EquipHat()
    {
        if (string.IsNullOrEmpty(state.Loadouts[0].Hat))
        {
            return;
        }
        _loadoutController.EquipHat(state.Loadouts[0].Hat);
    }
}
