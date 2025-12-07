using UnityEngine;

public class MilitarizationState : AIState
{
    public MilitarizationState(AI_General context) : base(context) { }

    public override void OnEnter()
    {
        Debug.Log("🪖 Entrando en Estado: MILITARIZACIÓN");
        context.CurrentOrder = TacticalAction.BuildArmy;
    }

    public override void Execute(float threatLevel)
    {
        // 1. SEGURIDAD: Si economía crítica, forzar retirada
        if (context.IsEconomyCritical())
        {
            Debug.Log("📉 MILITARIZATION: Economía crítica. Volviendo a Economía.");
            context.ChangeState(new EconomyState(context));
            return;
        }

        // 2. ESCALADA: Si amenaza crítica, ir a guerra
        if (threatLevel > context.warThreshold)
        {
            Debug.Log("⚠️ MILITARIZATION: Amenaza crítica detectada. Escalando a Guerra.");
            context.ChangeState(new WarState(context));
            return;
        }

        // 3. DESMILITARIZACIÓN: Si amenaza muy baja, volver a paz
        // 🔧 FIX ALTO #6: Usar umbral de salida para histéresis
        if (threatLevel < context.exitMilitarizationThreshold)
        {
            Debug.Log($"🏳️ MILITARIZATION: Amenaza muy baja ({threatLevel:F0} < {context.exitMilitarizationThreshold}). Volviendo a Economía.");
            context.ChangeState(new EconomyState(context));
            return;
        }

        // 4. OPTIMIZACIÓN: Si tenemos ejército suficiente y amenaza controlada
        float militaryPower = context.CalculateMyMilitaryPower();
        // 🔧 FIX ALTO #6: Comparar con exitWarThreshold para consistencia
        if (militaryPower > threatLevel * 1.5f && threatLevel < context.exitWarThreshold)
        {
            Debug.Log($"💪 MILITARIZATION: Ejército suficiente ({militaryPower:F0} > {threatLevel:F0}*1.5). Volviendo a Development.");
            context.CurrentOrder = TacticalAction.Development;
            context.ChangeState(new EconomyState(context));
            return;
        }

        // Mantener orden de construcción de ejército
        context.CurrentOrder = TacticalAction.BuildArmy;
    }

    public override void OnExit()
    {
        Debug.Log("🚪 Saliendo de Estado: MILITARIZACIÓN");
    }
}
