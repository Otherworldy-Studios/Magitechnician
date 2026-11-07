using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WinScreen : UIScreen
{
    [SerializeField] private Button hubButton;
    [SerializeField] private TMP_Text runTimeText;
    [SerializeField] private Image winImage;

    
    private void Awake()
    {
        hubButton.onClick.AddListener(OnHub);
    }
    
    protected override void OnShown()
    {
        runTimeText.text = $"Run Time: {CollectEndTime()}";
    }
    
    private void OnHub()
    {
        Debug.Log("On Hub");
        UIManager.Instance.ResetButtonPressed();
    }

    private float CollectEndTime()
    {
        return GameManager.Instance.EndTime;
    }
}
