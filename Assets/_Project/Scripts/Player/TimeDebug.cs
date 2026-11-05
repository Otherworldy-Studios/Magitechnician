using UnityEngine;
using UnityEngine.InputSystem;

public class TimeDebug : MonoBehaviour
{
    [SerializeField, Range(0.01f, 1f)]
    private float _slowMotionScale = 0.2f;

    private void Update()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.digit1Key.wasPressedThisFrame)
            Time.timeScale = 0f;

        if (keyboard.digit2Key.wasPressedThisFrame)
            Time.timeScale = _slowMotionScale;

        if (keyboard.digit3Key.wasPressedThisFrame)
            Time.timeScale = 1f;
#endif
    }

    private void OnDisable()
    {
        Time.timeScale = 1f;
    }
}