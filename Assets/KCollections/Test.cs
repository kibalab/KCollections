
using K13A.Collections.Generic;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Test : UdonSharpBehaviour
{
    public KDictionary dic;
    public KStack stack;
    public KList list;
    public KQueue queue;

    private void Start()
    {
        dic.Add("TestKey1", "TestValue1");
        dic.Add("TestKey2", "TestValue2");
        dic.RemoveWithKey("TestKey1");
        dic.Add("TestKey3", "TestValue3");

        Debug.Log(dic.Count());

        Debug.Log((string)dic.GetValue("TestKey3"));
    }
}
