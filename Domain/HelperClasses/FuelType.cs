namespace Domain.HelperClasses;

public sealed class FuelType
{
    public int Id { get; }
    public string NameEN { get; }
    public string NameAR { get; }

    private FuelType(int id, string nameEN, string nameAR)
    {
        Id = id;
        NameEN = nameEN;
        NameAR = nameAR;
    }

    public static readonly FuelType Gasoline = new(1, "Gasoline", "بنزين");
    public static readonly FuelType Diesel = new(2, "Diesel", "ديزل");
    public static readonly FuelType Electric = new(3, "Electric", "كهربائي");
    public static readonly FuelType Hybrid = new(4, "Hybrid", "هجين");

    public static IEnumerable<FuelType> All => [Gasoline, Diesel, Hybrid, Electric];


    public static FuelType? FromId(int id) => All.Single(x => x.Id == id);
    public static int MaxId => All.Max(x => x.Id);

    // to prevent wrong comparisons
    public override string ToString() => NameEN;
    public override bool Equals(object? obj) => obj is FuelType other && Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();
}