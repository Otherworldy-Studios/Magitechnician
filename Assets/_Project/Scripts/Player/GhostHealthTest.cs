#if UNITY_EDITOR

using UnityEngine;
using UnityEngine.InputSystem;

public class GhostHealthTest : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DamageReceiver damageReceiver;
    [SerializeField] private Health health;
    [SerializeField] private GhostHealth ghostHealth;

    private void Awake()
    {
        if (damageReceiver == null)
            damageReceiver = GetComponent<DamageReceiver>();

        if (health == null)
            health = GetComponent<Health>();

        if (ghostHealth == null)
            ghostHealth = GetComponent<GhostHealth>();
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        // M = Simulate taking 30 damage.
        if (Keyboard.current.mKey.wasPressedThisFrame)
        {
            if (damageReceiver == null)
            {
                Debug.LogWarning(
                    "Assign DamageReceiver in the Inspector."
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

            Debug.Log("M pressed - Damage applied!");

            PrintHealth();
        }

        // O = Simulate dealing 20 damage to an enemy.
        if (Keyboard.current.oKey.wasPressedThisFrame)
        {
            if (ghostHealth == null)
            {
                Debug.LogWarning(
                    "Assign GhostHealth in the Inspector."
                );
                return;
            }

            int recovered =
                ghostHealth.RecoverGhostHealth(20);

            Debug.Log(
                "O pressed - Recovered " +
                recovered + " HP!"
            );

            PrintHealth();
        }
    }

    private void PrintHealth()
    {
        if (health == null || ghostHealth == null)
            return;

        Debug.Log(
            "Real HP: " + health.CurrentHealth +
            " | Ghost HP: " +
            ghostHealth.GhostHealthAmount
        );
    }
}

#endif