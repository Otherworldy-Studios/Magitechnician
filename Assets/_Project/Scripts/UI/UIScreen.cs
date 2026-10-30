using UnityEngine;

public abstract class UIScreen : MonoBehaviour
{

    public bool showsCursor;


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
