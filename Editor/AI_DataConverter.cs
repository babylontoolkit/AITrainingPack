#if UNITY_EDITOR
using System;
using System.IO;
using System.Net;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using System.Collections.Specialized;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

using UnityEditor;
using UnityEngine;

public class AI_DataConverter : EditorWindow
{
    private string outputFilename = null;
    private string scriptContent = null;
    private MonoScript scriptAsset = null;
    private DefaultAsset babylonAsset = null;
    private TextAsset trainingAsset = null;
    private string trainingPath = null;
    private string scriptDescription = null;
    private bool isCustomPrompt = false;
    private bool isUsingCustomPrompt = false;

    public void OnInitialize(string scriptFilename, string scriptContent, MonoScript scriptComponent)
    {
        maxSize = new Vector2(604, 140);
        minSize = this.maxSize;
        string fileName = Path.GetFileNameWithoutExtension(scriptFilename);
        this.scriptContent = scriptContent;
        this.scriptAsset = scriptComponent;
        this.scriptDescription = fileName;
        this.outputFilename = Path.Combine(Directory.GetParent(scriptFilename).FullName, fileName + ".jsonl");
        this.trainingPath = PROJECT.AI_TrainingPackage.ReadOpenAITraining();
        // ..
        // DEBUG: UnityEngine.Debug.LogFormat("DEBUG: Convert script component initialized - script: {0}", scriptFilename);
        // DEBUG: UnityEngine.Debug.LogFormat("DEBUG: Convert script component initialized - output: {0}", this.outputFilename);
        // ..
        string babylonFilename = Path.Combine(Directory.GetParent(scriptFilename).FullName, fileName + ".ts");
        if (!String.IsNullOrEmpty(babylonFilename) && File.Exists(babylonFilename))
        {
            string fixedBabylonFilename = PROJECT.AI_TrainingPackage.GetAssetPathFromFullPath(babylonFilename);
            babylonAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(fixedBabylonFilename);
        }
        else
        {
            babylonAsset = null;
        }
        string customFilename = Path.Combine(Directory.GetParent(scriptFilename).FullName, fileName + ".txt");
        if (!String.IsNullOrEmpty(customFilename) && File.Exists(customFilename))
        {
            this.trainingPath = PROJECT.AI_TrainingPackage.GetAssetPathFromFullPath(customFilename);
            this.isCustomPrompt = true;
            this.isUsingCustomPrompt = false;
        }
        else
        {
            this.isCustomPrompt = false;
            this.isUsingCustomPrompt = false;
        }
    }

    void OnEnable()
    {
        titleContent = new GUIContent("Training Example Wizard");
    }

    public void OnGUI()
    {
        scriptDescription = EditorGUILayout.TextField("Description:", scriptDescription, GUILayout.ExpandWidth(true));
        EditorGUILayout.Space();

        EditorGUI.BeginDisabledGroup(true);
            scriptAsset = (MonoScript)EditorGUILayout.ObjectField("Unity Script File:", scriptAsset, typeof(MonoScript), false);
            EditorGUILayout.Space();
        EditorGUI.EndDisabledGroup();

        babylonAsset = (DefaultAsset)EditorGUILayout.ObjectField("Babylon Pair Script:", babylonAsset, typeof(DefaultAsset), false);
        EditorGUILayout.Space();

        trainingAsset = (TextAsset)EditorGUILayout.ObjectField("Training Prompt File:", trainingAsset, typeof(TextAsset), false);
        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Reset Training Prompt"))
        {
            if (UnityTools.ShowMessage("Are you sure want to reset the training prompt?", CanvasToolsStatics.CANVAS_TOOLS_TITLE, "Yes", "No"))
            {
                ResetDefaultTraining();
            }
        }
        if (GUILayout.Button("Generate Training Example"))
        {
            ConvertTrainingDataset();
        }
        EditorGUILayout.EndHorizontal();
    }

