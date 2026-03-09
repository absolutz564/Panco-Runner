// PlayerController.cs (resumido)
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public Animator animator;
    public float laneWidth = 2f;
    public float laneChangeSpeed = 10f;
    public float runSpeed = 10f;

    public float maxRunSpeed = 25f;      // velocidade máxima
    public float acceleration = 0.5f;    // quanto aumenta por segundo

    private int currentLane = 1;      // 0 = esq, 1 = centro, 2 = dir
    private float targetX;
    private bool isJumping, isSliding;
    public float jumpHeight = 3f;      // altura do pulo
    public float jumpDuration = 0.6f;  // tempo total do pulo

    private float jumpTimer;
    private float groundY;
    public float baseRunSpeed = 6f;   // velocidade em que a animação parece correta

    // === Mobile: controle por swipe ===
    private Vector2 touchStartPos;
    private bool touchStarted;
    [Tooltip("Distância mínima (em pixels) para considerar um swipe")]
    public float minSwipeDistance = 50f;

    void Start()
    {
        groundY = transform.position.y;
    }
    
    void Update()
    {
        // Aumenta a velocidade com o tempo até o máximo
        runSpeed += acceleration * Time.deltaTime;
        runSpeed = Mathf.Clamp(runSpeed, 0f, maxRunSpeed);
        float speedFactor = runSpeed / baseRunSpeed;
        animator.SetFloat("Speed", speedFactor);
        // Movimento para frente (mundo ou em relação à câmera)
        transform.Translate(0, 0, runSpeed * Time.deltaTime);

        // Input: esquerda/direita
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            MoveLane(-1);
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            MoveLane(1);

        // Jump / Slide (triggers no Animator)
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            Jump();
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            Slide();

                    // === Mobile: swipe na tela ===
        HandleTouchInput();

        // Atualiza posição X (suave)
        float newX = Mathf.MoveTowards(transform.position.x, targetX, laneChangeSpeed * Time.deltaTime);

        // ======= LÓGICA DO PULO (Y) =======
        float newY = transform.position.y;

        if (isJumping)
        {
            jumpTimer += Time.deltaTime;
            float t = Mathf.Clamp01(jumpTimer / jumpDuration);

            // curva parabólica simples: 0 -> altura -> 0
            float yOffset = 4f * jumpHeight * t * (1f - t);
            newY = groundY + yOffset;

            if (t >= 1f)
            {
                isJumping = false;
                newY = groundY;  // garante que volta pro chão
            }
        }
        else
        {
            newY = groundY;
        }
        // ======= FIM LÓGICA DO PULO =======

        // aplica nova posição
        transform.position = new Vector3(newX, newY, transform.position.z);

        // ======= LÓGICA DA ANIMAÇÃO DIAGONAL =======
        // diferença entre onde estou e onde quero chegar na faixa
        float xDiff = targetX - newX;

        float posXParam = 0f;
        if (Mathf.Abs(xDiff) > 0.01f)
        {
            // ainda está deslizando entre faixas -> anima diagonal
            posXParam = Mathf.Sign(xDiff);   // -1 indo pra esquerda, +1 pra direita
        }
        // ======= FIM LÓGICA DIAGONAL =======

        // Animator: PosX só ≠ 0 enquanto troca de faixa, PosY = 1 quando correndo
        animator.SetFloat("PosX", posXParam);
        animator.SetFloat("PosY", 1f);
        animator.SetBool("Running", true);
    }

    void HandleTouchInput()
    {
        if (Input.touchCount == 0)
        {
            touchStarted = false;
            return;
        }

        Touch touch = Input.GetTouch(0);

        switch (touch.phase)
        {
            case TouchPhase.Began:
                touchStartPos = touch.position;
                touchStarted = true;
                break;

            case TouchPhase.Ended:
                if (!touchStarted) break;

                Vector2 delta = touch.position - touchStartPos;
                float dist = delta.magnitude;

                if (dist < minSwipeDistance) break;

                // Swipe mais horizontal -> muda faixa
                if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                {
                    if (delta.x > 0) MoveLane(1);
                    else MoveLane(-1);
                }
                else
                {
                    // Swipe mais vertical -> pulo ou slide
                    if (delta.y > 0) Jump();
                    // else Slide();
                }

                touchStarted = false;
                break;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Obstacle"))
        {
            // avisa o GameController que o jogador levou um hit
            FindObjectOfType<GameController>().OnPlayerHit();

            // desativa o collider do obstáculo para não ter nova colisão
            Collider obstacleCollider = other.GetComponent<Collider>();
            if (obstacleCollider != null)
            {
                obstacleCollider.enabled = false;
            }
        }
    }

    void MoveLane(int direction)
    {
        currentLane = Mathf.Clamp(currentLane + direction, 0, 2);
        targetX = (currentLane - 1) * laneWidth;
    }

    void Jump()
    {
        if (isJumping) return;

        isJumping = true;
        jumpTimer = 0f;

        animator.SetTrigger("Jump");
    }

    void Slide()
    {
        if (isJumping) return;
        animator.SetTrigger("Slide"); // ou SetBool("Slide", true)
        isSliding = true;
    }

    // Chamar quando colidir com obstáculo
    public void OnHit()
    {
        animator.SetTrigger("Hit");
    }

    public void OnVictory()
    {
        animator.SetTrigger("Victory");
    }
}