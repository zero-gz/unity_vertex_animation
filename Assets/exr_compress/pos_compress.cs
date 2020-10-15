using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class pos_compress : MonoBehaviour {
    public Texture2D exr_tex;
    public string save_path = "Assets/pos_compress/compress.png";
    public enum CompressType {RGBE, RGBM, DIRECT};
    public CompressType c_type;

    private Texture2D debug_tex;

    private float MinValue;
    private float MaxValue;
    private float ChaosValue = 0.00001f;

    public Color RGBE_Encode(Color exr_color)
    {
        Color ret_color = new Color();
        float MaxColor = Mathf.Max(Mathf.Max(exr_color.r, exr_color.g), exr_color.b);
        float E = Mathf.Clamp01((Mathf.Log(MaxColor, 2.0f)+127.0f)/255.0f);
        float Decode_MaxColor = Mathf.Pow(2.0f, E * 255.0f - 127.0f);
        float R = exr_color.r / Decode_MaxColor;
        float G = exr_color.g / Decode_MaxColor;
        float B = exr_color.b / Decode_MaxColor;

        ret_color.r = Mathf.Clamp01(R);
        ret_color.g = Mathf.Clamp01(G);
        ret_color.b = Mathf.Clamp01(B);
        ret_color.a = Mathf.Clamp01(E);
        return ret_color;
    }

    public Color RGBE_Decode(Color rgbe_color)
    {
        Color ret_color = new Color();
        float E = rgbe_color.a;
        float Decode_MaxColor = Mathf.Pow(2.0f, E*255.0f - 127.0f);
        //Debug.Log(string.Format("get decode max color {0} {1}", Decode_MaxColor, E * 255.0f - 127.0f));
        float R = Decode_MaxColor * rgbe_color.r;
        float G = Decode_MaxColor * rgbe_color.g;
        float B = Decode_MaxColor * rgbe_color.b;

        ret_color.r = R;
        ret_color.g = G;
        ret_color.b = B;
        return ret_color;
    }


    public Color RGBM_Encode(Color exr_color, float MaxValue)
    {
        Color ret_color = new Color();
        float MaxColor = Mathf.Max(Mathf.Max(exr_color.r, exr_color.g), exr_color.b);
        float M = MaxColor / MaxValue;
        float Decode_MaxColor = Mathf.CeilToInt(M*255)/255.0f*MaxValue;

        float R = exr_color.r / Decode_MaxColor;
        float G = exr_color.g / Decode_MaxColor;
        float B = exr_color.b / Decode_MaxColor;

        ret_color.r = Mathf.Clamp01(R);
        ret_color.g = Mathf.Clamp01(G);
        ret_color.b = Mathf.Clamp01(B);
        ret_color.a = Mathf.Clamp01(M);
        return ret_color;
    }

    public Color RGBM_Decode(Color rgbm_color, float MaxValue)
    {
        Color ret_color = new Color();
        float M = rgbm_color.a;
        float Decode_MaxColor = M * MaxValue;
        float R = Decode_MaxColor * rgbm_color.r;
        float G = Decode_MaxColor * rgbm_color.g;
        float B = Decode_MaxColor * rgbm_color.b;

        ret_color.r = R;
        ret_color.g = G;
        ret_color.b = B;
        return ret_color;
    }

    public Color DIRECT_Encode(Color exr_color)
    {
        Color ret_color = new Color();
        ret_color = exr_color;
        return ret_color;
    }

    public Color DIRECT_Decode(Color exr_color)
    {
        Color ret_color = new Color();
        ret_color = exr_color;
        return ret_color;
    }

    public Color TransformColor(Color exr_color, float min_value, float max_value)
    {
        switch(c_type)
        {
            case CompressType.RGBE:
                return RGBE_Encode(exr_color);
            case CompressType.RGBM:
                return RGBM_Encode(exr_color, max_value);
            case CompressType.DIRECT:
                return DIRECT_Encode(exr_color);
            default:
                return RGBE_Encode(exr_color);
        }
    }

    public Color DecodeColor(Color save_color, float min_value, float max_value)
    {
        // process the 255 color
        Color tmp_color = new Color();
        tmp_color.r = Mathf.CeilToInt(save_color.r * 255) / 255.0f;
        tmp_color.g = Mathf.CeilToInt(save_color.g * 255) / 255.0f;
        tmp_color.b = Mathf.CeilToInt(save_color.b * 255) / 255.0f;
        tmp_color.a = Mathf.CeilToInt(save_color.a * 255) / 255.0f;

        switch (c_type)
        {
            case CompressType.RGBE:
                return RGBE_Decode(tmp_color);
            case CompressType.RGBM:
                return RGBM_Decode(tmp_color, max_value);
            case CompressType.DIRECT:
                return DIRECT_Decode(tmp_color);
            default:
                return RGBE_Encode(save_color);
        }
    }

    public void trans_exr_to_png()
    {
        MinValue = 9999.0f;
        MaxValue = -9999.0f;
        int width = exr_tex.width;
        int height = exr_tex.height;
        Debug.Log(string.Format("get width {0}, height {1}", width, height));
        Texture2D save_tex = new Texture2D(width, height, TextureFormat.ARGB32, false);

        Texture2D diff_tex = new Texture2D(width, height, TextureFormat.RGBAHalf, false);
        for(int i=0;i<height;i++)
            for(int j=0;j<width;j++)
            {
                Color exr_color = exr_tex.GetPixel(j, i);
                if (exr_color.r < MinValue)
                    MinValue = exr_color.r;
                if (exr_color.g < MinValue)
                    MinValue = exr_color.g;
                if (exr_color.b < MinValue)
                    MinValue = exr_color.b;

                if (exr_color.r > MaxValue)
                    MaxValue = exr_color.r;
                if (exr_color.g > MaxValue)
                    MaxValue = exr_color.g;
                if (exr_color.b > MaxValue)
                    MaxValue = exr_color.b;
            }

        Debug.Log(string.Format("get MinValue:{0}, MaxValue:{1}", MinValue, MaxValue));

        for (int i = 0; i < height; i++)
            for (int j = 0; j < width; j++)
            {
                Color exr_color = exr_tex.GetPixel(j, i) - new Color(MinValue- ChaosValue, MinValue- ChaosValue, MinValue- ChaosValue);
                Color save_color = TransformColor(exr_color, MinValue, MaxValue);
                //Debug.Log(string.Format("{0} {1} get Pixel data {2},encode data:{3}", i, j, exr_color.ToString(), save_color.ToString()));
                save_tex.SetPixel(j, i, save_color);

                Color color_diff = exr_color - DecodeColor(save_color, MinValue, MaxValue);
                diff_tex.SetPixel(j, i, color_diff);
            }

        save_tex.Apply();
        diff_tex.Apply();
        util.save_texture(save_path, save_tex);

        string diff_path = save_path.Replace(".png", "_diff.exr");
        util.save_texture(diff_path, diff_tex);

        // override for pc的这些属性不知道怎么设置……
        TextureImporter footprintTextureImporter = (TextureImporter)AssetImporter.GetAtPath(save_path);
        footprintTextureImporter.sRGBTexture = false;
        footprintTextureImporter.npotScale = TextureImporterNPOTScale.None;
        footprintTextureImporter.isReadable = true;
        footprintTextureImporter.mipmapEnabled = false;
        footprintTextureImporter.textureFormat = TextureImporterFormat.RGBA32;
        EditorUtility.SetDirty(footprintTextureImporter);
        footprintTextureImporter.SaveAndReimport();

        footprintTextureImporter = (TextureImporter)AssetImporter.GetAtPath(diff_path);
        footprintTextureImporter.sRGBTexture = false;
        footprintTextureImporter.npotScale = TextureImporterNPOTScale.None;
        footprintTextureImporter.isReadable = true;
        footprintTextureImporter.mipmapEnabled = false;
        footprintTextureImporter.textureFormat = TextureImporterFormat.RGBAHalf;
        EditorUtility.SetDirty(footprintTextureImporter);
        footprintTextureImporter.SaveAndReimport();
    }

    public void create_exr_file()
    {
        int width = 10;
        int height = 5;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBAHalf, false);

        Color color = new Color(-1.0f, 0.0f, 100.0f);
        
        Color tmp_color = new Color(0, 0, 0);
        for(int i=0;i<height;i++)
            for(int j=0;j<width;j++)
            {
                tex.SetPixel(j, i, tmp_color);
            }

        tex.SetPixel(0, 0, color);
        tex.SetPixel(8, 0, color);
        tex.Apply();
        util.save_texture("Assets/pos_compress/test_exr.exr", tex);
    }

    //public void decode_png()
    //{
    //    int width = encode_tex.width;
    //    int height = encode_tex.height;
    //    for (int i = 0; i < height; i++)
    //        for (int j = 0; j < width; j++)
    //        {
    //            Color rgbe_color = encode_tex.GetPixel(j, i);
    //            Color decode_color = RGBE_Decode(rgbe_color);
    //            Debug.Log(string.Format("{0} {1} get rgbe color:{2}, decode color:{3}", i, j, rgbe_color.ToString(), decode_color.ToString()));
    //        }
    //}

    // Use this for initialization
    void Start () {
    }

    public void test_rgbe_decode()
    {
        //Color input = new Color(-1.0f, 0.0f, 100.0f);
        Color input = new Color(0.0f, 12.0f, 10000.0f);
        Color encode = RGBE_Encode(input);
        Color decode = RGBE_Decode(encode);

        Debug.Log(string.Format("rgbe get test:{0} \t{1}\t {2}", input.ToString(), decode.ToString(), encode.ToString()));
    }

    public void test_rgbm_decode()
    {
        Color input = new Color(0.0f, 12.0f, 10000.0f);
        float max_value = 10000.0f;
        //Color input = new Color(0.0f, 0.0f, 0.0f);
        Color encode = RGBM_Encode(input, max_value);
        Color decode = RGBM_Decode(encode, max_value);

        Debug.Log(string.Format("rgbm get test:{0} \t{1}\t {2}", input.ToString(), decode.ToString(), encode.ToString()));
    }

    public void show_texture()
    {
        
        int width = debug_tex.width;
        int height = debug_tex.height;
        for (int i = 0; i < height; i++)
            for (int j = 0; j < width; j++)
            {
                Debug.Log(string.Format("Get texture data({0}, {1}): {2}", i, j, debug_tex.GetPixel(j, i).ToString()));
            }
    }

    // Update is called once per frame
    void Update () {
        if (Input.GetKeyDown(KeyCode.C))
            create_exr_file();
        if (Input.GetKeyDown(KeyCode.T))
            trans_exr_to_png();

        if (Input.GetKeyDown(KeyCode.S))
        {
            //show_texture();
            test_rgbe_decode();
            test_rgbm_decode();

            Color tmp = new Color(0.0f, 0.001f, 1.0f, 0.552f);
            Color decode = RGBE_Decode(tmp);
            Debug.Log(string.Format("get test color decode:{0}", decode));
        }
	}
}
