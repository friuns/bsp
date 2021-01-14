using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

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
        // faces.array[faces.offset++] = face;

        var offset = verts.length;
        verts.Add(face.verts);

        int tristep = 1;
        for (int i = 1; i < face.numedges - 1; i++)
        {
            var i2 = i + offset;
            tris.array[tris.length + tristep - 1] = offset;
            tris.array[tris.length + tristep] = i2;
            tris.array[tris.length + tristep + 1] = i2 + 1;
            tristep += 3;
        }


        tris.length += (face.numedges - 2) * 3;


        uvs.Add(face.uv);
        uvs2.Add(face.uv2);
        uvs3.Add(face.uv3);

        // transparent |= face.mip.transparent;
        if(mip==null||face.mip.solid)
            mip = face.mip;
    }
    // private bool transparent;
    public BSPMipTexture mip;
    public Renderer GenerateMesh(bool transparent)
    {
        if (vertsCount == 0) return null;
        mesh.name = name;
        mesh.vertices = verts.ToArray();
        mesh.SetIndices(tris.ToArray(), MeshTopology.Triangles, 0);
        mesh.uv = uvs.ToArray();
        mesh.uv2 = uvs2.ToArray();
        mesh.SetUVs(2,uvs3.ToArray());
        // AutoWeld.Weld(mesh,.1f,100);
        mesh.RecalculateNormals();
        // mesh.Optimize();
        var g = new GameObject(name);
        
        g.AddComponent<MeshFilter>().mesh = mesh;
        g.transform.SetParent(Base._BspGenerateMapVis.level, true);
        var renderer = g.AddComponent<MeshRenderer>();
        
        var trigger = mip.hidden;
        if (trigger)
            renderer.sharedMaterials = new Material[0];
        else
            renderer.sharedMaterial =  transparent  ? Base._BspGenerateMapVis.matTrans : Base._BspGenerateMapVis.mat;
        if(mip.disabled)
            g.gameObject.SetActive(false);
        
        // g.layer = /*transparent?Layer.ignoreRayCast:*/ trigger?Layer.trigger: Layer.level;
        

        renderer.shadowCastingMode = ShadowCastingMode.Off;
        g.isStatic = true;
        

        return renderer;
    }
    
}
}