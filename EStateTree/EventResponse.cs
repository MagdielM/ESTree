namespace EStateTree;

/// <summary>
/// Describes a <see cref="State"/>'s response to an event.
/// </summary>
///
/// <param name="Response">The action that should be invoked in response to the event.</param>
///
/// <param name="ShouldConsumeEvent">
/// Whether or not the <see cref="State"/> executing this response should consume the given
/// event.
/// </param>
public readonly record struct EventResponse(Action Response, bool ShouldConsumeEvent);
