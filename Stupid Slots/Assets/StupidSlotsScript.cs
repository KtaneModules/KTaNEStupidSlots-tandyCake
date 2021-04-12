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

    void Awake () {
        moduleId = moduleIdCounter++;
        
        foreach (KMSelectable arrow in arrows)
        {
            arrow.OnInteract += delegate () { ArrowPress(Array.IndexOf(arrows,arrow)); return false; };
        }
        submit.OnInteract += delegate () { if (!isAnimating) StartCoroutine(Submit()); return false; };

    }

    void Start ()
    {
        GetDisplayInfo();
        GetArrows();
        GetValidities();
        GetStartingNum();
    }

    void ArrowPress(int pos)
    {
        currentValue = (currentValue + allValues[pos]) % 1000;
        SetDisplays();
    }

    void GetArrows()
    {
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
        Debug.LogFormat("[Stupid Slots #{0}] The arrow values in reading order are: {1}", moduleId, allValues.Join());
    }

    int Mod(int input, int modulus)
    {
        return input % modulus + modulus % modulus;
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
    void GetStartingNum()
    {
        currentValue = UnityEngine.Random.Range(0, 1000);
        Debug.LogFormat("[Stupid Slots #{0}] The starting value is {1}.", moduleId, currentValue.ToString());
        SetDisplays();
    }
    void GetValidities()
    {
        
    }

    IEnumerator Submit()
    {
        yield return null;
    }

    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use !{0} to do something.";
    #pragma warning restore 414

    IEnumerator ProcessTwitchCommand (string Command) {
      yield return null;
    }

    IEnumerator TwitchHandleForcedSolve () {
      yield return null;
    }
}
