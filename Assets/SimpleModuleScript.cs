using System.Collections.Generic;
using UnityEngine;
using KModkit;
using Newtonsoft.Json;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections;
using System;
using Rnd = UnityEngine.Random;
using System.Runtime.InteropServices;

public class SimpleModuleScript : MonoBehaviour
{

    //Debug.LogFormat("[Eye Sees All #{0}] {1}", ModuleId, message);

    //Base vars
    public KMAudio audio;
    public KMBombInfo info;
    public KMBombModule module;
    public KMSelectable moduleSelect;
    public KMSelectable eyeSelectable;
    public GameObject[] eyelids;
    public GameObject[] iris;
    public GameObject irisLead;
    static int ModuleIdCounter = 1;
    int ModuleId;

    //Calculation vars
    private int cyclePos = 0;
    private int vibrPos = 0;
    private int cycleCount = 0;
    private int stage = 0;
    private Vector3 initial = new Vector3(0, 0, 0);
    private Vector3 centre = new Vector3(0,0,0);
    private Vector3 reqPos = new Vector3(0, 0, 0);

    private int[] vibration = new int[4] { 0, 1, 2, 3 }; //0NW, 1NE, 2SW, 3SE
    private int[] cornerMoves = new int[4] { 0, 1, 2, 3 }; //0NW, 1NE, 2SW, 3SE
    private int[,] angles = new int[4, 3];
    private int[,] colorNums = new int[4, 3];

    public Material[] colors;
    public Material black;

    private int framerate = 4;
    private bool focused = false;

    void Awake()
    {
        ModuleId = ModuleIdCounter++;
        eyeSelectable.OnInteract += delegate () { Select(); return false; };
        moduleSelect.OnFocus += delegate () { eyeToggle(true); };
        moduleSelect.OnDefocus += delegate () { eyeToggle(false); };
    }
    // Use this for initialization
    void Start()
    {
        eyeToggle(false);

        centre = irisLead.transform.localPosition;
        reqPos = centre;
        initial = centre;
        Shuffle(vibration);
        Shuffle(cornerMoves);

        string printer = "";
        foreach(int i in vibration) { printer += i; }
        Debug.LogFormat("[Eye Sees All #{0}] Vibration directions are {1}", ModuleId, printer);

        printer = "";
        foreach (int i in cornerMoves) { printer += i; }
        Debug.LogFormat("[Eye Sees All #{0}] Corner directions are {1}", ModuleId, printer);

        for (int i = 0; i < 4; i++)
        {
            angles[i, 0] = Rnd.Range(0, 4);
            angles[i, 1] = Rnd.Range(0, 4);
            while (angles[i, 0] == angles[i, 1]) { angles[i, 1] = Rnd.Range(0, 4); }
            angles[i, 2] = Rnd.Range(0, 4);
            while (angles[i, 2] == angles[i, 1] || angles[i, 2] == angles[i, 0]) { angles[i, 2] = Rnd.Range(0, 4); }
        }
        for (int i = 0; i < 4; i++)
        {
            colorNums[i, 0] = Rnd.Range(0, 4);
            colorNums[i, 1] = Rnd.Range(0, 4);
            while (colorNums[i, 0] == colorNums[i, 1]) { colorNums[i, 1] = Rnd.Range(0, 4); }
            colorNums[i, 2] = Rnd.Range(0, 4);
            while (colorNums[i, 2] == colorNums[i, 1] || colorNums[i, 2] == colorNums[i, 0]) { colorNums[i, 2] = Rnd.Range(0, 4); }
        }

        printer = "";
        foreach (int i in angles) { printer += i; }
        Debug.LogFormat("[Eye Sees All #{0}] Angles are {1}", ModuleId, printer);

        printer = "";
        foreach (int i in colorNums) { printer += i; }
        Debug.LogFormat("[Eye Sees All #{0}] Color numbers are {1}", ModuleId, printer);
    }

