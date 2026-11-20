using System.Collections.Generic;
using UnityEngine;

namespace LastLight
{
    /// <summary>
    /// Procedural faceted low-poly mesh generator ("POLYGON Horror Mansion" flat-shaded angular style)
    /// AND realistic imported Humanoid/Generic character rig attachment helper.
    /// </summary>
    public static class CharacterBuilder
    {
        // =========================================================================
        // IMPORTED CHARACTER RIGHT-HAND BONE & PROCEDURAL STRIDE BINDING
        // =========================================================================

        /// <summary>
        /// Finds the Right Hand bone (HumanBodyBones.RightHand if Humanoid Animator,
        /// or common right-hand transform names) on an imported character model, or creates
        /// a clean right-hand socket transform, and parents the lantern prop to it.
        /// </summary>
        public static Transform FindOrCreateRightHandSocket(GameObject modelRoot)
        {
            if (modelRoot == null) return null;

            Animator anim = modelRoot.GetComponentInChildren<Animator>();
            if (anim != null && anim.isHuman)
            {
                Transform rh = anim.GetBoneTransform(HumanBodyBones.RightHand);
                if (rh != null) return rh;
            }

            // Fallback: search common rig naming conventions (Mixamo, Rigify, Blender, FBX)
            Transform[] all = modelRoot.GetComponentsInChildren<Transform>(true);
            string[] handTokens = { "righthand", "hand_r", "hand.r", "r_hand", "rhand", "right_hand", "handr" };
            for (int i = 0; i < all.Length; i++)
            {
                string lower = all[i].name.ToLowerInvariant().Replace(" ", "");
                for (int t = 0; t < handTokens.Length; t++)
                {
                    if (lower.Contains(handTokens[t]))
                    {
                        return all[i];
                    }
                }
            }

            // Final fallback: dedicated right-hand socket transform at natural carry position
            GameObject socket = new GameObject("RightHandLanternSocket");
            socket.transform.SetParent(modelRoot.transform, false);
            socket.transform.localPosition = new Vector3(0.34f, 0.96f, 0.30f);
            return socket.transform;
        }

        /// <summary>
        /// Binds imported Humanoid limb bones into PlayerCharacterVisual.HumanoidRig
        /// so walking stride animation works even without external animation clips.
        /// </summary>
        public static void BindImportedAnimatorBones(GameObject modelRoot, PlayerCharacterVisual.HumanoidRig rig)
        {
            if (modelRoot == null || rig == null) return;

            Animator anim = modelRoot.GetComponentInChildren<Animator>();
            if (anim == null)
            {
                anim = modelRoot.AddComponent<Animator>();
            }

            if (anim.isHuman)
            {
                rig.leftArm = anim.GetBoneTransform(HumanBodyBones.LeftUpperArm);
                rig.rightArm = anim.GetBoneTransform(HumanBodyBones.RightUpperArm);
                rig.leftLeg = anim.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
                rig.rightLeg = anim.GetBoneTransform(HumanBodyBones.RightUpperLeg);
            }
        }

        // =========================================================================
        // FACETED LOW-POLY MESH GENERATORS (FLAT-SHADED ANGULAR STYLE)
        // =========================================================================

        public static Mesh CreateTaperedPrismMesh(string name, int sides, float bottomRadiusX, float bottomRadiusZ, float topRadiusX, float topRadiusZ, float height)
        {
            sides = Mathf.Max(3, sides);
            float halfH = height * 0.5f;

            List<Vector3> verts = new List<Vector3>();
            List<int> tris = new List<int>();

            Vector3[] botRing = new Vector3[sides];
            Vector3[] topRing = new Vector3[sides];

            for (int i = 0; i < sides; i++)
            {
                float a = (i / (float)sides) * Mathf.PI * 2f + Mathf.PI / sides;
                float c = Mathf.Cos(a);
                float s = Mathf.Sin(a);
                botRing[i] = new Vector3(c * bottomRadiusX, -halfH, s * bottomRadiusZ);
                topRing[i] = new Vector3(c * topRadiusX, halfH, s * topRadiusZ);
            }

            Vector3 topCenter = new Vector3(0f, halfH, 0f);
            Vector3 botCenter = new Vector3(0f, -halfH, 0f);

            for (int i = 0; i < sides; i++)
            {
                int next = (i + 1) % sides;
                AddFlatQuad(verts, tris, botRing[i], topRing[i], topRing[next], botRing[next]);
                if (topRadiusX > 0.001f || topRadiusZ > 0.001f) AddFlatTri(verts, tris, topCenter, topRing[next], topRing[i]);
                if (bottomRadiusX > 0.001f || bottomRadiusZ > 0.001f) AddFlatTri(verts, tris, botCenter, botRing[i], botRing[next]);
            }

            return FinalizeFlatMesh(name, verts, tris);
        }

