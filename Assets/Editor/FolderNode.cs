using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class FolderNode 
{
    public string DisplayName;
    public string Path;
    public List<FolderNode> Children = new List<FolderNode>();
    public List<string> AssociatedExtensions = new List<string>();
}
