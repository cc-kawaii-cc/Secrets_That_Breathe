using UnityEditor;
using UnityEngine;

namespace SecretsThatBreathe.LevelTools
{
    // Building envelope + street facade (the "RACE TOOL" front elevation).
    public static partial class Ch2GarageBuilder
    {
        // facade module lines (X), left -> right, total 20.25 m incl. wall thickness
        const float F_BLACK_L = X0 - WT * 0.5f;   // -10.125
        const float F_BLACK_R = -5.0f;
        const float F_PIL_A_L = -5.0f;
        const float F_PIL_A_R = -4.2f;
        const float F_BAY1_L = -4.2f;
        const float F_BAY1_R = -1.0f;
        const float F_MUL_L = -1.0f;
        const float F_MUL_R = -0.2f;
        const float F_BAY2_L = -0.2f;
        const float F_BAY2_R = 3.0f;
        const float F_PIL_B_L = 3.0f;
        const float F_PIL_B_R = PARTX;            // 3.8
        const float F_OFF_L = PARTX;
        const float F_OFF_R = X1 + WT * 0.5f;     // 10.125

        const float FASCIA_Y = 5.85f;             // centre of the red top band
        const float FASCIA_H = 1.10f;
        const float FASCIA_Z = Z0 - 0.30f;
        const float FACE_Z = Z0 - WT * 0.5f;      // outer face of the front wall (-7.125)

        /// <summary>Centre of the personnel door in the left wall.</summary>
        public const float SIDE_DOOR_Z = 4.1f;
        /// <summary>Rear roller shutter opening, x -1.5 .. 2.5.</summary>
        public const float REAR_DOOR_CX = 0.5f, REAR_DOOR_W = 4f;

        static void BuildShell()
        {
            var g = Group("BUILD_Shell", _struct);
            float xL = X0 - WT * 0.5f, xR = X1 + WT * 0.5f;
            float zF = Z0 - WT * 0.5f, zB = Z1 + WT * 0.5f;

            // ---- back wall (rear roller shutter 4.0 m wide at x -1.5..2.5) ----
            var back = Group("Wall_Back", g);
            float bl = (REAR_DOOR_CX - REAR_DOOR_W * 0.5f) - xL;
            float br = xR - (REAR_DOOR_CX + REAR_DOOR_W * 0.5f);
            Box("Back_L", back, new Vector3(xL + bl * 0.5f, BH * 0.5f, Z1), new Vector3(bl, BH, WT), "PanelDark");
            Box("Back_R", back, new Vector3(xR - br * 0.5f, BH * 0.5f, Z1), new Vector3(br, BH, WT), "PanelDark");
            Box("Back_Header", back, new Vector3(REAR_DOOR_CX, (DOOR_H + BH) * 0.5f, Z1),
                new Vector3(REAR_DOOR_W, BH - DOOR_H, WT), "PanelDark");

            // ---- left wall: personnel door sized off the player capsule, not eyeballed ----
            // yaw 90 maps local +X onto world -Z, hence the sign on the opening centre
            LevelKit.WallWithOpening("Wall_Left", g, new Vector3(X0, 0f, 0f), zB - zF, BH, WT, "PanelDark",
                                     -SIDE_DOOR_Z, LevelKit.Nav.DoorClear, LevelKit.Nav.DoorHeight, 90f);

            // ---- right wall (solid, office side) ----
            Box("Wall_Right", g, new Vector3(X1, BH * 0.5f, 0f), new Vector3(WT, BH, BD), "PanelDark");

            // ---- concrete plinth around the base (real workshops always have one) ----
            var pl = Group("Plinth", g);
            Box("Plinth_Left", pl, new Vector3(X0 - 0.06f, 0.45f, 0f), new Vector3(0.16f, 0.9f, BD + 0.3f), "ConcreteDark", default(Vector3), false);
            Box("Plinth_Right", pl, new Vector3(X1 + 0.06f, 0.45f, 0f), new Vector3(0.16f, 0.9f, BD + 0.3f), "ConcreteDark", default(Vector3), false);
            Box("Plinth_Back", pl, new Vector3(CX, 0.45f, Z1 + 0.06f), new Vector3(BW + 0.3f, 0.9f, 0.16f), "ConcreteDark", default(Vector3), false);

            // ---- roof slab ----
            Box("Roof_Slab", g, new Vector3(CX, BH + 0.15f, 0f), new Vector3(BW + 0.7f, 0.3f, BD + 0.7f), "PanelDark");

            // ---- parapet / fascia band ----
            var fas = Group("Parapet", g);
            Box("Fascia_Front_Red", fas, new Vector3(CX, FASCIA_Y, FASCIA_Z), new Vector3(BW + 0.9f, FASCIA_H, 0.4f), "BrandRed", default(Vector3), false);
            Box("Fascia_Front_Trim", fas, new Vector3(CX, FASCIA_Y - FASCIA_H * 0.5f - 0.06f, FASCIA_Z - 0.02f),
                new Vector3(BW + 0.9f, 0.12f, 0.42f), "PanelBlack", default(Vector3), false);
            // red returns down both sides for 4 m, black for the rest
            Box("Fascia_L_Red", fas, new Vector3(X0 - 0.32f, FASCIA_Y, Z0 + 2.1f), new Vector3(0.4f, FASCIA_H, 4.2f), "BrandRed", default(Vector3), false);
            Box("Fascia_R_Red", fas, new Vector3(X1 + 0.32f, FASCIA_Y, Z0 + 2.1f), new Vector3(0.4f, FASCIA_H, 4.2f), "BrandRed", default(Vector3), false);
            Box("Fascia_L_Blk", fas, new Vector3(X0 - 0.32f, FASCIA_Y, 2.275f), new Vector3(0.4f, FASCIA_H, 10.15f), "PanelBlack", default(Vector3), false);
            Box("Fascia_R_Blk", fas, new Vector3(X1 + 0.32f, FASCIA_Y, 2.275f), new Vector3(0.4f, FASCIA_H, 10.15f), "PanelBlack", default(Vector3), false);
            Box("Fascia_Back_Blk", fas, new Vector3(CX, FASCIA_Y, Z1 + 0.32f), new Vector3(BW + 0.9f, FASCIA_H, 0.4f), "PanelBlack", default(Vector3), false);

            BuildRoofDetails(g);
            BuildWallLiners(g);
        }

