using UnityEngine;

public class EconomyState : AIState
{
    public EconomyState(AI_General context) : base(context) { }

    public override void OnEnter()
    {
        Debug.Log("🕊️ Entrando en Estado: ECONOMÍA");
        context.CurrentOrder = TacticalAction.EarlyExpansion;
    }

    public override void Execute(float threatLevel)
    {
        // 1. ANÁLISIS DE SEGURIDAD (Reactivo)
        // Si la amenaza es demasiado alta, nos defendemos obligatoriamente.
        if (threatLevel > context.warThreshold)
        {
            Debug.Log("❗ IA: Amenaza detectada. Entrando en Guerra Defensiva.");
            context.ChangeState(new WarState(context));
            return; 
        }

        // 2. ANÁLISIS DE OPORTUNIDAD (Proactivo) - ¡NUEVO!
        // Calculamos nuestra fuerza
        float myPower = context.CalculateMyMilitaryPower();
        

        if (myPower > threatLevel * context.opportunismFactor && myPower > 10f) // >10 para no atacar con 1 soldado
        {
            Debug.Log("😈 IA: Soy superior. Iniciando Guerra Ofensiva.");
            context.ChangeState(new WarState(context));
            return;
        }

        // 3. LOGICA ECONÓMICA (Si no hay guerra)
        /*Vector2Int? bestSpot = context.aiAnalysis.GetBestPositionForExpansion();

        if (bestSpot.HasValue)
        {
            context.CurrentOrder = TacticalAction.EarlyExpansion;
        }
        else
        {
            context.CurrentOrder = TacticalAction.Development;
        }*/
    }

    public override void OnExit() { }
}