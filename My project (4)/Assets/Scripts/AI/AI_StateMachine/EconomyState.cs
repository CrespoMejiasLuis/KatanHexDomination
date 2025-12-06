using UnityEngine;
using System.Collections.Generic;

public class EconomyState : AIState
{
    public EconomyState(AI_General context) : base(context) { }

    public override void OnEnter()
    {
        // Al entrar en Economía, empezamos expandiéndonos
        context.CurrentOrder = TacticalAction.EarlyExpansion;
    }

    public override void Execute(float totalThreat)
    {
        // 1. CHEQUEO DE SEGURIDAD GLOBAL (Prioridad Máxima)
        if (totalThreat > context.warThreshold)
        {
            context.ChangeState(new WarState(context));
            return;
        }

        // 2. MÁQUINA DE SUB-ESTADOS (Dependiendo del CurrentOrder)
        switch (context.CurrentOrder)
        {
            // --- FASE 1: EXPANSIÓN ---
            case TacticalAction.EarlyExpansion:
                ExecuteExpansionLogic();
                break;

            // --- FASE 2: DESARROLLO ---
            case TacticalAction.Development:
                ExecuteDevelopmentLogic();
                break;
        }
    }

    // --- LÓGICA FASE 1: Expandirse hasta llegar a 5 ---
    private void ExecuteExpansionLogic()
    {
        int currentExpansionCount = CountExpansionUnits();

        // Condición de transición interna
        if (currentExpansionCount >= 5)
        {
            Debug.Log($"🧠 ECONOMY: Límite alcanzado ({currentExpansionCount}). Cambiando orden a DEVELOPMENT.");
            
            // CAMBIO DE SUB-ESTADO
            context.CurrentOrder = TacticalAction.Development; 
            
            // Opcional: Llamar a ExecuteDevelopmentLogic() aquí si quieres que empiece en este mismo frame
            return;
        }

        // Si no hemos llegado al límite, la IA sigue buscando sitios (PlayerIA leerá 'EarlyExpansion' y actuará)
        // No necesitas llamar a nada aquí si PlayerIA ya reacciona al enum 'EarlyExpansion'.
    }

    // --- LÓGICA FASE 2: Mejorar Ciudades / Tecnologías ---
    private void ExecuteDevelopmentLogic()
    {
        // Aquí ya no buscamos expandirnos. 
        // Simplemente mantenemos el orden 'Development'.
        // El script 'PlayerIA.cs' leerá este orden y asignará objetivos de "UpgradeCiudad" o "Recruit".
        
        // Opcional: Podrías chequear si perdiste unidades y necesitas volver a expandirte
        /*
        if (CountExpansionUnits() < 3) {
            context.CurrentOrder = TacticalAction.EarlyExpansion;
        }
        */
    }

    public override void OnExit() { }

    // --- HELPER (Igual que antes) ---
    private int CountExpansionUnits()
    {
        PlayerIA myPlayer = context.myPlayer;
        Debug.Log(myPlayer == null);
        if (myPlayer == null || myPlayer.ArmyManager == null) return 0;

        int count = 0;
        foreach (Unit u in myPlayer.ArmyManager.GetAllUnits())
        {
            if (u == null) continue;
            if (u.statsBase.nombreUnidad == TypeUnit.Colono || 
                u.statsBase.nombreUnidad == TypeUnit.Poblado || 
                u.statsBase.nombreUnidad == TypeUnit.Ciudad)
            {
                count++;
            }
        }
        return count;
    }
}