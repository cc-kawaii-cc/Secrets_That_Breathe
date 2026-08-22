using UnityEditor;
using UnityEngine;

namespace SecretsThatBreathe.LevelTools
{
    // Workshop equipment and set dressing.
    public static partial class Ch2GarageBuilder
    {
        // Workshop zoning (x -9.88 .. 3.72, z -6.88 .. 6.88)
        //   front centre  : BAY 1 (friend's job) + BAY 2 (kept clear, Kem's car rolls in)
        //   left  z -6.8..-4.8 : services corner (compressor, drums, parts washer)
        //   left  z -4.4..-0.8 : HANGOUT – sofa, fridge, fan, posters
        //   left  z -0.4.. 3.4 : workbench run + pegboard
        //   left  z  3.6.. 6.8 : INSPECTION ZONE – the chapter plays here
        //   back  x -6.1..-1.2 : parts racking      x -1.5..2.5 : rear shutter
        static void BuildWorkshopProps()
        {
            var g = Group("INT_Workshop", _dress);

            // ── service bays ─────────────────────────────────────────────
            TwoPostLift(g, "Lift_Bay1", new Vector3(-2.6f, FLR, -1.6f), true);
            TwoPostLift(g, "Lift_Bay2", new Vector3(1.4f, FLR, -1.6f), false);

            ToolChest(g, "ToolChest_Bay1", new Vector3(-4.6f, FLR, -2.6f), 90f);
            ToolChest(g, "ToolChest_Bay2", new Vector3(3.35f, FLR, -4.8f), -90f);   // clear of the reception doorway
            ToolChest(g, "ToolChest_Wall", new Vector3(X0 + 1.8f, FLR, -6.3f), 0f);

            FloorJack(g, "FloorJack_1", new Vector3(-0.2f, FLR, -4.6f), 15f);
            FloorJack(g, "FloorJack_2", new Vector3(2.6f, FLR, -4.2f), -60f);
            for (int i = 0; i < 4; i++)
                JackStand(g, "JackStand_" + i, new Vector3(0.2f + (i % 2) * 0.5f, FLR, -5.0f - (i / 2) * 0.55f));
            Creeper(g, "Creeper", new Vector3(-4.9f, FLR, -1.2f), 24f);   // friend slides out from under the car
            Creeper(g, "Creeper_2", new Vector3(-5.4f, FLR, 1.6f), -8f);

            OilPan(g, "OilPan_1", new Vector3(-2.6f, FLR, -1.4f));
            OilPan(g, "OilPan_2", new Vector3(1.9f, FLR, -3.9f));

            // ── left wall: workbench run ─────────────────────────────────
            WorkBench(g, "Bench_Main", X0 + 0.38f, -0.4f, 3.0f);   // stops short of the side exit
            BenchGrinder(g, "Grinder", new Vector3(X0 + 0.45f, 0.95f, 2.9f));
            Vise(g, "Vise", new Vector3(X0 + 0.5f, 0.95f, 0.0f));
            PegBoard(g, "PegBoard", X0 + 0.07f, -0.2f, 3.2f);

            // ── the two zones this chapter is really about ───────────────
            BuildInspectionZone(g);
            BuildHangoutCorner(g);

            // ── back wall: racking + consumables ─────────────────────────
            Shelving(g, "Rack_Parts_1", new Vector3(-4.0f, FLR, 6.5f), 2.4f, 0f);
            Shelving(g, "Rack_Parts_2", new Vector3(3.4f, FLR, 2.0f), 2.4f, 90f);
            for (int i = 0; i < 4; i++)
                TyreStack(g, "Tyres_" + i, new Vector3(-2.6f + i * 0.8f, FLR, 5.5f), 3 + (i % 3));
            for (int i = 0; i < 5; i++)
                Box("Carton_" + i, g, new Vector3(-5.6f + i * 0.62f, FLR + 0.22f, 5.3f),
                    new Vector3(0.55f, 0.44f, 0.45f), "Cardboard", new Vector3(0f, i * 13f, 0f), false);

            // ── services corner (front left) ─────────────────────────────
            Compressor(g, "Compressor", new Vector3(X0 + 0.9f, FLR, -6.1f));
            PartsWasher(g, "PartsWasher", new Vector3(-6.9f, FLR, -6.4f));
            WasteOilTank(g, "WasteOil", new Vector3(-4.9f, FLR, -6.45f));
            Place(P_DRUM, g, new Vector3(X0 + 0.8f, FLR, -5.2f), 12f);
            Place(P_DRUM, g, new Vector3(X0 + 0.8f, FLR, -4.5f), -40f);
            Drum(g, "Drum_Oil_A", new Vector3(X0 + 1.55f, FLR, -5.2f), "Yellow");
            Drum(g, "Drum_Oil_B", new Vector3(X0 + 1.55f, FLR, -4.5f), "SteelDark");
            Place(P_GASCAN, g, new Vector3(-7.6f, FLR, -6.3f), 30f);

            // ── back / centre floor equipment ────────────────────────────
            EngineHoist(g, "EngineHoist", new Vector3(-6.9f, FLR, -0.2f), 22f);
            WeldingCart(g, "WeldingCart", new Vector3(2.3f, FLR, 3.8f), -35f);
            TyreChanger(g, "TyreChanger", new Vector3(2.9f, FLR, 4.4f));
            Place(P_DRUM2, g, new Vector3(3.35f, FLR, 5.9f), 25f);
            Pallet(g, "Pallet_1", new Vector3(-3.2f, FLR, 4.0f), 12f);
            Pallet(g, "Pallet_2", new Vector3(1.0f, FLR, 1.8f), -25f);
            Bin(g, "Bin_Waste", new Vector3(-4.2f, FLR, -6.3f), "SteelDark");
            Bin(g, "Bin_Recycle", new Vector3(-3.4f, FLR, -6.3f), "SafetyGreen");
            Place(P_CONE, g, new Vector3(-0.9f, FLR, -6.2f), 0f);
            Place(P_CONE, g, new Vector3(3.35f, FLR, -6.2f), 0f);

            FireExtinguisher(g, "Ext_1", new Vector3(X0 + 0.42f, FLR, 3.7f), 90f);
            FireExtinguisher(g, "Ext_2", new Vector3(PARTX - 0.22f, FLR, 0.6f), -90f);
            FirstAid(g, "FirstAid", new Vector3(X0 + 0.4f, 1.55f, 1.6f), 90f);

            // ── rear-right storage strip ─────────────────────────────────
            Shelving(g, "Rack_Store_1", new Vector3(9.3f, FLR, 4.0f), 2.4f, 90f);
            Shelving(g, "Rack_Store_2", new Vector3(9.3f, FLR, 6.4f), 2.0f, 90f);
            TyreRack(g, "TyreRack", new Vector3(7.6f, FLR, 6.5f));
            for (int i = 0; i < 3; i++)
                TyreStack(g, "StoreTyres_" + i, new Vector3(8.4f + (i % 2) * 0.8f, FLR, 2.2f + (i / 2) * 0.85f), 4);
            Box("Locker_A", g, new Vector3(4.4f, FLR + 0.9f, 6.6f), new Vector3(0.9f, 1.8f, 0.5f), "SafetyGreen");
            Box("Locker_B", g, new Vector3(4.4f, FLR + 0.9f, 5.6f), new Vector3(0.9f, 1.8f, 0.5f), "SafetyGreen");
        }

        // ─────────────────────────── equipment builders ───────────────────────────

