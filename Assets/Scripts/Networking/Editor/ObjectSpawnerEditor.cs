using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ObjectSpawner))]
public class ObjectSpawnerEditor : Editor
{
    SerializedProperty isOwnerProperty;
    SerializedProperty sourceSpawnerProperty;
    SerializedProperty spawnIntervalProperty;
    SerializedProperty minSpawnZProperty;
    SerializedProperty maxSpawnZProperty;
    SerializedProperty lanePositionsProperty;
    SerializedProperty obstaclePrefabProperty;
    SerializedProperty obstaclePoolSizeProperty;
    SerializedProperty coinPrefabProperty;
    SerializedProperty coinPoolSizeProperty;
    SerializedProperty coinSpawnChanceProperty;
    SerializedProperty minSpawnIntervalAtMaxDifficultyProperty;
    SerializedProperty difficultyRampUpTimeProperty;
    SerializedProperty referenceCameraProperty;

    void OnEnable()
    {
        // Get all properties
        isOwnerProperty = serializedObject.FindProperty("isOwner");
        sourceSpawnerProperty = serializedObject.FindProperty("sourceSpawner");
        spawnIntervalProperty = serializedObject.FindProperty("spawnInterval");
        minSpawnZProperty = serializedObject.FindProperty("minSpawnZ");
        maxSpawnZProperty = serializedObject.FindProperty("maxSpawnZ");
        lanePositionsProperty = serializedObject.FindProperty("lanePositions");
        obstaclePrefabProperty = serializedObject.FindProperty("obstaclePrefab");
        obstaclePoolSizeProperty = serializedObject.FindProperty("obstaclePoolSize");
        coinPrefabProperty = serializedObject.FindProperty("coinPrefab");
        coinPoolSizeProperty = serializedObject.FindProperty("coinPoolSize");
        coinSpawnChanceProperty = serializedObject.FindProperty("coinSpawnChance");
        minSpawnIntervalAtMaxDifficultyProperty = serializedObject.FindProperty("minSpawnIntervalAtMaxDifficulty");
        difficultyRampUpTimeProperty = serializedObject.FindProperty("difficultyRampUpTime");
        referenceCameraProperty = serializedObject.FindProperty("referenceCamera");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Always show isOwner
        EditorGUILayout.PropertyField(isOwnerProperty);

        if (isOwnerProperty.boolValue)
        {
            // Show all properties except sourceSpawner when isOwner is true
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("General Spawn Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(spawnIntervalProperty);
            EditorGUILayout.PropertyField(minSpawnZProperty);
            EditorGUILayout.PropertyField(maxSpawnZProperty);
            EditorGUILayout.PropertyField(lanePositionsProperty);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Obstacle Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(obstaclePrefabProperty);
            EditorGUILayout.PropertyField(obstaclePoolSizeProperty);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Coin Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(coinPrefabProperty);
            EditorGUILayout.PropertyField(coinPoolSizeProperty);
            EditorGUILayout.PropertyField(coinSpawnChanceProperty);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Difficulty Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(minSpawnIntervalAtMaxDifficultyProperty);
            EditorGUILayout.PropertyField(difficultyRampUpTimeProperty);
            EditorGUILayout.PropertyField(referenceCameraProperty);
        }
        else
        {
            // Show only sourceSpawner and prefabs when isOwner is false
            EditorGUILayout.PropertyField(sourceSpawnerProperty);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Object References", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(obstaclePrefabProperty);
            EditorGUILayout.PropertyField(coinPrefabProperty);
        }

        serializedObject.ApplyModifiedProperties();
    }
}