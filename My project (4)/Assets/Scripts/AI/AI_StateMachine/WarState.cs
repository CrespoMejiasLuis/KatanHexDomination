using UnityEngine;

public class WarState : AIState
{
    public WarState(AI_General context) : base(context) { }

    public override void OnEnter()
    {
        Debug.Log("⚔️ Entrando en Estado: GUERRA");
    }

    public override void Execute(float threatLevel)
    {

        if (context.IsEconomyCritical())
        {
            Debug.Log("📉 IA: Economía crítica. Forzando retirada a Economía.");
            context.ChangeState(new EconomyState(context));
            return;
        }

        // 2. CHEQUEO DE PAZ (Victoria o Retirada enemiga)
        if (threatLevel < context.peaceThreshold)
        {
            Debug.Log("🏳️ IA: Amenaza baja. Volviendo a Economía.");
            context.ChangeState(new EconomyState(context));
            return;
        }
        
        // 3. DECISIÓN TÁCTICA: ¿Ataque o Defensa?
        float myPower = context.CalculateMyMilitaryPower();

        // Si soy más fuerte que la amenaza, ataco a la yugular (Asalto)
        if (myPower > threatLevel)
        {
            context.CurrentOrder = TacticalAction.Assault;
        }
        else
        {
            // Si soy más débil, me protejo (Defensa Activa)
            context.CurrentOrder = TacticalAction.ActiveDefense;
        }
    }

    public override void OnExit() { }
}