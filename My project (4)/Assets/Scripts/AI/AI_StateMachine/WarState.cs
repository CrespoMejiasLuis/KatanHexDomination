using UnityEngine;

public class WarState : AIState
{
    public WarState(AI_General context) : base(context) { }

    public override void OnEnter()
    {
        Debug.Log("⚔️ Entrando en Estado: GUERRA");
        // Ante la duda, al entrar en guerra nos ponemos a la defensiva
        context.CurrentOrder = TacticalAction.ActiveDefense;
    }

    public override void Execute(float threatLevel)
    {
        if (threatLevel < context.peaceThreshold)
        {
            // ...volvemos a Economía
            context.ChangeState(new EconomyState(context));
            return;
        }
        
        // TODO: En el futuro conectar con ArmyManager para comparar fuerzas reales.
        bool soyMasFuerte = false; 

        if (soyMasFuerte)
        {
            context.CurrentOrder = TacticalAction.Assault;
        }
        else
        {
            context.CurrentOrder = TacticalAction.ActiveDefense;
        }
    }

    public override void OnExit() { }
}