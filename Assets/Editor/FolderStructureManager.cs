using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEditor;

public static class FolderStructureManager 
{
    // Initialise called when editor is loaded.
    [InitializeOnLoadMethod]
    static void Initialise()
    {
       ApplyFolderStructure(DefaultFolderStructure);
    }

    private static void ApplyFolderStructure(List<FolderNode> structureToApply)
    {
        if (structureToApply == null) return;

        foreach (var node in structureToApply)
        {
            EnsureFolderExists(node);
        }
        AssetDatabase.Refresh();
        Debug.Log("Folder structure verified/applied.");
    }

    private static void EnsureFolderExists(FolderNode node)
    {
        if (string.IsNullOrEmpty(node.Path) || !node.Path.StartsWith("Assets/"))
        {
            Debug.LogWarning($"Skipping folder creation for invalid path: '{node.Path}' (Node: {node.DisplayName})");
            return;
        }

        if (!Directory.Exists(node.Path))
        {
            try
            {
                string parentPath = Path.GetDirectoryName(node.Path);
                string folderName = Path.GetFileName(node.Path);

                // Check if parent path is valid before creating
                if (!string.IsNullOrEmpty(parentPath) && !string.IsNullOrEmpty(folderName))
                {
                    string guid = AssetDatabase.CreateFolder(parentPath, folderName);
                    if (!string.IsNullOrEmpty(guid))
                    {
                        Debug.Log($"Created folder: {node.Path}");
                    }
                    else
                    {
                        Debug.LogError($"Failed to create folder: {node.Path}. AssetDatabase.CreateFolder returned empty GUID.");
                    }
                }
                else
                {
                    Debug.LogWarning($"Cannot determine parent path or folder name for: '{node.Path}'. Skipping creation.");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error creating folder '{node.Path}': {e.Message}");
            }
        }

        // Recursively process children
        if (node.Children != null && node.Children.Count > 0)
        {
            foreach (var childNode in node.Children)
            {
                EnsureFolderExists(childNode); 
            }
        }
    }

    public static string FindTargetPathForExtension(string extension, List<FolderNode> nodesToSearch)
    {
        if (string.IsNullOrEmpty(extension) || nodesToSearch == null)
        {
            return null;
        }

        foreach (var node in nodesToSearch)
        {
        
            if (node.AssociatedExtensions != null && node.AssociatedExtensions.Contains(extension))
            {
                return node.Path; 
            }

            
            if (node.Children != null && node.Children.Count > 0)
            {
                string pathInChildren = FindTargetPathForExtension(extension, node.Children);
                if (pathInChildren != null)
                {
                    return pathInChildren;
                }
            }
        }

        return null; 
    }


    public static List<FolderNode> DefaultFolderStructure { get; private set; } = new List<FolderNode>()
    {
        new FolderNode { DisplayName = "Assets", Path = "Assets/", Children = new List<FolderNode>()
        {
              new FolderNode { DisplayName = "Art", Path = "Assets/Art",  Children = new List<FolderNode>()
              {
                  new FolderNode { DisplayName = "Materials", Path = "Assets/Art/Materials",  AssociatedExtensions = new List<string> { ".mat" } },
                  new FolderNode { DisplayName = "Models", Path = "Assets/Art/Models",  AssociatedExtensions = new List<string> { ".fbx", ".obj" }},
                  new FolderNode { DisplayName = "Textures", Path = "Assets/Art/Textures", AssociatedExtensions = new List<string> { ".png", ".jpg", ".jpeg", ".tga", ".psd", ".tiff" }  }
              }},
              new FolderNode { DisplayName = "Audio", Path = "Assets/Audio", Children= new List<FolderNode>()
              {
                  new FolderNode {DisplayName ="Music", Path = "Assets/Audio/Music",  AssociatedExtensions = new List<string> { ".mp3", ".ogg" }},
                  new FolderNode {DisplayName ="Sound", Path = "Assets/Audio/Sound", AssociatedExtensions = new List<string> { ".wav" } }
              }},
              new FolderNode { DisplayName = "Code", Path = "Assets/Code" , Children = new List < FolderNode >()
              {
                  new FolderNode {DisplayName ="Scripts", Path = "Assets/Code/Scripts", AssociatedExtensions = new List<string> { ".cs" }},
                  new FolderNode {DisplayName = "Shaders", Path = "Assets/Code/Shaders", AssociatedExtensions = new List<string> { ".shader", ".cginc" }}
              }},
              new FolderNode { DisplayName = "Docs", Path = "Assets/Docs" },
              new FolderNode { DisplayName = "Level", Path = "Assets/Level", Children = new List < FolderNode >()
              {
                  new FolderNode {DisplayName = "Prefabs", Path = "Assets/Level/Prefabs", AssociatedExtensions = new List<string> { ".prefab" }},
                  new FolderNode {DisplayName = "Scenes", Path = "Assets/Level/Scenes", AssociatedExtensions = new List<string> { ".unity" }},
                  new FolderNode {DisplayName = "UI", Path = "Assets/Level/UI"},

              }}
        }
       }
    };



}