        static void TwoPostLift(Transform parent, string name, Vector3 p, bool raised)
        {
            var g = Group(name, parent);
            g.localPosition = p;
            float carriageY = raised ? 1.75f : 0.34f;

            for (int s = 0; s < 2; s++)
            {
                float x = s == 0 ? -1.5f : 1.5f;
                Box("Base_" + s, g, new Vector3(x, 0.03f, 0f), new Vector3(0.6f, 0.06f, 1.0f), "SteelDark");
                Box("Column_" + s, g, new Vector3(x, 1.85f, 0f), new Vector3(0.28f, 3.7f, 0.34f), "ToolRed");
                Box("ColumnCap_" + s, g, new Vector3(x, 3.72f, 0f), new Vector3(0.34f, 0.08f, 0.4f), "SteelDark", default(Vector3), false);
                Box("Carriage_" + s, g, new Vector3(x + (s == 0 ? 0.17f : -0.17f), carriageY, 0f), new Vector3(0.12f, 0.5f, 0.42f), "SteelDark", default(Vector3), false);

                for (int a = 0; a < 2; a++)
                {
                    float sign = a == 0 ? -1f : 1f;
                    float dir = s == 0 ? 1f : -1f;
                    var arm = Group("Arm_" + s + "_" + a, g);
                    arm.localPosition = new Vector3(x + dir * 0.2f, carriageY - 0.08f, 0f);
                    arm.localEulerAngles = new Vector3(0f, s == 0 ? sign * 32f : 180f - sign * 32f, 0f);
                    Box("Beam", arm, new Vector3(0f, 0f, 0.62f), new Vector3(0.14f, 0.11f, 1.25f), "SteelDark", default(Vector3), false);
                    Cyl("Pad", arm, new Vector3(0f, 0.08f, 1.18f), 0.15f, 0.12f, "Rubber");
                }
            }
            Box("TopBridge", g, new Vector3(0f, 3.78f, 0f), new Vector3(3.32f, 0.18f, 0.26f), "ToolRed", default(Vector3), false);
            Box("Control", g, new Vector3(-1.72f, 1.25f, 0.1f), new Vector3(0.18f, 0.5f, 0.34f), "Yellow");
            Cyl("Hose", g, new Vector3(0f, 3.7f, 0.16f), 0.04f, 3.0f, "PanelBlack", new Vector3(0f, 0f, 90f));
            Sign("Rating", g, new Vector3(-1.5f, 2.6f, -0.18f), new Vector2(0.24f, 0.1f), "4.0 T", Color.white, 0f);
        }

        static void WorkBench(Transform parent, string name, float x, float z0, float z1, float yaw = 0f)
        {
            var g = Group(name, parent);
            float len = z1 - z0, cz = (z0 + z1) * 0.5f;
            g.localPosition = new Vector3(x, FLR, cz);
            g.localEulerAngles = new Vector3(0f, yaw, 0f);

            Box("Top", g, new Vector3(0f, 0.9f, 0f), new Vector3(0.76f, 0.07f, len), "Wood");
            Box("TopEdge", g, new Vector3(0.36f, 0.86f, 0f), new Vector3(0.06f, 0.06f, len), "SteelDark", default(Vector3), false);
            Box("Shelf", g, new Vector3(0f, 0.22f, 0f), new Vector3(0.7f, 0.04f, len - 0.2f), "Steel", default(Vector3), false);
            int legs = Mathf.Max(2, Mathf.RoundToInt(len / 1.6f) + 1);
            for (int i = 0; i < legs; i++)
            {
                float lz = -len * 0.5f + 0.15f + i * (len - 0.3f) / (legs - 1);
                Box("Leg_A_" + i, g, new Vector3(-0.3f, 0.45f, lz), new Vector3(0.07f, 0.9f, 0.07f), "SteelDark", default(Vector3), false);
                Box("Leg_B_" + i, g, new Vector3(0.3f, 0.45f, lz), new Vector3(0.07f, 0.9f, 0.07f), "SteelDark", default(Vector3), false);
            }
            // drawer unit
            Box("Drawers", g, new Vector3(0.02f, 0.53f, -len * 0.5f + 0.75f), new Vector3(0.66f, 0.66f, 1.1f), "ToolRed", default(Vector3), false);
            for (int i = 0; i < 3; i++)
                Box("Handle_" + i, g, new Vector3(0.36f, 0.28f + i * 0.22f, -len * 0.5f + 0.75f), new Vector3(0.03f, 0.03f, 0.7f), "Alu", default(Vector3), false);
            // scattered tools on the top
            for (int i = 0; i < 6; i++)
                Box("Tool_" + i, g, new Vector3(-0.1f + (i % 3) * 0.14f, 0.955f, -len * 0.4f + i * (len * 0.16f)),
                    new Vector3(0.05f, 0.04f, 0.24f), i % 2 == 0 ? "Steel" : "Alu", new Vector3(0f, i * 22f, 0f), false);
            Box("Rag", g, new Vector3(0.12f, 0.95f, len * 0.3f), new Vector3(0.26f, 0.03f, 0.3f), "SafetyGreen", new Vector3(0f, 20f, 0f), false);
        }

        static void PegBoard(Transform parent, string name, float x, float z0, float z1)
        {
            var g = Group(name, parent);
            float len = z1 - z0;
            Box("Board", g, new Vector3(x, 1.75f, (z0 + z1) * 0.5f), new Vector3(0.04f, 1.5f, len), "OffWhite", default(Vector3), false);
            Box("Frame_T", g, new Vector3(x + 0.03f, 2.52f, (z0 + z1) * 0.5f), new Vector3(0.06f, 0.06f, len), "SteelDark", default(Vector3), false);
            // hanging spanners / hammers
            for (int i = 0; i < 14; i++)
            {
                float z = z0 + 0.35f + i * (len - 0.7f) / 13f;
                float h = 0.22f + (i % 4) * 0.09f;
                Box("Tool_" + i, g, new Vector3(x + 0.06f, 2.18f - h * 0.5f, z), new Vector3(0.03f, h, 0.05f), i % 3 == 0 ? "ToolRed" : "Steel", default(Vector3), false);
            }
            for (int i = 0; i < 5; i++)
                Box("Bin_" + i, g, new Vector3(x + 0.11f, 1.28f, z0 + 0.5f + i * (len - 1f) / 4f), new Vector3(0.16f, 0.14f, 0.24f), "Yellow", default(Vector3), false);
        }

        static void BenchGrinder(Transform parent, string name, Vector3 p)
        {
            var g = Group(name, parent);
            g.localPosition = p;
            Box("Base", g, Vector3.zero, new Vector3(0.28f, 0.1f, 0.34f), "SteelDark", default(Vector3), false);
            Box("Motor", g, new Vector3(0f, 0.16f, 0f), new Vector3(0.22f, 0.22f, 0.3f), "SafetyGreen", default(Vector3), false);
            Cyl("Wheel_L", g, new Vector3(0f, 0.16f, -0.2f), 0.18f, 0.04f, "Steel", new Vector3(90f, 0f, 0f));
            Cyl("Wheel_R", g, new Vector3(0f, 0.16f, 0.2f), 0.18f, 0.04f, "Steel", new Vector3(90f, 0f, 0f));
        }

        static void Vise(Transform parent, string name, Vector3 p)
        {
            var g = Group(name, parent);
            g.localPosition = p;
            Box("Body", g, Vector3.zero, new Vector3(0.18f, 0.12f, 0.34f), "SteelDark", default(Vector3), false);
            Box("Jaw", g, new Vector3(0f, 0.09f, -0.1f), new Vector3(0.2f, 0.1f, 0.08f), "Steel", default(Vector3), false);
            Cyl("Screw", g, new Vector3(0f, 0.06f, 0.24f), 0.04f, 0.3f, "Steel", new Vector3(90f, 0f, 0f));
        }

