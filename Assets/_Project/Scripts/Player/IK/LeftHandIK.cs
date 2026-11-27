using UnityEngine;
using UnityEngine.Animations.Rigging;

public class LeftHandIK : MonoBehaviour
{
    [SerializeField] private MultiParentConstraint _follower;

    public void SetPoint(int index = 0)
    {
        var sources = _follower.data.sourceObjects;

        if (index < 0 || index >= sources.Count)
            index = 0;

        for (int i = 0; i < sources.Count; i++)
        {
            sources.SetWeight(i, i == index ? 1f : 0f);
        }

        _follower.data.sourceObjects = sources;
    }
}