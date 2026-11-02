using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WinScreen : UIScreen
{
    [SerializeField] private Button hubButton;
    [SerializeField] private TMP_Text runTimeText;
    [SerializeField] private Image winImage;


    protected override void OnShown()
    {
        runTimeText.text = $"Run Time: {CollectEndTime()}";
    }

    private float CollectEndTime()
    {
        return GameManager.Instance.EndTime;
    }
}
