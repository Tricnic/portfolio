using System.Collections.Generic;
using UnityEngine;
using Bolt;
using Zenject;
using System.Linq;

public class PlayerPickupController : BaseEntityEventListener<IPlayerState>, ICanPickup
{
    [Inject] private SignalBus _signalBus = null;

    [SerializeField] private float _pickupRadius = 1.0f;
    [SerializeField] private LayerMask _pickupsLayer = default;

    private PlayerAudioStateController _playerAudioStateController = null;
    private HealthController _healthController;
    private StatusEffectController _statusEffectController;
    private MatchStateHelper _matchStateHelper;
    private bool _enableAutoPickup = true;
    private Collider[] _overlapResults = new Collider[10];
    private int _numPickupsInRange = 0;

    private WeaponPickupEntity _closestPickup;
    private string _lastSentPickupProfileId;
    private bool _wasInRangeOfPickup = false;

    private int _pickupSlot;
    private bool _sendPickupCommand = false;

    private void Awake()
    {
        _playerAudioStateController = GetComponent<PlayerAudioStateController>();
        _healthController = GetComponent<HealthController>();
        _statusEffectController = GetComponent<StatusEffectController>();
        _matchStateHelper = GetComponent<MatchStateHelper>();
    }

    protected override void OnControllerOnlyAttached()
    {
        _signalBus.Subscribe<TryPickupItemSignal>(FlagForPickup);
        state.AddCallback("Loadouts[].Weapons[].Id", OnEquippedWeaponChanged);
        state.AddCallback("IsPoweredUp", OnIsPoweredUpUpdated);
    }

    protected override void OnOwnerOnlyAttached()
    {
        _healthController.Died += OnDied;
    }

    private void OnIsPoweredUpUpdated()
    {
        Debug.Log($"[PlayerPickupController] IsPoweredUp changed to {state.IsPoweredUp}");
    }

    protected override void OnControllerOnlyDetached()
    {
        _signalBus.Unsubscribe<TryPickupItemSignal>(FlagForPickup);
        if (state.Damageable.Health > 0 && (state.GameModeType == (int)GameModeType.BattleRoyale || state.GameModeType == (int)GameModeType.Survival))
        {
            GameModeEntityHelper.DropAllItems(state, transform.position, transform.rotation, state.Loadouts[0].Weapons[0].Id);
        }
    }

    public override void SimulateController()
    {
        if (state.IsSecondLife || _matchStateHelper.MatchStateCached != MatchState.Active)
            return;

        HandlePickupInput();
        DetectPickupsInRange();
        UpdatePickupsUI();
    }

    public override void SimulateOwner()
    {
        if (state.IsSecondLife || _matchStateHelper.MatchStateCached != MatchState.Active)
            return;

        DetectPickupsInRange();
        HandleAutoPickups();
    }

    private void FlagForPickup(TryPickupItemSignal tryPickupItemSignal)
    {
        _pickupSlot = tryPickupItemSignal.Slot;
        _sendPickupCommand = true;
    }

    private void HandlePickupInput()
    {
        if (_sendPickupCommand && !state.Stunned && !state.WeaponsDisabled)
        {
            if (_closestPickup != null && _closestPickup.entity != null)
            {
                Debug.Log($"[PlayerPickupController] Sending pickup command {_closestPickup.PickupProfile.Id} | {_pickupSlot}");
                SendPickupCommand(_closestPickup.entity, _pickupSlot, PickupType.Weapon);
            }
            else
            {
                Debug.LogError($"[PlayerPickupController] Trying to send pickup command with null pickup entity");
            }
        }
        _sendPickupCommand = false;
    }

    private void SendPickupCommand(BoltEntity pickupEntity, int slot, PickupType pickupType)
    {
        IPickupItemCommandInput pickupItemInput = PickupItemCommand.Create();
        pickupItemInput.Entity = pickupEntity;
        pickupItemInput.Slot = slot;
        pickupItemInput.Type = (int)pickupType;
        entity.QueueInput(pickupItemInput);
    }

    private void DetectPickupsInRange()
    {
        _numPickupsInRange = Physics.OverlapSphereNonAlloc(transform.position, _pickupRadius, _overlapResults, _pickupsLayer);
    }

