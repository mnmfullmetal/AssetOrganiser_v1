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
        CreateDefaultFolders();
    }

    // create folder structure for user to adhere to
    private static void CreateDefaultFolders()
    {
        if (!Directory.Exists("Assets/Images/"))
        {
            Directory.CreateDirectory("Assets/Images/");
        }
        if (!Directory.Exists("Assets/Materials/"))
        {
            Directory.CreateDirectory("Assets/Materials/");
        }
        if (!Directory.Exists("Assets/Prefabs/"))
        {
            Directory.CreateDirectory("Assets/Prefabs/");
        }
        if (!Directory.Exists("Assets/Models/"))
        {
            Directory.CreateDirectory("Assets/Models/");
        }
        if (!Directory.Exists("Assets/Audio/"))
        {
            Directory.CreateDirectory("Assets/Audio/");
        }
        if (!Directory.Exists("Assets/Scripts/"))
        {
            Directory.CreateDirectory("Assets/Scripts/");
        }
    }
}
