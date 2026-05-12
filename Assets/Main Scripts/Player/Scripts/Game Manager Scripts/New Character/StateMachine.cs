public class StateMachine
{
    public State currentState;

    public void Initialize(State startingState)
    {
        currentState = startingState;
        startingState.Enter();
    }

    public void ChangeState(State newState)
    {
        var oldState = currentState;
        if (oldState != null && oldState.character != null)
            oldState.character.LogCritStateTransition(oldState, newState);

        currentState.Exit();

        currentState = newState;
        newState.Enter();
    }


}
