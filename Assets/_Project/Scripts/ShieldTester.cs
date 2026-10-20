using UnityEngine;
using UnityEngine.InputSystem;

public class ShieldTester : MonoBehaviour
{
    public ShieldSystem shield;
    public Healthbar health;

    void OnEnable()
    {
        if (shield != null)
        {
            shield.OnShieldDamaged += TestDamageEvent;
            shield.OnShieldBroken += TestBreakEvent;
            shield.OnShieldRestored += TestRestoreEvent; // Track the new restoration event criteria!
        }
    }

    void OnDisable()
    {
        if (shield != null)
        {
            shield.OnShieldDamaged -= TestDamageEvent;
            shield.OnShieldBroken -= TestBreakEvent;
            shield.OnShieldRestored -= TestRestoreEvent;
        }
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Debug.Log("Hitting shield for 20 damage");

            DamageInfo info = new DamageInfo();
            info.DamageAmount = 20f;

            if (shield != null)
            {
                float overflow = shield.AbsorbDamageAndReturnOverflow(info);

                if (overflow > 0f && health != null)
                {
                    info.DamageAmount = overflow;
                    health.TakeDamage(info);
                }
            }
        }
    }

    void TestDamageEvent(float currentShield, GameObject obj)
    {
        Debug.Log("Shield took damage. Current shield is now: " + currentShield);
    }

    void TestBreakEvent(GameObject obj)
    {
        Debug.Log("Shield broke completely");
    }

    void TestRestoreEvent(GameObject obj)
    {
        Debug.Log("Shield fully restored back to 100% capacity");
    }
}
