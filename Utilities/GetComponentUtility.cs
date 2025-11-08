using System;
using System.Reflection;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace MantenseiLib.GetComponent
{
    public static class GetComponentUtility
    {
        /// <summary>
        /// 3段階でコンポーネントを取得・設定する
        /// Phase 1: [parent] の取得
        /// Phase 2: [Sibling/Components] の取得
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

                // Phase 1: [parent] の取得
                ProcessparentAttributes(monoBehaviour, members);

                // Phase 2: [Sibling/Components] の取得
                ProcessSiblingAttributes(monoBehaviour, members);

                // Phase 3: その他の属性の取得
                ProcessStandardAttributes(monoBehaviour, members);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error processing GetComponent attributes in '{monoBehaviour.GetType().Name}' on '{monoBehaviour.name}': {ex.Message}");
            }
        }

        private static void ProcessparentAttributes(MonoBehaviour monoBehaviour, List<MemberInfo> members)
        {
            var parentMembers = members.Where(m => m.GetCustomAttribute<ParentAttribute>() != null).ToList();

            if (parentMembers.Count > 1)
            {
                Debug.LogWarning($"[parent] attribute found multiple times in '{monoBehaviour.GetType().Name}' on '{monoBehaviour.name}'. Using the first one.");
            }

            foreach (var memberInfo in parentMembers)
            {
                try
                {
                    Type componentType = memberInfo.GetMemberType();

                    // 親から取得（HierarchyRelation.Parent と同等）
                    object component = monoBehaviour.transform.parent?.GetComponentInParent(componentType);

                    if (Exists(component))
                    {
                        SetComponent(monoBehaviour, memberInfo, component);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error processing [parent] member '{memberInfo.Name}' in '{monoBehaviour.GetType().Name}' on '{monoBehaviour.name}': {ex.Message}");
                }
            }
        }

        private static void ProcessSiblingAttributes(MonoBehaviour monoBehaviour, List<MemberInfo> members)
        {
            var parentMember = members.FirstOrDefault(m => m.GetCustomAttribute<ParentAttribute>() != null);
            if (parentMember == null)
            {
                return;
            }

            object parentValue = GetMemberValue(monoBehaviour, parentMember);
            if (!Exists(parentValue))
            {
                return;
            }

            Component parentComponent = parentValue as Component;

            // インターフェイス型の場合も対応
            if (!Exists(parentComponent) && Exists(parentValue))
            {
                // GetComponent<T>()でインターフェイスを取得した場合、
                // 返り値はインターフェイス型だが実体はMonoBehaviourなので変換を試みる
                parentComponent = parentValue as MonoBehaviour;
            }

            if (!Exists(parentComponent))
            {
                Debug.LogWarning($"[parent] value in '{monoBehaviour.GetType().Name}' on '{monoBehaviour.name}' is not a Component. Sibling attributes will be skipped.");
                return;
            }

            GameObject parentGameObject = parentComponent.gameObject;

            // [Sibling] (単一)
            ProcessSiblingSingle(monoBehaviour, members, parentGameObject);

            // [Siblings] (複数)
            ProcessSiblingMultiple(monoBehaviour, members, parentGameObject);
        }

        private static void ProcessSiblingSingle(MonoBehaviour monoBehaviour, List<MemberInfo> members, GameObject parentGameObject)
        {
            var ownedMembers = members.Where(m => m.GetCustomAttribute<SiblingAttribute>() != null).ToList();

            foreach (var memberInfo in ownedMembers)
            {
                try
                {
                    Type componentType = memberInfo.GetMemberType();

                    // parent自身（Self）からまず探す
                    object component = parentGameObject.GetComponent(componentType);

                    // なければ子階層（Children）から探す
                    if (!Exists(component))
                    {
                        component = parentGameObject.GetComponentInChildren(componentType);
                    }

                    if (Exists(component))
                    {
                        SetComponent(monoBehaviour, memberInfo, component);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error processing [Sibling] member '{memberInfo.Name}' in '{monoBehaviour.GetType().Name}' on '{monoBehaviour.name}': {ex.Message}");
                }
            }
        }

        private static void ProcessSiblingMultiple(MonoBehaviour monoBehaviour, List<MemberInfo> members, GameObject parentGameObject)
        {
            var ownedMembers = members.Where(m => m.GetCustomAttribute<SiblingsAttribute>() != null).ToList();

            foreach (var memberInfo in ownedMembers)
            {
                try
                {
                    Type componentType = memberInfo.GetMemberType();
                    Type elementType = componentType.IsArray ? componentType.GetElementType() : componentType;

                    // parent自身（Self）と子階層（Children）の両方から取得
                    List<Component> componentList = new List<Component>();

                    // Self
                    componentList.AddRange(parentGameObject.GetComponents(elementType));

                    // Children
                    componentList.AddRange(parentGameObject.GetComponentsInChildren(elementType));

                    // 重複を除去
                    componentList = componentList.Distinct().ToList();

                    // 配列に変換して設定
                    Array componentsArray = Array.CreateInstance(elementType, componentList.Count);
                    componentList.ToArray().CopyTo(componentsArray, 0);

                    SetComponent(monoBehaviour, memberInfo, componentsArray);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error processing [Siblings] member '{memberInfo.Name}' in '{monoBehaviour.GetType().Name}' on '{monoBehaviour.name}': {ex.Message}");
                }
            }
        }

        private static void ProcessStandardAttributes(MonoBehaviour monoBehaviour, List<MemberInfo> members)
        {
            // [parent]、[Sibling]、[Siblings] 以外の属性を処理
            var standardMembers = members.Where(m =>
            {
                var hasparent = m.GetCustomAttribute<ParentAttribute>() != null;
                var hasSibling = m.GetCustomAttribute<SiblingAttribute>() != null;
                var hasSiblings = m.GetCustomAttribute<SiblingsAttribute>() != null;
                var hasGetComponent = m.GetCustomAttribute<GetComponentAttribute>() != null;

                return !hasparent && !hasSibling && !hasSiblings && hasGetComponent;
            }).ToList();

            foreach (var memberInfo in standardMembers)
            {
                var attribute = memberInfo.GetCustomAttribute<GetComponentAttribute>();
                if (attribute == null) continue;

                // 修正後
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

                    // 単一コンポーネントの場合
                    if (attribute.quantity == QuantityType.Single)
                    {
                        if (Exists(component))
                        {
                            SetComponent(monoBehaviour, memberInfo, component);
                        }
                    }
                    // 配列の場合
                    else if (attribute.quantity == QuantityType.Multiple)
                    {
                        if (component is Array array && array.Length > 0)
                        {
                            SetComponent(monoBehaviour, memberInfo, component);
                        }
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
                        if (Exists(components))
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
                    componentList.ToArray().CopyTo(componentsArray, 0);

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
                            if (Exists(component)) return component;
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

        // UnityEngine.Objectの安全なnullチェック
        static bool Exists(object obj)
        {
            return obj as Object != null;
        }
    }
}