using UnityEditor;
using UnityEngine;

namespace SecretsThatBreathe.LevelTools
{
    // Site: forecourt, parking, perimeter, rear scrap yard, street frontage, vehicles, gameplay markers.
    public static partial class Ch2GarageBuilder
    {
        const float FENCE_H = 2.4f;

        static void BuildExterior()
        {
            var g = Group("EXT_Site", _root);

            BuildForecourt(Group("Forecourt", g));
            BuildPerimeter(Group("Perimeter", g));
            BuildRearYard(Group("RearYard", g));
            BuildStreet(Group("Street", g));
            BuildSiteLighting(Group("SiteLighting", g));
        }

        // ───────────────────────── forecourt & parking ─────────────────────────
        static void BuildForecourt(Transform g)
        {
            float y = 0.008f;

            // customer parking – 6 bays, 2.6 x 5.0 m
            var park = Group("Parking_Customer", g);
            for (int i = 0; i < 7; i++)
                Box("Line_" + i, park, new Vector3(-10.4f + i * 2.6f, y, -16.5f), new Vector3(0.12f, 0.016f, 5.0f), "LineWhite", default(Vector3), false);
            Box("Line_Head", park, new Vector3(-2.6f, y, -19.0f), new Vector3(15.7f, 0.016f, 0.12f), "LineWhite", default(Vector3), false);
            for (int i = 0; i < 6; i++)
                Box("Stop_" + i, park, new Vector3(-9.1f + i * 2.6f, 0.06f, -18.7f), new Vector3(1.6f, 0.12f, 0.16f), "ConcreteDark", default(Vector3), false);

            // staff parking along the right boundary
            var staff = Group("Parking_Staff", g);
            for (int i = 0; i < 4; i++)
                Box("Line_" + i, staff, new Vector3(20.5f, y, -6f + i * 2.6f), new Vector3(5.0f, 0.016f, 0.12f), "LineWhite", default(Vector3), false);
            Sign("StaffSign", staff, new Vector3(17.6f, 1.6f, 1.6f), new Vector2(2.2f, 0.4f), "STAFF ONLY", Color.white, -90f);
            Box("StaffSign_Post", staff, new Vector3(17.6f, 0.8f, 1.6f), new Vector3(0.08f, 1.6f, 0.08f), "SteelDark", default(Vector3), false);

            // drive lane arrows + hatched no-parking strip in front of the doors
            for (int i = 0; i < 3; i++)
            {
                Box("Arrow_" + i, g, new Vector3(0f, y, -22f + i * 3.5f), new Vector3(0.16f, 0.016f, 1.6f), "LineWhite", default(Vector3), false);
                Box("ArrowHead_" + i, g, new Vector3(0f, y, -21.1f + i * 3.5f), new Vector3(0.5f, 0.016f, 0.5f), "LineWhite", new Vector3(0f, 45f, 0f), false);
            }
            for (int i = 0; i < 12; i++)
                Box("Hatch_" + i, g, new Vector3(-6.5f + i * 1.0f, y, -9.5f), new Vector3(0.12f, 0.016f, 2.6f), "Yellow", new Vector3(0f, 32f, 0f), false);
            Box("KeepClear", g, new Vector3(-0.6f, y, -8.2f), new Vector3(12.6f, 0.016f, 0.14f), "Yellow", default(Vector3), false);

            // wheel-stop bollards protecting the office glazing
            for (int i = 0; i < 4; i++)
                Bollard(g, "Bollard_" + i, new Vector3(4.6f + i * 1.7f, 0f, -8.4f));

            // air & water station
            var aw = Group("AirWaterStation", g);
            Box("Column", aw, new Vector3(12.6f, 0.75f, -12f), new Vector3(0.5f, 1.5f, 0.4f), "ToolRed");
            Box("Head", aw, new Vector3(12.6f, 1.62f, -12f), new Vector3(0.6f, 0.28f, 0.5f), "SteelDark", default(Vector3), false);
            Cyl("HoseReel", aw, new Vector3(12.6f, 1.2f, -12.25f), 0.4f, 0.16f, "PanelBlack", new Vector3(90f, 0f, 0f));
            Sign("Label", aw, new Vector3(12.6f, 1.75f, -12.27f), new Vector2(0.55f, 0.16f), "AIR / WATER", Color.white);

            // dirty details
            for (int i = 0; i < 7; i++)
                Decal("Tyre_Mark_" + i, g, new Vector3(-6f + i * 2.1f, 0.006f, -9.5f - (i % 3) * 1.4f), new Vector2(0.6f, 4.5f), "ConcreteDark", (i % 4) * 6f);
            Place(P_CONE, g, new Vector3(-6.4f, 0f, -9.0f), 0f, 0f, -90f);
            Place(P_CONE, g, new Vector3(-5.2f, 0f, -9.4f), 0f, 0f, -90f);
            Place(P_BARRIER, g, new Vector3(-8.5f, 0f, -9.2f), 12f, 0f, -90f);

            Marker("NAV_Forecourt", g, new Vector3(0f, 0f, -12f));
        }

