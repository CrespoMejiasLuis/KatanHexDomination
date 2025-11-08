public enum GameState
{
    None,
    Initializing,     // El juego se est� cargando, el tablero se est� animando
    PlayerTurn,       // El jugador puede realizar acciones
    AITurn,           // La IA (enemigo) est� pensando y actuando
    EndTurnResolution, // Se calculan los recursos, se comprueban las victorias
    GameOver          // La partida ha terminado
}
