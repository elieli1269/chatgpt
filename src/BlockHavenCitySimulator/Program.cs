using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlockHavenCitySimulator;

public static class Program
{
    [STAThread]
    public static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new BlockHavenForm());
    }
}

public enum BlockType
{
    Air,
    Grass,
    Dirt,
    Stone,
    Wood,
    Planks,
    Road,
    Glass,
    Water,
    Brick,
    Lamp,
    Metal,
    Roof
}

public enum GameMode { Survival, Creative }
public enum NpcState { Idle, Travel, Work, Shop, Sleep }
public enum NpcMood { Happy, Normal, Tired }

public sealed class BlockHavenForm : Form
{
    private readonly SaveManager saveManager = new();
    private readonly WorldManager world;
    private readonly EconomyManager economy;
    private readonly NpcManager npcs;
    private readonly VehicleManager vehicles;
    private readonly PlayerState player;
    private readonly System.Windows.Forms.Timer frameTimer = new();
    private readonly Stopwatch clock = Stopwatch.StartNew();
    private readonly HashSet<Keys> keys = new();
    private readonly Font hudFont = new("Segoe UI", 11f, FontStyle.Bold);
    private readonly Font smallFont = new("Segoe UI", 9f);
    private readonly Random random = new(42);
    private Point? selectedCell;
    private DateTime lastAutoSave = DateTime.UtcNow;
    private double lastFrameSeconds;
    private string toast = "Bienvenue à BlockHaven City Simulator — 100% local, sans Unity.";
    private double toastUntil = 8;

