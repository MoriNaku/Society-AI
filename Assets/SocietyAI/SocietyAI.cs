using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public enum ActionType
{
    ATK,
    TRAIN
}

public enum StatType
{
    Aggression,
    MemorySpan,
    Caution,
    ReproductionRate,
    ResourceCost
}

public enum BattleStats
{
    HP,
    ATK,
    DEF,
    SPD,
    INT,
    WIS
}

public enum Knowledge
{
    Threat,
    Confidence,
    LastSeenTurn
}

public sealed class WeightChange
{
    public Knowledge Know { get; }
    public StatType Stat { get; }
    public BattleStats Battle { get; }
    public string Type { get; }
    private float Value { get; }

    public WeightChange(StatType type, float value, string which)
    {
        Stat = type;
        Value = value;
        Type = which;
    }
    public WeightChange(BattleStats type, float value, string which)
    {
        Battle = type;
        Value = value;
        Type = which;
    }
    public WeightChange(Knowledge type, float value, string which)
    {
        Know = type;
        Value = value;
        Type = which;
    }

    public float GetWeight() => Value;
}

public sealed class SocietyMemory
{
    public Dictionary<Knowledge, float> tacticWeights = new();
    private float alpha = 0.1f;

    public SocietyMemory()
    {
        tacticWeights[Knowledge.Threat] = 0.0f;
        tacticWeights[Knowledge.Confidence] = 0.0f;
        tacticWeights[Knowledge.LastSeenTurn] = 0.0f;
    }

    public float GetWeight(Knowledge key)
        => tacticWeights.TryGetValue(key, out var w) ? w : 0f;

    public void AdjustWeight(Knowledge key, float weight)
    {
        tacticWeights.TryGetValue(key, out var current);
        if (key != Knowledge.LastSeenTurn)
        {
            //Debug.Log(key.ToString() + " " + current.ToString() + " + " + weight.ToString());
            float update = Mathf.Lerp(tacticWeights[key], weight, alpha);
            tacticWeights[key] = Mathf.Clamp01(update);
        }
        else
        {
            tacticWeights[key] = weight;
        }
    }
}

public sealed class SocietyAI
{
    public Dictionary<int, SocietyMemory> memories = new();

    public SocietyMemory GetOrCreateMemory(int id)
    {
        if (!memories.TryGetValue(id, out var mem))
        {
            mem = new SocietyMemory();
            memories.Add(id, mem);
        }
        return mem;
    }

    public int[] GetWeights(int id)
    {
        int[] values = new int[3];
        values[0] = (int)memories[id].tacticWeights[Knowledge.Threat];
        values[1] = (int)memories[id].tacticWeights[Knowledge.Confidence];
        values[2] = (int)memories[id].tacticWeights[Knowledge.LastSeenTurn];
        return values;
    }

    public void AdjustMemory(int id, WeightChange result, int currentTurn)
    {
        var mem = GetOrCreateMemory(id);
        mem.AdjustWeight(Knowledge.LastSeenTurn, currentTurn);
        mem.AdjustWeight(result.Know, result.GetWeight());
    }

