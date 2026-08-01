using UnityEditor;
using UnityEngine;

namespace SecretsThatBreathe.LevelTools
{
    // Customer reception (ground) and the manager's mezzanine office (the objective room).
    public static partial class Ch2GarageBuilder
    {
        static void BuildOffice()
        {
            var g = Group("INT_Office", _root);
            BuildReception(Group("Reception", g));
            BuildMezzOffice(Group("Mezzanine_Office", g));
        }

        // ───────────────────────── reception ─────────────────────────
        static void BuildReception(Transform g)
        {
            // floor finish
            Box("Floor_Tile", g, new Vector3(6.96f, FLR + 0.012f, -2.55f), new Vector3(6.1f, 0.02f, 9.0f), "White", default(Vector3), false);

            // service counter facing the entrance
            var c = Group("Counter", g);
            Box("Base", c, new Vector3(6.1f, FLR + 0.5f, -3.6f), new Vector3(3.4f, 1.0f, 0.62f), "PanelDark");
            Box("Top", c, new Vector3(6.1f, FLR + 1.03f, -3.66f), new Vector3(3.6f, 0.06f, 0.78f), "Wood", default(Vector3), false);
            Box("Face_Red", c, new Vector3(6.1f, FLR + 0.5f, -3.92f), new Vector3(3.4f, 0.9f, 0.04f), "BrandRed", default(Vector3), false);
            Box("Return", c, new Vector3(4.55f, FLR + 0.5f, -2.85f), new Vector3(0.62f, 1.0f, 1.9f), "PanelDark");
            Box("Return_Top", c, new Vector3(4.5f, FLR + 1.03f, -2.85f), new Vector3(0.78f, 0.06f, 1.9f), "Wood", default(Vector3), false);
            Box("Monitor", c, new Vector3(6.6f, FLR + 1.28f, -3.4f), new Vector3(0.5f, 0.32f, 0.04f), "ScreenBlue", new Vector3(0f, 165f, 0f), false);
            Box("Keyboard", c, new Vector3(6.5f, FLR + 1.08f, -3.65f), new Vector3(0.42f, 0.03f, 0.16f), "PanelDark", new Vector3(0f, 165f, 0f), false);
            Box("Card_Terminal", c, new Vector3(5.3f, FLR + 1.12f, -3.6f), new Vector3(0.12f, 0.14f, 0.2f), "SteelDark", new Vector3(-20f, 0f, 0f), false);
            Box("Papers", c, new Vector3(5.7f, FLR + 1.07f, -3.55f), new Vector3(0.3f, 0.02f, 0.22f), "White", new Vector3(0f, 12f, 0f), false);
            Sign("JobBoard", c, new Vector3(6.1f, FLR + 0.62f, -3.95f), new Vector2(2.6f, 0.24f), "RACE TOOL  ·  SERVICE & PERFORMANCE", Color.white);

            // back bar with parts display
            Box("BackBar", g, new Vector3(6.4f, FLR + 0.45f, -1.75f), new Vector3(3.0f, 0.9f, 0.5f), "PanelDark");
            Box("BackBar_Top", g, new Vector3(6.4f, FLR + 0.92f, -1.75f), new Vector3(3.1f, 0.05f, 0.56f), "SteelDark", default(Vector3), false);
            for (int i = 0; i < 3; i++)
                Box("WallShelf_" + i, g, new Vector3(6.4f, 1.35f + i * 0.45f, -1.62f), new Vector3(2.9f, 0.04f, 0.28f), "Alu", default(Vector3), false);
            for (int i = 0; i < 9; i++)
                Box("Product_" + i, g, new Vector3(5.2f + (i % 3) * 1.15f, 1.5f + (i / 3) * 0.45f, -1.62f),
                    new Vector3(0.22f, 0.26f, 0.2f), i % 3 == 0 ? "ToolRed" : (i % 3 == 1 ? "Yellow" : "SafetyGreen"), default(Vector3), false);

            // waiting area along the right wall
            var w = Group("Waiting", g);
            Sofa(w, "Sofa", new Vector3(9.35f, FLR, -5.3f), -90f);
            Box("CoffeeTable", w, new Vector3(8.35f, FLR + 0.2f, -5.3f), new Vector3(0.6f, 0.05f, 1.1f), "Wood", default(Vector3), false);
            for (int i = 0; i < 4; i++)
                Box("TableLeg_" + i, w, new Vector3(8.35f + (i < 2 ? -0.24f : 0.24f), FLR + 0.1f, -5.3f + (i % 2 == 0 ? -0.45f : 0.45f)),
                    new Vector3(0.04f, 0.2f, 0.04f), "SteelDark", default(Vector3), false);
            Box("Magazines", w, new Vector3(8.35f, FLR + 0.24f, -5.5f), new Vector3(0.3f, 0.03f, 0.24f), "White", new Vector3(0f, 14f, 0f), false);
            Box("Bin", w, new Vector3(9.6f, FLR + 0.25f, -6.6f), new Vector3(0.3f, 0.5f, 0.3f), "SteelDark", default(Vector3), false);
            WaterCooler(w, "WaterCooler", new Vector3(9.55f, FLR, -3.0f));
            Box("Plant_Pot", w, new Vector3(4.35f, FLR + 0.22f, -6.4f), new Vector3(0.4f, 0.44f, 0.4f), "ConcreteDark", default(Vector3), false);
            for (int i = 0; i < 5; i++)
                Sphere("Leaf_" + i, w, new Vector3(4.35f + Mathf.Cos(i * 1.3f) * 0.22f, FLR + 0.6f + i * 0.12f, -6.4f + Mathf.Sin(i * 1.3f) * 0.22f), 0.34f, "SafetyGreen");

            // wall tv playing the shop showreel
            Box("TV", g, new Vector3(4.0f, 1.95f, -5.0f), new Vector3(0.06f, 0.62f, 1.1f), "PanelBlack", default(Vector3), false);
            Box("TV_Screen", g, new Vector3(4.06f, 1.95f, -5.0f), new Vector3(0.02f, 0.54f, 1.0f), "ScreenBlue", default(Vector3), false);

            // tyre display against the partition
            for (int i = 0; i < 3; i++)
            {
                Cyl("DisplayTyre_" + i, g, new Vector3(4.15f, 0.4f + i * 0.02f, -4.3f + i * 0.85f), 0.7f, 0.24f, "Rubber", new Vector3(0f, 0f, 90f));
                Box("DisplayStand_" + i, g, new Vector3(4.1f, 0.06f, -4.3f + i * 0.85f), new Vector3(0.3f, 0.12f, 0.5f), "SteelDark", default(Vector3), false);
            }

            // ceiling + wall finish
            Box("Wall_Finish_R", g, new Vector3(X1 - 0.2f, 1.5f, -2.5f), new Vector3(0.04f, 3f, 9.0f), "White", default(Vector3), false);
            Marker("NAV_Reception", g, new Vector3(6.9f, FLR, -5.6f));
        }

