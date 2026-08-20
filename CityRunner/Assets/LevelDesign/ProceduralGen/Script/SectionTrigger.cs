using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class SectionTrigger : MonoBehaviour
{
    // List of all possible tiles for procedural gen
    public List<GameObject> tilePeices;

    // triggers on collide with player (invis wall)
    void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.CompareTag("Trigger"))
        {
            GameObject selectedTile = PickRandomTile();
            Instantiate(selectedTile, new Vector3(0,0,0), Quaternion.identity);
        }
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
