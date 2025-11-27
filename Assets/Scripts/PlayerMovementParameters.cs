using System;
using UnityEngine;

[Serializable]
public struct PlayerMovementParameters
{
    [SerializeField] public float groundAcceleration;
    [SerializeField] public float airAcceleration;
    [SerializeField] public float groundMaxVel;
    [SerializeField] public float airMaxVel;
    [SerializeField] public float friction;
    [SerializeField] public float maxFallSpeed;
    [SerializeField] public float gravity;
    [SerializeField] public float jumpVel;
    [SerializeField] public float airControl;
    [SerializeField] public float wallRunTime;
    [SerializeField] public float wallRunSpeed;
    [SerializeField] public float attackCooldown;
    [SerializeField] public float attackRange;
    // Wall run cooldown and reduced air control after walljump
    [SerializeField] public float wallRunRecoveryTime;
    [SerializeField] public float wallJumpLatVel;
    // Amount of times you can double jump
    [SerializeField] public int airJumps;
    // Whether to apply friction in air or not
    [SerializeField] public bool alwaysApplyFriction;
}