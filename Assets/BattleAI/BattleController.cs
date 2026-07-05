using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

public class BattleController : MonoBehaviour
{
    [SerializeField] TMP_Text nameA, nameB;
    [SerializeField] TMP_Text displayA, displayB;
    [SerializeField] TMP_Text thoughts;

    [SerializeField] private List<RaceDefinition> races;

    private readonly List<BattleUnit> foes = new();
    private readonly List<BattleUnit> player = new();

    [SerializeField] private int maxEnemyUnits = 1;
    [SerializeField] private int maxAllyUnits = 1;

    private int currentAllyID = 0;
    private int currentEnemyID = 0;

    private int turnNumber = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartEncounter();
    }

    public void StartEncounter()
    {
        if (races.Count == 0)
        {
            Debug.LogError("No races available.");
            return;
        }
        if (maxAllyUnits <= 0 || maxEnemyUnits <= 0)
        {
            Debug.LogError("Unit counts must be greater than zero.");
            return;
        }

        player.Clear();
        foes.Clear();

        int pickA = -1;
        int pickE = -1;

        // Randomize Unit Races
        for (int i = 0; i < maxAllyUnits; i++)
        {
            pickA = UnityEngine.Random.Range(0, races.Count);
            BattleUnit p = new BattleUnit();
            p.SetRace(races[pickA]);
            p.Initialize();
            player.Add(p);
        }

        for (int i = 0; i < maxEnemyUnits; i++)
        {
            pickE = UnityEngine.Random.Range(0, races.Count);
            BattleUnit e = new BattleUnit();
            e.SetRace(races[pickE]);
            e.Initialize();
            foes.Add(e);
        }

        //Testing Purposes
        foes.Clear();
        player.Clear();
        foreach(RaceDefinition r in races)
        {
            BattleUnit e = new BattleUnit();
            BattleUnit p = new BattleUnit();
            e.SetRace(r);
            e.Initialize();
            foes.Add(e);

            p.SetRace(r);
            p.Initialize();
            player.Add(p);
        }

        nameA.text = player[0].GetRace().raceName;
        nameB.text = foes[0].GetRace().raceName;

        UpdateDisplays();
    }

    public void UpdateDisplays()
    {
        var p = player[currentAllyID];
        nameA.text = p.GetRace().raceName;
        displayA.text =
            "HP:\t" + p.Stats[BattleStats.HP] + "/" + p.GetRace().HP + "\n"
            + "ATK:\t" + p.Stats[BattleStats.ATK] + "\n"
            + "DEF:\t" + p.Stats[BattleStats.DEF] + "\n"
            + "INT:\t" + p.Stats[BattleStats.INT] + "\n"
            + "WIS:\t" + p.Stats[BattleStats.WIS] + "\n"
            + "SPD:\t" + p.Stats[BattleStats.SPD];
        var e = foes[currentEnemyID];
        nameB.text = e.GetRace().raceName;
        displayB.text =
            "HP:\t" + e.Stats[BattleStats.HP] + "/" + e.GetRace().HP + "\n"
            + "ATK:\t" + e.Stats[BattleStats.ATK] + "\n"
            + "DEF:\t" + e.Stats[BattleStats.DEF] + "\n"
            + "INT:\t" + e.Stats[BattleStats.INT] + "\n"
            + "WIS:\t" + e.Stats[BattleStats.WIS] + "\n"
            + "SPD:\t" + e.Stats[BattleStats.SPD];
    }

    private void ExecuteAction(BattleAction action)
    {
        turnNumber++;
        thoughts.text = "Round " + turnNumber + " Commencing...";
        BattleUnit self = player[currentAllyID];
        BattleUnit target = foes[currentEnemyID];

        if(self.isDead)
        {
            thoughts.text += "\n\nPlayer unit is unconscious. No action can be taken!";
            return;
        }
        if (target.isDead)
        {
            thoughts.text += "\n\nEnemy unit is unconscious. Action cannot be taken!";
            return;
        }

        BattleUnit[] turnOrder = new BattleUnit[2];
        if (self.Stats[BattleStats.SPD] >= target.Stats[BattleStats.SPD])
        {
            turnOrder[0] = self;
            turnOrder[1] = target;
        } else
        {
            turnOrder[0] = target;
            turnOrder[1] = self;
        }

        for (int i = 0; i < turnOrder.Length; i++)
        {
            if (turnOrder.Any(u => u.isDead)) continue;
            if (turnOrder[i] == self)
            {
                if (self.Stats[BattleStats.DEF] != self.GetRace().DEF)
                    self.Stats[BattleStats.DEF] /= 1.5f;
                switch (action)
                {
                    case BattleAction.Attack:
                        thoughts.text += "\n\nPlayer " + self.GetRace().raceName + " chose to ATTACK";
                        var dmg = Mathf.Max(1.0f, self.Stats[BattleStats.ATK] - target.Stats[BattleStats.DEF]);
                        thoughts.text += "\n\n" + target.GetRace().raceName + " has taken " + dmg + " Damage";

                        target.lastDmgTaken = dmg;
                        //target.AI.AddMemory(turnNumber, BattleAction.Defend, Condition.DamageTaken, Mathf.Clamp(dmg / target.GetRace().HP, 0f, 1f));
                        target.ApplyChange(BattleStats.HP, -dmg);
                        if (target.isDead)
                            thoughts.text += "\n\nPlayer " + target.GetRace().raceName + " has been knocked out!";
                        break;
                    case BattleAction.Defend:
                        thoughts.text += "\n\nPlayer " + self.GetRace().raceName + " chose to DEFEND";
                        self.Stats[BattleStats.DEF] *= 1.5f;
                        break;
                    case BattleAction.Run:
                        thoughts.text += "\n\nPlayer " + self.GetRace().raceName + " chose to RUN";
                        self.isDead = true;
                        break;
                    default:
                        Debug.LogError("Invalid Action Selected");
                        return;
                }
            }
            else
            {
                if (target.Stats[BattleStats.DEF] != target.GetRace().DEF)
                    target.Stats[BattleStats.DEF] /= 1.5f;
                var temp = target.AI.memories.ToArray();
                foreach(BattleMemory mem in temp)
                {
                    if (mem.turnId >= turnNumber)
                    {
                        if (mem.conditions.Any(u => u.Condition == Condition.DamageTaken))
                        {
                            if (target.isDefending)
                            {
                                //Get DMG and compare
                                var normalDMG = Mathf.Max(1.0f, self.GetRace().ATK - target.GetRace().DEF);
                                var defWeight = Mathf.Clamp((mem.conditions[0].weight - normalDMG) / target.GetRace().HP, -1.0f, 1.0f);
                                target.AI.AddMemory(turnNumber, BattleAction.Defend, Condition.DefenseSuccess, defWeight);
                                target.isDefending = false;
                            }
                        }
                    }
                }

                BattleAction decision = target.AI.Decide(target, thoughts);
                switch (decision)
                {
                    case BattleAction.Attack:
                        thoughts.text += "\n\n" + target.GetRace().raceName + " chose to ATTACK";
                        var dmg = Mathf.Max(1.0f, target.Stats[BattleStats.ATK] - self.Stats[BattleStats.DEF]);
                        thoughts.text += "\n\nPlayer " + self.GetRace().raceName + " has taken " + dmg + " Damage";
                        //target.AI.AddMemory(turnNumber, BattleAction.Attack, Condition.DamageDealt, Mathf.Clamp(dmg/self.GetRace().HP, 0f, 1f));
                        
                        self.ApplyChange(BattleStats.HP, -dmg);
                        if (self.isDead)
                            thoughts.text += "\n\nPlayer " + self.GetRace().raceName + " has been knocked out!";
                        break;
                    case BattleAction.Skill:
                        thoughts.text += "\n\n" + target.GetRace().raceName + " chose to HEAL";
                        var heal = target.GetRace().HP * 0.1f;
                        thoughts.text += "\n" + self.GetRace().raceName + " has recovered " + heal + " Health";
                        target.Stats[BattleStats.HP] += heal;
                        break;
                    case BattleAction.Defend:
                        thoughts.text += "\n\n" + target.GetRace().raceName + " chose to DEFEND";
                        target.Stats[BattleStats.DEF] *= 1.5f;
                        //target.AI.AddMemory(turnNumber, BattleAction.Defend, Condition.Defended, 0f);
                        target.isDefending = true;
                        break;
                    case BattleAction.Run:
                        thoughts.text += "\n\n" + target.GetRace().raceName + " chose to RUN";
                        target.isDead = true;
                        break;
                    default:
                        Debug.LogError("Invalid Action Selected");
                        return;
                }
                
            }
        }

        //Non-Direction Action Memories Here

        //Run Away Memory Logic
        var dmgTaken = Mathf.Max(0f, self.GetRace().ATK - target.GetRace().DEF);
        var dmgDealt = Mathf.Max(0f, target.GetRace().ATK - self.GetRace().DEF);
        float victoryChance = Mathf.Min(1f, dmgDealt / self.Stats[BattleStats.HP]);
        float lossChance = Mathf.Min(1f, dmgTaken / target.Stats[BattleStats.HP]);
        if (victoryChance > lossChance)
            target.AI.AddMemory(turnNumber, BattleAction.Run, Condition.WillLose, -victoryChance);
        else if (victoryChance < lossChance)
            target.AI.AddMemory(turnNumber, BattleAction.Run, Condition.WillLose, lossChance);

        //Alarm Memory Logic
        if (Mathf.Clamp01(target.lastDmgTaken / target.GetRace().HP) > 0.1f)
            target.AI.AddMemory(turnNumber, BattleAction.Defend, Condition.Alarm, Mathf.Clamp01(target.lastDmgTaken / target.Stats[BattleStats.HP]));

        //Heal Memory Logic
        float currentPercentage = Mathf.Clamp01(target.Stats[BattleStats.HP] / target.GetRace().HP);
        if (currentPercentage < 0.5f)
            target.AI.AddMemory(turnNumber, BattleAction.Skill, Condition.WillLose, (0.5f - currentPercentage) * 2);

        //Continue Attacking Logic
        target.AI.AddMemory(turnNumber, BattleAction.Attack, Condition.WillLose, Mathf.Clamp(victoryChance - lossChance, -1.0f, 1.0f));

        while (target.AI.capacity < target.AI.memories.Count)
        {
            target.AI.memories.Dequeue();
        }
        target.lastDmgTaken = 0;

        if (!player.Any(u => !u.isDead) && !foes.Any(u => !u.isDead))
        {
            Debug.Log("It's a tie!");
            thoughts.text += "\n\nIt's a tie!";
            //StartEncounter();
            return;
        }
        if (!player.Any(u => !u.isDead))
        {
            Debug.Log("Foes Win!");
            thoughts.text += "\n\nFoes Win!";
            //StartEncounter();
            return;
        }
        if(!foes.Any(u => !u.isDead))
        {
            Debug.Log("Allies Win!");
            thoughts.text += "\n\nAllies Win!";
            //Start Encounter();
            return;
        }

        if (self.isDead) SwitchAlly();
        if (target.isDead) SwitchFoe();

        UpdateDisplays();
    }

    public void SwitchAlly()
    {
        currentAllyID++;
        if(currentAllyID >= player.Count)
        {
            currentAllyID = 0;
        }
        UpdateDisplays();
    }

    public void SwitchFoe()
    {
        currentEnemyID++;
        if (currentEnemyID >= foes.Count)
        {
            currentEnemyID = 0;
        }
        UpdateDisplays();
    }

    public void Attack()
    {
        ExecuteAction(BattleAction.Attack);
    }

    public void Defend()
    {
        ExecuteAction(BattleAction.Defend);
    }

    public void Run()
    {
        ExecuteAction(BattleAction.Run);
    }
}
