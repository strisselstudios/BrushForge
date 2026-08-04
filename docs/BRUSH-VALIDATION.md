# BrushForge Convex-Brush Validation

## 1. Plane orientation

Every brush-face normal must point outward.

Brush interior points satisfy:

face.Plane.SignedDistanceTo(point) <= tolerance

A reversed face changes the represented half-space and normally creates an
open, unbounded, or empty result.

## 2. Vertex reconstruction

Candidate vertices are calculated from every unique combination of three face
planes.

A candidate is retained only when:

- The three planes have a stable finite intersection.
- The point lies inside every face half-space.
- It is not within the configured merge tolerance of an existing vertex.

This avoids relying on the three serialized face points as literal polygon
corners. In Valve map files, those points define an infinite oriented plane.

## 3. Face reconstruction

A reconstructed vertex belongs to a face when its signed distance from that
face plane is within the plane-membership tolerance.

Each face must reconstruct to at least three distinct vertices.

Face vertices are ordered around the outward normal using a deterministic
two-dimensional basis on the face plane.

## 4. Closure

Every polygon edge must be shared by exactly two reconstructed faces.

An edge used once indicates an opening. An edge used more than twice indicates
non-manifold geometry.

Each reconstructed vertex must belong to at least three faces.

## 5. Convexity

Convexity is enforced by the half-space test. Every retained vertex must lie
on or behind every outward face plane.

Redundant planes are rejected because they do not reconstruct to a real face
polygon.

## 6. Finite dimensions

A valid brush requires:

- Positive X extent
- Positive Y extent
- Positive Z extent
- Positive face areas
- Positive enclosed volume

The default tolerances are deliberately much smaller than normal TrenchBroom
grid units. Later generator-level minimum brush thickness rules will be more
restrictive.

## 7. Calculated geometry

A successful validation result supplies:

- Unique vertices
- Ordered face polygons
- Bounds
- Centroid
- Edge count
- Enclosed volume
- Closure state

This geometry can later be reused by preview rendering, export validation, and
brush-complexity analysis.
