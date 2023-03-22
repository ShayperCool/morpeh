#if UNITY_EDITOR
#define MORPEH_DEBUG
#endif
#if !MORPEH_DEBUG
#define MORPEH_DEBUG_DISABLED
#endif

namespace Scellecs.Morpeh {
    using System;
    using System.Runtime.CompilerServices;
    using Collections;
    using JetBrains.Annotations;
    using Unity.IL2CPP.CompilerServices;

    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.DivideByZeroChecks, false)]
    public static class EntityExtensions {
        internal static Entity Create(int id, int worldId) {
            var world = World.worlds.data[worldId];
            var newEntity = new Entity { 
                entityId = new EntityId(id, world.entitiesGens[id]), 
                world = world,
                virtualArchetype = world.archetypesRoot
            };
            
            return newEntity;
        }
#if !MORPEH_STRICT_MODE
#if MORPEH_LEGACY
        [Obsolete("[MORPEH] Use Stash.Add() instead.")]
#endif
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T AddComponent<T>(this Entity entity) where T : struct, IComponent {
#if MORPEH_DEBUG
            if (entity.IsNullOrDisposed()) {
                throw new Exception("[MORPEH] You are trying AddComponent on null or disposed entity");
            }
#endif
            var stash = entity.world.GetStash<T>();

            return ref stash.Add(entity);
        }

#if MORPEH_LEGACY
        [Obsolete("[MORPEH] Use Stash.Add() instead.")]
#endif
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T AddComponent<T>(this Entity entity, out bool exist) where T : struct, IComponent {
#if MORPEH_DEBUG
            if (entity.IsNullOrDisposed()) {
                throw new Exception("[MORPEH] You are trying AddComponent on null or disposed entity");
            }
#endif
            var stash = entity.world.GetStash<T>();

            return ref stash.Add(entity, out exist);
        }

#if MORPEH_LEGACY
        [Obsolete("[MORPEH] Use Stash.Get() instead.")]
#endif
        public static ref T GetComponent<T>(this Entity entity) where T : struct, IComponent {
#if MORPEH_DEBUG
            if (entity.IsNullOrDisposed()) {
                throw new Exception("[MORPEH] You are trying GetComponent on null or disposed entity");
            }
#endif
            var stash = entity.world.GetStash<T>();

            return ref stash.Get(entity);
        }

#if MORPEH_LEGACY
        [Obsolete("[MORPEH] Use Stash.Get() instead.")]
#endif
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T GetComponent<T>(this Entity entity, out bool exist) where T : struct, IComponent {
#if MORPEH_DEBUG
            if (entity.IsNullOrDisposed()) {
                throw new Exception("[MORPEH] You are trying GetComponent on null or disposed entity");
            }
#endif
            var stash = entity.world.GetStash<T>();

            return ref stash.Get(entity, out exist);
        }

#if MORPEH_LEGACY
        [Obsolete("[MORPEH] Use Stash.Set() instead.")]
#endif
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetComponent<T>(this Entity entity, in T value) where T : struct, IComponent {
#if MORPEH_DEBUG
            if (entity.IsNullOrDisposed()) {
                throw new Exception("[MORPEH] You are trying SetComponent on null or disposed entity");
            }
#endif
            var stash = entity.world.GetStash<T>();

            stash.Set(entity, value);
        }

#if MORPEH_LEGACY
        [Obsolete("[MORPEH] Use Stash.Remove() instead.")]
#endif
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool RemoveComponent<T>(this Entity entity) where T : struct, IComponent {
#if MORPEH_DEBUG
            if (entity.IsNullOrDisposed()) {
                throw new Exception("[MORPEH] You are trying RemoveComponent on null or disposed entity");
            }
#endif
            var stash = entity.world.GetStash<T>();

            return stash.Remove(entity);
        }

#if MORPEH_LEGACY
        [Obsolete("[MORPEH] Use Stash.Has() instead.")]
#endif
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Has<T>(this Entity entity) where T : struct, IComponent {
#if MORPEH_DEBUG
            if (entity.IsNullOrDisposed()) {
                throw new Exception("[MORPEH] You are trying Has on null or disposed entity");
            }
#endif

            var stash = entity.world.GetStash<T>();
            return stash.Has(entity);
        }

#if MORPEH_LEGACY
        [Obsolete("[MORPEH] Use Stash.Migrate() instead.")]
#endif
        public static void MigrateTo(this Entity from, Entity to, bool overwrite = true) {
#if MORPEH_DEBUG
            if (from.IsNullOrDisposed() || to.IsNullOrDisposed()) {
                throw new Exception("[MORPEH] You are trying MigrateTo on null or disposed entities");
            }
#endif

            var world = from.world;
            foreach (var stashId in world.stashes) {
                var stash = Stash.stashes.data[world.stashes.GetValueByIndex(stashId)];
                stash.Migrate(from, to, overwrite);
            }
        }
        
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void AddTransfer(this Entity entity, int typeId) {
            if (entity.previousVirtualArchetype == null) {
                entity.previousVirtualArchetype = entity.virtualArchetype;
            }

            entity.virtualArchetype = entity.virtualArchetype.AddTransfer(typeId, entity.world);
            if (entity.isDirty == true) {
                return;
            }

            entity.world.dirtyEntities.Set(entity.entityId.id);
            entity.isDirty = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void RemoveTransfer(this Entity entity, int typeId) {
            if (entity.previousVirtualArchetype == null) {
                entity.previousVirtualArchetype = entity.virtualArchetype;
            }

            entity.virtualArchetype = entity.virtualArchetype.RemoveTransfer(typeId, entity.world);
            if (entity.isDirty == true) {
                return;
            }

            entity.world.dirtyEntities.Set(entity.entityId.id);
            entity.isDirty = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ApplyTransfer(this Entity entity) {
            // if (entity.virtualArchetype.level == 0) {
            //     entity.world.RemoveEntity(entity);
            //     return;
            // }

            if (entity.previousVirtualArchetype != entity.virtualArchetype) {
                if (entity.previousVirtualArchetype.level > 0) {
                    entity.previousVirtualArchetype.realArchetype?.Remove(entity);
                }
                
                if (entity.virtualArchetype.realArchetype == null) {
                    entity.virtualArchetype.CreateArchetype(entity.world);
                }
                entity.virtualArchetype.realArchetype.Add(entity);

                entity.previousVirtualArchetype = null;
            }

            entity.isDirty = false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void CreateArchetype(this VirtualArchetype node, World world) {
            var typeIds = new int[node.level];
            var currentNode = node;
            for (int i = node.level - 1; i >= 0; i--) {
                typeIds[i] = currentNode.typeId;
                currentNode = currentNode.parent;
            }

            var archetypeId = world.archetypes.length;
            node.realArchetype = new Archetype(archetypeId, typeIds, world);
            world.archetypes.Add(node.realArchetype);
            world.newArchetypes.Add(archetypeId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Dispose(this Entity entity) {
            if (entity.isDisposed) {
#if MORPEH_DEBUG
                MLogger.LogError($"You're trying to dispose disposed entity with ID {entity.entityId}.");
#endif
                return;
            }
            
            entity.world.ThreadSafetyCheck();
            
            var currentArchetype = entity.virtualArchetype.realArchetype;

            if (currentArchetype != null) {
                var stashes = currentArchetype.world.stashes;
                foreach (var typeId in currentArchetype.typeIds) {
                    if (stashes.TryGetValue(typeId, out var index)) {
                        Stash.stashes.data[index].Clean(entity);
                    }
                }
            }

            if (entity.previousVirtualArchetype != null) {
                entity.previousVirtualArchetype.realArchetype?.Remove(entity);
            }
            else {
                currentArchetype?.Remove(entity);
            }

            entity.world.ApplyRemoveEntity(entity.entityId.id);
            entity.world.dirtyEntities.Unset(entity.entityId.id);

            entity.DisposeFast();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void DisposeFast(this Entity entity) {
            entity.previousVirtualArchetype = null;

            entity.world            = null;
            entity.virtualArchetype = null;

            entity.entityId   = EntityId.Invalid;

            entity.isDirty    = false;
            entity.isDisposed = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsDisposed([NotNull] this Entity entity) => entity.isDisposed || entity.entityId == EntityId.Invalid;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNullOrDisposed([CanBeNull] this Entity entity) {
            if (entity != null) {
                if (entity.world != null) {
                    entity.world.ThreadSafetyCheck();
                    if (entity.isDisposed) {
                        return true;
                    }
                    return false;
                }
                return true;
            }
            return true;
        }
    }
}
