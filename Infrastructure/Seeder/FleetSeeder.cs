using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Seeder;

/// <summary>
/// Seeds the initial fleet data:
///   - 3 RentalPolicies  (Standard · Luxury · Supercar)
///   - 8 CarCategories
///   - 3 Branches        (Riyadh · Jeddah · Dammam)
///   - 110 Cars          distributed across branches / categories / statuses
///
/// Idempotent: skips seeding if Cars table already has data.
/// </summary>
public static class FleetSeeder
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Entry Point
    // ─────────────────────────────────────────────────────────────────────────

    public static async Task SeedAsync(AppDbContext context)
    {
        // Guard: run only on empty fleet
        if (await context.Cars.AnyAsync()) return;

        var policyIds = await _SeedPoliciesAsync(context);
        var branchIds = await _SeedBranchesAsync(context);
        var categoryIds = await _SeedCategoriesAsync(context, policyIds);
        await _SeedCarsAsync(context, branchIds, categoryIds, policyIds);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  1. Rental Policies
    //  Key: "Standard" | "Luxury" | "Supercar"
    // ─────────────────────────────────────────────────────────────────────────

    private static async Task<Dictionary<string, int>> _SeedPoliciesAsync(AppDbContext context)
    {
        var policies = new List<RentalPolicy>
        {
            // ── Standard ────────────────────────────────────────────────────
            // Doc: Cancel 24 h+ → full refund | within 6 h → 25% penalty
            // Buffer 8 h = 4 h grace + 2 h clean + 2 h contingency
            new RentalPolicy
            {
                Name                         = "Standard",
                BufferHours                  = 8,
                AllowDifferentDropOff        = true,// Economy / 
                MinCancellationLeadTimeHours = 6,
                CancellationPenaltyPercent   = 25,
                NoShowPenaltyDays            = 1,
                IsActive                     = true,
                CreatedAt                    = DateTime.UtcNow
            },

            // ── Luxury ──────────────────────────────────────────────────────
            // Doc: Cancel within 48 h → 50% | No-show → 3 days
            // Buffer 16 h = 4 h grace + detailed inspection + contingency
            new RentalPolicy
            {
                Name                         = "Luxury",
                BufferHours                  = 16,
                AllowDifferentDropOff = false,// Luxury / Luxury SUV
                MinCancellationLeadTimeHours = 48,
                CancellationPenaltyPercent   = 50,
                NoShowPenaltyDays            = 3,
                IsActive                     = true,
                CreatedAt                    = DateTime.UtcNow
            },

            // ── Supercar ────────────────────────────────────────────────────
            // Premium tier: strictest terms, premium service guarantee
            // Buffer 24 h to ensure full certification + VIP preparation
            new RentalPolicy
            {
                Name                         = "Supercar",
                BufferHours                  = 24,
                AllowDifferentDropOff        = false,  // Supercar
                MinCancellationLeadTimeHours = 72,
                CancellationPenaltyPercent   = 75,
                NoShowPenaltyDays            = 5,
                IsActive                     = true,
                CreatedAt                    = DateTime.UtcNow
            }
        };

        await context.RentalPolicies.AddRangeAsync(policies);
        await context.SaveChangesAsync();

        return policies.ToDictionary(
            p => p.Name,
            p => p.Id
        );
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  2. Branches
    // ─────────────────────────────────────────────────────────────────────────

    private static async Task<Dictionary<int, int>> _SeedBranchesAsync(AppDbContext context)
    {
        var branches = new List<Branch>
        {

            new Branch { NameEN = "Riyadh Main Branch", NameAR = "فرع الرياض الرئيسي", CityEN = "Riyadh", CityAR = "الرياض", Latitude = 24.7136, Longitude = 46.6753, IsActive = true },
            new Branch { NameEN = "Jeddah Branch", NameAR = "فرع جدة", CityEN = "Jeddah", CityAR = "جدة", Latitude = 21.4858, Longitude = 39.1925, IsActive = true },
            new Branch { NameEN = "Dammam Branch", NameAR = "فرع الدمام", CityEN = "Dammam", CityAR = "الدمام", Latitude = 26.4207, Longitude = 50.0888, IsActive = true },
            new Branch { NameEN = "Makkah Branch", NameAR = "فرع مكة المكرمة", CityEN = "Makkah", CityAR = "مكة المكرمة", Latitude = 21.3891, Longitude = 39.8579, IsActive = true },
            new Branch { NameEN = "Madinah Branch", NameAR = "فرع المدينة المنورة", CityEN = "Madinah", CityAR = "المدينة المنورة", Latitude = 24.5247, Longitude = 39.5692, IsActive = true },
            new Branch { NameEN = "Abha Branch", NameAR = "فرع أبها", CityEN = "Abha", CityAR = "أبها", Latitude = 18.2164, Longitude = 42.5053, IsActive = false },
            new Branch { NameEN = "Tabuk Branch", NameAR = "فرع تبوك", CityEN = "Tabuk", CityAR = "تبوك", Latitude = 28.3838, Longitude = 36.5656, IsActive = true },
            new Branch { NameEN = "Taif Branch", NameAR = "فرع الطائف", CityEN = "Taif", CityAR = "الطائف", Latitude = 21.2703, Longitude = 40.4158, IsActive = true },
            new Branch { NameEN = "Buraydah Branch", NameAR = "فرع بريدة", CityEN = "Buraydah", CityAR = "بريدة", Latitude = 26.326, Longitude = 43.975, IsActive = false },
            new Branch { NameEN = "Hail Branch", NameAR = "فرع حائل", CityEN = "Hail", CityAR = "حائل", Latitude = 27.5236, Longitude = 41.6934, IsActive = true },
            new Branch { NameEN = "Najran Branch", NameAR = "فرع نجران", CityEN = "Najran", CityAR = "نجران", Latitude = 17.4928, Longitude = 44.1321, IsActive = true },
            new Branch { NameEN = "Jazan Branch", NameAR = "فرع جيزان", CityEN = "Jazan", CityAR = "جيزان", Latitude = 16.8892, Longitude = 42.5511, IsActive = false }
        };

        await context.Branches.AddRangeAsync(branches);
        await context.SaveChangesAsync();

        // Map order-of-insertion (1-based) → actual DB Id
        return branches
            .Select((b, i) => (Key: i + 1, b.Id))
            .ToDictionary(x => x.Key, x => x.Id);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  3. Car Categories
    //  Key: 1-8 (insertion order)
    // ─────────────────────────────────────────────────────────────────────────

    private static async Task<Dictionary<int, int>> _SeedCategoriesAsync(
        AppDbContext context,
        Dictionary<string, int> policyIds)
    {
        var std = policyIds["Standard"];
        var lux = policyIds["Luxury"];
        var sc = policyIds["Supercar"];

        var categories = new List<CarCategory>
        {
            // 1 ─ Economy
            new CarCategory
            {
                NameEN               = "Economy",
                NameAR               = "اقتصادية",
                Description          = "Budget-friendly cars with low fuel consumption, ideal for daily commutes.",
                BaseDailyRate        = 120,
                BaseWeeklyRate       = 700,
                BaseMonthlyRate      = 2500,
                IsModelSpecific      = false,
                PolicyId             = std,
                IsActive             = true,
                CreatedAt            = DateTime.UtcNow
            },

            // 2 ─ Standard / Sedan
            new CarCategory
            {
                NameEN               = "Standard",
                NameAR               = "متوسطة",
                Description          = "Balanced comfort and pricing, most popular for business and personal use.",
                BaseDailyRate        = 200,
                BaseWeeklyRate       = 1200,
                BaseMonthlyRate      = 4000,
                IsModelSpecific      = false,
                PolicyId             = std,
                IsActive             = true,
                CreatedAt            = DateTime.UtcNow
            },

            // 3 ─ SUV
            new CarCategory
            {
                NameEN               = "SUV",
                NameAR               = "عائلية",
                Description          = "Spacious family-friendly vehicles, ideal for long trips and small families.",
                BaseDailyRate        = 280,
                BaseWeeklyRate       = 1700,
                BaseMonthlyRate      = 5800,
                IsModelSpecific      = false,
                PolicyId             = std,
                IsActive             = true,
                CreatedAt            = DateTime.UtcNow
            },

            // 4 ─ Minivan
            new CarCategory
            {
                NameEN               = "Minivan",
                NameAR               = "عائلية كبيرة",
                Description          = "Maximum seating capacity (7–9 seats), perfect for large families and groups.",
                BaseDailyRate        = 320,
                BaseWeeklyRate       = 1950,
                BaseMonthlyRate      = 6500,
                IsModelSpecific      = false,
                PolicyId             = std,
                IsActive             = true,
                CreatedAt            = DateTime.UtcNow
            },

            // 5 ─ Electric / Hybrid
            new CarCategory
            {
                NameEN               = "Electric & Hybrid",
                NameAR               = "كهربائية وهجينة",
                Description          = "Eco-friendly modern vehicles targeting sustainability-conscious customers.",
                BaseDailyRate        = 250,
                BaseWeeklyRate       = 1500,
                BaseMonthlyRate      = 5000,
                IsModelSpecific      = false,
                PolicyId             = std,
                IsActive             = true,
                CreatedAt            = DateTime.UtcNow
            },

            // 6 ─ Luxury
            new CarCategory
            {
                NameEN               = "Luxury",
                NameAR               = "فاخرة",
                Description          = "Premium driving experience with top-tier brands. Model-specific booking.",
                BaseDailyRate        = 700,
                BaseWeeklyRate       = 4200,
                BaseMonthlyRate      = 14000,
                IsModelSpecific      = true,
                PolicyId             = lux,
                IsActive             = true,
                CreatedAt            = DateTime.UtcNow
            },

            // 7 ─ Luxury SUV
            new CarCategory
            {
                NameEN               = "Luxury SUV",
                NameAR               = "دفع رباعي فاخر",
                Description          = "Combines luxury and power. High insurance deposit required. Model-specific booking.",
                BaseDailyRate        = 900,
                BaseWeeklyRate       = 5400,
                BaseMonthlyRate      = 18000,
                IsModelSpecific      = true,
                PolicyId             = lux,
                IsActive             = true,
                CreatedAt            = DateTime.UtcNow
            },

            // 8 ─ Supercar
            new CarCategory
            {
                NameEN               = "Supercar",
                NameAR               = "سيارات خارقة",
                Description          = "Exclusive VIP fleet — hypercars and ultra-luxury vehicles. Strictest policy applies.",
                BaseDailyRate        = 2500,
                BaseWeeklyRate       = 15000,
                BaseMonthlyRate      = 50000,
                IsModelSpecific      = true,
                PolicyId             = sc,
                IsActive             = true,
                CreatedAt            = DateTime.UtcNow
            }
        };

        await context.CarCategories.AddRangeAsync(categories);
        await context.SaveChangesAsync();

        return categories
            .Select((c, i) => (Key: i + 1, c.Id))
            .ToDictionary(x => x.Key, x => x.Id);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  4. Cars  (110 vehicles)
    //
    //  Test coverage:
    //    ✅ 15 cars per major category  → pagination (pageSize=10 → 2 pages)
    //    ✅ 3 branches, balanced distribution
    //    ✅ Edge cases: InMaintenance, Damaged, Inactive
    //    ✅ All FuelType values
    //    ✅ Automatic & Manual transmission
    //    ✅ PolicyOverride on selected Supercars
    //    ✅ Null & non-null PolicyOverrideId
    // ─────────────────────────────────────────────────────────────────────────

    private static async Task _SeedCarsAsync(
        AppDbContext context,
        Dictionary<int, int> branchIds,
        Dictionary<int, int> categoryIds,
        Dictionary<string, int> policyIds)
    {
        var cars = new List<Car>
        {
            // ══════════════════════════════════════════════════════════════
            //  CATEGORY 1 — Economy  (15 cars · years 2023-2026)
            // ══════════════════════════════════════════════════════════════
            _Car("Ford",       "Fiesta",    2023, "GTJ 1074", "ز ف ر 1074", "ZF8B4LV66LMAKTX4Z", branchIds[1], categoryIds[1], 1, enTransmissionType.Automatic,  enFleetConditionStatus.Ready,         true,  5, 2, 1200, null),
            _Car("Nissan",     "Versa",     2024, "KAY 1111", "ط أ و 1111", "3LN4P1TFHL8A76L5L", branchIds[2], categoryIds[1], 1, enTransmissionType.Manual,      enFleetConditionStatus.Ready,         true,  5, 3, 1400, null),
            _Car("Toyota",     "Yaris",     2025, "NHM 1148", "ق ر ط 1148", "LD7C17APJ5A3GJBCN", branchIds[3], categoryIds[1], 1, enTransmissionType.Automatic,  enFleetConditionStatus.Ready,         true,  5, 2, 1600, null),
            _Car("Honda",      "Fit",       2026, "SRB 1185", "ه غ ج 1185", "V98L69ZGBPB75YMTM", branchIds[1], categoryIds[1], 2, enTransmissionType.Manual,      enFleetConditionStatus.Ready,         true,  5, 3, 1200, null),
            _Car("Hyundai",    "Accent",    2023, "VYR 1222", "ت ي ك 1222", "82Z2EAAF6X7HMBK8P", branchIds[2], categoryIds[1], 1, enTransmissionType.Automatic,  enFleetConditionStatus.Ready,         true,  5, 2, 1400, null),
            _Car("Kia",        "Rio",       2024, "YFG 1259", "ث خ ش 1259", "5TBBH3X8H6MH5JPY5", branchIds[3], categoryIds[1], 1, enTransmissionType.Manual,      enFleetConditionStatus.Ready,         true,  5, 3, 1600, null),
            _Car("Volkswagen", "Polo",      2025, "BMV 1296", "ح ب ش 1296", "WVGFK9BP8CW087432", branchIds[1], categoryIds[1], 2, enTransmissionType.Automatic,  enFleetConditionStatus.Ready,         true,  5, 2, 1200, null),
            _Car("Mazda",      "3",         2026, "FTK 1333", "د ف ص 1333", "JM1BL1L71C1629873", branchIds[2], categoryIds[1], 1, enTransmissionType.Manual,      enFleetConditionStatus.Ready,         true,  5, 3, 1600, null),
            _Car("Subaru",     "Impreza",   2023, "JBY 1370", "ذ ج ط 1370", "4S3BMBC62B3244785", branchIds[3], categoryIds[1], 1, enTransmissionType.Automatic,  enFleetConditionStatus.Ready,         true,  5, 2, 1400, null),
            _Car("Chevrolet",  "Spark",     2024, "MRS 1407", "ر ح غ 1407", "KL8CB6SA7EC401856", branchIds[1], categoryIds[1], 1, enTransmissionType.Manual,      enFleetConditionStatus.Ready,         true,  5, 2, 1200, null),
            // Edge cases ────────────────────────────────────────────────────
            _Car("Kia",        "Forte",     2025, "PGH 1444", "ز ن و 1444", "KNAFZ4A82F5391047", branchIds[2], categoryIds[1], 1, enTransmissionType.Automatic,  enFleetConditionStatus.InMaintenance, true,  5, 2, 1600, null),
            _Car("Ford",       "Focus",     2026, "TVM 1481", "س ه ي 1481", "1FADP3F25EL214753", branchIds[3], categoryIds[1], 2, enTransmissionType.Manual,      enFleetConditionStatus.Damaged,       false, 5, 3, 1400, null),
            _Car("Volkswagen", "Golf",      2023, "XKB 1518", "ش خ أ 1518", "WVWZZZ1JZXW123456", branchIds[1], categoryIds[1], 1, enTransmissionType.Automatic,  enFleetConditionStatus.Ready,         false, 5, 2, 1200, null),
            _Car("Nissan",     "Kicks",     2024, "AYR 1555", "ص ر ب 1555", "3N1CP5CU6JL524876", branchIds[2], categoryIds[1], 2, enTransmissionType.Automatic,  enFleetConditionStatus.InMaintenance, true,  5, 3, 1400, null),
            _Car("Hyundai",    "Venue",     2025, "ENF 1592", "ط ز ت 1592", "KMHRC8A36MU095431", branchIds[3], categoryIds[1], 1, enTransmissionType.Automatic,  enFleetConditionStatus.Ready,         true,  5, 2, 1600, null),

            // ══════════════════════════════════════════════════════════════
            //  CATEGORY 2 — Standard / Sedan  (15 cars · years 2023-2026)
            // ══════════════════════════════════════════════════════════════
            _Car("Toyota",     "Camry",    2023, "HWG 1629", "ث ق ج 1629", "4T1BF1FK9HU765432", branchIds[1], categoryIds[2], 1, enTransmissionType.Automatic,  enFleetConditionStatus.Ready,         true,  5, 3, 2000, null),
            _Car("Honda",      "Accord",   2024, "LLV 1666", "ح ب ح 1666", "1HGCR2F3XGA023518", branchIds[2], categoryIds[2], 1, enTransmissionType.Automatic,  enFleetConditionStatus.Ready,         true,  5, 3, 2500, null),
            _Car("Hyundai",    "Sonata",   2025, "PBK 1703", "خ ذ ش 1703", "5NPE24AF1FH088234", branchIds[3], categoryIds[2], 1, enTransmissionType.Automatic,  enFleetConditionStatus.Ready,         true,  5, 3, 2000, null),
            _Car("Nissan",     "Altima",   2026, "SRZ 1740", "د ك ظ 1740", "1N4AL3AP2JC188467", branchIds[1], categoryIds[2], 2, enTransmissionType.Automatic,  enFleetConditionStatus.Ready,         true,  5, 2, 2500, null),
            _Car("Toyota",     "Corolla",  2023, "VGP 1777", "ذ م ع 1777", "2T1BURHE0JC036851", branchIds[2], categoryIds[2], 1, enTransmissionType.Automatic,  enFleetConditionStatus.Ready,         true,  5, 3, 1600, null),
            _Car("Kia",        "Optima",   2024, "YVE 1814", "ر ق غ 1814", "5XXGM4A78FG450273", branchIds[3], categoryIds[2], 1, enTransmissionType.Automatic,  enFleetConditionStatus.Ready,         true,  5, 3, 2000, null),
            _Car("Mazda",      "6",        2025, "CLT 1851", "ز ن ف 1851", "JM1GJ1V52G1452836", branchIds[1], categoryIds[2], 2, enTransmissionType.Manual,      enFleetConditionStatus.Ready,         true,  5, 3, 2500, null),
            _Car("Subaru",     "Legacy",   2026, "GBJ 1888", "س ه ص 1888", "4S3BMBH64J3006782", branchIds[2], categoryIds[2], 1, enTransmissionType.Automatic,  enFleetConditionStatus.Ready,         true,  5, 3, 2000, null),
            _Car("Chevrolet",  "Malibu",   2023, "KRY 1925", "ش و ط 1925", "1G1ZD5ST8KF152783", branchIds[3], categoryIds[2], 1, enTransmissionType.Automatic,  enFleetConditionStatus.Ready,         true,  5, 3, 2500, null),
            _Car("Ford",       "Fusion",   2024, "NFH 1962", "ص ي ظ 1962", "3FA6P0H72JR180432", branchIds[1], categoryIds[2], 1, enTransmissionType.Automatic,  enFleetConditionStatus.Ready,         true,  5, 3, 2000, null),
            // Edge cases ────────────────────────────────────────────────────
            _Car("Volkswagen", "Passat",   2025, "RUW 1999", "ط أ ع 1999", "WVWZZZ3CZ9E123456", branchIds[2], categoryIds[2], 2, enTransmissionType.Manual,      enFleetConditionStatus.InMaintenance, true,  5, 3, 2500, null),
            _Car("Hyundai",    "Elantra",  2026, "VJM 2036", "ث ر غ 2036", "5NPD84LF8JH267513", branchIds[3], categoryIds[2], 1, enTransmissionType.Automatic,  enFleetConditionStatus.Damaged,       false, 5, 3, 2000, null),
            _Car("Honda",      "Civic",    2023, "YYB 2073", "ح ز ف 2073", "2HGFC2F73NH576234", branchIds[1], categoryIds[2], 1, enTransmissionType.Manual,      enFleetConditionStatus.Ready,         false, 5, 2, 1500, null),
            _Car("Nissan",     "Maxima",   2024, "BNR 2110", "خ س ص 2110", "1N4AA6AP5JC394521", branchIds[2], categoryIds[2], 1, enTransmissionType.Automatic,  enFleetConditionStatus.InMaintenance, true,  5, 3, 3500, null),
            _Car("Toyota",     "Avalon",   2025, "FBF 2147", "د ش ط 2147", "4T1BZ1FB6KU028573", branchIds[3], categoryIds[2], 1, enTransmissionType.Automatic,  enFleetConditionStatus.Ready,         true,  5, 3, 3500, null),

            // ══════════════════════════════════════════════════════════════
            //  CATEGORY 3 — SUV  (15 cars · years 2023-2026)
            // ══════════════════════════════════════════════════════════════
            _Car("Toyota",     "RAV4",          2023, "HUV 2184", "ذ ظ ظ 2184", "2T3RFREV7JW803451", branchIds[1], categoryIds[3], 1, enTransmissionType.Automatic, enFleetConditionStatus.Ready,         true,  5, 3, 2500, null),
            _Car("Honda",      "CR-V",           2024, "LJK 2221", "ر ع أ 2221", "5J6RW2H89KA010231", branchIds[2], categoryIds[3], 2, enTransmissionType.Automatic, enFleetConditionStatus.Ready,         true,  5, 4, 2000, null),
            _Car("Ford",       "Explorer",       2025, "PYZ 2258", "ز غ ب 2258", "1FM5K8D85KGA10987", branchIds[3], categoryIds[3], 1, enTransmissionType.Automatic, enFleetConditionStatus.Ready,         true,  7, 4, 3500, null),
            _Car("Nissan",     "Rogue",          2026, "TPN 2295", "س ف ت 2295", "5N1AT2MV6KC752308", branchIds[1], categoryIds[3], 1, enTransmissionType.Automatic, enFleetConditionStatus.Ready,         true,  5, 3, 2500, null),
            _Car("Chevrolet",  "Equinox",        2023, "XEE 2332", "ش ص ث 2332", "3GNAXKEV7JS505618", branchIds[2], categoryIds[3], 2, enTransmissionType.Automatic, enFleetConditionStatus.Ready,         true,  5, 3, 2000, null),
            _Car("Mazda",      "CX-5",           2024, "ATV 2369", "ح ط ج 2369", "JM3KFBDM8K0634872", branchIds[3], categoryIds[3], 1, enTransmissionType.Automatic, enFleetConditionStatus.Ready,         true,  5, 3, 2500, null),
            _Car("Hyundai",    "Santa Fe",       2025, "EJK 2406", "خ ظ ح 2406", "5NMS5CAA7KH102673", branchIds[1], categoryIds[3], 2, enTransmissionType.Automatic, enFleetConditionStatus.Ready,         true,  5, 4, 2000, null),
            _Car("Kia",        "Sorento",        2026, "HYZ 2443", "د ع خ 2443", "5XYPG4A55KG530218", branchIds[2], categoryIds[3], 1, enTransmissionType.Automatic, enFleetConditionStatus.Ready,         true,  7, 4, 3500, null),
            _Car("Volkswagen", "Tiguan",         2023, "LPP 2480", "ذ غ د 2480", "WVGEF9BP5CD002983", branchIds[3], categoryIds[3], 1, enTransmissionType.Automatic, enFleetConditionStatus.Ready,         true,  5, 3, 2000, null),
            _Car("Toyota",     "Highlander",     2024, "PEE 2517", "ر ف ذ 2517", "5TDBZRFH4KS981052", branchIds[1], categoryIds[3], 1, enTransmissionType.Automatic, enFleetConditionStatus.Ready,         true,  7, 4, 3500, null),
            // Edge cases ────────────────────────────────────────────────────
            _Car("Subaru",     "Forester",       2025, "STU 2554", "ز ص ر 2554", "JF2SJAEC5JH547863", branchIds[2], categoryIds[3], 2, enTransmissionType.Automatic, enFleetConditionStatus.InMaintenance, true,  5, 3, 2500, null),
            _Car("Hyundai",    "Palisade",       2026, "VJL 2591", "س ط ز 2591", "KM8R44HE1LU075432", branchIds[3], categoryIds[3], 1, enTransmissionType.Automatic, enFleetConditionStatus.Damaged,       false, 7, 4, 3500, null),
            _Car("Kia",        "Telluride",      2023, "YYB 2628", "ش ظ س 2628", "5XYP64HC8KG072381", branchIds[1], categoryIds[3], 1, enTransmissionType.Automatic, enFleetConditionStatus.Ready,         false, 7, 4, 3500, null),
            _Car("Ford",       "Escape",         2024, "BPN 2665", "ح ع ش 2665", "1FMCU9GD9KUC08732", branchIds[2], categoryIds[3], 2, enTransmissionType.Automatic, enFleetConditionStatus.InMaintenance, true,  5, 3, 2000, null),
            _Car("GMC",        "Acadia",         2025, "FEE 2702", "خ غ ص 2702", "1GKKRRKD0GJ250872", branchIds[3], categoryIds[3], 1, enTransmissionType.Automatic, enFleetConditionStatus.Ready,         true,  7, 4, 3600, null),

            // ══════════════════════════════════════════════════════════════
            //  CATEGORY 4 — Minivan  (12 cars · years 2023-2026)
            // ══════════════════════════════════════════════════════════════
            _Car("Toyota",       "Sienna",         2023, "HTV 2739", "د ف ط 2739", "5TDKZ3DCXKS015678", branchIds[1], categoryIds[4], 1, enTransmissionType.Automatic, enFleetConditionStatus.Ready,         true,  8, 5, 3500, null),
            _Car("Honda",        "Odyssey",        2024, "LJK 2776", "ذ ص ظ 2776", "5FNRL6H76KB077432", branchIds[2], categoryIds[4], 1, enTransmissionType.Automatic, enFleetConditionStatus.Ready,         true,  8, 5, 3500, null),
            _Car("Kia",          "Carnival",       2025, "PYZ 2813", "ر ط ع 2813", "KNDNB4H39P6223481", branchIds[3], categoryIds[4], 1, enTransmissionType.Automatic, enFleetConditionStatus.Ready,         true,  8, 5, 3500, null),
            _Car("Chrysler",     "Pacifica",       2026, "TPN 2850", "ز ظ غ 2850", "2C4RC1EG9JR213450", branchIds[1], categoryIds[4], 1, enTransmissionType.Automatic, enFleetConditionStatus.Ready,         true,  7, 4, 3600, null),
            _Car("Volkswagen",   "Multivan",       2023, "XEE 2887", "س ع ف 2887", "WV2ZZZ7HZSH062345", branchIds[2], categoryIds[4], 2, enTransmissionType.Automatic, enFleetConditionStatus.Ready,         true,  7, 5, 2000, null),
            _Car("Mercedes-Benz","V-Class",        2024, "ATV 2924", "ش غ ص 2924", "WDF44770123456789", branchIds[3], categoryIds[4], 2, enTransmissionType.Automatic, enFleetConditionStatus.Ready,         true,  7, 5, 2200, null),
            _Car("Toyota",       "Alphard",        2025, "EJK 2961", "ح ف ط 2961", "JTMBD33V085123456", branchIds[1], categoryIds[4], 1, enTransmissionType.Automatic, enFleetConditionStatus.Ready,         true,  7, 5, 3500, null),
            _Car("Hyundai",      "Staria",         2026, "HYZ 2998", "خ ص ظ 2998", "KMHH851XNPU045678", branchIds[2], categoryIds[4], 1, enTransmissionType.Automatic, enFleetConditionStatus.Ready,         true,  9, 5, 3500, null),
            // Edge cases ────────────────────────────────────────────────────
            _Car("Volkswagen",   "Caravelle",      2023, "LPP 3035", "د ط ع 3035", "WV2ZZZ7HZLH091234", branchIds[3], categoryIds[4], 2, enTransmissionType.Automatic, enFleetConditionStatus.InMaintenance, true,  9, 5, 2000, null),
            _Car("Chrysler",     "Grand Caravan",  2024, "PEE 3072", "ذ ظ غ 3072", "2C4RDGBG8KR706321", branchIds[1], categoryIds[4], 1, enTransmissionType.Automatic, enFleetConditionStatus.Damaged,       false, 7, 4, 3600, null),
            _Car("Ford",         "Galaxy",         2025, "STU 3109", "ر ع ف 3109", "WF0RXXGBWRLL98765", branchIds[2], categoryIds[4], 1, enTransmissionType.Automatic, enFleetConditionStatus.Ready,         false, 7, 5, 2000, null),
            _Car("Kia",          "Sedona",         2026, "VJL 3146", "ز غ ص 3146", "KNDMB4H35H6321456", branchIds[3], categoryIds[4], 1, enTransmissionType.Automatic, enFleetConditionStatus.InMaintenance, true,  8, 5, 3500, null),

            // ══════════════════════════════════════════════════════════════
            //  CATEGORY 5 — Electric & Hybrid  (13 cars · years 2023-2026)
            // ══════════════════════════════════════════════════════════════
            _Car("Tesla",      "Model 3",             2023, "YYB 3183", "س ف ط 3183", "5YJ3E1EA1JF006195", branchIds[1], categoryIds[5], 4, enTransmissionType.Automatic, enFleetConditionStatus.Ready,         true,  5, 2, 0, null),
            _Car("Nissan",     "Leaf",                2024, "BPN 3220", "ش ص ظ 3220", "1N4AZ1CP9KC311452", branchIds[2], categoryIds[5], 4, enTransmissionType.Automatic, enFleetConditionStatus.Ready,         true,  5, 2, 0, null),
            _Car("Hyundai",    "Kona Electric",       2025, "FEE 3257", "ح ط ع 3257", "KM8K23AG2LU117853", branchIds[3], categoryIds[5], 4, enTransmissionType.Automatic, enFleetConditionStatus.Ready,         true,  5, 2, 0, null),
            _Car("Chevrolet",  "Bolt EV",             2026, "HTV 3294", "خ ظ غ 3294", "1G1FX6S04H4165432", branchIds[1], categoryIds[5], 4, enTransmissionType.Automatic, enFleetConditionStatus.Ready,         true,  5, 2, 0, null),
            _Car("Volkswagen", "ID.4",                2023, "LJK 3331", "د ع ف 3331", "WVGZZZE2ZMP012345", branchIds[2], categoryIds[5], 4, enTransmissionType.Automatic, enFleetConditionStatus.Ready,         true,  5, 3, 0, null),
            _Car("Toyota",     "Prius",               2024, "PYZ 3368", "ذ غ ص 3368", "JTDKAMFU0N3156743", branchIds[3], categoryIds[5], 3, enTransmissionType.Automatic, enFleetConditionStatus.Ready,         true,  5, 2, 1800, null),
            _Car("BMW",        "i3",                  2025, "TPN 3405", "ر ف ط 3405", "WBY1Z4C51FV279807", branchIds[1], categoryIds[5], 4, enTransmissionType.Automatic, enFleetConditionStatus.Ready,         true,  4, 2, 0, null),
            _Car("Honda",      "Insight",             2026, "XEE 3442", "ز ص ظ 3442", "19XZE4F99KE017832", branchIds[2], categoryIds[5], 3, enTransmissionType.Automatic, enFleetConditionStatus.Ready,         true,  5, 2, 1500, null),
            _Car("Hyundai",    "Ioniq Hybrid",        2023, "ATV 3479", "س ط ع 3479", "KMHC85LC7MU206453", branchIds[3], categoryIds[5], 3, enTransmissionType.Automatic, enFleetConditionStatus.Ready,         true,  5, 2, 1600, null),
            _Car("Kia",        "Niro",                2024, "EJK 3516", "ش ظ غ 3516", "KNDCE3LC1K5204539", branchIds[1], categoryIds[5], 3, enTransmissionType.Automatic, enFleetConditionStatus.Ready,         true,  5, 2, 1600, null),
            // Edge cases ────────────────────────────────────────────────────
            _Car("Tesla",      "Model S",             2025, "HYZ 3553", "ح ع ف 3553", "5YJSA1E26MF426034", branchIds[2], categoryIds[5], 4, enTransmissionType.Automatic, enFleetConditionStatus.InMaintenance, true,  5, 2, 0, null),
            _Car("Audi",       "A3 Sportback e-tron", 2026, "LPP 3590", "خ غ ص 3590", "WAUZZZ8V5HA052341", branchIds[3], categoryIds[5], 4, enTransmissionType.Automatic, enFleetConditionStatus.Damaged,       false, 5, 2, 0, null),
            _Car("Chrysler",   "Pacifica Hybrid",     2023, "PEE 3627", "د ف ط 3627", "2C4RC1N70LR234567", branchIds[1], categoryIds[5], 3, enTransmissionType.Automatic, enFleetConditionStatus.InMaintenance, true,  7, 4, 3600, null),

            // ══════════════════════════════════════════════════════════════
            //  CATEGORY 6 — Luxury  (14 cars · years 2018-2024)
            // ══════════════════════════════════════════════════════════════
            _Car("BMW",          "5 Series",      2018, "STU 3664", "ذ ص ظ 3664", "WBAJA7C50KBX12345", branchIds[1], categoryIds[6], 1, enTransmissionType.Automatic, enFleetConditionStatus.Ready,         true,  5, 3, 3000, null),
            _Car("Mercedes-Benz","E-Class",       2019, "VJL 3701", "ر ط ع 3701", "WDD2130161A012345", branchIds[2], categoryIds[6], 1, enTransmissionType.Automatic, enFleetConditionStatus.Ready,         true,  5, 3, 2000, null),
            _Car("Audi",         "A6",            2020, "YYB 3738", "ز ظ غ 3738", "WAUZZZ4G5KN012345", branchIds[3], categoryIds[6], 2, enTransmissionType.Automatic, enFleetConditionStatus.Ready,         true,  5, 3, 3000, null),
            _Car("Jaguar",       "XF",            2021, "BPN 3775", "س ع ف 3775", "SAJWA0ES8HMV12345", branchIds[1], categoryIds[6], 1, enTransmissionType.Automatic, enFleetConditionStatus.Ready,         true,  5, 3, 2000, null),
            _Car("Volvo",        "S90",           2022, "FEE 3812", "ش غ ص 3812", "YV1RS685XE1234567", branchIds[2], categoryIds[6], 1, enTransmissionType.Automatic, enFleetConditionStatus.Ready,         true,  5, 3, 2000, null),
            _Car("Cadillac",     "CT6",           2023, "HTV 3849", "ح ف ط 3849", "1G6KF5RS8HU123456", branchIds[3], categoryIds[6], 1, enTransmissionType.Automatic, enFleetConditionStatus.Ready,         true,  5, 3, 3600, null),
            _Car("Infiniti",     "Q70",           2024, "LJK 3886", "خ ص ظ 3886", "JN1BY1AR9FM234567", branchIds[1], categoryIds[6], 1, enTransmissionType.Automatic, enFleetConditionStatus.Ready,         true,  5, 3, 3700, null),
            _Car("Porsche",      "Panamera",      2018, "PYZ 3923", "د ط ع 3923", "WP0AA2A79JL123456", branchIds[2], categoryIds[6], 1, enTransmissionType.Automatic, enFleetConditionStatus.Ready,         true,  4, 3, 3000, null),
            _Car("Lexus",        "LS",            2019, "TPN 3960", "ذ ظ غ 3960", "JTHB51FF5J5023456", branchIds[3], categoryIds[6], 1, enTransmissionType.Automatic, enFleetConditionStatus.Ready,         true,  5, 3, 3500, null),
            _Car("Mercedes-Benz","S-Class",       2020, "XEE 3997", "ر ع ف 3997", "WDD2220561A234567", branchIds[1], categoryIds[6], 1, enTransmissionType.Automatic, enFleetConditionStatus.Ready,         true,  5, 3, 3000, null),
            // Edge cases ────────────────────────────────────────────────────
            _Car("BMW",          "7 Series",      2021, "ATV 4034", "ز غ ص 4034", "WBA7E2C56JG123456", branchIds[2], categoryIds[6], 2, enTransmissionType.Automatic, enFleetConditionStatus.InMaintenance, true,  5, 3, 3000, null),
            _Car("Audi",         "A8",            2022, "EJK 4071", "س ف ط 4071", "WAUZZZ4H5HN012345", branchIds[3], categoryIds[6], 1, enTransmissionType.Automatic, enFleetConditionStatus.Damaged,       false, 5, 3, 4000, null),
            _Car("Maserati",     "Quattroporte",  2023, "HYZ 4108", "ش ص ظ 4108", "ZAM56RRA5H1234567", branchIds[1], categoryIds[6], 1, enTransmissionType.Automatic, enFleetConditionStatus.Ready,         false, 5, 3, 3800, null),
            _Car("Lexus",        "LC 500",        2024, "LPP 4145", "ح ط ع 4145", "JTHHP5BC1K5000234", branchIds[2], categoryIds[6], 1, enTransmissionType.Automatic, enFleetConditionStatus.InMaintenance, true,  4, 2, 5000, null),

            // ══════════════════════════════════════════════════════════════
            //  CATEGORY 7 — Luxury SUV  (12 cars · years 2018-2024)
            // ══════════════════════════════════════════════════════════════
            _Car("BMW",        "X5",              2018, "PEE 4182", "خ ظ غ 4182", "5UXKU2C57J0W12345", branchIds[1], categoryIds[7], 1, enTransmissionType.Automatic, enFleetConditionStatus.Ready,         true,  5, 4, 3000, null),
            _Car("Audi",       "Q5",              2019, "STU 4219", "د ع ف 4219", "WA1BNAFY9K2034567", branchIds[2], categoryIds[7], 1, enTransmissionType.Automatic, enFleetConditionStatus.Ready,         true,  5, 4, 3000, null),
            _Car("GMC",        "Yukon",           2020, "VJL 4256", "ذ غ ص 4256", "1GKS2GKC6LR234567", branchIds[3], categoryIds[7], 1, enTransmissionType.Automatic, enFleetConditionStatus.Ready,         true,  7, 5, 5300, null),
            _Car("Tesla",      "Model X",         2021, "YYB 4293", "ر ف ط 4293", "5YJXCDE29MF345678", branchIds[1], categoryIds[7], 4, enTransmissionType.Automatic, enFleetConditionStatus.Ready,         true,  7, 4, 0,    null),
            _Car("Audi",       "RS Q8",           2022, "BPN 4330", "ز ص ظ 4330", "WAUZZZ4M5LD012345", branchIds[2], categoryIds[7], 1, enTransmissionType.Automatic, enFleetConditionStatus.Ready,         true,  5, 4, 4000, null),
            _Car("Jeep",       "Grand Cherokee",  2023, "FEE 4367", "س ط ع 4367", "1C4RJFBG4KC789012", branchIds[3], categoryIds[7], 1, enTransmissionType.Automatic, enFleetConditionStatus.Ready,         true,  5, 4, 3600, null),
            _Car("Chevrolet",  "Tahoe",           2024, "HTV 4404", "ش ظ غ 4404", "1GNSCBKC1LR456789", branchIds[1], categoryIds[7], 1, enTransmissionType.Automatic, enFleetConditionStatus.Ready,         true,  7, 5, 5300, null),
            _Car("Subaru",     "Ascent",          2018, "LJK 4441", "ح ع ف 4441", "4S4WMALD1J3456789", branchIds[2], categoryIds[7], 1, enTransmissionType.Automatic, enFleetConditionStatus.Ready,         true,  7, 4, 2500, null),
            // Edge cases ────────────────────────────────────────────────────
            _Car("Lincoln",    "MKZ",             2019, "PYZ 4478", "خ غ ص 4478", "3LN6L5MU0KR234567", branchIds[3], categoryIds[7], 3, enTransmissionType.Automatic, enFleetConditionStatus.InMaintenance, true,  5, 3, 2000, null),
            _Car("Cadillac",   "CT5",             2020, "TPN 4515", "د ف ط 4515", "1G6DA5RK0L0123456", branchIds[1], categoryIds[7], 1, enTransmissionType.Automatic, enFleetConditionStatus.Damaged,       false, 5, 3, 3000, null),
            _Car("Infiniti",   "Q50",             2021, "XEE 4552", "ذ ص ظ 4552", "JN1EV7AR5LM234567", branchIds[2], categoryIds[7], 1, enTransmissionType.Automatic, enFleetConditionStatus.Ready,         false, 5, 3, 3000, null),
            _Car("Acura",      "TLX",             2022, "ATV 4589", "ر ط ع 4589", "19UUB2F54NA012345", branchIds[3], categoryIds[7], 1, enTransmissionType.Automatic, enFleetConditionStatus.InMaintenance, true,  5, 3, 2000, null),

            // ══════════════════════════════════════════════════════════════
            //  CATEGORY 8 — Supercar  (14 cars · years 2018-2024)
            //  Some have PolicyOverride to demonstrate override logic
            // ══════════════════════════════════════════════════════════════
            _Car("Rolls-Royce",   "Phantom",             2018, "EJK 4626", "ز ظ غ 4626", "SCA664S57JUX12345", branchIds[1], categoryIds[8], 1, enTransmissionType.Automatic, enFleetConditionStatus.Ready, true,  4, 2, 6750, policyIds["Supercar"]),
            _Car("Bentley",       "Continental GT",      2019, "HYZ 4663", "س ع ف 4663", "SCBBU53W09C045678", branchIds[2], categoryIds[8], 1, enTransmissionType.Automatic, enFleetConditionStatus.Ready, true,  4, 2, 6000, policyIds["Supercar"]),
            _Car("Ferrari",       "GTC4Lusso",           2020, "LPP 4700", "ش غ ص 4700", "ZFF80AMA0K0237891", branchIds[3], categoryIds[8], 1, enTransmissionType.Automatic, enFleetConditionStatus.Ready, true,  4, 1, 6300, policyIds["Supercar"]),
            _Car("Lamborghini",   "Aventador",           2021, "PEE 4737", "ح ف ط 4737", "ZHWBU4ZF3HLA12345", branchIds[1], categoryIds[8], 1, enTransmissionType.Automatic, enFleetConditionStatus.Ready, true,  2, 1, 6500, policyIds["Supercar"]),
            _Car("McLaren",       "720S",                2022, "STU 4774", "خ ص ظ 4774", "SBM13AAA1KW000123", branchIds[2], categoryIds[8], 1, enTransmissionType.Automatic, enFleetConditionStatus.Ready, true,  2, 1, 4000, policyIds["Supercar"]),
            _Car("Aston Martin",  "DB11",                2023, "VJL 4811", "د ط ع 4811", "SCFRMFAW9KGR12345", branchIds[3], categoryIds[8], 1, enTransmissionType.Automatic, enFleetConditionStatus.Ready, true,  4, 2, 5200, policyIds["Supercar"]),
            _Car("Porsche",       "911 GT3",             2024, "YYB 4848", "ذ ظ غ 4848", "WP0AC2A91JS123456", branchIds[1], categoryIds[8], 1, enTransmissionType.Automatic, enFleetConditionStatus.Ready, true,  2, 1, 4000, policyIds["Supercar"]),
            _Car("Mercedes-Maybach","S650",              2018, "BPN 4885", "ر ع ف 4885", "WDD2221561A345678", branchIds[2], categoryIds[8], 1, enTransmissionType.Automatic, enFleetConditionStatus.Ready, true,  4, 3, 6000, policyIds["Supercar"]),
            _Car("BMW",           "M8 Competition",      2019, "FEE 4922", "ز غ ص 4922", "WBSGV0C04LCE12345", branchIds[3], categoryIds[8], 1, enTransmissionType.Automatic, enFleetConditionStatus.Ready, true,  4, 2, 4400, policyIds["Supercar"]),
            _Car("Mercedes-Benz", "AMG GT R",            2020, "HTV 4959", "س ف ط 4959", "WDD1901781A012345", branchIds[1], categoryIds[8], 1, enTransmissionType.Automatic, enFleetConditionStatus.Ready, true,  2, 1, 4000, policyIds["Supercar"]),
            // Edge cases ────────────────────────────────────────────────────
            _Car("Ferrari",       "488 Pista",           2021, "LJK 4996", "ش ص ظ 4996", "ZFF90HLA0L0234567", branchIds[2], categoryIds[8], 1, enTransmissionType.Automatic, enFleetConditionStatus.InMaintenance, true,  2, 1, 3900, policyIds["Supercar"]),
            _Car("Lamborghini",   "Huracan Performante", 2022, "PYZ 5033", "ح ط ع 5033", "ZHWUC4ZF8JLA23456", branchIds[3], categoryIds[8], 1, enTransmissionType.Automatic, enFleetConditionStatus.Damaged,       false, 2, 1, 5200, policyIds["Supercar"]),
            _Car("Rolls-Royce",   "Dawn",                2023, "TPN 5070", "خ ظ غ 5070", "SCA666D5XJU012345", branchIds[1], categoryIds[8], 1, enTransmissionType.Automatic, enFleetConditionStatus.Ready,         false, 4, 2, 6750, policyIds["Supercar"]),
            _Car("Bentley",       "Mulsanne",            2024, "XEE 5107", "د ع ف 5107", "SCBBR53W49C056789", branchIds[2], categoryIds[8], 1, enTransmissionType.Automatic, enFleetConditionStatus.Ready,         true,  5, 3, 6750, null),  // Uses category policy (no override)
        };

        await context.Cars.AddRangeAsync(cars);
        await context.SaveChangesAsync();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Factory Helper
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// FuelType parameter: 1=Gasoline · 2=Diesel · 3=Hybrid · 4=Electric
    /// Stored as byte using FuelType Object Pattern (FuelTypeObject setter)
    /// </summary>
    private static Car _Car(
        string brand, string model, int year,
        string plateEN, string plateAR, string vin,
        int branchId, int categoryId,
        byte fuelTypeId, enTransmissionType transmission,
        enFleetConditionStatus condition, bool isActive,
        byte seats, byte bags, short engineCc,
        int? policyOverrideId)
    {
        var car = new Car
        {
            Brand = brand,
            Model = model,
            Year = year,
            PlateNumberEN = plateEN,
            PlateNumberAR = plateAR,
            VIN = vin,
            CurrentBranchId = branchId,
            CategoryId = categoryId,
            TransmissionType = transmission,
            FleetConditionStatus = condition,
            IsActive = isActive,
            NumberOfSeats = seats,
            NumberOfBags = bags,
            EngineCapacity = engineCc,
            PolicyOverrideId = policyOverrideId,
            KmMileage = 0,
            CreatedAt = DateTime.UtcNow
        };

        // Uses the Object Pattern setter — converts Id → byte for DB storage
        car.FuelTypeObject = Domain.HelperClasses.FuelType.FromId(fuelTypeId)
            ?? throw new InvalidOperationException($"Invalid FuelType Id: {fuelTypeId}");

        return car;
    }
}