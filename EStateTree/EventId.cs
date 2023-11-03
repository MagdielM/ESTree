namespace EStateTree;

/// <summary>
/// Describes an event that a <see cref="State"/> may respond to, defined as a single
/// identifier.
/// </summary>
///
/// <param name="Id">The string that comprises this event ID.</param>
public readonly record struct EventId(string Id);