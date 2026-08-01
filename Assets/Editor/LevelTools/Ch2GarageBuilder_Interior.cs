using UnityEditor;
using UnityEngine;

namespace SecretsThatBreathe.LevelTools
{
    // Interior shell: partition, mezzanine, stairs, roof structure, lighting, floor graphics.
    public static partial class Ch2GarageBuilder
    {
        public const float OFF_BACK_Z = 2.0f;      // office block rear wall
        public const float OFF_L_X = PARTX;        // 3.8
        public const float OFF_R_X = X1 + WT * 0.5f;
        const float PT = 0.15f;                    // partition thickness

        static void BuildInteriorStructure()
        {
            var g = Group("INT_Structure", _root);

            BuildPartition(g);
            BuildMezzanine(g);
            BuildStairs(g);
            BuildRearShutter(g);
            BuildInteriorDoors(g);
            BuildRoofStructure(g);
            BuildInteriorLighting(g);
            BuildFloorGraphics(g);
            BuildServices(g);
        }

        // ── partition between workshop and office block (x = 3.8) ──
        static void BuildPartition(Transform parent)
        {
            var g = Group("Partition_Wall", parent);
            float zA = Z0 - WT * 0.5f;    // -7.125
            float zB = OFF_BACK_Z;        //  2.0

            // ground level, door opening z -2.6 .. -1.6
            Box("Lower_A", g, new Vector3(PARTX, 1.575f, (zA + -2.6f) * 0.5f), new Vector3(PT, 3.15f, -2.6f - zA), "OffWhite");
            Box("Lower_B", g, new Vector3(PARTX, 1.575f, (-1.6f + zB) * 0.5f), new Vector3(PT, 3.15f, zB + 1.6f), "OffWhite");
            Box("Lower_Header", g, new Vector3(PARTX, 2.625f, -2.1f), new Vector3(PT, 1.05f, 1.0f), "OffWhite");

            // upper level: glazed supervisor window over the shop floor
            Box("Upper_Sill", g, new Vector3(PARTX, 3.35f, (zA + zB) * 0.5f), new Vector3(PT, 0.4f, zB - zA), "OffWhite");
            Box("Upper_Head", g, new Vector3(PARTX, 5.6f, (zA + zB) * 0.5f), new Vector3(PT, 0.8f, zB - zA), "OffWhite");
            // door to the mezzanine, z 1.05 .. 2.0
            Box("Upper_DoorJamb", g, new Vector3(PARTX, 4.35f, 0.85f), new Vector3(PT, 1.8f, 0.4f), "OffWhite");
            Box("Upper_Glass", g, new Vector3(PARTX, 4.35f, (zA + 0.65f) * 0.5f), new Vector3(0.06f, 1.8f, 0.65f - zA), "Glass", default(Vector3), false);
            for (int i = 0; i < 6; i++)
                Box("Glass_Mullion_" + i, g, new Vector3(PARTX, 4.35f, zA + 0.9f + i * 1.28f), new Vector3(0.1f, 1.8f, 0.08f), "Alu", default(Vector3), false);
            Box("Glass_Frame_Bot", g, new Vector3(PARTX, 3.48f, (zA + 0.65f) * 0.5f), new Vector3(0.12f, 0.08f, 0.65f - zA), "Alu", default(Vector3), false);
            Box("Glass_Frame_Top", g, new Vector3(PARTX, 5.22f, (zA + 0.65f) * 0.5f), new Vector3(0.12f, 0.08f, 0.65f - zA), "Alu", default(Vector3), false);

            // office block rear wall (z = 2.0)
            var b = Group("Office_BackWall", parent);
            // built as panels so the mezzanine stair door stays open
            Box("Wall_L", b, new Vector3((OFF_L_X + 5.3f) * 0.5f, 3f, OFF_BACK_Z), new Vector3(5.3f - OFF_L_X, 6f, PT), "OffWhite");
            Box("Wall_R", b, new Vector3((6.4f + OFF_R_X) * 0.5f, 3f, OFF_BACK_Z), new Vector3(OFF_R_X - 6.4f, 6f, PT), "OffWhite");
            Box("Wall_Mid_Lo", b, new Vector3(5.85f, 1.575f, OFF_BACK_Z), new Vector3(1.1f, 3.15f, PT), "OffWhite");
            Box("Wall_Mid_Hi", b, new Vector3(5.85f, 5.62f, OFF_BACK_Z), new Vector3(1.1f, 0.75f, PT), "OffWhite");
        }

