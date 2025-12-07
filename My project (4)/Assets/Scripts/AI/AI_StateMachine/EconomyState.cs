using UnityEngine;
using System.Collections.Generic;

public class EconomyState : AIState
{
    public EconomyState(AI_General context) : base(context) { }

    public override void OnEnter()
    {
        // 🔧 FIX ALTO #5: No sobrescribir CurrentOrder si ya es apropiado para Economy
        // Solo resetear si venimos de un estado no-económico (War, Militarization)
        
        if (context.CurrentOrder != TacticalAction.EarlyExpansion && 
            context.CurrentOrder != TacticalAction.Development)
        {
            // Decidir sub-estado basándose en situación actual
            int settlementCount = CountSettlements();
            int expansionCount = CountExpansionUnits();
            
            if (expansionCount >= 5 || settlementCount >= 3)
            {
                context.CurrentOrder = TacticalAction.Development;
                Debug.Log($"🏗️ ECONOMY OnEnter: Entrando en DEVELOPMENT ({expansionCount} unidades expansión, {settlementCount} asentamientos)");
            }
            else
            {
                context.CurrentOrder = TacticalAction.EarlyExpansion;
                Debug.Log($"🌱 ECONOMY OnEnter: Entrando en EARLY EXPANSION ({expansionCount} unidades expansión, {settlementCount} asentamientos)");
            }
        }
        else
        {
            Debug.Log($"✅ ECONOMY OnEnter: Preservando CurrentOrder existente: {context.CurrentOrder}");
        }
    }

    public override void Execute(float totalThreat)
    {
        // 1. CHEQUEO DE GUERRA (Prioridad Máxima)
        if (totalThreat > context.warThreshold)
        {
            Debug.Log("⚠️ ECONOMY: Amenaza crítica detectada. Entrando en Guerra.");
            context.ChangeState(new WarState(context));
            return;
        }
        
        // 2. CHEQUEO DE MILITARIZACIÓN (Amenaza moderada)
        if (totalThreat > context.militarizationThreshold)
        {
            // VERIFICAR: Necesitamos al menos 2 asentamientos antes de militarizarnos
            int settlementCount = CountSettlements();
            if (settlementCount >= 2)
            {
                Debug.Log($"🪖 ECONOMY: Amenaza moderada ({totalThreat:F0}) + {settlementCount} asentamientos. Iniciando militarización.");
                context.ChangeState(new MilitarizationState(context));
                return;
            }
            else
            {
                Debug.Log($"⚠️ ECONOMY: Amenaza {totalThreat:F0} pero solo {settlementCount} asentamiento(s). Continuar expansión primero.");
                // Seguir en expansión aunque haya amenaza
                context.CurrentOrder = TacticalAction.EarlyExpansion;
            }
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

    // --- HELPER: Cuenta asentamientos para verificar infraestructura ---
    private int CountSettlements()
    {
        if (context.myPlayer == null || context.myPlayer.ArmyManager == null) 
            return 0;

        int count = 0;
        var myUnits = context.myPlayer.ArmyManager.GetAllUnits();

        foreach (var unit in myUnits)
        {
            if (unit != null && unit.statsBase != null)
            {
                if (unit.statsBase.nombreUnidad == TypeUnit.Poblado || 
                    unit.statsBase.nombreUnidad == TypeUnit.Ciudad)
                {
                    count++;
                }
            }
        }

        return count;
    }

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