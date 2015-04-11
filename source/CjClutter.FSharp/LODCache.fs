﻿module LODCache
open CjClutter.OpenGl
open System

type node = ChunkedLodTreeFactory.ChunkedLodTreeNode

type LODCache = {
        contains : node -> bool
        beginCache : node -> unit
        get : node -> Rendering.AllocatedMesh
    }

let getNodesToDrawAndCache cache (requestedNodes:node array) =

    let rec isDescendantOf (r:node) (n:node) =
        if n.Parent <> null && n.Parent.Equals(r) then
            true
        else if n.Parent <> null then
            isDescendantOf r n.Parent
        else
            false
//        match n.Parent with
//        | r -> true
//        | _ when n.Parent <> null -> isDescendantOf r n.Parent
//        | _ -> false

    let removeDescendantsWhenParentIsRequested nodes =
        let hasAncestorIn nodes n =
            nodes |> Array.exists (fun root -> isDescendantOf root n)
        nodes |> Array.where (fun n -> not(hasAncestorIn nodes n))
        
    let cacheContainsNode n = cache.contains n
    let rec getNodesToDrawInternal (requested, notCached) =
        let allAreCached = requested |> Array.forall cacheContainsNode
//        let isRoot = requested.Length = 1 && requestedNodes.[0].Parent = null //needs a better check
        match allAreCached with
        | true -> (requested, notCached)
        | _ -> 
            let notCachedNodes = requested |> Array.filter (fun n -> not (cacheContainsNode n))
            let notCachedNodesParents = notCachedNodes |> Array.map (fun n -> n.Parent) |> Array.filter (fun n -> n <> null) |> Array.distinct 
            let cachedNodes = requestedNodes |> Array.filter cacheContainsNode //This should check descendants, not only parents
//            let cachedNodesNotDescendedFromRequestedParent = Array.filter a cachedNodes
            let nodesToRequest = Array.concat [cachedNodes; notCachedNodesParents] |> removeDescendantsWhenParentIsRequested
            let nodesToCache = Array.concat [notCachedNodes; notCached]
            getNodesToDrawInternal (nodesToRequest, nodesToCache)

    getNodesToDrawInternal (requestedNodes, [||])

let queueNodes cache (nodes: node array) =
    let largestFirst = nodes |> Array.sortBy (fun n -> n.GeometricError)
    for n in largestFirst do
        cache.beginCache n

let getMeshesFromCache cache (requestedNodes:node array) =
    requestedNodes |> Array.map (fun n -> cache.get n)

let getMeshesToDraw cache (requestedNodes:node array) = 
    let (nodesToDraw, nodesToCache) = getNodesToDrawAndCache cache requestedNodes
    queueNodes cache nodesToCache
    getMeshesFromCache cache nodesToDraw
    
type CachedNode = {
        mutable mesh : option<Rendering.AllocatedMesh>
    }
   
let makeCache (chunkFactory : node -> Rendering.AllocatedMesh) =
    let dict = new System.Collections.Concurrent.ConcurrentDictionary<node, CachedNode>()

    let contains node = 
        let result = dict.TryGetValue(node)
        match result with
        | (false, _) -> false
        | (true, cachedNode) ->
            match cachedNode.mesh with
            | Some x -> true
            | None -> false

    let get node = 
        let m = dict.[node].mesh
        match m with
        | Some mesh -> mesh
        | _ -> failwith "Something went wrong!"

    let beginCache node = 
        let cn = { CachedNode.mesh = None }
        dict.TryAdd(node, cn) |> ignore
        let work() = 
            let mesh = chunkFactory node
            cn.mesh <- Some mesh
        ()
        CjClutter.OpenGl.Gui.JobDispatcher.Instance.Enqueue(Action work)
    {
        contains = contains
        get = get
        beginCache = beginCache
    }



