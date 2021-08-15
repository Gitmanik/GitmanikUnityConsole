using System;
using UnityEditor;

namespace Gitmanik.Console.Editor
{
	public class PreprocessorDefine
    {
        private static string define = "GITMANIK_CONSOLE_USED";

        [InitializeOnLoadMethod]
        public static void AddDefine()
        {
            string symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(
                BuildTargetGroup.Standalone);

            if (!symbols.Contains(define))
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(
                    BuildTargetGroup.Standalone, symbols + ";" + define);
            }
        }
    }
}