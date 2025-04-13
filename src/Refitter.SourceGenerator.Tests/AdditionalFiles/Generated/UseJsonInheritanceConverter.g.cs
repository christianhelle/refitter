﻿// <auto-generated>
//     This code was generated by Refitter.
// </auto-generated>


using Refit;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

#nullable enable annotations

namespace Refitter.Tests.UseJsonInheritanceConverter
{
    [System.CodeDom.Compiler.GeneratedCode("Refitter", "1.0.0.0")]
    public partial interface IApiClient
    {
        /// <param name="token">Some Token</param>
        /// <returns>Created</returns>
        /// <exception cref="ApiException">
        /// Thrown when the request returns a non-success status code:
        /// <list type="table">
        /// <listheader>
        /// <term>Status</term>
        /// <description>Description</description>
        /// </listheader>
        /// <item>
        /// <term>400</term>
        /// <description>Bad Request</description>
        /// </item>
        /// <item>
        /// <term>500</term>
        /// <description>Server Error</description>
        /// </item>
        /// </list>
        /// </exception>
        [Headers("Accept: application/json", "Content-Type: application/json")]
        [Post("/v1/Warehouses")]
        Task<ICollection<WarehouseResponse>> CreateWarehouse([Query] string token, [Body] Warehouse body);


    }

}

//----------------------
// <auto-generated>
//     Generated using the NSwag toolchain v14.3.0.0 (NJsonSchema v11.2.0.0 (Newtonsoft.Json v13.0.0.0)) (http://NSwag.org)
// </auto-generated>
//----------------------

#pragma warning disable 108 // Disable "CS0108 '{derivedDto}.ToJson()' hides inherited member '{dtoBase}.ToJson()'. Use the new keyword if hiding was intended."
#pragma warning disable 114 // Disable "CS0114 '{derivedDto}.RaisePropertyChanged(String)' hides inherited member 'dtoBase.RaisePropertyChanged(String)'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword."
#pragma warning disable 472 // Disable "CS0472 The result of the expression is always 'false' since a value of type 'Int32' is never equal to 'null' of type 'Int32?'
#pragma warning disable 612 // Disable "CS0612 '...' is obsolete"
#pragma warning disable 649 // Disable "CS0649 Field is never assigned to, and will always have its default value null"
#pragma warning disable 1573 // Disable "CS1573 Parameter '...' has no matching param tag in the XML comment for ...
#pragma warning disable 1591 // Disable "CS1591 Missing XML comment for publicly visible type or member ..."
#pragma warning disable 8073 // Disable "CS8073 The result of the expression is always 'false' since a value of type 'T' is never equal to 'null' of type 'T?'"
#pragma warning disable 3016 // Disable "CS3016 Arrays as attribute arguments is not CLS-compliant"
#pragma warning disable 8600 // Disable "CS8600 Converting null literal or possible null value to non-nullable type"
#pragma warning disable 8602 // Disable "CS8602 Dereference of a possibly null reference"
#pragma warning disable 8603 // Disable "CS8603 Possible null reference return"
#pragma warning disable 8604 // Disable "CS8604 Possible null reference argument for parameter"
#pragma warning disable 8625 // Disable "CS8625 Cannot convert null literal to non-nullable reference type"
#pragma warning disable 8765 // Disable "CS8765 Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes)."

namespace Refitter.Tests.UseJsonInheritanceConverter
{
    using System = global::System;

    

    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.3.0.0 (NJsonSchema v11.2.0.0 (Newtonsoft.Json v13.0.0.0))")]
    public partial class Metadata
    {

        [JsonPropertyName("createdAt")]
        public System.DateTimeOffset CreatedAt { get; set; }

        [JsonPropertyName("createdBy")]
        public string CreatedBy { get; set; }

        [JsonPropertyName("lastModifiedAt")]
        public System.DateTimeOffset LastModifiedAt { get; set; }

        [JsonPropertyName("lastModifiedBy")]
        public string LastModifiedBy { get; set; }

    }

    [JsonInheritanceConverter(typeof(SomeComponent), "$type")]
    [JsonInheritanceAttribute("Warehouse", typeof(Warehouse))]
    [JsonInheritanceAttribute("WarehouseResponse", typeof(WarehouseResponse))]
    [JsonInheritanceAttribute("LoadingAddress", typeof(LoadingAddress))]
    [JsonInheritanceAttribute("UserComponent", typeof(UserComponent))]
    [JsonInheritanceAttribute("UserComponent2", typeof(UserComponent2))]
    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.3.0.0 (NJsonSchema v11.2.0.0 (Newtonsoft.Json v13.0.0.0))")]
    public partial class SomeComponent : Component
    {

