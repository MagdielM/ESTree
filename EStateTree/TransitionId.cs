using System;

namespace EStateTree;

public readonly record struct TransitionId(EventId Id, StateId From);
