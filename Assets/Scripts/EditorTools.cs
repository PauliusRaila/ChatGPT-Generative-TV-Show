using UnityEngine;
using UnityEditor;
using System.Collections.Generic;


public class EditorTools : Editor
{
    [MenuItem("Assets/Utilities/Remesh Skin")]
    static void RemeshSkin()
    {
        Selection.activeGameObject.transform.GetComponent<SkinnedMeshRenderer>().ResetBounds();
        Selection.activeGameObject.transform.GetComponent<SkinnedMeshRenderer>().bones = BuildBonesArray(Selection.activeGameObject.transform.GetComponent<SkinnedMeshRenderer>().rootBone, Selection.activeGameObject.transform.GetComponent<SkinnedMeshRenderer>().bones);
    }

    static Transform[] BuildBonesArray(Transform rootBone, Transform[] bones)
    {
        List<Transform> boneList = new List<Transform>();
        ExtractBonesRecursively(rootBone, ref boneList);

        List<Transform> Reorder = new List<Transform>();
        foreach(Transform bone in bones)
        {
            foreach(Transform extractbone in boneList)
            {
                if(bone.name == extractbone.name)
                {
                    Reorder.Add(extractbone);
                }
            }

        }

        return Reorder.ToArray();
    }

    static void ExtractBonesRecursively(Transform bone, ref List<Transform> boneList)
    {
        boneList.Add(bone);

        for (int i = 0; i < bone.childCount; i++)
        {
            ExtractBonesRecursively(bone.GetChild(i), ref boneList);
        }
    }

}





