using System;

namespace MantenseiLib
{
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

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class GetComponentsAttribute : GetComponentAttribute
    {
        public GetComponentsAttribute() : base() { }
        public GetComponentsAttribute(HierarchyRelation relation) : base(relation){ }

        public override QuantityType quantity => QuantityType.Multiple;
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class AddComponentAttribute : GetComponentAttribute
    {
        public override GetComponentType GetComponentType { get => GetComponentType.AddComponent; }
    }

    /// <summary>
    /// �e�I�u�W�F�N�g�ւ̎Q�Ƃ𖾎��I�Ƀ}�[�N���鑮��
    /// [GetComponent(HierarchyRelation.Parent)] �Ɠ���
    /// 1�R���|�[�l���g�ɂ�1�����������
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class OwnerAttribute : Attribute
    {
        
    }

    /// <summary>
    /// Owner�z���̃R���|�[�l���g��1�擾���鑮��
    /// Owner.GetComponent() �� Owner.GetComponentInChildren() �̗�����T���iSelf | Children�j
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class OwnedComponentAttribute : Attribute
    {
        
    }

    /// <summary>
    /// Owner�z���̃R���|�[�l���g�𕡐��擾���鑮���i�z��Łj
    /// Owner.GetComponents() �� Owner.GetComponentsInChildren() �̗�����T���iSelf | Children�j
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
