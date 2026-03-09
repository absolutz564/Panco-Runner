using System.Collections.Generic;
using UnityEngine;

public class TileManager : MonoBehaviour
{
    [Header("Tiles")]
    public GameObject[] tilePrefabs;   // seus 4 prefabs de blocos

    [Header("Configuração")]
    public int tilesAhead = 5;         // quantos tiles manter à frente do player
    public float tileLength = 20f;     // comprimento de UM tile no eixo Z
    public float safeZone = 15f;       // distância atrás do player onde o tile pode sumir

    private Transform playerTransform;
    private float nextSpawnZ = 0f;     // Z onde o próximo tile será instanciado
    private List<GameObject> activeTiles = new List<GameObject>();

    void Start()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;

        // Spawna os primeiros tiles em sequência (sem sobrepor)
        for (int i = 0; i < tilesAhead; i++)
        {
            SpawnTile();
        }
    }

    void Update()
    {
        // Mantém sempre "tilesAhead" tiles à frente do player
        // Quando o player avança, spawna 1 novo e remove o mais antigo
        if (playerTransform.position.z - safeZone > nextSpawnZ - tilesAhead * tileLength)
        {
            SpawnTile();
            DeleteOldestTile();
        }
    }

    void SpawnTile(int prefabIndex = -1)
    {
        if (prefabIndex == -1)
        {
            prefabIndex = Random.Range(0, tilePrefabs.Length);
        }

        // IMPORTANTE: cada tile nasce em nextSpawnZ, e depois avançamos nextSpawnZ
        Vector3 spawnPos = new Vector3(0f, 0f, nextSpawnZ);
        GameObject tile = Instantiate(tilePrefabs[prefabIndex], spawnPos, Quaternion.identity);
        activeTiles.Add(tile);

        nextSpawnZ += tileLength; // próxima posição Z fica logo à frente deste tile
    }

    void DeleteOldestTile()
    {
        if (activeTiles.Count == 0) return;

        Destroy(activeTiles[0]);
        activeTiles.RemoveAt(0);
    }
}