        static void ToolChest(Transform parent, string name, Vector3 p, float yaw)
        {
            var g = Group(name, parent);
            g.localPosition = p;
            g.localEulerAngles = new Vector3(0f, yaw, 0f);
            Box("Body", g, new Vector3(0f, 0.52f, 0f), new Vector3(1.02f, 0.9f, 0.48f), "ToolRed");
            Box("Top", g, new Vector3(0f, 1.0f, 0f), new Vector3(1.08f, 0.06f, 0.54f), "SteelDark", default(Vector3), false);
            for (int i = 0; i < 5; i++)
            {
                Box("Drawer_" + i, g, new Vector3(0f, 0.19f + i * 0.16f, -0.245f), new Vector3(0.96f, 0.14f, 0.02f), "SteelDark", default(Vector3), false);
                Box("Pull_" + i, g, new Vector3(0f, 0.19f + i * 0.16f, -0.27f), new Vector3(0.5f, 0.03f, 0.03f), "Alu", default(Vector3), false);
            }
            for (int i = 0; i < 4; i++)
                Cyl("Caster_" + i, g, new Vector3(i < 2 ? -0.42f : 0.42f, 0.05f, i % 2 == 0 ? -0.18f : 0.18f), 0.1f, 0.06f, "PanelBlack", new Vector3(90f, 0f, 0f));
            Box("Tools", g, new Vector3(0.1f, 1.06f, 0.05f), new Vector3(0.3f, 0.06f, 0.2f), "Steel", new Vector3(0f, 15f, 0f), false);
        }

        static void Shelving(Transform parent, string name, Vector3 p, float width, float yaw)
        {
            var g = Group(name, parent);
            g.localPosition = p;
            g.localEulerAngles = new Vector3(0f, yaw, 0f);
            const float depth = 0.62f, h = 2.6f;
            for (int i = 0; i < 4; i++)
            {
                float ux = (i % 2 == 0 ? -1f : 1f) * (width * 0.5f - 0.05f);
                float uz = (i < 2 ? -1f : 1f) * (depth * 0.5f - 0.05f);
                Box("Upright_" + i, g, new Vector3(ux, h * 0.5f, uz), new Vector3(0.07f, h, 0.07f), "Steel");
            }
            for (int i = 0; i < 4; i++)
            {
                float y = 0.32f + i * 0.72f;
                Box("Shelf_" + i, g, new Vector3(0f, y, 0f), new Vector3(width, 0.04f, depth), "Alu", default(Vector3), i == 0);
                int boxes = 3;
                for (int b = 0; b < boxes; b++)
                {
                    if ((i + b) % 4 == 0) continue;
                    Box("Crate_" + i + "_" + b, g, new Vector3(-width * 0.5f + 0.3f + b * (width - 0.6f) / (boxes - 1), y + 0.19f, 0f),
                        new Vector3(0.42f, 0.34f, 0.42f), (i + b) % 3 == 0 ? "Cardboard" : ((i + b) % 3 == 1 ? "SteelDark" : "ToolRed"),
                        new Vector3(0f, (i * 7 + b * 11) % 15, 0f), false);
                }
            }
            Sign("Label", g, new Vector3(0f, 2.72f, -depth * 0.5f - 0.02f), new Vector2(width * 0.8f, 0.16f), "PARTS", new Color(0.9f, 0.9f, 0.9f), 180f);
        }

        static void TyreStack(Transform parent, string name, Vector3 p, int count)
        {
            var g = Group(name, parent);
            g.localPosition = p;
            for (int i = 0; i < count; i++)
            {
                Cyl("Tyre_" + i, g, new Vector3(0f, 0.115f + i * 0.23f, 0f), 0.66f, 0.22f, "Rubber", new Vector3(0f, i * 17f, 0f), i == 0);
                Cyl("Rim_" + i, g, new Vector3(0f, 0.115f + i * 0.23f, 0f), 0.42f, 0.23f, "Alu", new Vector3(0f, i * 17f, 0f));
            }
        }

        static void TyreRack(Transform parent, string name, Vector3 p)
        {
            var g = Group(name, parent);
            g.localPosition = p;
            Box("Frame_L", g, new Vector3(-1.2f, 1.3f, 0f), new Vector3(0.08f, 2.6f, 0.5f), "Steel");
            Box("Frame_R", g, new Vector3(1.2f, 1.3f, 0f), new Vector3(0.08f, 2.6f, 0.5f), "Steel");
            for (int r = 0; r < 3; r++)
            {
                float y = 0.5f + r * 0.85f;
                Box("Bar_" + r, g, new Vector3(0f, y, 0f), new Vector3(2.4f, 0.06f, 0.06f), "Steel", default(Vector3), false);
                for (int t = 0; t < 6; t++)
                {
                    Cyl("Tyre_" + r + "_" + t, g, new Vector3(-1.0f + t * 0.4f, y + 0.36f, 0f), 0.68f, 0.24f, "Rubber", new Vector3(0f, 0f, 90f));
                }
            }
        }

        static void Drum(Transform parent, string name, Vector3 p, string mat)
        {
            var g = Group(name, parent);
            g.localPosition = p;
            Cyl("Body", g, new Vector3(0f, 0.44f, 0f), 0.58f, 0.88f, mat, default(Vector3), true);
            Cyl("Rib_A", g, new Vector3(0f, 0.3f, 0f), 0.61f, 0.05f, "SteelDark");
            Cyl("Rib_B", g, new Vector3(0f, 0.58f, 0f), 0.61f, 0.05f, "SteelDark");
            Cyl("Lid", g, new Vector3(0f, 0.89f, 0f), 0.59f, 0.03f, "SteelDark");
        }

        static void Compressor(Transform parent, string name, Vector3 p)
        {
            var g = Group(name, parent);
            g.localPosition = p;
            Cyl("Tank", g, new Vector3(0f, 0.55f, 0f), 0.56f, 1.7f, "SafetyGreen", new Vector3(0f, 0f, 90f), true);
            Box("Foot_A", g, new Vector3(-0.6f, 0.14f, 0f), new Vector3(0.14f, 0.28f, 0.4f), "SteelDark", default(Vector3), false);
            Box("Foot_B", g, new Vector3(0.6f, 0.14f, 0f), new Vector3(0.14f, 0.28f, 0.4f), "SteelDark", default(Vector3), false);
            Box("Motor", g, new Vector3(-0.1f, 1.02f, 0f), new Vector3(0.5f, 0.4f, 0.42f), "SteelDark", default(Vector3), false);
            Cyl("Pump", g, new Vector3(0.35f, 1.05f, 0f), 0.26f, 0.34f, "Steel");
            Cyl("Flywheel", g, new Vector3(0.35f, 1.05f, 0.24f), 0.4f, 0.06f, "PanelBlack", new Vector3(90f, 0f, 0f));
            Cyl("Pipe", g, new Vector3(0.55f, 0.85f, 0f), 0.05f, 0.6f, "Copper");
            Box("Gauge", g, new Vector3(-0.5f, 1.28f, 0f), new Vector3(0.12f, 0.12f, 0.06f), "White", default(Vector3), false);
        }

        static void PartsWasher(Transform parent, string name, Vector3 p)
        {
            var g = Group(name, parent);
            g.localPosition = p;
            Box("Cabinet", g, new Vector3(0f, 0.42f, 0f), new Vector3(0.95f, 0.84f, 0.6f), "ToolRed");
            Box("Basin", g, new Vector3(0f, 0.9f, 0f), new Vector3(0.98f, 0.12f, 0.63f), "SteelDark", default(Vector3), false);
            Box("Lid", g, new Vector3(0f, 1.28f, 0.28f), new Vector3(0.98f, 0.04f, 0.66f), "SteelDark", new Vector3(58f, 0f, 0f), false);
            Cyl("Nozzle", g, new Vector3(0.3f, 1.0f, -0.1f), 0.03f, 0.4f, "Copper", new Vector3(24f, 0f, 0f));
        }

