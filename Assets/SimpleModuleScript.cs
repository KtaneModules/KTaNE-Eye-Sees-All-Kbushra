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

    private int framerate = 4; //Normally 4
    private bool focused = false;

    private int[] newCode = new int[4];
    private string movementCalc = "";
    private int[] angcolProduct = new int[4];
    private int clickCount = 0;
    private int clickPos = 0;
    private float lastCall = 0;
    private bool checkTime = false;

    private bool solved = false;

    public class ModSettingsJSON { public int _framerate; }
    public KMModSettings modSettings;

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

        while (vibration[0] != 0)
        {
            int temp = vibration[1];
            vibration[1] = vibration[0];
            int temp2 = vibration[2];
            vibration[2] = temp;
            temp = vibration[3];
            vibration[3] = temp2;
            vibration[0] = temp;
        }

        string printer = "";
        foreach(int i in vibration) { printer += i+1; }
        Debug.LogFormat("[Eye Sees All #{0}] Vibration directions (0NW, 1NE, 2SW, 3SE) are {1}", ModuleId, printer);

        printer = "";
        foreach (int i in cornerMoves) { printer += i+1; }
        Debug.LogFormat("[Eye Sees All #{0}] Corner directions (0NW, 1NE, 2SW, 3SE) are {1}", ModuleId, printer);

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
        foreach (int i in angles) { printer += i+1; }
        Debug.LogFormat("[Eye Sees All #{0}] Angles (3 values 4 times) are {1}", ModuleId, printer);

        printer = "";
        foreach (int i in colorNums) { printer += i+1; }
        Debug.LogFormat("[Eye Sees All #{0}] Color numbers (3 values 4 times) are {1}", ModuleId, printer);

        Calculate();
        framerate = ManageOptions();
    }

    int ManageOptions()
    {
        ModSettingsJSON settings = JsonConvert.DeserializeObject<ModSettingsJSON>(modSettings.Settings);
        if (settings != null)
        {
            if (settings._framerate < 1) { Debug.LogFormat("[Eye Sees All #{0}] Framerate set to lower than 1. Using default settings.", ModuleId); return 4; }
            if (settings._framerate > 15) { Debug.LogFormat("[Eye Sees All #{0}] Framerate set to higher than 15. Using default settings.", ModuleId); return 4; }
            Debug.LogFormat("[Eye Sees All #{0}] Using given settings of framerate {1}.", ModuleId, settings._framerate);
            return settings._framerate;
        }
        Debug.LogFormat("[Eye Sees All #{0}] Settings not found. Using default settings.", ModuleId);
        return 4;
    }

    void Calculate()
    {
        for(int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            { if (vibration[i] == cornerMoves[j]) { movementCalc += (j+1).ToString(); break; } }
        }

        Debug.LogFormat("[Eye Sees All #{0}] Digits for movement are {1}", ModuleId, movementCalc);
        for (int i = 0; i < 4; i++)
        {
            int angleSums = 0;
            int colorSums = 0;
            for (int j = 0; j < 3; j++)
            {
                angleSums += angles[i, j]+1;
                colorSums += colorNums[i, j]+1;
            }
            angcolProduct[i] += angleSums * colorSums;

            char movementChar = movementCalc.ToCharArray()[i];
            newCode[i] = angcolProduct[i] + int.Parse(movementChar.ToString());
            newCode[i] %= 10;
        }

        string printer = "";
        foreach (int i in newCode) { printer += i; }
        Debug.LogFormat("[Eye Sees All #{0}] Final digits are {1}", ModuleId, printer);

    }

    // Update is called once per frame
    void Update()
    {
        if (solved) { return; }

        if (checkTime)
        {
            lastCall += Time.deltaTime;
            if (lastCall > 1)
            {
                if (clickCount == newCode[clickPos])
                {
                    clickPos++;
                    if (clickPos > 3) { module.HandlePass(); solved = true; eyeToggle(false); }
                }
                else { module.HandleStrike(); }

                clickCount = 0;
                lastCall = 0;
                checkTime = false;
            }
        }
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
            if (cyclePos == 8) { stage++; }
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
        if (solved) { on = false; }

        foreach (GameObject ir in iris) { ir.SetActive(on); }
        focused = on;
        if (!on)
        {
            Vector3 pos = eyelids[0].transform.localPosition;
            pos.z = -0.0115f;
            eyelids[0].transform.localPosition = pos;
            pos.z = 0.0012f;
            eyelids[1].transform.localPosition = pos;

            irisLead.SetActive(false);
        }
        else
        {
            Vector3 pos = eyelids[0].transform.localPosition;
            pos.z = 0.008f;
            eyelids[0].transform.localPosition = pos;
            pos.z = -0.015f;
            eyelids[1].transform.localPosition = pos;

            centre = initial;
            switch (cornerMoves[0])
            {
                case 0: centre.x -= 0.007f; centre.z += 0.003f; break;
                case 1: centre.x += 0.007f; centre.z += 0.003f; break;
                case 2: centre.x -= 0.007f; centre.z -= 0.003f; break;
                case 3: centre.x += 0.007f; centre.z -= 0.003f; break;
            }

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
                iris[i].transform.localPosition = centre;
            }
        }
    }

    void Select()
    {
        if (solved) { return; }

        audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        eyeSelectable.AddInteractionPunch(0.5f);

        checkTime = true;
        lastCall = 0;
        clickCount++;
        clickCount %= 10;
    }

    #pragma warning disable 414
    private string TwitchHelpMessage = "!{0} eye n [selects the eye n times]";
    #pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        string[] parameters = command.Split(' ');
        if (parameters[0].ToLower() != "eye") { yield return "sendtochaterror First parameter not 'eye'!"; }
        else if (parameters.Length == 1) { yield return "sendtochaterror Too few parameters!"; }
        else if (parameters.Length > 2) { yield return "sendtochaterror Too many parameters!"; }
        else if (parameters[1].TryParseInt() == null) { yield return "sendtochaterror Second parameter not an integer!"; }
        else if (parameters[1].TryParseInt() <= 0) { yield return "sendtochaterror Second parameter less than or equal to 0!"; }
        else if (parameters[1].TryParseInt() >= 1000) { yield return "sendtochaterror Are you trying to crash the game? Second parameter too large!"; }
        else
        {
            yield return null;
            for (int i = 0; i < parameters[1].TryParseInt(); i++) { eyeSelectable.OnInteract(); }
        }
    }
}
