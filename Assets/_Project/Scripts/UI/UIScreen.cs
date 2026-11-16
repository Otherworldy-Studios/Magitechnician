using UnityEngine;

public abstract class UIScreen : MonoBehaviour
{

    public bool showsCursor;


    // ReSharper disable Unity.PerformanceAnalysis
    public virtual void Show()
    {
        gameObject.SetActive(true);
        OnShown(); 
    }

    public virtual void Hide()
    {
        gameObject.SetActive(false);
        OnHidden();
    }

    protected virtual void OnShown()
    {
        
    }

    protected virtual void OnHidden()
    {
        
    }
    
}
