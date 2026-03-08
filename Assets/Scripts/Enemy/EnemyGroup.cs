using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EnemyGroup
{
    public Enemy leader;
    public List<Enemy> members = new List<Enemy>();
    public Vector3 groupCenter;

    int attackerA = -1;
    int attackerB = -1;
    private float nextAttackSwitch;
    private float attackInterval = 2f;

    float nextTacticUpdate;
    float tacticRate = 0.3f;

    float rotationOffset;

    public void UpdateGroup()
    {
        members.RemoveAll(m => m == null);
        if (members.Count == 0) return;

        if (leader == null) TransferLeadership();

        // NOVIDADE: Se o líder não tem alvo, procura se algum membro tem
        if (leader.targetPlayer == null)
        {
            foreach (var m in members)
            {
                if (m.targetPlayer != null)
                {
                    leader.targetPlayer = m.targetPlayer;
                    leader.isChasing = true;
                    break;
                }
            }
        }

        // Sempre garante broadcast imediato do target do líder para os membros,
        // evitando dependência do tick de tática para atribuir aggro.
        if (leader != null && leader.targetPlayer != null)
        {
            foreach (var m in members)
            {
                if (m == null) continue;
                m.targetPlayer = leader.targetPlayer;
                m.isChasing = true;
            }
        }

        // Executa a atualização das posições táticas com a taxa configurada
        if (leader != null && leader.targetPlayer != null && Time.time > nextTacticUpdate)
        {
            nextTacticUpdate = Time.time + tacticRate;
            ExecuteTactics(leader.targetPlayer);
        }

        rotationOffset += Time.deltaTime * Random.Range(3f, 8f);
    }

    private void ExecuteTactics(Transform player)
    {
        if (player == null) return;

        float attackDistance = 2.2f + Random.Range(-0.4f, 0.5f);
        float ringDistance = attackDistance * 1.2f;

        int attackers = Mathf.Min(2, members.Count);

        // escolhe novos atacantes
        if (Time.time > nextAttackSwitch)
        {
            attackerA = Random.Range(0, members.Count);
            attackerB = Random.Range(0, members.Count);

            if (attackerB == attackerA)
                attackerB = (attackerA + 1) % members.Count;

            nextAttackSwitch = Time.time + attackInterval;
        }

        for (int i = 0; i < members.Count; i++)
        {
            if (members[i] == null) continue;

            float angle = i * (360f / members.Count) + rotationOffset;

            Vector3 offset =
                new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0,
                            Mathf.Sin(angle * Mathf.Deg2Rad));

            offset += Random.insideUnitSphere * 0.25f;
            offset.y = 0;

            Vector3 targetPos;

            // ATACANTES
            if (i == attackerA || i == attackerB)
            {
                targetPos = player.position + offset * (attackDistance * 0.5f);
            }

            // PRESSURE (perto do jogador)
            else if (i % 2 == 0)
            {
                targetPos = player.position + offset * attackDistance;
            }

            // FLANKERS (mais afastados)
            else
            {
                targetPos = player.position + offset * ringDistance;
            }

            members[i].SetGroupSlot(targetPos);
        }
    }

    public void AlertGroup(Transform player)
    {
        foreach (var m in members)
        {
            if (m == null) continue;

            m.targetPlayer = player;
            m.isChasing = true;
        }

        if (leader != null)
            leader.targetPlayer = player;
    }

    public void TransferLeadership()
    {
        if (members.Count > 0)
        {
            leader = members[0];
            leader.role = SocialRole.Leader;
        }
    }

    // Método para fundir este grupo em outro maior
    public void MergeInto(EnemyGroup largerGroup)
    {

        Debug.Log($"<color=red>FUSÃO:</color> Grupo menor absorvido por {largerGroup.leader.name}. Novo tamanho: {largerGroup.members.Count}");

        foreach (var member in members)
        {
            largerGroup.members.Add(member);
            member.currentGroup = largerGroup;
            member.role = SocialRole.Soldier; // Ex-líderes viram soldados
        }
        members.Clear();
    }
}