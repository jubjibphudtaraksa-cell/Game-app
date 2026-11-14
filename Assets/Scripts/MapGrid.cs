using System.Collections.Generic;
using UnityEngine;

// แผนที่ "Iron Harbor": 3 เลน + 2 ไซต์ (A/B) สร้างจาก grid อัตโนมัติ
public static class MapGrid {
    public const int W = 40, H = 32; public const float Cell = 2.5f, WallH = 6f;
    static bool[,] wall = new bool[W, H], crate = new bool[W, H];

    public static Vector3 Center(int x, int y) { return new Vector3((x - W / 2f + .5f) * Cell, 0f, (H / 2f - y - .5f) * Cell); }
    public static Vector2Int ToCell(Vector3 p) {
        return new Vector2Int(Mathf.Clamp(Mathf.FloorToInt(p.x / Cell + W / 2f), 0, W - 1), Mathf.Clamp(Mathf.FloorToInt(H / 2f - p.z / Cell), 0, H - 1));
    }
    static bool Walkable(int x, int y) { return x >= 0 && y >= 0 && x < W && y < H && !wall[x, y] && !crate[x, y]; }

    static void Carve(int x0, int y0, int x1, int y1) { for (int x = x0; x <= x1; x++) for (int y = y0; y <= y1; y++) wall[x, y] = false; }
    static void Fillw(int x0, int y0, int x1, int y1) { for (int x = x0; x <= x1; x++) for (int y = y0; y <= y1; y++) wall[x, y] = true; }
    static void Crate(int x, int y) { crate[x, y] = true; crate[W - 1 - x, y] = true; }
    static void Pillar(int x0, int y0, int x1, int y1) { Fillw(x0, y0, x1, y1); Fillw(W - 1 - x1, y0, W - 1 - x0, y1); }

    static void Layout() {
        for (int x = 0; x < W; x++) for (int y = 0; y < H; y++) { wall[x, y] = true; crate[x, y] = false; }
        Carve(15, 1, 24, 4);                    // Defender spawn
        Carve(2, 2, 11, 9); Carve(28, 2, 37, 9);  // Site A / B
        Carve(11, 3, 15, 4); Carve(24, 3, 28, 4); // spawn -> sites
        Carve(17, 5, 22, 26);                   // Mid lane
        Carve(2, 10, 7, 25); Carve(32, 10, 37, 25); // Left / Right lanes
        Carve(12, 26, 27, 30);                  // Attacker spawn
        Carve(2, 24, 13, 26); Carve(26, 24, 37, 26);
        Carve(8, 14, 16, 16); Carve(23, 14, 31, 16); // cross halls
        Carve(8, 19, 16, 20); Carve(23, 19, 31, 20);
        Carve(8, 10, 16, 11); Carve(23, 10, 31, 11);
        Pillar(4, 17, 5, 18); Pillar(4, 21, 5, 22);   // lane pillars
        Fillw(19, 12, 20, 13); Fillw(19, 22, 20, 23); // mid pillars
        Crate(4, 4); Crate(5, 4); Crate(8, 6); Crate(9, 6); Crate(5, 8); Crate(10, 3);   // site cover
        Crate(12, 15); Crate(12, 20); Crate(18, 9); Crate(18, 17); Crate(19, 3);          // mid / hall cover
    }

    static bool OpenNear(int x, int y) {
        for (int dx = -1; dx <= 1; dx++) for (int dy = -1; dy <= 1; dy++) {
            int nx = x + dx, ny = y + dy;
            if (nx >= 0 && ny >= 0 && nx < W && ny < H && !wall[nx, ny]) return true;
        }
        return false;
    }

    static void Tint(Transform root, int x0, int y0, int x1, int y1, Color c) {
        Vector3 pos = new Vector3(((x0 + x1 + 1) / 2f - W / 2f) * Cell, .02f, (H / 2f - (y0 + y1 + 1) / 2f) * Cell);
        Util.Prim(PrimitiveType.Cube, root, pos, new Vector3((x1 - x0 + 1) * Cell, .04f, (y1 - y0 + 1) * Cell), c, false, false);
    }