        static void WasteOilTank(Transform parent, string name, Vector3 p)
        {
            var g = Group(name, parent);
            g.localPosition = p;
            Cyl("Tank", g, new Vector3(0f, 0.62f, 0f), 0.72f, 1.24f, "SteelDark", default(Vector3), true);
            Cyl("Funnel", g, new Vector3(0f, 1.32f, 0f), 0.5f, 0.16f, "Steel");
            Sign("Label", g, new Vector3(0f, 0.8f, -0.37f), new Vector2(0.6f, 0.16f), "WASTE OIL", new Color(0.95f, 0.8f, 0.1f), 180f);
        }

        static void EngineHoist(Transform parent, string name, Vector3 p, float yaw)
        {
            var g = Group(name, parent);
            g.localPosition = p;
            g.localEulerAngles = new Vector3(0f, yaw, 0f);
            Box("Leg_L", g, new Vector3(-0.42f, 0.1f, 0.6f), new Vector3(0.1f, 0.14f, 1.5f), "ToolRed", default(Vector3), false);
            Box("Leg_R", g, new Vector3(0.42f, 0.1f, 0.6f), new Vector3(0.1f, 0.14f, 1.5f), "ToolRed", default(Vector3), false);
            Box("Cross", g, new Vector3(0f, 0.1f, -0.1f), new Vector3(0.94f, 0.14f, 0.12f), "ToolRed", default(Vector3), false);
            Box("Mast", g, new Vector3(0f, 1.05f, -0.05f), new Vector3(0.14f, 1.9f, 0.14f), "ToolRed");
            Box("Boom", g, new Vector3(0f, 1.82f, 0.72f), new Vector3(0.12f, 0.12f, 1.6f), "ToolRed", new Vector3(-14f, 0f, 0f), false);
            Cyl("Ram", g, new Vector3(0f, 1.25f, 0.34f), 0.09f, 0.9f, "Steel", new Vector3(-38f, 0f, 0f));
            Cyl("Chain", g, new Vector3(0f, 1.45f, 1.45f), 0.03f, 0.7f, "SteelDark");
            Box("Hook", g, new Vector3(0f, 1.06f, 1.45f), new Vector3(0.08f, 0.14f, 0.06f), "SteelDark", default(Vector3), false);
            for (int i = 0; i < 4; i++)
                Cyl("Caster_" + i, g, new Vector3(i < 2 ? -0.42f : 0.42f, 0.06f, i % 2 == 0 ? -0.1f : 1.3f), 0.12f, 0.06f, "PanelBlack", new Vector3(90f, 0f, 0f));
        }

        static void WeldingCart(Transform parent, string name, Vector3 p, float yaw)
        {
            var g = Group(name, parent);
            g.localPosition = p;
            g.localEulerAngles = new Vector3(0f, yaw, 0f);
            Box("Cart", g, new Vector3(0f, 0.4f, 0f), new Vector3(0.62f, 0.06f, 0.85f), "SteelDark", default(Vector3), false);
            Box("Frame", g, new Vector3(0f, 0.7f, 0.38f), new Vector3(0.58f, 0.66f, 0.05f), "SteelDark", default(Vector3), false);
            Box("Welder", g, new Vector3(0f, 0.62f, -0.1f), new Vector3(0.52f, 0.4f, 0.62f), "ToolRed");
            Cyl("Bottle_A", g, new Vector3(-0.16f, 0.85f, 0.3f), 0.23f, 1.35f, "SafetyGreen", default(Vector3), true);
            Cyl("Bottle_B", g, new Vector3(0.16f, 0.85f, 0.3f), 0.23f, 1.35f, "SteelDark", default(Vector3), true);
            Cyl("Reg_A", g, new Vector3(-0.16f, 1.58f, 0.3f), 0.1f, 0.14f, "Copper");
            Cyl("Reg_B", g, new Vector3(0.16f, 1.58f, 0.3f), 0.1f, 0.14f, "Copper");
            for (int i = 0; i < 2; i++)
                Cyl("Wheel_" + i, g, new Vector3(i == 0 ? -0.32f : 0.32f, 0.16f, 0.3f), 0.3f, 0.06f, "PanelBlack", new Vector3(0f, 0f, 90f));
            Cyl("Torch_Hose", g, new Vector3(-0.35f, 0.55f, -0.2f), 0.04f, 0.9f, "PanelBlack", new Vector3(70f, 20f, 0f));
        }

        static void TyreChanger(Transform parent, string name, Vector3 p)
        {
            var g = Group(name, parent);
            g.localPosition = p;
            Box("Base", g, new Vector3(0f, 0.06f, 0f), new Vector3(0.9f, 0.12f, 0.9f), "ToolRed");
            Box("Table", g, new Vector3(0f, 0.55f, 0f), new Vector3(0.7f, 0.86f, 0.7f), "ToolRed", default(Vector3), false);
            Cyl("Plate", g, new Vector3(0f, 1.02f, 0f), 0.62f, 0.06f, "SteelDark");
            Box("Column", g, new Vector3(0f, 0.95f, -0.5f), new Vector3(0.16f, 1.9f, 0.16f), "ToolRed");
            Box("Arm", g, new Vector3(0.24f, 1.78f, -0.35f), new Vector3(0.62f, 0.12f, 0.12f), "SteelDark", default(Vector3), false);
            Cyl("Duckhead", g, new Vector3(0.5f, 1.5f, -0.35f), 0.07f, 0.5f, "SteelDark");
        }

        static void FloorJack(Transform parent, string name, Vector3 p, float yaw)
        {
            var g = Group(name, parent);
            g.localPosition = p;
            g.localEulerAngles = new Vector3(0f, yaw, 0f);
            Box("Body", g, new Vector3(0f, 0.13f, 0f), new Vector3(0.34f, 0.22f, 0.95f), "ToolRed", default(Vector3), false);
            Cyl("Saddle", g, new Vector3(0f, 0.27f, -0.28f), 0.14f, 0.08f, "SteelDark");
            Cyl("Handle", g, new Vector3(0f, 0.45f, 0.75f), 0.05f, 1.1f, "SteelDark", new Vector3(70f, 0f, 0f));
            for (int i = 0; i < 4; i++)
                Cyl("Wheel_" + i, g, new Vector3(i < 2 ? -0.16f : 0.16f, 0.06f, i % 2 == 0 ? -0.36f : 0.36f), 0.12f, 0.05f, "PanelBlack", new Vector3(0f, 0f, 90f));
        }

        static void JackStand(Transform parent, string name, Vector3 p)
        {
            var g = Group(name, parent);
            g.localPosition = p;
            Box("Base", g, new Vector3(0f, 0.02f, 0f), new Vector3(0.3f, 0.04f, 0.3f), "Yellow", default(Vector3), false);
            for (int i = 0; i < 4; i++)
                Box("Leg_" + i, g, new Vector3(i < 2 ? -0.1f : 0.1f, 0.2f, i % 2 == 0 ? -0.1f : 0.1f), new Vector3(0.03f, 0.4f, 0.03f), "Yellow",
                    new Vector3(i % 2 == 0 ? -8f : 8f, 0f, i < 2 ? 8f : -8f), false);
            Box("Post", g, new Vector3(0f, 0.44f, 0f), new Vector3(0.06f, 0.18f, 0.06f), "Yellow", default(Vector3), false);
            Box("Saddle", g, new Vector3(0f, 0.53f, 0f), new Vector3(0.12f, 0.04f, 0.12f), "SteelDark", default(Vector3), false);
        }

