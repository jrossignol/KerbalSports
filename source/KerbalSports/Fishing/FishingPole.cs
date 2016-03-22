using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KerbalSports.Fishing
{
    public class FishingPole : MonoBehaviour
    {
        public Transform referenceTransform;
        public GameObject parent;

        const int slices = 12;
        const float topScale = 0.004f;
        const float bottomScale = 0.0100f;
        const float height = -1.0f;
        Color rodColor = new Color(150 / 255.0f, 94 / 255.0f, 63 / 255.0f);

        void Awake()
        {
            Debug.Log("FishingPole.Awake()");

            // Build the fishing pole mesh on the fly
            // TODO - get someone with some modelling skills...
            MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
            meshFilter.mesh = new Mesh();
            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Vector2> uv = new List<Vector2>();

            // Create the points
            vertices.Add(new Vector3(0.0f, 0.0f, -height * 0.1f));
            normals.Add(Vector3.forward);
            uv.Add(Vector2.zero);
            for (int section = 0; section < 4; section++)
            {
                float scale = section >= 2 ? topScale: bottomScale;
                float z = section >= 2 ? height : -height * 0.1f;
                for (int i = 0; i < slices; i++)
                {
                    float theta = 2.0f * (float)Math.PI * i / slices;
                    Vector3 vertex = new Vector3(scale * (float)Math.Cos(theta), scale * (float)Math.Sin(theta), z);
                    vertices.Add(vertex);
                    uv.Add(Vector2.zero);
                    if (section == 0)
                    {
                        normals.Add(Vector3.forward);
                    }
                    else if (section == 3)
                    {
                        normals.Add(Vector3.back);
                    }
                    else
                    {
                        normals.Add(new Vector3(vertex.x, vertex.y, (bottomScale - topScale) / height).normalized);
                    }
                }
            }
            vertices.Add(new Vector3(0.0f, 0.0f, height));
            normals.Add(Vector3.back);
            uv.Add(Vector2.zero);

            // Create the triangles
            for (int i = 0; i < slices; i++)
            {
                triangles.Add(0);
                triangles.Add(i + 1);
                triangles.Add(((i == slices - 1) ? -1 : i) + 2);
            }
            for (int i = 0; i < slices; i++)
            {
                triangles.Add(i + slices + 1);
                triangles.Add(i + 2 * slices + 1);
                triangles.Add(((i == slices - 1) ? -1 : i) + slices + 2);
            }
            for (int i = 0; i < slices; i++)
            {
                triangles.Add(i + 2 * slices + 1);
                triangles.Add(((i == slices - 1) ? -1 : i) + 2 * slices + 2);
                triangles.Add(((i == slices - 1) ? -1 : i) + slices + 2);
            }
            for (int i = 0; i < slices; i++)
            {
                triangles.Add(3 * slices + 1);
                triangles.Add(((i == slices - 1) ? -1 : i) + 3 * slices + 3);
                triangles.Add(i + 3 * slices + 2);
            }

            meshFilter.mesh.vertices = vertices.ToArray();
            meshFilter.mesh.uv = uv.ToArray();
            meshFilter.mesh.normals = normals.ToArray();
            meshFilter.mesh.triangles = triangles.ToArray();

            // Give it a mesh renderer
            MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.material.shader = Shader.Find("KSP/Diffuse");
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, rodColor);
            tex.Apply();
            renderer.material.mainTexture = tex;
            renderer.material.color = rodColor;

            // Set up a parent transform which we will maintain as a copy of the Kerbal's right hand (non-active transform).
            parent = new GameObject("fishingPoleParent");
            transform.parent = parent.transform;
            transform.localPosition = new Vector3(0.0f, 0.03f, 0.0f);
        }

        void Start()
        {
            Debug.Log("FishingPole.Start()");
        }

        void Update()
        {
        }

        void OnDestroy()
        {
            Destroy(parent);
        }

        void FixedUpdate()
        {
            parent.transform.localPosition = referenceTransform.position;
            parent.transform.localRotation = referenceTransform.rotation;
        }
    }
}
