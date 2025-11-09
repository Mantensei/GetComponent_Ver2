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
    /// 同クラス内につき1つだけ許可される
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ParentAttribute : GetComponentAttribute
    {
        public ParentAttribute() : base(HierarchyRelation.Parent) { }
    }

    /// <summary>
    /// parent配下のコンポーネントを1つ取得する属性
    /// parent自身を含む子要素を取得（Self | Children）
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class SiblingAttribute : GetComponentAttribute
    {
        public SiblingAttribute() : base(HierarchyRelation.Self | HierarchyRelation.Children) { }
    }

    /// <summary>
    /// parent配下のコンポーネントを複数取得する属性（配列版）
    /// parent自身を含む子要素を取得（Self | Children）
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class SiblingsAttribute : GetComponentAttribute
    {
        public SiblingsAttribute() : base(HierarchyRelation.Self | HierarchyRelation.Children) { }
        public override QuantityType quantity => QuantityType.Multiple;
    }

    /// <summary>
    /// コンポーネント取得時の階層関係
    /// (HierarcheyRelation.Self | HierarchyRelation.Parent)など複数指定可能
    /// </summary>
    [Flags]
    public enum HierarchyRelation
    {
        /// <summary>自身</summary>
        Self = 1,
        /// <summary>親</summary>
        Parent = 2,
        /// <summary>子</summary>
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