    public List<TurnDecision> Decide(Society self, IReadOnlyList<Society> all, int turn)
    {
        List<TurnDecision> decisions = new();
        foreach (Society s in all)
        {
            if (s.Id == self.Id) continue;
            var debugText = "";

            var mem = GetOrCreateMemory(s.Id);
            float threat = mem.tacticWeights[Knowledge.Threat];
            float conf = mem.tacticWeights[Knowledge.Confidence];
            float lastSeen = mem.tacticWeights[Knowledge.LastSeenTurn];
            int age = turn - (int)lastSeen;

            debugText += "threat: " + threat + "\n";
            debugText += "conf: " + conf + "\n";
            debugText += "lastSeen: " + lastSeen + "\n";
            debugText += "age: " + age + "\n";

            int actualPatrols = 0;
            if (age >= (self.Stats[StatType.MemorySpan] * self.Battle[BattleStats.INT]) || lastSeen <= 1)
            {
                float patrolSize = 1 + (threat * conf) - self.Stats[StatType.Caution];
                debugText += "patrolSize " + patrolSize + "\n";
                actualPatrols = Mathf.Max(0,Mathf.RoundToInt(patrolSize));
            }
            debugText += "actualPatrols: " + actualPatrols;

            //Debug.Log(s.Name + "\n" + debugText);
            decisions.Add(new TurnDecision { type = ActionType.ATK, targetId = s.Id, score = actualPatrols });
        }

        return decisions;

        /*
        int bestTarget = -1;
        float bestFightScore = float.NegativeInfinity;

        foreach (Society target in other)
        {
            if (target.Id == self.Id) continue;
            
            var mem = GetOrCreateMemory(target.Id);

            float threat = mem.tacticWeights[Knowledge.Threat];
            float conf = mem.tacticWeights[Knowledge.Confidence] / 100f;
            int age = turn - (int)mem.tacticWeights[Knowledge.LastSeenTurn];
            float recencyPenalty = Mathf.Exp(-age / Mathf.Max(0.01f,self.Stats[StatType.MemorySpan]));

            float fightScore = 1f
                + self.Stats[StatType.Aggression]
                + conf * recencyPenalty
                - threat * self.Stats[StatType.Caution];

            if (fightScore > bestFightScore)
            {
                bestFightScore = fightScore;
                bestTarget = target.Id;
            }
        }

        float trainScore = 1f
            + Mathf.Max(0f, -bestFightScore);
        
        //Debug.Log(self.Name + ": " + trainScore + " > " + bestFightScore);

        if (trainScore >= bestFightScore)
        {
            //Debug.Log(self.Name + " decided to TRAIN");
            return new TurnDecision { type = ActionType.TRAIN, targetId = -1, score = trainScore };
        }
        else
        {
            //Debug.Log(self.Id + " decided to ATK " + bestTarget);
            return new TurnDecision { type = ActionType.ATK, targetId = bestTarget, score = bestFightScore };
        }
        */
    }
}

public struct TurnDecision
{
    public ActionType type;
    public int targetId;
    public float score;
}

