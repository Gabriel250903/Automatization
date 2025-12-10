using Automatization.Types;

namespace Automatization.Services
{
    public class MarketService
    {
        public List<MarketItem> Items = [];
        public List<RankItem> Ranks = [];

        public MarketService()
        {
            InitializeItems();
            InitializeProductKits();
            InitializeSuppliesKits();
            InitializeRanks();
        }

        private void InitializeRanks()
        {
            Ranks =
            [
                new() { Name = "Recruit", Icon = "https://wiki.pro-tanki.com/en/images/6/64/IconsNormal_01.png" },
                new() { Name = "Private", Icon = "https://wiki.pro-tanki.com/en/images/9/91/IconsNormal_02.png" },
                new() { Name = "Gefreiter", Icon = "https://wiki.pro-tanki.com/en/images/2/20/IconsNormal_03.png" },
                new() { Name = "Corporal", Icon = "https://wiki.pro-tanki.com/en/images/d/d6/IconsNormal_04.png" },
                new() { Name = "Master Corporal", Icon = "https://wiki.pro-tanki.com/en/images/5/51/IconsNormal_05.png" },
                new() { Name = "Sergeant", Icon = "https://wiki.pro-tanki.com/en/images/5/57/IconsNormal_06.png" },
                new() { Name = "Staff Sergeant", Icon = "https://wiki.pro-tanki.com/en/images/5/53/IconsNormal_07.png" },
                new() { Name = "Master Sergeant", Icon = "https://wiki.pro-tanki.com/en/images/6/6b/IconsNormal_08.png" },
                new() { Name = "First Sergeant", Icon = "https://wiki.pro-tanki.com/en/images/1/14/IconsNormal_09.png" },
                new() { Name = "Sergeant-Major", Icon = "https://wiki.pro-tanki.com/en/images/6/63/IconsNormal_10.png" },
                new() { Name = "Warrant Officer 1", Icon = "https://wiki.pro-tanki.com/en/images/3/39/IconsNormal_11.png" },
                new() { Name = "Warrant Officer 2", Icon = "https://wiki.pro-tanki.com/en/images/5/50/IconsNormal_12.png" },
                new() { Name = "Warrant Officer 3", Icon = "https://wiki.pro-tanki.com/en/images/c/cc/IconsNormal_13.png" },
                new() { Name = "Warrant Officer 4", Icon = "https://wiki.pro-tanki.com/en/images/e/e8/IconsNormal_14.png" },
                new() { Name = "Warrant Officer 5", Icon = "https://wiki.pro-tanki.com/en/images/5/55/IconsNormal_15.png" },
                new() { Name = "Third Lieutenant", Icon = "https://wiki.pro-tanki.com/en/images/3/3f/IconsNormal_16.png" },
                new() { Name = "Second Lieutenant", Icon = "https://wiki.pro-tanki.com/en/images/4/48/IconsNormal_17.png" },
                new() { Name = "First Lieutenant", Icon = "https://wiki.pro-tanki.com/en/images/7/72/IconsNormal_18.png" },
                new() { Name = "Captain", Icon = "https://wiki.pro-tanki.com/en/images/e/ed/IconsNormal_19.png" },
                new() { Name = "Major", Icon = "https://wiki.pro-tanki.com/en/images/b/b3/IconsNormal_20.png" },
                new() { Name = "Lieutenant Colonel", Icon = "https://wiki.pro-tanki.com/en/images/c/c8/IconsNormal_21.png" },
                new() { Name = "Colonel", Icon = "https://wiki.pro-tanki.com/en/images/7/7c/IconsNormal_22.png" },
                new() { Name = "Brigadier", Icon = "https://wiki.pro-tanki.com/en/images/2/24/IconsNormal_23.png" },
                new() { Name = "Major General", Icon = "https://wiki.pro-tanki.com/en/images/7/73/IconsNormal_24.png" },
                new() { Name = "Lieutenant General", Icon = "https://wiki.pro-tanki.com/en/images/7/71/IconsNormal_25.png" },
                new() { Name = "General", Icon = "https://wiki.pro-tanki.com/en/images/0/04/IconsNormal_26.png" },
                new() { Name = "Marshal", Icon = "https://wiki.pro-tanki.com/en/images/0/0f/IconsNormal_27.png" },
                new() { Name = "Field Marshal", Icon = "https://wiki.pro-tanki.com/en/images/3/38/IconsNormal_28.png" },
                new() { Name = "Commander", Icon = "https://wiki.pro-tanki.com/en/images/d/db/IconsNormal_29.png" },
                new() { Name = "Generalissimo", Icon = "https://wiki.pro-tanki.com/en/images/e/eb/IconsNormal_30.png" }
            ];
        }

        public string? GetRankIcon(string rankName)
        {
            return Ranks.FirstOrDefault(r => r.Name.Equals(rankName.Trim(), System.StringComparison.OrdinalIgnoreCase))?.Icon;
        }

