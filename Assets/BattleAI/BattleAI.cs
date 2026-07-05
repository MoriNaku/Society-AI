using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using TMPro;

public enum BattleAction
{
    Attack,
    Skill,
    Defend,
    Run
}

public enum Condition
{
    DamageTaken,
    DamageDealt,
    Defended,
    DefenseSuccess,
    WillLose,
    Alarm
}

public class BattleMemory
{
    public int targetId {  get; }
    public int turnId { get; }
    public List<BattleCondition> conditions = new();

    public BattleMemory(int id)
    {
        this.turnId = id;
    }

}

public struct BattleCondition
{
    public Condition Condition;
    public BattleAction BattleAction;
    public float weight;
}

public class BattleUnit
{
    private RaceDefinition race;
    public BattleAI AI = new();
    public Dictionary<BattleStats, float> Stats = new();
    public bool isDead = false;
    public bool isRunning = false;
    public bool isDefending = false;

    //Temporary Use Stuff
    public float lastDmgTaken = 0f;

    public void SetRace(RaceDefinition r) => this.race = r;
    public RaceDefinition GetRace() => this.race;
    public void Initialize()
    {
        if (race == null)
        {
            Debug.LogError("No Race Set");
            return;
        }
        Stats.Add(BattleStats.HP, race.HP);
        Stats.Add(BattleStats.ATK, race.ATK);
        Stats.Add(BattleStats.DEF, race.DEF);
        Stats.Add(BattleStats.SPD, race.SPD);
        Stats.Add(BattleStats.INT, race.INT);
        Stats.Add(BattleStats.WIS, race.WIS);

        AI.capacity += (int)race.WIS;
    }

    public void ApplyChange(BattleStats stat, float value)
    {
        Stats[stat] += value;

        if (Stats[BattleStats.HP] <= 0f)
        {
            Stats[BattleStats.HP] = 0f;
            isDead = true;
        }
        else
        {
            isDead = false;
        }
    }
}

public class BattleAI
{
    public Queue<BattleMemory> memories = new();
    public int capacity = 4;
 
    public void AddMemory(int turnId, BattleAction action, Condition cond, float value)
    {
        BattleMemory mem = new BattleMemory(turnId);
        BattleCondition bC = new BattleCondition();
        bC.Condition = cond;
        bC.BattleAction = action;
        bC.weight = value;
        mem.conditions.Add(bC);
        memories.Enqueue(mem);
    }

    private BattleAction ChooseActionTieRandom(float a, float sk, float d, float r, float epsilon = 0.0001f)
    {
        float[] s = { a, sk, d, r };

        for (int i = 0; i < s.Length; i++)
            if (float.IsNaN(s[i])) s[i] = float.NegativeInfinity;

        float best = s[0];
        for (int i = 1; i < s.Length; i++)
            if (s[i] > best) best = s[i];

        List<int> ties = new List<int>(4);
        for (int i = 0;i < s.Length; i++) 
            if(Mathf.Abs(s[i] - best) <= epsilon)
                ties.Add(i);

        var pick = ties[UnityEngine.Random.Range(0, ties.Count)];
        return (BattleAction)pick;
    }

