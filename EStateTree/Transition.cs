namespace EStateTree;

public readonly record struct Transition(StateId To, Func<bool>? Condition, Action? Behavior);
