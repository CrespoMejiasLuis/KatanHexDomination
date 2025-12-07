using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class GoapPlanner
{
    public class Node{
        public Node parent;
        public float cost;
        public Dictionary<string, int> state;
        public GoapAction action;

        public Node(Node parent, float cost, Dictionary<string, int> state, GoapAction action)
        {
            this.parent = parent;
            this.cost = cost;
            this.state = state;
            this.action = action;
        }
    }

    //crear el plan
    public Queue<GoapAction> Plan(GameObject agent, HashSet<GoapAction> availableActions, Dictionary<string, int> worldState, Dictionary<string, int> goal)
    {
        Debug.Log($"═══════════════════════════════════════════════════");
        Debug.Log($"🧠 GOAP PLANNER: Iniciando planificación para {agent.name}");
        Debug.Log($"Acciones totales disponibles: {availableActions.Count}");
        
        //1.Inicializacion

        //1.1.Resetear acciones
        foreach(var act in availableActions)
        {
            act.DoReset();
        }

        //1.2.Filtrar acciones usables
        HashSet<GoapAction> usableActions = new HashSet<GoapAction>();
        foreach(var act in availableActions)
        {
            if(act.CheckProceduralPrecondition(agent))
            {
                usableActions.Add(act);
            }
        }

        //1.3.Varibles de busqueda
        List<Node> leaves = new List<Node>();
        Node startNode = new Node(null, 0, worldState, null);

        Debug.Log($"═══ FILTRADO DE ACCIONES ═══");
        Debug.Log($"Acciones usables tras filtro: {usableActions.Count} de {availableActions.Count}");
        
        if (usableActions.Count > 0)
        {
            foreach(var a in usableActions) 
            {
                Debug.Log($"  ✅ Acción válida: {a.GetType().Name} (Costo: {a.cost})");
            }
        }
        else
        {
            Debug.LogWarning($"❌ NINGUNA acción pasó el filtro procedural!");
        }

        Debug.Log($"═══ ESTADO DEL MUNDO ═══");
        foreach(var kvp in worldState) 
        {
            Debug.Log($"  [{kvp.Key}] = {kvp.Value}");
        }

        Debug.Log($"═══ OBJETIVO ═══");
        foreach(var kvp in goal) 
        {
            Debug.Log($"  [{kvp.Key}] = {kvp.Value}");
        }

        //2.Construir grafo(recursivo)
        bool succes = BuildGraph(startNode, leaves, usableActions, goal);

        //si no se ha podido construir
        if(!succes)
        {
            Debug.LogWarning($"❌ GOAP PLANNER: NO se encontró un plan válido para {agent.name}");
            Debug.Log($"═══════════════════════════════════════════════════");
            return null;
        }
        
        //si si se ha podido
        //3.encontrar hoja mas barata
        Node cheapestNode = null;
        foreach(var leaf in leaves)
        {
            if(cheapestNode == null || cheapestNode.cost > leaf.cost)
            {
                cheapestNode = leaf;
            }
        }

        //3.2.Reconstruir secuencia de acciones
        List<GoapAction> planResult = new List<GoapAction>();
        Node currentNode = cheapestNode;

        while(currentNode!=null)
        {
            if(currentNode.action !=null)
                planResult.Insert(0, currentNode.action); //para siempre meter la accion siguiente la primera y mantener orden
            currentNode = currentNode.parent;
        }

        //convertir la lista en una cola
        Queue<GoapAction> planQueue = new Queue<GoapAction>();
        foreach(var act in planResult)
        {
            planQueue.Enqueue(act);
        }

        return planQueue;
    }

    //crear grafo de forma recursiva. Realiza busqueda en profundidad
    public bool BuildGraph(Node parent, List<Node> leaves, HashSet<GoapAction> usableActions, Dictionary<string, int> goal)
    {
        //1.Chequear objetivo
        if(InState(parent.state, goal)) ///si el padre ya cumple objetivo
        {
            leaves.Add(parent);
            return true; 
        }

        bool foundSolution = false;

        foreach(var act in usableActions)
        {
            if(InState(parent.state, act.Preconditions))
            {
                Dictionary<string, int> newState = simulateEffects(parent.state, act.Effects);

                Node newNode = new Node(parent, parent.cost + act.cost, newState, act);

                //continuar busqueda desde nuevo nodo (recursiva)

                HashSet<GoapAction> subset = new HashSet<GoapAction>(usableActions);
                subset.Remove(act);

                if(BuildGraph(newNode, leaves, subset, goal))
                    foundSolution = true;
            }
        }

        return foundSolution;        
    }

    private bool InState(Dictionary<string, int> state, Dictionary<string, int> test)
    {
        foreach(var requierement in test)
        {
            if(!state.ContainsKey(requierement.Key) || state[requierement.Key] != requierement.Value)
            {
                return false;
            }
        }
        return true;
    }

    private Dictionary<string, int> simulateEffects(Dictionary<string, int> currentState, Dictionary<string, int> effects)
    {
        Dictionary<string, int> newState = new Dictionary<string, int>(currentState);

        foreach(var effect in effects)
        {
            newState[effect.Key] = effect.Value;
        }
        return newState;
    }
}