        static void Creeper(Transform parent, string name, Vector3 p, float yaw)
        {
            var g = Group(name, parent);
            g.localPosition = p;
            g.localEulerAngles = new Vector3(0f, yaw, 0f);
            Box("Deck", g, new Vector3(0f, 0.11f, 0f), new Vector3(0.44f, 0.06f, 1.25f), "ToolRed", default(Vector3), false);
            Box("Head", g, new Vector3(0f, 0.16f, -0.5f), new Vector3(0.36f, 0.06f, 0.28f), "PanelBlack", new Vector3(-14f, 0f, 0f), false);
            for (int i = 0; i < 6; i++)
                Cyl("Caster_" + i, g, new Vector3(i % 2 == 0 ? -0.19f : 0.19f, 0.04f, -0.45f + (i / 2) * 0.45f), 0.07f, 0.04f, "PanelBlack", new Vector3(0f, 0f, 90f));
        }

        static void OilPan(Transform parent, string name, Vector3 p)
        {
            var g = Group(name, parent);
            g.localPosition = p;
            Cyl("Pan", g, new Vector3(0f, 0.06f, 0f), 0.62f, 0.12f, "PanelBlack");
            Decal("Spill", g, new Vector3(0.35f, 0.008f, 0.2f), new Vector2(0.9f, 0.7f), "PanelBlack", 20f);
        }

        static void Pallet(Transform parent, string name, Vector3 p, float yaw)
        {
            var g = Group(name, parent);
            g.localPosition = p;
            g.localEulerAngles = new Vector3(0f, yaw, 0f);
            for (int i = 0; i < 3; i++)
                Box("Bearer_" + i, g, new Vector3(-0.5f + i * 0.5f, 0.06f, 0f), new Vector3(0.1f, 0.12f, 1.2f), "Wood", default(Vector3), false);
            for (int i = 0; i < 6; i++)
                Box("Deck_" + i, g, new Vector3(0f, 0.14f, -0.5f + i * 0.2f), new Vector3(1.2f, 0.03f, 0.12f), "Wood", default(Vector3), i == 0);
        }

        static void Bin(Transform parent, string name, Vector3 p, string mat)
        {
            var g = Group(name, parent);
            g.localPosition = p;
            Box("Body", g, new Vector3(0f, 0.45f, 0f), new Vector3(0.62f, 0.9f, 0.62f), mat);
            Box("Rim", g, new Vector3(0f, 0.92f, 0f), new Vector3(0.66f, 0.05f, 0.66f), "SteelDark", default(Vector3), false);
            Box("Bag", g, new Vector3(0.04f, 0.98f, 0f), new Vector3(0.5f, 0.12f, 0.5f), "PanelBlack", default(Vector3), false);
        }

        static void FireExtinguisher(Transform parent, string name, Vector3 p, float yaw)
        {
            var g = Group(name, parent);
            g.localPosition = p;
            g.localEulerAngles = new Vector3(0f, yaw, 0f);
            Cyl("Bottle", g, new Vector3(0f, 1.05f, 0f), 0.17f, 0.58f, "ToolRed");
            Cyl("Neck", g, new Vector3(0f, 1.38f, 0f), 0.07f, 0.12f, "SteelDark");
            Box("Handle", g, new Vector3(0f, 1.46f, 0f), new Vector3(0.14f, 0.05f, 0.06f), "SteelDark", default(Vector3), false);
            Box("Bracket", g, new Vector3(0f, 1.1f, 0.1f), new Vector3(0.2f, 0.05f, 0.08f), "SteelDark", default(Vector3), false);
            Box("SignPlate", g, new Vector3(0f, 1.85f, 0.06f), new Vector3(0.28f, 0.28f, 0.02f), "ToolRed", default(Vector3), false);
            Sign("SignText", g, new Vector3(0f, 1.85f, 0.05f), new Vector2(0.26f, 0.12f), "FIRE", Color.white, 0f);
        }

        static void FirstAid(Transform parent, string name, Vector3 p, float yaw)
        {
            var g = Group(name, parent);
            g.localPosition = p;
            g.localEulerAngles = new Vector3(0f, yaw, 0f);
            Box("Box", g, Vector3.zero, new Vector3(0.42f, 0.34f, 0.16f), "White", default(Vector3), false);
            Box("Cross_V", g, new Vector3(0f, 0f, -0.09f), new Vector3(0.07f, 0.22f, 0.02f), "SafetyGreen", default(Vector3), false);
            Box("Cross_H", g, new Vector3(0f, 0f, -0.09f), new Vector3(0.22f, 0.07f, 0.02f), "SafetyGreen", default(Vector3), false);
        }

        static void WorkLight(Transform parent, string name, Vector3 p)
        {
            var g = Group(name, parent);
            g.localPosition = p;
            Box("Foot", g, Vector3.zero, new Vector3(0.5f, 0.05f, 0.5f), "SteelDark", default(Vector3), false);
            Cyl("Post", g, new Vector3(0f, 0.8f, 0f), 0.05f, 1.6f, "SteelDark");
            Box("Head", g, new Vector3(0f, 1.62f, 0.08f), new Vector3(0.44f, 0.24f, 0.18f), "Yellow", new Vector3(24f, 0f, 0f), false);
            Box("Lens", g, new Vector3(0f, 1.55f, 0.16f), new Vector3(0.38f, 0.16f, 0.03f), "LampWhite", new Vector3(24f, 0f, 0f), false);
            var lg = new GameObject("Light");
            lg.transform.SetParent(g, false);
            lg.transform.localPosition = new Vector3(0f, 1.55f, 0.2f);
            lg.transform.localEulerAngles = new Vector3(24f, 0f, 0f);
            var l = lg.AddComponent<Light>();
            l.type = LightType.Spot;
            l.spotAngle = 85f;
            l.range = 9f;
            l.intensity = 3.2f;
            l.color = new Color(1f, 0.95f, 0.86f);
            l.shadows = LightShadows.Soft;
        }

        // ══════════════════ CHAPTER 2 CORE: the inspection zone ══════════════════
        // Rear-left corner of the shop, deliberately the cleanest, brightest and most
        // enclosed spot in the building – this is the room the whole chapter plays in.

        public static readonly Vector3 INSPECT_TABLE = new Vector3(-7.6f + WSHIFT, FLR, 5.5f);
        const float BACKWALL_FACE = Z1 - WT * 0.5f - 0.08f;   // inside face of the rear wall

