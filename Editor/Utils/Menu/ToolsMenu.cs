//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace BlueCheese.Core
{
    public class ToolsMenu : MonoBehaviour
    {
        [MenuItem("Tools/Force compilation")]
		public static void ToolsForceCompilation()
		{
			CompilationPipeline.RequestScriptCompilation(RequestScriptCompilationOptions.CleanBuildCache);
		}

    }
}
