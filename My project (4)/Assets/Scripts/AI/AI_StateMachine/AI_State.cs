using UnityEngine;

public abstract class AIState
{
    protected AI_General context; 

    public AIState(AI_General context)
    {
        this.context = context;
    }

    public abstract void OnEnter();                
    public abstract void Execute(float threatLevel); 
    public abstract void OnExit();                  
}