        private void InitializeItems()
        {
            // Turrets
            Items.Add(new MarketItem("Turret_Smoky", ItemCategory.Turret, [0, 7100, 61400, 177700], "https://wiki.pro-tanki.com/en/images/e/e5/Smoky_m3.png", "Turret_Smoky_Desc"));
            Items.Add(new MarketItem("Turret_Firebird", ItemCategory.Turret, [150, 7100, 61400, 177700], "https://wiki.pro-tanki.com/en/images/6/67/Firebird_m3.png", "Turret_Firebird_Desc"));
            Items.Add(new MarketItem("Turret_Firebird_XT", ItemCategory.Turret, [150, 4260, 36840, 106620], "https://wiki.pro-tanki.com/en/images/8/8e/Firebird_XT_m3.png", "Turret_Firebird_XT_Desc"));
            Items.Add(new MarketItem("Turret_Twins", ItemCategory.Turret, [350, 21350, 70300, 188500], "https://wiki.pro-tanki.com/en/images/f/fd/Twins_m3.png", "Turret_Twins_Desc"));
            Items.Add(new MarketItem("Turret_Railgun", ItemCategory.Turret, [800, 17600, 79200, 199300], "https://wiki.pro-tanki.com/en/images/d/d1/Railgun_m3.png", "Turret_Railgun_Desc"));
            Items.Add(new MarketItem("Turret_Railgun_XT", ItemCategory.Turret, [0, 10560, 47520, 119580], "https://wiki.pro-tanki.com/en/images/e/e5/Railgun_XT_m3.png", "Turret_Railgun_XT_Desc"));
            Items.Add(new MarketItem("Turret_Isida", ItemCategory.Turret, [1250, 22850, 88100, 221000], "https://wiki.pro-tanki.com/en/images/c/c6/Isida_m3.png", "Turret_Isida_Desc"));
            Items.Add(new MarketItem("Turret_Thunder", ItemCategory.Turret, [1450, 28100, 97000, 242500], "https://wiki.pro-tanki.com/en/images/3/34/Thunder_m3.png", "Turret_Thunder_Desc"));
            Items.Add(new MarketItem("Turret_Thunder_XT", ItemCategory.Turret, [0, 16860, 58200, 145500], "https://wiki.pro-tanki.com/en/images/4/4c/Thunder_XT_m3.png", "Turret_Thunder_XT_Desc"));
            Items.Add(new MarketItem("Turret_Hammer", ItemCategory.Turret, [800, 17600, 79200, 210100], "https://wiki.pro-tanki.com/en/images/1/18/Hammer_m3.png", "Turret_Hammer_Desc"));
            Items.Add(new MarketItem("Turret_Freeze", ItemCategory.Turret, [1450, 28100, 97100, 253300], "https://wiki.pro-tanki.com/en/images/7/7c/Freeze3.png", "Turret_Freeze_Desc"));
            Items.Add(new MarketItem("Turret_Ricochet", ItemCategory.Turret, [1700, 33350, 105900, 264200], "https://wiki.pro-tanki.com/en/images/7/7d/Ricochet_m3.png", "Turret_Ricochet_Desc"));
            Items.Add(new MarketItem("Turret_Vulcan", ItemCategory.Turret, [1250, 22850, 88100, 231800], "https://wiki.pro-tanki.com/en/images/9/9c/Vulcan_m3.png", "Turret_Vulcan_Desc"));
            Items.Add(new MarketItem("Turret_Shaft", ItemCategory.Turret, [1900, 38600, 114800, 275000], "https://wiki.pro-tanki.com/en/images/a/ad/Shaft_m3.png", "Turret_Shaft_Desc"));
            Items.Add(new MarketItem("Turret_Vulcan_XT", ItemCategory.Turret, [1250, 13710, 52860, 139080], "https://wiki.pro-tanki.com/en/images/b/bc/Vulcan3_XT.png", "Turret_Vulcan_XT_Desc"));

            // Hulls
            Items.Add(new MarketItem("Hull_Wasp", ItemCategory.Hull, [200, 7650, 62450, 172600], "https://wiki.pro-tanki.com/en/images/b/b1/Wasp_m3.png", "Hull_Wasp_Desc"));
            Items.Add(new MarketItem("Hull_Wasp_XT", ItemCategory.Hull, [200, 4590, 37470, 103560], "https://wiki.pro-tanki.com/en/images/0/0a/Wasp_XT_m3.png", "Hull_Wasp_XT_Desc"));
            Items.Add(new MarketItem("Hull_Hornet", ItemCategory.Hull, [500, 21000, 86600, 215500], "https://wiki.pro-tanki.com/en/images/1/10/Hornet_m3.png", "Hull_Hornet_Desc"));
            Items.Add(new MarketItem("Hull_Hornet_XT", ItemCategory.Hull, [500, 12600, 51960, 129300], "https://wiki.pro-tanki.com/en/images/3/36/Hornet_XT_m3.png", "Hull_Hornet_XT_Desc"));
            Items.Add(new MarketItem("Hull_Hunter", ItemCategory.Hull, [0, 3200, 54400, 158300], "https://wiki.pro-tanki.com/en/images/3/3d/Hunter_m3.png", "Hull_Hunter_Desc"));
            Items.Add(new MarketItem("Hull_Dictator", ItemCategory.Hull, [400, 16550, 78550, 201200], "https://wiki.pro-tanki.com/en/images/a/ae/Dictator_m3.png", "Hull_Dictator_Desc"));
            Items.Add(new MarketItem("Hull_Viking", ItemCategory.Hull, [700, 29900, 102700, 244200], "https://wiki.pro-tanki.com/en/images/d/d0/Viking_m3.png", "Hull_Viking_Desc"));
            Items.Add(new MarketItem("Hull_Viking_XT", ItemCategory.Hull, [700, 17940, 61620, 146520], "https://wiki.pro-tanki.com/en/images/7/7b/Viking_XT_m3.png", "Hull_Viking_XT_Desc"));
            Items.Add(new MarketItem("Hull_Titan", ItemCategory.Hull, [300, 12100, 70500, 187500], "https://wiki.pro-tanki.com/en/images/5/58/Titan_m3.png", "Hull_Titan_Desc"));
            Items.Add(new MarketItem("Hull_Mammoth", ItemCategory.Hull, [600, 25450, 94650, 229900], "https://wiki.pro-tanki.com/en/images/6/62/Mammoth_m3.png", "Hull_Mammoth_Desc"));
            Items.Add(new MarketItem("Hull_Mammoth_XT", ItemCategory.Hull, [600, 15270, 56790, 137940], "https://wiki.pro-tanki.com/en/images/c/ce/Mammoth3_XT.png", "Hull_Mammoth_XT_Desc"));

            // Paints
            Items.Add(new MarketItem("Paint_Green", ItemCategory.Paint, [0], "https://wiki.pro-tanki.com/en/images/3/37/Green_paint.png", "Paint_Green_Desc"));
            Items.Add(new MarketItem("Paint_Holiday", ItemCategory.Paint, [0], "https://wiki.pro-tanki.com/en/images/4/4e/Holiday_paint.png", "Paint_Holiday_Desc"));
            Items.Add(new MarketItem("Paint_Red", ItemCategory.Paint, [100], "https://wiki.pro-tanki.com/en/images/e/ef/Red_paint.png", "Paint_Red_Desc"));
            Items.Add(new MarketItem("Paint_Blue", ItemCategory.Paint, [100], "https://wiki.pro-tanki.com/en/images/7/70/Blue_paint.png", "Paint_Blue_Desc"));
            Items.Add(new MarketItem("Paint_Black", ItemCategory.Paint, [100], "https://wiki.pro-tanki.com/en/images/7/7d/Black_paint.png", "Paint_Black_Desc"));
            Items.Add(new MarketItem("Paint_White", ItemCategory.Paint, [100], "https://wiki.pro-tanki.com/en/images/4/43/White_paint.png", "Paint_White_Desc"));
            Items.Add(new MarketItem("Paint_Orange", ItemCategory.Paint, [100], "https://wiki.pro-tanki.com/en/images/3/33/Orange_paint.png", "Paint_Orange_Desc"));
            Items.Add(new MarketItem("Paint_Flora", ItemCategory.Paint, [500], "https://wiki.pro-tanki.com/en/images/6/6f/Flora_paint.png", "Paint_Flora_Desc"));
            Items.Add(new MarketItem("Paint_Marine", ItemCategory.Paint, [500], "https://wiki.pro-tanki.com/en/images/7/70/Marine_paint.png", "Paint_Marine_Desc"));
            Items.Add(new MarketItem("Paint_Swamp", ItemCategory.Paint, [900], "https://wiki.pro-tanki.com/en/images/d/d8/Swamp_paint.png", "Paint_Swamp_Desc"));
            Items.Add(new MarketItem("Paint_Forester", ItemCategory.Paint, [1350], "https://wiki.pro-tanki.com/en/images/d/d6/Forester_paint.png", "Paint_Forester_Desc"));
            Items.Add(new MarketItem("Paint_Magma", ItemCategory.Paint, [1350], "https://wiki.pro-tanki.com/en/images/3/3d/Magma_paint.png", "Paint_Magma_Desc"));
            Items.Add(new MarketItem("Paint_Safari", ItemCategory.Paint, [1750], "https://wiki.pro-tanki.com/en/images/a/a5/Safari_paint.png", "Paint_Safari_Desc"));
            Items.Add(new MarketItem("Paint_Invader", ItemCategory.Paint, [1750], "https://wiki.pro-tanki.com/en/images/7/78/Invader_paint.png", "Paint_Invader_Desc"));
            Items.Add(new MarketItem("Paint_Metallic", ItemCategory.Paint, [2100], "https://wiki.pro-tanki.com/en/images/5/59/Metallic_paint.png", "Paint_Metallic_Desc"));
            Items.Add(new MarketItem("Paint_Lava", ItemCategory.Paint, [2100], "https://wiki.pro-tanki.com/en/images/e/ed/Lava_paint.png", "Paint_Lava_Desc"));
            Items.Add(new MarketItem("Paint_Dragon", ItemCategory.Paint, [2500], "https://wiki.pro-tanki.com/en/images/1/14/Dragon_paint.png", "Paint_Dragon_Desc"));
            Items.Add(new MarketItem("Paint_Lead", ItemCategory.Paint, [3000], "https://wiki.pro-tanki.com/en/images/9/93/Lead_paint.png", "Paint_Lead_Desc"));
            Items.Add(new MarketItem("Paint_Mary", ItemCategory.Paint, [6500], "https://wiki.pro-tanki.com/en/images/6/69/Mary_paint.png", "Paint_Mary_Desc"));
            Items.Add(new MarketItem("Paint_Carbon", ItemCategory.Paint, [6500], "https://wiki.pro-tanki.com/en/images/b/b1/Carbon_paint.png", "Paint_Carbon_Desc"));
            Items.Add(new MarketItem("Paint_Roger", ItemCategory.Paint, [12500], "https://wiki.pro-tanki.com/en/images/0/03/Roger_paint.png", "Paint_Roger_Desc"));
            Items.Add(new MarketItem("Paint_Fracture", ItemCategory.Paint, [12500], "https://wiki.pro-tanki.com/en/images/7/70/Fracture_paint.png", "Paint_Fracture_Desc"));
            Items.Add(new MarketItem("Paint_Vortex", ItemCategory.Paint, [12500], "https://wiki.pro-tanki.com/en/images/d/dc/Vortex_paint.png", "Paint_Vortex_Desc"));
            Items.Add(new MarketItem("Paint_Chainmail", ItemCategory.Paint, [18500], "https://wiki.pro-tanki.com/en/images/2/27/Chainmail_paint.png", "Paint_Chainmail_Desc"));
            Items.Add(new MarketItem("Paint_Corrosion", ItemCategory.Paint, [18500], "https://wiki.pro-tanki.com/en/images/c/c7/Corrosion_paint.png", "Paint_Corrosion_Desc"));
            Items.Add(new MarketItem("Paint_Tundra", ItemCategory.Paint, [18500], "https://wiki.pro-tanki.com/en/images/e/e1/Tundra_paint.png", "Paint_Tundra_Desc"));
            Items.Add(new MarketItem("Paint_Alien", ItemCategory.Paint, [24500], "https://wiki.pro-tanki.com/en/images/8/81/Alien_paint.png", "Paint_Alien_Desc"));
            Items.Add(new MarketItem("Paint_Swash", ItemCategory.Paint, [24500], "https://wiki.pro-tanki.com/en/images/0/07/Swash_paint.png", "Paint_Swash_Desc"));
            Items.Add(new MarketItem("Paint_Pixel", ItemCategory.Paint, [30500], "https://wiki.pro-tanki.com/en/images/7/7a/Pixel_paint.png", "Paint_Pixel_Desc"));
            Items.Add(new MarketItem("Paint_Guerrilla", ItemCategory.Paint, [30500], "https://wiki.pro-tanki.com/en/images/f/f9/Guerrilla_paint.png", "Paint_Guerrilla_Desc"));
            Items.Add(new MarketItem("Paint_Cedar", ItemCategory.Paint, [30500], "https://wiki.pro-tanki.com/en/images/3/33/Cedar_paint.png", "Paint_Cedar_Desc"));
            Items.Add(new MarketItem("Paint_InLove", ItemCategory.Paint, [30500], "https://wiki.pro-tanki.com/en/images/d/da/In_love_paint.png", "Paint_InLove_Desc"));
            Items.Add(new MarketItem("Paint_Desert", ItemCategory.Paint, [36500], "https://wiki.pro-tanki.com/en/images/1/18/Desert_paint.png", "Paint_Desert_Desc"));
            Items.Add(new MarketItem("Paint_Dirty", ItemCategory.Paint, [42500], "https://wiki.pro-tanki.com/en/images/2/28/Dirty_paint.png", "Paint_Dirty_Desc"));
            Items.Add(new MarketItem("Paint_Jaguar", ItemCategory.Paint, [42500], "https://wiki.pro-tanki.com/en/images/c/cd/Jaguar_paint.png", "Paint_Jaguar_Desc"));
            Items.Add(new MarketItem("Paint_Savanna", ItemCategory.Paint, [50500], "https://wiki.pro-tanki.com/en/images/2/27/Savanna_paint.png", "Paint_Savanna_Desc"));
            Items.Add(new MarketItem("Paint_Loam", ItemCategory.Paint, [50500], "https://wiki.pro-tanki.com/en/images/a/af/Loam_paint.png", "Paint_Loam_Desc"));
            Items.Add(new MarketItem("Paint_Sakura", ItemCategory.Paint, [50500], "https://wiki.pro-tanki.com/en/images/8/8f/Sakura_paint.png", "Paint_Sakura_Desc"));
            Items.Add(new MarketItem("Paint_Urban", ItemCategory.Paint, [63500], "https://wiki.pro-tanki.com/en/images/6/63/Urban_paint.png", "Paint_Urban_Desc"));
            Items.Add(new MarketItem("Paint_Atom", ItemCategory.Paint, [76600], "https://wiki.pro-tanki.com/en/images/d/da/Atom_paint.png", "Paint_Atom_Desc"));
            Items.Add(new MarketItem("Paint_Digital", ItemCategory.Paint, [76600], "https://wiki.pro-tanki.com/en/images/3/32/Digital_paint.png", "Paint_Digital_Desc"));
            Items.Add(new MarketItem("Paint_Hohloma", ItemCategory.Paint, [76600], "https://wiki.pro-tanki.com/en/images/a/a4/Hohloma_paint.png", "Paint_Hohloma_Desc"));
            Items.Add(new MarketItem("Paint_Rhino", ItemCategory.Paint, [90000], "https://wiki.pro-tanki.com/en/images/d/d9/Rhino_paint.png", "Paint_Rhino_Desc"));
            Items.Add(new MarketItem("Paint_Electra", ItemCategory.Paint, [90000], "https://wiki.pro-tanki.com/en/images/f/fc/Electra_paint.png", "Paint_Electra_Desc"));
            Items.Add(new MarketItem("Paint_Cherry", ItemCategory.Paint, [103000], "https://wiki.pro-tanki.com/en/images/2/23/Cherry_paint.png", "Paint_Cherry_Desc"));
            Items.Add(new MarketItem("Paint_Blacksmith", ItemCategory.Paint, [103000], "https://wiki.pro-tanki.com/en/images/9/9f/Blacksmith_paint.png", "Paint_Blacksmith_Desc"));
            Items.Add(new MarketItem("Paint_Rustle", ItemCategory.Paint, [103000], "https://wiki.pro-tanki.com/en/images/2/2d/Rustle_paint.png", "Paint_Rustle_Desc"));
            Items.Add(new MarketItem("Paint_Python", ItemCategory.Paint, [103000], "https://wiki.pro-tanki.com/en/images/a/a9/Python_paint.png", "Paint_Python_Desc"));
            Items.Add(new MarketItem("Paint_Sandstone", ItemCategory.Paint, [116000], "https://wiki.pro-tanki.com/en/images/7/71/Sandstone_paint.png", "Paint_Sandstone_Desc"));
            Items.Add(new MarketItem("Paint_Spark", ItemCategory.Paint, [116000], "https://wiki.pro-tanki.com/en/images/3/30/Spark_paint.png", "Paint_Spark_Desc"));
            Items.Add(new MarketItem("Paint_Winter", ItemCategory.Paint, [129000], "https://wiki.pro-tanki.com/en/images/3/3f/Winter_paint.png", "Paint_Winter_Desc"));
            Items.Add(new MarketItem("Paint_Needle", ItemCategory.Paint, [180000], "https://wiki.pro-tanki.com/en/images/a/a3/Needle_paint.png", "Paint_Needle_Desc"));
            Items.Add(new MarketItem("Paint_Zeus", ItemCategory.Paint, [180000], "https://wiki.pro-tanki.com/en/images/f/f5/Zeus_paint.png", "Paint_Zeus_Desc"));
            Items.Add(new MarketItem("Paint_Hive", ItemCategory.Paint, [192000], "https://wiki.pro-tanki.com/en/images/d/d2/Hive_paint.png", "Paint_Hive_Desc"));
            Items.Add(new MarketItem("Paint_Rock", ItemCategory.Paint, [192000], "https://wiki.pro-tanki.com/en/images/7/76/Rock_paint.png", "Paint_Rock_Desc"));
            Items.Add(new MarketItem("Paint_Mars", ItemCategory.Paint, [204000], "https://wiki.pro-tanki.com/en/images/3/36/Mars_paint.png", "Paint_Mars_Desc"));
            Items.Add(new MarketItem("Paint_Prodigi", ItemCategory.Paint, [204000], "https://wiki.pro-tanki.com/en/images/6/69/Prodigi_paint.png", "Paint_Prodigi_Desc"));
            Items.Add(new MarketItem("Paint_Graffiti", ItemCategory.Paint, [216000], "https://wiki.pro-tanki.com/en/images/5/55/Graffiti_paint.png", "Paint_Graffiti_Desc"));
            Items.Add(new MarketItem("Paint_Irbis", ItemCategory.Paint, [216000], "https://wiki.pro-tanki.com/en/images/5/55/Irbis_paint.png", "Paint_Irbis_Desc"));
            Items.Add(new MarketItem("Paint_Mirage", ItemCategory.Paint, [216000], "https://wiki.pro-tanki.com/en/images/6/64/Coloring_mirage.png", "Paint_Mirage_Desc"));
            Items.Add(new MarketItem("Paint_Emerald", ItemCategory.Paint, [228000], "https://wiki.pro-tanki.com/en/images/a/a2/Emerald_paint.png", "Paint_Emerald_Desc"));
            Items.Add(new MarketItem("Paint_Inferno", ItemCategory.Paint, [228000], "https://wiki.pro-tanki.com/en/images/8/89/Inferno_paint.png", "Paint_Inferno_Desc"));
            Items.Add(new MarketItem("Paint_Nano", ItemCategory.Paint, [240000], "https://wiki.pro-tanki.com/en/images/1/15/Nano_paint.png", "Paint_Nano_Desc"));
            Items.Add(new MarketItem("Paint_Raccoon", ItemCategory.Paint, [240000], "https://wiki.pro-tanki.com/en/images/7/71/Raccoon_paint.png", "Paint_Raccoon_Desc"));
            Items.Add(new MarketItem("Paint_Clay", ItemCategory.Paint, [240000], "https://wiki.pro-tanki.com/en/images/2/2d/Clay_paint.png", "Paint_Clay_Desc"));
            Items.Add(new MarketItem("Paint_Taiga", ItemCategory.Paint, [240000], "https://wiki.pro-tanki.com/en/images/3/31/Taiga_paint.png", "Paint_Taiga_Desc"));
            Items.Add(new MarketItem("Paint_Tiger", ItemCategory.Paint, [250000], "https://wiki.pro-tanki.com/en/images/5/5a/Tiger_paint.png", "Paint_Tiger_Desc"));
            Items.Add(new MarketItem("Paint_Jade", ItemCategory.Paint, [250000], "https://wiki.pro-tanki.com/en/images/3/35/Jade_paint.png", "Paint_Jade_Desc"));
            Items.Add(new MarketItem("Paint_Picasso", ItemCategory.Paint, [250000], "https://wiki.pro-tanki.com/en/images/f/f7/Picasso_paint.png", "Paint_Picasso_Desc"));
            Items.Add(new MarketItem("Paint_Lumberjack", ItemCategory.Paint, [250000], "https://wiki.pro-tanki.com/en/images/a/ad/Lumberjack_paint.png", "Paint_Lumberjack_Desc"));
            Items.Add(new MarketItem("Paint_Africa", ItemCategory.Paint, [250000], "https://wiki.pro-tanki.com/en/images/8/81/Africa_paint.png", "Paint_Africa_Desc"));

            // Supplies
            Items.Add(new MarketItem("Supply_RepairKit", ItemCategory.Supplies, [150], "https://wiki.pro-tanki.com/en/images/8/8f/Inventory_first_aid_.png", "Supply_RepairKit_Desc"));
            Items.Add(new MarketItem("Supply_DoubleArmor", ItemCategory.Supplies, [50], "https://wiki.pro-tanki.com/en/images/5/55/Inventory_double_armor.png", "Supply_DoubleArmor_Desc"));
            Items.Add(new MarketItem("Supply_DoubleDamage", ItemCategory.Supplies, [50], "https://wiki.pro-tanki.com/en/images/b/be/Inventory_double_power.png", "Supply_DoubleDamage_Desc"));
            Items.Add(new MarketItem("Supply_SpeedBoost", ItemCategory.Supplies, [50], "https://wiki.pro-tanki.com/en/images/1/12/Inventory_nitro.png", "Supply_SpeedBoost_Desc"));
            Items.Add(new MarketItem("Supply_Mine", ItemCategory.Supplies, [50], "https://wiki.pro-tanki.com/en/images/e/eb/Inventory_mine.png", "Supply_Mine_Desc"));
        }

