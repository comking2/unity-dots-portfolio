using Unity.Entities;
using UnityEngine.InputSystem;

public struct SimpleDragInput : IComponentData
{
    public float DeltaX;
    public bool IsHolding;
}

// SystemBase로 변경 (class 사용 가능)
public partial class InputSystem : SystemBase
{
    private Mouse mouse;
    private float lastMouseX;
    
    protected override void OnCreate()
    {
        EntityManager.CreateEntity(typeof(SimpleDragInput));
        mouse = Mouse.current;
    }
    
    protected override void OnUpdate()
    {
        if (mouse == null)
        {
            mouse = Mouse.current;
            if (mouse == null) return;
        }
        
        var input = SystemAPI.GetSingletonRW<SimpleDragInput>();
        float currentMouseX = mouse.position.ReadValue().x;
        
        if (mouse.leftButton.isPressed)
        {
            if (!input.ValueRO.IsHolding)
            {
                lastMouseX = currentMouseX;
                input.ValueRW.IsHolding = true;
            }
            
            input.ValueRW.DeltaX = (currentMouseX - lastMouseX) * 0.01f;
            //lastMouseX = currentMouseX;
        }
        else
        {
            input.ValueRW.IsHolding = false;
            input.ValueRW.DeltaX = 0;
        }
    }
}