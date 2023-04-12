# if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
 
public class EnumMaskAttribute : PropertyAttribute
{
    public Type EnumType;
    public Enum Enum;
    public EnumMaskAttribute(Type enumType)
    {
        this.EnumType = enumType;
        this.Enum = (Enum)Enum.GetValues(enumType).GetValue(0);
    }
}
 
[CustomPropertyDrawer(typeof(EnumMaskAttribute))]
public class EnumMaskDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EnumMaskAttribute enumMaskAttribute = attribute as EnumMaskAttribute;
        enumMaskAttribute.Enum = EditorGUI.EnumFlagsField(position, label, enumMaskAttribute.Enum);
        if (enumMaskAttribute.EnumType == typeof(Enum))
        {
            Debug.Log(enumMaskAttribute.Enum);
            property.intValue = Convert.ToInt32(Enum.Parse(enumMaskAttribute.EnumType, Enum.GetName(enumMaskAttribute.EnumType, enumMaskAttribute.Enum)));
        }
        else
        {
            property.intValue = Convert.ToInt32(enumMaskAttribute.Enum);
        }
    }
}
#endif