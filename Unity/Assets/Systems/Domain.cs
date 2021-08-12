namespace Domain
{
    using System;
    using System.Collections.Generic;

    using System.Globalization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// This document is a schema for input data and a design whiteboard.
    /// </summary>
    public partial class City
    {
        [JsonProperty("intersections", NullValueHandling = NullValueHandling.Ignore)]
        public List<Intersection> Intersections { get; set; }

        [JsonProperty("streets", NullValueHandling = NullValueHandling.Ignore)]
        public List<Street> Streets { get; set; }

        [JsonProperty("vehicles", NullValueHandling = NullValueHandling.Ignore)]
        public Vehicles Vehicles { get; set; }
    }

    /// <summary>
    /// An intersection must be linked at least to three Streets.
    /// </summary>
    public partial class Intersection
    {
        [JsonProperty("adjacentStreets", NullValueHandling = NullValueHandling.Ignore)]
        public List<AdjacentStreet> AdjacentStreets { get; set; }

        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public long? Id { get; set; }

        [JsonProperty("isRoundabout", NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsRoundabout { get; set; }
    }

    public partial class AdjacentStreet
    {
        [JsonProperty("hasSemaphore", NullValueHandling = NullValueHandling.Ignore)]
        public bool? HasSemaphore { get; set; }

        /// <summary>
        /// The id of the adjacent street
        /// </summary>
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public long? Id { get; set; }
    }

    public partial class Street
    {
        [JsonProperty("endingIntersectionId", NullValueHandling = NullValueHandling.Ignore)]
        public long? EndingIntersectionId { get; set; }

        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public long? Id { get; set; }

        [JsonProperty("isOneWay")]
        public bool IsOneWay { get; set; }

        /// <summary>
        /// The length of a Street sets the maximum capacity of it; therefore, it is useful also for
        /// the algorithm.
        /// </summary>
        [JsonProperty("length", NullValueHandling = NullValueHandling.Ignore)]
        public double? Length { get; set; }

        [JsonProperty("semiCarriageways", NullValueHandling = NullValueHandling.Ignore)]
        public List<SemiCarriageway> SemiCarriageways { get; set; }

        [JsonProperty("startingIntersectionId", NullValueHandling = NullValueHandling.Ignore)]
        public long? StartingIntersectionId { get; set; }
    }

    public partial class SemiCarriageway
    {
        [JsonProperty("hasBusStop", NullValueHandling = NullValueHandling.Ignore)]
        public bool? HasBusStop { get; set; }

        [JsonProperty("lanesAmount", NullValueHandling = NullValueHandling.Ignore)]
        public long? LanesAmount { get; set; }
    }

    public partial class Vehicles
    {
        [JsonProperty("buses", NullValueHandling = NullValueHandling.Ignore)]
        public Buses Buses { get; set; }

        [JsonProperty("cars", NullValueHandling = NullValueHandling.Ignore)]
        public Cars Cars { get; set; }
    }

    public partial class Buses
    {
        [JsonProperty("amount", NullValueHandling = NullValueHandling.Ignore)]
        public long? Amount { get; set; }

        /// <summary>
        /// You can only specify the length of buses.
        /// </summary>
        [JsonProperty("length", NullValueHandling = NullValueHandling.Ignore)]
        public double? Length { get; set; }
    }

    public partial class Cars
    {
        [JsonProperty("amount", NullValueHandling = NullValueHandling.Ignore)]
        public long? Amount { get; set; }

        /// <summary>
        /// You can only specify the length of cars.
        /// </summary>
        [JsonProperty("length", NullValueHandling = NullValueHandling.Ignore)]
        public double? Length { get; set; }
    }

    public partial class City
    {
        public static City FromJson(string json) => JsonConvert.DeserializeObject<City>(json, Domain.Converter.Settings);
    }

    public static class Serialize
    {
        public static string ToJson(this City self) => JsonConvert.SerializeObject(self, Domain.Converter.Settings);
    }

    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters =
            {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }

}

