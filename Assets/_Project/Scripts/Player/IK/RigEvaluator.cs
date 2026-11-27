using UnityEngine;
using UnityEngine.Animations.Rigging;

[DefaultExecutionOrder(1000)]
public class RigEvaluator : MonoBehaviour
{
    [SerializeField] private RigBuilder _rigBuilder;

    private void OnEnable()
    {
        _rigBuilder.enabled = false;

        if (_rigBuilder.Build())
        {
            _rigBuilder.graph.SetTimeUpdateMode(
                UnityEngine.Playables.DirectorUpdateMode.Manual);
        }
    }

    private void LateUpdate()
    {
        _rigBuilder.Evaluate(Time.deltaTime);
    }

    private void OnDisable()
    {
        _rigBuilder.Clear();
    }
}