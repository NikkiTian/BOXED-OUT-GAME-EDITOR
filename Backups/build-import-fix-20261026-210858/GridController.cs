using System.Drawing.Text;
using Unity.VisualScripting;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using static GridController;
using System.Runtime.Remoting.Metadata.W3cXsd2001;

public class GridController : MonoBehaviour
{
    // We're making our own type for gameobject based tiles
    public struct Tile3D 
    {
        public Tile3D(Tilemap tilemap, GameObject gameObject, Vector3Int cellPos)
        {
            this.Tmap = tilemap;
            this.GameObj = gameObject;
            this.CellPos = cellPos;
        }

        public Tilemap Tmap { get; }
        public GameObject GameObj { get; }
        public Vector3Int CellPos
        {
            get { return WorldToGridPos(this.Tmap, this.GameObj.transform.position); }
            private set {; }
        }

        // NOTE: This method is NOT checking if the tile's gameobj is referring to the exact same instance, just if the gameobjects belong in the same group
        // checks if the gameobject referenced in this tile3D is the same as another gameObject
        public bool RefersToSimilarGameObject(GameObject comparisonGameObj)
        {
            return comparisonGameObj.tag == this.GameObj.tag;
        }

        public void Move(Vector3Int pos)
        {
            GameObj.transform.position = GridToWorldPos(Tmap, pos);
        }

        public void Rotate(Vector3 angle)
        {
            // to rotate a point P1 around P2 by angle THETA
            // set origin to the point of rotation (P2) --> relative_vector = P1 - P2 
            // apply this quaternion to this directional vector --> resultant_vector = quaternion * relative_vector (resultant vector is the point rotated about the P2 origin)
            // translate the point back to its previous position --> P1_rotated_around_P2 = resultant_Vector + P2

            Vector3 normalizedAngle = angle;

            Vector3 resultantPos = Quaternion.Euler(angle) * GameObj.transform.forward;
            Vector3 refPoint = resultantPos + GameObj.transform.position;

            GameObj.transform.LookAt(refPoint);
        }
    }

    // NOTE: The following method is based on a grid using the XZY swizzle so X and Y values are now attributed to X and Z respectively
    // Get position of grid in the world position
    public static Vector3 GridToWorldPos(Tilemap tmap, Vector3Int cellPos)
    {
        // it's a bit weird by anchors are different depending on which brush you are using, though their default value is (0.5f, 0.5f, 0f)
        // for ex: gameobjs made thru gameobj brush are not affected by changing anchor values in the tilemap (tile anchors), instead
        // you have to change the anchor in the game obj brush editor in tile palette

        // this just means we have to manually adjust the anchor... sigh --> btw go to gameobject brush to check your anchor settings
        Vector3 anchor = new Vector3(0.5f, 0, 0.5f);
        return tmap.CellToWorld(cellPos) + anchor;
    }

    // Get position of grid we're in
    public static Vector3Int WorldToGridPos(Tilemap tmap, Vector3 worldPos)
    {
        return tmap.WorldToCell(worldPos);
    }

    // Get the grid parent of tilemap
    public static Grid GetGrid(Tilemap tmap)
    {
        return tmap.transform.parent.GetComponent<Grid>();
    }

    // Get the cell size of the tilemap we're in
    public static Vector3 GetTileMapCellSize(Tilemap tmap)
    {
        return GetGrid(tmap).cellSize;
    }

    // since 3D gameobjects instantiated with a gameobject brush are only children, and not type of tiles, we can't check through tilemap.GetTile()
    // so we make our own custom function to detect it; returns a custom Tile3D type or null if there is no tile
    //public static Tile3D? GetTile3D(Tilemap tmap, Vector3Int cellPos)
    //{
    //    Vector3 worldPos = GridToWorldPos(tmap, cellPos);
    //    // Note: the marginal extent should be less than the halfextent of one one tile (right now it's about 0.5f so anything < 0.5f would work)
    //    Vector3 marginalExtent = Vector3.one * 0.1f;

    //    Collider[] colliders = Physics.OverlapBox(worldPos, marginalExtent, Quaternion.identity, LayerMask.GetMask("Default"));
    //    foreach (Collider col in colliders)
    //    {
    //        if (col.transform.parent == tmap.transform)
    //        {
    //            return new Tile3D(tmap, col.gameObject, cellPos);
    //        }
    //    }

    //    return null;
    //}

    public static IList<Tile3D> GetAllTile3D(Tilemap tmap, Vector3Int cellPos)
    {
        Vector3 worldPos = GridToWorldPos(tmap, cellPos);
        // Note: the marginal extent should be less than the halfextent of one one tile (right now it's about 0.5f so anything < 0.5f would work)
        Vector3 marginalExtent = Vector3.one * 0.1f;

        IList<Tile3D> tile3Ds = new List<Tile3D>();

        Collider[] colliders = Physics.OverlapBox(worldPos, marginalExtent, Quaternion.identity, LayerMask.GetMask("Default"));
        foreach (Collider col in colliders)
        {
            if (col.transform.parent == tmap.transform)
            {
                tile3Ds.Add(new Tile3D(tmap, col.gameObject, cellPos));
            }
        }

        return tile3Ds;
    }
}

