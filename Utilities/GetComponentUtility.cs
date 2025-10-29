using System;
using System.Reflection;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace MantenseiLib.GetComponent
{
    public static class GetComponentUtility
    {
        /// <summary>
        /// 3段階でコンポーネントを取得・設定する
        /// Phase 1: [Owner] の取得
        /// Phase 2: [OwnedComponent/Components] の取得
        /// Phase 3: その他の属性の取得
        /// </summary>
        public static void GetOrAddComponent(MonoBehaviour monoBehaviour)
        {
            try
            {
                var type = monoBehaviour.GetType();
                var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var members = fields.Cast<MemberInfo>().Concat(properties.Cast<MemberInfo>()).ToList();

                // Phase 1: [Owner] の取得
                ProcessOwnerAttributes(monoBehaviour, members);

                // Phase 2: [OwnedComponent/Components] の取得
                ProcessOwnedComponentAttributes(monoBehaviour, members);

                // Phase 3: その他の属性の取得
                ProcessStandardAttributes(monoBehaviour, members);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error processing GetComponent attributes in '{monoBehaviour.GetType().Name}' on '{monoBehaviour.name}': {ex.Message}");
            }
        }

        private static void ProcessOwnerAttributes(MonoBehaviour monoBehaviour, List<MemberInfo> members)
        {
            var ownerMembers = members.Where(m => m.GetCustomAttribute<OwnerAttribute>() != null).ToList();

            // 複数の[Owner]がある場合は警告
            if (ownerMembers.Count > 1)
            {
                Debug.LogError($"[Owner] attribute found multiple times in '{monoBehaviour.GetType().Name}' on '{monoBehaviour.name}'. Only one [Owner] is allowed per component.");
                return;
            }

            foreach (var memberInfo in ownerMembers)
            {
                try
                {
                    Type componentType = memberInfo.GetMemberType();

                    // 親から取得（HierarchyRelation.Parent と同等）
                    object component = monoBehaviour.transform.parent?.GetComponentInParent(componentType);

                    if (component != null)
                    {
                        SetComponent(monoBehaviour, memberInfo, component);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error processing [Owner] member '{memberInfo.Name}' in '{monoBehaviour.GetType().Name}' on '{monoBehaviour.name}': {ex.Message}");
                }
            }
        }

        private static void ProcessOwnedComponentAttributes(MonoBehaviour monoBehaviour, List<MemberInfo> members)
        {
            var ownerMember = members.FirstOrDefault(m => m.GetCustomAttribute<OwnerAttribute>() != null);
            if (ownerMember == null)
            {
                // [Owner]がない場合、[OwnedComponent]系は処理しない
                return;
            }

            object ownerValue = GetMemberValue(monoBehaviour, ownerMember);
            if (ownerValue == null)
            {
                // Ownerがnullの場合、[OwnedComponent]系はスキップ
                return;
            }

            Component ownerComponent = ownerValue as Component;
            if (ownerComponent == null)
            {
                return;
            }

            GameObject ownerGameObject = ownerComponent.gameObject;

            // [OwnedComponent] (単一)
            ProcessOwnedComponentSingle(monoBehaviour, members, ownerGameObject);

            // [OwnedComponents] (複数)
            ProcessOwnedComponentMultiple(monoBehaviour, members, ownerGameObject);
        }

        private static void ProcessOwnedComponentSingle(MonoBehaviour monoBehaviour, List<MemberInfo> members, GameObject ownerGameObject)
        {
            var ownedMembers = members.Where(m => m.GetCustomAttribute<OwnedComponentAttribute>() != null).ToList();

            foreach (var memberInfo in ownedMembers)
            {
                try
                {
                    Type componentType = memberInfo.GetMemberType();

                    // Owner自身（Self）からまず探す
                    object component = ownerGameObject.GetComponent(componentType);

                    // なければ子階層（Children）から探す
                    if (component == null)
                    {
                        component = ownerGameObject.GetComponentInChildren(componentType);
                    }

                    if (component != null)
                    {
                        SetComponent(monoBehaviour, memberInfo, component);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error processing [OwnedComponent] member '{memberInfo.Name}' in '{monoBehaviour.GetType().Name}' on '{monoBehaviour.name}': {ex.Message}");
                }
            }
        }

        private static void ProcessOwnedComponentMultiple(MonoBehaviour monoBehaviour, List<MemberInfo> members, GameObject ownerGameObject)
        {
            var ownedMembers = members.Where(m => m.GetCustomAttribute<OwnedComponentsAttribute>() != null).ToList();

            foreach (var memberInfo in ownedMembers)
            {
                try
                {
                    Type componentType = memberInfo.GetMemberType();
                    Type elementType = componentType.IsArray ? componentType.GetElementType() : componentType;

                    // Owner自身（Self）と子階層（Children）の両方から取得
                    List<Component> componentList = new List<Component>();

                    // Self
                    componentList.AddRange(ownerGameObject.GetComponents(elementType));

                    // Children
                    componentList.AddRange(ownerGameObject.GetComponentsInChildren(elementType));

                    // 重複を除去
                    componentList = componentList.Distinct().ToList();

                    // 配列に変換して設定
                    Array componentsArray = Array.CreateInstance(elementType, componentList.Count);
                    componentList.ToArray().CopyTo(componentsArray, 0);

                    SetComponent(monoBehaviour, memberInfo, componentsArray);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error processing [OwnedComponents] member '{memberInfo.Name}' in '{monoBehaviour.GetType().Name}' on '{monoBehaviour.name}': {ex.Message}");
                }
            }
        }

        private static void ProcessStandardAttributes(MonoBehaviour monoBehaviour, List<MemberInfo> members)
        {
            // [Owner]、[OwnedComponent]、[OwnedComponents] 以外の属性を処理
            var standardMembers = members.Where(m =>
            {
                var hasOwner = m.GetCustomAttribute<OwnerAttribute>() != null;
                var hasOwnedComponent = m.GetCustomAttribute<OwnedComponentAttribute>() != null;
                var hasOwnedComponents = m.GetCustomAttribute<OwnedComponentsAttribute>() != null;
                var hasGetComponent = m.GetCustomAttribute<GetComponentAttribute>() != null;

                return !hasOwner && !hasOwnedComponent && !hasOwnedComponents && hasGetComponent;
            }).ToList();

            foreach (var memberInfo in standardMembers)
            {
                var attribute = memberInfo.GetCustomAttribute<GetComponentAttribute>();
                if (attribute == null) continue;

                try
                {
                    object component = null;

                    switch (attribute.GetComponentType)
                    {
                        case GetComponentType.GetComponent:
                            component = GetComponentByRelations(monoBehaviour, memberInfo, attribute.relation, attribute.quantity);
                            break;

                        case GetComponentType.AddComponent:
                            component = AddComponentByRelation(monoBehaviour, memberInfo, attribute.relation);
                            break;
                    }

                    if (component != null)
                    {
                        SetComponent(monoBehaviour, memberInfo, component);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error processing member '{memberInfo.Name}' in '{monoBehaviour.GetType().Name}' on '{monoBehaviour.name}': {ex.Message}");
                }
            }
        }

        private static object GetComponentByRelations(MonoBehaviour obj, MemberInfo memberInfo, HierarchyRelation relations, QuantityType quantity)
        {
            Type componentType = memberInfo.GetMemberType();
            bool isArray = componentType.IsArray;
            Type elementType = isArray ? componentType.GetElementType() : componentType;

            object components = null;

            if (quantity == QuantityType.Single)
            {
                foreach (HierarchyRelation relation in Enum.GetValues(typeof(HierarchyRelation)))
                {
                    if (relation != HierarchyRelation.None && relations.HasFlag(relation))
                    {
                        components = GetComponentByRelation(obj, elementType, relation, quantity);
                        if (components as UnityEngine.Object != null)
                        {
                            break;
                        }
                    }
                }
            }
            else if (quantity == QuantityType.Multiple)
            {
                List<object> componentList = new List<object>();

                foreach (HierarchyRelation relation in Enum.GetValues(typeof(HierarchyRelation)))
                {
                    if (relation != HierarchyRelation.None && relations.HasFlag(relation))
                    {
                        object tempComponents = GetComponentByRelation(obj, elementType, relation, quantity);
                        if (tempComponents is Array tempArray)
                        {
                            foreach (var item in tempArray)
                            {
                                componentList.Add(item);
                            }
                        }
                    }
                }

                if (componentList.Count > 0)
                {
                    componentList = componentList.Distinct().ToList();
                    Array componentsArray = Array.CreateInstance(elementType, componentList.Count);
                    componentList.Distinct().ToArray().CopyTo(componentsArray, 0);

                    components = componentsArray;
                }
            }

            return components;
        }

        private static object GetComponentByRelation(MonoBehaviour obj, Type elementType, HierarchyRelation relation, QuantityType quantity)
        {
            switch (relation)
            {
                case HierarchyRelation.Parent:
                    return quantity == QuantityType.Single ? obj.transform.parent?.GetComponentInParent(elementType)
                                                           : obj.transform.parent?.GetComponentsInParent(elementType);

                case HierarchyRelation.Children:
                    if (quantity == QuantityType.Single)
                    {
                        foreach (Transform child in obj.transform)
                        {
                            var component = child.GetComponentInChildren(elementType);
                            if (component != null) return component;
                        }
                        return null;
                    }
                    else
                    {
                        List<Component> components = new List<Component>();
                        foreach (Transform child in obj.transform)
                        {
                            components.AddRange(child.GetComponentsInChildren(elementType));
                        }
                        return components.ToArray();
                    }

                case HierarchyRelation.Self:
                default:
                    return quantity == QuantityType.Single ? obj.GetComponent(elementType)
                                                           : obj.GetComponents(elementType);
            }
        }

        private static object AddComponentByRelation(MonoBehaviour obj, MemberInfo memberInfo, HierarchyRelation relation)
        {
            if (relation == HierarchyRelation.Self)
            {
                return obj.gameObject.AddComponent(memberInfo.GetMemberType());
            }
            else
            {
                Debug.LogWarning("AddComponent to parent or children is not supported.");
                return null;
            }
        }

        private static void SetComponent(MonoBehaviour obj, MemberInfo memberInfo, object component)
        {
            try
            {
                if (memberInfo is FieldInfo fieldInfo)
                {
                    Type fieldType = fieldInfo.FieldType;
                    if (fieldType.IsArray && component is Array componentArray)
                    {
                        Array array = Array.CreateInstance(fieldType.GetElementType(), componentArray.Length);
                        Array.Copy(componentArray, array, componentArray.Length);
                        fieldInfo.SetValue(obj, array);
                    }
                    else
                    {
                        fieldInfo.SetValue(obj, component);
                    }
                }
                else if (memberInfo is PropertyInfo propertyInfo)
                {
                    var setMethod = propertyInfo.GetSetMethod(true);
                    if (setMethod != null)
                    {
                        Type propertyType = propertyInfo.PropertyType;
                        if (propertyType.IsArray && component is Array componentArray)
                        {
                            Array array = Array.CreateInstance(propertyType.GetElementType(), componentArray.Length);
                            Array.Copy(componentArray, array, componentArray.Length);
                            setMethod.Invoke(obj, new[] { array });
                        }
                        else
                        {
                            setMethod.Invoke(obj, new[] { component });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error setting component to '{memberInfo.Name}' in '{obj.GetType().Name}' on '{obj.name}': {ex.Message}");
            }
        }

        private static object GetMemberValue(object obj, MemberInfo memberInfo)
        {
            if (memberInfo is FieldInfo fieldInfo)
            {
                return fieldInfo.GetValue(obj);
            }
            else if (memberInfo is PropertyInfo propertyInfo)
            {
                return propertyInfo.GetValue(obj);
            }
            return null;
        }

        public static Type GetMemberType(this MemberInfo memberInfo)
        {
            if (memberInfo is FieldInfo fieldInfo)
            {
                return fieldInfo.FieldType;
            }
            else if (memberInfo is PropertyInfo propertyInfo)
            {
                return propertyInfo.PropertyType;
            }
            return null;
        }
    }
}