        static void BuildMezzanine(Transform parent)
        {
            var g = Group("Mezzanine", parent);
            float zA = Z0 - WT * 0.5f, zB = OFF_BACK_Z;
            float cz = (zA + zB) * 0.5f, dz = zB - zA;
            float cx = (OFF_L_X + OFF_R_X) * 0.5f, dx = OFF_R_X - OFF_L_X;

            Box("Slab", g, new Vector3(cx, MEZZ - 0.1f, cz), new Vector3(dx, 0.2f, dz), "Concrete");
            Box("Edge_Beam", g, new Vector3(OFF_L_X - 0.02f, 2.9f, cz), new Vector3(0.12f, 0.45f, dz), "SteelDark", default(Vector3), false);
            // support columns under the mezzanine
            for (int i = 0; i < 3; i++)
                Box("Column_" + i, g, new Vector3(OFF_L_X + 0.25f, 1.45f, zA + 1.6f + i * 3.0f), new Vector3(0.22f, 2.9f, 0.22f), "SteelDark");
            // ground floor ceiling of the office block
            Box("Ceiling_Lower", g, new Vector3(cx, 2.79f, cz), new Vector3(dx - 0.1f, 0.03f, dz - 0.1f), "White", default(Vector3), false);
            // upper office floor finish
            Box("Floor_Finish", g, new Vector3(cx, MEZZ + 0.01f, cz), new Vector3(dx - 0.1f, 0.02f, dz - 0.1f), "SteelDark", default(Vector3), false);
        }

        static void BuildStairs(Transform parent)
        {
            var g = Group("Stairs_ToMezzanine", parent);
            // straight steel flight in the rear-right bay, climbing towards -Z
            const float w = 1.1f, x = 5.85f;
            const float zBottom = 6.6f, tread = 0.27f, rise = 3.15f / 16f;
            for (int i = 0; i < 16; i++)
            {
                float y = (i + 1) * rise;
                float z = zBottom - i * tread - tread * 0.5f;
                Box("Step_" + i, g, new Vector3(x, y - 0.03f, z), new Vector3(w, 0.06f, tread), "Steel");
                Box("Riser_" + i, g, new Vector3(x, y - rise * 0.5f, z - tread * 0.5f), new Vector3(w, rise, 0.03f), "SteelDark", default(Vector3), false);
            }
            // stringers
            for (int s = 0; s < 2; s++)
            {
                float sx = x + (s == 0 ? -w * 0.5f - 0.04f : w * 0.5f + 0.04f);
                Box("Stringer_" + s, g, new Vector3(sx, 1.575f, zBottom - 2.03f), new Vector3(0.08f, 0.34f, 5.15f), "SteelDark", new Vector3(37.8f, 0f, 0f), false);
            }
            // handrail
            for (int s = 0; s < 2; s++)
            {
                float sx = x + (s == 0 ? -w * 0.5f - 0.04f : w * 0.5f + 0.04f);
                Box("Rail_" + s, g, new Vector3(sx, 2.575f, zBottom - 2.03f), new Vector3(0.05f, 0.05f, 5.15f), "Steel", new Vector3(37.8f, 0f, 0f), false);
                for (int p = 0; p < 5; p++)
                {
                    float z = zBottom - 0.35f - p * 0.95f;
                    float ys = (zBottom - z) * 0.7285f;
                    Box("Post_" + s + "_" + p, g, new Vector3(sx, ys + 0.5f, z), new Vector3(0.04f, 1.0f, 0.04f), "Steel", default(Vector3), false);
                }
            }
            // top landing
            Box("Landing", g, new Vector3(x, MEZZ - 0.05f, 1.85f), new Vector3(1.4f, 0.1f, 1.4f), "Steel");
            Box("Landing_Rail", g, new Vector3(x - 0.72f, MEZZ + 0.5f, 1.85f), new Vector3(0.05f, 1.0f, 1.4f), "Steel", default(Vector3), false);
            Marker("NAV_StairBottom", g, new Vector3(x, FLR, zBottom + 0.6f));
            Marker("NAV_StairTop", g, new Vector3(x, MEZZ, 1.85f));
        }