    public BlockHavenForm()
    {
        Text = "BlockHaven City Simulator - Offline EXE";
        ClientSize = new Size(1280, 800);
        MinimumSize = new Size(960, 640);
        DoubleBuffered = true;
        KeyPreview = true;

        SaveBundle saves = saveManager.LoadAll();
        world = new WorldManager(saves.World);
        economy = new EconomyManager(saves.Economy);
        player = saves.Player;
        if (player.Inventory.Count == 0)
        {
            player.Inventory[BlockType.Dirt] = 64;
            player.Inventory[BlockType.Stone] = 32;
            player.Inventory[BlockType.Planks] = 48;
            player.Inventory[BlockType.Wood] = 32;
        }

        npcs = new NpcManager(saves.Npcs, world);
        vehicles = new VehicleManager(saves.World.Vehicles);

        frameTimer.Interval = 16;
        frameTimer.Tick += (_, _) => TickGame();
        frameTimer.Start();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        keys.Add(e.KeyCode);
        HandleOneShotKey(e.KeyCode);
        base.OnKeyDown(e);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        keys.Remove(e.KeyCode);
        base.OnKeyUp(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        Point? cell = ScreenToCell(e.Location);
        if (cell is null)
        {
            return;
        }

        selectedCell = cell;
        int topY = world.TopY(cell.Value.X, cell.Value.Y);
        var target = new Vec3i(cell.Value.X, topY, cell.Value.Y);
        if (e.Button == MouseButtons.Left)
        {
            if (world.BreakBlock(target))
            {
                player.Inventory[player.SelectedBlock] = player.Inventory.GetValueOrDefault(player.SelectedBlock) + 1;
                toast = $"Bloc cassé en {target}.";
                toastUntil = clock.Elapsed.TotalSeconds + 2;
            }
        }
        else if (e.Button == MouseButtons.Right)
        {
            var place = new Vec3i(cell.Value.X, topY + 1, cell.Value.Y);
            if (player.Mode == GameMode.Creative || ConsumeSelectedBlock())
            {
                world.PlaceBlock(place, player.SelectedBlock);
                toast = $"Bloc {player.SelectedBlock} posé.";
                toastUntil = clock.Elapsed.TotalSeconds + 2;
            }
        }

        Invalidate();
        base.OnMouseDown(e);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        SaveNow();
        base.OnFormClosing(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.FromArgb(124, 191, 238));
        DrawSky(g);
        DrawWorld(g);
        DrawActors(g);
        DrawHud(g);
    }

    private void TickGame()
    {
        double now = clock.Elapsed.TotalSeconds;
        float dt = (float)Math.Max(0.001, now - lastFrameSeconds);
        lastFrameSeconds = now;
        UpdatePlayer(dt);
        world.WorldTime += dt;
        npcs.Update(dt, world.WorldTime);
        vehicles.Update(dt, player);

        if ((DateTime.UtcNow - lastAutoSave).TotalSeconds > 12)
        {
            SaveNow();
            lastAutoSave = DateTime.UtcNow;
        }

        Invalidate();
    }

    private void UpdatePlayer(float dt)
    {
        float speed = keys.Contains(Keys.ShiftKey) ? 10f : 6f;
        if (player.InVehicleId is not null)
        {
            VehicleState? vehicle = vehicles.Find(player.InVehicleId);
            if (vehicle is not null)
            {
                float throttle = AxisAny(new[] { Keys.W, Keys.Z }, Keys.S);
                float steer = AxisAny(new[] { Keys.D }, Keys.A, Keys.Q);
                vehicle.Angle += steer * 2.5f * dt;
                vehicle.X += MathF.Cos(vehicle.Angle) * throttle * 12f * dt;
                vehicle.Z += MathF.Sin(vehicle.Angle) * throttle * 12f * dt;
                player.X = vehicle.X;
                player.Z = vehicle.Z;
                player.Y = world.TopY((int)MathF.Round(player.X), (int)MathF.Round(player.Z)) + 1;
            }
            return;
        }

        float dx = AxisAny(new[] { Keys.D }, Keys.A, Keys.Q);
        float dz = AxisAny(new[] { Keys.S }, Keys.W, Keys.Z);
        if (dx != 0 || dz != 0)
        {
            float length = MathF.Sqrt(dx * dx + dz * dz);
            dx /= length;
            dz /= length;
            player.X += dx * speed * dt;
            player.Z += dz * speed * dt;
            player.Y = world.TopY((int)MathF.Round(player.X), (int)MathF.Round(player.Z)) + 1;
        }
    }

    private float AxisAny(Keys[] positive, params Keys[] negative)
    {
        bool hasPositive = positive.Any(keys.Contains);
        bool hasNegative = negative.Any(keys.Contains);
        return (hasPositive ? 1f : 0f) - (hasNegative ? 1f : 0f);
    }

    private void HandleOneShotKey(Keys key)
    {
        if (key == Keys.C)
        {
            player.Mode = player.Mode == GameMode.Creative ? GameMode.Survival : GameMode.Creative;
            toast = $"Mode {player.Mode}";
            toastUntil = clock.Elapsed.TotalSeconds + 2;
        }
        else if (key is Keys.D1 or Keys.NumPad1) player.SelectedBlock = BlockType.Dirt;
        else if (key is Keys.D2 or Keys.NumPad2) player.SelectedBlock = BlockType.Stone;
        else if (key is Keys.D3 or Keys.NumPad3) player.SelectedBlock = BlockType.Wood;
        else if (key is Keys.D4 or Keys.NumPad4) player.SelectedBlock = BlockType.Planks;
        else if (key is Keys.D5 or Keys.NumPad5) player.SelectedBlock = BlockType.Glass;
        else if (key == Keys.E) ToggleVehicle();
        else if (key == Keys.F1) SetJob("Police", 180);
        else if (key == Keys.F2) SetJob("Doctor", 220);
        else if (key == Keys.F3) SetJob("Firefighter", 190);
        else if (key == Keys.F4) SetJob("Delivery", 130);
        else if (key == Keys.B) TryBuyNearestHouse();
        else if (key == Keys.F5)
        {
            SaveNow();
            toast = "Sauvegarde locale écrite.";
            toastUntil = clock.Elapsed.TotalSeconds + 3;
        }
    }

    private void SetJob(string job, int salary)
    {
        player.CurrentJob = job;
        player.Money += salary;
        economy.CityTreasury -= salary;
        toast = $"Métier: {job}. Salaire immédiat: ${salary}.";
        toastUntil = clock.Elapsed.TotalSeconds + 3;
    }

    private void TryBuyNearestHouse()
    {
        PropertyState? house = economy.NearestProperty(player.X, player.Z, 8f);
        if (house is null)
        {
            toast = "Aucune maison achetable proche.";
        }
        else if (!string.IsNullOrEmpty(house.Owner))
        {
            toast = $"{house.Id} appartient déjà à {house.Owner}.";
        }
        else if (player.Money < house.Price)
        {
            toast = $"Il faut ${house.Price} pour acheter {house.Id}.";
        }
        else
        {
            player.Money -= house.Price;
            house.Owner = "player";
            player.OwnedHouseIds.Add(house.Id);
            toast = $"Maison achetée: {house.Id}.";
        }
        toastUntil = clock.Elapsed.TotalSeconds + 3;
    }

    private void ToggleVehicle()
    {
        if (player.InVehicleId is not null)
        {
            player.InVehicleId = null;
            toast = "Sortie du véhicule.";
            toastUntil = clock.Elapsed.TotalSeconds + 2;
            return;
        }

        VehicleState? nearest = vehicles.Nearest(player.X, player.Z, 5f);
        if (nearest is null)
        {
            toast = "Aucun véhicule proche.";
        }
        else
        {
            player.InVehicleId = nearest.Id;
            toast = $"Conduite: {nearest.Type}.";
        }
        toastUntil = clock.Elapsed.TotalSeconds + 2;
    }

    private bool ConsumeSelectedBlock()
    {
        int count = player.Inventory.GetValueOrDefault(player.SelectedBlock);
        if (count <= 0)
        {
            toast = $"Inventaire vide pour {player.SelectedBlock}.";
            toastUntil = clock.Elapsed.TotalSeconds + 2;
            return false;
        }
        player.Inventory[player.SelectedBlock] = count - 1;
        return true;
    }

    private void SaveNow()
    {
        saveManager.SaveAll(new SaveBundle
        {
            Player = player,
            World = world.ToSaveData(vehicles.All, economy.Properties),
            Economy = economy.ToSaveData(),
            Npcs = npcs.ToSaveData()
        });
    }

    private void DrawSky(Graphics g)
    {
        float day = (float)(world.WorldTime % 900.0 / 900.0);
        int alpha = (int)(Math.Sin(day * Math.PI) * 60 + 70);
        using var sun = new SolidBrush(Color.FromArgb(220, 255, 242, 136));
        g.FillEllipse(sun, ClientSize.Width - 180, 50 + (int)(Math.Sin(day * Math.PI) * 80), 70, 70);
        using var shade = new SolidBrush(Color.FromArgb(Math.Clamp(120 - alpha, 0, 90), 12, 20, 60));
        g.FillRectangle(shade, ClientRectangle);
    }

    private void DrawWorld(Graphics g)
    {
        int radius = 34;
        int px = (int)MathF.Round(player.X);
        int pz = (int)MathF.Round(player.Z);
        List<Vec3i> blocks = new(radius * radius * 4);
        for (int x = px - radius; x <= px + radius; x++)
        {
            for (int z = pz - radius; z <= pz + radius; z++)
            {
                foreach (Vec3i block in world.VisibleColumn(x, z))
                {
                    blocks.Add(block);
                }
            }
        }

        blocks.Sort((a, b) => (a.X + a.Z + a.Y).CompareTo(b.X + b.Z + b.Y));
        foreach (Vec3i block in blocks)
        {
            DrawCube(g, block, world.GetBlock(block));
        }

        if (selectedCell is not null)
        {
            int y = world.TopY(selectedCell.Value.X, selectedCell.Value.Y) + 1;
            PointF p = Project(selectedCell.Value.X, selectedCell.Value.Y, y);
            using var pen = new Pen(Color.Yellow, 3f);
            g.DrawRectangle(pen, p.X - 18, p.Y - 18, 36, 24);
        }
    }

    private void DrawActors(Graphics g)
    {
        foreach (VehicleState vehicle in vehicles.All)
        {
            PointF p = Project(vehicle.X, vehicle.Z, world.TopY((int)vehicle.X, (int)vehicle.Z) + 1.5f);
            using var brush = new SolidBrush(vehicle.Type == "Moto" ? Color.Cyan : Color.Red);
            g.FillRectangle(brush, p.X - 12, p.Y - 12, vehicle.Type == "Moto" ? 16 : 28, 14);
            g.DrawString(vehicle.Type, smallFont, Brushes.Black, p.X - 15, p.Y - 28);
        }

        foreach (NpcStateData npc in npcs.All)
        {
            PointF p = Project(npc.X, npc.Z, world.TopY((int)npc.X, (int)npc.Z) + 1);
            using var brush = new SolidBrush(npc.Role switch
            {
                "Police" => Color.Navy,
                "Doctor" => Color.White,
                "Merchant" => Color.Orange,
                "Firefighter" => Color.DarkRed,
                _ => Color.MediumPurple
            });
            g.FillEllipse(brush, p.X - 6, p.Y - 18, 12, 18);
            g.DrawString(npc.DisplayName, smallFont, Brushes.Black, p.X - 24, p.Y - 36);
        }

        PointF playerPoint = Project(player.X, player.Z, player.Y + 0.5f);
        using var playerBrush = new SolidBrush(Color.LimeGreen);
        g.FillEllipse(playerBrush, playerPoint.X - 8, playerPoint.Y - 20, 16, 20);
        g.DrawString("PLAYER", smallFont, Brushes.Black, playerPoint.X - 22, playerPoint.Y - 38);
    }

    private void DrawHud(Graphics g)
    {
        using var panel = new SolidBrush(Color.FromArgb(205, 19, 75, 150));
        using var panel2 = new SolidBrush(Color.FromArgb(185, 255, 255, 255));
        g.FillRoundedRectangle(panel, new Rectangle(14, 14, 430, 170), 18);
        g.DrawString($"BlockHaven City Simulator", hudFont, Brushes.White, 28, 24);
        g.DrawString($"$ {player.Money} | Job: {player.CurrentJob} | Mode: {player.Mode}", smallFont, Brushes.White, 28, 54);
        g.DrawString($"Position: {player.X:0},{player.Y:0},{player.Z:0} | Bloc: {player.SelectedBlock}", smallFont, Brushes.White, 28, 76);
        g.DrawString("ZQSD/WASD déplacer • Shift sprint • clic gauche/droit bloc", smallFont, Brushes.White, 28, 102);
        g.DrawString("E véhicule • B acheter maison • F1-F4 métiers • F5 sauver", smallFont, Brushes.White, 28, 124);
        g.DrawString($"Saves: {saveManager.SaveDirectory}", smallFont, Brushes.White, 28, 146);

        Rectangle minimap = new(ClientSize.Width - 190, 18, 170, 170);
        g.FillRoundedRectangle(panel2, minimap, 14);
        g.DrawString("Mini-map", smallFont, Brushes.Black, minimap.X + 56, minimap.Y + 8);
        foreach (PropertyState property in economy.Properties)
        {
            float mx = minimap.X + 85 + property.X * 0.8f;
            float my = minimap.Y + 90 + property.Z * 0.8f;
            g.FillRectangle(property.Owner == "player" ? Brushes.Green : Brushes.Brown, mx, my, 5, 5);
        }
        g.FillEllipse(Brushes.Blue, minimap.X + 85 + player.X * 0.8f - 4, minimap.Y + 90 + player.Z * 0.8f - 4, 8, 8);

        if (clock.Elapsed.TotalSeconds < toastUntil)
        {
            SizeF size = g.MeasureString(toast, hudFont);
            Rectangle rect = new((ClientSize.Width - (int)size.Width) / 2 - 16, ClientSize.Height - 80, (int)size.Width + 32, 42);
            g.FillRoundedRectangle(panel, rect, 18);
            g.DrawString(toast, hudFont, Brushes.White, rect.X + 16, rect.Y + 11);
        }
    }

    private void DrawCube(Graphics g, Vec3i block, BlockType type)
    {
        if (type == BlockType.Air)
        {
            return;
        }

        PointF top = Project(block.X, block.Z, block.Y + 1);
        PointF left = Project(block.X, block.Z, block.Y);
        Color c = Palette.For(type);
        PointF[] topFace =
        {
            new(top.X, top.Y - 16), new(top.X + 28, top.Y), new(top.X, top.Y + 16), new(top.X - 28, top.Y)
        };
        PointF[] leftFace = { topFace[3], topFace[2], new(left.X, left.Y + 16), new(left.X - 28, left.Y) };
        PointF[] rightFace = { topFace[1], topFace[2], new(left.X, left.Y + 16), new(left.X + 28, left.Y) };
        using var topBrush = new SolidBrush(Light(c, 1.15f));
        using var leftBrush = new SolidBrush(Light(c, 0.78f));
        using var rightBrush = new SolidBrush(Light(c, 0.92f));
        g.FillPolygon(leftBrush, leftFace);
        g.FillPolygon(rightBrush, rightFace);
        g.FillPolygon(topBrush, topFace);
        using var pen = new Pen(Color.FromArgb(65, 0, 0, 0));
        g.DrawPolygon(pen, topFace);
    }

    private PointF Project(float x, float z, float y)
    {
        float scale = 28f;
        float sx = ClientSize.Width / 2f + (x - player.X - (z - player.Z)) * scale;
        float sy = ClientSize.Height / 2f + (x - player.X + z - player.Z) * scale * 0.48f - y * 18f + 120f;
        return new PointF(sx, sy);
    }

    private Point? ScreenToCell(Point mouse)
    {
        Point? best = null;
        double bestDistance = 999999;
        int px = (int)MathF.Round(player.X);
        int pz = (int)MathF.Round(player.Z);
        for (int x = px - 36; x <= px + 36; x++)
        {
            for (int z = pz - 36; z <= pz + 36; z++)
            {
                PointF p = Project(x, z, world.TopY(x, z) + 1);
                double d = Math.Pow(mouse.X - p.X, 2) + Math.Pow(mouse.Y - p.Y, 2);
                if (d < bestDistance && d < 1200)
                {
                    bestDistance = d;
                    best = new Point(x, z);
                }
            }
        }
        return best;
    }

    private static Color Light(Color c, float factor)
    {
        return Color.FromArgb(c.A, Math.Clamp((int)(c.R * factor), 0, 255), Math.Clamp((int)(c.G * factor), 0, 255), Math.Clamp((int)(c.B * factor), 0, 255));
    }
}

public sealed class WorldManager
{
    private readonly Dictionary<Vec3i, BlockType> modified = new();
    private readonly Dictionary<Vec3i, BlockType> city = new();
    private readonly int seed;
    public double WorldTime { get; set; }

