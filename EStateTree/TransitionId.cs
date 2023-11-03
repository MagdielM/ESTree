namespace EStateTree;

/// <summary>
/// Describes an identifier for a <see cref="Transition"/>.
/// </summary>
///
/// <param name="Id">The event that triggers the transition identified by this ID.</param>
///
/// <param name="From">
/// The ID of the origin state for the transition identified by this ID.
/// </param>
public readonly record struct TransitionId(EventId Id, StateId From);
