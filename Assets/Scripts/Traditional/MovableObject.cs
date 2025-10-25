using UnityEngine;

public class MovableObject : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 1f;
    public Vector3 direction = Vector3.zero;
    
    [Header("Performance Settings")]
    public bool useFixedUpdate = true;
    
    [Header("Animation")]
    public AnimationClip idleAnimation;
    public AnimationClip moveAnimation;
    public float animationSpeed = 1f;
    
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
        if (!useFixedUpdate)
        {
            ApplyMovement(Time.deltaTime);
        }
        UpdateAnimation();
    }
    
    void FixedUpdate()
    {
        if (useFixedUpdate)
        {
            ApplyMovement(Time.fixedDeltaTime);
        }
    }
    
    public void SetMovement(Vector3 newDirection, float newSpeed)
    {
        direction = newDirection;
        speed = newSpeed;
    }
    
    public void SetDirection(Vector3 newDirection)
    {
        direction = newDirection;
    }
    
    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
    }
    
    private void ApplyMovement(float deltaTime)
    {
        transform.position += direction * speed * deltaTime;
    }
    
    private void UpdateAnimation()
    {
        if (animationComponent == null) return;
        
        float currentSpeed = direction.magnitude * speed;
        bool shouldMove = currentSpeed > 0.01f;
        
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
            animationComponent[moveAnimation.name].speed = currentSpeed * animationSpeed;
        }
    }
}