    public WorldManager(WorldSaveData data)
    {
        seed = data.Seed == 0 ? 133742 : data.Seed;
        WorldTime = data.WorldTime;
        BuildCity();
        foreach (BlockRecord record in data.ModifiedBlocks)
        {
            modified[new Vec3i(record.X, record.Y, record.Z)] = record.Type;
        }
    }

    public BlockType GetBlock(Vec3i p)
    {
        if (p.Y < 0 || p.Y > 32) return BlockType.Air;
        if (modified.TryGetValue(p, out BlockType edited)) return edited;
        if (city.TryGetValue(p, out BlockType prefab)) return prefab;
        return GeneratedBlock(p);
    }

    public void PlaceBlock(Vec3i p, BlockType type)
    {
        modified[p] = type;
    }

    public bool BreakBlock(Vec3i p)
    {
        if (GetBlock(p) == BlockType.Air || p.Y <= 0) return false;
        modified[p] = BlockType.Air;
        return true;
    }

    public int TopY(int x, int z)
    {
        for (int y = 32; y >= 0; y--)
        {
            if (GetBlock(new Vec3i(x, y, z)) != BlockType.Air)
            {
                return y;
            }
        }
        return 0;
    }

    public IEnumerable<Vec3i> VisibleColumn(int x, int z)
    {
        int top = TopY(x, z);
        int min = Math.Max(0, top - 2);
        for (int y = min; y <= top; y++)
        {
            Vec3i p = new(x, y, z);
            if (GetBlock(p) != BlockType.Air) yield return p;
        }
    }

