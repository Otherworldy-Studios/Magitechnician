using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using _Project.Scripts.Dungeon;

[DefaultExecutionOrder(100)]
public class GhostHealth : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Health health;
    [SerializeField] private Gun ghostRecoverySource; // Existing Inspector field; no longer used for recovery.

    // Added for Inspector damage testing.
    [SerializeField] private DamageReceiver damageReceiver;

    [Header("Ghost Health Settings")]
    [SerializeField] private bool useContestedHealth = true;

    [SerializeField, Range(0f, 1f)]
    private float ghostHealthPercent = 0.4f;

    [SerializeField, Min(0.1f)]
    private float ghostDrainTime = 5f;

    // NEW: Percentage of damage dealt to an enemy that can be recovered.
    [SerializeField, Range(0f, 1f)]
    private float ghostRecoveryPercent = 0.2f;

    public float GhostHealthAmount { get; private set; }

    public event Action<float> OnGhostHealthChanged;

    private int previousHealth;
    private float ghostTimeRemaining;
    private float pendingRecovery;

    // NEW: Track which enemy Health events we subscribed to.
    private readonly List<Health> enemyHealthList = new();
    private Room currentRoom;
    private Coroutine registrationRoutine;

    private void Awake()
    {
        // Find Health automatically if not assigned.
        if (health == null)
        {
            health = GetComponent<Health>();

            if (health == null)
                health = GetComponentInParent<Health>();

            if (health == null)
                health = GetComponentInChildren<Health>();
        }

        if (health == null)
        {
            Debug.LogError("GhostHealth: No Health component assigned!", this);
            enabled = false;
        }
    }

    private void Start()
    {
        if (health == null)
            return;

        // Health.cs initializes its HP in Start().
        previousHealth = health.CurrentHealth;

        // NEW: The run may have entered a room before our Start ran.
        if (GameManager.HasInstance && GameManager.Instance.CurrentRoom != null)
            HandleRoomEntered(GameManager.Instance.CurrentRoom);
    }

    private void OnEnable()
    {
        if (health != null)
        {
            health.OnDamaged += HandleDamage;
            health.OnHealed += HandleHealing;
            health.OnDied += HandleDeath;
        }

        // CHANGED: Listen for enemy Health events, not Gun.OnDamageDealt.
        if (GameManager.HasInstance)
            GameManager.Instance.OnRoomEntered += HandleRoomEntered;

        Room.OnRoomCleared += HandleRoomCleared;
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.OnDamaged -= HandleDamage;
            health.OnHealed -= HandleHealing;
            health.OnDied -= HandleDeath;
        }

        if (GameManager.HasInstance)
            GameManager.Instance.OnRoomEntered -= HandleRoomEntered;

        Room.OnRoomCleared -= HandleRoomCleared;
        UnregisterEnemies();
    }

    private void Update()
    {
        if (!useContestedHealth)
        {
            SetGhostHealth(0f);
            return;
        }

        if (health == null ||
            health.CurrentHealth <= 0 ||
            GhostHealthAmount <= 0f)
            return;

        DrainGhostHealth();
    }

    // Player takes damage and gains 40% Ghost Health.
    private void HandleDamage(int currentHealth, GameObject player)
    {
        int damageTaken = Mathf.Max(0, previousHealth - currentHealth);
        previousHealth = currentHealth;

        if (!useContestedHealth || damageTaken <= 0)
            return;

        float ghostGained = damageTaken * ghostHealthPercent;
        float missingHealth = health.MaxHealth - currentHealth;

        SetGhostHealth(Mathf.Min(GhostHealthAmount + ghostGained, missingHealth));

        if (GhostHealthAmount > 0f)
        {
            // Start (or refresh) the five-second recovery window.
            ghostTimeRemaining = ghostDrainTime;
        }
    }

    private void HandleHealing(int currentHealth, GameObject player)
    {
        previousHealth = currentHealth;
        SetGhostHealth(Mathf.Min(GhostHealthAmount, health.MaxHealth - currentHealth));
    }

    private void HandleDeath(GameObject player)
    {
        previousHealth = 0;
        SetGhostHealth(0f);
    }

    // CHANGED: Gradually decrease Ghost Health to zero over the remaining time.
    private void DrainGhostHealth()
    {
        if (ghostTimeRemaining <= 0f)
        {
            SetGhostHealth(0f);
            return;
        }

        float delta = Mathf.Min(Time.deltaTime, ghostTimeRemaining);
        float t = delta / ghostTimeRemaining;
        SetGhostHealth(Mathf.Lerp(GhostHealthAmount, 0f, t));

        ghostTimeRemaining = Mathf.Max(0f, ghostTimeRemaining - delta);

        if (ghostTimeRemaining <= 0f)
            SetGhostHealth(0f);
    }

    // NEW: The room entry event comes from GameManager.ChangeRoom().
    private void HandleRoomEntered(Room room)
    {
        UnregisterEnemies();
        currentRoom = room;

        if (room == null || room.Complete)
            return;

        // Let Room.OnRoomEntered / RoomEncounter activate enemies first.
        registrationRoutine = StartCoroutine(RegisterEnemiesNextFrame(room));
    }

    private IEnumerator RegisterEnemiesNextFrame(Room room)
    {
        yield return null;
        registrationRoutine = null;

        if (room == null || currentRoom != room || room.Complete)
            yield break;

        RegisterEnemies(room);
    }

    private void RegisterEnemies(Room room)
    {
        // IMPORTANT: The Room script provided does not have room.Enemies.
        // This works only if enemies exist under the Room GameObject
        // when this method runs. Verify against RoomEncounter.cs.
        EnemyBase[] enemies = room.GetComponentsInChildren<EnemyBase>(true);

        foreach (EnemyBase enemy in enemies)
        {
            if (enemy == null)
                continue;

            Health enemyHealth = enemy.GetComponentInChildren<Health>();

            if (enemyHealth == null || enemyHealthList.Contains(enemyHealth))
                continue;

            enemyHealth.OnDamageInstance += HandleEnemyDamageDealt;
            enemyHealthList.Add(enemyHealth);
        }

        Debug.Log("GhostHealth registered " + enemyHealthList.Count + " enemies.", this);
    }

    private void HandleRoomCleared(Room room)
    {
        if (room != currentRoom)
            return;

        UnregisterEnemies();
    }

    private void UnregisterEnemies()
    {
        if (registrationRoutine != null)
        {
            StopCoroutine(registrationRoutine);
            registrationRoutine = null;
        }

        foreach (Health enemyHealth in enemyHealthList)
        {
            if (enemyHealth != null)
                enemyHealth.OnDamageInstance -= HandleEnemyDamageDealt;
        }

        enemyHealthList.Clear();
        currentRoom = null;
    }

    // NEW: Recover only a percentage of damage dealt to enemies.
    private void HandleEnemyDamageDealt(DamageInfo damageInfo)
    {
        if (GhostHealthAmount <= 0f || ghostTimeRemaining <= 0f)
            return;

        // Only player-caused enemy damage should restore this player's HP.
        if (!GameManager.HasInstance ||
            damageInfo.DamageSource != GameManager.Instance.Player)
            return;

        int recovered = RecoverGhostHealth(damageInfo.DamageAmount);

        Debug.Log("Enemy damage reported: " + damageInfo.DamageAmount +
                  " | Health recovered: " + recovered, this);
    }

    public int RecoverGhostHealth(int damageDealt)
    {
        if (!useContestedHealth ||
            health == null ||
            health.CurrentHealth <= 0 ||
            damageDealt <= 0 ||
            ghostTimeRemaining <= 0f ||
            GhostHealthAmount <= 0f)
            return 0;

        // Example: 20 enemy damage * 0.2 = 4 HP.
        pendingRecovery += damageDealt * ghostRecoveryPercent;

        int recovery = Mathf.FloorToInt(
            Mathf.Min(pendingRecovery,
                      GhostHealthAmount,
                      health.MaxHealth - health.CurrentHealth)
        );

        if (recovery <= 0)
            return 0;

        pendingRecovery -= recovery;

        // Consume only what was recovered, then restore real HP.
        SetGhostHealth(GhostHealthAmount - recovery);
        health.RegenHealth(recovery);

        return recovery;
    }

    private void SetGhostHealth(float amount)
    {
        float newAmount = Mathf.Max(0f, amount);

        if (newAmount <= 0f)
        {
            ghostTimeRemaining = 0f;
            pendingRecovery = 0f;
        }

        if (GhostHealthAmount == newAmount)
            return;

        GhostHealthAmount = newAmount;
        OnGhostHealthChanged?.Invoke(GhostHealthAmount);
    }

    // =======================================
    // DEBUG BUTTONS - ODIN INSPECTOR
    // =======================================
    #region Debug

    [Button, DisableInEditorMode]
    private void DebugGhostHealth()
    {
        if (damageReceiver == null)
        {
            Debug.LogWarning("Assign DamageReceiver in the Inspector.", this);
            return;
        }

        DamageInfo damage = new DamageInfo
        {
            DamageAmount = 30,
            DamageSource = gameObject,
            DamageDirection = Vector3.zero,
            DamageType = DamageType.Physical
        };

        damageReceiver.TakeDamage(damage);
        Debug.Log("Debug: Player received damage.");
        PrintHealth();
    }

    [Button, DisableInEditorMode]
    private void DebugRecoverGhostHealth()
    {
        int recovered = RecoverGhostHealth(20);
        Debug.Log("Debug: Recovered " + recovered + " HP.");
        PrintHealth();
    }

    private void PrintHealth()
    {
        if (health == null)
            return;

        Debug.Log("Real HP: " + health.CurrentHealth +
                  " | Ghost HP: " + GhostHealthAmount);
    }
    #endregion
}
