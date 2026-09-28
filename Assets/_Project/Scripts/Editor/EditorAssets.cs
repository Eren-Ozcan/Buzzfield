using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Buzzfield.Editor
{
    /// <summary>Helpers for creating assets once and writing private serialized fields.</summary>
    internal static class EditorAssets
    {
        public const string Root = "Assets/_Project";
        public const string DataRoot = Root + "/ScriptableObjects";
        public const string PrefabRoot = Root + "/Prefabs";
        public const string MaterialRoot = Root + "/Materials";
        public const string ScenePath = Root + "/Scenes/Main.unity";

        /// <summary>Loads the asset at <paramref name="path"/>, or creates it and runs <paramref name="init"/> once.</summary>
        public static T LoadOrCreate<T>(string path, Action<T> init) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
                return existing;

            EnsureFolder(Path.GetDirectoryName(path));
            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            init(asset);
            EditorUtility.SetDirty(asset);
            Debug.Log($"Created {path}");
            return asset;
        }

        public static Material LoadOrCreateMaterial(string name, Color color, string shaderName = "Universal Render Pipeline/Simple Lit")
        {
            string path = $"{MaterialRoot}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
                return existing;

            EnsureFolder(MaterialRoot);
            Shader shader = Shader.Find(shaderName);
            var material = new Material(shader) { name = name };
            material.SetColor("_BaseColor", color);
            material.enableInstancing = true;
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        /// <summary>Loads the prefab, or builds a temporary object, saves it as the prefab and destroys the temporary.</summary>
        public static GameObject LoadOrCreatePrefab(string name, Func<GameObject> build)
        {
            string path = $"{PrefabRoot}/{name}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
                return existing;

            EnsureFolder(PrefabRoot);
            GameObject temp = build();
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, path);
            Object.DestroyImmediate(temp);
            return prefab;
        }

        /// <summary>A primitive without its collider (bees and flowers never use physics).</summary>
        public static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        public static void EnsureFolder(string folder)
        {
            folder = folder.Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(folder))
                return;
            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }

        /// <summary>Writes serialized fields by name; values are float, int, double, bool, string, Color, Vector2, Vector2Int, enums or Objects.</summary>
        public static void Set(Object target, params (string field, object value)[] values)
        {
            var so = new SerializedObject(target);
            foreach ((string field, object value) in values)
            {
                SerializedProperty property = so.FindProperty(field);
                if (property == null)
                    throw new ArgumentException($"{target.GetType().Name} has no serialized field '{field}'.");
                Assign(property, value);
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Sets an object reference only while it is empty. Lets a re-run of Create Default Data
        /// fill fields added in later phases without touching values already tuned.
        /// </summary>
        public static void SetIfMissing(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(field);
            if (property == null)
                throw new ArgumentException($"{target.GetType().Name} has no serialized field '{field}'.");
            if (property.objectReferenceValue != null)
                return;
            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
            Debug.Log($"Filled {target.name}.{field}");
        }

        /// <summary>Fills a serialized list. Each element is either a value or an array of (field, value) pairs for structs.</summary>
        public static void SetList(Object target, string field, params object[] elements)
        {
            var so = new SerializedObject(target);
            SerializedProperty list = so.FindProperty(field);
            if (list == null || !list.isArray)
                throw new ArgumentException($"{target.GetType().Name} has no serialized list '{field}'.");
            list.arraySize = elements.Length;
            for (int i = 0; i < elements.Length; i++)
            {
                SerializedProperty element = list.GetArrayElementAtIndex(i);
                if (elements[i] is ValueTuple<string, object>[] members)
                {
                    foreach ((string name, object value) in members)
                        Assign(element.FindPropertyRelative(name), value);
                }
                else
                {
                    Assign(element, elements[i]);
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void Assign(SerializedProperty property, object value)
        {
            switch (value)
            {
                case null: property.objectReferenceValue = null; break;
                case float f: property.floatValue = f; break;
                case double d: property.doubleValue = d; break;
                case int i: property.intValue = i; break;
                case bool b: property.boolValue = b; break;
                case string s: property.stringValue = s; break;
                case Color c: property.colorValue = c; break;
                case Vector2 v: property.vector2Value = v; break;
                case Vector2Int v: property.vector2IntValue = v; break;
                case Enum e: property.enumValueIndex = Convert.ToInt32(e); break;
                case Object[] array:
                    property.arraySize = array.Length;
                    for (int i = 0; i < array.Length; i++)
                        property.GetArrayElementAtIndex(i).objectReferenceValue = array[i];
                    break;
                case Object o: property.objectReferenceValue = o; break;
                default: throw new ArgumentException($"Unsupported value type {value.GetType().Name}.");
            }
        }
    }
}
