using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SerializationWrapper
{
    [System.Serializable]
    public struct KeyValue
    {
        public string key;
        public string value;
    }

    public List<KeyValue> items = new List<KeyValue>();

    public SerializationWrapper(Dictionary<string, string> dict)
    {
        items = new List<KeyValue>();
        foreach (var kvp in dict)
        {
            items.Add(new KeyValue { key = kvp.Key, value = kvp.Value });
        }
    }

    public Dictionary<string, string> ToDictionary()
    {
        var dict = new Dictionary<string, string>();
        foreach (var kv in items)
        {
            dict[kv.key] = kv.value;
        }
        return dict;
    }
}