    public WorldSaveData ToSaveData(IEnumerable<VehicleState> vehicles, IEnumerable<PropertyState> properties)
    {
        return new WorldSaveData
        {
            Seed = seed,
            WorldTime = WorldTime,
            ModifiedBlocks = modified.Select(kv => new BlockRecord(kv.Key.X, kv.Key.Y, kv.Key.Z, kv.Value)).ToList(),
            Vehicles = vehicles.Select(v => v.Clone()).ToList(),
            Properties = properties.Select(p => p.Clone()).ToList()
        };
    }

    private BlockType GeneratedBlock(Vec3i p)
    {
        int height = TerrainHeight(p.X, p.Z);
        if (IsRiver(p.X, p.Z) && p.Y <= 3) return BlockType.Water;
        if (p.Y > height) return BlockType.Air;
        if (p.Y == height) return IsRiver(p.X, p.Z) ? BlockType.Dirt : BlockType.Grass;
        if (p.Y > height - 3) return BlockType.Dirt;
        return BlockType.Stone;
    }

    private int TerrainHeight(int x, int z)
    {
        if (Math.Abs(x) < 80 && Math.Abs(z) < 80) return 2;
        double forest = Noise(x * 0.061, z * 0.061) * 4;
        double mountain = Noise((x + seed) * 0.019, (z - seed) * 0.019) * 12;
        return 2 + (int)Math.Round(forest + (mountain > 7 ? mountain : 0));
    }

