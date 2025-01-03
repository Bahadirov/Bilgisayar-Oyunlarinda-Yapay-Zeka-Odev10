using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class FindPathAStar : MonoBehaviour
{
    // Labirent referansı
    public Maze maze;

    // Open ve Closed listeler
    List<PathMarker> open = new List<PathMarker>();
    List<PathMarker> closed = new List<PathMarker>();

    // Materyaller
    public Material closedMaterial; // Kapalı düğümler için (örn. kırmızı)
    public Material openMaterial;   // Açık düğümler için (örn. yeşil)

    // Başlangıç, hedef ve yol işaretleyicileri
    public GameObject start;  // Başlangıç noktası için prefab
    public GameObject end;    // Hedef noktası için prefab
    public GameObject pathP;  // Yol noktaları için prefab

    // A* algoritması için düğüm referansları
    PathMarker goalNode;  // Hedef düğüm
    PathMarker startNode; // Başlangıç düğüm
    PathMarker lastPos;   // Son ziyaret edilen düğüm

    // Algoritma durumları
    bool done = false; // Arama tamamlandı mı?

    // Mevcut marker'ları temizleme fonksiyonu
    void RemoveAllMarkers()
    {
        GameObject[] markers = GameObject.FindGameObjectsWithTag("marker");
        foreach (GameObject m in markers)
        {
            Destroy(m);
        }
    }

    void BeginSearch()
    {
        done = false;
        RemoveAllMarkers();
        List<MapLocation> locations = new List<MapLocation>();
        for (int z = 1; z < maze.depth - 1; z++)
        {
            for (int x = 1; x < maze.width - 1; x++)
            {
                if (maze.map[x, z] != 1)
                {
                    locations.Add(new MapLocation(x, z));
                }
            }
        }

        locations.Shuffle();
        Vector3 startLocation = new Vector3(locations[0].x * maze.scale, 0, locations[0].z * maze.scale);
        startNode = new PathMarker(
            new MapLocation(locations[0].x, locations[0].z),
            0, 0, 0,
            Instantiate(start, startLocation, Quaternion.identity),
            null
        );

        Vector3 goalLocation = new Vector3(locations[1].x * maze.scale, 0, locations[1].z * maze.scale);
        goalNode = new PathMarker(
            new MapLocation(locations[1].x, locations[1].z),
            0, 0, 0,
            Instantiate(end, goalLocation, Quaternion.identity),
            null
        );

        open.Clear();
        closed.Clear();
        open.Add(startNode);
        lastPos = startNode;
    }

    bool IsClosed(MapLocation marker)
    {
        foreach (PathMarker p in closed)
        {
            if (p.location.Equals(marker))
            {
                return true;
            }
        }
        return false;
    }

    bool UpdateMarker(MapLocation pos, float g, float h, float f, PathMarker parent)
    {
        foreach (PathMarker p in open)
        {
            if (p.location.Equals(pos))
            {
                p.G = g;
                p.H = h;
                p.F = f;
                p.parent = parent;
                return true;
            }
        }
        return false;
    }

    void Start()
    {
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            BeginSearch();
        }

        if (Input.GetKeyDown(KeyCode.C) && !done)
        {
            Search(lastPos);
        }

        if (Input.GetKeyDown(KeyCode.M) && done)
        {
            GetPath();
        }
    }

    void Search(PathMarker thisNode)
    {
        if (thisNode == null) return;

        // Eğer hedef düğüme ulaşıldıysa
        if (thisNode.Equals(goalNode))
        {
            done = true;
            return;
        }

        // Mevcut düğümün komşularını kontrol et
        foreach (MapLocation dir in maze.directions)
        {
            MapLocation neighbor = dir + thisNode.location;

            // Komşu labirent sınırlarının dışında ise atla
            if (neighbor.x < 1 || neighbor.x >= maze.width - 1 || neighbor.z < 1 || neighbor.z >= maze.depth - 1)
                continue;

            // Komşu bir duvarsa atla
            if (maze.map[neighbor.x, neighbor.z] == 1)
                continue;

            // Komşu zaten Closed listesinde ise atla
            if (IsClosed(neighbor))
                continue;

            // G, H ve F değerlerini hesapla
            float G = thisNode.G + Vector2.Distance(thisNode.location.ToVector(), neighbor.ToVector());
            float H = Vector2.Distance(neighbor.ToVector(), goalNode.location.ToVector());
            float F = G + H;

            // Marker oluştur
            GameObject pathBlock = Instantiate(
                pathP,
                new Vector3(neighbor.x * maze.scale, 0, neighbor.z * maze.scale),
                Quaternion.identity
            );

            // Open listede zaten varsa güncelle
            if (!UpdateMarker(neighbor, G, H, F, thisNode))
            {
                // Open listeye ekle
                open.Add(new PathMarker(neighbor, G, H, F, pathBlock, thisNode));
            }
        }

        // Open listesinden en düşük F değerine sahip olanı seç
        open = open.OrderBy(p => p.F).ThenBy(p => p.H).ToList();
        PathMarker nextNode = open[0];

        // Open listesinden çıkar ve Closed listesine ekle
        open.Remove(nextNode);
        closed.Add(nextNode);

        // Marker rengini kapalı olarak ayarla
        nextNode.marker.GetComponent<Renderer>().material = closedMaterial;

        // Son konumu güncelle
        lastPos = nextNode;
    }

    void GetPath()
    {
        // Mevcut tüm marker'ları temizle
        RemoveAllMarkers();

        // Hedeften başlayarak başlangıca kadar yolu takip et
        PathMarker current = lastPos;
        while (current != null && !current.Equals(startNode))
        {
            Instantiate(
                pathP,
                new Vector3(current.location.x * maze.scale, 0, current.location.z * maze.scale),
                Quaternion.identity
            );
            current = current.parent; // Bir önceki düğüme geç
        }

        // Başlangıç düğümünü de işaretle
        Instantiate(
            pathP,
            new Vector3(startNode.location.x * maze.scale, 0, startNode.location.z * maze.scale),
            Quaternion.identity
        );
    }
}

// PathMarker sınıfı bağımsız olarak tanımlandı
public class PathMarker
{
    public MapLocation location;
    public float G;
    public float H;
    public float F;
    public GameObject marker;
    public PathMarker parent;

    public PathMarker(MapLocation l, float g, float h, float f, GameObject marker, PathMarker p)
    {
        location = l;
        G = g;
        H = h;
        F = f;
        this.marker = marker;
        parent = p;
    }

    public override bool Equals(object obj)
    {
        if ((obj == null) || !this.GetType().Equals(obj.GetType()))
        {
            return false;
        }
        else
        {
            return location.Equals(((PathMarker)obj).location);
        }
    }

    public override int GetHashCode()
    {
        return location.GetHashCode();
    }
}