        static void Bollard(Transform parent, string name, Vector3 p)
        {
            var g = Group(name, parent);
            g.localPosition = p;
            Cyl("Post", g, new Vector3(0f, 0.5f, 0f), 0.16f, 1.0f, "Yellow", default(Vector3), true);
            Cyl("Band", g, new Vector3(0f, 0.82f, 0f), 0.17f, 0.14f, "PanelBlack");
            Cyl("Cap", g, new Vector3(0f, 1.0f, 0f), 0.16f, 0.04f, "PanelBlack");
        }

        // ───────────────────────── perimeter wall, gate, guard booth ─────────────────────────
        // A neighbourhood shop, not a compound: solid walls on the sides and back,
        // a low wall and a wide open frontage so the street can see straight in.
        static void BuildPerimeter(Transform g)
        {
            WallRun(g, "Wall_West", new Vector3(-LOT_HALF_X, 0f, (LOT_FRONT_Z + LOT_BACK_Z) * 0.5f), LOT_BACK_Z - LOT_FRONT_Z, 90f, FENCE_H);
            WallRun(g, "Wall_East", new Vector3(LOT_HALF_X, 0f, (LOT_FRONT_Z + LOT_BACK_Z) * 0.5f), LOT_BACK_Z - LOT_FRONT_Z, 90f, FENCE_H);
            WallRun(g, "Wall_North", new Vector3(0f, 0f, LOT_BACK_Z), LOT_HALF_X * 2f, 0f, FENCE_H);
            WallRun(g, "Front_LowWall_W", new Vector3((-LOT_HALF_X - 9f) * 0.5f, 0f, LOT_FRONT_Z), LOT_HALF_X - 9f, 0f, 1.1f);
            WallRun(g, "Front_LowWall_E", new Vector3((LOT_HALF_X + 9f) * 0.5f, 0f, LOT_FRONT_Z), LOT_HALF_X - 9f, 0f, 1.1f);

            // entrance piers either side of the 18 m opening
            for (int i = 0; i < 2; i++)
            {
                float x = i == 0 ? -9f : 9f;
                var p = Group("EntrancePier_" + i, g);
                p.localPosition = new Vector3(x, 0f, LOT_FRONT_Z);
                Box("Pier", p, new Vector3(0f, 1.4f, 0f), new Vector3(0.6f, 2.8f, 0.6f), "Concrete");
                Box("Cap", p, new Vector3(0f, 2.86f, 0f), new Vector3(0.74f, 0.12f, 0.74f), "BrandRed", default(Vector3), false);
                Box("Band", p, new Vector3(0f, 2.2f, 0f), new Vector3(0.66f, 0.3f, 0.66f), "BrandRed", default(Vector3), false);
                Sign("Pier_Text", p, new Vector3(0f, 2.2f, -0.35f), new Vector2(0.55f, 0.22f), i == 0 ? "RACE" : "TOOL", Color.white);
                var wl = Group("WallPack_Pier_" + i, p);
                Box("Lamp", wl, new Vector3(0f, 2.62f, -0.2f), new Vector3(0.2f, 0.12f, 0.2f), "LampWhite", default(Vector3), false);
                var lg0 = new GameObject("Light");
                lg0.transform.SetParent(wl, false);
                lg0.transform.localPosition = new Vector3(0f, 2.5f, -0.2f);
                lg0.transform.localEulerAngles = new Vector3(75f, 0f, 0f);
                var l0 = lg0.AddComponent<Light>();
                l0.type = LightType.Spot; l0.spotAngle = 100f; l0.range = 9f; l0.intensity = 2.4f;
                l0.color = new Color(1f, 0.93f, 0.8f); l0.shadows = LightShadows.None;
            }

            // opening hours / service board by the entrance
            var brd = Group("ShopBoard", g);
            brd.localPosition = new Vector3(-11.5f, 0f, LOT_FRONT_Z + 0.4f);
            Box("Post_L", brd, new Vector3(-0.8f, 0.9f, 0f), new Vector3(0.1f, 1.8f, 0.1f), "SteelDark", default(Vector3), false);
            Box("Post_R", brd, new Vector3(0.8f, 0.9f, 0f), new Vector3(0.1f, 1.8f, 0.1f), "SteelDark", default(Vector3), false);
            Box("Face", brd, new Vector3(0f, 1.55f, 0f), new Vector3(1.9f, 1.1f, 0.06f), "PanelBlack");
            Box("Face_Trim", brd, new Vector3(0f, 1.55f, -0.04f), new Vector3(2.0f, 1.2f, 0.03f), "BrandRed", default(Vector3), false);
            Sign("Head", brd, new Vector3(0f, 1.85f, -0.07f), new Vector2(1.7f, 0.24f), "OPEN  08:00 - 18:00", Color.white);
            Sign("Body", brd, new Vector3(0f, 1.42f, -0.07f), new Vector2(1.7f, 0.5f),
                "SERVICE · TYRES · TUNING\nWALK-IN WELCOME", new Color(0.88f, 0.88f, 0.86f), 180f, false);

            // folding gate, parked open against the west pier (shop is still open)
            var gate = Group("Gate_FoldedOpen", g);
            gate.localPosition = new Vector3(-8.2f, 0f, LOT_FRONT_Z);
            for (int i = 0; i < 6; i++)
                Box("Leaf_" + i, gate, new Vector3(-i * 0.14f, 1.05f, 0f), new Vector3(0.06f, 1.9f, 0.9f), "SteelDark", default(Vector3), i == 0);
            Box("Rail", g, new Vector3(0f, 0.03f, LOT_FRONT_Z), new Vector3(17.5f, 0.06f, 0.12f), "Steel", default(Vector3), false);
            Marker("ENTRY_Driveway", g, new Vector3(0f, 0f, LOT_FRONT_Z));

            BuildOutdoorSeating(g);
        }