        static void Sofa(Transform parent, string name, Vector3 p, float yaw)
        {
            var g = Group(name, parent);
            g.localPosition = p;
            g.localEulerAngles = new Vector3(0f, yaw, 0f);
            Box("Base", g, new Vector3(0f, 0.2f, 0f), new Vector3(1.9f, 0.4f, 0.78f), "PanelDark");
            Box("Seat", g, new Vector3(0f, 0.44f, 0.03f), new Vector3(1.86f, 0.14f, 0.72f), "SteelDark", default(Vector3), false);
            Box("Back", g, new Vector3(0f, 0.65f, -0.32f), new Vector3(1.9f, 0.6f, 0.16f), "PanelDark", default(Vector3), false);
            Box("Arm_L", g, new Vector3(-0.95f, 0.5f, 0f), new Vector3(0.14f, 0.6f, 0.78f), "PanelDark", default(Vector3), false);
            Box("Arm_R", g, new Vector3(0.95f, 0.5f, 0f), new Vector3(0.14f, 0.6f, 0.78f), "PanelDark", default(Vector3), false);
        }

        static void WaterCooler(Transform parent, string name, Vector3 p)
        {
            var g = Group(name, parent);
            g.localPosition = p;
            Box("Body", g, new Vector3(0f, 0.55f, 0f), new Vector3(0.34f, 1.1f, 0.34f), "White");
            Cyl("Bottle", g, new Vector3(0f, 1.35f, 0f), 0.28f, 0.5f, "Glass");
            Box("Tap", g, new Vector3(0f, 0.9f, -0.19f), new Vector3(0.1f, 0.08f, 0.06f), "SteelDark", default(Vector3), false);
        }

