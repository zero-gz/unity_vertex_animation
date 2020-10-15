using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using System.IO;
using System.Text;
using System;

// 制作这种通用类
public class util{

    public static void test_call()
    {
        Debug.Log("i want to call !!!!");
    }

    public static void save_texture(string save_path, Texture2D tex)
    {
        //if(tex.)

        byte[] data;

        if (tex.format == TextureFormat.ARGB32 || tex.format == TextureFormat.RGB24 || tex.format == TextureFormat.RGBA32)
            data = tex.EncodeToPNG();
        else if (tex.format == TextureFormat.RGBAHalf || tex.format == TextureFormat.RGBAFloat)
            data = tex.EncodeToEXR();
        else
        {
            Debug.LogError("texture is not support format!");
            return;
        }
        System.IO.File.WriteAllBytes(save_path, data);
        AssetDatabase.Refresh();
    }

    public static void save_screen_tex(string save_path, int start_x = 0, int start_y = 0, int width = 0,int height = 0)
    {
        if (width == 0) width = Screen.width;
        if (height == 0) height = Screen.height;

        Texture2D tex = new Texture2D(width, height, TextureFormat.ARGB32, false);
        Debug.Log(string.Format("get screen width and height: {0} {1}", Screen.width, Screen.height) );
        tex.ReadPixels(new Rect(start_x, start_y, width, height), 0, 0);
        tex.Apply();

        save_texture(save_path, tex);
    }

    public static void save_rendertexture(string save_path, RenderTexture rt)
    {
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;

        int width = rt.width;
        int height = rt.height;

        Texture2D tex = new Texture2D(width, height, TextureFormat.ARGB32, false);
        Debug.Log(string.Format("get render texture width and height: {0} {1}", width, height));
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();

        save_texture(save_path, tex);

        RenderTexture.active = prev;
    }

    // 这里给出一个接口设置的例子
    //public static void save_texture_importer(string save_path)
    //{
    //    // override for pc的这些属性不知道怎么设置……
    //    TextureImporter footprintTextureImporter = (TextureImporter)AssetImporter.GetAtPath(save_path);
    //    footprintTextureImporter.sRGBTexture = false;
    //    footprintTextureImporter.npotScale = TextureImporterNPOTScale.None;
    //    footprintTextureImporter.isReadable = true;
    //    footprintTextureImporter.mipmapEnabled = false;
    //    footprintTextureImporter.textureFormat = TextureImporterFormat.RGBA32;
    //    EditorUtility.SetDirty(footprintTextureImporter);
    //    footprintTextureImporter.SaveAndReimport();
    //}

    // -----------------------------------------------------------------------------

    public static string trans_mesh_to_string(MeshFilter mf, Vector3 scale)
    {
        Mesh mesh = mf.mesh;
        Vector2 textureOffset = new Vector2(0, 0);
        Vector2 textureScale = new Vector2(1, 1);
        StringBuilder stringBuilder = new StringBuilder().Append("mtllib design.mtl")
            .Append("\n")
            .Append("g ")
            .Append(mf.name)
            .Append("\n");
        Vector3[] vertices = mesh.vertices;
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 vector = vertices[i];
            stringBuilder.Append(string.Format("v {0} {1} {2}\n", vector.x * scale.x, vector.y * scale.y, vector.z * scale.z));
        }
        stringBuilder.Append("\n");
        Dictionary<int, int> dictionary = new Dictionary<int, int>();
        if (mesh.subMeshCount > 1)
        {
            int[] triangles = mesh.GetTriangles(1);
            for (int j = 0; j < triangles.Length; j += 3)
            {
                if (!dictionary.ContainsKey(triangles[j]))
                {
                    dictionary.Add(triangles[j], 1);
                }
                if (!dictionary.ContainsKey(triangles[j + 1]))
                {
                    dictionary.Add(triangles[j + 1], 1);
                }
                if (!dictionary.ContainsKey(triangles[j + 2]))
                {
                    dictionary.Add(triangles[j + 2], 1);
                }
            }
        }

        int num = 0;
        for (num = 0; num != mesh.uv.Length; num++)
        {
            Vector2 vector2 = Vector2.Scale(mesh.uv[num], textureScale) + textureOffset;
            if (dictionary.ContainsKey(num))
            {
                stringBuilder.Append(string.Format("vt {0} {1}\n", mesh.uv[num].x, mesh.uv[num].y));
            }
            else
            {
                stringBuilder.Append(string.Format("vt {0} {1}\n", vector2.x, vector2.y));
            }
        }