        static void BuildInspectionZone(Transform parent)
        {
            var g = Group("ZONE_Inspection", parent);

            // clean-zone floor paint, so the area reads as "not a normal service bay"
            float fy = FLR + 0.007f;
            Box("Zone_L", g, new Vector3(-9.75f + WSHIFT, fy, 5.2f), new Vector3(0.1f, 0.012f, 3.3f), "LineWhite", default(Vector3), false);
            Box("Zone_R", g, new Vector3(-5.3f + WSHIFT, fy, 5.2f), new Vector3(0.1f, 0.012f, 3.3f), "LineWhite", default(Vector3), false);
            Box("Zone_F", g, new Vector3(-7.5f + WSHIFT, fy, 3.55f), new Vector3(4.5f, 0.012f, 0.1f), "LineWhite", default(Vector3), false);
            Box("Zone_Mat", g, new Vector3(-7.5f + WSHIFT, fy - 0.002f, 5.2f), new Vector3(4.3f, 0.01f, 3.2f), "ConcreteDark", default(Vector3), false);

            // stainless inspection bench, long axis along X so both of them face the boards
            var t = Group("InspectionBench", g);
            t.localPosition = INSPECT_TABLE;
            Box("Top", t, new Vector3(0f, 0.9f, 0f), new Vector3(2.4f, 0.06f, 1.0f), "Stainless");
            Box("Lip", t, new Vector3(0f, 0.94f, 0.5f), new Vector3(2.4f, 0.05f, 0.05f), "Stainless", default(Vector3), false);
            Box("Shelf", t, new Vector3(0f, 0.24f, 0f), new Vector3(2.3f, 0.04f, 0.92f), "Stainless", default(Vector3), false);
            for (int i = 0; i < 4; i++)
                Box("Leg_" + i, t, new Vector3(i < 2 ? -1.05f : 1.05f, 0.45f, i % 2 == 0 ? -0.42f : 0.42f),
                    new Vector3(0.06f, 0.9f, 0.06f), "Steel", default(Vector3), false);
            Box("Drawer", t, new Vector3(0.75f, 0.72f, 0f), new Vector3(0.7f, 0.2f, 0.9f), "Stainless", default(Vector3), false);
            Box("Drawer_Pull", t, new Vector3(0.75f, 0.72f, -0.46f), new Vector3(0.5f, 0.03f, 0.03f), "Alu", default(Vector3), false);

            EvidenceLayout(t, new Vector3(-0.35f, 0.93f, 0f));
            MagnifierLamp(t, new Vector3(-1.0f, 0.93f, 0.38f));

            // rolling stools – Kem on one side, his friend on the other
            Stool(g, "Stool_Kem", new Vector3(-8.4f + WSHIFT, FLR, 4.5f));
            Stool(g, "Stool_Friend", new Vector3(-6.7f + WSHIFT, FLR, 4.6f));

            // laptop / parts database cart
            var c = Group("DatabaseCart", g);
            // moved off the left wall: it used to sit right in the personnel door opening
            c.localPosition = new Vector3(-8.5f + WSHIFT, FLR, 3.9f);
            Box("Frame", c, new Vector3(0f, 0.45f, 0f), new Vector3(0.6f, 0.06f, 1.0f), "SteelDark", default(Vector3), false);
            Box("Top", c, new Vector3(0f, 0.92f, 0f), new Vector3(0.66f, 0.05f, 1.1f), "SteelDark");
            for (int i = 0; i < 4; i++)
            {
                Box("Post_" + i, c, new Vector3(i < 2 ? -0.28f : 0.28f, 0.46f, i % 2 == 0 ? -0.5f : 0.5f), new Vector3(0.04f, 0.92f, 0.04f), "Steel", default(Vector3), false);
                Cyl("Caster_" + i, c, new Vector3(i < 2 ? -0.28f : 0.28f, 0.05f, i % 2 == 0 ? -0.5f : 0.5f), 0.09f, 0.05f, "PanelBlack", new Vector3(90f, 0f, 0f));
            }
            Box("Monitor", c, new Vector3(0.24f, 1.24f, 0.1f), new Vector3(0.04f, 0.44f, 0.76f), "PanelBlack", new Vector3(0f, 0f, 0f), false);
            Box("Screen", c, new Vector3(0.21f, 1.24f, 0.1f), new Vector3(0.02f, 0.38f, 0.7f), "ScreenBlue", default(Vector3), false);
            Box("Stand", c, new Vector3(0.24f, 1.0f, 0.1f), new Vector3(0.16f, 0.16f, 0.2f), "PanelBlack", default(Vector3), false);
            Box("Laptop_Base", c, new Vector3(-0.05f, 0.96f, -0.3f), new Vector3(0.34f, 0.03f, 0.26f), "SteelDark", default(Vector3), false);
            Box("Laptop_Lid", c, new Vector3(-0.2f, 1.06f, -0.3f), new Vector3(0.03f, 0.22f, 0.26f), "ScreenBlue", new Vector3(0f, 0f, 14f), false);
            Marker("INTERACT_PartsDatabase", c, new Vector3(0.6f, 0f, 0f));

            // the two reference boards, mounted on the rear wall right behind the bench
            InvestigationBoard(g, new Vector3(-8.6f + WSHIFT, 1.85f, BACKWALL_FACE));
            PaintChipBoard(g, new Vector3(-6.4f + WSHIFT, 1.75f, BACKWALL_FACE));

            // catalogue shelf + printer, the friend's reference library (against the left wall)
            var sh = Group("CatalogueShelf", g);
            sh.localPosition = new Vector3(-9.6f + WSHIFT, FLR, 6.1f);
            Box("Body", sh, new Vector3(0f, 0.9f, 0f), new Vector3(0.42f, 1.8f, 1.5f), "SteelDark");
            for (int i = 0; i < 4; i++)
            {
                Box("Shelf_" + i, sh, new Vector3(0f, 0.28f + i * 0.44f, 0f), new Vector3(0.4f, 0.03f, 1.42f), "Steel", default(Vector3), false);
                for (int b = 0; b < 6; b++)
                    Box("Binder_" + i + "_" + b, sh, new Vector3(0f, 0.44f + i * 0.44f, -0.6f + b * 0.24f),
                        new Vector3(0.34f, 0.3f, 0.2f), (i + b) % 3 == 0 ? "ToolRed" : ((i + b) % 3 == 1 ? "SafetyGreen" : "Cardboard"), default(Vector3), false);
            }
            Box("Printer", sh, new Vector3(0f, 1.94f, 0f), new Vector3(0.42f, 0.28f, 0.6f), "OffWhite", default(Vector3), false);
            Box("Printout", sh, new Vector3(0.16f, 2.1f, 0f), new Vector3(0.3f, 0.02f, 0.42f), "Paper", new Vector3(0f, 0f, 24f), false);
            Marker("INTERACT_Records_Printout", sh, new Vector3(0.6f, 1.9f, 0f));

            // dedicated task lighting over the bench (named Insp_* so the mood switch boosts them)
            TaskLight(g, "Insp_Light_A", new Vector3(-8.5f + WSHIFT, 3.3f, 5.4f), true);
            TaskLight(g, "Insp_Light_B", new Vector3(-6.7f + WSHIFT, 3.3f, 5.4f), false);
            WorkLight(g, "Insp_StandLight", new Vector3(-5.7f + WSHIFT, FLR, 4.2f));

            Marker("NAV_InspectionZone", g, new Vector3(-7.6f + WSHIFT, FLR, 4.4f));
            Marker("STAND_Kem", g, new Vector3(-8.4f + WSHIFT, FLR, 4.5f));
            Marker("STAND_Friend", g, new Vector3(-6.7f + WSHIFT, FLR, 4.6f));
        }