        /// <summary>Shaded chairs outside the office – where customers and friends actually sit.</summary>
        static void BuildOutdoorSeating(Transform parent)
        {
            var g = Group("OutdoorSeating", parent);
            g.localPosition = new Vector3(14f, 0f, -8.5f);

            Box("Slab", g, new Vector3(0f, 0.04f, 0f), new Vector3(5.0f, 0.08f, 4.0f), "Concrete");
            for (int i = 0; i < 4; i++)
                Box("Post_" + i, g, new Vector3(i < 2 ? -2.3f : 2.3f, 1.3f, i % 2 == 0 ? -1.8f : 1.8f), new Vector3(0.1f, 2.6f, 0.1f), "SteelDark");
            Box("Awning", g, new Vector3(0f, 2.68f, 0f), new Vector3(5.2f, 0.1f, 4.2f), "BrandRed", new Vector3(4f, 0f, 0f), false);
            for (int i = 0; i < 9; i++)
                Box("Rib_" + i, g, new Vector3(-2.2f + i * 0.55f, 2.6f, 0f), new Vector3(0.06f, 0.06f, 4.2f), "Alu", new Vector3(4f, 0f, 0f), false);

            Cyl("Table", g, new Vector3(0f, 0.72f, 0f), 0.9f, 0.05f, "White");
            Cyl("Table_Leg", g, new Vector3(0f, 0.38f, 0f), 0.1f, 0.68f, "White");
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI * 0.5f + 0.4f;
                var c = Group("Chair_" + i, g);
                c.localPosition = new Vector3(Mathf.Cos(a) * 1.05f, 0.08f, Mathf.Sin(a) * 1.05f);
                c.localEulerAngles = new Vector3(0f, -a * Mathf.Rad2Deg + 90f, 0f);
                Box("Seat", c, new Vector3(0f, 0.44f, 0f), new Vector3(0.44f, 0.05f, 0.44f), "OffWhite", default(Vector3), false);
                Box("Back", c, new Vector3(0f, 0.66f, 0.21f), new Vector3(0.44f, 0.45f, 0.04f), "OffWhite", default(Vector3), false);
                for (int k = 0; k < 4; k++)
                    Box("Leg_" + k, c, new Vector3(k < 2 ? -0.18f : 0.18f, 0.22f, k % 2 == 0 ? -0.18f : 0.18f), new Vector3(0.04f, 0.44f, 0.04f), "OffWhite", default(Vector3), false);
            }
            Box("Cooler", g, new Vector3(1.9f, 0.36f, -1.4f), new Vector3(0.7f, 0.56f, 0.5f), "SafetyGreen", default(Vector3), false);
            Box("Bin", g, new Vector3(-2.0f, 0.4f, 1.5f), new Vector3(0.5f, 0.72f, 0.5f), "SteelDark", default(Vector3), false);
            Marker("NAV_OutdoorSeating", g, Vector3.zero);
            Marker("SIT_Outdoor", g, new Vector3(1.0f, 0.5f, 0.4f));
        }

