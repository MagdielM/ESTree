namespace EStateTree;

/// <summary>
/// Describes a transition that a <see cref="State"/> may perform.
/// </summary>
///
/// <param name="To">The <see cref="StateId"/> of the endpoint of this transition.</param>
///
/// <param name="Condition">
/// A function that returns whether or not this transition may be performed.
/// </param>
///
/// <param name="Behavior">
/// An action to be invoked when the reansition is performed.
/// </param>
public readonly record struct Transition(StateId To, Func<bool>? Condition, Action? Behavior);