        static void BuildRearShutter(Transform parent)
        {
            var g = Group("Rear_RollerShutter", parent);
            float cx = 0.5f, w = 4f;
            for (int i = 0; i < 21; i++)
                Box("Slat_" + i, g, new Vector3(cx, 0.1f + i * 0.2f, Z1 - 0.2f), new Vector3(w, 0.19f, 0.06f), "Alu", default(Vector3), i == 0);
            Box("Box", g, new Vector3(cx, DOOR_H + 0.28f, Z1 - 0.25f), new Vector3(w + 0.3f, 0.5f, 0.4f), "SteelDark", default(Vector3), false);
            Box("Guide_L", g, new Vector3(cx - w * 0.5f - 0.06f, 2.1f, Z1 - 0.2f), new Vector3(0.12f, DOOR_H, 0.18f), "SteelDark", default(Vector3), false);
            Box("Guide_R", g, new Vector3(cx + w * 0.5f + 0.06f, 2.1f, Z1 - 0.2f), new Vector3(0.12f, DOOR_H, 0.18f), "SteelDark", default(Vector3), false);
            Box("Control", g, new Vector3(cx + w * 0.5f + 0.35f, 1.2f, Z1 - 0.32f), new Vector3(0.16f, 0.24f, 0.1f), "Yellow", default(Vector3), false);
            Marker("DOOR_RearShutter", g, new Vector3(cx, 0f, Z1));
        }

        static void BuildInteriorDoors(Transform parent)
        {
            var g = Group("Doors", parent);

            // reception <-> workshop
            var d1 = Group("Door_Reception", g);
            Box("Leaf", d1, new Vector3(PARTX, 1.05f, -2.1f), new Vector3(0.06f, 2.1f, 0.95f), "OffWhite");
            Box("Frame_T", d1, new Vector3(PARTX, 2.13f, -2.1f), new Vector3(0.16f, 0.08f, 1.1f), "SteelDark", default(Vector3), false);
            Cyl("Handle", d1, new Vector3(PARTX - 0.07f, 1.05f, -1.75f), 0.04f, 0.12f, "Alu", new Vector3(0f, 0f, 90f));
            Box("Kickplate", d1, new Vector3(PARTX - 0.04f, 0.2f, -2.1f), new Vector3(0.02f, 0.35f, 0.9f), "Alu", default(Vector3), false);

            // mezzanine office door
            var d2 = Group("Door_MezzOffice", g);
            Box("Leaf", d2, new Vector3(PARTX, MEZZ + 1.05f, 1.55f), new Vector3(0.06f, 2.1f, 0.9f), "OffWhite");
            Cyl("Handle", d2, new Vector3(PARTX - 0.07f, MEZZ + 1.05f, 1.2f), 0.04f, 0.12f, "Alu", new Vector3(0f, 0f, 90f));

            // side personnel door in the left wall
            var d3 = Group("Door_SideExit", g);
            Box("Leaf", d3, new Vector3(X0 + 0.05f, 1.05f, 4.1f), new Vector3(0.06f, 2.1f, 0.95f), "SteelDark");
            Cyl("Bar", d3, new Vector3(X0 + 0.16f, 1.05f, 4.1f), 0.05f, 0.8f, "Alu", new Vector3(90f, 0f, 0f));
            Sign("Exit", d3, new Vector3(X0 + 0.3f, 2.35f, 4.1f), new Vector2(0.7f, 0.22f), "EXIT", new Color(0.4f, 1f, 0.5f), 90f);
            Marker("DOOR_SideExit", d3, new Vector3(X0, 0f, 4.1f));
        }

        static void BuildRoofStructure(Transform parent)
        {
            var g = Group("Roof_Structure", parent);
            // primary beams across the workshop
            for (int i = 0; i < 5; i++)
            {
                float z = -5.5f + i * 2.75f;
                Box("Beam_" + i, g, new Vector3(-3.1f, 5.5f, z), new Vector3(13.6f, 0.42f, 0.2f), "SteelDark", default(Vector3), false);
                Box("Beam_Bot_" + i, g, new Vector3(-3.1f, 5.31f, z), new Vector3(13.6f, 0.05f, 0.34f), "SteelDark", default(Vector3), false);
                Box("Beam_Top_" + i, g, new Vector3(-3.1f, 5.69f, z), new Vector3(13.6f, 0.05f, 0.34f), "SteelDark", default(Vector3), false);
            }
            // purlins
            for (int i = 0; i < 6; i++)
                Box("Purlin_" + i, g, new Vector3(-9.4f + i * 2.6f, 5.78f, 0f), new Vector3(0.12f, 0.16f, BD - 0.5f), "SteelDark", default(Vector3), false);
            // corrugated deck
            Box("Deck", g, new Vector3(-3.1f, 5.9f, 0f), new Vector3(13.7f, 0.06f, BD - 0.4f), "Alu", default(Vector3), false);
            // translucent roof lights – workshops always have a few
            for (int i = 0; i < 3; i++)
                Box("Skylight_" + i, g, new Vector3(-8f + i * 4.5f, 5.93f, 1.5f), new Vector3(2.2f, 0.05f, 3.2f), "Glass", default(Vector3), false);
        }

