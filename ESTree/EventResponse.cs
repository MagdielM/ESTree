namespace ESTree;

public readonly record struct EventResponse(Action Response, bool ShouldConsumeEvent);
