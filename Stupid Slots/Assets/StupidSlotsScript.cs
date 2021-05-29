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

    private int[][] orders = new int[3][].Select(x => x = Enumerable.Range(0,10).ToArray()).ToArray();
    private Material[][] slotColors = new Material[3][] { new Material[10], new Material[10], new Material[10] };
    private Material[][] slotTextColors = new Material[3][] { new Material[10], new Material[10], new Material[10] };
    private FontInfo[][] slotFonts;

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

    int spinCount = 0;
    bool interacted;

    void Awake ()
    {
        slotFonts = new FontInfo[3][].Select(x => new FontInfo[]
        {
            new FontInfo(fonts[0], fontMats[0], new Vector3(0,-1,4), Vector3.one),
            new FontInfo(fonts[1], fontMats[1], new Vector3(-0.2f,0,4), Vector3.one),
            new FontInfo(fonts[2], fontMats[2], new Vector3(0,-0.5f,4), 0.9f*Vector3.one),
            new FontInfo(fonts[3], fontMats[3], new Vector3(0,0,4), 0.8f*Vector3.one),
            new FontInfo(fonts[4], fontMats[4], new Vector3(0,1.5f,4), 0.8f*Vector3.one),
            new FontInfo(fonts[5], fontMats[5], new Vector3(0,-1.5f,4), Vector3.one),
            new FontInfo(fonts[6], fontMats[6], new Vector3(0,0,4), 0.5f*Vector3.one),
            new FontInfo(fonts[7], fontMats[7], new Vector3(0.25f,1,4), new Vector3(1, 0.8f, 1)),
            new FontInfo(fonts[8], fontMats[8], new Vector3(0,-0.5f,4), Vector3.one),
            new FontInfo(fonts[9], fontMats[9], new Vector3(0.5f,0.5f,4), new Vector3(1, 0.9f, 1))
        }).ToArray();
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
        currentValue = Mod(currentValue + allValues[pos], 1000);
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
            }.Shuffle();
            valuesUpper = new int[] { 0, relevantValues[0], relevantValues[1] }.Shuffle();
            valuesLower = new int[] { 0, relevantValues[2], relevantValues[3] }.Shuffle();
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
            slotFonts[slotIndex].Shuffle();
            do
            {
                int[] tempOrder = Enumerable.Range(0,10).ToArray().Shuffle();
                for (int i = 0; i < 10; i++)
                    slotColors[slotIndex][i] = colors[tempOrder[i]];
                tempOrder.Shuffle();
                for (int i = 0; i < 10; i++)
                    slotTextColors[slotIndex][i] = colors[tempOrder[i]];
            } while (Enumerable.Range(0,10).Any(x => slotColors[slotIndex][x] == slotTextColors[slotIndex][x])); //Checks
        }
    }
    void SetDisplays()
    {
        for (int slotIndex = 0; slotIndex < 3; slotIndex++)
        {
            TextMesh targetMesh = displays[slotIndex].GetComponentInChildren<TextMesh>();
            int digit = currentValue.ToString().PadLeft(3, '0')[slotIndex] - '0'; 
            displays[slotIndex].GetComponent<MeshRenderer>().material = slotColors[slotIndex][digit];
            targetMesh.text = digit.ToString();
            targetMesh.color = slotTextColors[slotIndex][digit].color;
            targetMesh.font = slotFonts[slotIndex][digit].font;
            targetMesh.GetComponent<MeshRenderer>().material = slotFonts[slotIndex][digit].mat;
            targetMesh.transform.localPosition = slotFonts[slotIndex][digit].position;
            targetMesh.transform.localScale = slotFonts[slotIndex][digit].scale;
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
                case 2: if ((operationResults[i] != 0 && number % operationResults[i] == 0) || number % (operationResults[i] + 5) == 0) validities[i] = true; break; //Needs to abort the calc if we're checking for divisibility by 0
                case 3: if ((number / 100) == operationResults[i] || (number / 100) == operationResults[i] + 5) validities[i] = true; break;
                case 4: if (number % 100 / 10 == operationResults[i] || number % 100 / 10 == operationResults[i] + 5) validities[i] = true; break;
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

        Audio.PlaySoundAtTransform("bogosort", transform);
        Debug.LogFormat("[Stupid Slots #{0}] Submitted value {1}.", moduleId, currentValue);
        if (CheckValidities(currentValue))
            moduleSolved = true;

        isAnimating = true;
        for (int i = 0; i < 22; i++)
        {
            currentValue = UnityEngine.Random.Range(0, 1000);
            SetDisplays();
            yield return new WaitForSecondsRealtime(0.25f);
        }
        isAnimating = false;
        if (moduleSolved)
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
        spinCount++;
        if (spinCount > 100)
        {
            Vector3 movement = new Vector3(UnityEngine.Random.Range(-10, 11), 0, UnityEngine.Random.Range(-10, 11));
            for (int i = 0; i < 3; i++)
                displays[i].GetComponentInChildren<TextMesh>().text = ":|";
            for (int i = 0; i < 500; i++)
            {
                Audio.PlaySoundAtTransform("boing", submit.transform);
                submit.transform.localEulerAngles += new Vector3(0, 8, 0);
                submit.transform.localPosition += 0.001f * movement;
                yield return null;
            }
            for (int i = 0; i < 3; i++)
                displays[i].GetComponentInChildren<TextMesh>().text = ":)";
        }
        for (int i = 0; i < 45; i++)
        {
            submit.transform.localEulerAngles += new Vector3(0, 8, 0);
            yield return null;
        }
    }

    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use <!{0} press 1 2 3 4 5 6> or <!{0} press TL TM TR BL BM BR> to press the arrow buttons in that positions. Use <!{0} submit> to press the submit button. Use <!{0} cycle> to press each arrow button once slowly.";
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
            yield return moduleSolved ? "solve" : "strike";
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
            if (parameters.Any(x => !validPositions.Contains(x)))
            {
                yield return "sendtochaterror Invalid position";
                yield break;
            }
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
