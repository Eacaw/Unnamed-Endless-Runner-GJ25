using UnityEngine;
using System.Collections;
using UnityEngine.UIElements;

public class PlayerController : MonoBehaviour
{
    public Camera camera;
    public GameObject[] prefabsToInstantiate;
    public Sprite[] tutorialImages;

    private int currentLane = 1; // 0 = left, 1 = middle, 2 = right
    private float laneWidth = 2.5f; // Distance between lanes
    private Vector3 targetPosition;
    private bool isDead = false;
    private int livesRemaining = 5;

    private bool spawnCooldown = false;
    private float spawnCooldownTime = 3.0f; // seconds
    private float spawnCooldownTimer = 0f;

    // UI Elements
    private Animator animator;
    private UIDocument uiDocument;
    private Label gameOverLabel;
    private Label respawnLabel;
    public Label livesLabel;

    private float laneTurnAngle = 20f; // Maximum angle to tilt when changing lanes
    private float turnResetSpeed = 10f; // How quickly to return to forward
    private float currentTurn = 0f; // Current turn angle
    private bool isJumping = false;

    private int currentTutorialStep = 0;
    private string[] tutorialMessages =
    {
        "Move left/right between lanes",
        "Jump over obstacles on the floor",
        "Collect apples to gain points",
        "Avoid the obstacles to survive",
    };

    void Awake()
    {
        SetupUI();
        targetPosition = transform.position;
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        HandleInput();
        HandleMovement();
        HandleCamera();
        HandleSpawnCooldown();
    }

    private void SetupUI()
    {
        uiDocument = FindFirstObjectByType<UIDocument>();
        gameOverLabel = uiDocument.rootVisualElement.Q<Label>("GameOverLabel");
        respawnLabel = uiDocument.rootVisualElement.Q<Label>("RespawnLabel");
        livesLabel = uiDocument.rootVisualElement.Q<Label>("LivesLabel");
    }

    private void HandleInput()
    {
        bool laneChanged = false;
        float turnDirection = 0f;

        if (Input.GetAxis("Horizontal") < 0 && currentLane > 0 && !isDead)
        {
            currentLane--;
            UpdateTargetPosition();
            Input.ResetInputAxes();
            laneChanged = true;
            turnDirection = -1f;
            animator.SetTrigger("StrafeLeft");
        }
        else if (Input.GetAxis("Horizontal") > 0 && currentLane < 2 && !isDead)
        {
            currentLane++;
            UpdateTargetPosition();
            Input.ResetInputAxes();
            laneChanged = true;
            turnDirection = 1f;
            animator.SetTrigger("StrafeRight");
        }

        if (!isJumping && Input.GetKeyDown(KeyCode.Space) && !isDead)
        {
            StartCoroutine(JumpRoutine());
        }

        if (Input.GetKeyDown(KeyCode.R) && isDead)
        {
            MoveToEndGameScene();
        }

        UpdateTurn(laneChanged, turnDirection);
    }

    private void HandleMovement()
    {
        Vector3 newPosition = Vector3.Lerp(
            transform.position,
            targetPosition,
            Time.deltaTime * 10f
        );
        if (!isJumping)
        {
            newPosition.y = transform.position.y;
        }
        transform.position = newPosition;
    }

    private void HandleCamera()
    {
        Vector3 directionToPlayer =
            new Vector3(
                transform.position.x + (currentLane - 1) * 0.5f,
                transform.position.y,
                transform.position.z
            ) - camera.transform.position;
        directionToPlayer.x *= 0.1f;
        directionToPlayer.z *= 0.25f;
        Quaternion targetRotation = Quaternion.Slerp(
            camera.transform.rotation,
            Quaternion.LookRotation(directionToPlayer),
            0.5f
        );
        Vector3 eulerRotation = targetRotation.eulerAngles;
        eulerRotation.x = camera.transform.rotation.eulerAngles.x;
        eulerRotation.z = camera.transform.rotation.eulerAngles.z;
        camera.transform.rotation = Quaternion.Lerp(
            camera.transform.rotation,
            Quaternion.Euler(eulerRotation),
            Time.deltaTime * 5f
        );
    }

