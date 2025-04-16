using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEditor;
using System.Xml.Schema;
using System;

public static class FolderStructureManager 
{
    public static string PresetSaveDirectory => "ProjectSettings/AssetOrganiserPresets";
    public static readonly char[] invalidFileNameChars = Path.GetInvalidFileNameChars();

    //Default folder structure following unity project organisation best practices.
    public static List<FolderNode> DefaultFolderStructure { get; private set; } = new List<FolderNode>()
    {
        new FolderNode { displayName = "Assets", path = "Assets/", children = new List<FolderNode>()
        {
              new FolderNode { displayName = "Art", path = "Assets/Art",  children = new List<FolderNode>()
              {
                  new FolderNode { displayName = "Materials", path = "Assets/Art/Materials",  associatedExtensions = new List<string> { ".mat" } },
                  new FolderNode { displayName = "Models", path = "Assets/Art/Models",  associatedExtensions = new List<string> { ".fbx", ".obj" }},
                  new FolderNode { displayName = "Textures", path = "Assets/Art/Textures", associatedExtensions = new List<string> { ".png", ".jpg", ".jpeg", ".tga", ".psd", ".tiff" }  }
              }},
              new FolderNode { displayName = "Audio", path = "Assets/Audio", children= new List<FolderNode>()
              {
                  new FolderNode {displayName ="Music", path = "Assets/Audio/Music",  associatedExtensions = new List<string> { ".mp3", ".ogg" }},
                  new FolderNode {displayName ="Sound", path = "Assets/Audio/Sound", associatedExtensions = new List<string> { ".wav" } }
              }},
              new FolderNode { displayName = "Code", path = "Assets/Code" , children = new List < FolderNode >()
              {
                  new FolderNode {displayName ="Scripts", path = "Assets/Code/Scripts", associatedExtensions = new List<string> { ".cs" }},
                  new FolderNode {displayName = "Shaders", path = "Assets/Code/Shaders", associatedExtensions = new List<string> { ".shader", ".cginc" }}
              }},
              new FolderNode { displayName = "Docs", path = "Assets/Docs" },
              new FolderNode { displayName = "Level", path = "Assets/Level", children = new List < FolderNode >()
              {
                  new FolderNode {displayName = "Prefabs", path = "Assets/Level/Prefabs", associatedExtensions = new List<string> { ".prefab" }},
                  new FolderNode {displayName = "Scenes", path = "Assets/Level/Scenes", associatedExtensions = new List<string> { ".unity" }},
                  new FolderNode {displayName = "UI", path = "Assets/Level/UI"},

              }}
        }
       }
    };

    // Initialise called when editor is loaded.
    [InitializeOnLoadMethod]
    static void Initialise()
    {
        ApplyFolderStructure(DefaultFolderStructure);

        if(PresetSaveDirectory != null)
        {
            Directory.CreateDirectory(PresetSaveDirectory);

        }
    }

    public static void ApplyFolderStructure(List<FolderNode> structureToApply)
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
        // Ensure that the folders being created are within root folder "assets"
        if (string.IsNullOrEmpty(node.path) || !node.path.StartsWith("Assets/"))
        {
            Debug.LogWarning($"Skipping folder creation for invalid path: '{node.path}' (Node: {node.displayName})");
            return;
        }

        // Check that the folder being created does not already exist
        if (!Directory.Exists(node.path))
        {
            try
            {
                string parentPath = Path.GetDirectoryName(node.path);
                string folderName = Path.GetFileName(node.path);

                // Check if parent path and the newfolder name exist before creating
                if (!string.IsNullOrEmpty(parentPath) && !string.IsNullOrEmpty(folderName))
                {
                    // Get GUID and check if a folder was created sucessfully. 
                    string guid = AssetDatabase.CreateFolder(parentPath, folderName);
                    if (!string.IsNullOrEmpty(guid))
                    {
                        Debug.Log($"Created folder: {node.path}");
                    }
                    else
                    {
                        Debug.LogError($"Failed to create folder: {node.path}. AssetDatabase.CreateFolder returned empty GUID.");
                    }
                }
                else
                {
                    Debug.LogWarning($"Cannot determine parent path or folder name for: '{node.path}'. Skipping creation.");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error creating folder '{node.path}': {e.Message}");
            }
        }

        // Recursively process children folders
        if (node.children != null && node.children.Count > 0)
        {
            foreach (var childNode in node.children)
            {
                EnsureFolderExists(childNode); 
            }
        }
    }

    // Find target path for file to be moved to, based on the file extension and the folder associated by that extension
    public static string FindTargetPathForExtension(string extension, List<FolderNode> nodesToSearch)
    {
        if (string.IsNullOrEmpty(extension) || nodesToSearch == null)
        {
            return null;
        }

        foreach (var node in nodesToSearch)
        {
            if (node.associatedExtensions != null && node.associatedExtensions.Contains(extension))
            {
                return node.path; 
            }

            if (node.children != null && node.children.Count > 0)
            {
                // Recursively search for target path
                string pathInChildren = FindTargetPathForExtension(extension, node.children);
                if (pathInChildren != null)
                {
                    return pathInChildren;
                }
            }
        }

        return null; 
    }

    // Deep copy method of copying the immutable saved and default presets for the folder structure. 
    public static FolderNode CloneFolderNode(FolderNode originalNode)
    {
        if (originalNode == null)
        {
            return null;
        }

        var copyNode = new FolderNode();
        copyNode.displayName = originalNode.displayName;
        copyNode.path = originalNode.path;

        if(originalNode.associatedExtensions == null)
        {
            copyNode.associatedExtensions = new List<string>();
        }
        else
        {
            copyNode.associatedExtensions = new List<string>(originalNode.associatedExtensions);

        }

        if(originalNode.children != null && originalNode.children.Count > 0)
        {
            foreach (var child in originalNode.children)
            {
                copyNode.children.Add(CloneFolderNode(child));
            }
        }

        return copyNode;
    }

    // Search for the parent of a folder, used for deletion of nodes.
    public static FolderNode FindParentNode (List<FolderNode> nodesToSearch, FolderNode childNode)
    {
        foreach( var potentialParent in nodesToSearch)
        {
            if (potentialParent != null)
            {
                Debug.Log($"Checking if '{potentialParent.displayName}' children list contains '{childNode.displayName}'. Result: {potentialParent.children.Contains(childNode)}");

                // Check if node exists as child of the potential parent node
                if (potentialParent.children.Contains(childNode))
                {
                    return potentialParent;
                }

                // If the potential parent doesnt contain the child, check the potential parent for children
                else if (potentialParent.children.Count > 0)
                {
                    // Recursively search the children, or "subparents" of the potential parent 
                    var potentialSubParent = FindParentNode(potentialParent.children, childNode);
                    if (potentialSubParent != null)
                    {
                        return potentialSubParent;
                    }
                }
            }
          
        }
        return null;

    }

    public static FolderNode IsExtensionAlreadyInUse(List<FolderNode> foldersToSearch,FolderNode folderToExclude, string extension)
    {
        foreach(var folder in foldersToSearch)
        {
            if (folder == null)
            {
                continue; 
            }

            if (folder == folderToExclude)
            {
                continue;
            }

            if (folder.associatedExtensions != null && folder.associatedExtensions.Contains(extension))
            {
                return folder;
            }
            else
            {
                if (folder.children != null && folder.children.Count > 0)
                {
                    var conflictingFolder = IsExtensionAlreadyInUse(folder.children, folderToExclude, extension);

                    if (conflictingFolder != null)
                    {
                        return conflictingFolder;
                    }
                }

            }
        }
        return null;
    }


   
}