        static void BuildInteriorLighting(Transform parent)
        {
            var g = Group("INT_Lighting", parent);

            float[] xs = { -7.8f, -3.9f, 0.0f, 3.0f };
            float[] zs = { -4.6f, 0.0f, 4.6f };
            int n = 0;
            for (int a = 0; a < xs.Length; a++)
                for (int b = 0; b < zs.Length; b++)
                {
                    bool withLight = (n % 2 == 0);
                    Fluorescent(g, "Shop_Light_" + n, new Vector3(xs[a], 5.15f, zs[b]), 2.2f, withLight, n == 4);
                    n++;
                }

            // rear-right service strip
            Fluorescent(g, "Rear_Light_0", new Vector3(6.5f, 5.15f, 4.6f), 2.0f, true, false);
            Fluorescent(g, "Rear_Light_1", new Vector3(8.5f, 5.15f, 6.2f), 1.6f, false, false);

            // reception (under mezzanine)
            Panel(g, "Recep_Light_0", new Vector3(5.6f, 2.75f, -5.2f), true);
            Panel(g, "Recep_Light_1", new Vector3(8.6f, 2.75f, -5.2f), false);
            Panel(g, "Recep_Light_2", new Vector3(5.6f, 2.75f, -2.2f), false);
            Panel(g, "Recep_Light_3", new Vector3(8.6f, 2.75f, -2.2f), true);

            // upper office
            Panel(g, "Mezz_Light_0", new Vector3(5.6f, 5.75f, -5.0f), true);
            Panel(g, "Mezz_Light_1", new Vector3(8.6f, 5.75f, -5.0f), true);
            Panel(g, "Mezz_Light_3", new Vector3(8.8f, 5.75f, 0.6f), true);
            Panel(g, "Mezz_Light_2", new Vector3(7.0f, 5.75f, -1.5f), true);

            // exit / emergency lighting
            Box("EmergencyLight", g, new Vector3(X0 + 0.4f, 2.6f, 4.1f), new Vector3(0.28f, 0.12f, 0.1f), "LampGreen", default(Vector3), false);
        }

        /// <summary>Twin-tube industrial batten.</summary>
        static void Fluorescent(Transform parent, string name, Vector3 pos, float length, bool realLight, bool shadows)
        {
            var g = Group(name, parent);
            g.localPosition = pos;
            Box("Housing", g, new Vector3(0f, 0.06f, 0f), new Vector3(length, 0.1f, 0.22f), "Alu", default(Vector3), false);
            Box("Tube_A", g, new Vector3(0f, -0.01f, -0.06f), new Vector3(length - 0.1f, 0.06f, 0.06f), "LampWhite", default(Vector3), false);
            Box("Tube_B", g, new Vector3(0f, -0.01f, 0.06f), new Vector3(length - 0.1f, 0.06f, 0.06f), "LampWhite", default(Vector3), false);
            Box("Chain_L", g, new Vector3(-length * 0.35f, 0.42f, 0f), new Vector3(0.02f, 0.62f, 0.02f), "SteelDark", default(Vector3), false);
            Box("Chain_R", g, new Vector3(length * 0.35f, 0.42f, 0f), new Vector3(0.02f, 0.62f, 0.02f), "SteelDark", default(Vector3), false);
            if (!realLight) return;
            var lg = new GameObject("Light");
            lg.transform.SetParent(g, false);
            lg.transform.localPosition = new Vector3(0f, -0.15f, 0f);
            var l = lg.AddComponent<Light>();
            l.type = LightType.Point;
            l.range = 12f;
            l.intensity = 2.6f;
            l.color = new Color(0.96f, 0.975f, 1f);
            l.shadows = shadows ? LightShadows.Soft : LightShadows.None;
        }