        stringBuilder.Append("\n");
        for (num = 0; num != mesh.normals.Length; num++)
        {
            Vector3 normal = mesh.normals[num];
            stringBuilder.Append(string.Format("vn {0} {1} {2}\n", normal.x, normal.y, normal.z));
        }

        for (int k = 0; k < mesh.subMeshCount; k++)
        {
            stringBuilder.Append("\n");
            if (k == 0)
            {
                stringBuilder.Append("usemtl ").Append("Material_design").Append("\n");
            }
            if (k == 1)
            {
                stringBuilder.Append("usemtl ").Append("Material_logo").Append("\n");
            }
            int[] triangles2 = mesh.GetTriangles(k);
            for (int l = 0; l < triangles2.Length; l += 3)
            {
                stringBuilder.Append(string.Format("f {0}/{0} {1}/{1} {2}/{2}\n", triangles2[l] + 1, triangles2[l + 2] + 1, triangles2[l + 1] + 1));
            }
        }
        return stringBuilder.ToString();
    }

    public void save_mesh_to_obj(GameObject obj, string save_path)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        MeshFilter mf = renderer.GetComponent<MeshFilter>();

        StreamWriter streamWriter = new StreamWriter(save_path);
        streamWriter.Write(trans_mesh_to_string(mf, new Vector3(-1f, 1f, 1f)));
        streamWriter.Close();
        
        AssetDatabase.Refresh();
    }
    // -----------------------------------------------------------------------------------------------------------------
    public static Material change_material(GameObject obj, string shader_name)
    {
        Renderer renderer = obj.GetComponent<Renderer>();

        Material old_mtl = renderer.material;

        Material new_mtl = new Material(Shader.Find(shader_name));
        new_mtl.SetPass(0);
        renderer.material = new_mtl;

        return old_mtl;
    }

    // ------------------------------------------------------------------------------------------------------------------

    public static Mesh create_plane(int w_num, int h_num)
    {
        Mesh sub_mesh = new Mesh();

        int vertex_num = (w_num + 1) * (h_num + 1);
        int index_num = w_num * h_num * 2 * 3;
        Vector3[] vertices = new Vector3[vertex_num];
        Vector2[] uv = new Vector2[vertex_num];
        int[] indexs = new int[index_num];

        int i, j, count;
        i = j = count = 0;
        for (i = 0; i <= h_num; i++)
        {
            for (j = 0; j <= w_num; j++)
            {
                int index = (w_num + 1) * i + j;
                vertices[index] = new Vector3(j * 1.0f / w_num, i * 1.0f / h_num, 0);
                uv[index] = new Vector2(j * 1.0f / w_num, i * 1.0f / h_num);
            }
        }


        for (i = 0; i < h_num; i++)
        {
            for (j = 0; j < w_num; j++)
            {
                //产生两个三角形
                int tri_a_1 = (w_num + 1) * i + j;
                int tri_b_1 = tri_a_1 + 1;
                int tri_c_1 = (w_num + 1) * (i + 1) + (j + 1);

                int tri_a_2 = tri_a_1;
                int tri_b_2 = tri_c_1;
                int tri_c_2 = (w_num + 1) * (i + 1) + j;

                indexs[count] = tri_a_1;
                indexs[count + 1] = tri_b_1;
                indexs[count + 2] = tri_c_1;

                indexs[count + 3] = tri_a_2;
                indexs[count + 4] = tri_b_2;
                indexs[count + 5] = tri_c_2;

                count += 6;
            }
        }

        sub_mesh.vertices = vertices;
        sub_mesh.uv = uv;
        sub_mesh.triangles = indexs;

        /*
        string s = "";
        for (i = 0; i < vertex_num; i++)
            s += string.Format("({0}, {1}, {2}), ", vertices[i].x, vertices[i].y, vertices[i].z);

        s += "\n";
        for (i = 0; i < index_num; i++)
            s += string.Format("{0} ", indexs[i]);

        Debug.Log(s);
        */
        sub_mesh.RecalculateBounds();
        return sub_mesh;
    }

    // -------------------------------------------------------------------------

    public static RenderTexture gpu_draw_rendertexture(Texture src, Material mtl, int width=0, int height=0)
    {
        if (width == 0) width = src.width;
        if (height == 0) height = src.height;
        RenderTexture dst = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(src, dst, mtl);
        return dst;
    }
}


/*
public class script_util : MonoBehaviour
{
    public bool grab = false;

    virtual protected void space_key_func()
    {
        Debug.Log("get space key down!");
    }

    protected void Update()
    {
        //Press space to start the screen grab
        if (Input.GetKeyDown(KeyCode.Space))
        {
            grab = true;
            space_key_func();
        }
    }
}
*/
