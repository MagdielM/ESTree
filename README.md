# EStateTree

EStateTree is a simple, flexible state tree implementation which facilitates modelling complex behaviors
compositionally. It allows you to define hierarchies of state objects that can be used to drive your program logic,
simplifying the implementation of complex logical branching.

EStateTree's design derives heavily from state charts, albeit without the concept of regions for simplicity's sake.
You can learn more about state charts [here](https://statecharts.dev/).

Using the library mainly involves defining hierarchies of states, defining transitions between them, defining responses
to events, and assigning behaviors to each of these.



## Usage



### `State`

The `State` type comprises the majority of the library. Each state is comprised of an ID, a number of event responses,
a number of child `State`s, and transitions between the aforementioned children. All you need to create a new `State`
is a `StateId`, which is a record struct that wraps a string:

```cs
StateId id = new("state");
State state = new(id);
// There is an alternative string constructor: State state = new("state");
```



### Adding children

Any number of states may be added as children of another state, using `AddChild()`:

```cs
State state = new("state");
State child1 = new("child1");

state.AddChild(child1);
```

This, however, is beholden to a number of restrictions:

- There may not be more than one child with the same ID.
- The child must not already be parented to another state.
- The parent must not be within the hierarchy of the child's own children, as this would create a cycle.

> When a state with no children has a new state added to it, this becomes its new default and active child.
>
> The active child is the child that will have events forwarded to it when `DrillEvent()` is used to propagate events
down the hierarchy. If the active child were to be removed, the default child will become the new active child.

A collection of children may also be added at once using `AddChildren()`:

```cs
List<State> states = new() {
    new("child2"),
    new("child3"),
    new("child4"),
}

state.AddChildren(states);
```



### Accessing children

A state's children may be accessed by calling `GetChild()` or using the index operator, passing in the ID of the
desired child as a parameter:

```cs
State child = state.GetChild(new StateId("child3"));
child = state[new StateId("child3")];
// There are overloads that take in strings:
// child = state.GetChild("child3");
// child = state["child3"];
```

You may also retrieve a collection of all of a state's children using `GetChildren()`:

```cs
IEnumerable<State> children = state.GetChildren();
```



### Removing children

Children may be removed using `RemoveChild()`, which takes in the ID of the child to be removed:

```cs
state.RemoveChild(states[2].Id);
// StateIds are value-equivalent, so state.RemoveChild(new("child4"));
// would work as well.
```

As mentioned previously, when the active child of a state is removed, the state's default child becomes its new active
child. Because of this, the default child cannot be removed unless the state has no other children.

All children in a state may also be removed at once using `ClearChildren()`:

```cs
state.ClearChildren();
```

> Both `RemoveChild()` and `ClearChildren()` also remove all transitions referencing any removed children from the
state.



### Events

Events are used to trigger behavior within the state hierarchy. They can be used to trigger transitions or to elicit
responses from states. Each event is defined as an `EventId`, a record struct that wraps around a string, much like
`StateId`.

Events can be propagated up and down the hierarchy with `FireEvent()` and `DrillEvent()` respectively.

```cs
state.FireEvent(new EventId("event"));
```

> `FireEvent()` will attempt to consume the event before propagating it, whereas `DrillEvent()` will propagate the
event immediately.



### `EventResponse`

Each state contains a property, `EventResponses`, that maps `EventId`s to `EventResponse` structs. `EventResponse` is a
record struct comprised of an `Action` to be performed and a boolean, `ShouldConsumeEvent`, that indicates whether the
event should be considered consumed by the state if the response is performed. They are constructed as follows:

```cs
EventId eventId = new EventId("event");
EventResponse response = new EventResponse(
    () => Console.WriteLine($"Responding to event: {eventId.Id}"),
    true);
```

You can add event responses to the state by simply adding new entries to the `EventResponses` dictionary. Removing them
is as simple as calling `Remove()` on the dictionary:

```cs
state.EventResponses.Add(eventId, response);
state.EventResponses.Remove(eventId);
```



### Transitions

Transitions are connections between child states that allow the state's active child to be reassigned. They are
comprised of an origin `StateId` (the "from" state), a target `StateId` (the "to" state), an event that triggers the
transition (the "on" event), an optional condition to be checked before the transition can be performed, and optional
behavior to be executed when the transition is performed.

> Transitions always take priority over event responses, **and always consume events**.

Transitions may be added to a state using the `AddTransition()` method:

```cs
int count = 0;
bool canTransition = true;

state.AddTransition(
    new StateId("child2"),
    new StateId("child3"),
    new EventId("PerformTransition"),
    () => canTransition,
    () => count++);
```

Adding transitions is beholden to the following restrictions:

- There must be child states with the "from" and "to" `StateId`s among the state's chidlren.
- The "from" state must not be the same as the "to" state.
- The "on" event must not be null, empty, or comprised soleley of whitespace.
- The state must not already contain a transition that has the same "on" event and "from" state.

> If no condition is provided, the transition will always be performed.

Transitions are marked as "shallow" by default. Shallow transitions only perform the entry and exit behaviors of the
child states directly involved in the transition, whereas non-shallow transitions also perform the entry and exit
behaviors of the active branches of the states being entered and exited respectively. To mark a transition as
non-shallow, set the `isShallow` parameter of `AddTransition()` to `false`:

```cs
state.AddTransition(
    new StateId("child3"),
    new StateId("child2"),
    new EventId("PerformAnotherTransition"),
    isShallow: false);
```



### Entry, update, and exit behaviors

Each state contains `Action` delegates for behavior to be performed when entering and exiting the state, as well as
another delegate for behavior to be performed on demand. States have dedicated methods for adding and removing all of
these:

```cs
void PrintHello()
{
    Console.WriteLine("Hello!");
}

state.AddEnterBehavior(PrintHello);
state.RemoveEnterBehavior(PrintHello);

state.AddUpdateBehavior(PrintHello);
state.RemoveUpdateBehavior(PrintHello);

state.AddExitBehavior(PrintHello);
state.RemoveExitBehavior(PrintHello);
```

> When states are entered via non-shallow transitions or updated, the entry and update behaviors execute top-to-bottom:
the entry and update behavior of the outermost states in the active branch will be performed before that of the
innermost states. **Exit behaviors, by contrast, execute bottom-to-top.**

`Action` delegates naturally also accept lambda expressions, but keep in mind that you will need a reference to the
lambda expression in order to remove it.

> Behavior added to states via inline lambda expressions **cannot be removed.**



### `Machine`

The `State`-derived `Machine` type is intended to be a top-level container for a state hierarchy, providing safer and
more convenient access to several `State` methods.


#### `Enter()` and `Exit()`

The `Enter()` and `Exit()` methods allow state hierarchies to perform initial and final behaviors on demand, with
additional safeguards to ensure that entry behavior cannot be performed more than once without performing exit behavior and vice versa.


#### `Update()`

The `Update()` methods can be used to execute the state hierarchy's update behavior on demand.


#### `SendEvent()`

The `SendEvent()` method functions similarly to `State`'s `DrillEvent()` method, but also attempts to have the
`Machine` itself consume the event before propagating it downwards, allowing events to be sent down from the top of the
hierarchy while also allowing the containing machine to respond to them if needed.


#### `BubbleEvent()`

The `BubbleEvent()` method serves as a convenient way to call `FireEvent()` from the innermost state in the active
branch, without forcing callers to find this state beforehand.