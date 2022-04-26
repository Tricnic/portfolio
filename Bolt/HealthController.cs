using UnityEngine;
using Zenject;

public class HealthController : BaseEntityBehaviour<IPlayerState>, IDamageable
{
    private event System.Action _died;
    public event System.Action Died { add => _died += value; remove => _died -= value; }

    private event System.Action _respawned;
    public event System.Action Respawned { add => _respawned += value; remove => _respawned -= value; }

    private event System.Action _changed;
    public event System.Action Changed { add => _changed += value; remove => _changed -= value; }

    private event System.Action<BoltEntity, float> _damaged;
    public event System.Action<BoltEntity, float> Damaged { add => _damaged += value; remove => _damaged -= value; }

    private event System.Action _maxChanged;
    public event System.Action MaxChanged { add => _maxChanged += value; remove => _maxChanged -= value; }

    [Inject] private SignalBus _signalBus = null;

    private float _health = 0f;
    private PlayerController _playerController;
    private StatusEffectController _statusEffectController;
    private MatchStateHelper _matchStateHelper;
    private Outfit _outfit => _playerController.LoadoutController.Outfit;

    public int Team => state.Team;
    public Collider HurtCollider => _playerController.HurtCollider;
    public Transform DamageNumberSpawn => _playerController.LoadoutController.Outfit.HatContainer;

    protected override void Awake()
    {
        base.Awake();
        _statusEffectController = GetComponent<StatusEffectController>();
        _playerController = GetComponent<PlayerController>();
        _matchStateHelper = GetComponent<MatchStateHelper>();
    }

    protected override void OnAnyAttached()
    {
        state.AddCallback("Damageable.Health", OnHealthChanged);
        state.AddCallback("Damageable.MaxHealth", OnMaxHealthChanged);
        _health = state.Damageable.Health;
    }

    protected override void OnAnyDetached()
    {
        state.RemoveCallback("Damageable.Health", OnHealthChanged);
        state.RemoveCallback("Damageable.MaxHealth", OnMaxHealthChanged);        
    }

    private void OnHealthChanged()
    {
        float newHealth = state.Damageable.Health;
        if (_health > 0f && newHealth <= 0f)
        {
            _died?.Invoke();
        }
        else if (_health <= 0f && newHealth > 0f)
        {
            _respawned?.Invoke();
        }

        _changed?.Invoke();
        _health = newHealth;
    }

    private void OnMaxHealthChanged()
    {
        _maxChanged?.Invoke();
    }

    public void TakeSelfDamage(float damage)
    {
        if (!entity.isOwner)
            return;

        var hitInfo = new HitInfo();
        TakeDamage(hitInfo, damage, entity);
    }

    public void TakeDamage(HitInfo hitInfo, float damage, BoltEntity attacker)
    {
        TakeDamage(hitInfo, damage, attacker, null);
    }

    public void TakeDamage(HitInfo hitInfo, float damage, BoltEntity attacker, WeaponProfile.EffectData effect)
    {
        if (state.Damageable.Health <= 0 || state.IsShielded || _matchStateHelper.MatchStateCached != MatchState.Active)
            return;

        //hitInfo.point // This is where the player was hit

        IPlayerState attackerPlayer = attacker.GetState<IPlayerState>();
        if (effect != null && effect.InverseForAlly && attackerPlayer != null && attackerPlayer.Team == state.Team)
        {
            damage *= -1f;
        }
        Debug.Log($"[HealthController] {name} has taken {damage} damage from {attacker.name}");
        float maxDamage = state.Damageable.Health + state.Damageable.Shield;
        if (damage > 0f && damage > maxDamage)
        {
            damage = maxDamage;
        }
        else if (damage < 0f && state.Damageable.Health + Mathf.Abs(damage) > state.Damageable.MaxHealth)
        {
            damage = -(state.Damageable.MaxHealth - state.Damageable.Health);
        }

        float damageToShield = 0f;
        if (state.Damageable.Shield > 0f && damage > 0f)
        {
            damageToShield = Mathf.Min(state.Damageable.Shield, damage);
            state.Damageable.Shield -= damageToShield;
            damage -= damageToShield;
        }
        state.Damageable.Health = Mathf.Clamp(state.Damageable.Health - damage, 0f, state.Damageable.MaxHealth);

        if (state.Damageable.Health > 0f && effect != null && !hitInfo.weaponProfile.SpawnedEntity.SpawnsEntity)
        {
            _statusEffectController.TryApplyEffect(hitInfo.weaponId, effect, attacker);
        }

        if (damage > 0f || damageToShield > 0f)
        {
            float totalDamage = damage + damageToShield;
            _damaged?.Invoke(attacker, totalDamage);
            var playerDamagedEvent = DamagableDamaged.Create(Bolt.GlobalTargets.AllClients, Bolt.ReliabilityModes.ReliableOrdered);
            playerDamagedEvent.IsPlayer = true;
            playerDamagedEvent.Attacker = attacker;
            playerDamagedEvent.Victim = entity;
            playerDamagedEvent.Damage = totalDamage;
            playerDamagedEvent.Died = false;
            playerDamagedEvent.WeaponId = hitInfo.weaponId;

            if (state.Damageable.Health == 0f)
            {
                playerDamagedEvent.Died = true;
                state.InputEnabled = false;
                _signalBus.Fire(new PlayerDiedSignal(_playerController, attacker));
                _playerController.StatusEffectController.KillAllEffects();
            }
            OnHealthChanged();
            playerDamagedEvent.Send();
        }
    }

    public bool TryHeal(float amount)
    {
        if (state.Damageable.Health >= state.Damageable.MaxHealth || state.Damageable.Health <= 0f)
            return false;

        state.Damageable.Health = Mathf.Clamp(state.Damageable.Health + amount, 0, state.Damageable.MaxHealth);
        return true;
    }
}
