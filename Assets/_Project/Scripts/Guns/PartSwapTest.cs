using System;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

// Debug tool: one key cycles through the gun's sockets, the other cycles the part in that socket.
public class PartSwapTest : MonoBehaviour
{
    [SerializeField] private Gun _gun;

    [Header("Keys (Input System binding paths, read when play starts)")]
    [SerializeField] private string _nextSocketBinding = "<Keyboard>/leftBracket";
    [SerializeField] private string _nextPartBinding = "<Keyboard>/rightBracket";

    [Header("Parts per socket (element 0 = default, element 1 = alternate)")]
    [SerializeField] private GunBarrel[] _barrels;
    [SerializeField] private GunMagazine[] _magazines;
    [SerializeField] private GunScope[] _scopes;
    [SerializeField] private GunStock[] _stocks;
    [SerializeField] private GunUnderbarrel[] _underbarrels;

    [Header("Readout (rewritten on every swap)")]
    [SerializeField] private GunPartType _currentSocketReadout;
    [SerializeField] private string _currentPartReadout;
    [SerializeField, TextArea(8, 30)] private string _equippedStats;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    //Sockets the keys cycle through. Frame isn't here because it isn't equipped through a socket
    private static readonly GunPartType[] Sockets =
    {
        GunPartType.Barrel,
        GunPartType.Magazine,
        GunPartType.Scope,
        GunPartType.Stock,
        GunPartType.Underbarrel
    };

    private InputAction _nextSocketAction;
    private InputAction _nextPartAction;

    private int _currentSocket;
    private int _currentPart = -1; //-1 here means nothing from this socket's list is equipped yet.

    private GunPartType CurrentSocket => Sockets[_currentSocket];

    private void Awake()
    {
        if (!_gun)
        {
            _gun = GetComponent<Gun>();
        }

        _nextSocketAction = new InputAction("Next Socket", InputActionType.Button, _nextSocketBinding);
        _nextPartAction = new InputAction("Next Part", InputActionType.Button, _nextPartBinding);
    }

    private void OnEnable()
    {
        _nextSocketAction.performed += OnNextSocket;
        _nextPartAction.performed += OnNextPart;

        _nextSocketAction.Enable();
        _nextPartAction.Enable();
    }

    private void OnDisable()
    {
        _nextSocketAction.performed -= OnNextSocket;
        _nextPartAction.performed -= OnNextPart;

        _nextSocketAction.Disable();
        _nextPartAction.Disable();
    }

    private void OnDestroy()
    {
        _nextSocketAction.Dispose();
        _nextPartAction.Dispose();
    }

    private void Start()
    {
        if (!_gun)
        {
            Debug.LogError("[PartSwapTest] No Gun assigned or found on this object.", this);
            enabled = false;
            return;
        }

        // Gun builds itself in Awake, so the default parts are already equipped here.
        if (!CanSwap(CurrentSocket))
        {
            SelectNextSocket();
        }

        SyncPartIndex();
        RefreshReadout();
    }

    private void OnNextSocket(InputAction.CallbackContext context)
    {
        SelectNextSocket();
        SyncPartIndex();
        RefreshReadout();

        Debug.Log($"[PartSwapTest] Socket: {CurrentSocket}", this);
    }

    private void OnNextPart(InputAction.CallbackContext context)
    {
        GunPart[] parts = GetParts(CurrentSocket);

        if (parts == null || parts.Length == 0)
        {
            Debug.LogWarning($"[PartSwapTest] No parts assigned for {CurrentSocket}.", this);
            return;
        }

        // +1, looping back to the start of the list
        _currentPart = (_currentPart + 1) % parts.Length;
        GunPart part = parts[_currentPart];

        if (_gun.EquipPart(part))
        {
            Debug.Log($"[PartSwapTest] {CurrentSocket} -> {GetName(part)}", this);
        }
        else
        {
            Debug.LogWarning($"[PartSwapTest] Couldn't equip element {_currentPart} for {CurrentSocket}. " +
                             "Is that element empty, or does the frame have no socket of this type?", this);
        }

        RefreshReadout();
    }

    // +1, looping, skipping sockets the frame doesn't have or that have no parts assigned.
    private void SelectNextSocket()
    {
        for (int i = 0; i < Sockets.Length; i++)
        {
            _currentSocket = (_currentSocket + 1) % Sockets.Length;

            if (CanSwap(CurrentSocket))
            {
                return;
            }
        }
    }

    // Points _currentPart at whatever is already equipped, so the next press moves to the other part.
    private void SyncPartIndex()
    {
        GunPart[] parts = GetParts(CurrentSocket);
        GunPart equipped = _gun.GetEquippedPart(CurrentSocket);

        _currentPart = parts != null && equipped ? Array.IndexOf(parts, equipped) : -1;
    }

    private bool CanSwap(GunPartType socket)
    {
        GunPart[] parts = GetParts(socket);

        return _gun.HasSocket(socket) && parts != null && parts.Length > 0;
    }

    private GunPart[] GetParts(GunPartType socket)
    {
        return socket switch
        {
            GunPartType.Barrel => _barrels,
            GunPartType.Magazine => _magazines,
            GunPartType.Scope => _scopes,
            GunPartType.Stock => _stocks,
            GunPartType.Underbarrel => _underbarrels,
            _ => null
        };
    }

    private void RefreshReadout()
    {
        GunPart current = _gun.GetEquippedPart(CurrentSocket);

        _currentSocketReadout = CurrentSocket;
        _currentPartReadout = current ? GetName(current) : "(none)";

        StringBuilder text = new();

        foreach (GunPartType socket in Sockets)
        {
            text.Append(socket == CurrentSocket ? "> " : "  ").Append(socket).Append(": ");

            if (!_gun.HasSocket(socket))
            {
                text.AppendLine("(no socket on frame)");
                continue;
            }

            GunPart part = _gun.GetEquippedPart(socket);

            if (!part)
            {
                text.AppendLine("(empty)");
                continue;
            }

            text.AppendLine(GetName(part));
            AppendStats(text, part);
        }

        _equippedStats = text.ToString();
    }

    // Reads the part type's own [SerializeField] values, so the part scripts don't need public getters.
    private static void AppendStats(StringBuilder text, GunPart part)
    {
        FieldInfo[] fields = part.GetType().GetFields(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

        bool hasStats = false;

        foreach (FieldInfo field in fields)
        {
            if (!field.IsPublic && !field.IsDefined(typeof(SerializeField), false))
            {
                continue;
            }

            text.Append("      ").Append(field.Name.TrimStart('_')).Append(": ").Append(field.GetValue(part)).AppendLine();
            hasStats = true;
        }

        if (!hasStats)
        {
            text.AppendLine("      (no stats)");
        }
    }

    private static string GetName(GunPart part)
    {
        return string.IsNullOrEmpty(part.PartName) ? part.name : part.PartName;
    }
#endif
}
