using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Plays the enemy's animations. Goes on the enemy root, next to EnemyBase. The enemy states ask for an EnemyAnimation
/// in Enter, such as Chase, and this crossfades to the Animator state picked for it in the Inspector. Each field is a
/// dropdown of the states in the Animator Controller, so an enemy's animations are set up here without renaming states.
/// Leave a field empty to skip that animation. Several fields can share one state, such as one locomotion blend tree
/// for Idle, Chase, and Reposition, and switching between them doesn't restart it. Also plays Hit when the enemy takes
/// damage, and sets the speed and direction parameters for blend trees.
/// </summary>
[RequireComponent(typeof(EnemyBase))]
public class EnemyAnimator : MonoBehaviour
{
    private const string StateNamesMethod = nameof(GetStateNames);
    private const string FloatParametersMethod = nameof(GetFloatParameters);

    [Tooltip("Leave empty to find the Animator in children.")]
    [SerializeField] private Animator _animator;
    [Tooltip("Seconds each crossfade blends over.")]
    [SerializeField, Min(0f)] private float _crossFadeTime = 0.15f;

    [Title("States")]
    [Tooltip("For a locomotion blend tree, pick the same state for Idle, Chase, and Reposition.")]
    [SerializeField, ValueDropdown(StateNamesMethod, AppendNextDrawer = true)] private string _idle = "Idle";
    [Tooltip("Played while chasing the player.")]
    [SerializeField, ValueDropdown(StateNamesMethod, AppendNextDrawer = true)] private string _chase = "Run";
    [Tooltip("Played while backing away from a player who's too close.")]
    [SerializeField, ValueDropdown(StateNamesMethod, AppendNextDrawer = true)] private string _reposition = "Reposition";
    [Tooltip("Played in AttackState between attacks. Each attack sets its own animation in its Animation State field.")]
    [SerializeField, ValueDropdown(StateNamesMethod, AppendNextDrawer = true)] private string _combatIdle = "CombatIdle";
    [Tooltip("Played when the stun starts. Give this state a transition with Has Exit Time to the looping stun state, which plays until the stun runs out.")]
    [SerializeField, ValueDropdown(StateNamesMethod, AppendNextDrawer = true)] private string _stunnedBegin = "StunnedBegin";
    [Tooltip("Played when the stun runs out. The enemy stays stunned until it finishes, so give this state no transitions out.")]
    [SerializeField, ValueDropdown(StateNamesMethod, AppendNextDrawer = true)] private string _stunnedEnd = "StunnedEnd";
    [SerializeField, ValueDropdown(StateNamesMethod, AppendNextDrawer = true)] private string _dead = "Dead";
    [Tooltip("Dark Knight only. Played while the shield is up.")]
    [SerializeField, ValueDropdown(StateNamesMethod, AppendNextDrawer = true)] private string _block = "Block";

    [Title("Hit Reaction")]
    [Tooltip("Played on the Hit Layer when the enemy takes damage, unless it's attacking. Needs a transition back to an empty state on that layer.")]
    [SerializeField, ValueDropdown(StateNamesMethod, AppendNextDrawer = true)] private string _hit = "Hit";
    [Tooltip("The Animator layer Hit plays on, so it plays over the base layer.")]
    [SerializeField, ValueDropdown(nameof(GetLayers))] private int _hitLayer = 1;

    [Title("Parameters")]
    [Tooltip("Float set to the enemy's speed every frame. Leave empty to skip.")]
    [SerializeField, ValueDropdown(FloatParametersMethod, AppendNextDrawer = true)] private string _speedParameter = "Speed";
    [Tooltip("Float set to the sideways part of the velocity, in the enemy's local space: negative is left. For a 2D directional blend tree. Leave empty to skip.")]
    [SerializeField, ValueDropdown(FloatParametersMethod, AppendNextDrawer = true)] private string _moveXParameter = "MoveX";
    [Tooltip("Float set to the forward part of the velocity, in the enemy's local space: negative is backward. For a 2D directional blend tree. Leave empty to skip.")]
    [SerializeField, ValueDropdown(FloatParametersMethod, AppendNextDrawer = true)] private string _moveZParameter = "MoveZ";
    [Tooltip("On: the three parameters are divided by the enemy's Move Speed stat, so full speed is 1. Use this when the blend tree's positions go from -1 to 1. Off: meters per second.")]
    [SerializeField] private bool _normalizeBySpeed = true;
    [Tooltip("Seconds the parameters take to catch up, so the blend doesn't snap when the agent turns.")]
    [SerializeField, Min(0f)] private float _parameterDampTime = 0.1f;

    private EnemyBase _enemy;
    private Health _health;
    private EnemyMovement _movement;
    private EnemyAnimation? _currentStateAnimation; // The animation the current state asked for, so ReturnToState can go back to it.

    // Hashes of the float parameters the Animator has. 0 when it doesn't have one, so Update skips it.
    private int _speedHash;
    private int _moveXHash;
    private int _moveZHash;

