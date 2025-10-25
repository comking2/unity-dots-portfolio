using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 1f;
    
    [Header("Animation")]
    public AnimationClip idleAnimation;
    public AnimationClip moveAnimation;
    public float animationSpeed = 1f;
    
    private Vector3 moveDirection = Vector3.zero;
    private Animation animationComponent;
    private bool isMoving = false;
    
    void Start()
    {
        animationComponent = GetComponent<Animation>();
        
        if (animationComponent != null && idleAnimation != null)
        {
            animationComponent.clip = idleAnimation;
            animationComponent.Play();
        }
    }
    
    void Update()
    {
        HandleInput();
        ApplyMovement();
        UpdateAnimation();
    }
    
    private void HandleInput()
    {
        if (InputManager.Instance == null) return;
        
        float x = InputManager.Instance.DeltaX;
        bool isStop = Mathf.Epsilon > Mathf.Abs(x);
        moveDirection = isStop ? Vector3.zero : new Vector3(x, 0, 0).normalized;
    }
    
    private void ApplyMovement()
    {
        transform.position += moveDirection * moveSpeed * Time.deltaTime;
    }
    
    private void UpdateAnimation()
    {
        if (animationComponent == null) return;
        
        bool shouldMove = Mathf.Abs(moveDirection.x) > 0.01f;
        
        if (shouldMove != isMoving)
        {
            isMoving = shouldMove;
            
            if (isMoving && moveAnimation != null)
            {
                animationComponent.clip = moveAnimation;
                animationComponent[moveAnimation.name].speed = animationSpeed;
                animationComponent.Play();
            }
            else if (!isMoving && idleAnimation != null)
            {
                animationComponent.clip = idleAnimation;
                animationComponent[idleAnimation.name].speed = 1f;
                animationComponent.Play();
            }
        }
        
        if (isMoving && moveAnimation != null)
        {
            float speed = Mathf.Abs(moveDirection.x);
            animationComponent[moveAnimation.name].speed = speed * animationSpeed;
        }
    }
}