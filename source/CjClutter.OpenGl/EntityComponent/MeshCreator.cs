﻿using System.Collections.Generic;
using CjClutter.OpenGl.OpenGl.VertexTypes;
using CjClutter.OpenGl.SceneGraph;
using OpenTK;

namespace CjClutter.OpenGl.EntityComponent
{
    public class MeshCreator
    {
        public static Mesh3V3N CreateXZGrid(int columns, int rows)
        {
            return CreateFromHeightMap(columns, rows, new FlatHeightMap());
        }

        public static Mesh3V3N CreateFromHeightMap(int columns, int rows, IHeightMap heightMap)
        {
            var vertices = new List<Vertex3V3N>();
            for (var x = 0; x <= columns; x++)
            {
                for (var y = 0; y <= rows; y++)
                {
                    var height = heightMap.GetHeight(x, y);
                    vertices.Add(new Vertex3V3N
                    {
                        Position = new Vector3((float) x/columns - 0.5f, (float) height, (float) y/rows - 0.5f),
                        Normal = heightMap.GetNormal(x, y)
                    });
                }
            }

            var faces = new List<Face3>();
            for (var x = 0; x < columns; x++)
            {
                for (var y = 0; y < rows; y++)
                {
                    var verticesInColumn = rows + 1;
                    var v0 = x*verticesInColumn + y;
                    var v1 = (x + 1)*verticesInColumn + y;
                    var v2 = (x + 1)*verticesInColumn + y + 1;
                    var v3 = x*verticesInColumn + y + 1;

                    var f0 = new Face3 {V0 = v0, V1 = v1, V2 = v2};
                    var f1 = new Face3 {V0 = v0, V1 = v2, V2 = v3};

                    faces.Add(f0);
                    faces.Add(f1);
                }
            }

            return new Mesh3V3N(vertices, faces);
        }

        private class FlatHeightMap : IHeightMap
        {
            public double GetHeight(int column, int row)
            {
                return 0;
            }

            public Vector3 GetNormal(int column, int row)
            {
                return new Vector3(0, 1, 0);
            }
        }
    }
}

    public interface IHeightMap
    {
        double GetHeight(int column, int row);
        Vector3 GetNormal(int column, int row);
    }
}