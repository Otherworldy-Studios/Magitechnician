using System;
using UnityEngine;

/// <summary>
/// Turns the player's upper body and arms after the animation each frame, so the gun's barrel points at the crosshair's
/// aim point while the player is aiming or shooting. Bullets already fly from the muzzle to the aim point; this makes the barrel
/// line up with them, so shots don't leave the gun at an angle while the run or idle animation swings it around. Blends
/// in while aiming or on each shot, and back out to the plain animation once the player stops both. Visual only: aiming and hits are
/// unchanged. Goes on the player, next to Shoot.
/// </summary>
public class GunAimer : MonoBehaviour
{
    [Serializable]
    private class AimBone
    {
        public Transform Bone;
        [Tooltip("Share of the remaining correction this bone takes, from 0 to 1. Give the last bone 1, so the barrel ends up exactly on target.")]
        [Range(0f, 1f)] public float Weight = 0.5f;
    }

    [Tooltip("Found on this object or in children if empty.")]
    [SerializeField] private Shoot _shoot;
    [Tooltip("Found in children if empty. Its Muzzle's blue (Z) axis has to point out of the barrel.")]
    [SerializeField] private Gun _gun;

    [Tooltip("Bones turned toward the aim point, from the hips up, such as Spine 0.3, Chest 0.5, Right Hand 1. Turning the spine first keeps both hands on a two-handed gun.")]
    [SerializeField] private AimBone[] _bones = Array.Empty<AimBone>();

    [Tooltip("How much of the correction is applied, from 0 (animation only) to 1 (barrel on target).")]
    [SerializeField, Range(0f, 1f)] private float _weight = 1f;
    [Tooltip("Largest angle each bone turns to line up the barrel. Stops strange poses when the aim point is behind or far below the gun.")]
    [SerializeField, Range(0f, 90f)] private float _maxAngle = 60f;
    [Tooltip("How fast the barrel follows the aim point. Higher is snappier. Smooths the jump when the crosshair moves from a near wall to a far one.")]
    [SerializeField, Min(0.1f)] private float _followSpeed = 20f;

    [Header("Aiming and Shooting")]
    [SerializeField] private PlayerCombatState _combatState;
    [Tooltip("Seconds to blend fully in or out.")]
    [SerializeField, Min(0.01f)] private float _blendTime = 0.12f;

    private Vector3 _smoothedAimPoint;
    private bool _hasSmoothedAimPoint;
    private float _blend; // 0 while not aiming or shooting, 1 while either. Eases between them over Blend Time.

    private void Awake()
    {
        if (_shoot == null)
            _shoot = GetComponentInChildren<Shoot>();

        if (_gun == null)
            _gun = GetComponentInChildren<Gun>();
        
        if (_combatState == null)
            _combatState = GetComponentInChildren<PlayerCombatState>();
    }

    // Runs after the Animator has posed the skeleton this frame, so the correction is added on top of the animation.
    private void LateUpdate()
    {
        if (_shoot == null || !_shoot.HasAim || _gun == null || _gun.Muzzle == null || _weight <= 0f)
            return;

        bool isInCombat =
            _combatState.CurrentCombatState == PlayerCombatStates.Aiming ||
            _combatState.CurrentCombatState == PlayerCombatStates.HipFire;

        float target = isInCombat ? 1f : 0f;
        _blend = Mathf.MoveTowards(_blend, target, Time.deltaTime / _blendTime);

        Vector3 aimPoint = _shoot.LastAim.AimPoint;

        // Not aiming or shooting: start from the current aim point next time, instead of from where the last burst ended.
        if (_blend <= 0f)
        {
            _hasSmoothedAimPoint = false;
            return;
        }

        if (!_hasSmoothedAimPoint)
        {
            _smoothedAimPoint = aimPoint;
            _hasSmoothedAimPoint = true;
        }
        else
        {
            _smoothedAimPoint = Vector3.Lerp(_smoothedAimPoint, aimPoint, 1f - Mathf.Exp(-_followSpeed * Time.deltaTime));
        }

        foreach (AimBone aimBone in _bones)
        {
            if (aimBone.Bone != null)
                TurnBone(aimBone.Bone, aimBone.Weight * _weight * _blend);
        }
    }

    /// <summary>
    /// Turns one bone by its share of the angle between the barrel and the direction from the muzzle to the aim point.
    /// Each bone measures what's left after the bones before it, so the shares add up instead of overshooting.
    /// </summary>
    private void TurnBone(Transform bone, float share)
    {
        Transform muzzle = _gun.Muzzle;
        Vector3 toAim = _smoothedAimPoint - muzzle.position;

        if (toAim.sqrMagnitude < 0.0001f)
            return;

        Quaternion fullCorrection = Quaternion.FromToRotation(muzzle.forward, toAim.normalized);
        Quaternion limited = Quaternion.RotateTowards(Quaternion.identity, fullCorrection, _maxAngle);
        Quaternion correction = Quaternion.Slerp(Quaternion.identity, limited, share);

        bone.rotation = correction * bone.rotation;
    }
}