        private void InitializeProductKits()
        {
            Items.Add(new MarketItem("ProductKit_Supplier", ItemCategory.ProductKit, [1215], "https://wiki.pro-tanki.com/en/images/f/f4/Supplier.png", "ProductKit_Supplier_Desc"));
            Items.Add(new MarketItem("ProductKit_Collector", ItemCategory.ProductKit, [4440], "https://wiki.pro-tanki.com/en/images/a/a3/Collector.png", "ProductKit_Collector_Desc"));
            Items.Add(new MarketItem("ProductKit_FullLoad", ItemCategory.ProductKit, [4830], "https://wiki.pro-tanki.com/en/images/e/ea/FullLoad.png", "ProductKit_FullLoad_Desc"));
            Items.Add(new MarketItem("ProductKit_Saboteur", ItemCategory.ProductKit, [382], "https://wiki.pro-tanki.com/en/images/b/b3/Saboteur.png", "ProductKit_Saboteur_Desc"));
            Items.Add(new MarketItem("ProductKit_Pyromancer", ItemCategory.ProductKit, [427], "https://wiki.pro-tanki.com/en/images/7/78/Pyromancer.png", "ProductKit_Pyromancer_Desc"));
            Items.Add(new MarketItem("ProductKit_ATV", ItemCategory.ProductKit, [760], "https://wiki.pro-tanki.com/en/images/d/db/ATV.png", "ProductKit_ATV_Desc"));
            Items.Add(new MarketItem("ProductKit_Almighty", ItemCategory.ProductKit, [742], "https://wiki.pro-tanki.com/en/images/f/fe/Almighty.png", "ProductKit_Almighty_Desc"));
            Items.Add(new MarketItem("ProductKit_Mosquito", ItemCategory.ProductKit, [750], "https://wiki.pro-tanki.com/en/images/8/8a/Mosquito.png", "ProductKit_Mosquito_Desc"));
            Items.Add(new MarketItem("ProductKit_Cadet", ItemCategory.ProductKit, [1192], "https://wiki.pro-tanki.com/en/images/c/c4/Cadet.png", "ProductKit_Cadet_Desc"));
            Items.Add(new MarketItem("ProductKit_Nidavellir", ItemCategory.ProductKit, [800], "https://wiki.pro-tanki.com/en/images/8/8e/Nidavellir.png", "ProductKit_Nidavellir_Desc"));
            Items.Add(new MarketItem("ProductKit_Anvil", ItemCategory.ProductKit, [1147], "https://wiki.pro-tanki.com/en/images/d/dd/Anvil.png", "ProductKit_Anvil_Desc"));
            Items.Add(new MarketItem("ProductKit_Ant", ItemCategory.ProductKit, [1175], "https://wiki.pro-tanki.com/en/images/5/56/Ant.png", "ProductKit_Ant_Desc"));
            Items.Add(new MarketItem("ProductKit_FieldMedic", ItemCategory.ProductKit, [1575], "https://wiki.pro-tanki.com/en/images/e/ec/FieldMedic.png", "ProductKit_FieldMedic_Desc"));
            Items.Add(new MarketItem("ProductKit_Reaper", ItemCategory.ProductKit, [1025], "https://wiki.pro-tanki.com/en/images/6/6a/Reaper.png", "ProductKit_Reaper_Desc"));
            Items.Add(new MarketItem("ProductKit_Geyser", ItemCategory.ProductKit, [1530], "https://wiki.pro-tanki.com/en/images/5/5a/Geyser.png", "ProductKit_Geyser_Desc"));
            Items.Add(new MarketItem("ProductKit_Horn", ItemCategory.ProductKit, [1777], "https://wiki.pro-tanki.com/en/images/5/58/Horn.png", "ProductKit_Horn_Desc"));
            Items.Add(new MarketItem("ProductKit_Grizzly", ItemCategory.ProductKit, [1700], "https://wiki.pro-tanki.com/en/images/9/9d/Grizzly.png", "ProductKit_Grizzly_Desc"));
            Items.Add(new MarketItem("ProductKit_Extinguisher", ItemCategory.ProductKit, [1665], "https://wiki.pro-tanki.com/en/images/0/04/Extinguisher.png", "ProductKit_Extinguisher_Desc"));
            Items.Add(new MarketItem("ProductKit_Berserker", ItemCategory.ProductKit, [1912], "https://wiki.pro-tanki.com/en/images/a/a8/Berserker.png", "ProductKit_Berserker_Desc"));
            Items.Add(new MarketItem("ProductKit_Micro", ItemCategory.ProductKit, [1980], "https://wiki.pro-tanki.com/en/images/3/3f/Micro.png", "ProductKit_Micro_Desc"));
            Items.Add(new MarketItem("ProductKit_Commando", ItemCategory.ProductKit, [2205], "https://wiki.pro-tanki.com/en/images/5/50/Commando.png", "ProductKit_Commando_Desc"));
            Items.Add(new MarketItem("ProductKit_Chameleon", ItemCategory.ProductKit, [2205], "https://wiki.pro-tanki.com/en/images/f/f5/Chameleon.png", "ProductKit_Chameleon_Desc"));
            Items.Add(new MarketItem("ProductKit_Liquidator", ItemCategory.ProductKit, [3645], "https://wiki.pro-tanki.com/en/images/c/cc/Liquidator.png", "ProductKit_Liquidator_Desc"));
            Items.Add(new MarketItem("ProductKit_Auditor", ItemCategory.ProductKit, [7865], "https://wiki.pro-tanki.com/en/images/0/0a/Auditor.png", "ProductKit_Auditor_Desc"));
            Items.Add(new MarketItem("ProductKit_Razor", ItemCategory.ProductKit, [9240], "https://wiki.pro-tanki.com/en/images/a/a0/Razor.png", "ProductKit_Razor_Desc"));
            Items.Add(new MarketItem("ProductKit_Nord", ItemCategory.ProductKit, [9240], "https://wiki.pro-tanki.com/en/images/2/24/Nord.png", "ProductKit_Nord_Desc"));
            Items.Add(new MarketItem("ProductKit_Firefly", ItemCategory.ProductKit, [13625], "https://wiki.pro-tanki.com/en/images/d/d7/Firefly.png", "ProductKit_Firefly_Desc"));
            Items.Add(new MarketItem("ProductKit_Flagship", ItemCategory.ProductKit, [17022], "https://wiki.pro-tanki.com/en/images/4/47/Flagship.png", "ProductKit_Flagship_Desc"));
            Items.Add(new MarketItem("ProductKit_Jester", ItemCategory.ProductKit, [22770], "https://wiki.pro-tanki.com/en/images/e/e0/Jester.png", "ProductKit_Jester_Desc"));
            Items.Add(new MarketItem("ProductKit_Snooker", ItemCategory.ProductKit, [18315], "https://wiki.pro-tanki.com/en/images/9/91/Snooker.png", "ProductKit_Snooker_Desc"));
            Items.Add(new MarketItem("ProductKit_Marksman", ItemCategory.ProductKit, [31405], "https://wiki.pro-tanki.com/en/images/a/a0/Marksman.png", "ProductKit_Marksman_Desc"));
            Items.Add(new MarketItem("ProductKit_Warhammer", ItemCategory.ProductKit, [28957], "https://wiki.pro-tanki.com/en/images/a/a5/Warhammer.png", "ProductKit_Warhammer_Desc"));
            Items.Add(new MarketItem("ProductKit_Gnome", ItemCategory.ProductKit, [30660], "https://wiki.pro-tanki.com/en/images/7/78/Gnome.png", "ProductKit_Gnome_Desc"));
            Items.Add(new MarketItem("ProductKit_Val", ItemCategory.ProductKit, [32697], "https://wiki.pro-tanki.com/en/images/f/fc/Val.png", "ProductKit_Val_Desc"));
            Items.Add(new MarketItem("ProductKit_Outlander", ItemCategory.ProductKit, [37592], "https://wiki.pro-tanki.com/en/images/d/d4/Outlander.png", "ProductKit_Outlander_Desc"));
            Items.Add(new MarketItem("ProductKit_Yellowstone", ItemCategory.ProductKit, [29397], "https://wiki.pro-tanki.com/en/images/e/e2/Yellowstone.png", "ProductKit_Yellowstone_Desc"));
            Items.Add(new MarketItem("ProductKit_Obliterator", ItemCategory.ProductKit, [34950], "https://wiki.pro-tanki.com/en/images/e/e1/Obliterator.png", "ProductKit_Obliterator_Desc"));
            Items.Add(new MarketItem("ProductKit_Nutcracker", ItemCategory.ProductKit, [41332], "https://wiki.pro-tanki.com/en/images/3/38/Nutcracker.png", "ProductKit_Nutcracker_Desc"));
            Items.Add(new MarketItem("ProductKit_Destroyer", ItemCategory.ProductKit, [46227], "https://wiki.pro-tanki.com/en/images/3/3a/Destroyer.png", "ProductKit_Destroyer_Desc"));
            Items.Add(new MarketItem("ProductKit_Blizzard", ItemCategory.ProductKit, [33360], "https://wiki.pro-tanki.com/en/images/3/39/Blizzard.png", "ProductKit_Blizzard_Desc"));
            Items.Add(new MarketItem("ProductKit_Cupid", ItemCategory.ProductKit, [48675], "https://wiki.pro-tanki.com/en/images/0/06/Cupid.png", "ProductKit_Cupid_Desc"));
            Items.Add(new MarketItem("ProductKit_Cyberpsycho", ItemCategory.ProductKit, [44220], "https://wiki.pro-tanki.com/en/images/8/8e/Cyberpsycho.png", "ProductKit_Cyberpsycho_Desc"));
            Items.Add(new MarketItem("ProductKit_Abrams", ItemCategory.ProductKit, [52415], "https://wiki.pro-tanki.com/en/images/e/e0/Abrams.png", "ProductKit_Abrams_Desc"));
            Items.Add(new MarketItem("ProductKit_Piranha", ItemCategory.ProductKit, [48860], "https://wiki.pro-tanki.com/en/images/4/43/Piranha.png", "ProductKit_Piranha_Desc"));
            Items.Add(new MarketItem("ProductKit_TheEnd", ItemCategory.ProductKit, [74525], "https://wiki.pro-tanki.com/en/images/7/72/TheEnd.png", "ProductKit_TheEnd_Desc"));
            Items.Add(new MarketItem("ProductKit_Striker", ItemCategory.ProductKit, [75270], "https://wiki.pro-tanki.com/en/images/2/22/Striker.png", "ProductKit_Striker_Desc"));
            Items.Add(new MarketItem("ProductKit_Native", ItemCategory.ProductKit, [99780], "https://wiki.pro-tanki.com/en/images/4/4c/Native.png", "ProductKit_Native_Desc"));
            Items.Add(new MarketItem("ProductKit_Potter", ItemCategory.ProductKit, [85080], "https://wiki.pro-tanki.com/en/images/b/bb/Potter.png", "ProductKit_Potter_Desc"));
            Items.Add(new MarketItem("ProductKit_Flare", ItemCategory.ProductKit, [110247], "https://wiki.pro-tanki.com/en/images/5/54/Flare.png", "ProductKit_Flare_Desc"));
            Items.Add(new MarketItem("ProductKit_Vanguard", ItemCategory.ProductKit, [112920], "https://wiki.pro-tanki.com/en/images/f/fd/Vanguard.png", "ProductKit_Vanguard_Desc"));
            Items.Add(new MarketItem("ProductKit_Moose", ItemCategory.ProductKit, [131367], "https://wiki.pro-tanki.com/en/images/2/27/Moose.png", "ProductKit_Moose_Desc"));
            Items.Add(new MarketItem("ProductKit_Rivet", ItemCategory.ProductKit, [134557], "https://wiki.pro-tanki.com/en/images/e/e8/Rivet.png", "ProductKit_Rivet_Desc"));
            Items.Add(new MarketItem("ProductKit_Tornado", ItemCategory.ProductKit, [145440], "https://wiki.pro-tanki.com/en/images/4/40/Tornado.png", "ProductKit_Tornado_Desc"));
            Items.Add(new MarketItem("ProductKit_Sentinel", ItemCategory.ProductKit, [131835], "https://wiki.pro-tanki.com/en/images/9/91/Sentinel.png", "ProductKit_Sentinel_Desc"));
            Items.Add(new MarketItem("ProductKit_Firestorm", ItemCategory.ProductKit, [145440], "https://wiki.pro-tanki.com/en/images/9/96/Firestorm.png", "ProductKit_Firestorm_Desc"));
            Items.Add(new MarketItem("ProductKit_Voltage", ItemCategory.ProductKit, [153990], "https://wiki.pro-tanki.com/en/images/e/e2/Voltage.png", "ProductKit_Voltage_Desc"));
            Items.Add(new MarketItem("ProductKit_Osaka", ItemCategory.ProductKit, [146380], "https://wiki.pro-tanki.com/en/images/f/fd/Osaka.png", "ProductKit_Osaka_Desc"));
            Items.Add(new MarketItem("ProductKit_Olympus", ItemCategory.ProductKit, [141147], "https://wiki.pro-tanki.com/en/images/4/4d/Olympus.png", "ProductKit_Olympus_Desc"));
            Items.Add(new MarketItem("ProductKit_Terminator", ItemCategory.ProductKit, [163650], "https://wiki.pro-tanki.com/en/images/8/81/Terminator.png", "ProductKit_Terminator_Desc"));
            Items.Add(new MarketItem("ProductKit_Simoom", ItemCategory.ProductKit, [160352], "https://wiki.pro-tanki.com/en/images/f/f0/Simoom.png", "ProductKit_Simoom_Desc"));
            Items.Add(new MarketItem("ProductKit_Oldtimer", ItemCategory.ProductKit, [181620], "https://wiki.pro-tanki.com/en/images/0/0c/Oldtimer.png", "ProductKit_Oldtimer_Desc"));
            Items.Add(new MarketItem("ProductKit_Herbarium", ItemCategory.ProductKit, [167130], "https://wiki.pro-tanki.com/en/images/c/c9/Herbarium.png", "ProductKit_Herbarium_Desc"));
            Items.Add(new MarketItem("ProductKit_Avalanche", ItemCategory.ProductKit, [171960], "https://wiki.pro-tanki.com/en/images/e/ed/Avalanche.png", "ProductKit_Avalanche_Desc"));
            Items.Add(new MarketItem("ProductKit_Goalie", ItemCategory.ProductKit, [189930], "https://wiki.pro-tanki.com/en/images/c/cf/Goalie.png", "ProductKit_Goalie_Desc"));
            Items.Add(new MarketItem("ProductKit_Juggler", ItemCategory.ProductKit, [216260], "https://wiki.pro-tanki.com/en/images/b/be/Juggler.png", "ProductKit_Juggler_Desc"));
            Items.Add(new MarketItem("ProductKit_Perun", ItemCategory.ProductKit, [209770], "https://wiki.pro-tanki.com/en/images/1/1d/Perun.png", "ProductKit_Perun_Desc"));
            Items.Add(new MarketItem("ProductKit_Bonecracker", ItemCategory.ProductKit, [207900], "https://wiki.pro-tanki.com/en/images/3/3a/Bonecracker.png", "ProductKit_Bonecracker_Desc"));
            Items.Add(new MarketItem("ProductKit_Varangian", ItemCategory.ProductKit, [292240], "https://wiki.pro-tanki.com/en/images/f/f9/Varangian.png", "ProductKit_Varangian_Desc"));
            Items.Add(new MarketItem("ProductKit_Warden", ItemCategory.ProductKit, [327540], "https://wiki.pro-tanki.com/en/images/a/a5/Warden.png", "ProductKit_Warden_Desc"));
            Items.Add(new MarketItem("ProductKit_Prometheus", ItemCategory.ProductKit, [335400], "https://wiki.pro-tanki.com/en/images/6/63/Prometheus.png", "ProductKit_Prometheus_Desc"));
            Items.Add(new MarketItem("ProductKit_Bolt", ItemCategory.ProductKit, [335510], "https://wiki.pro-tanki.com/en/images/8/8e/Bolt.png", "ProductKit_Bolt_Desc"));
            Items.Add(new MarketItem("ProductKit_Cardinal", ItemCategory.ProductKit, [368875], "https://wiki.pro-tanki.com/en/images/d/d2/Cardinal.png", "ProductKit_Cardinal_Desc"));
            Items.Add(new MarketItem("ProductKit_Bedrock", ItemCategory.ProductKit, [378105], "https://wiki.pro-tanki.com/en/images/e/ee/Bedrock.png", "ProductKit_Bedrock_Desc"));
            Items.Add(new MarketItem("ProductKit_Touché", ItemCategory.ProductKit, [386330], "https://wiki.pro-tanki.com/en/images/d/d5/Touche.png", "ProductKit_Touché_Desc"));
            Items.Add(new MarketItem("ProductKit_Legend", ItemCategory.ProductKit, [433160], "https://wiki.pro-tanki.com/en/images/1/10/Legend.png", "ProductKit_Legend_Desc"));
            Items.Add(new MarketItem("ProductKit_Meteor", ItemCategory.ProductKit, [372060], "https://wiki.pro-tanki.com/en/images/9/99/Meteor.png", "ProductKit_Meteor_Desc"));
            Items.Add(new MarketItem("ProductKit_Cerberus", ItemCategory.ProductKit, [392145], "https://wiki.pro-tanki.com/en/images/4/45/Cerberus.png", "ProductKit_Cerberus_Desc"));
            Items.Add(new MarketItem("ProductKit_Lifeline", ItemCategory.ProductKit, [424125], "https://wiki.pro-tanki.com/en/images/3/3d/Lifeline.png", "ProductKit_Lifeline_Desc"));
            Items.Add(new MarketItem("ProductKit_Seal", ItemCategory.ProductKit, [433485], "https://wiki.pro-tanki.com/en/images/8/8e/Seal.png", "ProductKit_Seal_Desc"));
            Items.Add(new MarketItem("ProductKit_Scholar", ItemCategory.ProductKit, [403800], "https://wiki.pro-tanki.com/en/images/c/c5/Scholar.png", "ProductKit_Scholar_Desc"));
            Items.Add(new MarketItem("ProductKit_Eyjafjallajokull", ItemCategory.ProductKit, [427020], "https://wiki.pro-tanki.com/en/images/5/56/Eyjafjallajkull.png", "ProductKit_Eyjafjallajokull_Desc"));
            Items.Add(new MarketItem("ProductKit_Dozer", ItemCategory.ProductKit, [433440], "https://wiki.pro-tanki.com/en/images/1/1e/Dozer.png", "ProductKit_Dozer_Desc"));
            Items.Add(new MarketItem("ProductKit_Raiden", ItemCategory.ProductKit, [464555], "https://wiki.pro-tanki.com/en/images/5/5f/Raiden.png", "ProductKit_Raiden_Desc"));
            Items.Add(new MarketItem("ProductKit_Sprinter", ItemCategory.ProductKit, [425280], "https://wiki.pro-tanki.com/en/images/0/06/Sprinter.png", "ProductKit_Sprinter_Desc"));
            Items.Add(new MarketItem("ProductKit_Peltier", ItemCategory.ProductKit, [471575], "https://wiki.pro-tanki.com/en/images/1/16/Peltier.png", "ProductKit_Peltier_Desc"));
            Items.Add(new MarketItem("ProductKit_Codebreaker", ItemCategory.ProductKit, [446460], "https://wiki.pro-tanki.com/en/images/c/cd/Codebreaker.png", "ProductKit_Codebreaker_Desc"));
            Items.Add(new MarketItem("ProductKit_Drugger", ItemCategory.ProductKit, [486460], "https://wiki.pro-tanki.com/en/images/1/10/Drugger.png", "ProductKit_Drugger_Desc"));
            Items.Add(new MarketItem("ProductKit_Centaur", ItemCategory.ProductKit, [481325], "https://wiki.pro-tanki.com/en/images/b/bc/Centaur.png", "ProductKit_Centaur_Desc"));
            Items.Add(new MarketItem("ProductKit_Sniper", ItemCategory.ProductKit, [449980], "https://wiki.pro-tanki.com/en/images/8/8f/Sniper.png", "ProductKit_Sniper_Desc"));
        }

