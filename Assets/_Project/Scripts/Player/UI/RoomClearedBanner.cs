using System.Collections;
using UnityEngine;

public class RoomClearedBanner : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(Display());
    }

    IEnumerator Display()
    {
        yield return new WaitForSeconds(3);
        gameObject.SetActive(false);
    }
}