public sealed class SimplePatrolCombat
{
    private float baseGain = 0f;
    private float closeFightBonus = 0.1f;
    public int StartCombat(Patrol a, Patrol b, int currentTurn, List<Society> societies)
    {
        var debugText = "";
        debugText += societies[a.selfId].Name + " is A\n";
        debugText += societies[b.selfId].Name + " is B\n";

        societies[a.selfId].Battle.TryGetValue(BattleStats.HP, out var aHP);
        societies[a.selfId].Battle.TryGetValue(BattleStats.ATK, out var aATK);
        societies[a.selfId].Battle.TryGetValue(BattleStats.DEF, out var aDEF);
        societies[a.selfId].Battle.TryGetValue(BattleStats.SPD, out var aSPD);

        societies[b.selfId].Battle.TryGetValue(BattleStats.HP, out var bHP);
        societies[b.selfId].Battle.TryGetValue(BattleStats.ATK, out var bATK);
        societies[b.selfId].Battle.TryGetValue(BattleStats.DEF, out var bDEF);
        societies[b.selfId].Battle.TryGetValue(BattleStats.SPD, out var bSPD);

        var aHitDMG = Mathf.Max(0f, aATK - bDEF) * a.troopComposition[0].count;
        var bHitDMG = Mathf.Max(0f, bATK - aDEF) * b.troopComposition[0].count;

        if (aSPD > bSPD)
        {
            bHP -= (int)aHitDMG;
        }
        else if (aSPD < bSPD)
        {
            aHP -= (int)bHitDMG;
        }

        var aToWin = float.PositiveInfinity;
        var bToWin = float.PositiveInfinity;
        if (aHitDMG >= 1f)
            aToWin = Mathf.Ceil(bHP / aHitDMG);
        if (bHitDMG >= 1f)
            bToWin = Mathf.Ceil(aHP / bHitDMG);

        bool aWins = aToWin <= bToWin;

        debugText += "aHitDMG: " + aHitDMG + "\n";
        debugText += "bHitDMG: " + bHitDMG + "\n";
        debugText += "aToWin: " + aToWin + "\n";
        debugText += "bToWin: " + bToWin + "\n";

        var turnDifference = Mathf.Abs(aToWin - bToWin);
        var maxTurns = Mathf.Max(aToWin, bToWin);
        float decisiveness = turnDifference / maxTurns;
        
        var obsThreatA = 0.5f;
        if (float.IsInfinity(aToWin))
        {
            if (float.IsInfinity(bToWin))
            {
                obsThreatA = 0f;
            }
            else
            {
                obsThreatA = 0.5f;
            }
        }
        else if (float.IsInfinity(bToWin))
        {
            obsThreatA = 0f;
        }
        else
        {
            obsThreatA = Mathf.Clamp01(0.5f + 0.5f * decisiveness);
        }
        float closeness = 1f - obsThreatA;
        if (float.IsNaN(decisiveness))
            decisiveness = 0f;

        debugText += "turnDifference: " + turnDifference + "\n";
        debugText += "maxTurns: " + maxTurns + "\n";
        debugText += "decisiveness: " + decisiveness + "\n";
        debugText += "obsThreatA: " + obsThreatA + "\n";
        debugText += "closeness: " + closeness;

        //float cautionGain = baseGain + (closeness * closeFightBonus);
        //float lossMultiplier = aWins ? 1f : 1.5f;
        //float threatDelta = Mathf.Max(0.01f, closeness);

        //Debug.Log(debugText);

        if (aWins)
        {
            societies[a.selfId].Stats[StatType.Aggression] += closeness;
            //societies[a.selfId].Stats[StatType.Caution] -= cautionGain * lossMultiplier;
            societies[a.selfId].AI.AdjustMemory(b.selfId, new WeightChange(Knowledge.Threat, obsThreatA, "Know"), currentTurn);
            societies[a.selfId].AI.AdjustMemory(b.selfId, new WeightChange(Knowledge.Confidence, decisiveness, "Know"), currentTurn);

            societies[b.selfId].Stats[StatType.Aggression] += -closeness;
            //societies[b.selfId].Stats[StatType.Caution] += cautionGain * lossMultiplier;
            societies[b.selfId].AI.AdjustMemory(a.selfId, new WeightChange(Knowledge.Threat, closeness, "Know"), currentTurn);
            societies[b.selfId].AI.AdjustMemory(a.selfId, new WeightChange(Knowledge.Confidence, decisiveness, "Know"), currentTurn);
        }
        else
        {
            societies[a.selfId].Stats[StatType.Aggression] += -closeness;
            //societies[a.selfId].Stats[StatType.Caution] += cautionGain * lossMultiplier;
            societies[a.selfId].AI.AdjustMemory(b.selfId, new WeightChange(Knowledge.Threat, obsThreatA, "Know"), currentTurn);
            societies[a.selfId].AI.AdjustMemory(b.selfId, new WeightChange(Knowledge.Confidence, decisiveness, "Know"), currentTurn);

            societies[b.selfId].Stats[StatType.Aggression] += closeness;
            //societies[b.selfId].Stats[StatType.Caution] -= cautionGain * lossMultiplier;
            societies[b.selfId].AI.AdjustMemory(a.selfId, new WeightChange(Knowledge.Threat, closeness, "Know"), currentTurn);
            societies[b.selfId].AI.AdjustMemory(a.selfId, new WeightChange(Knowledge.Confidence, decisiveness, "Know"), currentTurn);
        }
        societies[a.selfId].Stats[StatType.Aggression] = Mathf.Clamp01(societies[a.selfId].Stats[StatType.Aggression]);
        societies[b.selfId].Stats[StatType.Aggression] = Mathf.Clamp01(societies[b.selfId].Stats[StatType.Aggression]);

        if(aWins)
        {
            a.status = 1;
            b.status = 2;
            return 0;
        } else
        {
            a.status = 2;
            b.status = 1;
            return 1;
        }
    }
}

