namespace TEA {
    public interface IUpdate<State, Message> {
        State Update(State state, Message message);
    }
}