        [JsonPropertyName("typeId")]
        public long TypeId { get; set; }

    }

    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.3.0.0 (NJsonSchema v11.2.0.0 (Newtonsoft.Json v13.0.0.0))")]
    public enum SomeComponentState
    {

        [System.Runtime.Serialization.EnumMember(Value = @"Active")]
        Active = 0,

        [System.Runtime.Serialization.EnumMember(Value = @"Inactive")]
        Inactive = 1,

        [System.Runtime.Serialization.EnumMember(Value = @"Blocked")]
        Blocked = 2,

        [System.Runtime.Serialization.EnumMember(Value = @"Deleted")]
        Deleted = 3,

    }

    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.3.0.0 (NJsonSchema v11.2.0.0 (Newtonsoft.Json v13.0.0.0))")]
    public partial class SomeComponentType : Component
    {

        [JsonPropertyName("state")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public SomeComponentState State { get; set; }

        [JsonPropertyName("isBaseRole")]
        public bool IsBaseRole { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("numberingId")]
        public string NumberingId { get; set; }

    }

    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.3.0.0 (NJsonSchema v11.2.0.0 (Newtonsoft.Json v13.0.0.0))")]
    public partial class Component
    {

        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("metadata")]
        public Metadata Metadata { get; set; }

    }

    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.3.0.0 (NJsonSchema v11.2.0.0 (Newtonsoft.Json v13.0.0.0))")]
    public partial class LoadingAddress : SomeComponent
    {

        [JsonPropertyName("info")]
        public string Info { get; set; }

    }

    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.3.0.0 (NJsonSchema v11.2.0.0 (Newtonsoft.Json v13.0.0.0))")]
    public partial class Warehouse : SomeComponent
    {

        [JsonPropertyName("info")]
        public string Info { get; set; }

    }

    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.3.0.0 (NJsonSchema v11.2.0.0 (Newtonsoft.Json v13.0.0.0))")]
    public partial class WarehouseResponse : SomeComponent
    {

        [JsonPropertyName("info")]
        public string Info { get; set; }

    }

    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.3.0.0 (NJsonSchema v11.2.0.0 (Newtonsoft.Json v13.0.0.0))")]
    public partial class UserComponent : SomeComponent
    {

        [JsonPropertyName("info")]
        public string Info { get; set; }

    }

    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.3.0.0 (NJsonSchema v11.2.0.0 (Newtonsoft.Json v13.0.0.0))")]
    public partial class UserComponent2 : UserComponent
    {

        [JsonPropertyName("info2")]
        public string Info2 { get; set; }

    }

    [JsonInheritanceConverter(typeof(ProblemDetails), "$type")]
    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.3.0.0 (NJsonSchema v11.2.0.0 (Newtonsoft.Json v13.0.0.0))")]
    public partial class ProblemDetails
    {

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("status")]
        public int? Status { get; set; }

        [JsonPropertyName("detail")]
        public string Detail { get; set; }

        [JsonPropertyName("instance")]
        public string Instance { get; set; }

        private IDictionary<string, object> _additionalProperties;