        /// <summary>Flush ceiling panel for the office areas.</summary>
        static void Panel(Transform parent, string name, Vector3 pos, bool realLight)
        {
            var g = Group(name, parent);
            g.localPosition = pos;
            Box("Frame", g, Vector3.zero, new Vector3(1.24f, 0.05f, 0.64f), "Alu", default(Vector3), false);
            Box("Diffuser", g, new Vector3(0f, -0.03f, 0f), new Vector3(1.16f, 0.02f, 0.56f), "LampWhite", default(Vector3), false);
            if (!realLight) return;
            var lg = new GameObject("Light");
            lg.transform.SetParent(g, false);
            lg.transform.localPosition = new Vector3(0f, -0.1f, 0f);
            var l = lg.AddComponent<Light>();
            l.type = LightType.Point;
            l.range = 8f;
            l.intensity = 2.0f;
            l.color = new Color(0.98f, 0.98f, 1f);
            l.shadows = LightShadows.None;
        }

        // ── painted floor graphics: bays, walkway, hazard zones ──
        static void BuildFloorGraphics(Transform parent)
        {
            var g = Group("Floor_Graphics", parent);
            float y = FLR + 0.006f;

            BayOutline(g, "Bay1", -4.35f, -0.85f, -6.6f, 1.4f);
            BayOutline(g, "Bay2", -0.35f, 3.15f, -6.6f, 1.4f);

            var b1 = Sign("Floor_Bay1", g, new Vector3(-2.6f, y, 2.0f), new Vector2(2.4f, 0.7f), "BAY 1", new Color(0.82f, 0.64f, 0.06f), 0f);
            b1.transform.localEulerAngles = new Vector3(90f, 180f, 0f);
            var b2 = Sign("Floor_Bay2", g, new Vector3(1.4f, y, 2.0f), new Vector2(2.4f, 0.7f), "BAY 2", new Color(0.82f, 0.64f, 0.06f), 0f);
            b2.transform.localEulerAngles = new Vector3(90f, 180f, 0f);

            // pedestrian walkway along the left wall – stops short of the inspection zone
            Box("Walk_A", g, new Vector3(X0 + 1.6f, y, -1.6f), new Vector3(0.1f, 0.012f, 9.6f), "Yellow", default(Vector3), false);
            Box("Walk_B", g, new Vector3(X0 + 2.5f, y, -1.6f), new Vector3(0.1f, 0.012f, 9.6f), "Yellow", default(Vector3), false);

            // hazard hatching in front of the electrical panel
            for (int i = 0; i < 7; i++)
                Box("Hazard_" + i, g, new Vector3(2.9f + i * 0.22f, y, 5.2f), new Vector3(0.1f, 0.012f, 1.2f), "Yellow", new Vector3(0f, 30f, 0f), false);

            // drainage channel with a steel grate
            Box("Drain_Channel", g, new Vector3(-3.2f, FLR - 0.03f, -5.2f), new Vector3(13.2f, 0.08f, 0.28f), "ConcreteDark", default(Vector3), false);
            for (int i = 0; i < 44; i++)
                Box("Grate_" + i, g, new Vector3(-9.7f + i * 0.3f, FLR + 0.001f, -5.2f), new Vector3(0.16f, 0.02f, 0.26f), "Steel", default(Vector3), false);

            // oil stains / worn patches
            for (int i = 0; i < 6; i++)
            {
                float ox = -8.5f + i * 2.4f;
                Decal("Stain_" + i, g, new Vector3(ox, y - 0.002f, -1.5f + (i % 3) * 2.3f), new Vector2(1.6f + i * 0.2f, 1.2f), "ConcreteDark", i * 27f);
            }
        }

        static void BayOutline(Transform parent, string name, float xl, float xr, float zl, float zr)
        {
            var g = Group("Outline_" + name, parent);
            float y = FLR + 0.006f;
            Box("L", g, new Vector3(xl, y, (zl + zr) * 0.5f), new Vector3(0.1f, 0.012f, zr - zl), "Yellow", default(Vector3), false);
            Box("R", g, new Vector3(xr, y, (zl + zr) * 0.5f), new Vector3(0.1f, 0.012f, zr - zl), "Yellow", default(Vector3), false);
            Box("B", g, new Vector3((xl + xr) * 0.5f, y, zr), new Vector3(xr - xl, 0.012f, 0.1f), "Yellow", default(Vector3), false);
        }