        static void BuildRoofDetails(Transform parent)
        {
            var g = Group("Roof_Details", parent);
            float y = BH + 0.3f;   // roof top surface

            // packaged AC / ventilation units
            for (int i = 0; i < 2; i++)
            {
                float x = -4f + i * 6.5f;
                var unit = Group("RTU_" + (i + 1), g);
                Box("Curb", unit, new Vector3(x, y + 0.12f, 3.2f), new Vector3(2.4f, 0.24f, 1.8f), "ConcreteDark", default(Vector3), false);
                Box("Body", unit, new Vector3(x, y + 0.75f, 3.2f), new Vector3(2.2f, 1.05f, 1.6f), "Steel", default(Vector3), false);
                Box("Grille", unit, new Vector3(x, y + 0.75f, 2.42f), new Vector3(1.7f, 0.7f, 0.06f), "SteelDark", default(Vector3), false);
                Cyl("Fan", unit, new Vector3(x, y + 1.32f, 3.2f), 0.85f, 0.16f, "SteelDark");
            }

            // roof vents
            for (int i = 0; i < 3; i++)
            {
                float x = -8f + i * 5.5f;
                Cyl("Vent_" + i, g, new Vector3(x, y + 0.35f, -2.5f), 0.55f, 0.7f, "Alu");
                Cyl("VentCap_" + i, g, new Vector3(x, y + 0.75f, -2.5f), 0.75f, 0.1f, "Alu");
            }

            // rear maintenance ladder
            var lad = Group("Roof_Ladder", g);
            Box("Rail_L", lad, new Vector3(-1.2f, 3.1f, Z1 + 0.36f), new Vector3(0.06f, 6.2f, 0.06f), "SteelDark", default(Vector3), false);
            Box("Rail_R", lad, new Vector3(-0.6f, 3.1f, Z1 + 0.36f), new Vector3(0.06f, 6.2f, 0.06f), "SteelDark", default(Vector3), false);
            for (int i = 0; i < 19; i++)
                Box("Rung_" + i, lad, new Vector3(-0.9f, 0.6f + i * 0.3f, Z1 + 0.36f), new Vector3(0.62f, 0.04f, 0.04f), "SteelDark", default(Vector3), false);
            Box("Cage", lad, new Vector3(-0.9f, 4.5f, Z1 + 0.52f), new Vector3(0.8f, 3f, 0.04f), "SteelDark", default(Vector3), false);
            Marker("ROOF_Access", g, new Vector3(-0.9f, y, Z1 - 0.8f));

            // parapet coping strip so the roof reads as a real flat roof
            Box("Coping_Back", g, new Vector3(CX, y + 0.08f, Z1 + 0.2f), new Vector3(BW + 0.7f, 0.16f, 0.3f), "Alu", default(Vector3), false);
            Box("Coping_L", g, new Vector3(X0 - 0.2f, y + 0.08f, 0f), new Vector3(0.3f, 0.16f, BD + 0.7f), "Alu", default(Vector3), false);
            Box("Coping_R", g, new Vector3(X1 + 0.2f, y + 0.08f, 0f), new Vector3(0.3f, 0.16f, BD + 0.7f), "Alu", default(Vector3), false);
        }