        /// <summary>What Kem carries in from chapter 1, laid out for examination.</summary>
        static void EvidenceLayout(Transform parent, Vector3 p)
        {
            var g = Group("EVIDENCE_Layout", parent);
            g.localPosition = p;

            // photo scale + tray
            Box("Tray", g, new Vector3(0f, 0.01f, 0f), new Vector3(0.62f, 0.03f, 0.9f), "Stainless", default(Vector3), false);
            Box("ScaleRuler", g, new Vector3(-0.22f, 0.03f, -0.34f), new Vector3(0.05f, 0.01f, 0.3f), "Yellow", default(Vector3), false);

            // the red bumper fragment – the item picked up at the crash site
            Box("Fragment_Bumper_RED", g, new Vector3(0.02f, 0.06f, 0.06f), new Vector3(0.3f, 0.07f, 0.42f), "CarRed", new Vector3(6f, 16f, 3f), false);
            Box("Fragment_Edge", g, new Vector3(0.12f, 0.06f, 0.24f), new Vector3(0.1f, 0.05f, 0.12f), "CarRed", new Vector3(0f, 34f, 12f), false);
            Box("Fragment_Paint_Flake", g, new Vector3(-0.14f, 0.035f, 0.3f), new Vector3(0.06f, 0.01f, 0.07f), "CarRed", new Vector3(0f, 22f, 0f), false);

            // evidence bag, gloves, tweezers, torch, camera
            Box("EvidenceBag", g, new Vector3(-0.16f, 0.03f, -0.1f), new Vector3(0.26f, 0.02f, 0.34f), "Glass", new Vector3(0f, -8f, 0f), false);
            Box("Gloves", g, new Vector3(0.2f, 0.03f, -0.3f), new Vector3(0.18f, 0.02f, 0.22f), "Glass", new Vector3(0f, 20f, 0f), false);
            Box("Tweezers", g, new Vector3(-0.02f, 0.03f, -0.22f), new Vector3(0.03f, 0.01f, 0.18f), "Steel", new Vector3(0f, -14f, 0f), false);
            Box("Torch", g, new Vector3(0.22f, 0.05f, 0.34f), new Vector3(0.05f, 0.05f, 0.22f), "SteelDark", new Vector3(0f, 8f, 0f), false);
            Box("Camera_Body", g, new Vector3(-0.2f, 0.07f, 0.36f), new Vector3(0.16f, 0.11f, 0.09f), "PanelBlack", new Vector3(0f, 18f, 0f), false);
            Cyl("Camera_Lens", g, new Vector3(-0.17f, 0.07f, 0.31f), 0.09f, 0.08f, "PanelBlack", new Vector3(90f, 0f, 0f));
            Box("Notepad", g, new Vector3(0.2f, 0.025f, -0.02f), new Vector3(0.2f, 0.01f, 0.28f), "Paper", new Vector3(0f, -6f, 0f), false);

            Marker("INTERACT_PlaceEvidence", g, Vector3.zero);
            Marker("INTERACT_Fragment", g, new Vector3(0.02f, 0.1f, 0.06f));
            Marker("INTERACT_TakePhoto", g, new Vector3(-0.2f, 0.12f, 0.36f));
        }

        static void MagnifierLamp(Transform parent, Vector3 p)
        {
            var g = Group("MagnifierLamp", parent);
            g.localPosition = p;
            Box("Clamp", g, new Vector3(0f, 0.02f, 0f), new Vector3(0.12f, 0.06f, 0.12f), "SteelDark", default(Vector3), false);
            Box("Arm_A", g, new Vector3(0f, 0.3f, -0.12f), new Vector3(0.04f, 0.6f, 0.04f), "Alu", new Vector3(22f, 0f, 0f), false);
            Box("Arm_B", g, new Vector3(0f, 0.62f, -0.52f), new Vector3(0.04f, 0.62f, 0.04f), "Alu", new Vector3(72f, 0f, 0f), false);
            Cyl("LensRing", g, new Vector3(0f, 0.62f, -0.9f), 0.3f, 0.05f, "Alu", new Vector3(70f, 0f, 0f));
            Cyl("Lens", g, new Vector3(0f, 0.61f, -0.89f), 0.26f, 0.03f, "Glass", new Vector3(70f, 0f, 0f));
            Box("Switch", g, new Vector3(0.06f, 0.06f, 0f), new Vector3(0.04f, 0.03f, 0.05f), "ToolRed", default(Vector3), false);
            var lg = new GameObject("Light");
            lg.transform.SetParent(g, false);
            lg.transform.localPosition = new Vector3(0f, 0.58f, -0.85f);
            lg.transform.localEulerAngles = new Vector3(70f, 0f, 0f);
            var l = lg.AddComponent<Light>();
            l.type = LightType.Spot; l.spotAngle = 62f; l.range = 3.2f; l.intensity = 3.0f;
            l.color = new Color(1f, 0.98f, 0.94f); l.shadows = LightShadows.None;
            Marker("INTERACT_Magnifier", g, new Vector3(0f, 0.5f, -0.9f));
        }

        /// <summary>Manufacturer paint-code chart: the friend matches the fragment against it.</summary>
        static void PaintChipBoard(Transform parent, Vector3 p)
        {
            var g = Group("PaintChipBoard", parent);
            g.localPosition = p;
            Box("Panel", g, Vector3.zero, new Vector3(2.0f, 1.15f, 0.05f), "OffWhite", default(Vector3), false);
            Box("Frame", g, new Vector3(0f, 0f, 0.02f), new Vector3(2.08f, 1.22f, 0.02f), "SteelDark", default(Vector3), false);
            string[] chips = { "CarRed", "BrandRed", "ToolRed", "Rust", "Yellow", "SafetyGreen", "White", "PanelBlack",
                               "Steel", "Copper", "Alu", "Wood", "Cardboard", "Tarp", "ConcreteDark", "Grass" };
            for (int i = 0; i < chips.Length; i++)
            {
                int col = i % 8, row = i / 8;
                Box("Chip_" + i, g, new Vector3(-0.85f + col * 0.24f, 0.28f - row * 0.34f, -0.04f),
                    new Vector3(0.2f, 0.26f, 0.02f), chips[i], default(Vector3), false);
            }
            // the candidate reds are ringed off with a marker pen box
            Box("Ring_T", g, new Vector3(-0.61f, 0.44f, -0.055f), new Vector3(0.78f, 0.03f, 0.01f), "Yellow", default(Vector3), false);
            Box("Ring_B", g, new Vector3(-0.61f, 0.12f, -0.055f), new Vector3(0.78f, 0.03f, 0.01f), "Yellow", default(Vector3), false);
            Box("Ring_L", g, new Vector3(-1.0f, 0.28f, -0.055f), new Vector3(0.03f, 0.35f, 0.01f), "Yellow", default(Vector3), false);
            Box("Ring_R", g, new Vector3(-0.22f, 0.28f, -0.055f), new Vector3(0.03f, 0.35f, 0.01f), "Yellow", default(Vector3), false);
            Sign("Title", g, new Vector3(0f, 0.5f, -0.06f), new Vector2(1.6f, 0.16f), "OEM PAINT CODE CHART", new Color(0.2f, 0.2f, 0.2f), 180f);
            Marker("INTERACT_PaintChart", g, new Vector3(0f, -0.9f, -0.9f));
        }

        /// <summary>Cork board where the two of them pin the case together.</summary>
        static void InvestigationBoard(Transform parent, Vector3 p)
        {
            var g = Group("InvestigationBoard", parent);
            g.localPosition = p;
            Box("Cork", g, Vector3.zero, new Vector3(2.2f, 1.3f, 0.05f), "Cork", default(Vector3), false);
            Box("Frame", g, new Vector3(0f, 0f, 0.02f), new Vector3(2.28f, 1.38f, 0.02f), "Wood", default(Vector3), false);
            for (int i = 0; i < 9; i++)
            {
                int col = i % 3, row = i / 3;
                Box("Photo_" + i, g, new Vector3(-0.68f + col * 0.68f, 0.38f - row * 0.42f, -0.04f),
                    new Vector3(0.38f, 0.28f, 0.02f), i % 4 == 0 ? "ScreenBlue" : "Paper", new Vector3(0f, 0f, (i % 2 == 0 ? 2f : -2f)), false);
                Box("Pin_" + i, g, new Vector3(-0.68f + col * 0.68f, 0.5f - row * 0.42f, -0.055f), new Vector3(0.02f, 0.02f, 0.02f), "ToolRed", default(Vector3), false);
            }
            // red string linking the pins
            Box("String_1", g, new Vector3(-0.34f, 0.28f, -0.06f), new Vector3(0.78f, 0.01f, 0.01f), "StringRed", new Vector3(0f, 0f, 16f), false);
            Box("String_2", g, new Vector3(0.34f, -0.1f, -0.06f), new Vector3(0.8f, 0.01f, 0.01f), "StringRed", new Vector3(0f, 0f, -22f), false);
            Box("String_3", g, new Vector3(0f, 0.06f, -0.06f), new Vector3(1.5f, 0.01f, 0.01f), "StringRed", new Vector3(0f, 0f, 8f), false);
            Sign("Title", g, new Vector3(0f, 0.58f, -0.06f), new Vector2(1.5f, 0.16f), "CASE  ·  HIT AND RUN", new Color(0.9f, 0.9f, 0.88f), 180f);
            Marker("INTERACT_Board", g, new Vector3(0f, -1.0f, -0.9f));
        }