    private bool IsRiver(int x, int z) => Math.Abs(z - Math.Sin(x * 0.06) * 14) < 3 && Math.Abs(x) > 80;

    private static double Noise(double x, double z)
    {
        double value = Math.Sin(x * 12.9898 + z * 78.233) * 43758.5453;
        return value - Math.Floor(value);
    }

    private void BuildCity()
    {
        Road(-72, 72, -3, 3);
        Road(-3, 3, -72, 72);
        for (int i = -64; i <= 64; i += 16)
        {
            Lamp(i, 5);
            Lamp(5, i);
        }

        House(-30, -24, 12, 10, BlockType.Planks, true);
        House(26, -28, 14, 12, BlockType.Brick, false);
        House(-52, 24, 18, 14, BlockType.Planks, false);
        Building(18, 18, 18, 14, 9, BlockType.Brick, "HOSPITAL");
        Building(-22, 24, 15, 12, 8, BlockType.Metal, "POLICE");
        Building(-10, -52, 24, 16, 8, BlockType.Planks, "MARKET");
        ForestRing();
    }

    private void Road(int x1, int x2, int z1, int z2)
    {
        for (int x = x1; x <= x2; x++)
        for (int z = z1; z <= z2; z++) city[new Vec3i(x, 3, z)] = BlockType.Road;
    }

    private void House(int ox, int oz, int sx, int sz, BlockType wall, bool starter)
    {
        Building(ox, oz, sx, sz, 7, wall, starter ? "STARTER" : "HOUSE");
        city[new Vec3i(ox + 2, 4, oz + sz - 3)] = BlockType.Wood;
        city[new Vec3i(ox + 3, 4, oz + sz - 3)] = BlockType.Wood;
        city[new Vec3i(ox + sx - 3, 4, oz + 3)] = BlockType.Planks;
    }

    private void Building(int ox, int oz, int sx, int sz, int sy, BlockType wall, string label)
    {
        for (int x = 0; x < sx; x++)
        for (int z = 0; z < sz; z++)
        {
            city[new Vec3i(ox + x, 3, oz + z)] = BlockType.Wood;
            for (int y = 4; y < 4 + sy; y++)
            {
                bool shell = x == 0 || z == 0 || x == sx - 1 || z == sz - 1 || y == 3 + sy;
                if (shell) city[new Vec3i(ox + x, y, oz + z)] = wall;
            }
        }
        for (int x = -1; x <= sx; x++)
        for (int z = -1; z <= sz; z++) city[new Vec3i(ox + x, 4 + sy, oz + z)] = BlockType.Roof;
        city[new Vec3i(ox + sx / 2, 4, oz)] = BlockType.Air;
        city[new Vec3i(ox + sx / 2, 5, oz)] = BlockType.Air;
        for (int i = 0; i < Math.Min(label.Length, sx - 2); i++) city[new Vec3i(ox + 1 + i, 4 + sy, oz - 1)] = BlockType.Lamp;
        for (int x = 2; x < sx - 2; x += 4)
        {
            city[new Vec3i(ox + x, 6, oz)] = BlockType.Glass;
            city[new Vec3i(ox + x + 1, 6, oz)] = BlockType.Glass;
        }
    }