        public static Mesh CreateFacetedEllipsoidMesh(string name, int lonSegments, int latSegments, Vector3 radii)
        {
            lonSegments = Mathf.Max(5, lonSegments);
            latSegments = Mathf.Max(3, latSegments);

            List<Vector3> verts = new List<Vector3>();
            List<int> tris = new List<int>();

            Vector3[,] grid = new Vector3[latSegments + 1, lonSegments];
            for (int lat = 0; lat <= latSegments; lat++)
            {
                float v = (float)lat / latSegments;
                float phi = (v - 0.5f) * Mathf.PI;
                float cosPhi = Mathf.Cos(phi);
                float sinPhi = Mathf.Sin(phi);

                for (int lon = 0; lon < lonSegments; lon++)
                {
                    float theta = (lon / (float)lonSegments) * Mathf.PI * 2f;
                    grid[lat, lon] = new Vector3(
                        Mathf.Cos(theta) * cosPhi * radii.x,
                        sinPhi * radii.y,
                        Mathf.Sin(theta) * cosPhi * radii.z
                    );
                }
            }

            for (int lat = 0; lat < latSegments; lat++)
            {
                for (int lon = 0; lon < lonSegments; lon++)
                {
                    int nextLon = (lon + 1) % lonSegments;
                    Vector3 p00 = grid[lat, lon];
                    Vector3 p10 = grid[lat + 1, lon];
                    Vector3 p11 = grid[lat + 1, nextLon];
                    Vector3 p01 = grid[lat, nextLon];

                    if (lat == 0) AddFlatTri(verts, tris, p00, p10, p11);
                    else if (lat == latSegments - 1) AddFlatTri(verts, tris, p00, p10, p01);
                    else AddFlatQuad(verts, tris, p00, p10, p11, p01);
                }
            }

            return FinalizeFlatMesh(name, verts, tris);
        }

        public static Mesh CreateWedgeTentMesh(string name, float width, float height, float length)
        {
            float hw = width * 0.5f;
            float hl = length * 0.5f;

            Vector3 fl = new Vector3(-hw, 0f, hl);
            Vector3 fr = new Vector3(hw, 0f, hl);
            Vector3 ft = new Vector3(0f, height, hl);

            Vector3 bl = new Vector3(-hw, 0f, -hl);
            Vector3 br = new Vector3(hw, 0f, -hl);
            Vector3 bt = new Vector3(0f, height, -hl);

            List<Vector3> verts = new List<Vector3>();
            List<int> tris = new List<int>();

            AddFlatQuad(verts, tris, bl, bt, ft, fl);
            AddFlatQuad(verts, tris, fr, ft, bt, br);
            AddFlatTri(verts, tris, bl, br, bt);
            AddFlatTri(verts, tris, fr, fl, ft);
            AddFlatQuad(verts, tris, bl, fl, fr, br);

            return FinalizeFlatMesh(name, verts, tris);
        }

        /// <summary>
        /// Builds a double-sided UV-mapped corner cobweb quad mesh so webs are visible from both sides.
        /// </summary>
        public static Mesh CreateDoubleSidedWebQuad(string name, float size)
        {
            float h = size * 0.5f;
            Vector3[] verts =
            {
                // Front face
                new Vector3(-h, -h, 0f), new Vector3(-h, h, 0f), new Vector3(h, h, 0f), new Vector3(h, -h, 0f),
                // Back face
                new Vector3(-h, -h, 0f), new Vector3(-h, h, 0f), new Vector3(h, h, 0f), new Vector3(h, -h, 0f)
            };
            Vector2[] uvs =
            {
                new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0),
                new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0)
            };
            int[] tris =
            {
                0, 1, 2, 0, 2, 3,
                4, 6, 5, 4, 7, 6
            };

            Mesh m = new Mesh { name = name };
            m.SetVertices(new List<Vector3>(verts));
            m.SetUVs(0, new List<Vector2>(uvs));
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        private static void AddFlatTri(List<Vector3> verts, List<int> tris, Vector3 a, Vector3 b, Vector3 c)
        {
            int idx = verts.Count;
            verts.Add(a);
            verts.Add(b);
            verts.Add(c);
            tris.Add(idx);
            tris.Add(idx + 1);
            tris.Add(idx + 2);
        }

        private static void AddFlatQuad(List<Vector3> verts, List<int> tris, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            AddFlatTri(verts, tris, a, b, c);
            AddFlatTri(verts, tris, a, c, d);
        }

        private static Mesh FinalizeFlatMesh(string name, List<Vector3> verts, List<int> tris)
        {
            Mesh m = new Mesh { name = name };
            m.SetVertices(verts);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }
    }
}
