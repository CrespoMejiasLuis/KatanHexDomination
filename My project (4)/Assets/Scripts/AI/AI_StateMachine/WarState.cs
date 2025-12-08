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
        // 🔧 FIX ALTO #6: Usar umbral de salida para histéresis
        if (threatLevel < context.exitWarThreshold)
        {
            Debug.Log($"🏳️ WAR: Amenaza baja ({threatLevel:F0} < {context.exitWarThreshold}). Volviendo a Economía.");
            context.ChangeState(new EconomyState(context));
            return;
        }
        
        // 3. DECISIÓN TÁCTICA: SIEMPRE ASALTO
        // El usuario solicitó no entrar en estado defensivo desde WarState.
        context.CurrentOrder = TacticalAction.Assault;
    }

    public override void OnExit() { }
}