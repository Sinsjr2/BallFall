public interface IUpdate<State, Act> {
    State Update(State state, Act act);
}
