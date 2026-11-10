using UnityEngine;

public class ItemBob : MonoBehaviour
{
    private Vector3 startPos;
    public float bobSpeed = 5f;
    public float bobAmount = 0.05f;

    void Start()
    {
        startPos = transform.localPosition;
    }

    void Update()
    {
        float newY = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobAmount;
        transform.localPosition = new Vector3(startPos.x, newY, startPos.z);
    }
}
