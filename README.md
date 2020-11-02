# Poly Nester

Given a set of 2D irregular shapes (polygons), we would like to enclose the collection in the smallest possible container such that no two shapes overlap. The problem is interesting from a purely mathematical view point, but also sees various applications ranging from CNC cutting, to circuit design, to 3D art creation, and more.

The method here is based on no-fit-polygon generation and heuristic techniques for finding optimal polygon orientation and orderings. In this sense, the method trades off optimality for efficiency.

## Usage and Examples

We define our polygon collection using traditional vertex and index buffers as described [here](https://docs.microsoft.com/en-us/windows/win32/direct3d9/rendering-from-vertex-and-index-buffers). Also, define the target container: a 500 x 400 unit box.

```cs
Vector64[] verts = data.verts;
int[] tris = data.tris;
Rect64 container = new Rect64(0, 400, 500, 0);
```

Create a Nester class object and pass our data, with an optional padding parameter.

```cs
Nester nester = new Nester();
int[] handles = nester.AddUVPolygons(verts, tris, 0.0);
```

Queue commands for determining optimal rotation, nesting and refitting the polygons into a container.

```cs
nester.CMD_OptimalRotation(null);
nester.CMD_Nest(null, NFPQUALITY.Full);
nester.CMD_Refit(container, false, null);
```

Execute command buffer with async callbacks for progress updates and completion of commands.

```cs
nester.ExecuteCommandBuffer(NesterProgress, NesterComplete);
```

Various quality settings trade off speed for quality of solutions.

```cs
public enum NFPQUALITY
{
    Simple,         // simplify shapes to bounding boxes
    Convex,         // use convex hull of shapes
    Concave,        // use shape boundary (ignore holes)
    Full            // use full shape boundary and holes
}
```

We can compare the results under these settings on a model with 1622 irregular shapes.
