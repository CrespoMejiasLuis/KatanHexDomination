using UnityEngine;

public class EconomyState : AIState
{
    // Pasamos el contexto al constructor del padre (base)
    public EconomyState(AI_General context) : base(context) { }

    public override void OnEnter()
    {
        Debug.Log("🕊️ Entrando en Estado: ECONOMÍA");
        // Por defecto, al entrar en paz, buscamos expandirnos
        context.CurrentOrder = TacticalAction.EarlyExpansion;
    }

    public override void Execute(float threatLevel)
    {
        if (threatLevel > context.warThreshold)
        {
            // ...cambiamos al estado de Guerra
            context.ChangeState(new WarState(context));
            return; 
        }

        Vector2Int? bestSpot = context.aiAnalysis.GetBestPositionForExpansion();

        if (bestSpot.HasValue)
        {
            // Hay hueco -> Ordenamos EXPANSIÓN
            context.CurrentOrder = TacticalAction.EarlyExpansion;
        }
        else
        {
            // Mapa lleno -> Ordenamos DESARROLLO
            context.CurrentOrder = TacticalAction.Development;
        }
    }

    public override void OnExit() 
    {
        // Aquí podrías limpiar cosas si fuera necesario
    }
}