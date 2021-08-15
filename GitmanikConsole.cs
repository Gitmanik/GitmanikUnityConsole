#define GITMANIK_CONSOLE_USED
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using UnityEngine.UI;
using System.Linq;
using System.Reflection;

namespace Gitmanik.Console
{
    public class GitmanikConsole : MonoBehaviour
    {
        public static GitmanikConsole singleton = null;
        public static bool Visible = false;

        private static Dictionary<ConsoleCommandAttribute, MethodInfo> commands = new Dictionary<ConsoleCommandAttribute, MethodInfo>();

        [Header("Console Settings")]
        [SerializeField] private bool enableUnityLogging = false;
        [SerializeField] private int characterLimit = 0;

        [Header("UI")]
        [SerializeField] private TMP_Text consoleText = null;
        [SerializeField] private TMP_InputField inputField = null;
        [SerializeField] private Scrollbar scrollbar = null;

        private string prefix = "[GitmanikConsole]";
        private Transform consoleTransform;
        private string waitingText;

        #region MonoBehaviour

        private void OnDestroy()
        {
            singleton = null;
        }

        private void Awake()
        {
            if (singleton == null)
                singleton = this;
            else
            {
                Debug.LogError($"{prefix} Tried to create second GitmanikConsole instance!");
                Destroy(gameObject);
                return;
            }
            if (enableUnityLogging)
                Application.logMessageReceivedThreaded += HandleDebugLog;

            //consoleText.font.material.mainTexture.filterMode = FilterMode.Point;

            consoleTransform = transform.GetChild(0).transform;

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (Assembly a in assemblies)
            {
                foreach (Type t in a.GetTypes())
                {
                    foreach (MethodInfo mi in t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                    {
                        ConsoleCommandAttribute cca = mi.GetCustomAttribute<ConsoleCommandAttribute>();
                        if (cca == null)
                            continue;

                        foreach (ParameterInfo pi in mi.GetParameters())
                        {
                            if (pi.ParameterType != typeof(string))
                            {
                                Debug.LogError($"Method {mi.Name} of {t.Name} contains wrong parameter {pi.Name}! Only strings allowed.");
                                continue;
                            }

                        }

                        commands.Add(cca, mi);
                    }
                }
            }
        }

        private void Update()
        {
            if (!Visible)
                return;

            consoleText.text += waitingText;
            waitingText = string.Empty;

            if (consoleText.text.Length > characterLimit)
            {
                consoleText.text = consoleText.text.Substring(consoleText.text.IndexOf('\n', consoleText.text.Length - characterLimit));
            }
        }
        #endregion

        #region Visibility
        public void ToggleConsole()
        {
            if (!Visible)
                ShowConsole();
            else
                HideConsole();
        }

        private void ShowConsole()
        {
            Visible = true;
            consoleTransform.gameObject.SetActive(true);

            inputField.onSubmit.AddListener(ParseCommand);
            inputField.text = "";
            inputField.ActivateInputField();
        }

        private void HideConsole()
        {
            //Application.logMessageReceivedThreaded -= HandleDebugLog;
            Visible = false;
            consoleTransform.gameObject.SetActive(false);
            inputField.onSubmit.RemoveListener(ParseCommand);
        }
        #endregion

        #region Printing
        private void HandleDebugLog(string logString, string stackTrace, LogType type)
        {
            switch(type)
            {
                case LogType.Log:
                    Print(logString);
                    break;

                case LogType.Warning:
                    PrintWarning(logString);
                    break;

                case LogType.Error:
                case LogType.Assert:
                    PrintError(logString);
                    break;

                case LogType.Exception:
                    PrintError(logString + "\n" + stackTrace);
                    break;
            }
        }

        public void Print(string newText, string color = "93e743")
        {
            var timeNow = DateTime.Now;
            waitingText += $"[<color=#{color}>{timeNow.Hour:d2}:{timeNow.Minute:d2}:{timeNow.Second:d2}</color>] {newText}\n";
        }
        public void PrintWarning(string newText) => Print(newText, "ffa500");
        public void PrintError(string newText) => Print(newText, "f44242");
        public void PrintRaw(string v) => consoleText.text += v;
        #endregion

        #region Command Handling

        private string GenUsage(KeyValuePair<ConsoleCommandAttribute, MethodInfo> command) => $"{prefix} Usage: {command.Key.command} {string.Join(" ", command.Value.GetParameters().Select(x => x.Name))}";

        public void Command(string text)
        {
            string[] userInput = text.Split(' ');

            try
            {
                string cmd = userInput[0].ToLower();
                string[] param = userInput.Skip(1).ToArray();

                KeyValuePair<ConsoleCommandAttribute, MethodInfo> command = commands.First(x => x.Key.command.ToLower() == cmd);

                if (command.Value.GetParameters().Length != param.Length)
                {
                    Debug.LogError($"{prefix} Wrong parameter count for command {cmd}! Provided {param.Length} while {command.Value.GetParameters().Length} required.");
                    Debug.Log(GenUsage(command));
                    return;
                }
                try
                {
                    bool success = (bool) command.Value.Invoke(null, param);
                    if (!success)
                    {
                        Debug.LogError($"{prefix} Command {cmd} ran with error");
                        Debug.Log(GenUsage(command));
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"{prefix} {e.GetType()} while running {command.Key.command}!\n{e}");
                }
            }
            catch (InvalidOperationException)
            {
                Debug.LogWarning($"{prefix} Command \"{text}\" not found!");
            }
        }

        private void ParseCommand(string text)
        {
            inputField.text = string.Empty;
            inputField.ActivateInputField();
            scrollbar.value = 0;

            PrintRaw("> " + text + "\n");
            Command(text);
        }
        #endregion

        [ConsoleCommand("clear", "Clears console.")]
        public static bool ClearConsole()
        {
            singleton.consoleText.text = string.Empty;
            return true;
        }

        [ConsoleCommand("list", "Lists all available commands.")]
        public static bool ListCommands()
        {
            foreach (ConsoleCommandAttribute s in commands.Keys)
            {
                singleton.Print($"{s.command} - {s.description}");
            }
            return true;
        }
    }
}