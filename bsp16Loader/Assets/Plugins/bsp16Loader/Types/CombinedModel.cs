using UnityEngine;

namespace bsp
{
public class CombinedModel
{
    // public MipModel(int verts, int tris, int uvs)
    // {
    //     
    // }
    public string name;
    // public Texture2D  texture {get {return mip.texture; } }
    // public BSPMipTexture mip;
    public Mesh mesh = new Mesh();
    public ArrayOffset<Vector3> verts;
    public ArrayOffset<int> tris;
    public ArrayOffset<Vector2> uvs;
    public ArrayOffset<Vector2> uvs2;
    public ArrayOffset<Vector4> uvs3;
    // public ArrayOffset<BSPFace> faces;        
    public int vertsCount;
    public int faceCount;
    public int trianglesCount;
    public void PreAddFace(BSPFace face)
    {
        faceCount++;
        vertsCount += face.numedges;
        trianglesCount += (face.numedges - 2) * 3;
    }
    // public int uvsCount;
    public void Init()
    {
        verts = new ArrayOffset<Vector3>(vertsCount);
        tris = new ArrayOffset<int>(trianglesCount); //vertsCount * 3
        uvs = new ArrayOffset<Vector2>(vertsCount);
        uvs2 = new ArrayOffset<Vector2>(vertsCount);
        uvs3 = new ArrayOffset<Vector4>(vertsCount);
        // faces = new BSPFace[faceCount];
    }
    public void AddFace(BSPFace face)
    {
        var combined = this;
        // combined.faces.array[combined.faces.offset++] = face;

        var offset = combined.verts.offset;
        combined.verts.Add(face.verts);

        var tris = combined.tris;
        int tristep = 1;
        for (int i = 1; i < face.numedges - 1; i++)
        {
            var i2 = i + offset;
            tris.array[tris.offset + tristep - 1] = offset;
            tris.array[tris.offset + tristep] = i2;
            tris.array[tris.offset + tristep + 1] = i2 + 1;
            tristep += 3;
        }


        tris.offset += (face.numedges - 2) * 3;


        combined.uvs.Add(face.uv);
        combined.uvs2.Add(face.uv2);
        combined.uvs3.Add(face.uv3);

        if (!inited)
            material = face.mainTex.format == TextureFormat.ARGB32 ? bsWeb._BspGenerateMapVis.matTrans : bsWeb._BspGenerateMapVis.mat;
        inited = true;

    }
    private bool inited;
    public Material material;   
    public Renderer GenerateMesh()
    {
        mesh.name = name;
        mesh.vertices = verts.ToArray();
        mesh.SetIndices(tris.ToArray(), MeshTopology.Triangles, 0);
        mesh.uv = uvs.ToArray();
        mesh.uv2 = uvs2.ToArray();
        mesh.SetUVs(2,uvs3.ToArray());
        // AutoWeld.Weld(mesh,.1f,100);
        mesh.RecalculateNormals();
        // mesh.Optimize();
        var g = new GameObject(this.name);
        
        g.AddComponent<MeshFilter>().mesh = mesh;
        g.transform.SetParent(bsWeb._BspGenerateMapVis.level, true);
        var renderer = g.AddComponent<MeshRenderer>();
        renderer.material = material;
        g.isStatic = true;
        
    
            
        // else
        // {
        //     var trigger = hide.Any(a => string.Equals(combined.name, a, StringComparison.OrdinalIgnoreCase));
        //     if (trigger)
        //         renderer.sharedMaterials = new Material[0];
        //
        //     if (!disableTexturesAndColliders)
        //     {
        //         Collider c = trigger ? g.AddComponent<BoxCollider>() : (Collider)g.AddComponent<MeshCollider>();
        //         c.isTrigger = trigger;
        //     }
        //     
        //     
        //     g.layer = /*transparent?Layer.ignoreRayCast:*/ trigger?Layer.trigger: Layer.level;
        // }
            
        return renderer;
    }
    
}
}