        // ───────────────────────── mezzanine office ─────────────────────────
        static void BuildMezzOffice(Transform g)
        {
            float y = MEZZ;

            // carpet
            Box("Carpet", g, new Vector3(7.0f, y + 0.03f, -3.4f), new Vector3(5.9f, 0.02f, 6.6f), "PanelDark", default(Vector3), false);

            // manager's desk facing the shop-floor window
            var d = Group("Desk", g);
            Box("Top", d, new Vector3(7.6f, y + 0.74f, -3.6f), new Vector3(1.9f, 0.05f, 0.85f), "Wood");
            Box("Modesty", d, new Vector3(7.6f, y + 0.4f, -3.95f), new Vector3(1.86f, 0.62f, 0.04f), "PanelDark", default(Vector3), false);
            Box("Ped", d, new Vector3(8.35f, y + 0.35f, -3.6f), new Vector3(0.42f, 0.7f, 0.7f), "PanelDark", default(Vector3), false);
            Box("Leg", d, new Vector3(6.75f, y + 0.36f, -3.6f), new Vector3(0.06f, 0.72f, 0.72f), "SteelDark", default(Vector3), false);
            Box("Monitor", d, new Vector3(7.9f, y + 1.05f, -3.85f), new Vector3(0.62f, 0.38f, 0.04f), "ScreenBlue", new Vector3(0f, 8f, 0f), false);
            Box("Monitor_Stand", d, new Vector3(7.9f, y + 0.82f, -3.85f), new Vector3(0.16f, 0.18f, 0.16f), "SteelDark", default(Vector3), false);
            Box("Keyboard", d, new Vector3(7.75f, y + 0.78f, -3.5f), new Vector3(0.44f, 0.03f, 0.16f), "PanelBlack", default(Vector3), false);
            Box("Ledger", d, new Vector3(7.0f, y + 0.79f, -3.45f), new Vector3(0.32f, 0.04f, 0.24f), "ToolRed", new Vector3(0f, 18f, 0f), false);
            Box("Phone", d, new Vector3(6.9f, y + 0.8f, -3.9f), new Vector3(0.16f, 0.08f, 0.22f), "PanelBlack", default(Vector3), false);
            Box("Ashtray", d, new Vector3(8.2f, y + 0.79f, -3.3f), new Vector3(0.16f, 0.04f, 0.16f), "Glass", default(Vector3), false);
            Marker("EVIDENCE_Point_Ledger", d, new Vector3(7.0f, y + 0.8f, -3.45f));
            OfficeChair(g, "Chair_Manager", new Vector3(7.6f, y, -2.75f), 180f);
            OfficeChair(g, "Chair_Guest", new Vector3(6.4f, y, -5.0f), 20f);

            // parts-order workstation: the friend digs through supplier records from here
            var s = Group("RecordsStation", g);
            Box("Bench", s, new Vector3(9.3f, y + 0.72f, -1.0f), new Vector3(0.75f, 0.05f, 2.4f), "Wood");
            Box("Bench_Leg", s, new Vector3(9.3f, y + 0.36f, -0.1f), new Vector3(0.6f, 0.7f, 0.06f), "SteelDark", default(Vector3), false);
            for (int i = 0; i < 2; i++)
            {
                float mz = -1.6f + i * 0.85f;
                Box("Mon_" + i, s, new Vector3(9.72f, y + 1.05f, mz), new Vector3(0.04f, 0.46f, 0.78f), "PanelBlack", default(Vector3), false);
                Box("Feed_" + i, s, new Vector3(9.68f, y + 1.05f, mz), new Vector3(0.02f, 0.4f, 0.72f), "ScreenBlue", default(Vector3), false);
                Box("Stand_" + i, s, new Vector3(9.68f, y + 0.8f, mz), new Vector3(0.14f, 0.2f, 0.18f), "PanelBlack", default(Vector3), false);
            }
            Box("Tower", s, new Vector3(9.3f, y + 0.28f, -1.9f), new Vector3(0.24f, 0.46f, 0.5f), "SteelDark", default(Vector3), false);
            Box("Tower_LED", s, new Vector3(9.3f, y + 0.44f, -2.16f), new Vector3(0.03f, 0.03f, 0.02f), "LampGreen", default(Vector3), false);
            Box("Keyboard", s, new Vector3(9.28f, y + 0.76f, -1.2f), new Vector3(0.18f, 0.03f, 0.44f), "PanelBlack", default(Vector3), false);
            Box("InvoiceSpike", s, new Vector3(9.2f, y + 0.82f, -0.2f), new Vector3(0.14f, 0.16f, 0.14f), "Paper", new Vector3(0f, 12f, 0f), false);
            Box("Mug", s, new Vector3(9.45f, y + 0.8f, -0.5f), new Vector3(0.1f, 0.12f, 0.1f), "ToolRed", default(Vector3), false);
            Marker("INTERACT_OrderRecords_PC", s, new Vector3(8.7f, y, -1.2f));

            // archive of supplier invoices – where the red bumper order finally turns up
            var sf = Group("OrderArchive", g);
            Box("Cabinet", sf, new Vector3(9.4f, y + 0.6f, 1.35f), new Vector3(0.7f, 1.2f, 0.9f), "SteelDark");
            for (int i = 0; i < 3; i++)
            {
                Box("Shelf_" + i, sf, new Vector3(9.4f, y + 0.28f + i * 0.38f, 1.35f), new Vector3(0.66f, 0.03f, 0.86f), "Steel", default(Vector3), false);
                for (int b = 0; b < 3; b++)
                    Box("ArchiveBox_" + i + "_" + b, sf, new Vector3(9.4f, y + 0.46f + i * 0.38f, 1.0f + b * 0.32f),
                        new Vector3(0.6f, 0.3f, 0.28f), b == 1 && i == 1 ? "ToolRed" : "Cardboard", default(Vector3), false);
            }
            Box("Label_Year", sf, new Vector3(9.05f, y + 0.84f, 1.32f), new Vector3(0.02f, 0.1f, 0.24f), "Paper", default(Vector3), false);
            Marker("INTERACT_OrderArchive", sf, new Vector3(8.8f, y, 1.35f));

            // framed photo of the two of them – why Kem trusts this place
            Box("Photo_Frame", g, new Vector3(PARTX + 0.12f, y + 1.35f, -4.6f), new Vector3(0.04f, 0.34f, 0.46f), "Wood", default(Vector3), false);
            Box("Photo_Print", g, new Vector3(PARTX + 0.15f, y + 1.35f, -4.6f), new Vector3(0.02f, 0.28f, 0.4f), "Paper", default(Vector3), false);

            // filing cabinets + storage
            for (int i = 0; i < 3; i++)
            {
                var fc = Group("FilingCabinet_" + i, g);
                Box("Body", fc, new Vector3(5.0f + i * 0.85f, y + 0.66f, 1.5f), new Vector3(0.8f, 1.32f, 0.55f), "SteelDark");
                for (int k = 0; k < 4; k++)
                {
                    Box("Drawer_" + k, fc, new Vector3(5.0f + i * 0.85f, y + 0.25f + k * 0.32f, 1.22f), new Vector3(0.74f, 0.28f, 0.03f), "Steel", default(Vector3), false);
                    Box("Pull_" + k, fc, new Vector3(5.0f + i * 0.85f, y + 0.25f + k * 0.32f, 1.19f), new Vector3(0.2f, 0.03f, 0.03f), "Alu", default(Vector3), false);
                }
                Box("Files", fc, new Vector3(5.0f + i * 0.85f, y + 1.4f, 1.5f), new Vector3(0.5f, 0.14f, 0.3f), "Cardboard", new Vector3(0f, i * 9f, 0f), false);
            }

            // wall board with photos / notes (story dressing)
            Box("Board", g, new Vector3(PARTX + 0.12f, y + 1.7f, -6.3f), new Vector3(0.05f, 1.1f, 1.7f), "Cardboard", default(Vector3), false);
            for (int i = 0; i < 8; i++)
                Box("Note_" + i, g, new Vector3(PARTX + 0.16f, y + 1.3f + (i % 3) * 0.36f, -6.9f + (i / 3) * 0.52f),
                    new Vector3(0.02f, 0.22f, 0.3f), i % 4 == 0 ? "ToolRed" : "White", default(Vector3), false);

            // meeting corner
            Cyl("SideTable", g, new Vector3(5.4f, y + 0.35f, -5.6f), 0.7f, 0.7f, "Wood", default(Vector3), true);
            Box("Whisky", g, new Vector3(5.4f, y + 0.85f, -5.6f), new Vector3(0.12f, 0.3f, 0.12f), "Glass", default(Vector3), false);
            Box("Glass_1", g, new Vector3(5.62f, y + 0.77f, -5.45f), new Vector3(0.08f, 0.14f, 0.08f), "Glass", default(Vector3), false);

            Marker("NAV_MezzOffice", g, new Vector3(7.0f, y, -2.0f));
        }

