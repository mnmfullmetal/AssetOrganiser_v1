using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FolderNode 
{
    public string DisplayName { get; set; }
    public string Path { get; set; }

    public List<FolderNode> Children { get; set; }

    public List<string> AssociatedExtensions { get; set; } = new List<string>();

}