        private void InitializeSuppliesKits()
        {
            Items.Add(new MarketItem("SupplyKit_100", ItemCategory.SuppliesKit, [17500], "https://wiki.pro-tanki.com/en/images/7/75/Kit_supplies_100.png", "SupplyKit_100_Desc"));
            Items.Add(new MarketItem("SupplyKit_200", ItemCategory.SuppliesKit, [35000], "https://wiki.pro-tanki.com/en/images/8/85/200.png", "SupplyKit_200_Desc"));
            Items.Add(new MarketItem("SupplyKit_300", ItemCategory.SuppliesKit, [52500], "https://wiki.pro-tanki.com/en/images/c/c3/300.png", "SupplyKit_300_Desc"));
            Items.Add(new MarketItem("SupplyKit_400", ItemCategory.SuppliesKit, [63000], "https://wiki.pro-tanki.com/en/images/a/a5/400.png", "SupplyKit_400_Desc"));
            Items.Add(new MarketItem("SupplyKit_500", ItemCategory.SuppliesKit, [78750], "https://wiki.pro-tanki.com/en/images/b/be/500.png", "SupplyKit_500_Desc"));
            Items.Add(new MarketItem("SupplyKit_600", ItemCategory.SuppliesKit, [84000], "https://wiki.pro-tanki.com/en/images/4/43/600.png", "SupplyKit_600_Desc"));
            Items.Add(new MarketItem("SupplyKit_700", ItemCategory.SuppliesKit, [98000], "https://wiki.pro-tanki.com/en/images/b/bc/700.png", "SupplyKit_700_Desc"));
            Items.Add(new MarketItem("SupplyKit_800", ItemCategory.SuppliesKit, [98000], "https://wiki.pro-tanki.com/en/images/c/cf/800.png", "SupplyKit_800_Desc"));
            Items.Add(new MarketItem("SupplyKit_900", ItemCategory.SuppliesKit, [110250], "https://wiki.pro-tanki.com/en/images/9/95/900.png", "SupplyKit_900_Desc"));
            Items.Add(new MarketItem("SupplyKit_1000", ItemCategory.SuppliesKit, [105000], "https://wiki.pro-tanki.com/en/images/4/46/1000.png", "SupplyKit_1000_Desc"));
            Items.Add(new MarketItem("SupplyKit_1100", ItemCategory.SuppliesKit, [115500], "https://wiki.pro-tanki.com/en/images/b/b7/1100.png", "SupplyKit_1100_Desc"));
            Items.Add(new MarketItem("SupplyKit_1200", ItemCategory.SuppliesKit, [105000], "https://wiki.pro-tanki.com/en/images/2/22/1200.png", "SupplyKit_1200_Desc"));
            Items.Add(new MarketItem("SupplyKit_1300", ItemCategory.SuppliesKit, [113750], "https://wiki.pro-tanki.com/en/images/b/b6/1300.png", "SupplyKit_1300_Desc"));
            Items.Add(new MarketItem("SupplyKit_1500", ItemCategory.SuppliesKit, [105000], "https://wiki.pro-tanki.com/en/images/9/9f/1500.png", "SupplyKit_1500_Desc"));
        }

        public List<MarketItem> GetItemsByCategory(ItemCategory category)
        {
            return [.. Items.Where(x => x.Category == category)];
        }
    }
}