    private void HandleSpawnCooldown()
    {
        if (spawnCooldown)
        {
            spawnCooldownTimer -= Time.deltaTime;
            if (spawnCooldownTimer <= 0f)
            {
                spawnCooldown = false;
            }
        }
    }

    private void UpdateTurn(bool laneChanged, float turnDirection)
    {
        if (laneChanged)
        {
            currentTurn = laneTurnAngle * turnDirection;
        }
        else
        {
            currentTurn = Mathf.Lerp(currentTurn, 0f, Time.deltaTime * turnResetSpeed);
        }
        Quaternion targetPlayerRotation = Quaternion.Euler(0f, 0f, currentTurn);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetPlayerRotation,
            Time.deltaTime * 8f
        );
    }

    private void MoveToEndGameScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("EndGame");
    }

    private IEnumerator JumpRoutine()
    {
        isJumping = true;
        animator.SetTrigger("Jump");
        float jumpHeight = 1.8f;
        float jumpDuration = 0.5f;
        float elapsedTime = 0f;
        Vector3 startPosition = transform.position;
        Vector3 peakPosition = startPosition + Vector3.up * jumpHeight;
        while (elapsedTime < jumpDuration)
        {
            float t = Mathf.Clamp01(elapsedTime / jumpDuration);
            transform.position = Vector3.Lerp(startPosition, peakPosition, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        elapsedTime = 0f;
        while (elapsedTime < jumpDuration)
        {
            float t = Mathf.Clamp01(elapsedTime / jumpDuration);
            transform.position = Vector3.Lerp(peakPosition, startPosition, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.position = startPosition;
        animator.SetTrigger("JumpLand");
        isJumping = false;
    }

    private void UpdateTargetPosition()
    {
        targetPosition = new Vector3(
            currentLane * laneWidth - laneWidth,
            transform.position.y,
            transform.position.z
        );
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Trigger Entered: " + other.gameObject.name);
        if (!spawnCooldown && other.gameObject.CompareTag("PlatformSpawnTrigger"))
        {
            SpawnPlatform(other);
        }
        else if (other.gameObject.CompareTag("TutorialTrigger"))
        {
            HandleTutorialTrigger();
            other.gameObject.SetActive(false);
        }
        else if (other.gameObject.CompareTag("Obstacle"))
        {
            HandleDeath(other);
        }
        else if (other.gameObject.CompareTag("TutorialObstacle"))
        {
            HandleDeath(other, false);
        }
    }

    private void HandleTutorialTrigger()
    {
        Label tutorialLabel = uiDocument.rootVisualElement.Q<Label>("InstructionsLabel");
        VisualElement tutorialImageContainer = uiDocument.rootVisualElement.Q<VisualElement>(
            "InstructionsImage"
        );
        VisualElement instructionsOverlay = uiDocument.rootVisualElement.Q<VisualElement>(
            "Instructions"
        );

        if (currentTutorialStep > tutorialMessages.Length - 1)
        {
            instructionsOverlay.style.display = DisplayStyle.None;
            tutorialLabel.style.display = DisplayStyle.None;
            tutorialImageContainer.style.display = DisplayStyle.None;
        }
        else
        {
            if (currentTutorialStep <= 3)
            {
                tutorialImageContainer.style.backgroundImage = new StyleBackground(
                    tutorialImages[currentTutorialStep]
                );
            }
            else
            {
                tutorialImageContainer.style.backgroundImage = new StyleBackground();
            }
            tutorialLabel.text = tutorialMessages[currentTutorialStep];
            tutorialLabel.style.display = DisplayStyle.Flex;
            tutorialImageContainer.style.display = DisplayStyle.Flex;
        }
        currentTutorialStep++;
    }

    private void SpawnPlatform(Collider other)
    {
        GameObject prefabToInstantiate = prefabsToInstantiate[
            Random.Range(0, prefabsToInstantiate.Length)
        ];
        Instantiate(
            prefabToInstantiate,
            new Vector3(0, 0, other.gameObject.transform.parent.position.z + 64),
            Quaternion.identity
        );
        other.gameObject.SetActive(false);
        spawnCooldown = true;
        spawnCooldownTimer = spawnCooldownTime;
    }

    private void HandleDeath(Collider other, bool reduceLives = true)
    {
        isDead = true;
        Vector3 triggerPosition = other.gameObject.transform.position;
        Vector3 playerPosition = transform.position;
        if (triggerPosition.z > playerPosition.z)
        {
            animator.SetTrigger("DeathA");
        }
        else
        {
            animator.SetTrigger("DeathB");
        }
        PlatformController[] platformControllers = FindObjectsByType<PlatformController>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );
        foreach (var controller in platformControllers)
        {
            controller.speed = 0f;
        }
        if (livesRemaining > 0 && reduceLives)
        {
            livesRemaining--;
            livesLabel.text = "Lives: " + livesRemaining.ToString();
            respawnLabel.style.display = DisplayStyle.Flex;
            StartCoroutine(Respawn(other));
        }
        else if (livesRemaining > 0 && !reduceLives)
        {
            respawnLabel.text = "Try to avoid obstacles!";
            respawnLabel.style.display = DisplayStyle.Flex;
            StartCoroutine(Respawn(other));
        }
        else
        {
            GameOver();
        }
    }

    private IEnumerator Respawn(Collider other)
    {
        respawnLabel.style.display = DisplayStyle.Flex;
        respawnLabel.text = "Respawning in 3...";
        yield return new WaitForSeconds(1f);
        respawnLabel.text = "Respawning in 2...";
        yield return new WaitForSeconds(1f);
        respawnLabel.text = "Respawning in 1...";
        yield return new WaitForSeconds(1f);
        respawnLabel.style.display = DisplayStyle.None;

        PlatformController[] platformControllers = FindObjectsByType<PlatformController>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );
        float respawnOffset = 20f;
        foreach (var controller in platformControllers)
        {
            controller.transform.parent.position += new Vector3(0, 0, respawnOffset);
        }
        int desiredLane = 1;
        int respawnRow = 0;
        bool laneSet = false;
        foreach (var controller in platformControllers)
        {
            if (Mathf.Abs(controller.transform.position.z - transform.position.z) < 10f)
            {
                bool[,] obstacles = controller.obstaclePositions;
                if (!obstacles[respawnRow, desiredLane])
                {
                    currentLane = desiredLane;
                    laneSet = true;
                    break;
                }
                if (!obstacles[respawnRow, 0])
                {
                    currentLane = 0;
                    laneSet = true;
                    break;
                }
                if (!obstacles[respawnRow, 2])
                {
                    currentLane = 2;
                    laneSet = true;
                    break;
                }
            }
        }
        if (!laneSet)
        {
            currentLane = 1;
        }
        UpdateTargetPosition();
        transform.position = new Vector3(
            currentLane * laneWidth - laneWidth,
            transform.position.y,
            transform.position.z
        );

        // Destroy the other obstacle in the tutorial area
        // Player Crashes to learn a lesson -> then path is clear next time, no lives lost
        if (other.gameObject.CompareTag("TutorialObstacle"))
        {
            other.gameObject.SetActive(false);
        }

        animator.SetTrigger("Respawn");
        yield return new WaitForSeconds(2.5f);
        foreach (var controller in platformControllers)
        {
            controller.speed = 10f;
        }
        isDead = false;
        spawnCooldown = false;
    }

    private void GameOver()
    {
        MoveToEndGameScene();
    }
}
