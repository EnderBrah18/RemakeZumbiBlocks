using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EnemyGroup
{
    public Enemy leader;
    public List<Enemy> members = new List<Enemy>();
    public GroupPersonality personality;
    public Vector3 groupCenter;

    int attackerA = -1;
    int attackerB = -1;
    private float nextAttackSwitch;
    private float attackInterval = 2f;

    float nextTacticUpdate;
    float tacticRate = 0.3f;

    float rotationOffset;

    public EnemyGroup(Enemy leader)
    {
        this.leader = leader;
        this.members.Add(leader);

        // Define uma personalidade aleatória ao criar o grupo
        // 70% tático (Defensive/Supportive), 30% agressivo (Aggressive)
        // float roll = Random.value;
        // if (roll < 0.4f) personality = GroupPersonality.Aggressive;
        // if (roll < 0.8f) personality = GroupPersonality.Defensive;
        // personality = GroupPersonality.Supportive;

        personality = GroupPersonality.Aggressive; // Para testes, deixe fixo como agressivo

        Debug.Log($"Grupo criado por {leader.name} com personalidade: {personality}");
    }

    public void UpdateGroup()
    {
        members.RemoveAll(m => m == null || m.currentHealth <= 0);

        if (leader != null)
        {
            foreach (var m in members)
            {
                // O líder compartilha o DESTINO estratégico
                m.navigationTarget = leader.navigationTarget;

                // Mas o SOLDIER ainda decide o seu próprio combatTarget no seu próprio Update
            }
        }

        // O líder decide o foco estratégico
        if (leader != null && leader.targetPlayer != null)
        {
            foreach (var m in members)
            {
                if (m == null || m == leader) continue;

                // Soldados herdam o alvo do líder para manter coesão
                m.targetPlayer = leader.targetPlayer;
                m.isChasing = true;
            }
        }

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

        // Distâncias base para a formação
        float attackDistance = 2.2f + Random.Range(-0.4f, 0.5f);
        float ringDistance = attackDistance * 1.5f;

        // Gerenciamento de atacantes designados
        if (Time.time > nextAttackSwitch)
        {
            attackerA = Random.Range(0, members.Count);
            attackerB = Random.Range(0, members.Count);

            if (attackerB == attackerA && members.Count > 1)
                attackerB = (attackerA + 1) % members.Count;

            nextAttackSwitch = Time.time + attackInterval;
        }

        for (int i = 0; i < members.Count; i++)
        {
            if (members[i] == null) continue;

            // Cálculo do ângulo para espalhar os inimigos em volta do player
            float angle = i * (360f / members.Count) + rotationOffset;

            Vector3 offset = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0,
                                        Mathf.Sin(angle * Mathf.Deg2Rad));

            offset += Random.insideUnitSphere * 0.25f;
            offset.y = 0;

            Vector3 targetPos;

            // --- LÓGICA DE POSICIONAMENTO ---
            // 1. ATACANTES DESIGNADOS: Tentam fechar a distância
            if (i == attackerA || i == attackerB)
            {
                targetPos = player.position + offset * (attackDistance * 0.5f);
            }
            // 2. PRESSURE: Mantêm-se no limite do range de ataque
            else if (i % 2 == 0)
            {
                targetPos = player.position + offset * attackDistance;
            }
            // 3. FLANKERS: Criam o cerco externo
            else
            {
                targetPos = player.position + offset * ringDistance;
            }

            // Aplica um pequeno offset individual para evitar sobreposição perfeita
            float individualSpacing = (i * 0.15f);
            targetPos += offset * individualSpacing;

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
        // Limpa referências nulas ou inimigos que estão marcados como mortos
        members.RemoveAll(m => m == null || m.currentHealth <= 0);

        if (members.Count > 0)
        {
            leader = members[0];
            leader.role = SocialRole.Leader;

            // Opcional: Avisa o novo líder quem é o alvo para ele não perder o foco
            leader.isChasing = true;

            Debug.Log($"<color=cyan>SUCESSÃO:</color> O líder morreu. {leader.name} é o novo mestre do grupo.");
        }
        else
        {
            leader = null;
            Debug.Log("<color=red>GRUPO EXTINTO:</color> Todos os membros morreram.");
        }
    }

    public void MergeInto(EnemyGroup largerGroup)
    {
        foreach (var member in members)
        {
            largerGroup.members.Add(member);
            member.currentGroup = largerGroup;
            member.role = SocialRole.Soldier;
        }
        members.Clear();
    }
}