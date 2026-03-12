using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EnemyGroup
{
    public Enemy leader;
    public List<Enemy> members = new List<Enemy>();
    public GroupPersonality personality { get; private set; }

    public GroupIntent currentIntent = GroupIntent.MarchToCore;

    public Vector3 groupCenter;

    int attackerA = -1;
    int attackerB = -1;
    private float nextAttackSwitch;
    private float attackInterval = 2f;

    float nextTacticUpdate;
    float tacticRate = 0.3f;

    float rotationOffset;
    public bool allowNewMembers = true;

    public EnemyGroup(Enemy leader)
    {
        this.leader = leader;
        this.members.Add(leader);

        // Define uma personalidade aleatória ao criar o grupo
        // 70% tático (Defensive/Supportive), 30% agressivo (Aggressive)
        float roll = Random.value;
        
        personality = GroupPersonality.Aggressive;

    }

    public EnemyGroup(Enemy leader, GroupPersonality personality)
    {
        this.leader = leader;
        this.members.Add(leader);
        this.personality = personality;

    }

    public void UpdateGroup()
    {
        // Limpeza de membros mortos ou nulos
        members.RemoveAll(m => m == null || m.currentHealth <= 0);

        if (leader != null)
        {
            foreach (var m in members)
            {
                if (m == null) continue;

                // Não interfere se o inimigo está focado no player
                if (m.focus == EnemyFocus.PlayerSlayer)
                    continue;

                // Não interfere se ele já está lutando
                if (m.combatTarget != null)
                    continue;

                m.navigationTarget = leader.navigationTarget;
            }
        }

        // O líder decide o foco estratégico e propaga para o grupo
        if (leader != null && leader.combatTarget != null && Time.time > nextTacticUpdate)
        {
            AlertGroup(leader.combatTarget);
        }

        // Atualização de táticas de posicionamento (slots em volta do alvo)
        if (leader != null && leader.combatTarget != null && Time.time > nextTacticUpdate)
        {
            nextTacticUpdate = Time.time + tacticRate;
            ExecuteTactics(leader.combatTarget);
        }

        // Incrementa a rotação para que o cerco não seja estático
        rotationOffset += Time.deltaTime * Random.Range(3f, 8f);
    }

    private void ExecuteTactics(Transform player)
    {
        if (player == null) return;

        // Distâncias base para a formação de cerco
        float attackDistance = 2.2f + Random.Range(-0.4f, 0.5f);
        float ringDistance = attackDistance * 1.5f;

        // Gerenciamento de atacantes designados (quem realmente fecha a distância)
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

            // Cálculo do ângulo para espalhar os inimigos em volta do player (360 graus)
            float angle = i * (360f / members.Count) + rotationOffset;

            Vector3 offset = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0,
                                        Mathf.Sin(angle * Mathf.Deg2Rad));

            offset += Random.insideUnitSphere * 0.25f;
            offset.y = 0;

            Vector3 targetPos;

            // --- LÓGICA DE POSICIONAMENTO ---
            // 1. ATACANTES DESIGNADOS: Tentam fechar a distância para golpear
            if (i == attackerA || i == attackerB)
            {
                targetPos = player.position + offset * (attackDistance * 0.5f);
            }
            // 2. PRESSURE: Mantêm-se no limite do range de ataque para cercar
            else if (i % 2 == 0)
            {
                targetPos = player.position + offset * attackDistance;
            }
            // 3. FLANKERS: Criam o cerco externo para evitar fuga
            else
            {
                targetPos = player.position + offset * ringDistance;
            }

            // Aplica um pequeno offset individual para evitar sobreposição perfeita (Z-fighting de IA)
            float individualSpacing = (i * 0.15f);
            targetPos += offset * individualSpacing;

            members[i].SetGroupSlot(targetPos);
        }
    }

    public void AlertGroup(Transform target)
    {
        Debug.Log($"<color=orange>Líder {leader.name} recebeu alerta sobre {target.name}</color> "   );

        foreach (var member in members)
        {
            if (member == null) continue;
            member.ReceiveGroupAlert(target);
        }
    }

    public void TransferLeadership()
    {
        members.RemoveAll(m => m == null || m.currentHealth <= 0);

        if (members.Count > 0)
        {
            // O primeiro membro da lista assume o posto
            leader = members[0];
            leader.role = SocialRole.Leader;

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
        if (!allowNewMembers)
            return;

        foreach (var member in members)
        {
            largerGroup.members.Add(member);
            member.currentGroup = largerGroup;
            member.role = SocialRole.Soldier;
        }
        members.Clear();
    }
}