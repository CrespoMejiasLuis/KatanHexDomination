 public enum StrategicState 
{   
    Economy, 
    War      
}

public enum TacticalAction
{ 
    EarlyExpansion, 
    Development,
    BuildArmy,      // NUEVO: Construcción preventiva de ejército
    ActiveDefense,  
    Assault        
}

public enum AIGoalType { None, Expand, Defend, Attack, ProduceUnit }
