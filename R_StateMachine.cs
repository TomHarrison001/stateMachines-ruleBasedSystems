using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class R_StateMachine : MonoBehaviour
{
    private Dictionary<Type, R_BaseState> states;
    public R_BaseState currentState;

    public void SetStates(Dictionary<Type, R_BaseState> states)
    {
        this.states = states;
        currentState = states.Values.First();
    }

    private void Update()
    {
        var nextState = currentState.StateUpdate();
        if (nextState != null && nextState != currentState.GetType())
        {
            SwitchToState(nextState);
        }
    }

    private void SwitchToState(Type nextState)
    {
        currentState.StateExit();
        currentState = states[nextState];
        currentState.StateEnter();
    }
}
