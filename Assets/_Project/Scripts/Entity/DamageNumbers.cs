using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// A floating damage number. It rises and fades over its lifetime, faces the camera,
/// and keeps the same on-screen size at any distance. When a new number appears, older
/// numbers that would overlap it on screen are pushed up, so bursts of hits form a
/// readable stack instead of a pile.
///
/// Create them with DamageNumbers.Spawn. Instances are pooled and reused.
/// </summary>
[RequireComponent(typeof(TextMeshPro))]
public class DamageNumbers : MonoBehaviour
{
    [Header("Motion")]
    [Min(0.01f)]
    [SerializeField] private float _lifetime = 1f;

    [Tooltip("How far the number rises over its lifetime, in line heights.")]
    [SerializeField] private float _rise = 1.5f;

    [Tooltip("When fading starts, as a fraction of the lifetime.")]
    [Range(0f, 1f)]
    [SerializeField] private float _fadeStart = 0.5f;

    [Header("Look")]
    [Tooltip("\"0\" shows whole numbers, \"0.#\" shows one decimal.")]
    [SerializeField] private string _format = "0";

    [Tooltip("Size at the moment of a hit, shrinking back to normal.")]
    [SerializeField] private float _popScale = 1.3f;

    [Tooltip("Seconds the pop takes to shrink back to normal size.")]
    [SerializeField] private float _popDuration = 0.12f;

    [Header("Readability")]
    [Tooltip("Distance at which the number shows at the prefab's own size. " +
             "It keeps that on-screen size at every distance, so far-away numbers stay readable.")]
    [Min(0.01f)]
    [SerializeField] private float _referenceDistance = 10f;
    
    [Header("Big Damage")]
    [Tooltip("How much damage it takes to become emphasized and how emphasize it becomes")]
    [SerializeField] private float _bigHitThreshold = 30f;
    [SerializeField] private float _bigHitSizeMultiplier = 1.4f;
    [SerializeField] private Color _bigHitColor = Color.orange;

    [SerializeField] private float _massiveHitThreshold = 50f;
    [SerializeField] private float _massiveHitSizeMultiplier = 1.8f;
    [SerializeField] private Color _massiveHitColor = Color.red;

    private float _baseFontSize;
    private Color _baseColor;

    private const float StackGap = 0.15f;  // Space between stacked numbers, in line heights.
    private const float StackSpeed = 12f;  // How fast pushed numbers slide up, in line heights per second.

    private static readonly List<DamageNumbers> Active = new();                                // Showing now, oldest first.
    private static readonly Dictionary<DamageNumbers, Stack<DamageNumbers>> Pools = new();     // Hidden instances, per prefab.
    private static readonly List<Rect> Placed = new();                                         // Reused by MakeRoomFor.
    private static Transform _container;  // Parent of every instance. Created on the first Spawn in each scene.
    private static int _nextSerial;

    /// <summary>Changes every time this instance is reused, so a caller can tell whether it's still theirs.</summary>
    public int Serial { get; private set; }

    /// <summary>Seconds since this number appeared.</summary>
    public float Age { get; private set; }

    private DamageNumbers _sourcePrefab; // The prefab this instance came from, so Release returns it to the right pool.
    private TMP_Text _text;
    private Camera _cam;
    private Vector3 _baseScale;
    private Vector3 _anchor;     // The world point the number rises from.
    private Vector2 _size;       // Text size in world units at the reference distance.
    private float _amount;
    private float _stack;        // Extra lift from being pushed up, in line heights.
    private float _targetStack;  // Where _stack is sliding to.
    private float _popTimer;

    /// <summary>
    /// Shows a number at a world position and returns it, so more damage can be added to it.
    /// </summary>
    public static DamageNumbers Spawn(DamageNumbers prefab, Vector3 position, float amount)
    {
        DamageNumbers number = TakeFromPool(prefab);
        number.Begin(position, amount);
        Active.Add(number);
        MakeRoomFor(number);
        return number;
    }

    /// <summary>Adds damage to this number, used to merge rapid hits, and replays the pop.</summary>
    public void Add(float amount)
    {
        _amount += amount;
        _text.text = _amount.ToString(_format);

        float alpha = _text.alpha;

        if (_amount >= _massiveHitThreshold)
        {
            _text.fontSize = _baseFontSize * _massiveHitSizeMultiplier;
            _text.color = _massiveHitColor;
        }
        else if (_amount >= _bigHitThreshold)
        {
            _text.fontSize = _baseFontSize * _bigHitSizeMultiplier;
            _text.color = _bigHitColor;
        }
        else
        {
            _text.fontSize = _baseFontSize;
            _text.color = _baseColor;
        }

        _text.alpha = alpha;

        _size = Vector2.Scale(_text.GetPreferredValues(), _baseScale);
        _popTimer = 0f;
    }

    private void Awake()
    {
        _text = GetComponent<TMP_Text>();
        _baseScale = transform.localScale;

        _baseFontSize = _text.fontSize;
        _baseColor = _text.color;
    }

    private void OnDisable() => Active.Remove(this);

