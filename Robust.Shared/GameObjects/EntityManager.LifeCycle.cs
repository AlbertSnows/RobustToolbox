﻿using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects;

public partial class EntityManager
{
    private static readonly ComponentAdd CompAddInstance = new();
    private static readonly ComponentInit CompInitInstance = new();
    private static readonly ComponentStartup CompStartupInstance = new();
    private static readonly ComponentShutdown CompShutdownInstance = new();
    private static readonly ComponentRemove CompRemoveInstance = new();

    /// <summary>
    /// Increases the life stage from <see cref="ComponentLifeStage.PreAdd" /> to <see cref="ComponentLifeStage.Added" />,
    /// after raising a <see cref="ComponentAdd"/> event.
    /// </summary>
    internal void LifeAddToEntity(Component component, CompIdx type)
    {
        DebugTools.Assert(component.LifeStage == ComponentLifeStage.PreAdd);

        component.LifeStage = ComponentLifeStage.Adding;
        component.CreationTick = CurrentTick;
        // networked components are assumed to be dirty when added to entities. See also: ClearTicks()
        component.LastModifiedTick = CurrentTick;
        EventBus.RaiseComponentEvent(component, type, CompAddInstance);
        component.LifeStage = ComponentLifeStage.Added;
    }

    /// <summary>
    /// Increases the life stage from <see cref="ComponentLifeStage.Added" /> to <see cref="ComponentLifeStage.Initialized" />,
    /// calling <see cref="Initialize" />.
    /// </summary>
    internal void LifeInitialize(Component component, CompIdx type)
    {
        DebugTools.Assert(component.LifeStage == ComponentLifeStage.Added);

        component.LifeStage = ComponentLifeStage.Initializing;
        EventBus.RaiseComponentEvent(component, type, CompInitInstance);
        component.LifeStage = ComponentLifeStage.Initialized;
    }

    /// <summary>
    /// Increases the life stage from <see cref="ComponentLifeStage.Initialized" /> to
    /// <see cref="ComponentLifeStage.Running" />, calling <see cref="Startup" />.
    /// </summary>
    internal void LifeStartup(Component component)
    {
        DebugTools.Assert(component.LifeStage == ComponentLifeStage.Initialized);

        component.LifeStage = ComponentLifeStage.Starting;
        EventBus.RaiseComponentEvent(component, CompStartupInstance);
        component.LifeStage = ComponentLifeStage.Running;
    }

    /// <summary>
    /// Increases the life stage from <see cref="ComponentLifeStage.Running" /> to <see cref="ComponentLifeStage.Stopped" />,
    /// calling <see cref="Shutdown" />.
    /// </summary>
    /// <remarks>
    /// Components are allowed to remove themselves in their own Startup function.
    /// </remarks>
    internal void LifeShutdown(Component component)
    {
        DebugTools.Assert(component.LifeStage is >= ComponentLifeStage.Initializing and < ComponentLifeStage.Stopping);

        if (component.LifeStage <= ComponentLifeStage.Initialized)
        {
            // Component was never started, no shutdown logic necessary. Simply mark it as stopped.
            component.LifeStage = ComponentLifeStage.Stopped;
            return;
        }

        component.LifeStage = ComponentLifeStage.Stopping;
        EventBus.RaiseComponentEvent(component, CompShutdownInstance);
        component.LifeStage = ComponentLifeStage.Stopped;
    }

    /// <summary>
    /// Increases the life stage from <see cref="ComponentLifeStage.Stopped" /> to <see cref="ComponentLifeStage.Deleted" />,
    /// calling <see cref="Component.OnRemove" />.
    /// </summary>
    internal void LifeRemoveFromEntity(Component component)
    {
        // can be called at any time after PreAdd, including inside other life stage events.
        DebugTools.Assert(component.LifeStage != ComponentLifeStage.PreAdd);

        component.LifeStage = ComponentLifeStage.Removing;
        EventBus.RaiseComponentEvent(component, CompRemoveInstance);

        component.OnRemove();

#if DEBUG
        if (component.LifeStage != ComponentLifeStage.Deleted)
        {
            DebugTools.Assert($"Component {component.GetType().Name} did not call base {nameof(component.OnRemove)} in derived method.");
        }
#endif
    }
}