        static void WallRun(Transform parent, string name, Vector3 centre, float length, float yaw, float height)
        {
            var g = Group(name, parent);
            g.localPosition = centre;
            g.localEulerAngles = new Vector3(0f, yaw, 0f);
            Box("Wall", g, new Vector3(0f, height * 0.5f, 0f), new Vector3(length, height, 0.2f), "Concrete");
            Box("Coping", g, new Vector3(0f, height + 0.05f, 0f), new Vector3(length, 0.1f, 0.3f), "ConcreteDark", default(Vector3), false);
            Box("Base", g, new Vector3(0f, 0.15f, 0f), new Vector3(length, 0.3f, 0.3f), "ConcreteDark", default(Vector3), false);
            int posts = Mathf.Max(2, Mathf.RoundToInt(length / 4f));
            for (int i = 0; i <= posts; i++)
            {
                float x = -length * 0.5f + i * (length / posts);
                Box("Pilaster_" + i, g, new Vector3(x, height * 0.5f + 0.1f, 0f), new Vector3(0.34f, height + 0.2f, 0.34f), "Concrete", default(Vector3), false);
            }
        }

        // ───────────────────────── rear scrap yard ─────────────────────────
        static void BuildRearYard(Transform g)
        {
            // shipping container used as parts storage
            var c = Group("Container", g);
            c.localPosition = new Vector3(-17f, 0f, 15.4f);
            c.localEulerAngles = new Vector3(0f, 7f, 0f);
            Box("Body", c, new Vector3(0f, 1.3f, 0f), new Vector3(6.06f, 2.59f, 2.44f), "SafetyGreen");
            for (int i = 0; i < 18; i++)
                Box("Rib_" + i, c, new Vector3(-2.9f + i * 0.34f, 1.3f, 1.24f), new Vector3(0.1f, 2.4f, 0.05f), "SafetyGreen", default(Vector3), false);
            Box("Door_L", c, new Vector3(-3.05f, 1.3f, -0.6f), new Vector3(0.06f, 2.4f, 1.2f), "Rust", default(Vector3), false);
            Box("Door_R", c, new Vector3(-3.05f, 1.3f, 0.6f), new Vector3(0.06f, 2.4f, 1.2f), "Rust", default(Vector3), false);
            Box("Corner_A", c, new Vector3(3.0f, 0.15f, 1.2f), new Vector3(0.2f, 0.3f, 0.2f), "SteelDark", default(Vector3), false);

            // dumpster
            var d = Group("Dumpster", g);
            d.localPosition = new Vector3(9.5f, 0f, 15.2f);
            d.localEulerAngles = new Vector3(0f, -12f, 0f);
            Box("Body", d, new Vector3(0f, 0.7f, 0f), new Vector3(2.4f, 1.3f, 1.5f), "Rust");
            Box("Lip", d, new Vector3(0f, 1.38f, 0f), new Vector3(2.5f, 0.1f, 1.6f), "SteelDark", default(Vector3), false);
            Box("Junk_A", d, new Vector3(-0.4f, 1.5f, 0.2f), new Vector3(1.0f, 0.4f, 0.7f), "Cardboard", new Vector3(9f, 20f, 0f), false);
            Box("Junk_B", d, new Vector3(0.6f, 1.45f, -0.2f), new Vector3(0.8f, 0.3f, 0.6f), "SteelDark", new Vector3(-6f, -14f, 0f), false);
            for (int i = 0; i < 4; i++)
                Cyl("Wheel_" + i, d, new Vector3(i < 2 ? -1.0f : 1.0f, 0.1f, i % 2 == 0 ? -0.6f : 0.6f), 0.2f, 0.08f, "PanelBlack", new Vector3(0f, 0f, 90f));

            // tyre mountain + scrap
            for (int i = 0; i < 5; i++)
                TyreStack(g, "YardTyres_" + i, new Vector3(2.0f + (i % 3) * 0.85f, 0f, 11.6f + (i / 3) * 0.9f), 5 + (i % 4));
            for (int i = 0; i < 8; i++)
                Box("Scrap_" + i, g, new Vector3(-6f + (i % 4) * 1.3f, 0.18f + (i % 2) * 0.2f, 18f - (i / 4) * 1.4f),
                    new Vector3(1.3f, 0.35f, 0.7f), i % 2 == 0 ? "Rust" : "SteelDark", new Vector3((i % 3) * 7f, i * 23f, (i % 2) * 5f), false);
            for (int i = 0; i < 3; i++)
                Pallet(g, "YardPallet_" + i, new Vector3(13f + i * 1.5f, i * 0.16f, 11.5f), i * 14f);
            Drum(g, "YardDrum_A", new Vector3(-11.5f, 0f, 11.2f), "Rust");
            Drum(g, "YardDrum_B", new Vector3(-10.7f, 0f, 11.6f), "SteelDark");
            Drum(g, "YardDrum_C", new Vector3(-11.1f, 0f, 12.3f), "Yellow");

            // engine blocks / axles on the ground
            for (int i = 0; i < 4; i++)
                Box("EngineBlock_" + i, g, new Vector3(-2f + i * 1.6f, 0.28f, 9.4f), new Vector3(0.75f, 0.56f, 0.85f), "SteelDark", new Vector3(0f, i * 31f, 0f), false);

            // rear yard lighting + cctv
            Cctv(g, "CCTV_Rear", new Vector3(0f, 4.6f, Z1 + 0.4f), 0f, 25f);
            Marker("NAV_RearYard", g, new Vector3(0f, 0f, 12f));

            // weeds / dirt patches
            for (int i = 0; i < 6; i++)
                Decal("Dirt_" + i, g, new Vector3(-20f + i * 7f, 0.006f, 17.5f), new Vector2(5f, 3.4f), "Dirt", i * 17f);
        }