    private void Lamp(int x, int z)
    {
        for (int y = 4; y <= 7; y++) city[new Vec3i(x, y, z)] = BlockType.Metal;
        city[new Vec3i(x, 8, z)] = BlockType.Lamp;
    }

    private void ForestRing()
    {
        for (int x = -110; x <= 110; x += 16)
        for (int z = -110; z <= 110; z += 16)
        {
            if (Math.Abs(x) < 80 && Math.Abs(z) < 80) continue;
            for (int y = 3; y < 8; y++) city[new Vec3i(x, y, z)] = BlockType.Wood;
            for (int dx = -2; dx <= 2; dx++)
            for (int dz = -2; dz <= 2; dz++)
            for (int y = 7; y <= 10; y++) city[new Vec3i(x + dx, y, z + dz)] = BlockType.Grass;
        }
    }
}

public sealed class EconomyManager
{
    public int CityTreasury { get; set; }
    public List<PropertyState> Properties { get; }
    public Dictionary<string, ShopPrice> Prices { get; }

    public EconomyManager(EconomySaveData data)
    {
        CityTreasury = data.CityTreasury == 0 ? 100_000 : data.CityTreasury;
        Properties = data.Properties.Count > 0 ? data.Properties : Defaults.Properties();
        Prices = data.Prices.Count > 0 ? data.Prices.ToDictionary(p => p.ItemId) : Defaults.Prices().ToDictionary(p => p.ItemId);
    }

    public PropertyState? NearestProperty(float x, float z, float radius)
    {
        return Properties.Select(p => new { Property = p, Distance = MathF.Sqrt((p.X - x) * (p.X - x) + (p.Z - z) * (p.Z - z)) })
            .Where(p => p.Distance <= radius).OrderBy(p => p.Distance).Select(p => p.Property).FirstOrDefault();
    }

    public EconomySaveData ToSaveData() => new() { CityTreasury = CityTreasury, Properties = Properties.Select(p => p.Clone()).ToList(), Prices = Prices.Values.Select(p => p.Clone()).ToList() };
}

public sealed class NpcManager
{
    private readonly Random random = new(7);
    public List<NpcStateData> All { get; }

    public NpcManager(NpcSaveData data, WorldManager world)
    {
        All = data.Npcs.Count > 0 ? data.Npcs : Defaults.Npcs();
        foreach (NpcStateData npc in All)
        {
            npc.Y = world.TopY((int)npc.X, (int)npc.Z) + 1;
        }
    }

    public void Update(float dt, double worldTime)
    {
        int hour = (int)(worldTime / 30.0 % 24.0);
        foreach (NpcStateData npc in All)
        {
            (float tx, float tz, NpcState state) = RoutineTarget(npc, hour);
            npc.State = state;
            npc.Mood = hour is < 7 or > 21 ? NpcMood.Tired : hour is >= 17 and <= 20 ? NpcMood.Happy : NpcMood.Normal;
            float dx = tx - npc.X;
            float dz = tz - npc.Z;
            float len = MathF.Sqrt(dx * dx + dz * dz);
            if (len > 0.2f)
            {
                npc.X += dx / len * dt * 2.5f;
                npc.Z += dz / len * dt * 2.5f;
            }
            else if (random.NextDouble() < 0.01)
            {
                npc.X += random.Next(-1, 2) * 0.3f;
                npc.Z += random.Next(-1, 2) * 0.3f;
            }
        }
    }

    public NpcSaveData ToSaveData() => new() { Npcs = All.Select(n => n.Clone()).ToList() };

    private static (float x, float z, NpcState state) RoutineTarget(NpcStateData npc, int hour)
    {
        if (hour < 7 || hour > 21) return (npc.HomeX, npc.HomeZ, NpcState.Sleep);
        if (hour < 16) return (npc.WorkX, npc.WorkZ, NpcState.Work);
        if (hour < 19) return (0, -42, NpcState.Shop);
        return (npc.HomeX, npc.HomeZ, NpcState.Travel);
    }
}

public sealed class VehicleManager
{
    public List<VehicleState> All { get; }

    public VehicleManager(List<VehicleState> saved)
    {
        All = saved.Count > 0 ? saved : Defaults.Vehicles();
    }