        [JsonExtensionData]
        public IDictionary<string, object> AdditionalProperties
        {
            get { return _additionalProperties ?? (_additionalProperties = new Dictionary<string, object>()); }
            set { _additionalProperties = value; }
        }

    }


    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.3.0.0 (NJsonSchema v11.2.0.0 (Newtonsoft.Json v13.0.0.0))")]
    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Interface, AllowMultiple = true)]
    internal class JsonInheritanceAttribute : System.Attribute
    {
        public JsonInheritanceAttribute(string key, System.Type type)
        {
            Key = key;
            Type = type;
        }

        public string Key { get; }

        public System.Type Type { get; }
    }

    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.3.0.0 (NJsonSchema v11.2.0.0 (Newtonsoft.Json v13.0.0.0))")]
    internal class JsonInheritanceConverterAttribute : JsonConverterAttribute
    {
        public string DiscriminatorName { get; }

        public JsonInheritanceConverterAttribute(System.Type baseType, string discriminatorName = "discriminator")
            : base(typeof(JsonInheritanceConverter<>).MakeGenericType(baseType))
        {
            DiscriminatorName = discriminatorName;
        }
    }

    public class JsonInheritanceConverter<TBase> : JsonConverter<TBase>
    {
        private readonly string _discriminatorName;

        public JsonInheritanceConverter()
        {
            var attribute = System.Reflection.CustomAttributeExtensions.GetCustomAttribute<JsonInheritanceConverterAttribute>(typeof(TBase));
            _discriminatorName = attribute?.DiscriminatorName ?? "discriminator";
        }

        public JsonInheritanceConverter(string discriminatorName)
        {
            _discriminatorName = discriminatorName;
        }

        public string DiscriminatorName { get { return _discriminatorName; } }

        public override TBase Read(ref System.Text.Json.Utf8JsonReader reader, System.Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
        {
            var document = System.Text.Json.JsonDocument.ParseValue(ref reader);
            var hasDiscriminator = document.RootElement.TryGetProperty(_discriminatorName, out var discriminator);
            var subtype = GetDiscriminatorType(document.RootElement, typeToConvert, hasDiscriminator ? discriminator.GetString() : null);

            var bufferWriter = new System.IO.MemoryStream();
            using (var writer = new System.Text.Json.Utf8JsonWriter(bufferWriter))
            {
                document.RootElement.WriteTo(writer);
            }

            return (TBase)System.Text.Json.JsonSerializer.Deserialize(bufferWriter.ToArray(), subtype, options);
        }

        public override void Write(System.Text.Json.Utf8JsonWriter writer, TBase value, System.Text.Json.JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString(_discriminatorName, GetDiscriminatorValue(value.GetType()));

            var bytes = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes((object)value, options);
            var document = System.Text.Json.JsonDocument.Parse(bytes);
            foreach (var property in document.RootElement.EnumerateObject())
            {
                property.WriteTo(writer);
            }

            writer.WriteEndObject();
        }

        public string GetDiscriminatorValue(System.Type type)
        {
            var jsonInheritanceAttributeDiscriminator = GetSubtypeDiscriminator(type);
            if (jsonInheritanceAttributeDiscriminator != null)
            {
                return jsonInheritanceAttributeDiscriminator;
            }

            return type.Name;
        }

        protected System.Type GetDiscriminatorType(System.Text.Json.JsonElement jObject, System.Type objectType, string discriminatorValue)
        {
            var jsonInheritanceAttributeSubtype = GetObjectSubtype(objectType, discriminatorValue);
            if (jsonInheritanceAttributeSubtype != null)
            {
                return jsonInheritanceAttributeSubtype;
            }

            if (objectType.Name == discriminatorValue)
            {
                return objectType;
            }

            var typeName = objectType.Namespace + "." + discriminatorValue;
            var subtype = System.Reflection.IntrospectionExtensions.GetTypeInfo(objectType).Assembly.GetType(typeName);
            if (subtype != null)
            {
                return subtype;
            }

            throw new System.InvalidOperationException("Could not find subtype of '" + objectType.Name + "' with discriminator '" + discriminatorValue + "'.");
        }

        private System.Type GetObjectSubtype(System.Type objectType, string discriminator)
        {
            foreach (var attribute in System.Reflection.CustomAttributeExtensions.GetCustomAttributes<JsonInheritanceAttribute>(System.Reflection.IntrospectionExtensions.GetTypeInfo(objectType), true))
            {
                if (attribute.Key == discriminator)
                    return attribute.Type;
            }

            return objectType;
        }

        private string GetSubtypeDiscriminator(System.Type objectType)
        {
            foreach (var attribute in System.Reflection.CustomAttributeExtensions.GetCustomAttributes<JsonInheritanceAttribute>(System.Reflection.IntrospectionExtensions.GetTypeInfo(objectType), true))
            {
                if (attribute.Type == objectType)
                    return attribute.Key;
            }

            return objectType.Name;
        }
    }


}

#pragma warning restore  108
#pragma warning restore  114
#pragma warning restore  472
#pragma warning restore  612
#pragma warning restore 1573
#pragma warning restore 1591
#pragma warning restore 8073
#pragma warning restore 3016
#pragma warning restore 8600
#pragma warning restore 8602
#pragma warning restore 8603
#pragma warning restore 8604
#pragma warning restore 8625