        // ───────────────────────── street frontage ─────────────────────────
        static void BuildStreet(Transform g)
        {
            Box("Road", g, new Vector3(0f, -0.06f, -30.2f), new Vector3(80f, 0.12f, 7.6f), "AsphaltRoad");
            Box("Sidewalk", g, new Vector3(0f, 0.075f, -25.2f), new Vector3(80f, 0.15f, 2.4f), "Curb");
            Box("Curb", g, new Vector3(0f, 0.09f, -26.35f), new Vector3(80f, 0.18f, 0.3f), "Concrete", default(Vector3), false);
            Box("Driveway", g, new Vector3(0f, 0.07f, -25.2f), new Vector3(18.5f, 0.16f, 2.5f), "Asphalt", default(Vector3), false);
            for (int i = 0; i < 20; i++)
                Box("CentreLine_" + i, g, new Vector3(-38f + i * 4f, 0.005f, -30.2f), new Vector3(2.2f, 0.02f, 0.16f), "LineWhite", default(Vector3), false);
            Box("EdgeLine_A", g, new Vector3(0f, 0.005f, -26.9f), new Vector3(80f, 0.02f, 0.12f), "LineWhite", default(Vector3), false);
            Box("EdgeLine_B", g, new Vector3(0f, 0.005f, -33.5f), new Vector3(80f, 0.02f, 0.12f), "LineWhite", default(Vector3), false);

            // pylon sign on the street corner
            var s = Group("PylonSign", g);
            s.localPosition = new Vector3(-15.5f, 0f, -22.4f);
            Box("Base", s, new Vector3(0f, 0.2f, 0f), new Vector3(2.0f, 0.4f, 0.9f), "ConcreteDark");
            Box("Post_L", s, new Vector3(-0.7f, 2.4f, 0f), new Vector3(0.22f, 4.4f, 0.22f), "SteelDark");
            Box("Post_R", s, new Vector3(0.7f, 2.4f, 0f), new Vector3(0.22f, 4.4f, 0.22f), "SteelDark");
            Box("Panel", s, new Vector3(0f, 5.3f, 0f), new Vector3(3.6f, 2.1f, 0.3f), "PanelBlack");
            Box("Panel_Border", s, new Vector3(0f, 5.3f, -0.17f), new Vector3(3.7f, 2.2f, 0.06f), "BrandRed", default(Vector3), false);
            Box("Panel_Face", s, new Vector3(0f, 5.3f, -0.21f), new Vector3(3.4f, 1.9f, 0.03f), "PanelBlack", default(Vector3), false);
            Sign("Text_Main", s, new Vector3(0f, 5.55f, -0.24f), new Vector2(3.1f, 0.9f), "RACE TOOL", Color.white);
            Sign("Text_Sub", s, new Vector3(0f, 4.72f, -0.24f), new Vector2(3.1f, 0.28f), "SERVICE · TUNING · TYRES", new Color(0.95f, 0.25f, 0.2f), 180f, false);
            Cyl("Logo", s, new Vector3(0f, 6.0f, -0.24f), 0.7f, 0.04f, "BrandRed", new Vector3(90f, 0f, 0f));
            Sign("Logo_Text", s, new Vector3(0f, 6.0f, -0.28f), new Vector2(0.5f, 0.3f), "RT", Color.white);

            // hedge + trees to break the boundary line
            for (int i = 0; i < 6; i++)
            {
                Box("Hedge_W_" + i, g, new Vector3(-24f + i * 2.2f, 0.45f, -23.0f), new Vector3(2.0f, 0.9f, 0.7f), "Grass", default(Vector3), false);
                Box("Hedge_E_" + i, g, new Vector3(12f + i * 2.2f, 0.45f, -23.0f), new Vector3(2.0f, 0.9f, 0.7f), "Grass", default(Vector3), false);
            }
            Tree(g, "Tree_SW", new Vector3(-22f, 0f, -21f), 6.6f);
            Tree(g, "Tree_SE", new Vector3(21.5f, 0f, -21f), 5.8f);
            Tree(g, "Tree_NE", new Vector3(23.5f, 0f, 6f), 7.2f);

            // opposite side of the road – a plain block so the street is not empty
            Box("Opposite_Block", g, new Vector3(-12f, 3f, -42f), new Vector3(26f, 6f, 12f), "ConcreteDark");
            Box("Opposite_Block_2", g, new Vector3(18f, 4f, -44f), new Vector3(20f, 8f, 14f), "PanelDark");
            Box("Opposite_Kerb", g, new Vector3(0f, 0.08f, -34.8f), new Vector3(80f, 0.16f, 2.4f), "Curb", default(Vector3), false);
        }

