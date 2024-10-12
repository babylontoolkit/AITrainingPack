#if UNITY_EDITOR
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

namespace PROJECT
{
    public static class AI_TrainingPackage
    {
        [MenuItem("Assets/Train Example", false, 15)] // The action
        static void AI_TrainScriptComponent()
        {
            // Validate that the training project option is enabled
            if (CanvasToolsInfo.Instance == null || CanvasToolsInfo.Instance.AITrainingProject == false) return;

            // Get the selected file
            var obj = Selection.activeObject;
            MonoScript monoscript = null;
            if (obj is MonoBehaviour monoBehaviour)
            {
                monoscript = MonoScript.FromMonoBehaviour(monoBehaviour);
                // DEBUG: UnityEngine.Debug.Log($"DEBUG: Processing MonoBehaviour script: {monoscript.name}");
            }        
            else if (obj is MonoScript monoScript)
            {
                monoscript = monoScript;
                // DEBUG: UnityEngine.Debug.Log($"DEBUG: Processing MonoBehaviour script: {monoScript.name}");
            }

            // Get the path of the script
            string path = Path.GetFullPath(AssetDatabase.GetAssetPath(obj));

            // Process the script file here (For example, log it, open it, modify it, etc.)
            // UnityEngine.Debug.Log($"Processing MonoBehaviour script: {path}");

            // Perform your desired action here
            // For example, open the script in the default editor or modify it
            // For now, we will simply log a message

            string script = File.ReadAllText(path);
            // .. 
            // TODO: Validate Is Proper Script Content
            // ..
            CanvasTools.CanvasToolsExporter.Initialize();
            AI_DataConverter converter = ScriptableObject.CreateInstance<AI_DataConverter>();
            converter.OnInitialize(path, script, monoscript);
            converter.ShowUtility();
        }
        [MenuItem("Assets/Train Example", true)] // Validation function
        static bool AI_ValidateTrainScriptComponent()
        {
            // Validate that the training project option is enabled
            if (CanvasToolsInfo.Instance == null || CanvasToolsInfo.Instance.AITrainingProject == false) return false;

            // Validate that the selected file is a C# script and a MonoBehaviour
            var obj = Selection.activeObject;
            if (obj == null) return false;

            // Get the path to the script
            string path = Path.GetFullPath(AssetDatabase.GetAssetPath(obj));
            if (!path.EndsWith(".cs", System.StringComparison.OrdinalIgnoreCase)) return false;
            if (path.IndexOf("AI_CodeAssistant", System.StringComparison.OrdinalIgnoreCase) > 0) return false;
            if (path.IndexOf("AI_DataConverter", System.StringComparison.OrdinalIgnoreCase) > 0) return false;
            if (path.IndexOf("AI_ScriptConverter", System.StringComparison.OrdinalIgnoreCase) > 0) return false;
            
            // Check if the file contains MonoBehaviour to limit the action only to MonoBehaviour scripts
            // string script = File.ReadAllText(path);
            // return (script.Contains(": MonoBehaviour") || script.Contains(": EditorScriptComponent"));
            return true; // Note: Convert Any C# Script To Babylon Toolkit
        }
        // [MenuItem("Assets/Save Example", false, 16)] // The action
        // static void AI_AppendTrainingExample()
        // {
        //     // Validate that the training project option is enabled
        //     if (CanvasToolsInfo.Instance == null || CanvasToolsInfo.Instance.AITrainingProject == false) return;
        //
        //     // Get the selected file
        //     var obj = Selection.activeObject;
        //
        //     // Get the path of the script
        //     string path = Path.GetFullPath(AssetDatabase.GetAssetPath(obj));
        //
        //     // Process the script file here (For example, log it, open it, modify it, etc.)
        //     // UnityEngine.Debug.Log($"Processing MonoBehaviour script: {path}");
        //
        //     // Perform your desired action here
        //     // For example, open the script in the default editor or modify it
        //     // For now, we will simply log a message
        //
        //     if (UnityTools.ShowMessage("Are you sure want to append example to training dataset?", CanvasToolsStatics.CANVAS_TOOLS_TITLE, "Yes", "No"))
        //     {
        //         string data = File.ReadAllText(path);
        //         if (AI_CodeAssistant.AppendExampleToTrainingDataset(data))
        //         {
        //             UnityTools.ShowMessage("The selected training example appended successfully.", CanvasToolsStatics.CANVAS_TOOLS_TITLE);
        //         }
        //         else
        //         {
        //             UnityTools.ShowMessage("Failed to append the selected training example.", CanvasToolsStatics.CANVAS_TOOLS_TITLE);
        //         }
        //     }
        // }
        // [MenuItem("Assets/Save Example", true)] // Validation function
        // static bool AI_ValidateAppendTrainingExample()
        // {
        //     // Validate that the training project option is enabled
        //     if (CanvasToolsInfo.Instance == null || CanvasToolsInfo.Instance.AITrainingProject == false) return false;
        //
        //     // Validate that the selected file is a C# script and a MonoBehaviour
        //     var obj = Selection.activeObject;
        //     if (obj == null) return false;
        //
        //     // Get the path to the script
        //     string path = Path.GetFullPath(AssetDatabase.GetAssetPath(obj));
        //     if (!path.EndsWith(".jsonl", System.StringComparison.OrdinalIgnoreCase)) return false;
        //     if (path.IndexOf("codewrx.ai", System.StringComparison.OrdinalIgnoreCase) > 0) return false;
        //
        //     return true; // Note: Append Any JSONL File To Training Dataset
        // }
        // public static bool AppendExampleToTrainingDataset(string example)
        // {
        //     bool result = false;
        //     string assetPath = "Assets/[Training]/codewrx.ai.jsonl";
        //     if (File.Exists(assetPath))
        //     {
        //         try
        //         {
        //             // Open file in read/write mode and seek to the end of the file
        //             using (FileStream fileStream = new FileStream(assetPath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
        //             {
        //                 // Seek to the last byte in the file to check for a newline
        //                 if (fileStream.Length > 0)
        //                 {
        //                     fileStream.Seek(-1, SeekOrigin.End);
        //                     int lastByte = fileStream.ReadByte();
        //
        //                     bool endsWithNewline = (lastByte == (byte)'\n');
        //
        //                     using (StreamWriter writer = new StreamWriter(fileStream, Encoding.UTF8))
        //                     {
        //                         fileStream.Seek(0, SeekOrigin.End); // Move pointer to the end for writing
        //
        //                         // Prepare the example data with proper newline handling
        //                         string jsonLine = example.TrimEnd(Environment.NewLine.ToCharArray());
        //
        //                         if (!endsWithNewline)
        //                         {
        //                             writer.WriteLine(); // Add a newline if the file doesn't already end with one
        //                         }
        //
        //                         writer.WriteLine(jsonLine); // Append the example data with a final newline
        //                     }
        //
        //                     result = true;
        //                 }
        //                 else
        //                 {
        //                     // If file is empty, simply write the example
        //                     using (StreamWriter writer = new StreamWriter(fileStream, Encoding.UTF8))
        //                     {
        //                         string jsonLine = example.TrimEnd(Environment.NewLine.ToCharArray());
        //                         writer.WriteLine(jsonLine);
        //                     }
        //                     result = true;
        //                 }
        //             }
        //         }
        //         catch (Exception ex)
        //         {
        //             Console.WriteLine("Error appending example: " + ex.Message);
        //             result = false;
        //         }
        //     }
        //     return result;
        // }
    }
}
#endif