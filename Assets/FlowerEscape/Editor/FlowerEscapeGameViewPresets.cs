using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FlowerEscape.Editor
{
    // Game View sizes live in each developer's local Unity settings, which Git does not track.
    // This menu recreates the two resolutions required by the assignment after a fresh clone.
    public static class FlowerEscapeGameViewPresets
    {
        private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        [MenuItem("Tools/Flower Escape/Restore Game View Presets")]
        public static void Restore()
        {
            try
            {
                var editorAssembly = typeof(EditorWindow).Assembly;
                var sizesType = editorAssembly.GetType("UnityEditor.GameViewSizes", true);
                var sizeType = editorAssembly.GetType("UnityEditor.GameViewSize", true);
                var sizeKindType = editorAssembly.GetType("UnityEditor.GameViewSizeType", true);
                var groupType = editorAssembly.GetType("UnityEditor.GameViewSizeGroupType", true);
                var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
                var sizes = singleton.GetProperty("instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    .GetValue(null);
                var group = sizesType.GetMethod("GetGroup", InstanceFlags)
                    .Invoke(sizes, new[] { Enum.Parse(groupType, "Standalone") });

                AddIfMissing(group, sizeType, sizeKindType, 1080, 2520, "Flower Escape 9:21 (1080x2520)");
                AddIfMissing(group, sizeType, sizeKindType, 900, 1200, "Flower Escape 3:4 (900x1200)");
                Debug.Log("[Flower Escape] Game View presets ready: 1080x2520 and 900x1200. Select one from the Game View resolution dropdown.");
            }
            catch (Exception exception)
            {
                Debug.LogError("[Flower Escape] Could not restore Game View presets in this Unity version: " + exception);
            }
        }

        private static void AddIfMissing(object group, Type sizeType, Type sizeKindType, int width, int height, string label)
        {
            var groupType = group.GetType();
            var count = (int)groupType.GetMethod("GetTotalCount", InstanceFlags).Invoke(group, null);
            var getSize = groupType.GetMethod("GetGameViewSize", InstanceFlags);
            for (var index = 0; index < count; index++)
            {
                var size = getSize.Invoke(group, new object[] { index });
                if (ReadDimension(size, "width", "m_Width") == width &&
                    ReadDimension(size, "height", "m_Height") == height)
                    return;
            }

            var kind = Enum.Parse(sizeKindType, "FixedResolution");
            var newSize = Activator.CreateInstance(sizeType, InstanceFlags, null,
                new object[] { kind, width, height, label }, null);
            groupType.GetMethod("AddCustomSize", InstanceFlags).Invoke(group, new[] { newSize });
        }

        private static int ReadDimension(object size, string propertyName, string fieldName)
        {
            var type = size.GetType();
            var property = type.GetProperty(propertyName, InstanceFlags);
            if (property != null) return (int)property.GetValue(size);
            var field = type.GetField(fieldName, InstanceFlags);
            if (field != null) return (int)field.GetValue(size);
            throw new MissingMemberException(type.FullName, propertyName);
        }
    }
}
