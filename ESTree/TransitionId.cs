using System;

namespace ESTree;

public readonly record struct TransitionId(EventId Id, StateId From);