    private void UpdatePickupsUI()
    {
        bool pickupsInRange = false;
        if (!state.WeaponsDisabled)
        {
            if (_numPickupsInRange > 0)
            {
                var possiblePickups = GetPossibleWeaponPickups(_numPickupsInRange);
                if (possiblePickups.Count > 0)
                {
                    pickupsInRange = true;
                    TryFindClosestWeaponPickup(possiblePickups);
                }
            }
            else
            {
                _closestPickup = null;
            }
        }

        if (!pickupsInRange && _wasInRangeOfPickup)
        {
            _wasInRangeOfPickup = false;
            _lastSentPickupProfileId = string.Empty;
            _signalBus.Fire<OutOfRangeOfPickupSignal>();
        }
    }

    private List<WeaponPickupEntity> GetPossibleWeaponPickups(int numFound)
    {
        var weaponPickups = new List<WeaponPickupEntity>();
        for (int i = 0; i < numFound; i++)
        {
            var weaponPickup = _overlapResults[i].GetComponent<WeaponPickupEntity>();
            if (weaponPickup != null)
            {
                if (weaponPickup.PickupProfile != null)
                {
                    var existingItemIds = new List<string>
                    {
                        state.Loadouts[0].Weapons[1].Id,
                        state.Loadouts[0].Weapons[2].Id,
                        state.Loadouts[0].Weapons[0].Id
                    };

                    if (existingItemIds.Count <= 0 || !existingItemIds.Contains(weaponPickup.PickupProfile.Id))
                    {
                        weaponPickups.Add(weaponPickup);
                    }
                }
            }
        }
        return weaponPickups;
    }

    private void TryFindClosestWeaponPickup(List<WeaponPickupEntity> possiblePickups)
    {
        _closestPickup = null;
        float nearestDistance = float.MaxValue;

        foreach (var pickup in possiblePickups)
        {
            var distance = (transform.position - pickup.transform.position).sqrMagnitude;
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                _closestPickup = pickup;
            }
        }

