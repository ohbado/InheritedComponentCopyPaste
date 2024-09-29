using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace OHBAEditor
{
    /// <summary>
    /// 親子関係にあるクラスのコンポーネント要素を可能な限りコピー
    /// </summary>
    public class InheritedComponentCopyPaste : UnityEditor.Editor
    {
        private static Component copiedComponent;

        // メニューを追加するための関数
        [MenuItem("CONTEXT/Component/Inherited Component Copy/Copy Value", false, 1500)]
        private static void CopyComponent(MenuCommand command)
        {
            copiedComponent = command.context as Component;
            Debug.Log("Component values copied: " + copiedComponent.GetType().Name);
        }

        [MenuItem("CONTEXT/Component/Inherited Component Copy/Paste Value", false, 1501)]
        private static void PasteComponent(MenuCommand command)
        {
            Component targetComponent = command.context as Component;
            if (copiedComponent == null)
            {
                Debug.LogWarning("No component values to paste.");
                return;
            }

            // 継承元クラスのフィールドのみをコピーする
            PasteBaseClassValues(copiedComponent, targetComponent);
            Debug.Log("Component values pasted from " + copiedComponent.GetType().Name + " to " + targetComponent.GetType().Name);
        }

        private static void PasteBaseClassValues(Component source, Component target)
        {
            var sourceType = source.GetType();
            var targetType = target.GetType();

            // sourceの継承階層を遡って、targetの型と一致するか確認
            var matchingBaseType = FindMatchingBaseOrParentType(sourceType, targetType);

            if (matchingBaseType == null)
            {
                Debug.LogWarning("No matching base class found between source and target.");
                return;
            }

            var sourceFields = GetFields(sourceType);
            var targetFields = GetFields(matchingBaseType);

            foreach (var field in sourceFields)
            {
                if (targetFields.ContainsKey(field.Key) && field.Value.FieldType == targetFields[field.Key].FieldType)
                {
                    var value = field.Value.GetValue(source);
                    targetFields[field.Key].SetValue(target, value);
                }
            }

            EditorUtility.SetDirty(target); // コンポーネントに変更を適用
        }

        /// <summary>
        /// sourceの型階層を遡り、targetTypeと一致する型か、もしくはその親型を探す
        /// </summary>
        /// <param name="sourceType"></param>
        /// <param name="targetType"></param>
        /// <returns></returns>
        private static System.Type FindMatchingBaseOrParentType(System.Type sourceType, System.Type targetType)
        {
            // コピー先がコピー元の継承元の場合
            if (IsAssignableFrom(targetType, sourceType))
            {
                return targetType;
            }

            // コピー元がコピー先の継承元の場合
            if (IsAssignableFrom(sourceType, targetType))
            {
                return sourceType;
            }

            return null; // 一致する型が見つからなかった場合
        }

        /// <summary>
        /// 型Aが型Bの継承元または同一型かを確認
        /// </summary>
        /// <param name="parentType"></param>
        /// <param name="childType"></param>
        /// <returns></returns>
        private static bool IsAssignableFrom(System.Type parentType, System.Type childType)
        {
            return parentType.IsAssignableFrom(childType);
        }

        private static Dictionary<string, FieldInfo> GetFields(System.Type type)
        {
            var fields = new Dictionary<string, FieldInfo>();
            while (type != null && type != typeof(MonoBehaviour))
            {
                foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    fields.TryAdd(field.Name, field);
                }

                type = type.BaseType;
            }

            return fields;
        }
    }
}