    public VehicleState? Find(string id) => All.FirstOrDefault(v => v.Id == id);

    public VehicleState? Nearest(float x, float z, float radius)
    {
        return All.Select(v => new { Vehicle = v, Distance = MathF.Sqrt((v.X - x) * (v.X - x) + (v.Z - z) * (v.Z - z)) })
            .Where(v => v.Distance <= radius).OrderBy(v => v.Distance).Select(v => v.Vehicle).FirstOrDefault();
    }

    public void Update(float dt, PlayerState player)
    {
        foreach (VehicleState vehicle in All.Where(v => v.IsTraffic && player.InVehicleId != v.Id))
        {
            vehicle.Angle += MathF.Sin((float)DateTime.UtcNow.TimeOfDay.TotalSeconds * 0.2f) * dt * 0.7f;
            vehicle.X += MathF.Cos(vehicle.Angle) * dt * 4f;
            vehicle.Z += MathF.Sin(vehicle.Angle) * dt * 4f;
            if (Math.Abs(vehicle.X) > 70 || Math.Abs(vehicle.Z) > 70)
            {
                vehicle.Angle += MathF.PI;
            }
        }
    }
}

public sealed class SaveManager
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true, Converters = { new JsonStringEnumConverter() } };
    public string SaveDirectory { get; } = Path.Combine(AppContext.BaseDirectory, "Saves");
    private string WorldPath => Path.Combine(SaveDirectory, "world.json");
    private string PlayerPath => Path.Combine(SaveDirectory, "player.json");
    private string EconomyPath => Path.Combine(SaveDirectory, "economy.json");
    private string NpcsPath => Path.Combine(SaveDirectory, "npcs.json");

    public SaveBundle LoadAll()
    {
        Directory.CreateDirectory(SaveDirectory);
        return new SaveBundle
        {
            World = Load(WorldPath, new WorldSaveData { Seed = 133742 }),
            Player = Load(PlayerPath, Defaults.Player()),
            Economy = Load(EconomyPath, new EconomySaveData { CityTreasury = 100_000, Properties = Defaults.Properties(), Prices = Defaults.Prices() }),
            Npcs = Load(NpcsPath, new NpcSaveData { Npcs = Defaults.Npcs() })
        };
    }

    public void SaveAll(SaveBundle bundle)
    {
        Directory.CreateDirectory(SaveDirectory);
        WriteAtomic(WorldPath, bundle.World);
        WriteAtomic(PlayerPath, bundle.Player);
        WriteAtomic(EconomyPath, bundle.Economy);
        WriteAtomic(NpcsPath, bundle.Npcs);
    }

    private static T Load<T>(string path, T fallback)
    {
        if (!File.Exists(path)) return fallback;
        string json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<T>(json, Options) ?? fallback;
    }

    private static void WriteAtomic<T>(string path, T data)
    {
        string temp = path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(data, Options));
        File.Move(temp, path, true);
    }
}

public static class Defaults
{
    public static PlayerState Player() => new()
    {
        X = -22,
        Y = 5,
        Z = -18,
        Money = 2000,
        Mode = GameMode.Creative,
        CurrentJob = "Citizen",
        SelectedBlock = BlockType.Planks
    };

    public static List<PropertyState> Properties() => new()
    {
        new PropertyState { Id = "starter_house", Owner = "player", Price = 1200, X = -30, Z = -24 },
        new PropertyState { Id = "family_house", Owner = "", Price = 3500, X = 26, Z = -28 },
        new PropertyState { Id = "villa", Owner = "", Price = 6500, X = -52, Z = 24 }
    };

    public static List<ShopPrice> Prices() => new()
    {
        new ShopPrice { ItemId = "dirt", BuyPrice = 1, SellPrice = 1 },
        new ShopPrice { ItemId = "planks", BuyPrice = 3, SellPrice = 1 },
        new ShopPrice { ItemId = "car", BuyPrice = 900, SellPrice = 600 },
        new ShopPrice { ItemId = "moto", BuyPrice = 550, SellPrice = 350 }
    };

    public static List<VehicleState> Vehicles() => new()
    {
        new VehicleState { Id = "starter_car", Type = "Car", X = -8, Z = -8, Owner = "player" },
        new VehicleState { Id = "moto_01", Type = "Moto", X = 8, Z = -8, Owner = "" },
        new VehicleState { Id = "traffic_01", Type = "Car", X = 16, Z = 5, Owner = "npc", IsTraffic = true, Angle = 0.6f }
    };