    private void Awake()
    {
        _enemy = GetComponent<EnemyBase>();
        _health = GetComponent<Health>();
        _movement = GetComponent<EnemyMovement>();

        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();

        _speedHash = FindFloatParameter(_speedParameter);
        _moveXHash = FindFloatParameter(_moveXParameter);
        _moveZHash = FindFloatParameter(_moveZParameter);
    }

    private void OnEnable()
    {
        if (_health != null)
            _health.OnDamaged += HandleDamaged;
    }

    private void OnDisable()
    {
        if (_health != null)
            _health.OnDamaged -= HandleDamaged;
    }

    private void Update()
    {
        if (_movement == null || _enemy.IsDead)
            return;

        Vector3 localVelocity = transform.InverseTransformDirection(_movement.Velocity);
        localVelocity.y = 0f;

        if (_normalizeBySpeed)
            localVelocity = _enemy.MoveSpeed > 0f ? localVelocity / _enemy.MoveSpeed : Vector3.zero;

        SetFloat(_speedHash, localVelocity.magnitude);
        SetFloat(_moveXHash, localVelocity.x);
        SetFloat(_moveZHash, localVelocity.z);
    }

    /// <summary>Plays a state's animation and remembers it, so ReturnToState can go back to it. States call this in Enter.</summary>
    public void PlayState(EnemyAnimation animation)
    {
        _currentStateAnimation = animation;
        Play(GetStateName(animation), 0, false);
    }

    public void ReturnToState()
    {
        if (_currentStateAnimation.HasValue)
            Play(GetStateName(_currentStateAnimation.Value), 0, false);
    }

    /// <summary>
    /// Plays the Animator state picked for a one-off animation, such as Block or Hit, without changing the current
    /// state's animation. Restarts it if it's already playing.
    /// </summary>
    public void Play(EnemyAnimation animation)
    {
        if (animation == EnemyAnimation.Hit)
            Play(_hit, _hitLayer);
        else
            Play(GetStateName(animation));
    }

    /// <summary>
    /// Crossfades to an Animator state by name, such as an attack's Animation State. Skipped when the name is empty
    /// or the layer has no state with that name, so a missing animation keeps the current one playing.
    /// </summary>
    /// <param name="stateName">The Animator state's name.</param>
    /// <param name="layer">The Animator layer. 0 is the base layer.</param>
    /// <param name="restartIfPlaying">
    /// True plays it from the start even if it's already playing, such as a second swing. False leaves it running,
    /// so states that share one blend tree don't restart it.
    /// </param>
    public void Play(string stateName, int layer = 0, bool restartIfPlaying = true)
    {
        if (_animator == null || _animator.runtimeAnimatorController == null || string.IsNullOrEmpty(stateName))
            return;

        if (layer < 0 || layer >= _animator.layerCount)
            return;

        int stateHash = Animator.StringToHash(stateName);
        if (!_animator.HasState(layer, stateHash))
            return;

        if (!restartIfPlaying && IsPlayingOrEntering(stateHash, layer))
            return;

        _animator.CrossFadeInFixedTime(stateHash, _crossFadeTime, layer);
    }

    /// <summary>
    /// True once the base layer has played the animation's state to the end. Also true when the animation has no state,
    /// so an enemy without it moves on right away. False while blending into it, including the frame after it's asked
    /// for, before the Animator has started the crossfade.
    /// </summary>
    public bool HasFinished(EnemyAnimation animation)
    {
        string stateName = GetStateName(animation);

        if (_animator == null || _animator.runtimeAnimatorController == null || string.IsNullOrEmpty(stateName))
            return true;

        int stateHash = Animator.StringToHash(stateName);
        if (!_animator.HasState(0, stateHash))
            return true;

        if (_animator.IsInTransition(0))
            return false;

        AnimatorStateInfo current = _animator.GetCurrentAnimatorStateInfo(0);
        return current.shortNameHash == stateHash && current.normalizedTime >= 1f;
    }

    private bool IsPlayingOrEntering(int stateHash, int layer)
    {
        if (_animator.IsInTransition(layer))
            return _animator.GetNextAnimatorStateInfo(layer).shortNameHash == stateHash;

        return _animator.GetCurrentAnimatorStateInfo(layer).shortNameHash == stateHash;
    }

    private void SetFloat(int parameterHash, float value)
    {
        if (parameterHash != 0)
            _animator.SetFloat(parameterHash, value, _parameterDampTime, Time.deltaTime);
    }

    /// <summary>Plays Hit, unless the enemy is attacking or dead, so the swing the player sees matches the hit they take.</summary>
    private void HandleDamaged(int currentHealth, GameObject owner)
    {
        if (currentHealth <= 0 || _enemy.IsDead || _enemy.IsAttacking)
            return;

        Play(EnemyAnimation.Hit);
    }