public sealed class SimpleCombat
{
    private float baseGain = 0f;
    private float closeFightBonus = 1f;
    public void StartCombat(Society a, Society b, int currentTurn)
    {
        //Debug.Log("A: " + a.Name + ", B: " + b.Name);
        a.Battle.TryGetValue(BattleStats.HP, out var aHP);
        a.Battle.TryGetValue(BattleStats.ATK, out var aATK);
        a.Battle.TryGetValue(BattleStats.DEF, out var aDEF);
        a.Battle.TryGetValue(BattleStats.SPD, out var aSPD);

        b.Battle.TryGetValue(BattleStats.HP, out var bHP);
        b.Battle.TryGetValue(BattleStats.ATK, out var bATK);
        b.Battle.TryGetValue(BattleStats.DEF, out var bDEF);
        b.Battle.TryGetValue(BattleStats.SPD, out var bSPD);

        var aHitDMG = Mathf.Max(1f, aATK - bDEF);
        var bHitDMG = Mathf.Max(1f, bATK - aDEF);

        if (aSPD > bSPD)
        {
            bHP -= (int)aHitDMG;
        }
        else if (aSPD < bSPD)
        {
            aHP -= (int)bHitDMG;
        }

        var aToWin = Mathf.Ceil(bHP / aHitDMG);
        var bToWin = Mathf.Ceil(aHP / bHitDMG);

        bool aWins = aToWin <= bToWin;

        float decisiveness = Mathf.Abs(aToWin - bToWin) / Mathf.Max(aToWin, bToWin);
        float closeness = 1f - decisiveness;
        float cautionGain = baseGain + (closeness * closeFightBonus);
        float lossMultiplier = aWins ? 1f : 1.5f;
        float threatDelta = Mathf.Max(0.01f, closeness);

        var turnDifference = Mathf.Abs(aToWin - bToWin);
        var maxTurns = Mathf.Max(aToWin, bToWin);
        var weightValue = (maxTurns == 0f) ? 0f : turnDifference / maxTurns;

        if(aWins)
        {
            a.Stats[StatType.Aggression] += 0.1f;
            a.Stats[StatType.Caution] -= cautionGain * lossMultiplier;
            a.AI.AdjustMemory(b.Id, new WeightChange(Knowledge.Threat, -threatDelta, "Know"), currentTurn);
            a.AI.AdjustMemory(b.Id, new WeightChange(Knowledge.Confidence, weightValue, "Know"), currentTurn);

            b.Stats[StatType.Aggression] += -0.1f;
            b.Stats[StatType.Caution] += cautionGain * lossMultiplier;
            b.AI.AdjustMemory(a.Id, new WeightChange(Knowledge.Threat, threatDelta, "Know"), currentTurn);
            b.AI.AdjustMemory(a.Id, new WeightChange(Knowledge.Confidence, weightValue, "Know"), currentTurn);
        }
        else
        {
            a.Stats[StatType.Aggression] += -0.1f;
            b.Stats[StatType.Caution] += cautionGain * lossMultiplier;
            a.AI.AdjustMemory(b.Id, new WeightChange(Knowledge.Threat, threatDelta, "Know"), currentTurn);
            a.AI.AdjustMemory(b.Id, new WeightChange(Knowledge.Confidence, weightValue, "Know"), currentTurn);

            b.Stats[StatType.Aggression] += 0.1f;
            b.Stats[StatType.Caution] -= cautionGain * lossMultiplier;
            b.AI.AdjustMemory(a.Id, new WeightChange(Knowledge.Threat, -threatDelta, "Know"), currentTurn);
            b.AI.AdjustMemory(a.Id, new WeightChange(Knowledge.Confidence, weightValue, "Know"), currentTurn);
        }
        a.Stats[StatType.Aggression] = Mathf.Clamp01(a.Stats[StatType.Aggression]);
        a.Stats[StatType.Caution] = Mathf.Clamp01(a.Stats[StatType.Caution]);
        a.AI.memories[b.Id].tacticWeights[Knowledge.Threat] = Mathf.Clamp01(a.AI.memories[b.Id].tacticWeights[Knowledge.Threat]);
        b.Stats[StatType.Aggression] = Mathf.Clamp01(b.Stats[StatType.Aggression]);
        b.Stats[StatType.Caution] = Mathf.Clamp01(b.Stats[StatType.Caution]);
        b.AI.memories[a.Id].tacticWeights[Knowledge.Threat] = Mathf.Clamp01(b.AI.memories[a.Id].tacticWeights[Knowledge.Threat]);
    }
}

public sealed class SimpleTraining
{
    public float HP_weight = 100f;
    public float ATK_weight = 10f;
    public float DEF_weight = 10f;
    public float SPD_weight = 1f;

    public void StartTraining(Society a)
    {
        var result = Mathf.Round(UnityEngine.Random.Range(0.01f, 1f) * 100) / 100f;
        var pick = UnityEngine.Random.Range(0, 4);

        var stat = 0f;

        switch (pick)
        {
            case 0:
                stat = a.Battle.GetValueOrDefault(BattleStats.HP);
                a.Battle[BattleStats.HP] = stat + (result * HP_weight);
                break;
            case 1:
                stat = a.Battle.GetValueOrDefault(BattleStats.ATK);
                a.Battle[BattleStats.ATK] = stat + (result * ATK_weight);
                break;
            case 2:
                stat = a.Battle.GetValueOrDefault(BattleStats.DEF);
                a.Battle[BattleStats.DEF] = stat + (result * DEF_weight);
                break;
            case 3:
                stat = a.Battle.GetValueOrDefault(BattleStats.SPD);
                a.Battle[BattleStats.SPD] = stat + (result * SPD_weight);
                break;
        }

        a.Stats[StatType.Aggression] += 0.1f;
    }
}

