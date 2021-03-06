﻿module Terrain

open Rendering
open OpenTK
open CjClutter.OpenGl
open CjClutter.OpenGl.OpenGl
open CjClutter.OpenGl.Gui
open CjClutter.OpenGl.EntityComponent
open OpenTK.Graphics.OpenGL4
open System.Collections.Generic
open System.Linq

type node = ChunkedLodTreeFactory.ChunkedLodTreeNode

let resourceFactory = new ResourceAllocator(new OpenGlResourceFactory())

let allocate (node:node) =
    let factory = new TerrainChunkFactory()
    let mesh = factory.Create(node.Bounds)
    let allocatedMesh = resourceFactory.AllocateResourceFor(mesh)
    let bounds = node.Bounds
    let translation = Matrix4.CreateTranslation(float32 bounds.Center.X, 0.0f, float32 bounds.Center.Y)
    let delta = bounds.Max - bounds.Min
    let scale = Matrix4.CreateScale(float32 delta.X, 1.0f, float32 delta.Y)
    let bind () =
        allocatedMesh.CreateVAO()
        allocatedMesh.VertexArrayObject.Bind()
    { 
        Bind = bind
        Faces = mesh.Faces.Length
//        renderContext = {
//                            ModelMatrix = scale * translation
//                            NormalMatrix = Matrix3.Identity
//                        }
    }

let allocateElementBuffer =
    let buffer = GL.GenBuffer()
    
    let faces = CjClutter.OpenGl.EntityComponent.MeshCreator.CreateFaces(127, 127)
    let indices = faces |> Array.ofSeq |> Array.collect (fun f -> [|uint32(f.V0);uint32(f.V1);uint32(f.V2)|])
    GL.BindBuffer(BufferTarget.ElementArrayBuffer, buffer)

    let size = nativeint(sizeof<uint32> * indices.Length)
    GL.BufferData(BufferTarget.ElementArrayBuffer, size, indices, BufferUsageHint.StaticRead)
    (buffer, faces.Count)
   
let allocateGpu (elementBuffer:int) (elements:int) (noiseShader:NoiseShaderProgram.NoiseShader) (node:node) =
    
    let storageBuffer = GL.GenBuffer()
    GL.BindBuffer(BufferTarget.ShaderStorageBuffer, storageBuffer)
    let numberOfPoints = 128 * 128
    let numberOfFloats = 8 * numberOfPoints
    let a:float32[] = null
    let size:nativeint = nativeint(sizeof<float32> * numberOfFloats)
    GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 4, storageBuffer)
    GL.BufferData(BufferTarget.ShaderStorageBuffer, size, a, BufferUsageHint.StaticRead)

    GL.UseProgram(noiseShader.ProgramId)

    let bounds = node.Bounds

    let max = new Vector2(float32 bounds.Max.X, float32 bounds.Max.Y)
    let min = new Vector2(float32 bounds.Min.X, float32 bounds.Min.Y)
    noiseShader.Max.set max
    noiseShader.Min.set min
    let widthInPoints = 128
    let meshDimensions = float32 (widthInPoints - 1)
    let numberOfPoints = 128 * 128
    noiseShader.Transform.set (Matrix4.CreateTranslation(-meshDimensions / 2.0f, 0.0f, -meshDimensions / 2.0f) * Matrix4.CreateScale(1.0f / meshDimensions, 1.0f, 1.0f / meshDimensions))
    noiseShader.NormalTransform.set OpenTK.Matrix3.Identity
    GL.DispatchCompute(numberOfPoints / 128, 1, 1)
    GL.MemoryBarrier(MemoryBarrierFlags.ShaderStorageBarrierBit)

    let vertexArray = GL.GenVertexArray()
    GL.BindVertexArray(vertexArray)
    GL.EnableVertexAttribArray(0)
    GL.EnableVertexAttribArray(1)
    GL.BindBuffer(BufferTarget.ElementArrayBuffer, elementBuffer)
    GL.BindBuffer(BufferTarget.ArrayBuffer, storageBuffer)
    GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, sizeof<float32> * 8, 0)
    GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, sizeof<float32> * 8, 4)
   
    GL.BindVertexArray(0)
    GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0)
    GL.BindBuffer(BufferTarget.ArrayBuffer, 0)

    let bounds = node.Bounds
    let translation = Matrix4.CreateTranslation(float32 bounds.Center.X, 0.0f, float32 bounds.Center.Y)
    let delta = bounds.Max - bounds.Min
    let scale = Matrix4.CreateScale(float32 delta.X, 1.0f, float32 delta.Y)
    {
        Bind = fun() -> 
            GL.BindVertexArray(vertexArray)
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, elementBuffer)
            GL.BindBuffer(BufferTarget.ArrayBuffer, storageBuffer)
        Faces = elements
    }
    
type NoiseShaderProgram = 
    {
        programId : int
    }

let makeTerrainLodTree =
    CjClutter.OpenGl.Terrain.CreateTree()