    private void Begin(Vector3 anchor, float amount)
    {
        Serial = ++_nextSerial;
        Age = 0f;
        _cam = Camera.main;
        _anchor = anchor;
        _amount = 0f;
        _stack = _targetStack = 0f;
        _text.alpha = 1f;

        Add(amount);
        UpdateTransform(); // Place it before it's first drawn.
    }

    private void LateUpdate()
    {
        Age += Time.deltaTime;
        if (Age >= _lifetime)
        {
            Release();
            return;
        }

        _popTimer += Time.deltaTime;
        _stack = Mathf.MoveTowards(_stack, _targetStack, StackSpeed * Time.deltaTime);
        _text.alpha = 1f - Mathf.InverseLerp(_fadeStart, 1f, Age / _lifetime);

        UpdateTransform();
    }

    private void UpdateTransform()
    {
        if (_cam == null)
        {
            _cam = Camera.main;
            if (_cam == null) return;
        }

        float pop = _popDuration > 0f ? Mathf.Lerp(_popScale, 1f, _popTimer / _popDuration) : 1f;

        // Copy the camera's rotation so the text always faces the screen squarely.
        transform.SetPositionAndRotation(WorldCenter(_cam, _stack), _cam.transform.rotation);
        transform.localScale = _baseScale * (DistanceScale(_cam) * pop);
    }

    /// <summary>
    /// The number's center. It rises along the screen's up direction at a steady speed. The steady speed matters:
    /// with easing, a younger number would move faster than the one above it and catch up into it.
    /// </summary>
    private Vector3 WorldCenter(Camera cam, float stack)
    {
        float lines = _rise * (Age / _lifetime) + stack;
        return _anchor + cam.transform.up * (lines * _size.y * DistanceScale(cam));
    }

    /// <summary>1 at the reference distance, growing with distance so the on-screen size never changes.</summary>
    private float DistanceScale(Camera cam)
    {
        if (cam.orthographic) return 1f;

        float depth = Vector3.Dot(_anchor - cam.transform.position, cam.transform.forward);
        return Mathf.Max(depth, cam.nearClipPlane) / _referenceDistance;
    }

    /// <summary>
    /// Where this number will be on screen once it has finished sliding to its stack position.
    /// Returns false when it's behind the camera.
    /// </summary>
    private bool TryGetScreenRect(Camera cam, out Rect rect)
    {
        Vector3 center = WorldCenter(cam, _targetStack);
        Transform camT = cam.transform;
        Vector3 half = (camT.right * _size.x + camT.up * _size.y) * (0.5f * DistanceScale(cam));

        Vector3 min = cam.WorldToScreenPoint(center - half);
        Vector3 max = cam.WorldToScreenPoint(center + half);
        rect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);

        return min.z > 0f && max.z > 0f;
    }

    /// <summary>
    /// Pushes older numbers up until none overlaps another on screen. The newest number stays at the hit point and
    /// the stack grows above it. This works across enemies, so an area attack on a crowd doesn't pile up either.
    /// </summary>
    private static void MakeRoomFor(DamageNumbers newest)
    {
        Camera cam = newest._cam;
        if (cam == null || !newest.TryGetScreenRect(cam, out Rect newestRect)) return;

        Placed.Clear();
        Placed.Add(newestRect);

        // Active is in spawn order, so walk it backwards: newest to oldest.
        for (int i = Active.Count - 1; i >= 0; i--)
        {
            DamageNumbers older = Active[i];
            if (older == newest || !older.TryGetScreenRect(cam, out Rect rect) || rect.height <= 0f)
                continue;

            // Each placed number can push this one at most once (it only ever moves up), so this loop always finishes.
            bool moved;
            do
            {
                moved = false;
                foreach (Rect placed in Placed)
                {
                    if (!rect.Overlaps(placed)) continue;

                    // rect.height is one line on screen, so this converts pixels to line heights.
                    older._targetStack += (placed.yMax - rect.yMin) / rect.height + StackGap;
                    older.TryGetScreenRect(cam, out rect);
                    moved = true;
                }
            } while (moved);

            Placed.Add(rect);
        }
    }

    private static DamageNumbers TakeFromPool(DamageNumbers prefab)
    {
        if (_container == null)
            _container = new GameObject("Damage Numbers").transform;

        if (!Pools.TryGetValue(prefab, out Stack<DamageNumbers> pool))
            Pools[prefab] = pool = new Stack<DamageNumbers>();

        while (pool.Count > 0)
        {
            DamageNumbers pooled = pool.Pop();
            if (pooled != null) // Skip any destroyed by a scene change.
            {
                pooled.gameObject.SetActive(true);
                return pooled;
            }
        }

        DamageNumbers created = Instantiate(prefab, _container);
        created._sourcePrefab = prefab;
        return created;
    }

    private void Release()
    {
        gameObject.SetActive(false); // OnDisable takes it out of Active.

        if (_sourcePrefab != null && Pools.TryGetValue(_sourcePrefab, out Stack<DamageNumbers> pool))
            pool.Push(this);
        else
            Destroy(gameObject);
    }

    /// <summary>Clears the shared lists when entering Play mode with domain reload turned off.</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Active.Clear();
        Pools.Clear();
        _container = null;
        _nextSerial = 0;
    }
}