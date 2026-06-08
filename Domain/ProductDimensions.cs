namespace ProductCatalogService.Domain;

public sealed class ProductDimensions
{
    public decimal HeightCm { get; private set; }
    public decimal WidthCm { get; private set; }
    public decimal LengthCm { get; private set; }

    private ProductDimensions()
    {
    }

    public ProductDimensions(decimal heightCm, decimal widthCm, decimal lengthCm)
    {
        if (heightCm <= 0)
        {
            throw new ArgumentException("Height must be greater than zero", nameof(heightCm));
        }

        if (widthCm <= 0)
        {
            throw new ArgumentException("Width must be greater than zero", nameof(widthCm));
        }

        if (lengthCm <= 0)
        {
            throw new ArgumentException("Length must be greater than zero", nameof(lengthCm));
        }

        HeightCm = heightCm;
        WidthCm = widthCm;
        LengthCm = lengthCm;
    }

    public decimal VolumeCm3 => HeightCm * WidthCm * LengthCm;
}
