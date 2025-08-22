using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.Extensions;

internal static class ValueConversionExtensions
{
    public static PropertyBuilder<TProperty> HasPropertyConversion<TProperty>(this PropertyBuilder<TProperty> propertyBuilder, ValueConverter converter)
    {
       return propertyBuilder.HasConversion(converter);
    }
}