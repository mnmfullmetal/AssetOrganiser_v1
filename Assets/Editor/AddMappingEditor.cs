using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class AddMappingEditor : EditorWindow
{
    public event Action<List<string>> OnApplyMappings;
    public FolderNode TargetNode { get; set; }

    private List<string> extensions = new List<string>()
    {
        // Textures
        ".png",
        ".jpg",
        ".jpeg",
        ".tga",
        ".psd", // Photoshop File
        ".tiff",
        ".bmp",
        ".gif",
        ".exr", // High Dynamic Range
        ".hdr", // High Dynamic Range

        // Models
        ".fbx",
        ".obj",
        ".blend", // Blender File (requires Blender install usually)
        ".max",
        ".ma",
        ".mb",
        // Add .max, .ma, .mb etc. if common for your users

        // Materials
        ".mat",
        ".physicsmaterial", // Physics Material 2D & 3D

        // Audio
        ".wav",
        ".mp3",
        ".ogg",
        ".aif", // AIFF Audio

        // Code / Shaders
        ".cs", // C# Script
        ".asmdef", // Assembly Definition
        ".shader", // Unity ShaderLab
        ".cginc", // Shader Include
        ".hlsl", // HLSL Shader File
        ".compute", // Compute Shader

        // Unity Specific Assets
        ".prefab",
        ".unity", // Scene file
        ".asset", // Used for ScriptableObjects, Animation Clips, etc. (Very Generic!)
        ".anim", // Animation Clip (often saved as .asset too)
        ".controller", // Animator Controller
        ".overridecontroller", // Animator Override Controller
        ".mask", // Avatar Mask
        ".rendertexture",
        ".cubemap",
        ".preset", // Unity Preset Asset

        // Fonts
        ".ttf",
        ".otf",

        // Data / Text
        ".txt",
        ".json",
        ".xml",
        ".csv",
        ".bytes", // TextAsset raw bytes

        // Video
        ".mp4",
        ".mov",
        ".webm"


    };

    public void CreateGUI()
    {
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/ExtensionMappingsEditor.uxml");
        VisualElement root = visualTree.Instantiate();
        rootVisualElement.Add(root);

        extensions.Sort();

        var predefinedExtensionsList = rootVisualElement.Q<ListView>("ExtensionsListView");
        if (predefinedExtensionsList != null)
        {
            predefinedExtensionsList.itemsSource = extensions;

            predefinedExtensionsList.makeItem = () => new Label();

            predefinedExtensionsList.bindItem = (element, index) =>
            {

                var label = element as Label;
                if (label != null && predefinedExtensionsList.itemsSource != null && index >= 0 && index < predefinedExtensionsList.itemsSource.Count)
                {
                    label.text = predefinedExtensionsList.itemsSource[index] as string ?? "Invalid Data";

                }
                else
                {
                    Debug.LogWarning("error in binding items to label in add mapping window ");
                    return;
                }
            };
        }
        else
        {
            Debug.LogWarning("ListView for extensions not found");
            return ;
        }

       
        
    }
}