public sealed class Society
{
    public int Id;
    public readonly string Name;
    public Dictionary<StatType, float> Stats = new();
    public Dictionary<BattleStats, float> Battle = new();

    public SocietyAI AI = new();
    public int populationCount = 0;
    public List<Patrol> currentPatrols = new();

    public Society(int id, string name)
    {
        Id = id;
        Name = name;
        InitializeStats(null);
    }
    public Society(int id, string name, RaceDefinition race)
    {
        Id = id;
        Name = name;
        InitializeStats(race);
    }

    public float[] GetFullStats()
    {
        float[] values = new float[12];

        values[0] = Stats[StatType.Aggression];
        values[1] = Stats[StatType.MemorySpan];
        values[2] = Stats[StatType.Caution];
        values[3] = Stats[StatType.ReproductionRate];
        values[4] = Stats[StatType.ResourceCost];

        values[5] = Battle[BattleStats.HP];
        values[6] = Battle[BattleStats.ATK];
        values[7] = Battle[BattleStats.DEF];
        values[8] = Battle[BattleStats.INT];
        values[9] = Battle[BattleStats.WIS];
        values[10] = Battle[BattleStats.SPD];

        values[11] = populationCount;
        Debug.Log(Id);
        switch(Id)
        {
            case 0:
                values[12] = AI.GetOrCreateMemory(1).tacticWeights[Knowledge.Threat];
                values[13] = AI.GetOrCreateMemory(2).tacticWeights[Knowledge.Threat];
                break;
            case 1:
                values[12] = AI.GetOrCreateMemory(0).tacticWeights[Knowledge.Threat];
                values[13] = AI.GetOrCreateMemory(2).tacticWeights[Knowledge.Threat];
                break;
            case 2:
                values[12] = AI.GetOrCreateMemory(0).tacticWeights[Knowledge.Threat];
                values[13] = AI.GetOrCreateMemory(1).tacticWeights[Knowledge.Threat];
                break;
            default:
                values[12] = 0f;
                values[13] = 0f;
                break;
        }
        
        return values;
    }

    private void InitializeStats(RaceDefinition bs)
    {
        foreach (StatType type in (StatType[])Enum.GetValues(typeof(StatType)))
        {
            Stats[type] = 0f;
        }
        if (bs != null)
        {
            Battle[BattleStats.HP] = bs.HP;
            Battle[BattleStats.ATK] = bs.ATK;
            Battle[BattleStats.DEF] = bs.DEF;
            Battle[BattleStats.INT] = bs.INT;
            Battle[BattleStats.WIS] = bs.WIS;
            Battle[BattleStats.SPD] = bs.SPD;
        }
        else
        {
            Battle[BattleStats.HP] = 1f;
            Battle[BattleStats.ATK] = 1f;
            Battle[BattleStats.DEF] = 1f;
            Battle[BattleStats.INT] = 1f;
            Battle[BattleStats.WIS] = 1f;
            Battle[BattleStats.SPD] = 1f;
        }
    }

    public void AdjustWeights(int id, WeightChange res, int turn)
    {
        switch (res.Type)
        {
            case "Know":
                AI.AdjustMemory(id, res, turn);
                break;
            case "Stat":
                Stats[res.Stat] += res.GetWeight();
                Stats[res.Stat] = Mathf.Clamp(Stats[res.Stat], -1.0f, 1.0f);
                break;
            case "Battle":
                Battle[res.Battle] += res.GetWeight();
                Battle[res.Battle] = Mathf.Clamp(Battle[res.Battle], -1.0f, 1.0f);
                break;
        }
    }
}

public class Patrol
{
    public int targetId;
    public int selfId;

    public List<TroopStack> troopComposition = new();
    public int createdTurn;

    public int status = 0;

    //PatrolIntent intent;
    public int GetTotal()
    {
        int total = 0;
        foreach (TroopStack ts in troopComposition)
        {
            total += ts.count;
        }
        return total;
    }
}

public class TroopStack
{
    public int raceId;
    public int count;
}