    public BattleAction Decide(BattleUnit self, TMP_Text display)
    {
        float racialBias = 0.5f;
        float heroBias = 0.0f;
        float memoryBias = 0.8f;

        float atkScore = 0f;
        float defScore = 0f;
        float sklScore = 0f;
        float runScore = 0f;

        display.text += "\n\n" + self.GetRace().raceName + " has started thinking...";

        // Attack Score calucation
        float memoryBonus = 0f;
        int memCount = 0;
        foreach (BattleMemory mem in memories)
            foreach (BattleCondition c in mem.conditions)
                if (c.BattleAction == BattleAction.Attack) {
                    memoryBonus += c.weight;
                    memCount++;
                }
        if (memCount > 0)
            memoryBonus /= memCount;
        memoryBonus *= memoryBias;

        float racialBonus = 0f;
        int raceCount = 0;
        foreach (BattleBias b in self.GetRace().biases)
            if (b.action == BattleAction.Attack) {
                racialBonus += b.weight;
                raceCount++;
            }
        if (raceCount > 0)
            racialBonus /= raceCount;
        racialBonus *= racialBias;

        float heroBonus = 0f;
        int heroCount = 0;
        //Hero logic here
        if (heroCount > 0)
            heroBonus /= heroCount;
        heroBonus *= heroBias;

        atkScore += memoryBonus + racialBonus + heroBonus;
        display.text += "\natkScore: " + atkScore.ToString("F2") + "[memory: " + memoryBonus.ToString("F2") + ", race: " + racialBonus.ToString("F2") + ", hero: " + heroBonus.ToString("F2") + "]"; ;

        // Skill Score calculation
        memoryBonus = 0f;
        memCount = 0;
        foreach (BattleMemory mem in memories)
            foreach (BattleCondition c in mem.conditions)
                if (c.BattleAction == BattleAction.Skill)
                    memoryBonus += c.weight;
        if (memCount > 0)
            memoryBonus /= memCount;
        memoryBonus *= memoryBias;

        racialBonus = 0f;
        raceCount = 0;
        foreach (BattleBias b in self.GetRace().biases)
            if (b.action == BattleAction.Skill)
            {
                racialBonus += b.weight;
                raceCount++;
            }
        if (raceCount > 0)
            racialBonus /= raceCount;
        racialBonus *= racialBias;

        heroBonus = 0f;
        heroCount = 0;
        //Hero logic here
        if (heroCount > 0)
            heroBonus /= heroCount;
        heroBonus *= heroBias;

        sklScore += memoryBonus + racialBonus + heroBonus;
        display.text += "\nsklScore: " + sklScore.ToString("F2") + "[memory: " + memoryBonus.ToString("F2") + ", race: " + racialBonus.ToString("F2") + ", hero: " + heroBonus.ToString("F2") + "]"; ;

        // Defense Score calculation
        memoryBonus = 0f;
        memCount = 0;
        foreach (BattleMemory mem in memories)
            foreach (BattleCondition c in mem.conditions)
                if (c.BattleAction == BattleAction.Defend)
                    memoryBonus += c.weight;
        if (memCount > 0)
            memoryBonus /= memCount;
        memoryBonus *= memoryBias;

        racialBonus = 0f;
        raceCount = 0;
        foreach (BattleBias b in self.GetRace().biases)
            if (b.action == BattleAction.Defend)
            {
                racialBonus += b.weight;
                raceCount++;
            }
        if (raceCount > 0)
            racialBonus /= raceCount;
        racialBonus *= racialBias;

        heroBonus = 0f;
        heroCount = 0;
        //Hero logic here
        if (heroCount > 0)
            heroBonus /= heroCount;
        heroBonus *= heroBias;

        defScore += memoryBonus + racialBonus + heroBonus;
        display.text += "\ndefScore: " + defScore.ToString("F2") + "[memory: " + memoryBonus.ToString("F2") + ", race: " + racialBonus.ToString("F2") + ", hero: " + heroBonus.ToString("F2") + "]"; ;

        // Run Score calculation
        memoryBonus = 0f;
        memCount = 0;
        foreach (BattleMemory mem in memories)
            foreach (BattleCondition c in mem.conditions)
                if (c.BattleAction == BattleAction.Run)
                    memoryBonus += c.weight;
        if (memCount > 0)
            memoryBonus /= memCount;
        memoryBonus *= memoryBias;

        racialBonus = 0f;
        raceCount = 0;
        foreach (BattleBias b in self.GetRace().biases)
            if (b.action == BattleAction.Run)
            {
                racialBonus += b.weight;
                raceCount++;
            }
        if (raceCount > 0)
            racialBonus /= raceCount;
        racialBonus *= racialBias;

        heroBonus = 0f;
        heroCount = 0;
        //Hero logic here
        if(heroCount > 0)
            heroBonus /= heroCount;
        heroBonus *= heroBias;

        runScore += memoryBonus + racialBonus + heroBonus;
        display.text += "\nrunScore: " + runScore.ToString("F2") + "[memory: " + memoryBonus.ToString("F2") + ", race: " + racialBonus.ToString("F2") + ", hero: " + heroBonus.ToString("F2") + "]"; ;
        
        return ChooseActionTieRandom(atkScore,sklScore,defScore,runScore);
    }
}