        /// <summary>Simple boundary tree – the imported tree packs are still on built-in shaders.</summary>
        static void Tree(Transform parent, string name, Vector3 p, float height)
        {
            var g = Group(name, parent);
            g.localPosition = p;
            float trunk = height * 0.42f;
            Cyl("Trunk", g, new Vector3(0f, trunk * 0.5f, 0f), height * 0.075f, trunk, "Wood", default(Vector3), true);
            Sphere("Canopy_A", g, new Vector3(0f, trunk + height * 0.24f, 0f), height * 0.62f, "Grass");
            Sphere("Canopy_B", g, new Vector3(height * 0.14f, trunk + height * 0.42f, height * 0.08f), height * 0.42f, "Grass");
            Sphere("Canopy_C", g, new Vector3(-height * 0.15f, trunk + height * 0.36f, -height * 0.1f), height * 0.38f, "Grass");
        }

        static void BuildSiteLighting(Transform g)
        {
            LightPole(g, "Pole_SW", new Vector3(-19f, 0f, -17f), 90f);
            LightPole(g, "Pole_SE", new Vector3(15.5f, 0f, -17f), -90f);
            LightPole(g, "Pole_NW", new Vector3(-19f, 0f, 11f), 90f);
            LightPole(g, "Pole_NE", new Vector3(19f, 0f, 11f), -90f);
            LightPole(g, "Pole_Street", new Vector3(-9f, 0f, -25.6f), 0f);

            // building mounted flood lights
            WallPack(g, "WallPack_Side_L", new Vector3(X0 - 0.2f, 4.6f, 2f), -90f);
            WallPack(g, "WallPack_Rear", new Vector3(-2f, 4.6f, Z1 + 0.2f), 180f);
            WallPack(g, "WallPack_Side_R", new Vector3(X1 + 0.2f, 4.6f, 2f), 90f);
        }

