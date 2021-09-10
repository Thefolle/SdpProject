# RotationMath

## Rotation of a simple vector in R2

### Clockwise rotation

direction = d = u_x = (1, 0)

M1 = (0, -1)
     (1, 0)

v4 = d * M1 = (0, -1) = -u_y
v5 = v4 * M1 = (-1, 0) = -u_x
v6 = v5 * M1 = (0, 1) = u_y
v7 = v6 * M1 = (1, 0) = u_x

### Counter-clockwise rotation

M2 = (0, 1)
     (-1, 0)

v8 = d * M2 = (0, 1) = u_y
v9 = v8 * M2 = (-1, 0) = -u_x
v10 = v9 * M2 = (0, -1) = -u_y
v11 = v10 * M2 = (1, 0) = u_x = d

What happens if the clockwise rotation matrix is applied?
v8 * M1 = (1, 0) = u_x
The operation brings back the initial vector.

## Rotation of a general vector in R2

d = (x0, y0)

M3 = M1

v12 = d * M3 = (y0, -x0); prove of orthogonality: d * v12 = x0*y0 - x0*y0 = 0
v13 = v12 * M3 = (-x0, -y0); prove of orthogonality: v13 * v12 = -x0y0 + x0y0 = 0
v14 = v13 * M3 = (-y0, x0); prove of orthogonality: v14 * v13 = x0y0 - x0y0 = 0
v15 = v14 * M3 = (x0, y0); prove of orthogonality: v15 * v14 = -x0y0 + x0y0 = 0

## Clockwise rotation of a general vector in R3 with respect to the z axis

d = (x0, y0, z0)

M4 = M1|R2->R3 = (0, -1, 0)
                 (1, 0, 0)
                 (0, 0, 1)

v16 = d * M4 = (y0, -x0, z0)
v17 = v16 * M4 = (-x0, -y0, z0)
...

These vectors share the same x and y coordinates of the case of rotation in R2, while z0 remains as is.
