using UnityEngine;
using System.Collections;

[RequireComponent(typeof(UniqueID))]
public class PlayerTransformSavable : SavableEntity
{
    [System.Serializable]
    public class PlayerTransformData
    {
        public string SaveKey => "PlayerTransform";
        public float px, py, pz;
        public float qx, qy, qz, qw;
    }

    public override string SaveData()
    {
        var d = new PlayerTransformData();
        var p = transform.position;
        var q = transform.rotation;
        d.px = p.x; d.py = p.y; d.pz = p.z;
        d.qx = q.x; d.qy = q.y; d.qz = q.z; d.qw = q.w;
        return JsonUtility.ToJson(d);
    }

    public override void LoadData(string json)
    {
        Debug.Log($"[LoadData] {GetSaveKey()} -> {json}");
        if (string.IsNullOrEmpty(json)) return;
        var d = JsonUtility.FromJson<PlayerTransformData>(json);

        // Positional restore seguro:
        var targetPos = new Vector3(d.px, d.py, d.pz);
        var targetRot = new Quaternion(d.qx, d.qy, d.qz, d.qw);

        // Se tiver CharacterController, desativa antes de setar
        var cc = GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
            transform.SetPositionAndRotation(targetPos, targetRot);
            cc.enabled = true;
            return;
        }

        // Se tiver Rigidbody, torne kinematic temporariamente
        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            bool wasKinematic = rb.isKinematic;
            rb.isKinematic = true;
            transform.SetPositionAndRotation(targetPos, targetRot);
            rb.isKinematic = wasKinematic;
            return;
        }

        // Caso normal
        transform.SetPositionAndRotation(targetPos, targetRot);
    }

    private IEnumerator Start()
    {
        // espera até SaveSystem existir (ou 1 frame)
        while (SaveSystem.Instance == null)
            yield return null;

        Debug.Log($"[PTS] Registering after SaveSystem exists: {GetSaveKey()}");
        SaveSystem.Instance.RegisterSavable(this);
    }

    private void OnDisable()
    {
        if (SaveSystem.Instance != null) SaveSystem.Instance.UnregisterSavable(this);
    }
}
