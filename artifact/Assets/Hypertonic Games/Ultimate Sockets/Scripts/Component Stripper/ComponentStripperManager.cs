using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Hypertonic.Modules.Utilities
{
    public static class ComponentStripperManager
    {
        /// <summary>
        /// Duplicates the game object and strips all components from the gameobject and all nested game objects.
        /// </summary>
        /// <param name="original"> The object to clone and strip </param>
        /// <returns>A copy of the original gameobject with all components stripped off</returns>
        public static GameObject DuplicateAndStrip(GameObject original)
        {
            return DuplicateAndStrip(original, new List<Type>());
        }

        /// <summary>
        /// Duplicates the game object and strips all components from the gameobject and all nested game objects.
        /// </summary>
        /// <param name="original">The object to clone and strip </param>
        /// <param name="componentsToPersist">A list of components that shouldn't be stripped</param>
        /// <returns>A copy of the original gameobject with all components stripped off</returns>
        public static GameObject DuplicateAndStrip(GameObject original, List<Type> componentsToPersist)
        {
            GameObject strippedGameObject = GameObject.Instantiate(original);
          
            return StripComponents(strippedGameObject, componentsToPersist);
        }

        /// <summary>
        /// Strips all of the components off a game object and all nested game objects
        /// </summary>
        /// <param name="original"> The game object to strip </param>
        /// <returns>The stripped game object </returns>
        public static GameObject StripComponents(GameObject original)
        {
            return StripComponents(original, new List<Type>());
        }

         /// <summary>
        /// Strips all of the components off a game object and all nested game objects unless the component is in the components to persist list
        /// </summary>
        /// <param name="original"> The game object to strip </param>
        /// <param name="componentsToPersist">A list of components that shouldn't be stripped</param>
        /// <returns>The stripped game object </returns>
        public static GameObject StripComponents(GameObject original, List<Type> componentsToPersist)
        {
            componentsToPersist = ValidateComponentsToPersist(componentsToPersist);

            StripComponentsFromGameObject(original, componentsToPersist);

            return original;
        }

        /// <summary>
        /// Removes just the specified types from the game object.
        /// </summary>
        /// <param name="original">The object to strip</param>
        /// <param name="componentsToStrip">The components to strip from the game object</param>
        /// <returns> The stripped game object </returns>
        public static GameObject StripSpecificComponents(GameObject original, List<Type> componentsToStrip)
        {
            StripComponentsFromGameObject(original, new List<Type>(), componentsToStrip);

            return original;
        }

        /// <summary>
        /// Duplicates the game object, then removes just the specified types from the copy.
        /// </summary>
        /// <param name="original">The object to duplicate and strip</param>
        /// <param name="componentsToStrip">The components to strip from the game object</param>
        /// <returns>A copy of the original gameobject with all components stripped off</returns>
        public static GameObject DuplicateAndStripSpecificComponents(GameObject original, List<Type> componentsToStrip)
        {
            GameObject strippedGameObject = GameObject.Instantiate(original);

            StripComponentsFromGameObject(strippedGameObject, new List<Type>(), componentsToStrip);

            return original;
        }

        /// <summary>
        /// A wrapper function that acts as a single entry point to the strip components recusive function, that allows for additional code to be executed once after the stripping
        /// process has finished
        /// </summary>
        /// <param name="gameObjectToStrip">The object to strip components from</param>
        /// <param name="componentsToPersist">The components to be kept</param>
        /// <param name="componentsToStrip">The specific components to remove </param>
        private static void StripComponentsFromGameObject(GameObject gameObjectToStrip, List<Type> componentsToPersist, List<Type> componentsToStrip = null)
        {
            StripComponentsFromGameObjectRecursive(gameObjectToStrip, componentsToPersist, componentsToStrip);

#if UNITY_EDITOR
            EditorUtility.SetDirty(gameObjectToStrip);
#endif
        }

        private static void StripComponentsFromGameObjectRecursive(GameObject gameObjectToStrip, List<Type> componentsToPersist, List<Type> componentsToStrip = null)
        {
            List<Component> components = gameObjectToStrip.GetComponents<Component>().ToList();

            Dictionary<Type, List<Type>> requiresDictionary = new Dictionary<Type, List<Type>>();
            Dictionary<Type, List<Component>> requiredByDictionary = new Dictionary<Type, List<Component>>();

            GenerateDependancyDictionaries(components, out requiresDictionary, out requiredByDictionary);

            AddDependantsToPersistList(components, requiresDictionary, componentsToPersist);

            if(componentsToStrip == null)
            {
                components = components.Where(x => !componentsToPersist.Contains(x.GetType())).ToList();
            }
            else
            {
                components = components.Where(x => !componentsToPersist.Contains(x.GetType()) && componentsToStrip.Contains(x.GetType())).ToList();
            }

            foreach (Component component in components)
            {
                DeleteDependants(component, requiredByDictionary, componentsToPersist);

                if (Application.isPlaying)
                {
                    GameObject.Destroy(component);
                }
                else
                {
                    GameObject.DestroyImmediate(component);
                }
            }

            foreach (Transform t in gameObjectToStrip.transform)
            {
                StripComponentsFromGameObject(t.gameObject, componentsToPersist, componentsToStrip);
            }
        }

        private static bool HasDependant(Component component, Dictionary<Type, List<Component>> requiredBy)
        {
            bool containsKey = requiredBy.ContainsKey(component.GetType());

            // If the key exists return true straight away
            if (containsKey)
            {
                return true;
            }

            // Check if the type is an inherited type
            foreach (Type type in requiredBy.Keys)
            {
                if (component.GetType().IsAssignableFrom(type))
                {
                    bool hasDependant = requiredBy.ContainsKey(type);

                    if (hasDependant)
                    {
                        return true;
                    }
                }
            }

            foreach (Type type in requiredBy.Keys)
            {
                if (component.GetType().IsSubclassOf(type))
                {
                    bool hasDependant = requiredBy.ContainsKey(type);

                    if (hasDependant)
                    {
                        return true;
                    }
                }
            }


            return false;
        }

        private static List<Component> GetDependants(Component component, Dictionary<Type, List<Component>> requiredBy)
        {
            bool containsKey = requiredBy.ContainsKey(component.GetType());

            if (containsKey)
            {
                return requiredBy[component.GetType()];
            }

            foreach (Type type in requiredBy.Keys)
            {
                if (component.GetType().IsAssignableFrom(type))
                {
                    return requiredBy[type];
                }
            }

            foreach (Type type in requiredBy.Keys)
            {
                if (component.GetType().IsSubclassOf(type))
                {
                    return requiredBy[type];
                }
            }

            return null;
        }

        private static void DeleteDependants(Component component, Dictionary<Type, List<Component>> requiredBy, List<Type> componentsToPersist)
        {
            bool hasDependant = HasDependant(component, requiredBy);

            if (!hasDependant)
            {
                if (!IsContainedInPersistedList(component.GetType(), componentsToPersist))
                {
                    if (Application.isPlaying)
                    {
                        GameObject.Destroy(component);
                    }
                    else
                    {
                        GameObject.DestroyImmediate(component);
                    }
                }
            }
            else
            {
                List<Component> dependants = GetDependants(component, requiredBy);

                if (dependants == null)
                {
                    Debug.LogError("Dependants have come back as null. Ensure the component has dependants");
                    return;
                }

                for (int i = dependants.Count - 1; i >= 0; i--)
                {
                    DeleteDependants(dependants[i], requiredBy, componentsToPersist);
                }

                if (!IsContainedInPersistedList(component.GetType(), componentsToPersist))
                {
                    if (Application.isPlaying)
                    {
                        GameObject.Destroy(component);
                    }
                    else
                    {
                        GameObject.DestroyImmediate(component);
                    }
                }
            }
        }

        private static void AddDependantsToPersistList(List<Component> components, Dictionary<Type, List<Type>> requiresDictionary, List<Type> componentsToPersist)
        {
            foreach (Component component in components)
            {
                if (componentsToPersist.Contains(component.GetType()))
                {
                    AddDependantsToPersistList(component.GetType(), requiresDictionary, componentsToPersist);
                }
            }
        }

        private static void AddDependantsToPersistList(Type componentType, Dictionary<Type, List<Type>> requiresDictionary, List<Type> componentsToPersist)
        {
            if (!requiresDictionary.ContainsKey(componentType))
            {
                return;
            }

            List<Type> dependants = requiresDictionary[componentType];

            foreach (Type dependant in dependants)
            {
                AddTypeToPersistList(dependant, componentsToPersist);

                AddDependantsToPersistList(dependant, requiresDictionary, componentsToPersist);
            }
        }

        private static void AddTypeToPersistList(Type type, List<Type> componentsToPersist)
        {
            if (!componentsToPersist.Contains(type))
            {
                componentsToPersist.Add(type);
            }
        }


        private static List<Type> ValidateComponentsToPersist(List<Type> componentsToPersist)
        {
            if (componentsToPersist == null)
            {
                Debug.LogError("Components to persist is null");
                return null;
            }

            if (!componentsToPersist.Contains(typeof(Transform)))
            {
                componentsToPersist.Add(typeof(Transform));
            }

            if (!componentsToPersist.Contains(typeof(RectTransform)))
            {
                componentsToPersist.Add(typeof(RectTransform));
            }

            return componentsToPersist;
        }

        private static bool HasRequireComponentAttribute(Component component, out List<RequireComponent> requireComponents)
        {
            requireComponents = new List<RequireComponent>();

            foreach (object attribute in component.GetType().GetCustomAttributes(true))
            {
                if (attribute is RequireComponent)
                {
                    requireComponents.Add(attribute as RequireComponent);
                }
            }

            if (requireComponents.Count > 0)
            {
                return true;
            }

            return false;
        }

        private static void AddToRequiredByDictionary(Component component, List<RequireComponent> requireComponents, Dictionary<Type, List<Component>> requiredByList)
        {
            foreach (RequireComponent requireComponent in requireComponents)
            {
                AddToRequiredByDictionary(component, requireComponent, requiredByList);
            }
        }

        private static void AddToRequiredByDictionary(Component component, RequireComponent requireComponent, Dictionary<Type, List<Component>> requiredByList)
        {
            if (requireComponent.m_Type0 != null)
            {
                AddToRequiredByDictionary(requireComponent.m_Type0, requiredByList, component);
            }

            if (requireComponent.m_Type1 != null)
            {
                AddToRequiredByDictionary(requireComponent.m_Type1, requiredByList, component);
            }

            if (requireComponent.m_Type2 != null)
            {
                AddToRequiredByDictionary(requireComponent.m_Type2, requiredByList, component);
            }
        }

        private static void AddToRequiredByDictionary(Type key, Dictionary<Type, List<Component>> requiredByList, Component requiredBy)
        {
            if (key == typeof(Transform) || requiredBy.GetType() == typeof(Transform))
                return;


            if (requiredByList.ContainsKey(key))
            {
                requiredByList[key].Add(requiredBy);
            }
            else
            {
                requiredByList.Add(key, new List<Component> { requiredBy });
            }
        }

        private static void AddToRequiresDictionary(Component component, List<RequireComponent> requireComponents, Dictionary<Type, List<Type>> requiresDictionary)
        {
            foreach (RequireComponent requireComponent in requireComponents)
            {
                AddToRequiresDictionary(component, requireComponent, requiresDictionary);
            }
        }

        private static void AddToRequiresDictionary(Component component, RequireComponent requireComponent, Dictionary<Type, List<Type>> requiresDictionary)
        {
            if (requireComponent.m_Type0 != null)
            {
                AddToRequiresDictionary(component.GetType(), requiresDictionary, requireComponent.m_Type0);
            }

            if (requireComponent.m_Type1 != null)
            {
                AddToRequiresDictionary(component.GetType(), requiresDictionary, requireComponent.m_Type1);
            }

            if (requireComponent.m_Type2 != null)
            {
                AddToRequiresDictionary(component.GetType(), requiresDictionary, requireComponent.m_Type1);
            }
        }

        private static void AddToRequiresDictionary(Type key, Dictionary<Type, List<Type>> requireDictionary, Type requires)
        {
            if (key == typeof(Transform) || requires == typeof(Transform))
                return;

            if (requireDictionary.ContainsKey(key))
            {
                requireDictionary[key].Add(requires);
            }
            else
            {
                requireDictionary.Add(key, new List<Type> { requires });
            }
        }


        private static void GenerateDependancyDictionaries(List<Component> components, out Dictionary<Type, List<Type>> requires, out Dictionary<Type, List<Component>> requiredBy)
        {
            requires = new Dictionary<Type, List<Type>>();
            requiredBy = new Dictionary<Type, List<Component>>();

            foreach (Component component in components)
            {
                if (HasRequireComponentAttribute(component, out List<RequireComponent> requireComponents))
                {
                    AddToRequiresDictionary(component, requireComponents, requires);
                    AddToRequiredByDictionary(component, requireComponents, requiredBy);
                }
            }
        }

        private static bool IsContainedInPersistedList(Type type, List<Type> typesToPersist)
        {
            if (typesToPersist.Contains(type))
            {
                return true;
            }

            foreach (Type typeToPersist in typesToPersist)
            {
                if (type.IsAssignableFrom(typeToPersist))
                {
                    return true;
                }
            }

            foreach (Type typeToPersist in typesToPersist)
            {
                if (typeToPersist.IsAssignableFrom(type))
                {
                    return true;
                }
            }

            foreach (Type typeToPersist in typesToPersist)
            {
                if (type.IsSubclassOf(typeToPersist))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
