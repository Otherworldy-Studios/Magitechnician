using UnityEngine;
using UnityEngine.InputSystem;

public class ShieldTester : MonoBehaviour
{
    public ShieldSystem shield;

    void OnEnable()
    {
        if (shield != null)
        {
            shield.OnShieldDamaged += TestDamageEvent;
            shield.OnShieldBroken += TestBreakEvent;
        }
    }

    void OnDisable()
    {
        if (shield != null)
        {
            shield.OnShieldDamaged -= TestDamageEvent;
            shield.OnShieldBroken -= TestBreakEvent;
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
                shield.TakeDamage(info);
            }
        }
    }

    void TestDamageEvent(float currentShield, GameObject obj)
    {
        Debug.Log("Shield took damage! Current shield value is: " + currentShield);
    }

    void TestBreakEvent(GameObject obj)
    {
        Debug.LogWarning("Shield broke completely!");
    }
}