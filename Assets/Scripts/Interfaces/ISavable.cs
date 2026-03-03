using UnityEngine;

public interface ISavable
{
    string SaveData();     // retorna JSON específico do objeto
    void LoadData(string json);  // carrega JSON específico do objeto
    string GetSaveKey();   // chave única para identificar o objeto no save
}
