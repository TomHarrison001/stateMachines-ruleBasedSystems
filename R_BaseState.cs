using System;

public abstract class R_BaseState
{
    public abstract Type StateEnter();
    public abstract Type StateExit();
    public abstract Type StateUpdate();
}