    private string GetStateName(EnemyAnimation animation)
    {
        return animation switch
        {
            EnemyAnimation.Idle => _idle,
            EnemyAnimation.Chase => _chase,
            EnemyAnimation.Reposition => _reposition,
            EnemyAnimation.CombatIdle => _combatIdle,
            EnemyAnimation.StunnedBegin => _stunnedBegin,
            EnemyAnimation.StunnedEnd => _stunnedEnd,
            EnemyAnimation.Dead => _dead,
            EnemyAnimation.Hit => _hit,
            EnemyAnimation.Block => _block,
            _ => null
        };
    }

    /// <summary>
    /// Returns the parameter's hash if the Animator has a float with that name, or 0, so Update skips parameters that are
    /// missing instead of logging a warning every frame.
    /// </summary>
    private int FindFloatParameter(string parameterName)
    {
        if (_animator == null || _animator.runtimeAnimatorController == null || string.IsNullOrEmpty(parameterName))
            return 0;

        int hash = Animator.StringToHash(parameterName);

        foreach (AnimatorControllerParameter parameter in _animator.parameters)
        {
            if (parameter.nameHash == hash && parameter.type == AnimatorControllerParameterType.Float)
                return hash;
        }

        return 0;
    }

    #region Inspector

    /// <summary>
    /// Every state name in the Animator Controller, on every layer and in sub-state machines, for the dropdowns here and
    /// on EnemyAttack. Empty outside the editor.
    /// </summary>
    public IEnumerable<string> GetStateNames()
    {
        List<string> names = new List<string> { "" };

#if UNITY_EDITOR
        UnityEditor.Animations.AnimatorController controller = FindController();
        if (controller == null)
            return names;

        foreach (UnityEditor.Animations.AnimatorControllerLayer layer in controller.layers)
        {
            AddStateNames(layer.stateMachine, names);
        }
#endif

        return names;
    }

    /// <summary>Debug: lists every animation field whose state isn't in the Animator Controller.</summary>
    [Button("Check States")]
    private void CheckStates()
    {
        HashSet<string> stateNames = new HashSet<string>(GetStateNames());
        List<string> missing = new List<string>();

        foreach (EnemyAnimation animation in System.Enum.GetValues(typeof(EnemyAnimation)))
        {
            string stateName = GetStateName(animation);
            if (!string.IsNullOrEmpty(stateName) && !stateNames.Contains(stateName))
                missing.Add($"{animation} ({stateName})");
        }

        foreach (EnemyAttack attack in GetComponentsInChildren<EnemyAttack>(true))
        {
            if (!string.IsNullOrEmpty(attack.AnimationState) && !stateNames.Contains(attack.AnimationState))
                missing.Add($"{attack.name} ({attack.AnimationState})");
        }

        if (missing.Count == 0)
            Debug.Log($"{name}: every animation has a state in the Animator Controller.", this);
        else
            Debug.LogWarning($"{name}: these animations have no matching state and will be skipped: {string.Join(", ", missing)}", this);
    }

    private ValueDropdownList<int> GetLayers()
    {
        ValueDropdownList<int> layers = new ValueDropdownList<int>();

#if UNITY_EDITOR
        UnityEditor.Animations.AnimatorController controller = FindController();
        if (controller != null)
        {
            for (int i = 0; i < controller.layers.Length; i++)
            {
                layers.Add($"{i}: {controller.layers[i].name}", i);
            }
        }
#endif

        if (layers.Count == 0)
            layers.Add("1", 1);

        return layers;
    }

    private IEnumerable<string> GetFloatParameters()
    {
        List<string> names = new List<string> { "" };

#if UNITY_EDITOR
        UnityEditor.Animations.AnimatorController controller = FindController();
        if (controller == null)
            return names;

        foreach (AnimatorControllerParameter parameter in controller.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Float)
                names.Add(parameter.name);
        }
#endif

        return names;
    }

#if UNITY_EDITOR
    /// <summary>The Animator's controller, looking through an Animator Override Controller to the one it overrides.</summary>
    private UnityEditor.Animations.AnimatorController FindController()
    {
        Animator animator = _animator != null ? _animator : GetComponentInChildren<Animator>(true);
        RuntimeAnimatorController runtimeController = animator != null ? animator.runtimeAnimatorController : null;

        if (runtimeController is AnimatorOverrideController overrideController)
            runtimeController = overrideController.runtimeAnimatorController;

        return runtimeController as UnityEditor.Animations.AnimatorController;
    }

    private static void AddStateNames(UnityEditor.Animations.AnimatorStateMachine stateMachine, List<string> names)
    {
        foreach (UnityEditor.Animations.ChildAnimatorState child in stateMachine.states)
        {
            if (!names.Contains(child.state.name))
                names.Add(child.state.name);
        }

        foreach (UnityEditor.Animations.ChildAnimatorStateMachine child in stateMachine.stateMachines)
        {
            AddStateNames(child.stateMachine, names);
        }
    }
#endif

    #endregion
}