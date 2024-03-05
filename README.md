# State Machines + Rule Based Systems + Behvaioural Trees
> AI implementation of State Machines, Rule Based Systems and Behavioural Trees

<br />

## R_BaseState.cs

```
using System;

public abstract class R_BaseState
{
    public abstract Type StateEnter();
    public abstract Type StateExit();
    public abstract Type StateUpdate();
}

```
  
<br />

## R_Rule.cs

```
using System;
using System.Collections.Generic;

public class R_Rule
{
    public string atecedentA, atecedentB;
    public Type consequentState;
    public enum Predicate { AND, OR, NAND };
    public Predicate compare;

    public R_Rule(string atecedentA, string atecedentB, Type consequentState, Predicate compare)
    {
        this.atecedentA = atecedentA;
        this.atecedentB = atecedentB;
        this.consequentState = consequentState;
        this.compare = compare;
    }

    public Type CheckRule(Dictionary<string, bool> stats)
    {
        bool atecedentABool = stats[atecedentA];
        bool atecedentBBool = stats[atecedentB];

        return compare switch
        {
            Predicate.AND => (atecedentABool && atecedentBBool) ? consequentState : null,
            Predicate.OR => (atecedentABool || atecedentBBool) ? consequentState : null,
            Predicate.NAND => (!atecedentABool && !atecedentBBool) ? consequentState : null,
            _ => null,
        };
    }
}

```
  
<br />

## R_BTSequence.cs

```
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class R_BTSequence : R_BTBaseNode
{
    // the leaf nodes within the sequencer
    protected List<R_BTBaseNode> btNodes = new();

    // btNodes set in constructor
    public R_BTSequence(List<R_BTBaseNode> btNodes)
    {
        this.btNodes = btNodes;
    }

    // all must return true for a success (AND)
    public override R_BTNodeStates Evaluate()
    {
        bool failed = false;
        foreach (R_BTBaseNode btNode in btNodes)
        {
            if (failed) break;

            switch (btNode.Evaluate())
            {
                case R_BTNodeStates.SUCCESS:
                    btNodeState = R_BTNodeStates.SUCCESS;
                    continue;
                case R_BTNodeStates.FAILURE:
                    btNodeState = R_BTNodeStates.FAILURE;
                    failed = true;
                    break;
                default:
                    btNodeState = R_BTNodeStates.FAILURE;
                    failed = true;
                    break;
            }
        }
        return btNodeState;
    }
}

```
  
<br />
