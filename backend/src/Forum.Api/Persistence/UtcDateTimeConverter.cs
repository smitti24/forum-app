using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Forum.Api.Persistence;

public class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    value => value,
    value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

public class NullableUtcDateTimeConverter() : ValueConverter<DateTime?, DateTime?>(
    value => value,
    value => value == null ? null : DateTime.SpecifyKind(value.Value, DateTimeKind.Utc));