        static void BuildWallLiners(Transform parent)
        {
            var g = Group("Wall_Liners", parent);
            // white sandwich-panel lining of the workshop
            Box("Liner_Left", g, new Vector3(X0 + WT * 0.5f + 0.02f, 3f, 0f), new Vector3(0.04f, 5.4f, BD - WT), "OffWhite", default(Vector3), false);
            float lbW = (REAR_DOOR_CX - REAR_DOOR_W * 0.5f) - (X0 + WT * 0.5f);
            float rbW = (X1 - WT * 0.5f) - (REAR_DOOR_CX + REAR_DOOR_W * 0.5f);
            Box("Liner_Back_L", g, new Vector3(X0 + WT * 0.5f + lbW * 0.5f, 3f, Z1 - WT * 0.5f - 0.02f), new Vector3(lbW, 5.4f, 0.04f), "OffWhite", default(Vector3), false);
            Box("Liner_Back_R", g, new Vector3(X1 - WT * 0.5f - rbW * 0.5f, 3f, Z1 - WT * 0.5f - 0.02f), new Vector3(rbW, 5.4f, 0.04f), "OffWhite", default(Vector3), false);
            Box("Liner_Back_Head", g, new Vector3(0.5f, 4.95f, Z1 - WT * 0.5f - 0.02f), new Vector3(4f, 1.5f, 0.04f), "OffWhite", default(Vector3), false);
            Box("Liner_Right", g, new Vector3(X1 - WT * 0.5f - 0.02f, 3f, 3.5f), new Vector3(0.04f, 5.4f, 7f), "OffWhite", default(Vector3), false);
            // grease skirt (dark lower band) – classic workshop detail
            Box("Skirt_Left", g, new Vector3(X0 + WT * 0.5f + 0.05f, 0.6f, 0f), new Vector3(0.04f, 1.2f, BD - WT), "SteelDark", default(Vector3), false);
            Box("Skirt_Back_L", g, new Vector3(X0 + WT * 0.5f + lbW * 0.5f, 0.6f, Z1 - WT * 0.5f - 0.05f), new Vector3(lbW, 1.2f, 0.04f), "SteelDark", default(Vector3), false);
            Box("Skirt_Back_R", g, new Vector3(X1 - WT * 0.5f - rbW * 0.5f, 0.6f, Z1 - WT * 0.5f - 0.05f), new Vector3(rbW, 1.2f, 0.04f), "SteelDark", default(Vector3), false);
        }

