using System.Collections.Generic;
using UnityEngine;

public class SectionTrigger : MonoBehaviour
{
    // List of all possible tiles for procedural gen
    public List<GameObject> tilePeices;
    bool hasSpawned;
    bool hasReportedOverlap;

    void Awake()
    {
        Debug.Log("SectionTrigger is active: " + gameObject.name, this);
    }

    // triggers on collide with player (invis wall)
    void OnTriggerEnter(Collider other)
    {
        TrySpawnTile(other);
    }

    void OnTriggerStay(Collider other)
    {
        if (!hasReportedOverlap)
        {
            hasReportedOverlap = true;
        }

        TrySpawnTile(other);
    }

    void TrySpawnTile(Collider other)
    {
        if (hasSpawned || other.GetComponentInParent<SideScrollerPlayerController>() == null)
        {
            return;
        }

        GameObject selectedTile = PickRandomTile();

        if (selectedTile == null)
        {
            Debug.LogWarning("SectionTrigger has no tile prefab assigned.", this);
            return;
        }

        hasSpawned = true;
        SpawnTileAtPlatformEnd(selectedTile);
        Debug.Log("Section trigger activated.", this);
    }

    void SpawnTileAtPlatformEnd(GameObject tilePrefab)
    {
        GameObject tile = Instantiate(tilePrefab, Vector3.zero, transform.rotation);
        Collider currentBase = FindBaseColliderInParents();
        Collider nextBase = FindBaseCollider(tile);

        if (currentBase == null || nextBase == null)
        {
            tile.transform.position = transform.position;
            return;
        }

        bool spawnToRight = transform.position.x >= currentBase.bounds.center.x;
        float currentEdge = spawnToRight ? currentBase.bounds.max.x : currentBase.bounds.min.x;
        float nextEdge = spawnToRight ? nextBase.bounds.min.x : nextBase.bounds.max.x;

        Vector3 offset = new Vector3(
            currentEdge - nextEdge,
            currentBase.bounds.min.y - nextBase.bounds.min.y,
            currentBase.bounds.center.z - nextBase.bounds.center.z);

        tile.transform.position += offset;
    }

    Collider FindBaseColliderInParents()
    {
        foreach (BoxCollider collider in GetComponentsInParent<BoxCollider>())
        {
            if (!collider.isTrigger)
            {
                return collider;
            }
        }

        return null;
    }

    Collider FindBaseCollider(GameObject tile)
    {
        foreach (BoxCollider collider in tile.GetComponentsInChildren<BoxCollider>())
        {
            if (!collider.isTrigger)
            {
                return collider;
            }
        }

        return null;
    }

    // funct to call random tile based on value between 0 & list count
    GameObject PickRandomTile()
    {
        if (tilePeices == null || tilePeices.Count == 0)
        {
            return null;
        }

        int randomIndex = UnityEngine.Random.Range(0, tilePeices.Count);
        return tilePeices[randomIndex];
    }

}