        static void LightPole(Transform parent, string name, Vector3 p, float yaw)
        {
            var g = Group(name, parent);
            g.localPosition = p;
            g.localEulerAngles = new Vector3(0f, yaw, 0f);
            Box("Base", g, new Vector3(0f, 0.2f, 0f), new Vector3(0.6f, 0.4f, 0.6f), "ConcreteDark");
            Cyl("Pole", g, new Vector3(0f, 3.4f, 0f), 0.22f, 6.4f, "Alu", default(Vector3), true);
            Box("Arm", g, new Vector3(0f, 6.5f, 0.8f), new Vector3(0.12f, 0.12f, 1.6f), "Alu", default(Vector3), false);
            Box("Head", g, new Vector3(0f, 6.38f, 1.62f), new Vector3(0.5f, 0.18f, 0.9f), "SteelDark", default(Vector3), false);
            Box("Lens", g, new Vector3(0f, 6.28f, 1.62f), new Vector3(0.42f, 0.03f, 0.8f), "LampWhite", default(Vector3), false);
            var lg = new GameObject("Light");
            lg.transform.SetParent(g, false);
            lg.transform.localPosition = new Vector3(0f, 6.2f, 1.62f);
            lg.transform.localEulerAngles = new Vector3(90f, 0f, 0f);
            var l = lg.AddComponent<Light>();
            l.type = LightType.Spot;
            l.spotAngle = 110f;
            l.range = 22f;
            l.intensity = 3.0f;
            l.color = new Color(1f, 0.90f, 0.72f);
            l.shadows = LightShadows.None;
        }

        // ───────────────────────── vehicles ─────────────────────────
        static void BuildVehicles()
        {
            var g = Group("VEHICLES", _root);

            // the friend's current job, up on the lift in bay 1
            var lifted = Place(P_CAR_A, g, new Vector3(-2.6f, 1.50f, -1.6f), 180f);
            if (lifted != null) lifted.name = "Car_FriendJob_OnLift";

            // KEM'S CAR – he drives in and parks straight in front of the open bay
            var kem = Place(P_CAR_B, g, new Vector3(2.6f, 0f, -11.6f), 180f);
            if (kem != null) kem.name = "Car_KEM_Arrival";

            // customers left overnight
            Place(P_CAR_A, g, new Vector3(-9.1f, 0f, -16.3f), 8f);
            Place(P_CAR_B, g, new Vector3(-3.9f, 0f, -16.5f), -4f);
            Place(P_CAR_A, g, new Vector3(20.5f, 0f, -3.2f), 90f);

            // long-term jobs / parts donors round the back
            var w1 = Place(P_CAR_B, g, new Vector3(-8.5f, 0f, 13.2f), 34f);
            if (w1 != null) w1.name = "Car_PartsDonor_01";
            var w2 = Place(P_CAR_A, g, new Vector3(-3.4f, 0f, 16.8f), -22f);
            if (w2 != null) w2.name = "Car_PartsDonor_02";

            Place(P_CAR_B, g, new Vector3(-15f, 0f, -25.4f), 90f);
        }