    public void OnInspectorUpdate()
    {
        if (trainingAsset == null)
        {
            MonoScript mscript = MonoScript.FromScriptableObject(this);
            if (mscript != null)
            {
                // UnityEngine.Debug.LogFormat("TRAIN DEBUG: Script Name: {0}", mscript.name);
                string scriptPath = AssetDatabase.GetAssetPath(mscript);
                if (!String.IsNullOrEmpty(scriptPath))
                {
                    // UnityEngine.Debug.LogFormat("TRAIN DEBUG: Script Path: {0}", scriptPath);
                    string folderPath = System.IO.Path.GetDirectoryName(scriptPath);
                    // UnityEngine.Debug.LogFormat("TRAIN DEBUG: Folder Path: {0}", folderPath);
                    string defaultPath = (folderPath + "/DefaultTrainingExample.txt");
                    // UnityEngine.Debug.LogFormat("TRAIN DEBUG: Persona Path: {0}", defaultPath);
                    // DEPRECATED: string templatePath = !System.String.IsNullOrEmpty(this.trainingPath) ? this.trainingPath : defaultPath;
                    string templatePath = defaultPath;
                    if (!System.String.IsNullOrEmpty(this.trainingPath))
                    {
                        templatePath = this.trainingPath;
                        if (this.isCustomPrompt == true)
                        {
                            this.isUsingCustomPrompt = true;
                        }
                    }
                    try
                    {
                        trainingAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(templatePath);
                    }
                    catch /*(Exception e)*/
                    {
                        // DEBUG: UnityEngine.Debug.LogErrorFormat("Load Prompt Asset At Path Error: {0}", e.Message);
                    }
                    // ..
                    // Double Check Has Valid Prompt Asset
                    // ..
                    if (trainingAsset == null)
                    {
                        try
                        {
                            trainingAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(defaultPath);
                        }
                        catch /*(Exception e)*/
                        {
                            // DEBUG: UnityEngine.Debug.LogErrorFormat("Load Prompt Asset At Path Error: {0}", e.Message);
                        }
                    }
                    // ..
                    // Mark the scene as dirty so the change is saved
                    // ..
                    if (trainingAsset != null)
                    {
                        EditorUtility.SetDirty(this);
                    }
                    else
                    {
                        // DEBUG: UnityEngine.Debug.LogWarningFormat("Failed To Load Prompt Asset File At: {0}", templatePath);
                    }
                }
                else
                {
                    // DEBUG: UnityEngine.Debug.LogWarning("Failed To Get Script Path For ScriptableObject");
                }
            }
            else
            {
                // DEBUG: UnityEngine.Debug.LogWarning("Failed To Get MonoScript For ScriptableObject");
            }
        }
        this.Repaint();
    }

    private void ResetDefaultTraining()
    {
        this.trainingPath = null;
        this.trainingAsset = null;
        PROJECT.AI_TrainingPackage.DeleteOpenAITraining();
    }

    private void ConvertTrainingDataset()
    {
        if (System.String.IsNullOrEmpty(this.scriptContent))
        {
            UnityTools.ShowMessage("You must specify a valid script component to convert.", CanvasToolsStatics.CANVAS_TOOLS_TITLE);
            return;
        }
        if (System.String.IsNullOrEmpty(this.scriptDescription))
        {
            UnityTools.ShowMessage("You must enter a valid script description.", CanvasToolsStatics.CANVAS_TOOLS_TITLE);
            return;
        }
        if (this.trainingAsset == null || System.String.IsNullOrEmpty(this.trainingAsset.text))
        {
            UnityTools.ShowMessage("You must specify a valid training prompt file.", CanvasToolsStatics.CANVAS_TOOLS_TITLE);
            return;
        }
        if (this.babylonAsset == null)
        {
             UnityTools.ShowMessage("You must specify a valid babylon pair script.", CanvasToolsStatics.CANVAS_TOOLS_TITLE);
             return;
        }
        if (File.Exists(this.outputFilename))
        {
            if (!UnityTools.ShowMessage("Are you sure you want to overwrite existing output file?", CanvasToolsStatics.CANVAS_TOOLS_TITLE, "Yes", "No"))
            {
                return;
            }                    
        }
        bool close = false;
        UnityTools.ReportProgress(1, "AI code assistant is generating training example...");
        if (this.isUsingCustomPrompt == false) PROJECT.AI_TrainingPackage.SaveOpenAITraining(AssetDatabase.GetAssetPath(this.trainingAsset));
        try
        {
            string scriptName = this.scriptDescription;
            string scriptContent = this.scriptContent;
            string defaultPrompt = PROJECT.AI_TrainingPackage.DEFAULT_TRAINING_EXAMPLE;
            string examplePrompt = (trainingAsset != null && !String.IsNullOrEmpty(trainingAsset.text)) ? trainingAsset.text : defaultPrompt;
            string babylonFilename = AssetDatabase.GetAssetPath(this.babylonAsset);
            if (File.Exists(babylonFilename))
            {
                string babylonContent = File.ReadAllText(babylonFilename);
                string templateContent = @"{{""messages"": [{{""role"": ""system"", ""content"": ""{0} {1}""}}, {{""role"": ""user"", ""content"": ""{2}""}}, {{""role"": ""assistant"", ""content"": ""{3}""}}]}}";
                string datasetContent = string.Format(templateContent, scriptName, examplePrompt, PROJECT.AI_TrainingPackage.EscapeDefaultJsonString(scriptContent), PROJECT.AI_TrainingPackage.EscapeDefaultJsonString(babylonContent));
                File.WriteAllLines(this.outputFilename, new string[] { datasetContent });
                close = true;
                AssetDatabase.Refresh();
                UnityTools.ShowMessage("AI training example dataset created successfully.", CanvasToolsStatics.CANVAS_TOOLS_TITLE);
            }
            else
            {
                UnityTools.ShowMessage("Failed to locate babylon script pair file.", CanvasToolsStatics.CANVAS_TOOLS_TITLE);
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogException(ex);
            UnityTools.ShowMessage("Exception: " + ex.Message, CanvasToolsStatics.CANVAS_TOOLS_TITLE);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            if (close == true) this.Close();
        }
    }
}
#endif