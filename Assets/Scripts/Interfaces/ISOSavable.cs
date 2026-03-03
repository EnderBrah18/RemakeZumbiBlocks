using UnityEngine;

public interface ISOSavable
{
    string GetSaveKey();       // Ex: "Vendor_LojaDoBob"
    string SaveData();         // JSON string
    void LoadData(string json);
}
