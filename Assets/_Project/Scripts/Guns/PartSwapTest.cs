using System;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Old debug tool for swapping gun parts at run time: "[" moves to the next socket, and "]" cycles through the parts
/// assigned to that socket. After every press it writes a readout of everything equipped into its own Inspector fields.
/// </summary>
// INTEGRATION: off the player prefab. Gun.EquipPart and Gun.RemovePart are Inspector buttons now, which replace this.
// Delete the file in cleanup. It creates its own InputActions, which the input standard no longer allows.
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
    // The sockets the keys cycle through, in order.
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

    private int _currentSocket; // Index into Sockets.
    private int _currentPart = -1; // Index into the current socket's parts. -1 means nothing from the list is equipped yet.

    private GunPartType CurrentSocket => Sockets[_currentSocket];

    /// <summary>Finds the Gun and creates the two key actions from the binding paths.</summary>
    private void Awake()
    {
        if (!_gun)
        {
            _gun = GetComponent<Gun>();
        }

        _nextSocketAction = new InputAction("Next Socket", InputActionType.Button, _nextSocketBinding);
        _nextPartAction = new InputAction("Next Part", InputActionType.Button, _nextPartBinding);
    }

    /// <summary>Listens to and turns on the key actions.</summary>
    private void OnEnable()
    {
        _nextSocketAction.performed += OnNextSocket;
        _nextPartAction.performed += OnNextPart;

        _nextSocketAction.Enable();
        _nextPartAction.Enable();
    }

    /// <summary>Stops listening to and turns off the key actions.</summary>
    private void OnDisable()
    {
        _nextSocketAction.performed -= OnNextSocket;
        _nextPartAction.performed -= OnNextPart;

        _nextSocketAction.Disable();
        _nextPartAction.Disable();
    }

    /// <summary>Releases the key actions.</summary>
    private void OnDestroy()
    {
        _nextSocketAction.Dispose();
        _nextPartAction.Dispose();
    }

    /// <summary>Selects the first socket that has parts, and writes the first readout.</summary>
    private void Start()
    {
        if (!_gun)
        {
            Debug.LogError("[PartSwapTest] No Gun assigned or found on this object.", this);
            enabled = false;
            return;
        }

        // Gun builds itself in its Start, which may run after this one, so the readout can start empty.
        if (!CanSwap(CurrentSocket))
        {
            SelectNextSocket();
        }

        SyncPartIndex();
        RefreshReadout();
    }

    /// <summary>
    /// Moves to the next socket that has parts, then points the part index at the part already equipped there,
    /// so the next "]" press moves to another part instead of equipping the same one again.
    /// </summary>
    private void OnNextSocket(InputAction.CallbackContext context)
    {
        SelectNextSocket();
        SyncPartIndex();
        RefreshReadout();

        Debug.Log($"[PartSwapTest] Socket: {CurrentSocket}", this);
    }

    /// <summary>Equips the next part in the current socket's list, looping back to the start.</summary>
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

    /// <summary>Moves to the next socket, looping, and skips sockets the gun doesn't have or that have no parts assigned.</summary>
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

    /// <summary>Points the part index at whatever is already equipped, so the next press moves to another part.</summary>
    private void SyncPartIndex()
    {
        GunPart[] parts = GetParts(CurrentSocket);
        GunPart equipped = _gun.GetEquippedPart(CurrentSocket);

        _currentPart = parts != null && equipped ? Array.IndexOf(parts, equipped) : -1;
    }

    /// <summary>True when the gun has the socket and the socket has parts assigned.</summary>
    private bool CanSwap(GunPartType socket)
    {
        GunPart[] parts = GetParts(socket);

        return _gun.HasSocket(socket) && parts != null && parts.Length > 0;
    }

    /// <summary>Returns the parts assigned to a socket.</summary>
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

    /// <summary>Writes the current socket, the current part, and every equipped part with its values into the readout fields.</summary>
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

    /// <summary>Appends the part class's own serialized values, read by reflection, so the part scripts don't need public getters.</summary>
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

    /// <summary>Returns the part's name, or its asset name if the name is empty.</summary>
    private static string GetName(GunPart part)
    {
        return string.IsNullOrEmpty(part.PartName) ? part.name : part.PartName;
    }
#endif
}
