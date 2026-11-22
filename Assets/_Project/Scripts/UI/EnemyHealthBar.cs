using UnityEngine;


public class EnemyHealthBar : HealthBar
{
    [Header("Enemy")]
    [Tooltip("The bar's visible parts. Must be a child, so this script keeps running while they're hidden.")]
    [SerializeField] private GameObject _content;
    [Tooltip("Seconds without damage or healing before the bar hides.")]
    [SerializeField, Min(0f)] private float _hideDelay = 3f;

    private Camera _camera;
    private float _hideTimer;

    protected override bool BindsToPlayer => false;

    protected override void Start()
    {
        base.Start();

        _camera = Camera.main;

        Bind(GetComponentInParent<Health>());
        if (BoundHealth != null)
            BoundHealth.OnDied += HandleDied;

        SetVisible(false);
    }

    protected override void OnDestroy()
    {
        if (BoundHealth != null)
            BoundHealth.OnDied -= HandleDied;

        base.OnDestroy();
    }

    protected override void Update()
    {
        base.Update();

        if (_hideTimer <= 0f) return;

        _hideTimer -= Time.deltaTime;
        if (_hideTimer <= 0f)
            SetVisible(false);
    }

    private void LateUpdate()
    {
        if (_camera == null || _content == null || !_content.activeSelf) return;

        // Matching the camera's rotation keeps the bar flat to the screen from any angle.
        transform.rotation = _camera.transform.rotation;
    }

    protected override void OnHealthChanged(float healthPercent)
    {
        if (healthPercent >= 1f)
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);
        _hideTimer = _hideDelay;
    }

    private void HandleDied(GameObject enemy)
    {
        BoundHealth.OnDied -= HandleDied;
        Unbind();
        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        if (_content != null)
            _content.SetActive(visible);

        if (!visible)
            _hideTimer = 0f;
    }
}