        // ───────────────────────── street facade ─────────────────────────
        static void BuildFacade()
        {
            var g = Group("BUILD_Facade", _struct);

            // 1. black cladding panel with the RT logo
            float bw = F_BLACK_R - F_BLACK_L;
            Box("Panel_Black", g, new Vector3((F_BLACK_L + F_BLACK_R) * 0.5f, BH * 0.5f, Z0), new Vector3(bw, BH, WT), "PanelBlack");
            // horizontal seams of the cladding
            for (int i = 1; i < 5; i++)
                Box("Seam_" + i, g, new Vector3((F_BLACK_L + F_BLACK_R) * 0.5f, i * 1.2f, FACE_Z - 0.01f),
                    new Vector3(bw, 0.03f, 0.02f), "PanelDark", default(Vector3), false);

            BuildRTLogo(g);

            // 2. red pilasters / mullion
            Box("Pilaster_A", g, new Vector3((F_PIL_A_L + F_PIL_A_R) * 0.5f, BH * 0.5f, Z0 - 0.2f), new Vector3(0.8f, BH, 0.7f), "BrandRed");
            Box("Mullion_Mid", g, new Vector3((F_MUL_L + F_MUL_R) * 0.5f, BH * 0.5f, Z0 - 0.2f), new Vector3(0.8f, BH, 0.7f), "BrandRed");
            Box("Pilaster_B", g, new Vector3((F_PIL_B_L + F_PIL_B_R) * 0.5f, BH * 0.5f, Z0 - 0.2f), new Vector3(0.8f, BH, 0.7f), "BrandRed");

            // 3. the two service bays
            // Bay 1 is rolled up: it is how the player walks in off the forecourt.
            // Bay 2 stays shut, and is the only leaf in the building that still blocks.
            BuildBayDoor(_circ, "BayDoor_1", F_BAY1_L, F_BAY1_R, true);
            BuildBayDoor(g, "BayDoor_2", F_BAY2_L, F_BAY2_R, false);

            // 4. office curtain wall
            BuildOfficeFacade(g);

            // 5. signage on the red band
            var sg = Group("Signage", g);
            Sign("Sign_RACETOOL", sg, new Vector3(4.6f, FASCIA_Y + 0.12f, FASCIA_Z - 0.21f), new Vector2(9.2f, 0.72f), "RACETOOL", Color.white);
            Sign("Sign_Sub", sg, new Vector3(4.6f, FASCIA_Y - 0.34f, FASCIA_Z - 0.21f), new Vector2(6.6f, 0.26f),
                "AUTO SERVICE  ·  PERFORMANCE", Color.white, 180f, false);
            Box("Sign_Underline", sg, new Vector3(4.6f, FASCIA_Y - 0.22f, FASCIA_Z - 0.21f), new Vector3(9.2f, 0.03f, 0.02f), "White", default(Vector3), false);

            // small entrance sign over the office door
            Sign("Sign_Reception", sg, new Vector3(6.45f, 4.95f, FACE_Z - 0.03f), new Vector2(2.4f, 0.3f), "RECEPTION", Color.white);

            // bay numbers painted over each door
            Sign("Sign_Bay1", sg, new Vector3((F_BAY1_L + F_BAY1_R) * 0.5f, 4.72f, FACE_Z - 0.02f), new Vector2(1.4f, 0.42f), "BAY 01", new Color(0.85f, 0.85f, 0.85f));
            Sign("Sign_Bay2", sg, new Vector3((F_BAY2_L + F_BAY2_R) * 0.5f, 4.72f, FACE_Z - 0.02f), new Vector2(1.4f, 0.42f), "BAY 02", new Color(0.85f, 0.85f, 0.85f));

            // facade wall packs
            for (int i = 0; i < 3; i++)
            {
                float x = -6.6f + i * 6.0f;
                WallPack(g, "WallPack_F" + i, new Vector3(x, 5.0f, FACE_Z - 0.12f), 0f);
            }
        }

        static void BuildRTLogo(Transform parent)
        {
            var g = Group("Logo_RT", parent);
            float cx = (F_BLACK_L + F_BLACK_R) * 0.5f;
            float cy = 3.5f;
            Cyl("Ring_Red", g, new Vector3(cx, cy, FACE_Z - 0.06f), 3.2f, 0.12f, "BrandRed", new Vector3(90f, 0f, 0f));
            Cyl("Ring_Inner", g, new Vector3(cx, cy, FACE_Z - 0.13f), 2.62f, 0.1f, "PanelBlack", new Vector3(90f, 0f, 0f));
            Sign("Logo_Text", g, new Vector3(cx, cy, FACE_Z - 0.20f), new Vector2(2.1f, 1.25f), "RT", Color.white);
        }

