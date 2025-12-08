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
            
            if (expansionCount >= 5 || settlementCount >= 5)
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
            Debug.Log($"[OK] ECONOMY OnEnter: Preservando CurrentOrder existente: {context.CurrentOrder}");
        }
    }

    public override void Execute(float totalThreat)
    {
        // 1. CHEQUEO DE GUERRA (Prioridad Máxima)
        if (totalThreat > context.warThreshold)
        {
            Debug.Log("[WARNING] ECONOMY: Amenaza crítica detectada. Entrando en Guerra.");
            context.ChangeState(new WarState(context));
            return;
        }
        
        
        // 2. CHEQUEO DE MILITARIZACIÓN (Amenaza moderada)
        // 🎯 MEJORA: Decisión multi-factor en vez de umbral fijo
        float ratio = context.GetMilitaryToEconomyRatio();
        int settlementCount = CountSettlements();
        
        // Solo militarizar si:
        // - Hay amenaza real (>50)
        // -  Y estamos vulnerables (ratio < 1.0)
        // - Y tenemos mínimo 2 asentamientos
        if (totalThreat > 50f && ratio < 1.0f && settlementCount >= 2)
        {
            Debug.Log($"🪖 ECONOMY: Amenaza ({totalThreat:F0}) + vulnerable (ratio {ratio:F1} < 1.0). Iniciando militarización.");
            context.ChangeState(new MilitarizationState(context));
            return;
        }
        else if (totalThreat > 50f && settlementCount < 2)
        {
            Debug.Log($"[WARNING] ECONOMY: Amenaza {totalThreat:F0} pero solo {settlementCount} asentamiento(s). Continuar expansión primero.");
            // Seguir en expansión aunque haya amenaza
            context.CurrentOrder = TacticalAction.EarlyExpansion;
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
        // 🎯 MEJORA: Expansión continua si todos son ciudades
        int totalSettlements = CountSettlements();
        int totalCities = CountCities();
        
        // Si TODOS los asentamientos ya son ciudades Y tenemos recursos
        if (totalSettlements > 0 && totalCities == totalSettlements)
        {
            // Verificar si todavía necesitamos más asentamientos para ganar
            // Asumimos victoria con 10 puntos (ajustar según tu juego)
            int pointsToWin = 10;
            int currentPoints = totalCities; // Simplificado: cada ciudad = 1 punto
            
            if (currentPoints < pointsToWin)
            {
                Debug.Log($"🏗️ DEVELOPMENT: Todas las ciudades mejoradas ({totalCities}/{totalSettlements}). Volviendo a expansión para ganar ({currentPoints}/{pointsToWin} puntos).");
                context.CurrentOrder = TacticalAction.EarlyExpansion;
                return;
            }
        }
        
        // Comportamiento normal: mantener desarrollo
        // PlayerIA leerá 'Development' y asignará objetivos de upgrade o producción
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