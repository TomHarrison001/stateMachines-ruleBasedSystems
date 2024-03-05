using System;
using UnityEngine;

public class R_AttackState : R_BaseState
{
    private R_SmartTank tank;

    public R_AttackState(R_SmartTank tank)
    {
        this.tank = tank;
    }

    public override Type StateEnter()
    {
        tank.stats["attackState"] = true;

        return null;
    }

    public override Type StateExit()
    {
        tank.stats["attackState"] = false;

        return null;
    }

    public override Type StateUpdate()
    {
        tank.Attack();

        foreach (var item in tank.rules.GetRules)
        {
            if (item.CheckRule(tank.stats) != null)
                return item.CheckRule(tank.stats);
        }

        return null;
    }
}
