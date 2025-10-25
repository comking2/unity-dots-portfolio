using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }
    
    [Header("Input Settings")]
    public float inputSensitivity = 0.01f;
    
    private float deltaX;
    private bool isHolding;
    private float lastMouseX;
    
    public float DeltaX => deltaX;
    public bool IsHolding => isHolding;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Update()
    {
        HandleMouseInput();
        HandleKeyboardInput();
    }
    
    private void HandleMouseInput()
    {
        float currentMouseX = Input.mousePosition.x;
        
        if (Input.GetMouseButton(0))
        {
            if (!isHolding)
            {
                lastMouseX = currentMouseX;
                isHolding = true;
            }
            
            deltaX = (currentMouseX - lastMouseX) * inputSensitivity;
        }
        else
        {
            isHolding = false;
            deltaX = 0;
        }
    }
    
    private void HandleKeyboardInput()
    {
        // F5: 현재 씬 재로드
        if (Input.GetKeyDown(KeyCode.F5))
        {
            SceneManager sceneManager = FindObjectOfType<SceneManager>();
            if (sceneManager != null)
            {
                sceneManager.ReloadCurrentScene();
            }
        }
        
        // F6: 다음 씬
        if (Input.GetKeyDown(KeyCode.F6))
        {
            SceneManager sceneManager = FindObjectOfType<SceneManager>();
            if (sceneManager != null)
            {
                sceneManager.LoadNextScene();
            }
        }
        
        // F4: 이전 씬
        if (Input.GetKeyDown(KeyCode.F4))
        {
            SceneManager sceneManager = FindObjectOfType<SceneManager>();
            if (sceneManager != null)
            {
                sceneManager.LoadPreviousScene();
            }
        }
    }
}