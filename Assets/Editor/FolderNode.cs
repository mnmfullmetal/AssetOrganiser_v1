using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class FolderNode 
{
    public string DisplayName { get; set; }
    public string Path { get; set; }

    public List<FolderNode> Children { get; set; } = new List<FolderNode>();

    public List<string> AssociatedExtensions { get; set; } = new List<string>();

}
