﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text.RegularExpressions;
using System;

public class RainScript : MonoBehaviour {

    public KMAudio audio;
    public KMBombInfo bomb;

    public KMSelectable[] buttons;
    public Material[] colorMats;
    public MeshRenderer[] dropRenderers;

    private Material[] privateColorMats = new Material[6];
    private List<int> whiteSpots = new List<int>() { 2, 5, 8, 11, 15, 18, 21 };
    private int[][] sequenceColors = new int[9][];
    private string[] colorNames = { "Azure", "Cornflower", "Electric", "Navy", "Teal" };
    private bool[] correctDrops = new bool[9];
    private bool[] isPressed = new bool[9];
    private bool hasPlayed = false;
    private bool isPlaying = false;

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    void Awake()
    {
        moduleId = moduleIdCounter++;
        moduleSolved = false;
        foreach (KMSelectable obj in buttons)
        {
            KMSelectable pressed = obj;
            pressed.OnInteract += delegate () { PressButton(pressed); return false; };
        }
        for (int i = 0; i < 6; i++)
            privateColorMats[i] = new Material(colorMats[i]);
    }

    void Start () {
        redo:
        for (int i = 0; i < 9; i++)
        {
            int[] nums = new int[26];
            int rando = UnityEngine.Random.Range(0, 2);
            regen:
            for (int j = 0; j < 26; j++)
            {
                if (whiteSpots.Contains(j))
                    nums[j] = 5;
                else
                    nums[j] = UnityEngine.Random.Range(0, 5);
            }
            for (int j = 0; j < 25; j++)
            {
                if (nums[j] == nums[j + 1] && rando == 0)
                    goto regen;
                else if (nums[j] == nums[j + 1] && rando == 1)
                    goto skip;
            }
            if (rando == 1)
                goto regen;
            skip:
            sequenceColors[i] = nums;
        }
        int ct = 0;
        for (int i = 0; i < 9; i++)
        {
            for (int j = 0; j < 25; j++)
            {
                if (sequenceColors[i][j] == sequenceColors[i][j + 1])
                {
                    correctDrops[i] = true;
                    ct++;
                    break;
                }
            }
        }
        if (ct == 0)
            goto redo;
        Debug.LogFormat("[Rain #{0}] The sets of each raindrop in the sequence are:", moduleId);
        for (int i = 0; i < 9; i++)
        {
            string seq = colorNames[sequenceColors[i][0]];
            for (int k = 1; k < 26; k++)
            {
                if (sequenceColors[i][k] == 5)
                    seq += " |";
                else if (sequenceColors[i][k - 1] == 5)
                    seq += " " + colorNames[sequenceColors[i][k]];
                else
                    seq += ", " + colorNames[sequenceColors[i][k]];
            }
            Debug.LogFormat("[Rain #{0}] {2}: {1}", moduleId, seq, i + 1);
        }
        string cor = "";
        bool first = true;
        for (int i = 0; i < 9; i++)
        {
            if (correctDrops[i])
            {
                if (first)
                {
                    cor += i + 1;
                    first = false;
                }
                else
                    cor += ", " + (i + 1);
            }
        }
        Debug.LogFormat("[Rain #{0}] The correct raindrops are: {1}", moduleId, cor);
    }

