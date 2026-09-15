using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// The primary responsibilities of the ModuleManager class are to:
// Initialize block modules, store them in a dictionary for easy access, and provide methods 
// to retrieve these modules based on specific the selected module names provided 
// from the activated block corner target vertices (update procedure).
[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/ModuleManager", order = 1)]
public class ModuleManager : ScriptableObject
{
    public GameObject moduleImportObject;
    private Dictionary<string, Mesh> blockModulesMeshes = new Dictionary<string, Mesh>();

    private void Awake()
    {
        InitializeBlockModules();
    }

    public void InitializeBlockModules()
    {
        foreach (Transform moduleTransform in moduleImportObject.transform)
        {
            Mesh moduleMesh = moduleTransform.GetComponent<MeshFilter>().sharedMesh;
            blockModulesMeshes.Add(moduleTransform.name, moduleMesh);
        }
    }

    public Mesh GetBlockModuleMesh(int moduleIndex) // returns a block module based on the moduleIndex (bitMask)
    {
        return blockModulesMeshes[moduleIndex.ToString()];
    }

    public Mesh GetBlockModuleMesh(string moduleName)
    {
        if (blockModulesMeshes.TryGetValue(moduleName, out Mesh mesh))
        {
            return mesh;
        }

        Debug.LogWarning($"Module '{moduleName}' not found!");
        return null;
    }

}