        // ───────────────────────── gameplay scaffolding ─────────────────────────
        // Chapter 2 beat sheet, wired as named empties the sequence scripts can grab.
        static void BuildGameplay()
        {
            var g = Group("GAMEPLAY", _root);

            // Kem arrives on foot from his car parked on the forecourt
            Marker("PlayerSpawn_Arrival", g, new Vector3(1.6f, 0.2f, -10.4f));
            var player = Place(P_PLAYER, g, new Vector3(1.6f, 1.2f, -10.4f), 0f);
            if (player != null) player.name = "player";

            Marker("PlayerSpawn_Street", g, new Vector3(0f, 1.2f, -25.4f));
            Marker("PlayerSpawn_Workshop", g, new Vector3(-2.6f, FLR, -5.5f));
            Marker("PlayerExit_ToStreet", g, new Vector3(0f, 0f, LOT_FRONT_Z));

            // where the friend is at each stage of the scene
            var npc = Group("NPC_Friend", g);
            var friend = Place("Assets/Champ&Kichzz/Prefab/Npc/NPC.prefab", npc, new Vector3(-2.4f, FLR, -4.4f), 180f);
            if (friend != null) friend.name = "NPC_Friend_PLACEHOLDER";
            Marker("NPC_Friend_UnderCar", npc, new Vector3(-3.4f, FLR, -3.4f));      // first found here, wrenching
            Marker("NPC_Friend_Greeting", npc, new Vector3(-2.4f, FLR, -4.4f));
            Marker("NPC_Friend_AtBench", npc, new Vector3(-6.7f, FLR, 4.6f));        // examines the fragment
            Marker("NPC_Friend_AtChart", npc, new Vector3(-6.2f, FLR, 5.9f));
            Marker("NPC_Friend_AtPC", npc, new Vector3(8.7f, MEZZ, -1.2f));          // digs out the order records
            Marker("NPC_Friend_Sofa", npc, new Vector3(-9.0f, FLR + 0.45f, -2.7f));
            Marker("NPC_Friend_Outdoor", npc, new Vector3(15f, 0.5f, -8.1f));

            // camera anchors for the dialogue beats
            var cam = Group("CutsceneCameras", g);
            CamAnchor(cam, "CUT_01_Arrival", new Vector3(6.2f, 1.75f, -13.5f), new Vector3(4f, -122f, 0f));
            CamAnchor(cam, "CUT_02_Greeting", new Vector3(-0.6f, 1.6f, -5.6f), new Vector3(6f, -132f, 0f));
            CamAnchor(cam, "CUT_03_WalkToBench", new Vector3(-4.2f, 1.7f, 1.2f), new Vector3(4f, -34f, 0f));
            CamAnchor(cam, "CUT_04_TwoShot_Bench", new Vector3(-7.5f, 1.6f, 2.6f), new Vector3(6f, 0f, 0f));
            CamAnchor(cam, "CUT_05_CloseUp_Fragment", new Vector3(-7.9f, 1.28f, 4.55f), new Vector3(26f, -14f, 0f));
            CamAnchor(cam, "CUT_06_PaintChart", new Vector3(-6.2f, 1.7f, 4.4f), new Vector3(2f, 0f, 0f));
            CamAnchor(cam, "CUT_07_Board", new Vector3(-8.5f, 1.8f, 4.6f), new Vector3(0f, 0f, 0f));
            CamAnchor(cam, "CUT_08_Office", new Vector3(6.6f, MEZZ + 1.6f, -2.4f), new Vector3(6f, 58f, 0f));

            // interaction beats, in play order
            var obj = Group("Objectives", g);
            Marker("OBJ_01_ArriveAtGarage", obj, new Vector3(1.6f, 0f, -10.4f));
            Marker("OBJ_02_TalkToFriend", obj, new Vector3(-2.6f, FLR, -4.6f));
            Marker("OBJ_03_PlaceEvidenceOnBench", obj, INSPECT_TABLE + new Vector3(-0.35f, 0.95f, 0f));
            Marker("OBJ_04_ExamineUnderMagnifier", obj, INSPECT_TABLE + new Vector3(-1.0f, 1.4f, 0.2f));
            Marker("OBJ_05_MatchPaintCode", obj, new Vector3(-6.2f, 1.4f, 6.0f));
            Marker("OBJ_06_SearchPartsDatabase", obj, new Vector3(-9.0f, 1.2f, 4.2f));
            Marker("OBJ_07_CheckOrderRecords", obj, new Vector3(8.9f, MEZZ + 1.0f, -1.2f));
            Marker("OBJ_08_PinResultOnBoard", obj, new Vector3(-8.5f, 1.85f, 6.2f));
            Marker("OBJ_09_LeaveGarage", obj, new Vector3(0f, 0f, LOT_FRONT_Z));
        }

        static void CamAnchor(Transform parent, string name, Vector3 pos, Vector3 euler)
        {
            var g = new GameObject(name);
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.transform.localEulerAngles = euler;
        }
    }
}