    // Update is called once per frame
    void Update()
    {
        if (!focused) return;

        //Corner movement
        if (Time.frameCount % (framerate * 4) == 0 && stage == 0)
        {
            for (int i = 0; i < 3; i++)
            {
                Vector3 scale = iris[i].transform.localScale;
                scale.x = 0.05f;
                iris[i].transform.localScale = scale;
            }

            centre = initial;
            switch (cornerMoves[cyclePos])
            {
                case 0: centre.x -= 0.007f; centre.z += 0.003f; break;
                case 1: centre.x += 0.007f; centre.z += 0.003f; break;
                case 2: centre.x -= 0.007f; centre.z -= 0.003f; break;
                case 3: centre.x += 0.007f; centre.z -= 0.003f; break;
            }
            cyclePos++;
            if (cyclePos == 4) { stage++; }
            irisLead.SetActive(true);
            irisLead.transform.localPosition = centre;
        }
        else if (Time.frameCount % (framerate * 2) == 0 && stage == 1) //Line movement
        {
            centre = initial;
            irisLead.SetActive(false);

            for (int i = 0; i < 3; i++)
            {
                Renderer renderer = iris[i].GetComponent<Renderer>();
                renderer.material = colors[colorNums[cyclePos % 4, i]];
                Vector3 scale = iris[i].transform.localScale;
                Vector3 angle = iris[i].transform.localEulerAngles;
                scale.x = 0.01f;
                angle.y = angles[cyclePos % 4, i] * 45;
                iris[i].transform.localScale = scale;
                iris[i].transform.localEulerAngles = angle;
            }
            cyclePos++;
            if (cyclePos == 10) { stage++; }
        }
        else if (Time.frameCount % (framerate * 2) == 0 && stage == 2) //Still line
        {
            for (int i = 0; i < 3; i++)
            {
                Renderer renderer = iris[i].GetComponent<Renderer>();
                renderer.material = black;
                Vector3 angle = iris[i].transform.localEulerAngles;
                angle.y = 0;
                iris[i].transform.localEulerAngles = angle;
            }
            cyclePos++;
            if (cyclePos == 13) { stage = 0; cyclePos = 0; }
        }

        //Vibrations
        if (Time.frameCount % framerate == 0)
        {
            switch (vibration[vibrPos])
            {
                case 0: reqPos.x = centre.x - 0.001f; reqPos.z = centre.z + 0.001f; break;
                case 1: reqPos.x = centre.x + 0.001f; reqPos.z = centre.z + 0.001f; break;
                case 2: reqPos.x = centre.x - 0.001f; reqPos.z = centre.z - 0.001f; break;
                case 3: reqPos.x = centre.x + 0.001f; reqPos.z = centre.z - 0.001f; break;
            }
            foreach (GameObject element in iris) { element.transform.localPosition = reqPos; }
            vibrPos = (vibrPos + 1) % 4;
        }
    }

    void Shuffle(int[] arr)
    {
        for (int i = 3; i > 0; i--)
        {
            int randomIndex = Rnd.Range(0, 4);
            int temp = arr[i];
            arr[i] = arr[randomIndex];
            arr[randomIndex] = temp;
        }
    }

    void eyeToggle(bool on)
    {
        foreach (GameObject ir in iris) { ir.SetActive(on); }
        focused = on;
        if (!on)
        {
            Vector3 pos = eyelids[0].transform.localPosition;
            pos.z = -0.0115f;
            eyelids[0].transform.localPosition = pos;
            pos.z = 0.0012f;
            eyelids[1].transform.localPosition = pos;
        }
        else
        {
            Vector3 pos = eyelids[0].transform.localPosition;
            pos.z = 0.008f;
            eyelids[0].transform.localPosition = pos;
            pos.z = -0.015f;
            eyelids[1].transform.localPosition = pos;

            stage = 0;
            cyclePos = 0;
            for (int i = 0; i < 3; i++) //Set eye to normal
            {
                Renderer renderer = iris[i].GetComponent<Renderer>();
                renderer.material = black;
                Vector3 scale = iris[i].transform.localScale;
                Vector3 angle = iris[i].transform.localEulerAngles;
                scale.x = 0.05f;
                angle.y = 0;
                iris[i].transform.localScale = scale;
                iris[i].transform.localEulerAngles = angle;
            }
        }
    }

    void Select()
    {
        audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        eyeSelectable.AddInteractionPunch(0.5f);
    }

    #pragma warning disable 414
    private string TwitchHelpMessage = "!{0} eye [selects the eye]";
    #pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        if (command.ToLower() == "eye") { eyeSelectable.OnInteract(); yield return null; }
        else { yield return "sendtochaterror Not the 'eye' parameter!"; }
    }
}
