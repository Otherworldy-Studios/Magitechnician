using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeathScreen : UIScreen
{
    [SerializeField] private Button hubButton;
    [SerializeField] private TMP_Text runTimeText;
    [SerializeField] private Image deathImage;

    private void Awake()
    {
        hubButton.onClick.AddListener(OnHub);
    }
    
    private void OnHub()
    {
        Debug.Log("On Hub");
        UIManager.Instance.ResetButtonPressed();
    }
    protected override void OnShown()
    {
        runTimeText.text = $"Run Time: {CollectEndTime()}";
    }

    private float CollectEndTime()
    {
        return GameManager.Instance.EndTime;
    }
}
