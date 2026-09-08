using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Flipside.EditorTools
{
    /// <summary>Creates the flip-wave material and registers the full-screen pass on the URP renderers.</summary>
    public static class FxSetup
    {
        public const string MaterialPath = "Assets/Flipside/Art/FX/FlipWave.mat";
        static readonly string[] RendererPaths = { "Assets/Settings/PC_Renderer.asset", "Assets/Settings/Mobile_Renderer.asset" };

        [MenuItem("Flipside/Setup Flip Wave Effect")]
        public static Material Setup()
        {
            Shader shader = Shader.Find("Flipside/FlipWave");
            if (shader == null) throw new System.Exception("Flipside/FlipWave shader not found");

            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            material.shader = shader;
            material.SetFloat("_Radius", 0f);
            material.SetFloat("_Inside", 0f);
            material.SetFloat("_Outside", 0f);
            EditorUtility.SetDirty(material);

            foreach (string path in RendererPaths)
            {
                ScriptableRendererData data = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(path);
                if (data == null) continue;
                bool present = false;
                foreach (ScriptableRendererFeature f in data.rendererFeatures)
                {
                    if (f is FullScreenPassRendererFeature fs && fs.name == "FlipWave")
                    {
                        fs.passMaterial = material;
                        present = true;
                    }
                }
                if (!present)
                {
                    FullScreenPassRendererFeature feature = ScriptableObject.CreateInstance<FullScreenPassRendererFeature>();
                    feature.name = "FlipWave";
                    feature.passMaterial = material;
                    feature.injectionPoint = FullScreenPassRendererFeature.InjectionPoint.BeforeRenderingPostProcessing;
                    feature.requirements = ScriptableRenderPassInput.None;
                    feature.fetchColorBuffer = true;
                    AssetDatabase.AddObjectToAsset(feature, data);

                    SerializedObject so = new SerializedObject(data);
                    SerializedProperty list = so.FindProperty("m_RendererFeatures");
                    SerializedProperty map = so.FindProperty("m_RendererFeatureMap");
                    list.arraySize++;
                    list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = feature;
                    map.arraySize++;
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out string guid, out long localId);
                    map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                EditorUtility.SetDirty(data);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("Flip wave effect registered on URP renderers");
            return material;
        }
    }
}
