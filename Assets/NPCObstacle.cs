using UnityEngine;

public class NPCObstacle : MonoBehaviour
{
    public float triggerDistanceZ = 8f;          // distância em Z para disparar o ataque
    public string attackTriggerName = "Skid";    // nome do Trigger no Animator
    private float disableColliderDelay = 1f;      // tempo após o ataque para desligar o collider

    private Transform player;
    private Animator animator;
    private Collider npcCollider;
    private bool attackTriggered = false;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        animator = GetComponent<Animator>();
        npcCollider = GetComponent<Collider>();  // pode ser Box, Capsule, etc.
    }

    void Update()
    {
        if (attackTriggered || player == null || animator == null)
            return;

        // Considerando que o NPC está à frente e o player vem de trás no eixo Z
        float dz = transform.position.z - player.position.z;

        if (dz <= triggerDistanceZ && dz >= 0f)
        {
            animator.SetTrigger(attackTriggerName);
            attackTriggered = true;

            // agenda para desligar o collider depois de 2s (ou o valor de disableColliderDelay)
            if (npcCollider != null)
                Invoke(nameof(DisableNpcCollider), disableColliderDelay);
        }
    }

    void DisableNpcCollider()
    {
        if (npcCollider != null)
            npcCollider.enabled = false;
    }
}