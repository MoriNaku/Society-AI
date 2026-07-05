using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Battle/Race")]
public class RaceDefinition : ScriptableObject
{
    public string raceName;

    [Header("Base Stats")]
    public float HP;
    public float ATK;
    public float DEF;
    public float SPD;
    public float WIS;
    public float INT;

    [Header("Racial Stats")]
    public float Aggression;
    public float Caution;
    public float Memory;

    [Header("Biases")]
    public List<BattleBias> biases;
}

[System.Serializable]
public class BattleBias
{
    public BattleAction action;
    public float weight;
}
