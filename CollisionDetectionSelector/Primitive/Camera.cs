﻿using System;
using OpenTK.Graphics.OpenGL;
using Math_Implementation;

namespace CollisionDetectionSelector.Primitive {
    class Camera {
        //members to hold position and orientation
        protected Vector3 position = new Vector3(0f, 0f, 0f);
        protected Vector3 forward = new Vector3(0f, 0f, 1f);
        protected Vector3 right = new Vector3(1f, 0f, 0f);
        protected Vector3 up = new Vector3(0f, 1f, 0f);

        //cached matrices to avoid work every frame
        protected bool worldDirty = true;
        protected bool viewDirty = true;
        protected Matrix4 cachedWorld = new Matrix4();
        protected Matrix4 cachedView = new Matrix4();

        public Matrix4 ProjectionMatrix {
            get {
                float[] rawPorjection = new float[16];
                GL.GetFloat(GetPName.ProjectionMatrix, rawPorjection);
                return Matrix4.Transpose(new Matrix4(rawPorjection));
            }
        }
        public Matrix4 Translation {
            get {
                //Identity, with 4th column being positions
                return new Matrix4(1f, 0f, 0f, position.X,
                                   0f, 1f, 0f, position.Y,
                                   0f, 0f, 1f, position.Z,
                                   0f, 0f, 0f, 1f);
            }
        }
        public Matrix4 Orientation {
            get {
                //return orientation matrix
                return new Matrix4(right.X,up.X,-forward.X,0f,
                                   right.Y,up.Y,-forward.Y,0f,
                                   right.Z,up.Z,-forward.Z,0f,
                                   0f,      0f,     0f,   1f
                                   );
            }
        }
        public Matrix4 WorldMatrix {
            get {
                if (worldDirty) {
                    //post multiplication
                    //rotate, translate, always reverse order
                    cachedWorld = Translation * Orientation;
                }
                worldDirty = false;
                return cachedWorld;
            }
        }
        public Matrix4 ViewMatrix {
            get {
                if (viewDirty) {
                    cachedView = Matrix4.Inverse(WorldMatrix);
                }
                viewDirty = false;
                return cachedView;
            }
        }
        #region READONLY
        public Point Position {
            get {
                return new Point(position.X, position.Y, position.Z);
            }
        }
        public Vector3 Forward {
            get {
                return new Vector3(forward.X, forward.Y, forward.Z);
            }
        }
        public Vector3 Right {
            get {
                return new Vector3(right.X, right.Y, right.Z);
            }
        }
        public Vector3 Up {
            get {
                return new Vector3(up.X, up.Y, up.Z);
            }
        }
        #endregion
        //same as matrix4.LookAt
        public void LookAt(Vector3 camPos, Vector3 camTarget, Vector3 camUp) {
            worldDirty = true;
            viewDirty = true;

            forward = Vector3.Normalize(camTarget - camPos);
            right = Vector3.Normalize(Vector3.Cross(forward, camUp));
            up = Vector3.Cross(right, forward);

            //set to a copy not reference
            position = new Vector3(camPos.X, camPos.Y, camPos.Z);
        }
        #region INTERACTIVE
        public void Pan(float horizontal, float vertical) {
            worldDirty = true;
            viewDirty = true;
            position.X += horizontal;
            position.Y += vertical;
        }
        public void Zoom(float value) {
            worldDirty = true;
            viewDirty = true;
            position.Z += value;
        }
        public void Pivot(float yaw, float pitch) {
            worldDirty = true;
            viewDirty = true;

            Matrix4 yawAngle = Matrix4.AngleAxis(yaw, up.X, up.Y, up.Z);
            forward = Matrix4.MultiplyVector(yawAngle, forward);

            Matrix4 pitchAngle = Matrix4.AngleAxis(pitch, right.X, right.Y, right.Z);
            forward = Matrix4.MultiplyVector(pitchAngle, forward);

            forward = Vector3.Normalize(forward);
            right = Vector3.Normalize(right);
            up = Vector3.Normalize(up);
        }
        #endregion

        #region FRUSTUM
        private static Plane FromNumbers(Vector4 numbers) {
            Vector3 abc = new Vector3(numbers.X, numbers.Y, numbers.Z);
            float mag = abc.Length();
            abc.Normalize();

            Plane p = new Plane();
            p.Normal = abc;
            p.Distance = numbers.W / mag;
            return p;
        }
        
        public Plane[] Frustum {
            get {
                Plane[] frustum = new Plane[6];

                Matrix4 vp = ProjectionMatrix * ViewMatrix;

                Vector4[] rows = new Vector4[4] {new Vector4(vp[0,0],vp[0,1],vp[0,2],vp[0,3]),
                                                 new Vector4(vp[1,0],vp[1,1],vp[1,2],vp[1,3]),
                                                 new Vector4(vp[2,0],vp[2,1],vp[2,2],vp[2,3]),
                                                 new Vector4(vp[3,0],vp[3,1],vp[3,2],vp[3,3]),
                                                 };

                frustum[0] = FromNumbers(rows[3] + rows[0]);
                frustum[1] = FromNumbers(rows[3] - rows[0]);
                frustum[2] = FromNumbers(rows[3] + rows[1]);
                frustum[3] = FromNumbers(rows[3] - rows[1]);
                frustum[4] = FromNumbers(rows[3] + rows[2]);
                frustum[5] = FromNumbers(rows[3] - rows[2]);

                return frustum;
                //frustum[0] = left plane
                //frustum[1] = right plane
                //frustum[2] = bottom plane
                //frustum[3] = top plane
                //frustum[4] = near plane
                //frustum[5] = far plane
            }
        }
        #endregion
    }
}