    void PressButton(KMSelectable pressed)
    {
        if (moduleSolved != true)
        {
            if (pressed == buttons[0])
            {
                if (isPlaying) return;
                pressed.AddInteractionPunch(0.75f);
                audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, pressed.transform);
                if (!hasPlayed)
                    hasPlayed = true;
                isPlaying = true;
                StartCoroutine(PlaySequence());
            }
            else
            {
                pressed.AddInteractionPunch(0.5f);
                if (!hasPlayed)
                {
                    Debug.LogFormat("[Rain #{0}] Attempted to interact with a raindrop button before playing the sequence. Strike!", moduleId);
                    GetComponent<KMBombModule>().HandleStrike();
                    return;
                }
                if (isPlaying || isPressed[Array.IndexOf(buttons, pressed) - 1]) return;
                else if (correctDrops[Array.IndexOf(buttons, pressed) - 1])
                {
                    Debug.LogFormat("[Rain #{0}] Pressed raindrop {1}, which is correct.", moduleId, Array.IndexOf(buttons, pressed));
                    isPressed[Array.IndexOf(buttons, pressed) - 1] = true;
                    audio.PlaySoundAtTransform("drop" + UnityEngine.Random.Range(1, 4), pressed.transform);
                    dropRenderers[Array.IndexOf(buttons, pressed) - 1].material = colorMats[6];
                    if (correctDrops.Where(x => x == true).Count() == isPressed.Where(x => x == true).Count())
                    {
                        moduleSolved = true;
                        GetComponent<KMBombModule>().HandlePass();
                        Debug.LogFormat("[Rain #{0}] All correct raindrops pressed, module solved!", moduleId);
                    }
                }
                else
                {
                    Debug.LogFormat("[Rain #{0}] Pressed raindrop {1}, which is incorrect. Strike!", moduleId, Array.IndexOf(buttons, pressed));
                    GetComponent<KMBombModule>().HandleStrike();
                }
            }
        }
    }

    private IEnumerator PlaySequence()
    {
        float[] times = { 0.475f, 0.475f, 0.75f, 0.475f, 0.475f, 0.75f, 0.475f, 0.475f, 0.75f, 0.475f, 0.475f, 0.475f, 0.475f, 0.475f, 0.475f, 0.75f, 0.475f, 0.475f, 0.75f, 0.475f, 0.475f, 0.75f, 0.475f, 0.475f, 0.475f, 0.475f };
        audio.PlaySoundAtTransform("sequence", transform);
        for (int k = 0; k < 26; k++)
        {
            for (int i = 0; i < 9; i++)
                dropRenderers[i].material = privateColorMats[sequenceColors[i][k]];
            StartCoroutine(FlashDrop());
            float t = 0;
            while (t < times[k])
            {
                yield return null;
                t += Time.deltaTime;
            }
        }
        for (int i = 0; i < 9; i++)
        {
            if (isPressed[i])
                dropRenderers[i].material = colorMats[6];
        }
        isPlaying = false;
    }

    private IEnumerator FlashDrop()
    {
        float t = 1f;
        while (t > 0f)
        {
            yield return null;
            for (int i = 0; i < 6; i++)
                privateColorMats[i].SetFloat("_Blend", t);
            t -= Time.deltaTime * 5f;
        }
        while (t < 0.05f)
        {
            yield return null;
            t += Time.deltaTime;
        }
        t = 0f;
        while (t < 1f)
        {
            yield return null;
            for (int i = 0; i < 6; i++)
                privateColorMats[i].SetFloat("_Blend", t);
            t += Time.deltaTime * 5f;
        }
    }

    //twitch plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} start [Presses the ""Start Rainfall"" button] | !{0} startfocus [Presses the ""Start Rainfall"" button AND focuses on the module (use if zooming)] | !{0} press <p1> (p2)... [Presses the raindrop button(s) in the specified postion(s)] | Valid positions are 1-9 in reading order or tl, mm, br, etc.";
    #pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string command)
    {
        if (isPlaying)
        {
            yield return null;
            yield return "sendtochaterror Cannot interact with the module while it's playing the sequence!";
            yield break;
        }
        if (Regex.IsMatch(command, @"^\s*start\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            buttons[0].OnInteract();
            yield break;
        }
        if (Regex.IsMatch(command, @"^\s*startfocus\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            buttons[0].OnInteract();
            while (isPlaying)
                yield return "trycancel Focus on sequence cancelled due to cancel request.";
            yield break;
        }
        string[] parameters = command.Split(' ');
        if (Regex.IsMatch(parameters[0], @"^\s*press\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            if (parameters.Length == 1)
            {
                yield return "sendtochaterror Please specify the position(s) of the raindrop button(s) to press!";
            }
            else
            {
                string[] valids = { "tl", "tm", "tr", "ml", "mm", "mr", "bl", "bm", "br" };
                for (int i = 1; i < parameters.Length; i++)
                {
                    int temp = -1;
                    if (!valids.Contains(parameters[i].ToLower()) && !int.TryParse(parameters[i], out temp))
                    {
                        yield return "sendtochaterror!f The specified position '" + parameters[i] + "' is invalid!";
                        yield break;
                    }
                    else if (!valids.Contains(parameters[i].ToLower()))
                    {
                        if (temp < 1 || temp > 9)
                        {
                            yield return "sendtochaterror!f The specified position '" + parameters[i] + "' is out of range 1-9!";
                            yield break;
                        }
                    }
                }
                for (int i = 1; i < parameters.Length; i++)
                {
                    int temp;
                    if (int.TryParse(parameters[i], out temp))
                        buttons[temp].OnInteract();
                    else if (valids.Contains(parameters[i].ToLower()))
                        buttons[Array.IndexOf(valids, parameters[i].ToLower()) + 1].OnInteract();
                    yield return new WaitForSeconds(0.1f);
                }
            }
            yield break;
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        while (isPlaying) yield return true;
        if (!hasPlayed)
        {
            buttons[0].OnInteract();
            while (isPlaying) yield return true;
        }
        for (int i = 0; i < 9; i++)
        {
            if (correctDrops[i] && !isPressed[i])
            {
                buttons[i + 1].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
        }
    }
}