        /// <summary>Sectional glass overhead door + its opening. An open door parks its leaf overhead.</summary>
        static void BuildBayDoor(Transform parent, string name, float xl, float xr, bool open)
        {
            var g = Group(name, parent);
            float w = xr - xl;
            float cx = (xl + xr) * 0.5f;

            // spandrel above the opening
            Box("Spandrel", g, new Vector3(cx, (DOOR_H + BH) * 0.5f, Z0), new Vector3(w, BH - DOOR_H, WT), "PanelBlack");
            // red lintel
            Box("Lintel_Red", g, new Vector3(cx, DOOR_H + 0.16f, Z0 - 0.22f), new Vector3(w + 0.8f, 0.32f, 0.74f), "BrandRed", default(Vector3), false);
            // steel jambs
            Box("Jamb_L", g, new Vector3(xl + 0.05f, DOOR_H * 0.5f, Z0 - 0.02f), new Vector3(0.1f, DOOR_H, 0.34f), "Alu", default(Vector3), false);
            Box("Jamb_R", g, new Vector3(xr - 0.05f, DOOR_H * 0.5f, Z0 - 0.02f), new Vector3(0.1f, DOOR_H, 0.34f), "Alu", default(Vector3), false);

            // 4 panel sectional leaf
            var leaf = Group("Leaf", g);
            for (int i = 0; i < 4; i++)
            {
                if (open)
                {
                    // rolled back along the ceiling track, clear of the opening entirely
                    float pz = Z0 + 0.75f + i * 1.06f;
                    Box("Panel_" + i, leaf, new Vector3(cx, DOOR_H + 0.42f, pz), new Vector3(w - 0.12f, 0.09f, 1.0f), "Alu", default(Vector3), false);
                    if (i > 0)
                        Box("Glass_" + i, leaf, new Vector3(cx, DOOR_H + 0.46f, pz), new Vector3(w - 0.42f, 0.03f, 0.72f), "Glass", default(Vector3), false);
                    continue;
                }
                float py = 0.53f + i * 1.04f;
                Box("Panel_" + i, leaf, new Vector3(cx, py, Z0 + 0.14f), new Vector3(w - 0.12f, 1.0f, 0.09f), "Alu", default(Vector3), i == 0);
                if (i > 0)
                    Box("Glass_" + i, leaf, new Vector3(cx, py, Z0 + 0.10f), new Vector3(w - 0.42f, 0.72f, 0.03f), "Glass", default(Vector3), false);
                else
                    for (int k = 0; k < 4; k++)
                        Box("Rib_" + k, leaf, new Vector3(cx - w * 0.5f + 0.35f + k * (w - 0.7f) / 3f, py, Z0 + 0.08f),
                            new Vector3(0.06f, 0.82f, 0.03f), "SteelDark", default(Vector3), false);
            }
            // tracks + torsion shaft (visible inside)
            Box("Track_L", g, new Vector3(xl + 0.16f, 2.6f, Z0 + 0.55f), new Vector3(0.08f, 4.6f, 0.08f), "Steel", default(Vector3), false);
            Box("Track_R", g, new Vector3(xr - 0.16f, 2.6f, Z0 + 0.55f), new Vector3(0.08f, 4.6f, 0.08f), "Steel", default(Vector3), false);
            Cyl("TorsionShaft", g, new Vector3(cx, DOOR_H + 0.22f, Z0 + 0.5f), 0.09f, w, "Steel", new Vector3(0f, 0f, 90f));
            Box("Opener", g, new Vector3(cx, 4.9f, Z0 + 2.4f), new Vector3(0.34f, 0.26f, 0.5f), "SteelDark", default(Vector3), false);

            // rubber threshold + floor guides
            Box("Threshold", g, new Vector3(cx, 0.055f, Z0 + 0.14f), new Vector3(w, 0.03f, 0.22f), "Rubber", default(Vector3), false);
            // a door that is locked by design is marked SHUT_ so the audit does not fail it
            Marker((open ? "DOOR_" : "SHUT_") + name, g, new Vector3(cx, 0f, Z0));
        }

