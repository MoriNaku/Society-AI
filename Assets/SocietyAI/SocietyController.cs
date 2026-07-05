using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using static SocietyAI;

public sealed class SocietyController : MonoBehaviour
{
    [SerializeField] private int maxTurns = 100;
    [SerializeField] private float secondsPerTurn = 0.25f;

    [SerializeField] private List<Society> societies = new();
    [SerializeField] private List<RaceDefinition> raceDefinitions = new();
    [SerializeField] private List<Patrol> patrolList = new();

    [SerializeField] private List<float[]> snapshotsA = new();
    [SerializeField] private List<float[]> snapshotsB = new();
    [SerializeField] private List<float[]> snapshotsC = new();

    private SimpleCombat combat = new();
    private SimpleTraining training = new();
    private SimplePatrolCombat patrolling = new();

    private int currentTurn;
    private Coroutine runningCoroutine;

    public TMP_Text turnDisplay;
    public TMP_Text societyAName;
    public TMP_Text societyADisplay;
    public TMP_Text societyBName;
    public TMP_Text societyBDisplay;
    public TMP_Text societyCName;
    public TMP_Text societyCDisplay;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //Setup Societies
        currentTurn = 0;
        Initialize();
    }

    public void RunSim()
    {
        if (runningCoroutine == null)
            runningCoroutine = StartCoroutine(RunLoop());
    }

    public void StepTurn()
    {
        ExecuteTurn();
    }

    private void Initialize()
    {
        Society a = new Society(0, "Humans", raceDefinitions[0]);
        Society b = new Society(1, "Goblins", raceDefinitions[1]);
        Society c = new Society(2, "Dragons", raceDefinitions[2]);

        a.populationCount = 100;
        b.populationCount = 200;
        c.populationCount = 25;

        societies.Add(a);
        societies.Add(b);
        societies.Add(c);

        a.AI.GetOrCreateMemory(2);
        a.AI.GetOrCreateMemory(3);
        b.AI.GetOrCreateMemory(1);
        b.AI.GetOrCreateMemory(3);
        c.AI.GetOrCreateMemory(1);
        c.AI.GetOrCreateMemory(2);

        //snapshotsA.Add(a.GetFullStats());
        //snapshotsB.Add(b.GetFullStats());
        //snapshotsC.Add(c.GetFullStats());

        societyAName.text = a.Name + " (ID: " + a.Id + ")";
        societyBName.text = b.Name + " (ID: " + b.Id + ")";
        societyCName.text = c.Name + " (ID: " + c.Id + ")";

        societyADisplay.text = "Initialized\n\n";
        societyBDisplay.text = "Initialized\n\n";
        societyCDisplay.text = "Initialized\n\n";

        updateDisplay();
    }

    private void updateDisplay()
    {
        turnDisplay.text = "Turn " + currentTurn.ToString();
        /*
        float[] aValues = snapshotsA[currentTurn];
        float[] bValues = snapshotsB[currentTurn];
        float[] cValues = snapshotsC[currentTurn];

        var disAText =
            "PopulationCount:\t" + aValues[11].ToString("F0") + "\n"
            + "Aggression:\t\t" + aValues[0].ToString("F2") + "\n"
            + "MemorySpan:\t" + aValues[1].ToString("F2") + "\n"
            + "Caution:\t\t" + aValues[2].ToString("F2") + "\n"
            + "PopulationRate:\t" + aValues[3].ToString("F2") + "\n"
            + "ResourceCost:\t" + aValues[4].ToString("F2") + "\n\n"
            + "Goblin Threat\t" + aValues[12].ToString("F2") + "\n"
            + "Dragon Threat\t" + aValues[13].ToString("F2");
        var disBText =
            "PopulationCount:\t" + bValues[11].ToString("F0") + "\n"
            + "Aggression:\t\t" + bValues[0].ToString("F2") + "\n"
            + "MemorySpan:\t" + bValues[1].ToString("F2") + "\n"
            + "Caution:\t\t" + aValues[2].ToString("F2") + "\n"
            + "PopulationRate:\t" + bValues[3].ToString("F2") + "\n"
            + "ResourceCost:\t" + bValues[4].ToString("F2") + "\n\n"
            + "Human Threat\t" + aValues[12].ToString("F2") + "\n"
            + "Dragon Threat\t" + aValues[13].ToString("F2");
        var disCText =
            "PopulationCount:\t" + cValues[11].ToString("F0") + "\n"
            + "Aggression:\t\t" + cValues[0].ToString("F2") + "\n"
            + "MemorySpan:\t" + cValues[1].ToString("F2") + "\n"
            + "Caution:\t\t" + aValues[2].ToString("F2") + "\n"
            + "PopulationRate:\t" + cValues[3].ToString("F2") + "\n"
            + "ResourceCost:\t" + cValues[4].ToString("F2") + "\n\n"
            + "Human Threat\t" + aValues[12].ToString("F2") + "\n"
            + "Goblin Threat\t" + aValues[13].ToString("F2");
        
        societyADisplay.text += disAText;
        societyBDisplay.text += disBText;
        societyCDisplay.text += disCText;
        */
    }

    public void ExecuteTurn()
    {
        currentTurn++;
        societyADisplay.text = "";
        societyBDisplay.text = "";
        societyCDisplay.text = "";

        patrolList.Clear();
        Debug.Log("Turn Number: " + currentTurn);

        for(int i = 0; i < societies.Count; i++)
        {
            var decision = societies[i].AI.Decide(societies[i], societies, currentTurn);
            var committedMax = Mathf.Round(societies[i].populationCount * societies[i].Stats[StatType.Aggression]);
            Dictionary<int, float> threatIndexes = new();
            var threatTotal = 0f;
            int totalPatrols = 0;

            for (int j = 0; j < societies.Count; j++)
            {
                if (societies[i].Id == societies[j].Id) continue;
                var threat = societies[i].AI.GetOrCreateMemory(societies[j].Id).tacticWeights[Knowledge.Threat];
                threatTotal += threat;
                //Debug.Log(societies[j].Name + ": " + threat);
                threatIndexes.Add(societies[j].Id, threat);
            }
            var threatDebug = "";
            threatDebug += societies[i].Name + " Total Threat Scores: " + threatTotal + "\n";
            foreach (float threat in threatIndexes.Values)
            {
                threatDebug += "ListID: " + ", Value: " + threat + "\n";
            }
            Debug.Log(threatDebug);

            foreach(TurnDecision td in decision)
            {
                totalPatrols += (int)td.score;
            }

            committedMax = Mathf.Max(totalPatrols, committedMax);
            var debug = "Society: " + societies[i].Name + "\nTotal Population Committed: " + committedMax + "\nTotal Patrols Sent Out: " + totalPatrols;

            //Debug.Log(debug);
            foreach (TurnDecision td in decision)
            {
                float threatValue = 0f;
                if (threatTotal > 0)
                {
                    threatValue = threatIndexes[td.targetId] / threatTotal;
                }
                else
                {
                    threatValue = threatIndexes[td.targetId] / 1f;
                }
                for (int j = 0; j < td.score; j++)
                {
                    Patrol p = new Patrol();
                    p.createdTurn = currentTurn;
                    p.targetId = td.targetId;
                    p.selfId = societies[i].Id;

                    var patrolLog = "";
                    patrolLog += "\nthreatIndexes[td.targetId]: " + threatIndexes[td.targetId];
                    patrolLog += "\nthreatTotal: " + threatTotal;
                    patrolLog += "\nthreatValue: " + threatValue;
                    patrolLog += "\ntd.score: " + td.score;

                    float troopCount = Mathf.Max(1f,(committedMax * threatValue) / td.score);
                    //patrolLog += "\n" + societies[i].Name + ": " + (int)troopCount + ", " + threatValue + ", " + td.score;

                    //Temporary single race TroopComposition
                    p.troopComposition.Add(new TroopStack { raceId = societies[i].Id, count = Mathf.RoundToInt(troopCount) });

                    patrolList.Add(p);
                    patrolLog += "\nCommitted Prior: " + committedMax;
                    patrolLog += "\nThis Troop: " + troopCount;
                    committedMax -= troopCount;
                    patrolLog += "\nCommitted After: " + committedMax;
                    //Debug.Log(patrolLog);
                }
            }
            //Debug.Log("After " + societies[i].Name + ", MasterPatrolList: " + patrolList.Count);
        }

        for (int i = 0; i < patrolList.Count; i++)
        {
            if (patrolList[i].status != 0) continue;
            for (int j = i + 1; j < patrolList.Count; j++)
            {
                if (patrolList[j].status != 0) continue;
                if (patrolList[i].targetId == patrolList[j].selfId && patrolList[i].selfId == patrolList[j].targetId)
                {
                    patrolling.StartCombat(patrolList[i], patrolList[j], currentTurn, societies);
                    //Debug.Log("Patrol #" + i + " Result: " + patrolList[i].status + "\nPatrol #" + j + " Result: " + patrolList[j].status);
                    break;
                }
            }
            if (patrolList[i].status == 0)
                patrolList[i].status = 3;
        }

        //Debug Check
        var debugText = "";
        for (int i = 0; i < patrolList.Count; i++)
        {
            debugText += societies[patrolList[i].selfId].Name + " Patrol (" + patrolList[i].GetTotal() + ") VS " + societies[patrolList[i].targetId].Name + " Result: ";
            switch(patrolList[i].status)
            {
                case 0:
                    debugText += "Not Initialized/Error\n";
                    break;
                case 1:
                    debugText += "Victory\n";
                    break;
                case 2:
                    debugText += "Loss\n";
                    break;
                case 3:
                    debugText += "No Encounter\n";
                    break;
            }
        }
        Debug.Log(debugText);
        /*
        for (int i = 0; i < societies.Count; i++)
        {
            List<Patrol> patrols = new();

            var decision = societies[i].AI.Decide(societies[i], societies, currentTurn);
            var committedMax = Mathf.Round(societies[i].populationCount * societies[i].Stats[StatType.Aggression]);
            Dictionary<int, float> threatIndexes = new();
            float threatTotal = 0f;
            int totalPatrols = 0;

            foreach(Society s in societies)
            {
                if (s.Id == societies[i].Id) continue;
                threatTotal += societies[i].AI.GetOrCreateMemory(s.Id).tacticWeights[Knowledge.Threat];
                threatIndexes.Add(s.Id, societies[i].AI.GetOrCreateMemory(s.Id).tacticWeights[Knowledge.Threat]);
            }
            //Debug.Log(self.Name + ": " + threatTotal);

            foreach (TurnDecision td in decision)
                totalPatrols += (int)td.score;

            committedMax = Mathf.Max(totalPatrols, committedMax);
            var debug = "Society: " + societies[i].Name + "\nTotal Population Committed: " + committedMax + "\nTotal Patrols Sent Out: " + totalPatrols;

            foreach (TurnDecision td in decision)
            {
                float threatValue = 0f;
                if (threatTotal >= 1)
                {
                    threatValue = threatIndexes[td.targetId] / threatTotal;
                }
                else
                {
                    threatValue = threatIndexes[(int)td.targetId] / 1f;
                }
                for (int j = 0; j < td.score; j++)
                {
                    Patrol p = new Patrol();
                    p.createdTurn = currentTurn;
                    p.targetId = td.targetId;
                    p.selfId = societies[i].Id;

                    float troopCount = (committedMax * threatValue) / td.score;
                    Debug.Log(societies[i].Name + ": " + (int)Mathf.Max(1, Mathf.RoundToInt(troopCount)) + ", " + threatValue + ", " + td.score);
                    p.troopComposition.Add(new TroopStack { raceId = societies[i].Id, count = (int)Mathf.Max(1, Mathf.RoundToInt(troopCount)) });

                    patrols.Add(p);
                }
                foreach(Patrol x in patrols)
                    if (x.targetId == td.targetId)
                    foreach(TroopStack ts in x.troopComposition)
                        committedMax -= ts.count;
            }

            //Debug text grabbing
            foreach(Patrol px in patrols)
            {
                if (px.selfId == societies[i].Id)
                {
                    debug += "\n[targetId: " + px.targetId + ", troopComp: {";
                    for(int k=0; k<px.troopComposition.Count; k++)
                    {
                        debug += "[raceId: " + px.troopComposition[k].raceId + ", count: " + px.troopComposition[k].count + "]";
                        if(k < px.troopComposition.Count)
                        {
                            debug += ", ";
                        }
                    }
                    debug += "}]";
                }
            }

            Debug.Log(debug);

            societies[i].currentPatrols = patrols;

            switch (i)
            {
                case 0:
                    societyADisplay.text += societies[i].Name + ": " + societies[i].currentPatrols.Count + " Patrols\nTotal Committed: " + committedMax;
                    break;
                case 1:
                    societyBDisplay.text += societies[i].Name + ": " + societies[i].currentPatrols.Count + " Patrols\nTotal Committed: " + committedMax;
                    break;
                case 2:
                    societyCDisplay.text += societies[i].Name + ": " + societies[i].currentPatrols.Count + " Patrols\nTotal Committed: " + committedMax;
                    break;
                default:
                    break;
            }
            
            
            if (decision.type == ActionType.TRAIN)
            {
                training.StartTraining(self);
            }
            else if (decision.type == ActionType.ATK)
            {
                var target = FindSocietyById(decision.targetId);
                if (target == null) continue;

                combat.StartCombat(self, target, currentTurn);
            }
            
        }

        foreach (Society s in societies)
        {
            List<Patrol> mine = s.currentPatrols;
            Dictionary<int, List<Patrol>> targeted = new();
            var displayText = "";
            var patrolCount = 0;
            
            foreach(Patrol x in mine)
            {
                var result = -1;
                int targetId = x.targetId;
                if (x.status == 0)
                {
                    //Find Patrol Opponent
                    Patrol targetPatrol = null;
                    if (societies[targetId].currentPatrols.Count > 0)
                    {
                        if (societies[targetId].currentPatrols[0].status == 0)
                            targetPatrol = societies[targetId].currentPatrols[0];
                    }

                    displayText += "\nPatrol #" + patrolCount + " => " + societies[targetId].Name + ", Size: " + x.troopComposition[0].count;

                    if (targetPatrol != null)
                        result = patrolling.StartCombat(x, targetPatrol, currentTurn, societies);

                } else
                {
                    result = x.status;
                }
                //Debug.Log("Result:" + result + ", Patrol Status: " + x.status);
                switch (result)
                {
                    case 0:
                        displayText += "\nVICTORY";
                        societies[targetId].currentPatrols[0].status = 1;
                        societies[x.selfId].currentPatrols[patrolCount].status = 2;
                        break;
                    case 1:
                        displayText += "\nDEFEAT";
                        societies[x.selfId].currentPatrols[patrolCount].status = 1;
                        break;
                    default:
                        displayText += "\nNO RESIST";
                        societies[x.selfId].currentPatrols[patrolCount].status = 2;
                        break;
                }
                patrolCount++;
            }

            switch (s.Id)
            {
                case 0:
                    societyADisplay.text += displayText;
                    break;
                case 1:
                    societyBDisplay.text += displayText;
                    break;
                case 2:
                    societyCDisplay.text += displayText;
                    break;
                default:
                    break;
            }

        }
        foreach(Society s  in societies)
        {
            s.currentPatrols.Clear();
        }
        */

        //snapshotsA.Add(societies[0].GetFullStats());
        //snapshotsB.Add(societies[1].GetFullStats());
        //snapshotsC.Add(societies[2].GetFullStats());

        //updateDisplay();
    }

    private IEnumerator RunLoop()
    {
        while (true)
        {
            if(currentTurn >= maxTurns)
            {
                StopAllCoroutines(); 
            }
            ExecuteTurn();
            yield return new WaitForSeconds(secondsPerTurn);
        }
    }

    public void ShowOne()
    {
        Dictionary<int, SocietyMemory>.KeyCollection k = societies[0].AI.memories.Keys;
        foreach (int i in k)
            Debug.Log("Key = " + i + "\n"
                + "Key = " + i);
        Dictionary<int, SocietyMemory>.ValueCollection a = societies[0].AI.memories.Values;
        foreach(SocietyMemory aS in a)
            Debug.Log("Value = " + aS.tacticWeights[Knowledge.Threat] + "\n"
                + "Value = " + aS.tacticWeights[Knowledge.Confidence] + "\n"
                + "Value = " + aS.tacticWeights[Knowledge.LastSeenTurn]);
    }
    public void ShowTwo()
    {
        Dictionary<int, SocietyMemory>.KeyCollection k = societies[1].AI.memories.Keys;
        foreach (int i in k)
            Debug.Log("Key = " + i + "\n"
                + "Key = " + i);
        Dictionary<int, SocietyMemory>.ValueCollection a = societies[1].AI.memories.Values;
        foreach (SocietyMemory aS in a)
            Debug.Log("Value = " + aS.tacticWeights[Knowledge.Threat] + "\n"
                + "Value = " + aS.tacticWeights[Knowledge.Confidence] + "\n"
                + "Value = " + aS.tacticWeights[Knowledge.LastSeenTurn]);
    }
    public void ShowThree()
    {
        Dictionary<int, SocietyMemory>.KeyCollection k = societies[2].AI.memories.Keys;
        foreach (int i in k)
            Debug.Log("Key = " + i + "\n"
                + "Key = " + i);
        Dictionary<int, SocietyMemory>.ValueCollection a = societies[2].AI.memories.Values;
        foreach (SocietyMemory aS in a)
            Debug.Log("Value = " + aS.tacticWeights[Knowledge.Threat] + "\n"
                + "Value = " + aS.tacticWeights[Knowledge.Confidence] + "\n"
                + "Value = " + aS.tacticWeights[Knowledge.LastSeenTurn]);
    }
    private Society FindSocietyById(int id)
    {
        for (int i = 0; i < societies.Count; i++)
            if (societies[i].Id == id) return societies[i];
        return null;
    }
}
