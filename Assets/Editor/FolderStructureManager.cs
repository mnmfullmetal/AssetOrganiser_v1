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
       
    }

    public static List<FolderNode> DefaultFolderStructure { get; private set; } = new List<FolderNode>()
    {
        new FolderNode { DisplayName = "Assets", Path = "Assets/", Children = new List<FolderNode>()
        {
              new FolderNode { DisplayName = "Art", Path = "Assets/Art/",  Children = new List<FolderNode>()
              {
                  new FolderNode { DisplayName = "Materials", Path = "Assets/Art/Materials/" },
                  new FolderNode { DisplayName = "Models", Path = "Assets/Art/Models/" },
                  new FolderNode { DisplayName = "Textures", Path = "Assets/Art/Textures/" }
              }},
              new FolderNode { DisplayName = "Audio", Path = "Assets/Audio/", Children= new List<FolderNode>()
              {
                  new FolderNode {DisplayName ="Music", Path = "Assets/Audio/Music"},
                  new FolderNode {DisplayName ="Sound", Path = "Assets/Audio/Sound"}
              }},
              new FolderNode { DisplayName = "Code", Path = "Assets/Code/" , Children = new List < FolderNode >()
              {
                  new FolderNode {DisplayName ="Scripts", Path = "Assets/Code/Scripts"},
                  new FolderNode {DisplayName = "Shaders", Path = "Assets/Code/Shaders"}
              }},
              new FolderNode { DisplayName = "Docs", Path = "Assets/Docs/" },
              new FolderNode { DisplayName = "Level", Path = "Assets/Level/", Children = new List < FolderNode >()
              {
                  new FolderNode {DisplayName = "Prefabs", Path = "Assets/Level/Prefabs"},
                  new FolderNode {DisplayName = "Scenes", Path = "Assets/Level/Scenes"},
                  new FolderNode {DisplayName = "UI", Path = "Assets/Level/UI"},

              }}
        }
       }
    };



}

//assets - Art, Audio, Code, Docs, Level