        static void BuildOfficeFacade(Transform parent)
        {
            var g = Group("Office_Facade", parent);
            float l = F_OFF_L, r = F_OFF_R;
            float w = r - l, cx = (l + r) * 0.5f;
            float glassTop = 4.6f;

            // sill + spandrel
            // a threshold, not a barrier: the capsule should walk over it, not into it
            Box("Sill", g, new Vector3(cx, 0.075f, Z0), new Vector3(w, 0.15f, WT), "ConcreteDark", default(Vector3), false);
            Box("Spandrel", g, new Vector3(cx, (glassTop + BH) * 0.5f, Z0), new Vector3(w, BH - glassTop, WT), "PanelBlack");

            // glazing (two storeys, the mezzanine office sits behind the upper half) – split around the entrance
            Box("Glazing_L", g, new Vector3(4.65f, 2.375f, Z0), new Vector3(1.7f, 4.45f, 0.05f), "GlassOffice", default(Vector3), false);
            Box("Glazing_R", g, new Vector3(8.71f, 2.375f, Z0), new Vector3(2.83f, 4.45f, 0.05f), "GlassOffice", default(Vector3), false);
            Box("Glazing_Transom", g, new Vector3(6.4f, 3.5f, Z0), new Vector3(1.8f, 2.2f, 0.05f), "GlassOffice", default(Vector3), false);

            // aluminium grid
            float[] mullions = { 5.5f, 7.3f, 8.75f };
            for (int i = 0; i < mullions.Length; i++)
                Box("Mullion_" + i, g, new Vector3(mullions[i], (0.15f + glassTop) * 0.5f, Z0 - 0.04f), new Vector3(0.1f, glassTop - 0.15f, 0.16f), "Alu", default(Vector3), false);
            Box("Transom_Lo", g, new Vector3(cx, 2.35f, Z0 - 0.04f), new Vector3(w, 0.1f, 0.16f), "Alu", default(Vector3), false);
            Box("Transom_Hi", g, new Vector3(cx, 3.05f, Z0 - 0.04f), new Vector3(w, 0.14f, 0.18f), "Alu", default(Vector3), false);

            // red frame around the whole glazed bay (matches the concept art)
            Box("Frame_Top", g, new Vector3(cx, glassTop + 0.13f, Z0 - 0.22f), new Vector3(w + 0.1f, 0.26f, 0.74f), "BrandRed", default(Vector3), false);
            Box("Frame_Right", g, new Vector3(r - 0.13f, (0.15f + glassTop) * 0.5f, Z0 - 0.22f), new Vector3(0.26f, glassTop, 0.74f), "BrandRed", default(Vector3), false);

            // entrance doors (double leaf, 1.8 m)
            var door = Group("Entrance", g);
            Box("Leaf_L", door, new Vector3(6.0f, 1.15f, Z0 - 0.06f), new Vector3(0.86f, 2.3f, 0.07f), "GlassDark", default(Vector3), false);
            Box("Leaf_R", door, new Vector3(6.9f, 1.15f, Z0 - 0.06f), new Vector3(0.86f, 2.3f, 0.07f), "GlassDark", default(Vector3), false);
            Box("DoorFrame", door, new Vector3(6.45f, 2.34f, Z0 - 0.06f), new Vector3(1.84f, 0.09f, 0.1f), "Alu", default(Vector3), false);
            Cyl("Handle_L", door, new Vector3(6.36f, 1.05f, Z0 - 0.13f), 0.045f, 0.9f, "Alu");
            Cyl("Handle_R", door, new Vector3(6.54f, 1.05f, Z0 - 0.13f), 0.045f, 0.9f, "Alu");
            Marker("DOOR_OfficeEntrance", door, new Vector3(6.45f, 0f, Z0));

            // entrance canopy
            Box("Canopy", g, new Vector3(6.45f, 3.32f, Z0 - 1.0f), new Vector3(3.6f, 0.18f, 1.9f), "BrandRed", default(Vector3), false);
            Box("Canopy_Edge", g, new Vector3(6.45f, 3.24f, Z0 - 1.9f), new Vector3(3.6f, 0.22f, 0.12f), "PanelBlack", default(Vector3), false);
            Box("Canopy_Strut_L", g, new Vector3(5.0f, 3.9f, Z0 - 0.6f), new Vector3(0.06f, 1.3f, 0.06f), "Alu", new Vector3(28f, 0f, 0f), false);
            Box("Canopy_Strut_R", g, new Vector3(7.9f, 3.9f, Z0 - 0.6f), new Vector3(0.06f, 1.3f, 0.06f), "Alu", new Vector3(28f, 0f, 0f), false);

            // step + entrance mat
            Box("Step", g, new Vector3(6.45f, 0.04f, Z0 - 0.75f), new Vector3(3.2f, 0.08f, 1.4f), "Concrete", default(Vector3), false);
            Decal("Mat", g, new Vector3(6.45f, 0.09f, Z0 - 0.7f), new Vector2(1.8f, 0.9f), "SteelDark");
        }

        /// <summary>Small exterior wall pack luminaire.</summary>
        static void WallPack(Transform parent, string name, Vector3 pos, float yaw)
        {
            var g = Group(name, parent);
            g.localPosition = pos;
            g.localEulerAngles = new Vector3(0f, yaw, 0f);
            Box("Body", g, Vector3.zero, new Vector3(0.34f, 0.2f, 0.26f), "SteelDark", default(Vector3), false);
            Box("Lens", g, new Vector3(0f, -0.09f, -0.02f), new Vector3(0.3f, 0.03f, 0.22f), "LampWhite", default(Vector3), false);
            var lg = new GameObject("Light");
            lg.transform.SetParent(g, false);
            lg.transform.localPosition = new Vector3(0f, -0.12f, 0f);
            lg.transform.localEulerAngles = new Vector3(72f, 0f, 0f);
            var l = lg.AddComponent<Light>();
            l.type = LightType.Spot;
            l.spotAngle = 110f;
            l.range = 12f;
            l.intensity = 2.2f;
            l.color = new Color(1f, 0.93f, 0.80f);
            l.shadows = LightShadows.None;
        }
    }
}
