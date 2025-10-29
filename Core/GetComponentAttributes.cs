using System;

namespace MantenseiLib
{
    
    /// <summary>
    /// 
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class GetComponentAttribute : Attribute
    {
        public HierarchyRelation relation { get; set; } = HierarchyRelation.Self;
        public virtual QuantityType quantity { get; } = QuantityType.Single;
        public virtual GetComponentType GetComponentType { get; } = GetComponentType.GetComponent;
        public bool HideErrorHandling { get; set; } = false;

        public GetComponentAttribute() { }
        public GetComponentAttribute(HierarchyRelation relation) { this.relation = relation; }
    }

    /// <summary>
    /// 
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class GetComponentsAttribute : GetComponentAttribute
    {
        public GetComponentsAttribute() : base() { }
        public GetComponentsAttribute(HierarchyRelation relation) : base(relation){ }

        public override QuantityType quantity => QuantityType.Multiple;
    }

    /// <summary>
    /// 
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class AddComponentAttribute : GetComponentAttribute
    {
        public override GetComponentType GetComponentType { get => GetComponentType.AddComponent; }
    }

    /// <summary>
    /// 親オブジェクトへの参照を明示的にマークする属性
    /// [GetComponent(HierarchyRelation.Parent)] と同等
    /// 1コンポーネントにつき1つだけ許可される
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class OwnerAttribute : Attribute
    {
        
    }

    /// <summary>
    /// Owner配下のコンポーネントを1つ取得する属性
    /// Owner.GetComponent() と Owner.GetComponentInChildren() の両方を探索（Self | Children）
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class OwnedComponentAttribute : Attribute
    {
        
    }

    /// <summary>
    /// Owner配下のコンポーネントを複数取得する属性（配列版）
    /// Owner.GetComponents() と Owner.GetComponentsInChildren() の両方を探索（Self | Children）
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class OwnedComponentsAttribute : Attribute
    {
        
    }

    [Flags]
    public enum HierarchyRelation
    {
        Self = 1,    
        Parent = 2,  
        Children = 4,

        None = 0,
        All = Self | Parent | Children,
    }

    public enum GetComponentType
    {
        GetComponent,
        AddComponent,
    }

    public enum QuantityType
    {
        Single,
        Multiple
    }
}
