using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;

public class StupidSlotsScript : MonoBehaviour {

    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMSelectable submit;
    public KMSelectable[] arrows;
    public GameObject[] displays;
    public Material[] colors;
    public Font[] fonts;
    public Material[] fontMats;
    public Font comicSans;
    public Material comicSansMat;

    private int[][] orders = { new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 } };
    private Material[][] slotColors = new Material[3][] { new Material[10], new Material[10], new Material[10] };
    private Material[][] slotTextColors = new Material[3][] { new Material[10], new Material[10], new Material[10] };
    private Font[][] slotFonts = new Font[3][] { new Font[10], new Font[10], new Font[10] };
    private Material[][] slotFontMats = new Material[3][] { new Material[10], new Material[10], new Material[10] };
    private Vector3[][][] slotFontPosisions = new Vector3[3][][] { new Vector3[10][], new Vector3[10][], new Vector3[10][] };

    private Vector3[][] fontPositions = new Vector3[][] //Stores the position and scale info for each font to be centered on the display.
    {
        new Vector3[] { new Vector3(0,-1,4), Vector3.one }, //Crazysk8 
        new Vector3[] { new Vector3(-0.2f,0,4), Vector3.one }, //Fantastic Party
        new Vector3[] { new Vector3(0,-0.5f,4), 0.9f*Vector3.one }, //Klotz
        new Vector3[] { new Vector3(0,0,4), 0.8f*Vector3.one }, //Lobster
        new Vector3[] { new Vector3(0,1.5f,4), 0.8f*Vector3.one }, //Nadine
        new Vector3[] { new Vector3(0,-1.5f,4), Vector3.one }, //Pacifico
        new Vector3[] { new Vector3(0,0,4), 0.5f*Vector3.one }, //Rock Salt
        new Vector3[] { new Vector3(0.25f,1,4), new Vector3(1, 0.8f ,1) }, //Sabitype
        new Vector3[] { new Vector3(0,-0.5f,4), Vector3.one }, //Wacky Sushi
        new Vector3[] { new Vector3(0.5f,0.5f,4), new Vector3(1, 0.9f, 1) } //Watermelon
    };
    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;
    bool isAnimating = false;
    int currentValue;
    int[] valuesUpper, valuesLower = new int[3];
    int[] allValues = new int[6];
    int[] relevantDigits;
    List<int> generatedPath = new List<int>();
    List<int> allAnswers = new List<int>();

    bool interacted;

    void Awake ()
    {
        moduleId = moduleIdCounter++;     
        foreach (KMSelectable arrow in arrows)
            arrow.OnInteract += delegate () { ArrowPress(Array.IndexOf(arrows,arrow)); return false; };
        submit.OnInteract += delegate () { if (!isAnimating) StartCoroutine(Submit()); return false; };

    }

    void Start ()
    {
        GetDisplayInfo();
        GetArrows();
        GetStartingNum();
        DoLogging();
    }

    void ArrowPress(int pos)
    {
        if (allValues[pos] != 0)
            interacted = true;
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, arrows[pos].transform);
        arrows[pos].AddInteractionPunch(0.2f);
        if (moduleSolved || isAnimating) return;
        currentValue = Mod((currentValue + allValues[pos]), 1000);
        SetDisplays();
    }

    void GetArrows()
    {
        do
        {
            allAnswers.Clear();
            List<int> relevantValues = new List<int>
            {
                UnityEngine.Random.Range(1, 16) * 2,
                UnityEngine.Random.Range(1, 16) * 2 - 1,
                UnityEngine.Random.Range(1, 16) * -2,
                UnityEngine.Random.Range(1, 16) * -2 + 1,
            };
            relevantValues.Shuffle();
            valuesUpper = new int[] { 0, relevantValues[0], relevantValues[1] };
            valuesLower = new int[] { 0, relevantValues[2], relevantValues[3] };
            valuesUpper.Shuffle();
            valuesLower.Shuffle();
            allValues = valuesUpper.Concat(valuesLower).ToArray();
            relevantDigits = new int[]
            {
                valuesUpper.First(x => x != 0),
                valuesLower.First(x => x != 0),
                valuesUpper.Last(x => x != 0),
                valuesLower.Last(x => x != 0)
            };
            for (int i = 0; i < 1000; i++)
            {
                if (CheckValidities(i))
                    allAnswers.Add(i);
            }
        } while (allAnswers.Count < 50);
    }


    void GetDisplayInfo()
    {
        for (int slotIndex = 0; slotIndex < 3; slotIndex++)
        {
            orders[slotIndex].Shuffle();
            for (int i = 0; i < 10; i++)
            {
                slotFonts[slotIndex][i] = fonts[orders[slotIndex][i]];
                slotFontMats[slotIndex][i] = fontMats[orders[slotIndex][i]];
                slotFontPosisions[slotIndex][i] = fontPositions[orders[slotIndex][i]];
            }
            do
            {
                int[] tempOrder = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
                tempOrder.Shuffle();
                for (int i = 0; i < 10; i++)
                    slotColors[slotIndex][i] = colors[tempOrder[i]];
                tempOrder.Shuffle();
                for (int i = 0; i < 10; i++)
                    slotTextColors[slotIndex][i] = colors[tempOrder[i]];
            } while (slotColors[slotIndex].Any(x => slotTextColors[slotIndex][Array.IndexOf(slotColors[slotIndex], x)] == x));
        }
    }
    void SetDisplays()
    {
        for (int slotIndex = 0; slotIndex < 3; slotIndex++)
        {
            TextMesh targetMesh = displays[slotIndex].GetComponentInChildren<TextMesh>();
            int digit = new int[] { currentValue / 100, currentValue / 10 % 10, currentValue % 10 }[slotIndex]; 
            displays[slotIndex].GetComponent<MeshRenderer>().material = slotColors[slotIndex][digit];
            targetMesh.text = digit.ToString();
            targetMesh.color = slotTextColors[slotIndex][digit].color;
            targetMesh.font = slotFonts[slotIndex][digit];
            targetMesh.GetComponent<MeshRenderer>().material = slotFontMats[slotIndex][digit];
            targetMesh.transform.localPosition = slotFontPosisions[slotIndex][digit][0];
            targetMesh.transform.localScale    = slotFontPosisions[slotIndex][digit][1];
        }
    }
    void GetStartingNum() //Generates a random number which is valid, and then backtracks from there to guarantee a solution.
    {
        currentValue = allAnswers.PickRandom();
        for (int i = 0; i < UnityEngine.Random.Range(10,21); i++)
        {
            int num = relevantDigits.PickRandom();
            generatedPath.Add(num);
            currentValue = Mod(currentValue - num, 1000);
        }
        generatedPath.Reverse();
        SetDisplays();
    }
    void LogValidities()
    {
        int[] operationResults = new int[] { Mod(relevantDigits[1], 5), Mod(relevantDigits[3], 5) };
        for (int i = 0; i < 2; i++)
        {
            switch (Mod(relevantDigits[2*i], 5))
            {
                case 0: Debug.LogFormat("[Stupid Slots #{0}] The number, modulo 5, must equal {1} or {2}.", moduleId, operationResults[i], operationResults[i] + 5); break;
                case 1: Debug.LogFormat("[Stupid Slots #{0}] The digital root must be {1} or {2}.", moduleId, operationResults[i], operationResults[i] + 5); break;
                case 2: Debug.LogFormat("[Stupid Slots #{0}] The number must be divisible by {1} or {2}.", moduleId, operationResults[i], operationResults[i] + 5); break;
                case 3: Debug.LogFormat("[Stupid Slots #{0}] The number's first digit must be {1} or {2}.", moduleId, operationResults[i], operationResults[i] + 5); break;
                case 4: Debug.LogFormat("[Stupid Slots #{0}] The number's second digit must be {1} or {2}.", moduleId, operationResults[i], operationResults[i] + 5); break;
            }
        }
        Debug.LogFormat("[Stupid Slots #{0}] In addition, the third digit of the number must be one of the following: {1}", moduleId,
            relevantDigits.Select(x => Mod(x, 10)).Distinct().Join(", "));
    }

    void DoLogging()
    {
        Debug.LogFormat("[Stupid Slots #{0}] The starting value is {1}.", moduleId, currentValue.ToString());
        Debug.LogFormat("[Stupid Slots #{0}] The arrow values in reading order are: {1} // {2}", moduleId, valuesUpper.Join(", "), valuesLower.Join(", "));
        LogValidities();
    }

    bool CheckValidities(int number)
    {
        int[] operationResults = new int[] { Mod(relevantDigits[1], 5), Mod(relevantDigits[3], 5) };
        bool[] validities = new bool[3];
        for (int i = 0; i < 2; i++)
        {
            switch (Mod(relevantDigits[2*i], 5))
            {
                case 0: if (number % 5 == operationResults[i]) validities[i] = true; break; //mod 5 will never be greater than 5, so we don't need to account for the +5 case. 
                case 1: if ((number - 1) % 9 + 1 == operationResults[i] || (number - 1) % 9 + 1 == operationResults[i] + 5) validities[i] = true; break;
                case 2:
                    if (operationResults[i] == 0)
                    {
                        if (number % 5 == 0)
                            validities[i] = true;
                    }
                    else if (number % operationResults[i] == 0 || number % (operationResults[i] + 5) == 0) validities[i] = true;
                    break;
                case 3: if ((number / 100) == operationResults[i] || (number / 100) == operationResults[i] + 5) validities[i] = true; break;
                case 4: if ((number % 100 / 10) == operationResults[i] || number % 100 / 10 == operationResults[i] + 5) validities[i] = true; break;
            }
        }
        if (relevantDigits.Select(x => Mod(x, 10)).Contains(number % 10))
            validities[2] = true;

        return validities.All(x => x);
    }
    int Mod(int input, int modulus)
    {
        return (input % modulus + modulus) % modulus;
    }

    IEnumerator Submit()
    {
        Audio.PlaySoundAtTransform("boing", submit.transform);
        submit.AddInteractionPunch(10);
        StartCoroutine(Spin());
        if (moduleSolved) yield break;
        bool willSolve;
        Audio.PlaySoundAtTransform("bogosort", transform);
        
        Debug.LogFormat("[Stupid Slots #{0}] Submitted value {1}.", moduleId, currentValue);
        willSolve = CheckValidities(currentValue);
        if (willSolve) moduleSolved = true;

        isAnimating = true;
        for (int i = 0; i < 22; i++)
        {
            currentValue = UnityEngine.Random.Range(0, 1000);
            SetDisplays();
            yield return new WaitForSecondsRealtime(0.25f);
        }
        isAnimating = false;
        if (willSolve)
        {
            Debug.LogFormat("[Stupid Slots #{0}] That was correct. Module solved.", moduleId);
            for (int i = 0; i < 3; i++)
            {
                displays[i].GetComponent<MeshRenderer>().material = colors[3]; //set displays to lime green
                TextMesh chosenMesh = displays[i].GetComponentInChildren<TextMesh>();
                chosenMesh.text = ":)";
                chosenMesh.font = comicSans;
                chosenMesh.GetComponent<MeshRenderer>().material = comicSansMat;
                chosenMesh.color = colors[8].color; //sets text color to white
                chosenMesh.transform.localPosition = new Vector3(0, -1, 4);
                chosenMesh.transform.localScale = Vector3.one * 0.85f;
            }
            GetComponent<KMBombModule>().HandlePass();
        }
        else
        {
            Debug.LogFormat("[Stupid Slots #{0}] That was incorrect. Strike incurred.", moduleId);
            GetComponent<KMBombModule>().HandleStrike();
            generatedPath.Clear();
            GetStartingNum();
            interacted = false;
        }
        yield return null;
    }
    IEnumerator Spin()
    {
        for (int i = 0; i < 45; i++)
        {
            submit.transform.localEulerAngles += new Vector3(0, 8, 0);
            yield return null;
        }
    }

    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use !{0} press 1/2/3/4/5/6 or !{0} press TL TM TR BL BM BR to press the arrow buttons in that positions. Use !{0} submit to press the submit button. Use !{0} cycle to press each arrow button once slowly.";
    #pragma warning restore 414

    IEnumerator ProcessTwitchCommand (string input)
    {
        string[] validPositions = new string[] { "1", "2", "3", "4", "5", "6", "TL", "TM", "TR", "BL", "BM", "BR" };
        string command = input.Trim().ToUpperInvariant();
        List<string> parameters = command.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        if (command == "SUBMIT")
        { 
            submit.OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
        else if (command == "CYCLE")
        {
            yield return null;
            for (int i = 0; i < 6; i++)
            {
                arrows[i].OnInteract();
                yield return new WaitForSeconds(1);
            }
        }
        else if (parameters.First() == "PRESS")
        {
            parameters.Remove("PRESS");
            if (!parameters.All(x => validPositions.Contains(x)))
                yield return "sendtochaterror Invalid position";
            yield return null;
            foreach (string position in parameters)
            {
                arrows[Array.IndexOf(validPositions, position) % 6].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
        }
    }

    IEnumerator TwitchHandleForcedSolve ()
    {
        if (interacted)
        {
            moduleSolved = true;
            GetComponent<KMBombModule>().HandlePass();
        }
        else
        {
            foreach (int movement in generatedPath)
            {
                for (int i = 0; i < 6; i++)
                {
                    if (allValues[i] == movement)
                    {
                        arrows[i].OnInteract();
                        yield return new WaitForSeconds(0.1f);
                        break;
                    }
                }
            }
            submit.OnInteract();
            while (!moduleSolved)
                yield return true;
        }
    }
}