    public static List<NpcStateData> Npcs()
    {
        string[] names = { "Alex", "Sam", "Mia", "Noah", "Lina", "Jules", "Emma", "Leo", "Zoé", "Nina" };
        string[] roles = { "Citizen", "Police", "Doctor", "Merchant", "Firefighter" };
        var result = new List<NpcStateData>();
        for (int i = 0; i < 36; i++)
        {
            string role = roles[i % roles.Length];
            (float wx, float wz) = role switch
            {
                "Police" => (-15, 30),
                "Doctor" => (25, 24),
                "Merchant" => (0, -42),
                "Firefighter" => (-5, 8),
                _ => (0, 0)
            };
            float hx = -55 + i % 12 * 10;
            float hz = 40 + i / 12 * 12;
            result.Add(new NpcStateData
            {
                Id = $"npc_{i:000}",
                DisplayName = $"{names[i % names.Length]} {10 + i}",
                Role = role,
                Money = 150 + i * 33,
                Mood = NpcMood.Normal,
                State = NpcState.Idle,
                X = hx,
                Z = hz,
                HomeX = hx,
                HomeZ = hz,
                WorkX = wx,
                WorkZ = wz
            });
        }
        return result;
    }
}

public static class Palette
{
    public static Color For(BlockType type) => type switch
    {
        BlockType.Grass => Color.FromArgb(70, 176, 58),
        BlockType.Dirt => Color.FromArgb(126, 82, 42),
        BlockType.Stone => Color.FromArgb(120, 120, 126),
        BlockType.Wood => Color.FromArgb(116, 67, 30),
        BlockType.Planks => Color.FromArgb(190, 137, 74),
        BlockType.Road => Color.FromArgb(30, 31, 36),
        BlockType.Glass => Color.FromArgb(130, 180, 230),
        BlockType.Water => Color.FromArgb(50, 100, 215),
        BlockType.Brick => Color.FromArgb(148, 48, 38),
        BlockType.Lamp => Color.FromArgb(255, 224, 75),
        BlockType.Metal => Color.FromArgb(86, 92, 100),
        BlockType.Roof => Color.FromArgb(105, 40, 32),
        _ => Color.Transparent
    };
}

public static class GraphicsExtensions
{
    public static void FillRoundedRectangle(this Graphics g, Brush brush, Rectangle rect, int radius)
    {
        using GraphicsPath path = new();
        int d = radius * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        g.FillPath(brush, path);
    }
}

public readonly record struct Vec3i(int X, int Y, int Z);

public sealed class SaveBundle
{
    public WorldSaveData World { get; set; } = new();
    public PlayerState Player { get; set; } = new();
    public EconomySaveData Economy { get; set; } = new();
    public NpcSaveData Npcs { get; set; } = new();
}

public sealed class WorldSaveData
{
    public int Seed { get; set; }
    public double WorldTime { get; set; }
    public List<BlockRecord> ModifiedBlocks { get; set; } = new();
    public List<PropertyState> Properties { get; set; } = new();
    public List<VehicleState> Vehicles { get; set; } = new();
}

public sealed class PlayerState
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public int Money { get; set; } = 2000;
    public GameMode Mode { get; set; } = GameMode.Creative;
    public string CurrentJob { get; set; } = "Citizen";
    public BlockType SelectedBlock { get; set; } = BlockType.Planks;
    public Dictionary<BlockType, int> Inventory { get; set; } = new();
    public HashSet<string> OwnedHouseIds { get; set; } = new();
    public HashSet<string> OwnedVehicleIds { get; set; } = new();
    public string? InVehicleId { get; set; }
}

public sealed class EconomySaveData
{
    public int CityTreasury { get; set; }
    public List<PropertyState> Properties { get; set; } = new();
    public List<ShopPrice> Prices { get; set; } = new();
}

public sealed class NpcSaveData
{
    public List<NpcStateData> Npcs { get; set; } = new();
}

public sealed record BlockRecord(int X, int Y, int Z, BlockType Type);

public sealed class PropertyState
{
    public string Id { get; set; } = "";
    public string Owner { get; set; } = "";
    public int Price { get; set; }
    public float X { get; set; }
    public float Z { get; set; }
    public PropertyState Clone() => (PropertyState)MemberwiseClone();
}

public sealed class ShopPrice
{
    public string ItemId { get; set; } = "";
    public int BuyPrice { get; set; }
    public int SellPrice { get; set; }
    public ShopPrice Clone() => (ShopPrice)MemberwiseClone();
}

public sealed class VehicleState
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "Car";
    public string Owner { get; set; } = "";
    public float X { get; set; }
    public float Z { get; set; }
    public float Angle { get; set; }
    public bool IsTraffic { get; set; }
    public VehicleState Clone() => (VehicleState)MemberwiseClone();
}

public sealed class NpcStateData
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Role { get; set; } = "Citizen";
    public int Money { get; set; }
    public NpcState State { get; set; }
    public NpcMood Mood { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public float HomeX { get; set; }
    public float HomeZ { get; set; }
    public float WorkX { get; set; }
    public float WorkZ { get; set; }
    public string Dialogue => $"{DisplayName} ({Role}) vit sa routine: {State}, humeur {Mood}.";
    public NpcStateData Clone() => (NpcStateData)MemberwiseClone();
}
