
using System;
using Sirenix.OdinInspector;
using UnityEngine;

[DefaultExecutionOrder(100)]
public class GhostHealth : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Health health;
    [SerializeField] private Gun ghostRecoverySource;

    // Added for Inspector damage testing.
    [SerializeField] private DamageReceiver damageReceiver;

    [Header("Ghost Health Settings")]
    [SerializeField] private bool useContestedHealth = true;

    [SerializeField, Range(0f, 1f)]
    private float ghostHealthPercent = 0.4f;

    [SerializeField, Min(0.1f)]
    private float ghostDrainTime = 5f;

    public float GhostHealthAmount { get; private set; }

    public event Action<float> OnGhostHealthChanged;

    private int previousHealth;

    // Countdown before Ghost Health disappears.
    private float ghostTimeRemaining;

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
            Debug.LogError(
                "GhostHealth: No Health component assigned!",
                this
            );

            enabled = false;
        }
    }

    private void Start()
    {
        if (health == null)
            return;

        // Health.cs initializes its HP in Start().
        previousHealth = health.CurrentHealth;
    }

    private void OnEnable()
    {
        if (health == null)
            return;

        health.OnDamaged += HandleDamage;
        health.OnHealed += HandleHealing;
        health.OnDied += HandleDeath;

        if (ghostRecoverySource != null)
        {
            ghostRecoverySource.OnDamageDealt += HandleDamageDealt;
        }
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.OnDamaged -= HandleDamage;
            health.OnHealed -= HandleHealing;
            health.OnDied -= HandleDeath;
        }

        if (ghostRecoverySource != null)
        {
            ghostRecoverySource.OnDamageDealt -= HandleDamageDealt;
        }
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

    // STEP 5 & 6:
    // Player takes damage and gains 40% Ghost Health.
    private void HandleDamage(
        int currentHealth,
        GameObject player)
    {
        int damageTaken = Mathf.Max(
            0,
            previousHealth - currentHealth
        );

        previousHealth = currentHealth;

        if (!useContestedHealth || damageTaken <= 0)
            return;

        float ghostGained =
            damageTaken * ghostHealthPercent;

        float missingHealth =
            health.MaxHealth - currentHealth;

        SetGhostHealth(
            Mathf.Min(
                GhostHealthAmount + ghostGained,
                missingHealth
            )
        );

        if (GhostHealthAmount > 0f)
        {
            // Start the five-second countdown.
            ghostTimeRemaining = ghostDrainTime;
        }
    }

    // Keep tracking correct after regular healing.
    private void HandleHealing(
        int currentHealth,
        GameObject player)
    {
        previousHealth = currentHealth;

        SetGhostHealth(
            Mathf.Min(
                GhostHealthAmount,
                health.MaxHealth - currentHealth
            )
        );
    }

    // Clear Ghost Health when the player dies.
    private void HandleDeath(GameObject player)
    {
        previousHealth = 0;
        SetGhostHealth(0f);
    }

    // STEP 7:
    // Wait five seconds, then remove Ghost Health.
    private void DrainGhostHealth()
    {
        ghostTimeRemaining -= Time.deltaTime;

        if (ghostTimeRemaining <= 0f)
        {
            SetGhostHealth(0f);
        }
    }

    // STEP 8:
    // Listen for damage reported by the Gun.
    private void HandleDamageDealt(DamageInfo damageInfo)
    {
        int recovered =
            RecoverGhostHealth(damageInfo.DamageAmount);

        Debug.Log(
            "Enemy damage reported: " +
            damageInfo.DamageAmount +
            " | Health recovered: " + recovered
        );
    }

    // Recover HP using available Ghost Health.
    public int RecoverGhostHealth(int damageDealt)
    {
        if (!useContestedHealth ||
            health == null ||
            health.CurrentHealth <= 0 ||
            damageDealt <= 0 ||
            GhostHealthAmount <= 0f)
            return 0;

        // Recover all available whole Ghost Health points.
        int recovery = Mathf.FloorToInt(
            Mathf.Min(
                GhostHealthAmount,
                health.MaxHealth - health.CurrentHealth
            )
        );

        if (recovery <= 0)
        {
            ghostTimeRemaining = 0f;
            SetGhostHealth(0f);
            return 0;
        }

        // FIX 1:
        // Stop the timer BEFORE changing real health.
        ghostTimeRemaining = 0f;

        // FIX 2:
        // Clear Ghost Health BEFORE healing.
        // This immediately tells the HUD to hide white.
        SetGhostHealth(0f);

        // Restore real HP.
        health.RegenHealth(recovery);

        Debug.Log(
            "Recovered HP: " + recovery +
            " | Remaining Ghost HP: " + GhostHealthAmount
        );

        return recovery;
    }

    // Update Ghost Health and notify other systems.
    private void SetGhostHealth(float amount)
    {
        float newAmount = Mathf.Max(0f, amount);

        if (GhostHealthAmount == newAmount)
            return;

        GhostHealthAmount = newAmount;

        OnGhostHealthChanged?.Invoke(GhostHealthAmount);
    }

    // =======================================
    // DEBUG BUTTONS - ODIN INSPECTOR
    // =======================================

    #region Debug

    // Replaces pressing M.
    [Button, DisableInEditorMode]
    private void DebugGhostHealth()
    {
        if (damageReceiver == null)
        {
            Debug.LogWarning(
                "Assign DamageReceiver in the Inspector.",
                this
            );
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

    // Replaces pressing O.
    [Button, DisableInEditorMode]
    private void DebugRecoverGhostHealth()
    {
        int recovered = RecoverGhostHealth(20);

        Debug.Log(
            "Debug: Recovered " + recovered + " HP."
        );

        PrintHealth();
    }

    // Prints current values in the Console.
    private void PrintHealth()
    {
        if (health == null)
            return;

        Debug.Log(
            "Real HP: " + health.CurrentHealth +
            " | Ghost HP: " + GhostHealthAmount
        );
    }

    #endregion
}