        // ── compressed air, power, cable trays, cctv ──
        static void BuildServices(Transform parent)
        {
            var g = Group("INT_Services", parent);

            // main air line along the left wall with drops at each bay
            Cyl("AirLine_Main", g, new Vector3(X0 + 0.35f, 3.4f, 0f), 0.06f, BD - 0.6f, "Steel", new Vector3(90f, 0f, 0f));
            Cyl("AirLine_Cross", g, new Vector3(-3.2f, 3.4f, -3.2f), 0.06f, 13.2f, "Steel", new Vector3(0f, 0f, 90f));
            float[] drops = { -2.6f, 1.4f };
            for (int i = 0; i < drops.Length; i++)
            {
                Cyl("AirDrop_" + i, g, new Vector3(drops[i], 2.5f, -3.2f), 0.05f, 1.8f, "Steel");
                Box("AirValve_" + i, g, new Vector3(drops[i], 1.65f, -3.2f), new Vector3(0.1f, 0.14f, 0.1f), "Copper", default(Vector3), false);
                // coiled hose
                Cyl("HoseReel_" + i, g, new Vector3(drops[i], 3.1f, -3.2f), 0.42f, 0.2f, "Yellow", new Vector3(0f, 0f, 90f));
            }

            // cable tray + conduit
            Box("CableTray", g, new Vector3(-3.2f, 4.9f, Z1 - 0.55f), new Vector3(13.2f, 0.1f, 0.3f), "Alu", default(Vector3), false);
            Cyl("Conduit_A", g, new Vector3(X0 + 0.5f, 4.2f, 0f), 0.05f, BD - 1f, "Alu", new Vector3(90f, 0f, 0f));

            // electrical panel, on the office partition out of the way of the work zones
            var ep = Group("ElectricalPanel", g);
            Box("Cabinet", ep, new Vector3(PARTX - 0.22f, 1.6f, 5.2f), new Vector3(0.28f, 1.6f, 1.2f), "SteelDark");
            Box("Door", ep, new Vector3(PARTX - 0.38f, 1.6f, 5.2f), new Vector3(0.04f, 1.5f, 1.1f), "Steel", default(Vector3), false);
            Box("Lamp", ep, new Vector3(PARTX - 0.4f, 2.28f, 4.8f), new Vector3(0.03f, 0.08f, 0.08f), "LampGreen", default(Vector3), false);
            Sign("Label", ep, new Vector3(PARTX - 0.39f, 2.5f, 5.2f), new Vector2(1.1f, 0.16f), "MAIN DISTRIBUTION - 400V", new Color(0.9f, 0.85f, 0.2f), -90f);

            // a small shop's worth of cameras – set dressing, not a security puzzle
            Cctv(g, "CCTV_Shop", new Vector3(X0 + 0.5f, 4.6f, Z1 - 0.6f), -35f, 28f);
            Cctv(g, "CCTV_Forecourt", new Vector3(X1 + 0.35f, 4.4f, Z0 + 0.6f), 250f, 22f);
        }

        static void Cctv(Transform parent, string name, Vector3 pos, float yaw, float pitch)
        {
            var g = Group(name, parent);
            g.localPosition = pos;
            g.localEulerAngles = new Vector3(0f, yaw, 0f);
            Box("Bracket", g, new Vector3(0f, 0f, 0.12f), new Vector3(0.05f, 0.05f, 0.24f), "SteelDark", default(Vector3), false);
            var body = Group("Body", g);
            body.localEulerAngles = new Vector3(pitch, 0f, 0f);
            Box("Case", body, new Vector3(0f, -0.04f, -0.16f), new Vector3(0.12f, 0.12f, 0.34f), "OffWhite", default(Vector3), false);
            Cyl("Lens", body, new Vector3(0f, -0.04f, -0.34f), 0.09f, 0.05f, "PanelBlack", new Vector3(90f, 0f, 0f));
            Box("LED", body, new Vector3(0.04f, 0.01f, -0.33f), new Vector3(0.02f, 0.02f, 0.02f), "LampRed", default(Vector3), false);
            Marker("VIEW_" + name, body, new Vector3(0f, -0.04f, -0.4f));
        }
    }
}
