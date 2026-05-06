using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityToolkit.Diagnostics;

namespace MeshWiz.Math;

internal interface IJsonConverterSelfProvider
{
    internal static abstract JsonConverter CreateConverter(JsonSerializerOptions options);
}

internal sealed class MeshWizJsonConverter : JsonConverterFactory
{
    private static readonly ConcurrentDictionary<Type, Func<JsonSerializerOptions, JsonConverter?>> s_converters = [];
    public override bool CanConvert(Type typeToConvert)
        => typeof(IJsonConverterSelfProvider).IsAssignableFrom(typeToConvert);

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        if (!CanConvert(typeToConvert))
        {
            ThrowHelper.ThrowArgumentException($"Type {typeToConvert.FullName} must implement {nameof(IJsonConverterSelfProvider)}.");
        }

        return s_converters.GetOrAdd(typeToConvert, GetFactory)(options);
    }

    private static Func<JsonSerializerOptions, JsonConverter?> GetFactory(Type typeToConvert)
    {
        var invokerType = typeof(Invoker<>).MakeGenericType(typeToConvert);
        var method = invokerType.GetMethod(nameof(Invoker<>.CreateConverter),
            BindingFlags.Public | BindingFlags.Static);
        return options => method!.Invoke(null, [options]) as JsonConverter;
    }
    private static class Invoker<T>
        where T : IJsonConverterSelfProvider
    {
        public static JsonConverter CreateConverter(JsonSerializerOptions options)
            => T.CreateConverter(options);
    }
    
    public static JsonConverter<TSource> Create<TSource,TDestination>(Func<TSource, TDestination> onSerialization, Func<TDestination?, TSource> onDeserialization)
    =>new MappingConverter<TSource,TDestination>(onSerialization,onDeserialization);
    
    
    private sealed class MappingConverter<TSource, TDestination> : JsonConverter<TSource>
    {
        private readonly Func<TSource, TDestination> _onSerialization;
        private readonly Func<TDestination?, TSource?> _onDeserialization;


        /// <inheritdoc />
        internal MappingConverter(Func<TSource, TDestination> onSerialization, Func<TDestination?, TSource?> onDeserialization)
        {
            _onSerialization = onSerialization;
            _onDeserialization = onDeserialization;
        }

        /// <inheritdoc />
        public override TSource? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => _onDeserialization(JsonSerializer.Deserialize<TDestination>(ref reader, options))!;
        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, TSource value, JsonSerializerOptions options) => JsonSerializer.Serialize(writer, _onSerialization(value), options);
    
    }
}