    public static void BuildWorld(Transform parent, out List<Vector3> atk, out List<Vector3> def, out Vector3[] siteCenters) {
        Layout();
        var root = new GameObject("Map_IronHarbor").transform; root.SetParent(parent, false);
        Util.Prim(PrimitiveType.Cube, root, new Vector3(0, -.5f, 0), new Vector3(W * Cell + 30, 1, H * Cell + 30), new Color(.34f, .35f, .38f), true, true);
        Tint(root, 4, 3, 9, 7, new Color(.85f, .55f, .2f));
        Tint(root, 30, 3, 35, 7, new Color(.2f, .65f, .75f));
        Tint(root, 13, 27, 26, 30, new Color(.55f, .28f, .28f));
        Tint(root, 15, 1, 24, 4, new Color(.25f, .35f, .6f));
        Color[] wc = { new Color(.55f, .56f, .6f), new Color(.5f, .5f, .54f), new Color(.6f, .58f, .55f) };
        for (int y = 0; y < H; y++) {
            int x = 0;
            while (x < W) {
                if (!(wall[x, y] && OpenNear(x, y))) { x++; continue; }
                int s = x; while (x < W && wall[x, y] && OpenNear(x, y)) x++;
                int n = x - s;
                Vector3 c = new Vector3((s + n / 2f - W / 2f) * Cell, WallH / 2f, (H / 2f - y - .5f) * Cell);
                Util.Prim(PrimitiveType.Cube, root, c, new Vector3(n * Cell, WallH, Cell), wc[(s + y) % 3], true, true);
            }
        }
        for (int x = 0; x < W; x++) for (int y = 0; y < H; y++) if (crate[x, y]) {
            Vector3 c = Center(x, y); c.y = .75f;
            Util.Prim(PrimitiveType.Cube, root, c, new Vector3(Cell * .85f, 1.5f, Cell * .85f), (x + y) % 2 == 0 ? new Color(.55f, .38f, .22f) : new Color(.45f, .32f, .2f), true, true);
        }
        float sx = ((4 + 9 + 1) / 2f - W / 2f) * Cell, sz = (H / 2f - (3 + 7 + 1) / 2f) * Cell;
        siteCenters = new Vector3[] { new Vector3(sx, 0, sz), new Vector3(-sx, 0, sz) };
        Util.Prim(PrimitiveType.Cylinder, root, new Vector3(sx, 3f, sz), new Vector3(.3f, 3f, .3f), new Color(1f, .6f, .1f), false, false);
        Util.Prim(PrimitiveType.Cylinder, root, new Vector3(-sx, 3f, sz), new Vector3(.3f, 3f, .3f), new Color(.1f, .8f, 1f), false, false);
        int[] ax = { 15, 17, 20, 22, 24 }, ay = { 28, 29, 28, 29, 28 };
        atk = new List<Vector3>(); for (int i = 0; i < 5; i++) { var p = Center(ax[i], ay[i]); p.y = .05f; atk.Add(p); }
        int[] dx = { 16, 18, 20, 22, 23 }, dy = { 2, 3, 2, 3, 2 };
        def = new List<Vector3>(); for (int i = 0; i < 5; i++) { var p = Center(dx[i], dy[i]); p.y = .05f; def.Add(p); }
    }

    static readonly int[] DX = { 1, -1, 0, 0, 1, 1, -1, -1 }, DY = { 0, 0, 1, -1, 1, -1, 1, -1 };

    public static List<Vector3> FindPath(Vector3 from, Vector3 to) {
        Vector2Int s = ToCell(from), g = ToCell(to);
        if (!Walkable(g.x, g.y)) {
            bool found = false;
            for (int r = 1; r <= 4 && !found; r++)
                for (int dx = -r; dx <= r && !found; dx++) for (int dy = -r; dy <= r && !found; dy++)
                    if (Walkable(g.x + dx, g.y + dy)) { g = new Vector2Int(g.x + dx, g.y + dy); found = true; }
            if (!found) return null;
        }
        int[] prev = new int[W * H]; for (int i = 0; i < prev.Length; i++) prev[i] = -2;
        var q = new Queue<int>(); int si = s.y * W + s.x, gi = g.y * W + g.x;
        prev[si] = -1; q.Enqueue(si);
        while (q.Count > 0 && prev[gi] == -2) {
            int cur = q.Dequeue(); int cx = cur % W, cy = cur / W;
            for (int k = 0; k < 8; k++) {
                int nx = cx + DX[k], ny = cy + DY[k];
                if (!Walkable(nx, ny) || prev[ny * W + nx] != -2) continue;
                if (k >= 4 && (!Walkable(cx + DX[k], cy) || !Walkable(cx, cy + DY[k]))) continue;
                prev[ny * W + nx] = cur; q.Enqueue(ny * W + nx);
            }
        }
        if (prev[gi] == -2) return null;
        var path = new List<Vector3>();
        for (int c = gi; c != -1 && c != si; c = prev[c]) path.Add(Center(c % W, c / W));
        path.Reverse();
        return path;
    }
}