        static void OfficeChair(Transform parent, string name, Vector3 p, float yaw)
        {
            var g = Group(name, parent);
            g.localPosition = p;
            g.localEulerAngles = new Vector3(0f, yaw, 0f);
            for (int i = 0; i < 5; i++)
            {
                float a = i * Mathf.PI * 2f / 5f;
                Box("Star_" + i, g, new Vector3(Mathf.Cos(a) * 0.16f, 0.06f, Mathf.Sin(a) * 0.16f), new Vector3(0.06f, 0.05f, 0.34f),
                    "PanelBlack", new Vector3(0f, -a * Mathf.Rad2Deg, 0f), false);
                Cyl("Caster_" + i, g, new Vector3(Mathf.Cos(a) * 0.3f, 0.035f, Mathf.Sin(a) * 0.3f), 0.07f, 0.04f, "PanelBlack", new Vector3(90f, 0f, 0f));
            }
            Cyl("Gas", g, new Vector3(0f, 0.26f, 0f), 0.07f, 0.36f, "Alu");
            Box("Seat", g, new Vector3(0f, 0.47f, 0f), new Vector3(0.5f, 0.1f, 0.5f), "PanelBlack", default(Vector3), false);
            Box("Back", g, new Vector3(0f, 0.78f, 0.24f), new Vector3(0.48f, 0.55f, 0.09f), "PanelBlack", new Vector3(-9f, 0f, 0f), false);
            Box("Arm_L", g, new Vector3(-0.27f, 0.62f, 0.05f), new Vector3(0.05f, 0.2f, 0.34f), "PanelBlack", default(Vector3), false);
            Box("Arm_R", g, new Vector3(0.27f, 0.62f, 0.05f), new Vector3(0.05f, 0.2f, 0.34f), "PanelBlack", default(Vector3), false);
        }
    }
}