        static void Stool(Transform parent, string name, Vector3 p)
        {
            var g = Group(name, parent);
            g.localPosition = p;
            Cyl("Seat", g, new Vector3(0f, 0.56f, 0f), 0.36f, 0.08f, "PanelDark");
            Cyl("Gas", g, new Vector3(0f, 0.3f, 0f), 0.06f, 0.5f, "Alu");
            for (int i = 0; i < 5; i++)
            {
                float a = i * Mathf.PI * 2f / 5f;
                Box("Star_" + i, g, new Vector3(Mathf.Cos(a) * 0.14f, 0.06f, Mathf.Sin(a) * 0.14f), new Vector3(0.05f, 0.04f, 0.3f),
                    "PanelBlack", new Vector3(0f, -a * Mathf.Rad2Deg, 0f), false);
                Cyl("Caster_" + i, g, new Vector3(Mathf.Cos(a) * 0.27f, 0.035f, Mathf.Sin(a) * 0.27f), 0.07f, 0.04f, "PanelBlack", new Vector3(90f, 0f, 0f));
            }
        }

        static void TaskLight(Transform parent, string name, Vector3 p, bool shadows)
        {
            var g = Group(name, parent);
            g.localPosition = p;
            Box("Chain", g, new Vector3(0f, 1.0f, 0f), new Vector3(0.02f, 2.0f, 0.02f), "SteelDark", default(Vector3), false);
            Box("Shade", g, new Vector3(0f, -0.06f, 0f), new Vector3(0.8f, 0.14f, 0.5f), "Alu", default(Vector3), false);
            Box("Tube", g, new Vector3(0f, -0.14f, 0f), new Vector3(0.72f, 0.06f, 0.4f), "LampWhite", default(Vector3), false);
            var lg = new GameObject("Light");
            lg.transform.SetParent(g, false);
            lg.transform.localPosition = new Vector3(0f, -0.2f, 0f);
            var l = lg.AddComponent<Light>();
            l.type = LightType.Point; l.range = 7f; l.intensity = 4.2f;
            l.color = new Color(1f, 0.98f, 0.95f);
            l.shadows = shadows ? LightShadows.Soft : LightShadows.None;
        }

        // ══════════════════ the friend's hangout corner ══════════════════
        static void BuildHangoutCorner(Transform parent)
        {
            var g = Group("ZONE_Hangout", parent);

            Box("Rug", g, new Vector3(-8.2f + WSHIFT, FLR + 0.008f, -2.7f), new Vector3(2.8f, 0.012f, 3.2f), "Fabric", default(Vector3), false);

            Sofa(g, "Sofa_Old", new Vector3(-9.25f + WSHIFT, FLR, -2.7f), 90f);
            Box("Sofa_Cushion", g, new Vector3(-9.05f + WSHIFT, FLR + 0.52f, -3.3f), new Vector3(0.5f, 0.14f, 0.5f), "ToolRed", new Vector3(0f, 12f, 0f), false);

            // low table with the usual garage clutter
            var t = Group("LowTable", g);
            t.localPosition = new Vector3(-7.9f + WSHIFT, FLR, -2.7f);
            Box("Top", t, new Vector3(0f, 0.44f, 0f), new Vector3(0.7f, 0.04f, 1.2f), "Wood");
            for (int i = 0; i < 4; i++)
                Box("Leg_" + i, t, new Vector3(i < 2 ? -0.3f : 0.3f, 0.22f, i % 2 == 0 ? -0.5f : 0.5f), new Vector3(0.05f, 0.44f, 0.05f), "SteelDark", default(Vector3), false);
            Cyl("Mug_A", t, new Vector3(-0.12f, 0.51f, 0.2f), 0.09f, 0.1f, "White");
            Cyl("Mug_B", t, new Vector3(0.14f, 0.51f, -0.15f), 0.09f, 0.1f, "ToolRed");
            Box("Ashtray", t, new Vector3(0.02f, 0.47f, -0.42f), new Vector3(0.16f, 0.04f, 0.16f), "Glass", default(Vector3), false);
            Box("Magazine", t, new Vector3(-0.05f, 0.47f, 0.5f), new Vector3(0.24f, 0.02f, 0.3f), "Paper", new Vector3(0f, 14f, 0f), false);

            // fridge, fan, radio – the character of the place
            var f = Group("MiniFridge", g);
            f.localPosition = new Vector3(-9.4f + WSHIFT, FLR, -0.9f);
            Box("Body", f, new Vector3(0f, 0.45f, 0f), new Vector3(0.55f, 0.9f, 0.55f), "OffWhite");
            Box("Door", f, new Vector3(0.29f, 0.45f, 0f), new Vector3(0.04f, 0.86f, 0.52f), "White", default(Vector3), false);
            Box("Handle", f, new Vector3(0.33f, 0.45f, -0.2f), new Vector3(0.03f, 0.3f, 0.03f), "Alu", default(Vector3), false);
            Box("Radio", f, new Vector3(0f, 1.02f, 0f), new Vector3(0.42f, 0.24f, 0.22f), "PanelBlack", default(Vector3), false);
            Box("Radio_Face", f, new Vector3(0.12f, 1.02f, 0f), new Vector3(0.18f, 0.12f, 0.02f), "ScreenBlue", new Vector3(0f, 90f, 0f), false);
            Marker("INTERACT_Fridge", f, new Vector3(0.6f, 0f, 0f));

            var fan = Group("StandFan", g);
            fan.localPosition = new Vector3(-6.9f + WSHIFT, FLR, -4.0f);
            Box("Base", fan, new Vector3(0f, 0.03f, 0f), new Vector3(0.42f, 0.06f, 0.42f), "SteelDark", default(Vector3), false);
            Cyl("Pole", fan, new Vector3(0f, 0.6f, 0f), 0.05f, 1.2f, "OffWhite");
            Cyl("Cage", fan, new Vector3(0f, 1.28f, 0.06f), 0.5f, 0.14f, "Alu", new Vector3(90f, 0f, 0f));
            Cyl("Blade", fan, new Vector3(0f, 1.28f, 0.06f), 0.4f, 0.05f, "White", new Vector3(90f, 0f, 0f));

            // wall dressing
            Box("Calendar", g, new Vector3(X0 + 0.32f, 2.1f, -1.5f), new Vector3(0.03f, 0.6f, 0.44f), "Paper", default(Vector3), false);
            Box("Poster", g, new Vector3(X0 + 0.32f, 2.2f, -2.7f), new Vector3(0.03f, 0.9f, 1.3f), "BrandRed", default(Vector3), false);
            Sign("Poster_Text", g, new Vector3(X0 + 0.35f, 2.2f, -2.7f), new Vector2(1.1f, 0.5f), "RACE TOOL", Color.white, 90f);
            for (int i = 0; i < 4; i++)
                Box("Trophy_" + i, g, new Vector3(X0 + 0.35f, 1.5f, -5.4f + i * 0.3f), new Vector3(0.14f, 0.3f + (i % 2) * 0.1f, 0.14f), "Copper", default(Vector3), false);
            Box("TrophyShelf", g, new Vector3(X0 + 0.4f, 1.32f, -5.1f), new Vector3(0.34f, 0.04f, 1.6f), "Wood", default(Vector3), false);

            Marker("NAV_Hangout", g, new Vector3(-8.2f + WSHIFT, FLR, -2.7f));
            Marker("SIT_Sofa", g, new Vector3(-9.0f + WSHIFT, FLR + 0.45f, -2.7f));
        }
    }
}
