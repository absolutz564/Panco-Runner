using System.Collections;
using UnityEngine;
using UnityEngine.UI; // se usar TextMeshPro, troque para TMPro e TMP_Text
using TMPro;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    [Header("Configuração de vidas")]
    public int maxLives = 3;
    public float hitStopDuration = 1.0f;      // tempo parado ao levar hit

    [Header("UI - Imagens de vida (3 vidas)")]
    public Image[] lifeImages;                 // 3 imagens na ordem: vida 1, 2, 3
    public Sprite lifeFullSprite;              // sprite quando a vida está ativa
    public Sprite lifeEmptySprite;             // sprite quando a vida foi perdida

    [Header("Countdown inicial")]
    public float startCountdown = 3f;         // segundos antes de começar
    public TMP_Text countdownText;                // UI Text do countdown (ou TMP_Text)
    public GameObject countdownRoot;

    [Header("Referências")]
    public GameObject gameOverPanel;          // painel/objeto de Game Over
    public PlayerController playerController; // script que move o player
    public Animator playerAnimator;           // Animator do personagem
    private int currentLives;
    private bool isGameOver;
    private bool isHitRecovering;
    private bool gameStarted;

    [Header("Power / Invencibilidade")]
    public int itemsPerPower = 10;          // quantos itens por power
    public float powerDuration = 5f;        // segundos de invencibilidade
    public Renderer playerRenderer;         // renderer do personagem (SkinnedMeshRenderer ou MeshRenderer)
    public string powerShaderProperty = "_PravaletPower"; // referência da propriedade no ShaderGraph

    private int collectedSinceLastPower = 0;
    private bool isInvulnerable = false;
    private Coroutine powerRoutine;

    [Header("Pontuação")]
    public int pointsPerMeter = 10;           // pontos por metro percorrido
    public int pointsPerItem = 25;             // pontos por item coletado
    public TMP_Text gameOverScoreText;         // TextMeshPro na tela de Game Over

    private int totalScore;
    private float totalDistanceMeters;
    private Vector3 lastPlayerPosition;

    public string hitClipName = "StumbleBackWards"; // nome EXATO do clip (Motion), não do estado
    private float hitClipLength = 0.8f;             // valor padrão qualquer
    void Start()
    {
        if (playerAnimator != null)
        {
            foreach (var clip in playerAnimator.runtimeAnimatorController.animationClips)
            {
                if (clip.name == hitClipName)
                {
                    hitClipLength = clip.length;
                    break;
                }
            }
        }
        currentLives = maxLives;
        UpdateLifeImages();
        isGameOver = false;
        isHitRecovering = false;
        gameStarted = false;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        // Garante que o player NÃO se mova durante o countdown
        if (playerController != null)
            playerController.enabled = false;

        // Garante que fique em Idle (ajuste para o que seu Animator usa)
        if (playerAnimator != null)
        {
            playerAnimator.SetBool("Running", false);
            // Se usar PosY/PosX para Idle/Running, você pode setar aqui também:
            // playerAnimator.SetFloat("PosY", 0f);
        }

        StartCoroutine(StartCountdownRoutine());
        totalScore = 0;
        totalDistanceMeters = 0f;
        if (playerController != null)
            lastPlayerPosition = playerController.transform.position;
    }

    void Update()
    {
        if (!gameStarted || isGameOver || playerController == null) return;

        Vector3 pos = playerController.transform.position;
        float deltaZ = pos.z - lastPlayerPosition.z;
        if (deltaZ > 0f)
        {
            totalDistanceMeters += deltaZ;
            totalScore += Mathf.FloorToInt(deltaZ * pointsPerMeter);
        }
        lastPlayerPosition = pos;
    }

    IEnumerator StartCountdownRoutine()
    {
        float timeLeft = startCountdown;

        // garante que o pai esteja visível durante a contagem
        if (countdownRoot != null)
            countdownRoot.SetActive(true);

        while (timeLeft > 0f)
        {
            if (countdownText != null)
                countdownText.text = Mathf.Ceil(timeLeft).ToString("0");

            timeLeft -= Time.deltaTime;
            yield return null;
        }

        // esconde tudo depois da contagem
        if (countdownRoot != null)
            countdownRoot.SetActive(false);

        gameStarted = true;

        if (playerController != null)
            playerController.enabled = true;

        if (playerAnimator != null)
            playerAnimator.SetBool("Running", true);
    }

    /// <summary>
    /// Chame quando o jogador colidir com um obstáculo.
    /// </summary>
    public void OnPlayerHit()
    {
        if (!gameStarted)               // ignora hits durante o countdown
            return;

    // NOVO: ignora hits durante power
        if (isInvulnerable)
            return;

        if (isGameOver || isHitRecovering)
            return;

        currentLives--;
        UpdateLifeImages();

        // Animação de Hit
        if (playerAnimator != null)
            playerAnimator.SetTrigger("Hit");

        // Pausa movimento
        if (playerController != null)
            playerController.enabled = false;

        if (currentLives <= 0)
        {
            isGameOver = true;
            StartCoroutine(GameOverRoutine());
        }
    }

    private void UpdateLifeImages()
    {
        if (lifeImages == null || lifeImages.Length == 0) return;

        for (int i = 0; i < lifeImages.Length; i++)
        {
            if (lifeImages[i] == null) continue;
            lifeImages[i].sprite = i < currentLives ? lifeFullSprite : lifeEmptySprite;
        }
    }
    
    public void OnHitAnimationEnd()
    {
        if (!isGameOver && playerController != null)
            playerController.enabled = true;

        isHitRecovering = false;
    }

    private IEnumerator GameOverRoutine()
    {
        yield return new WaitForSeconds(hitStopDuration);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        if (gameOverScoreText != null)
            gameOverScoreText.text = totalScore.ToString("N0");  // "N0" = número com separador de milhar, ex: 1.250
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Game");
    }

    public void GoToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Menu");
    }

    public void OnItemCollected()
    {
        totalScore += pointsPerItem;

        collectedSinceLastPower++;

        if (collectedSinceLastPower >= itemsPerPower)
        {
            collectedSinceLastPower = 0;
            ActivatePower();
        }
    }

    private void ActivatePower()
    {
        if (powerRoutine != null)
            StopCoroutine(powerRoutine);

        powerRoutine = StartCoroutine(PowerRoutine());
    }

    private IEnumerator PowerRoutine()
    {
        isInvulnerable = true;
        SetPowerShader(1f);   // liga o efeito no shader

        yield return new WaitForSeconds(powerDuration);

        isInvulnerable = false;
        SetPowerShader(0f);   // desliga o efeito
    }

    private void SetPowerShader(float value)
    {
        if (playerRenderer != null)
        {
            playerRenderer.material.SetFloat(powerShaderProperty, value);
        }
    }
}