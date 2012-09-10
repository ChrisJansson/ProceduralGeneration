using CjClutter.OpenGl.Noise;
using CjClutter.OpenGl.OpenGl;
using CjClutter.OpenGl.OpenGl.VertexTypes;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace CjClutter.OpenGl.SceneGraph
{
    public class Scene
    {
        private readonly SimplexNoise _noiseGenerator;
        private VertexBuffer<Vertex3V> _vertexBuffer;

        private const int TerrainWidth = 1280;
        private const int TerrainHeight = 1280;

        public Scene()
        {
            _noiseGenerator = new SimplexNoise();
        }

        public Matrix4d ViewMatrix { get; set; }
        public Matrix4d ProjectionMatrix { get; set; }

        public void OnLoad()
        {
            var heightMap = new Vector3[TerrainWidth, TerrainHeight];

            for (var i = 0; i < TerrainWidth; i++)
            {
                for (var j = 0; j < TerrainHeight; j++)
                {
                    var xin = i / (double)TerrainWidth * 2;
                    var yin = j / (double)TerrainHeight * 2;
                    var y = 0.2 * _noiseGenerator.Noise(xin, yin);
                    var x = -0.5 + ScaleTo(i, TerrainWidth);
                    var z = -0.5 + ScaleTo(j, TerrainHeight);

                    heightMap[i, j] = new Vector3((float)x, (float)y, (float)z);
                }
            }

            var vertices = new Vertex3V[(TerrainWidth - 1) * (TerrainHeight - 1) * 3 * 2];
            var vertexIndex = 0;

            for (var i = 0; i < TerrainWidth - 1; i++)
            {
                for (var j = 0; j < TerrainHeight - 1; j++)
                {
                    var v0 = heightMap[i, j];
                    var v1 = heightMap[i + 1, j];
                    var v2 = heightMap[i + 1, j + 1];
                    var v3 = heightMap[i, j + 1];

                    vertices[vertexIndex++] = new Vertex3V { Position = v0 };
                    vertices[vertexIndex++] = new Vertex3V { Position = v1 };
                    vertices[vertexIndex++] = new Vertex3V { Position = v2 };

                    vertices[vertexIndex++] = new Vertex3V { Position = v0 };
                    vertices[vertexIndex++] = new Vertex3V { Position = v2 };
                    vertices[vertexIndex++] = new Vertex3V { Position = v3 };
                }
            }

            _vertexBuffer = new VertexBuffer<Vertex3V>();
            _vertexBuffer.Generate();
            _vertexBuffer.Bind();
            _vertexBuffer.Data(vertices);
            _vertexBuffer.Unbind();

            var vertexShader = new Shader();
            vertexShader.Create(ShaderType.VertexShader);
            vertexShader.SetSource(VertexShader);
            vertexShader.Compile();

            var fragmentShader = new Shader();
            fragmentShader.Create(ShaderType.FragmentShader);
            fragmentShader.SetSource(FragmentShader);
            fragmentShader.Compile();

            _renderProgram = new Program();
            _renderProgram.Create();
            _renderProgram.AttachShader(vertexShader);
            _renderProgram.AttachShader(fragmentShader);
            _renderProgram.Link();
            _renderProgram.Use();

            _vertexArrayObject = new VertexArrayObject();
            _vertexArrayObject.Create();
            _vertexArrayObject.Bind();

            

            _vertexBuffer.Bind();
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, 0);
            GL.EnableVertexAttribArray(0);

            _vertexArrayObject.Unbind();
        }

        public void Update(double elapsedTime) { }

        public void Draw()
        {
            _vertexArrayObject.Bind();
            _renderProgram.Use();

            var projectionLocation = GL.GetUniformLocation(_renderProgram.ProgramId, "projection");
            var viewLocation = GL.GetUniformLocation(_renderProgram.ProgramId, "view");

            var projectionMatrix = new Matrix4(
                (float)ProjectionMatrix.M11,
                (float)ProjectionMatrix.M12,
                (float)ProjectionMatrix.M13,
                (float)ProjectionMatrix.M14,
                (float)ProjectionMatrix.M21,
                (float)ProjectionMatrix.M22,
                (float)ProjectionMatrix.M23,
                (float)ProjectionMatrix.M24,
                (float)ProjectionMatrix.M31,
                (float)ProjectionMatrix.M32,
                (float)ProjectionMatrix.M33,
                (float)ProjectionMatrix.M34,
                (float)ProjectionMatrix.M41,
                (float)ProjectionMatrix.M42,
                (float)ProjectionMatrix.M43,
                (float)ProjectionMatrix.M44);

            GL.UniformMatrix4(projectionLocation, false, ref projectionMatrix);

            var viewMatrix = new Matrix4(
                (float)ViewMatrix.M11,
                (float)ViewMatrix.M12,
                (float)ViewMatrix.M13,
                (float)ViewMatrix.M14,
                (float)ViewMatrix.M21,
                (float)ViewMatrix.M22,
                (float)ViewMatrix.M23,
                (float)ViewMatrix.M24,
                (float)ViewMatrix.M31,
                (float)ViewMatrix.M32,
                (float)ViewMatrix.M33,
                (float)ViewMatrix.M34,
                (float)ViewMatrix.M41,
                (float)ViewMatrix.M42,
                (float)ViewMatrix.M43,
                (float)ViewMatrix.M44);

            GL.UniformMatrix4(viewLocation, false, ref viewMatrix);

            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
            GL.DrawArrays(BeginMode.Triangles, 0, (TerrainWidth - 1) * (TerrainHeight - 1) * 3 * 2);
            GL.PolygonMode(MaterialFace.Front, PolygonMode.Fill);

            _renderProgram.Unbind();
            _vertexArrayObject.Unbind();
        }

        private double ScaleTo(double value, double max)
        {
            return value / max;
        }

        private const string VertexShader = @"#version 330

layout(location = 0)in vec4 vert;

uniform mat4 projection;
uniform mat4 view;
uniform mat4 model;

void main()
{
    gl_Position = projection * view * vert;
}";

        private const string FragmentShader = @"#version 330

out vec4 fragColor;

void main()
{
    fragColor = vec4(1.0, 0.0, 0.0, 1.0);
}";

        private Program _renderProgram;
        private VertexArrayObject _vertexArrayObject;
    }
}
