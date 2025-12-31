using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FullscreenColorQuad : MonoBehaviour
{
    public Color quadColor=Color.green; // 在Inspector中设置或动态更改颜色

    private Mesh quadMesh;
    private MeshRenderer meshRenderer;
    public GameObject obj_main;
    //private float position_offset;

    private void Awake(){
        //position_offset = transform.position.x - obj_main.transform.position.x;
    }

    private void Start()
    {
        //position_offset = transform.position.x - obj_main.transform.position.x;
        // 创建一个全屏四边形的Mesh
        quadMesh = new Mesh();
        quadMesh.vertices = new Vector3[] {
            new Vector3(-1, 1, 0),
            new Vector3(1, 1, 0),
            new Vector3(1, -1, 0),
            new Vector3(-1, -1, 0)
        };
        quadMesh.uv = new Vector2[] {
            new Vector2(0, 1),
            new Vector2(1, 1),
            new Vector2(1, 0),
            new Vector2(0, 0)
        };
        quadMesh.triangles = new int[] { 0, 1, 2, 2, 3, 0 };

        // 创建一个默认为Unlit Shader的基础材质，并设置颜色
        var quadMaterial = new Material(Shader.Find("Unlit/Color"));
        quadMaterial.color = quadColor;

        // 获取或添加Mesh Filter和Mesh Renderer组件
        var meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            meshFilter = gameObject.AddComponent<MeshFilter>();
        }
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
        }

        meshFilter.mesh = quadMesh;
        meshRenderer.material = quadMaterial;
        meshRenderer.enabled = true;
    }

    private void Update()
    {
        // 动态更新材质颜色（如果需要实时更改）
        meshRenderer.material.color = quadColor;
        transform.position = new Vector3(obj_main.transform.position.x-0.2f, transform.position.y, transform.position.z);//在camera 0.3f前
    }

    public void QuadVisibility(bool enabled)
    {
        meshRenderer.enabled = enabled;
    }
}
