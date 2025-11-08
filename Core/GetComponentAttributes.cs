using System;

namespace MantenseiLib
{

    /// <summary>
    /// 指定した階層関係からコンポーネントを自動取得する属性
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class GetComponentAttribute : Attribute
    {
        public HierarchyRelation relation { get; set; } = HierarchyRelation.Self;
        public virtual QuantityType quantity { get; } = QuantityType.Single;
        public virtual GetComponentType GetComponentType { get; } = GetComponentType.GetComponent;

        public GetComponentAttribute() { }
        public GetComponentAttribute(HierarchyRelation relation) { this.relation = relation; }
    }

    /// <summary>
    /// 複数のコンポーネントを配列で自動取得する属性
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class GetComponentsAttribute : GetComponentAttribute
    {
        public GetComponentsAttribute() : base() { }
        public GetComponentsAttribute(HierarchyRelation relation) : base(relation) { }

        public override QuantityType quantity => QuantityType.Multiple;
    }

    /// <summary>
    /// コンポーネントが存在しない場合に自動で追加する属性
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
    public class ParentAttribute : Attribute
    {

    }

    /// <summary>
    /// parent配下のコンポーネントを1つ取得する属性
    /// （Self | Children）
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class SiblingAttribute : Attribute
    {

    }

    /// <summary>
    /// parent配下のコンポーネントを複数取得する属性（配列版）
    /// （Self | Children）
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class SiblingsAttribute : Attribute
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