        if (_closestPickup != null && _closestPickup.PickupProfile != null)
        {
            if (_lastSentPickupProfileId != _closestPickup.PickupProfile.Id)
            {
                _lastSentPickupProfileId = _closestPickup.PickupProfile.Id;
                _wasInRangeOfPickup = true;
                if (_enableAutoPickup && _closestPickup.PickupProfile.ItemType != ItemType.meleeWeapon && HasEmptyWeaponSlot())
                {
                    for (int i = 1; i < state.Loadouts[0].Weapons.Length; i++)
                    {
                        if (string.IsNullOrEmpty(state.Loadouts[0].Weapons[i].Id))
                        {
                            _pickupSlot = i;
                            _sendPickupCommand = true;
                            break;
                        }
                    }
                }
                else
                {
                    _signalBus.Fire(new InRangeOfPickupSignal() { PickupProfile = _closestPickup.PickupProfile });
                }
            }
        }
        else if (_wasInRangeOfPickup)
        {
            _wasInRangeOfPickup = false;
            _lastSentPickupProfileId = string.Empty;
            _signalBus.Fire<OutOfRangeOfPickupSignal>();
        }
    }

    private bool HasEmptyWeaponSlot()
    {
        return state.Loadouts[0].Weapons.Any(w => string.IsNullOrEmpty(w.Id));
    }

    private void HandleAutoPickups()
    {
        if (_numPickupsInRange > 0)
        {
            for (int i = 0; i < _numPickupsInRange; i++)
            {
                var ammoPickup = _overlapResults[i].GetComponent<AmmoClipPickupEntity>();
                if (ammoPickup != null && ammoPickup.IsActive)
                {
                    TryPickupAmmoClip(ammoPickup.entity);
                }
            }
        }
    }

    public override void ExecuteCommand(Command command, bool resetState)
    {
        if (state.IsSecondLife)
            return;

        if (command is PickupItemCommand)
            ExecuteCommand(command as PickupItemCommand, resetState);
    }

    private void ExecuteCommand(PickupItemCommand cmd, bool resetState)
    {
        if (entity.isOwner && state.InputEnabled && !state.Stunned && !state.WeaponsDisabled)
        {
            var pickupEntityObj = cmd.Input.Entity;
            if (pickupEntityObj == null)
            {
                Debug.LogError($"[PlayerPickupController] Got Command to pickup Null Entity");
                return;
            }

            PickupType pickupType = (PickupType)cmd.Input.Type;
            switch (pickupType)
            {
                case PickupType.Weapon:
                    cmd.Result.Successful = TryPickupWeapon(pickupEntityObj, cmd.Input.Slot);
                    break;
                case PickupType.AmmoClip:
                    cmd.Result.Successful = TryPickupAmmoClip(pickupEntityObj);
                    break;
            }
        }
    }

    private bool TryPickupWeapon(BoltEntity pickupEntityObj, int slotIndex)
    {
        var pickupEntity = pickupEntityObj.gameObject.GetComponent<WeaponPickupEntity>();
        if (pickupEntity != null && pickupEntity.TryClaimPickup())
        {
            PickupWeapon(pickupEntity.PickupProfile, slotIndex);
            BoltNetwork.Destroy(pickupEntity.gameObject);
            return true;
        }
        return false;
    }

    private bool TryPickupAmmoClip(BoltEntity entity)
    {
        Debug.Log($"[PlayerPickupController] Picking up ammo clip");
        state.AmmoClips += 1f;
        BoltNetwork.Destroy(entity.gameObject);
        return true;
    }

    private void PickupWeapon(BB2ProfileWithPrefab itemProfile, int slotIndex)
    {
        if (itemProfile == null)
        {
            Debug.LogError("[PlayerPickupController] Trying to pickup item with a null profile");
            return;
        }

        if (slotIndex != 0 || itemProfile.ItemType == ItemType.meleeWeapon)
        {
            Debug.Log("[PlayerPickupController] Picking up Item " + itemProfile.Id);
            if (!string.IsNullOrEmpty(state.Loadouts[0].Weapons[slotIndex].Id))
            {
                GameModeEntityHelper.DropExistingItem(state.Loadouts[0].Weapons[slotIndex].Id, transform.position, transform.rotation);
            }
            if (state.Loadouts[0].Weapons.All(w => string.IsNullOrEmpty(w.Id)))
            {
                state.Loadouts[0].ActiveWeapon = slotIndex;
            }
            state.Loadouts[0].Weapons[slotIndex].Id = itemProfile.Id;
        }
    }

    private void OnEquippedWeaponChanged(IState state, string propertyPath, ArrayIndices arrayIndices)
    {
        if (_playerAudioStateController != null)
            _playerAudioStateController.PlayWeaponPickupSFX();
    }

    public bool TryPickup(PickupData pickup, BoltEntity pickupEntity)
    {
        if (pickup is HealthPickupData)
        {
            return TryPickup(pickup as HealthPickupData);
        }
        else if (pickup is EffectApplyingPickupData)
        {
            return TryPickup(pickup as EffectApplyingPickupData, pickupEntity);
        }
        else
        {
            Debug.LogError($"[PlayerPickupController] Tried to pickup pickup of type {pickup.GetType().Name}, but could not find a case for it.");
            return false;
        }
    }

    private bool TryPickup(HealthPickupData healthPickup)
    {
        return _healthController.TryHeal(healthPickup.Value);
    }

    private bool TryPickup(EffectApplyingPickupData effectApplyingPickup, BoltEntity pickupEntity)
    {
        if (!CanPickup(effectApplyingPickup))
            return false;

        if(state.IsPoweredUp)
        {
            _statusEffectController.TryKillExistingPowerupEffect();
        }

        void onEffectComplete()
        {
            Debug.Log($"[PlayerPickupController] Powerup ended");
            state.IsPoweredUp = false;
        }
        bool applied = _statusEffectController.TryApplyEffect("", effectApplyingPickup.Effect, pickupEntity, true, onEffectComplete);
        if (applied)
        {
            Debug.Log($"[PlayerPickupController] Powerup started");
            state.IsPoweredUp = true;
        }
        return applied;
    }

    private void OnDied()
    {
        state.IsPoweredUp = false;
    }

    public bool CanPickup(PickupData pickup)
    {
        if (_matchStateHelper.MatchStateCached != MatchState.Active)
            return false;

        if (pickup is HealthPickupData)
        {
            return CanPickup(pickup as HealthPickupData);
        }
        else if (pickup is EffectApplyingPickupData)
        {
            return CanPickup(pickup as EffectApplyingPickupData);
        }
        else
        {
            Debug.LogError($"[PlayerPickupController] Tried to mock pickup pickup of type {pickup.GetType().Name}, but could not find a case for it.");
            return false;
        }
    }

    private bool CanPickup(HealthPickupData healthPickup)
    {
        return state.Damageable.Health < state.Damageable.MaxHealth;
    }

    private bool CanPickup(EffectApplyingPickupData effectApplyingPickup)
    {
        return true;//!state.IsPoweredUp;
    }
}
