using UnityEngine;
using System;

[CreateAssetMenu(fileName = "EntityStats", menuName = "Scriptable Objects/EntityStats")]
public class EntityStats : ScriptableObject
{
    //Base values. Used BaseStats so it can be applied to any entity type (Player, Enemy, etc.)
    [Serializable]
    public struct BaseStats
    {
        public float _speed;
        public float _maxHealth;
        public float _baseDamage;
                   
        public float Get(statType type) => type switch
        {
            statType.speed => _speed,
            statType.maxHealth => _maxHealth,
            statType.baseDamage => _baseDamage,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        public static readonly BaseStats Default = new BaseStats
        {
            _speed = 5,
            _maxHealth = 100,
            _baseDamage = 20
        };
    }

    public BaseStats baseStats = BaseStats.Default;

}
