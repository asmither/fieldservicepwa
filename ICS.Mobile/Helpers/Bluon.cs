using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using static ICS.Portal.Data.Custom.BluonModels.ModelInfo;
using System.Threading;
using ICS.Mobile.Helpers;
using ICS.Mobile.DataModels.Custom;
using static ICS.Portal.Data.Custom.BluonModels.ModelManuals;
using Azure.Core;

namespace ICS.Portal.Data.Custom;

public class BluonBase
{

    #region Constructor and Globals / Base API and HTTP 

    protected string apikey { get; set; } = string.Empty;
    protected string apiroot { get; set; } = string.Empty;
    protected string apiuser { get; set; } = string.Empty; // Bluon API user
    protected int defaultTimeout { get; set; } = 20; // seconds

    protected HttpClient? client = null;

    public string LastErrorMessage { get; set; } = string.Empty; // visible to caller if get/post failed
    public bool LastError { get; set; } = false; // visible to caller if get/post failed

    protected IBluonCache? Cache { get; set; }  // OPTIONAL cache interface

    public BluonBase(IBluonCache? cache = null)
    {
        client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(defaultTimeout); // 15 seconds default timeout
        Cache = cache;
    }
    public BluonBase() : this(null)
    {
    }

    ~BluonBase()
    {
        client?.Dispose();
        client = null;
    }
    #endregion

    #region Shared Classes common to other Bluon Entities

    public class Conversions
    {
        [JsonProperty("thumb", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("thumb")]
        public string? Thumb { get; set; }
    }

    public class Image
    {
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("id")]
        public string? Id { get; set; } = string.Empty;

        [JsonProperty("url", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonProperty("conversions", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("conversions")]
        public object? Conversions { get; set; }
    }

    public class Link
    {
        [JsonProperty("url", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonProperty("label", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("label")]
        public string? Label { get; set; }

        [JsonProperty("active", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("active")]
        public bool? Active { get; set; }

        [JsonProperty("first", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("first")]
        public string? First { get; set; }

        [JsonProperty("last", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("last")]
        public string? Last { get; set; }

        [JsonProperty("prev", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("prev")]
        public string? Prev { get; set; }

        [JsonProperty("next", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("next")]
        public string? Next { get; set; }
    }

    public class Meta
    {
        [JsonProperty("current_page", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("current_page")]
        public int CurrentPage { get; set; } = 0;

        [JsonProperty("from", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("from")]
        public int From { get; set; } = 0;

        [JsonProperty("last_page", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("last_page")]
        public int LastPage { get; set; } = 0;

        [JsonProperty("links", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("links")]
        public List<Link>? Links { get; set; }

        [JsonProperty("path", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("path")]
        public string? Path { get; set; }

        [JsonProperty("per_page", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("per_page")]
        public int perpage { get; set; } = 0;

        [JsonProperty("to", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("to")]
        public int To { get; set; } = 0;

        [JsonProperty("total", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("total")]
        public int Total { get; set; } = 0;
    }

    public partial class PartSpecifications
    {
        #region Individual Properties
        [JsonProperty("application", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("application")]
        public string? Application { get; set; }

        [JsonProperty("signal_type", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("signal_type")]
        public string? SignalType { get; set; }

        [JsonProperty("measurement_range", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("measurement_range")]
        public string? MeasurementRange { get; set; }

        [JsonProperty("connection_type", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("connection_type")]
        public string? ConnectionType { get; set; }

        [JsonProperty("configuration", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("configuration")]
        public string? Configuration { get; set; }

        [JsonProperty("number_of_wires", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("number_of_wires")]
        public string? NumberOfWires { get; set; }

        [JsonProperty("accuracy", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("accuracy")]
        public string? Accuracy { get; set; }

        [JsonProperty("enclosure_rating", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("enclosure_rating")]
        public string? EnclosureRating { get; set; }

        [JsonProperty("lead_length", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("lead_length")]
        public string? LeadLength { get; set; }

        [JsonProperty("operating_temperature", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("operating_temperature")]
        public string? OperatingTemperature { get; set; }

        [JsonProperty("motor_type", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("motor_type")]
        public string? MotorType { get; set; }

        [JsonProperty("duty_rating", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("duty_rating")]
        public string? DutyRating { get; set; }

        [JsonProperty("voltage", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("voltage")]
        public string? Voltage { get; set; }

        [JsonProperty("ph", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("ph")]
        public string? Ph { get; set; }

        [JsonProperty("hz", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("hz")]
        public string? Hz { get; set; }

        [JsonProperty("run_capacitor_size", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("run_capacitor_size")]
        public string? RunCapacitorSize { get; set; }

        [JsonProperty("start_capacitor_size", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("start_capacitor_size")]
        public string? StartCapacitorSize { get; set; }

        [JsonProperty("rpm", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("rpm")]
        public string? Rpm { get; set; }

        [JsonProperty("output_hp", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("output_hp")]
        public string? OutputHp { get; set; }

        [JsonProperty("frame_type", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("frame_type")]
        public string? FrameType { get; set; }

        [JsonProperty("rotation", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("rotation")]
        public string? Rotation { get; set; }

        [JsonProperty("speed", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("speed")]
        public string? Speed { get; set; }

        [JsonProperty("shaft_diameter", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("shaft_diameter")]
        public string? ShaftDiameter { get; set; }

        [JsonProperty("shaft_keyway", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("shaft_keyway")]
        public string? ShaftKeyway { get; set; }

        [JsonProperty("shaft_length", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("shaft_length")]
        public string? ShaftLength { get; set; }

        [JsonProperty("shaft_type", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("shaft_type")]
        public string? ShaftType { get; set; }

        [JsonProperty("bearing_type", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("bearing_type")]
        public string? BearingType { get; set; }

        [JsonProperty("fla", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("fla")]
        public string? Fla { get; set; }

        [JsonProperty("mounting_type", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("mounting_type")]
        public string? MountingType { get; set; }

        [JsonProperty("replaceable_bearings", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("replaceable_bearings")]
        public string? ReplaceableBearings { get; set; }

        [JsonProperty("motor_diameter", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("motor_diameter")]
        public string? MotorDiameter { get; set; }

        [JsonProperty("motor_height", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("motor_height")]
        public string? MotorHeight { get; set; }

        [JsonProperty("enclosure_type", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("enclosure_type")]
        public string? EnclosureType { get; set; }

        [JsonProperty("material_type", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("material_type")]
        public string? MaterialType { get; set; }

        [JsonProperty("weight", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("weight")]
        public string? Weight { get; set; }

        [JsonProperty("protection", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("protection")]
        public string? Protection { get; set; }

        [JsonProperty("rla", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("rla")]
        public string? Rla { get; set; }

        [JsonProperty("lra", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("lra")]
        public string? Lra { get; set; }

        [JsonProperty("service_factor", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("service_factor")]
        public string? ServiceFactor { get; set; }

        [JsonProperty("power_factor", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("power_factor")]
        public string? PowerFactor { get; set; }

        [JsonProperty("cfm", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("cfm")]
        public string? Cfm { get; set; }

        [JsonProperty("efficiency", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("efficiency")]
        public string? Efficiency { get; set; }

        [JsonProperty("alternate_part_numbers", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("alternate_part_numbers")]
        public string? AlternatePartNumbers { get; set; }

        [JsonProperty("output_watts", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("output_watts")]
        public string? OutputWatts { get; set; }

        [JsonProperty("input_watts", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("input_watts")]
        public string? InputWatts { get; set; }

        [JsonProperty("ring_size", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("ring_size")]
        public string? RingSize { get; set; }

        [JsonProperty("resilient_ring_dimension", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("resilient_ring_dimension")]
        public string? ResilientRingDimension { get; set; }

        [JsonProperty("armature_amps", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("armature_amps")]
        public string? ArmatureAmps { get; set; }

        [JsonProperty("field_amps", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("field_amps")]
        public string? FieldAmps { get; set; }

        [JsonProperty("service_factor_amps", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("service_factor_amps")]
        public string? ServiceFactorAmps { get; set; }

        [JsonProperty("multi_voltage", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("multi_voltage")]
        public string? MultiVoltage { get; set; }

        [JsonProperty("rotation_orientation", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("rotation_orientation")]
        public string? RotationOrientation { get; set; }

        [JsonProperty("mounting_angle", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("mounting_angle")]
        public string? MountingAngle { get; set; }

        [JsonProperty("conduit_box_orientation", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("conduit_box_orientation")]
        public string? ConduitBoxOrientation { get; set; }

        [JsonProperty("torque_type", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("torque_type")]
        public string? TorqueType { get; set; }

        [JsonProperty("drive_type", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("drive_type")]
        public string? DriveType { get; set; }

        [JsonProperty("misc", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("misc")]
        public string? Misc { get; set; }

        [JsonProperty("armature_voltage", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("armature_voltage")]
        public string? ArmatureVoltage { get; set; }

        [JsonProperty("field_voltage", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("field_voltage")]
        public string? FieldVoltage { get; set; }

        [JsonProperty("start_type", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("start_type")]
        public string? StartType { get; set; }

        [JsonProperty("output_voltage", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("output_voltage")]
        public string? OutputVoltage { get; set; }

        [JsonProperty("trademark_name", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("trademark_name")]
        public string? TrademarkName { get; set; }

        [JsonProperty("module_part_number", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("module_part_number")]
        public string? ModulePartNumber { get; set; }

        [JsonProperty("included_with", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("included_with")]
        public string? IncludedWith { get; set; }

        [JsonProperty("electrical_notes", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("electrical_notes")]
        public string? ElectricalNotes { get; set; }

        [JsonProperty("constant_torque_speed", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("constant_torque_speed")]
        public string? ConstantTorqueSpeed { get; set; }

        [JsonProperty("variable_torque_speed", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("variable_torque_speed")]
        public string? VariableTorqueSpeed { get; set; }

        [JsonProperty("start_torque", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("start_torque")]
        public string? StartTorque { get; set; }

        [JsonProperty("run_torque", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("run_torque")]
        public string? RunTorque { get; set; }

        [JsonProperty("full_load_torque", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("full_load_torque")]
        public string? FullLoadTorque { get; set; }

        [JsonProperty("load_factor", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("load_factor")]
        public string? LoadFactor { get; set; }

        [JsonProperty("frame_diameter", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("frame_diameter")]
        public string? FrameDiameter { get; set; }

        [JsonProperty("cooling_type", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("cooling_type")]
        public string? CoolingType { get; set; }

        [JsonProperty("notes", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonProperty("a_dimension", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("a_dimension")]
        public string? ADimension { get; set; }

        [JsonProperty("b_dimension", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("b_dimension")]
        public string? BDimension { get; set; }

        [JsonProperty("total_length", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("total_length")]
        public string? TotalLength { get; set; }

        [JsonProperty("efficiency_type", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("efficiency_type")]
        public string? EfficiencyType { get; set; }

        [JsonProperty("shaft_dimensions", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("shaft_dimensions")]
        public string? ShaftDimensions { get; set; }

        [JsonProperty("fan_blade_dimensions", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("fan_blade_dimensions")]
        public string? FanBladeDimensions { get; set; }

        [JsonProperty("stack", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("stack")]
        public string? Stack { get; set; }

        [JsonProperty("part_type", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("part_type")]
        public string? PartType { get; set; }

        [JsonProperty("capacitor_part_number", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("capacitor_part_number")]
        public string? CapacitorPartNumber { get; set; }

        [JsonProperty("eisa_rating", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("eisa_rating")]
        public string? EisaRating { get; set; }

        [JsonProperty("bissc_rating", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("bissc_rating")]
        public string? BisscRating { get; set; }

        [JsonProperty("shaft_orientation", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("shaft_orientation")]
        public string? ShaftOrientation { get; set; }

        [JsonProperty("run_type", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("run_type")]
        public string? RunType { get; set; }

        [JsonProperty("number_of_poles", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("number_of_poles")]
        public string? NumberOfPoles { get; set; }

        [JsonProperty("correct_part_number", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("correct_part_number")]
        public string? CorrectPartNumber { get; set; }

        [JsonProperty("mount_part_number", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("mount_part_number")]
        public string? MountPartNumber { get; set; }

        [JsonProperty("iec_rating", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("iec_rating")]
        public string? IecRating { get; set; }

        [JsonProperty("braking_torque", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("braking_torque")]
        public string? BrakingTorque { get; set; }

        [JsonProperty("winding_type", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("winding_type")]
        public string? WindingType { get; set; }

        [JsonProperty("source_info", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("source_info")]
        public string? SourceInfo { get; set; }

        [JsonProperty("input_voltage", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("input_voltage")]
        public string? InputVoltage { get; set; }

        [JsonProperty("mechanical_hp", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("mechanical_hp")]
        public string? MechanicalHp { get; set; }

        [JsonProperty("hub_to_hub", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("hub_to_hub")]
        public string? HubToHub { get; set; }

        [JsonProperty("nominal_capacity", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("nominal_capacity")]
        public string? NominalCapacity { get; set; }

        [JsonProperty("wheel_dimensions", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("wheel_dimensions")]
        public string? WheelDimensions { get; set; }

        [JsonProperty("poles", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("poles")]
        public string? Poles { get; set; }

        [JsonProperty("shunts", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("shunts")]
        public string? Shunts { get; set; }

        [JsonProperty("coil_voltage", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("coil_voltage")]
        public string? CoilVoltage { get; set; }

        [JsonProperty("operating_voltage", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("operating_voltage")]
        public string? OperatingVoltage { get; set; }

        [JsonProperty("termination_type", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("termination_type")]
        public string? TerminationType { get; set; }

        [JsonProperty("resistive_amps", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("resistive_amps")]
        public string? ResistiveAmps { get; set; }

        [JsonProperty("noninductive_amps", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("noninductive_amps")]
        public string? NoninductiveAmps { get; set; }

        [JsonProperty("auxialliary_contacts", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("auxialliary_contacts")]
        public string? AuxialliaryContacts { get; set; }

        [JsonProperty("push_to_test_window", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("push_to_test_window")]
        public string? PushToTestWindow { get; set; }

        [JsonProperty("contactor_type", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("contactor_type")]
        public string? ContactorType { get; set; }

        [JsonProperty("height", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("height")]
        public string? Height { get; set; }

        [JsonProperty("width", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("width")]
        public string? Width { get; set; }

        [JsonProperty("length", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("length")]
        public string? Length { get; set; }

        [JsonProperty("coil_type", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("coil_type")]
        public string? CoilType { get; set; }

        [JsonProperty("max_hp", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("max_hp")]
        public string? MaxHp { get; set; }

        [JsonProperty("fuse_clip_size", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("fuse_clip_size")]
        public string? FuseClipSize { get; set; }

        [JsonProperty("temperature_rating", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("temperature_rating")]
        public string? TemperatureRating { get; set; }

        [JsonProperty("current_setting_range", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("current_setting_range")]
        public string? CurrentSettingRange { get; set; }

        [JsonProperty("reset_type", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("reset_type")]
        public string? ResetType { get; set; }

        [JsonProperty("accessories", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("accessories")]
        public string? Accessories { get; set; }

        [JsonProperty("overload_relays", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("overload_relays")]
        public string? OverloadRelays { get; set; }

        [JsonProperty("overload_time", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("overload_time")]
        public string? OverloadTime { get; set; }

        [JsonProperty("fused", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("fused")]
        public string? Fused { get; set; }

        [JsonProperty("microfarads", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("microfarads")]
        public string? Microfarads { get; set; }

        [JsonProperty("shape", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("shape")]
        public string? Shape { get; set; }

        [JsonProperty("tolerance", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("tolerance")]
        public string? Tolerance { get; set; }

        [JsonProperty("depth", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("depth")]
        public string? Depth { get; set; }

        [JsonProperty("part_number_correction", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("part_number_correction")]
        public string? PartNumberCorrection { get; set; }

        [JsonProperty("diameter", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("diameter")]
        public string? Diameter { get; set; }

        [JsonProperty("number_of_blades", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("number_of_blades")]
        public int? NumberOfBlades { get; set; }

        [JsonProperty("pitch", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("pitch")]
        public string? Pitch { get; set; }

        [JsonProperty("bore", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("bore")]
        public string? Bore { get; set; }

        [JsonProperty("bhp", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("bhp")]
        public string? Bhp { get; set; }

        [JsonProperty("material", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("material")]
        public string? Material { get; set; }

        [JsonProperty("rated_refrigerant", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("rated_refrigerant")]
        public string? RatedRefrigerant { get; set; }

        [JsonProperty("oil_type", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("oil_type")]
        public string? OilType { get; set; }

        [JsonProperty("nominal_capacity_tons", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("nominal_capacity_tons")]
        public string? NominalCapacityTons { get; set; }

        [JsonProperty("nominal_capacity_btuh", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("nominal_capacity_btuh")]
        public string? NominalCapacityBtuh { get; set; }

        [JsonProperty("run_capacitor", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("run_capacitor")]
        public string? RunCapacitor { get; set; }

        [JsonProperty("start_capacitor", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("start_capacitor")]
        public string? StartCapacitor { get; set; }

        [JsonProperty("suction_inlet_diameter", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("suction_inlet_diameter")]
        public string? SuctionInletDiameter { get; set; }

        [JsonProperty("discharge_diameter", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("discharge_diameter")]
        public string? DischargeDiameter { get; set; }

        [JsonProperty("number_of_cylinders", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("number_of_cylinders")]
        public string? NumberOfCylinders { get; set; }

        [JsonProperty("number_of_unloaders", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("number_of_unloaders")]
        public string? NumberOfUnloaders { get; set; }

        [JsonProperty("crankcase_heater", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("crankcase_heater")]
        public string? CrankcaseHeater { get; set; }

        [JsonProperty("eer", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("eer")]
        public string? Eer { get; set; }

        [JsonProperty("displacement", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("displacement")]
        public string? Displacement { get; set; }

        [JsonProperty("nominal_hp", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("nominal_hp")]
        public string? NominalHp { get; set; }

        [JsonProperty("nominal_power_watts", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("nominal_power_watts")]
        public string? NominalPowerWatts { get; set; }

        [JsonProperty("compressor_length", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("compressor_length")]
        public string? CompressorLength { get; set; }

        [JsonProperty("compressor_width", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("compressor_width")]
        public string? CompressorWidth { get; set; }

        [JsonProperty("compressor_height", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("compressor_height")]
        public string? CompressorHeight { get; set; }

        [JsonProperty("volume", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("volume")]
        public int? Volume { get; set; }

        [JsonProperty("inlet_diameter", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("inlet_diameter")]
        public string? InletDiameter { get; set; }

        [JsonProperty("inlet_connection_type", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("inlet_connection_type")]
        public string? InletConnectionType { get; set; }

        [JsonProperty("outlet_diameter", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("outlet_diameter")]
        public string? OutletDiameter { get; set; }

        [JsonProperty("outlet_connection_type", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("outlet_connection_type")]
        public string? OutletConnectionType { get; set; }

        [JsonProperty("direction_of_flow", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("direction_of_flow")]
        public string? DirectionOfFlow { get; set; }

        [JsonProperty("desiccant_type", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("desiccant_type")]
        public string? DesiccantType { get; set; }

        [JsonProperty("number_of_cores", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("number_of_cores")]
        public string? NumberOfCores { get; set; }

        [JsonProperty("options", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("options")]
        public string? Options { get; set; }

        [JsonProperty("rated_capacity", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("rated_capacity")]
        public string? RatedCapacity { get; set; }

        [JsonProperty("equalizer", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("equalizer")]
        public string? Equalizer { get; set; }

        [JsonProperty("external_equalizer_connection", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("external_equalizer_connection")]
        public string? ExternalEqualizerConnection { get; set; }

        [JsonProperty("bidirectional", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("bidirectional")]
        public string? Bidirectional { get; set; }

        [JsonProperty("adjustable", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("adjustable")]
        public string? Adjustable { get; set; }

        [JsonProperty("supply_voltage", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("supply_voltage")]
        public string? SupplyVoltage { get; set; }

        [JsonProperty("control_steps", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("control_steps")]
        public string? ControlSteps { get; set; }

        [JsonProperty("step_rate", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("step_rate")]
        public string? StepRate { get; set; }

        [JsonProperty("orfice_size", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("orfice_size")]
        public string? OrficeSize { get; set; }

        [JsonProperty("capillary_tube_length", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("capillary_tube_length")]
        public string? CapillaryTubeLength { get; set; }

        [JsonProperty("number_of_headers", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("number_of_headers")]
        public string? NumberOfHeaders { get; set; }

        [JsonProperty("spring_type", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("spring_type")]
        public string? SpringType { get; set; }

        [JsonProperty("check_valve", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("check_valve")]
        public string? CheckValve { get; set; }

        [JsonProperty("hermetic", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("hermetic")]
        public string? Hermetic { get; set; }

        [JsonProperty("balanced_port", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("balanced_port")]
        public string? BalancedPort { get; set; }

        [JsonProperty("applications", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("applications")]
        public string? Applications { get; set; }

        [JsonProperty("element_size", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("element_size")]
        public string? ElementSize { get; set; }

        [JsonProperty("body_type", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("body_type")]
        public string? BodyType { get; set; }

        [JsonProperty("thermostatic_charge", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("thermostatic_charge")]
        public string? ThermostaticCharge { get; set; }

        [JsonProperty("mesh_strainer", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("mesh_strainer")]
        public string? MeshStrainer { get; set; }

        [JsonProperty("max_operating_pressures", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("max_operating_pressures")]
        public string? MaxOperatingPressures { get; set; }

        [JsonProperty("max_differential_pressure_drop", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("max_differential_pressure_drop")]
        public string? MaxDifferentialPressureDrop { get; set; }

        [JsonProperty("ambient_temperature", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("ambient_temperature")]
        public string? AmbientTemperature { get; set; }

        [JsonProperty("refrigerant_temperature", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("refrigerant_temperature")]
        public string? RefrigerantTemperature { get; set; }

        [JsonProperty("current", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("current")]
        public string? Current { get; set; }

        [JsonProperty("drive_frequency", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("drive_frequency")]
        public string? DriveFrequency { get; set; }

        [JsonProperty("phase_resistance", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("phase_resistance")]
        public string? PhaseResistance { get; set; }

        [JsonProperty("compatible_oils", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("compatible_oils")]
        public string? CompatibleOils { get; set; }

        [JsonProperty("cable_type", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("cable_type")]
        public string? CableType { get; set; }

        [JsonProperty("max_power_input", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("max_power_input")]
        public string? MaxPowerInput { get; set; }

        [JsonProperty("step_angle", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("step_angle")]
        public string? StepAngle { get; set; }

        [JsonProperty("resolution", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("resolution")]
        public string? Resolution { get; set; }

        [JsonProperty("connections", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("connections")]
        public string? Connections { get; set; }

        [JsonProperty("closing_steps", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("closing_steps")]
        public string? ClosingSteps { get; set; }

        [JsonProperty("minimum_steps", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("minimum_steps")]
        public string? MinimumSteps { get; set; }

        [JsonProperty("hold_current", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("hold_current")]
        public string? HoldCurrent { get; set; }

        [JsonProperty("percent_duty", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("percent_duty")]
        public string? PercentDuty { get; set; }

        [JsonProperty("stroke", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("stroke")]
        public string? Stroke { get; set; }

        [JsonProperty("max_internal_leakage", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("max_internal_leakage")]
        public string? MaxInternalLeakage { get; set; }

        [JsonProperty("max_external_leakage", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("max_external_leakage")]
        public string? MaxExternalLeakage { get; set; }

        [JsonProperty("programmable", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("programmable")]
        public string? Programmable { get; set; }

        [JsonProperty("wifi", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("wifi")]
        public string? Wifi { get; set; }

        [JsonProperty("power_requirements", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("power_requirements")]
        public string? PowerRequirements { get; set; }

        [JsonProperty("switch", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("switch")]
        public string? Switch { get; set; }

        [JsonProperty("action", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("action")]
        public string? Action { get; set; }

        [JsonProperty("operation_of_contacts", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("operation_of_contacts")]
        public string? OperationOfContacts { get; set; }

        [JsonProperty("range_minimum", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("range_minimum")]
        public string? RangeMinimum { get; set; }

        [JsonProperty("range_maximum", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("range_maximum")]
        public string? RangeMaximum { get; set; }

        [JsonProperty("reset_minimum", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("reset_minimum")]
        public string? ResetMinimum { get; set; }

        [JsonProperty("reset_maximum", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("reset_maximum")]
        public string? ResetMaximum { get; set; }

        [JsonProperty("differential_minimum", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("differential_minimum")]
        public string? DifferentialMinimum { get; set; }

        [JsonProperty("differential_maximum", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("differential_maximum")]
        public string? DifferentialMaximum { get; set; }

        [JsonProperty("setpoint", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("setpoint")]
        public string? Setpoint { get; set; }

        [JsonProperty("reset", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("reset")]
        public string? Reset { get; set; }

        [JsonProperty("capillary_length", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("capillary_length")]
        public string? CapillaryLength { get; set; }

        [JsonProperty("max_amps", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("max_amps")]
        public string? MaxAmps { get; set; }

        [JsonProperty("max_volts", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("max_volts")]
        public string? MaxVolts { get; set; }

        [JsonProperty("replaceable_bulb", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("replaceable_bulb")]
        public string? ReplaceableBulb { get; set; }

        [JsonProperty("mount", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("mount")]
        public string? Mount { get; set; }

        [JsonProperty("sort", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("sort")]
        public string? Sort { get; set; }

        [JsonProperty("ac_contact_rating", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("ac_contact_rating")]
        public string? AcContactRating { get; set; }

        [JsonProperty("actual_depth", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("actual_depth")]
        public string? ActualDepth { get; set; }

        [JsonProperty("actual_length", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("actual_length")]
        public string? ActualLength { get; set; }

        [JsonProperty("actual_width", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("actual_width")]
        public string? ActualWidth { get; set; }

        [JsonProperty("amp_rating", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("amp_rating")]
        public string? AmpRating { get; set; }

        [JsonProperty("amperage", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("amperage")]
        public string? Amperage { get; set; }

        [JsonProperty("base_type", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("base_type")]
        public string? BaseType { get; set; }

        [JsonProperty("belt_length", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("belt_length")]
        public string? BeltLength { get; set; }

        [JsonProperty("belt_type", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("belt_type")]
        public string? BeltType { get; set; }

        [JsonProperty("bore_diameter", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("bore_diameter")]
        public string? BoreDiameter { get; set; }

        [JsonProperty("bore_mate_type", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("bore_mate_type")]
        public string? BoreMateType { get; set; }

        [JsonProperty("bushing_connection", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("bushing_connection")]
        public string? BushingConnection { get; set; }

        [JsonProperty("capacity", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("capacity")]
        public string? Capacity { get; set; }

        [JsonProperty("center_disc", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("center_disc")]
        public string? CenterDisc { get; set; }

        [JsonProperty("ceramic_block", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("ceramic_block")]
        public string? CeramicBlock { get; set; }

        [JsonProperty("cold_resistance", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("cold_resistance")]
        public string? ColdResistance { get; set; }

        [JsonProperty("compression_fitting_diameter", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("compression_fitting_diameter")]
        public string? CompressionFittingDiameter { get; set; }

        [JsonProperty("dc_contact_rating", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("dc_contact_rating")]
        public string? DcContactRating { get; set; }

        [JsonProperty("delay_on_break", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("delay_on_break")]
        public string? DelayOnBreak { get; set; }

        [JsonProperty("delay_on_make", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("delay_on_make")]
        public string? DelayOnMake { get; set; }

        [JsonProperty("electrical_rating", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("electrical_rating")]
        public string? ElectricalRating { get; set; }

        [JsonProperty("factory_settings", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("factory_settings")]
        public string? FactorySettings { get; set; }

        [JsonProperty("family", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("family")]
        public string? Family { get; set; }

        [JsonProperty("gas_cock_dial_markings", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("gas_cock_dial_markings")]
        public string? GasCockDialMarkings { get; set; }

        [JsonProperty("gas_type", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("gas_type")]
        public string? GasType { get; set; }

        [JsonProperty("hub_lock", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("hub_lock")]
        public string? HubLock { get; set; }

        [JsonProperty("inlet_size", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("inlet_size")]
        public string? InletSize { get; set; }

        [JsonProperty("keyway", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("keyway")]
        public string? Keyway { get; set; }

        [JsonProperty("keyway_height", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("keyway_height")]
        public string? KeywayHeight { get; set; }

        [JsonProperty("keyway_types", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("keyway_types")]
        public string? KeywayTypes { get; set; }

        [JsonProperty("keyway_width", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("keyway_width")]
        public string? KeywayWidth { get; set; }

        [JsonProperty("m1_m2_off_time", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("m1_m2_off_time")]
        public string? M1M2OffTime { get; set; }

        [JsonProperty("m1_m2_on_time", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("m1_m2_on_time")]
        public string? M1M2OnTime { get; set; }

        [JsonProperty("m3_m4_off_time", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("m3_m4_off_time")]
        public string? M3M4OffTime { get; set; }

        [JsonProperty("m3_m4_on_time", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("m3_m4_on_time")]
        public string? M3M4OnTime { get; set; }

        [JsonProperty("m5_m6_off_time", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("m5_m6_off_time")]
        public string? M5M6OffTime { get; set; }

        [JsonProperty("m5_m6_on_time", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("m5_m6_on_time")]
        public string? M5M6OnTime { get; set; }

        [JsonProperty("m7_m8_off_time", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("m7_m8_off_time")]
        public string? M7M8OffTime { get; set; }

        [JsonProperty("m7_m8_on_time", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("m7_m8_on_time")]
        public string? M7M8OnTime { get; set; }

        [JsonProperty("m9_m10_off_time", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("m9_m10_off_time")]
        public string? M9M10OffTime { get; set; }

        [JsonProperty("m9_m10_on_time", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("m9_m10_on_time")]
        public string? M9M10OnTime { get; set; }

        [JsonProperty("max_adjustable_setting", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("max_adjustable_setting")]
        public string? MaxAdjustableSetting { get; set; }

        [JsonProperty("max_capacitance", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("max_capacitance")]
        public string? MaxCapacitance { get; set; }

        [JsonProperty("max_current", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("max_current")]
        public string? MaxCurrent { get; set; }

        [JsonProperty("max_dimension", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("max_dimension")]
        public string? MaxDimension { get; set; }

        [JsonProperty("max_inlet_pressure", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("max_inlet_pressure")]
        public string? MaxInletPressure { get; set; }

        [JsonProperty("max_operating_temp", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("max_operating_temp")]
        public string? MaxOperatingTemp { get; set; }

        [JsonProperty("max_rpm", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("max_rpm")]
        public string? MaxRpm { get; set; }

        [JsonProperty("max_switching_voltage", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("max_switching_voltage")]
        public string? MaxSwitchingVoltage { get; set; }

        [JsonProperty("max_tons", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("max_tons")]
        public string? MaxTons { get; set; }

        [JsonProperty("maximum_dd", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("maximum_dd")]
        public string? MaximumDd { get; set; }

        [JsonProperty("media_type", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("media_type")]
        public string? MediaType { get; set; }

        [JsonProperty("merv_rating", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("merv_rating")]
        public string? MervRating { get; set; }

        [JsonProperty("min_adjustable_setting", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("min_adjustable_setting")]
        public string? MinAdjustableSetting { get; set; }

        [JsonProperty("min_capacitance", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("min_capacitance")]
        public string? MinCapacitance { get; set; }

        [JsonProperty("min_dimension", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("min_dimension")]
        public string? MinDimension { get; set; }

        [JsonProperty("min_hp", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("min_hp")]
        public string? MinHp { get; set; }

        [JsonProperty("min_switching_voltage", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("min_switching_voltage")]
        public string? MinSwitchingVoltage { get; set; }

        [JsonProperty("min_tons", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("min_tons")]
        public string? MinTons { get; set; }

        [JsonProperty("minimum_dd", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("minimum_dd")]
        public string? MinimumDd { get; set; }

        [JsonProperty("mounting", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("mounting")]
        public string? Mounting { get; set; }

        [JsonProperty("mounting_base", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("mounting_base")]
        public string? MountingBase { get; set; }

        [JsonProperty("mounting_relay", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("mounting_relay")]
        public string? MountingRelay { get; set; }

        [JsonProperty("nominal_depth", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("nominal_depth")]
        public string? NominalDepth { get; set; }

        [JsonProperty("nominal_length", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("nominal_length")]
        public string? NominalLength { get; set; }

        [JsonProperty("nominal_width", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("nominal_width")]
        public string? NominalWidth { get; set; }

        [JsonProperty("number_blades", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("number_blades")]
        public string? NumberBlades { get; set; }

        [JsonProperty("number_hubs", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("number_hubs")]
        public string? NumberHubs { get; set; }

        [JsonProperty("number_of_grooves", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("number_of_grooves")]
        public string? NumberOfGrooves { get; set; }

        [JsonProperty("number_of_pins", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("number_of_pins")]
        public string? NumberOfPins { get; set; }

        [JsonProperty("number_setscrews", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("number_setscrews")]
        public string? NumberSetscrews { get; set; }

        [JsonProperty("orifice_diameter", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("orifice_diameter")]
        public string? OrificeDiameter { get; set; }

        [JsonProperty("outlet_orientation", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("outlet_orientation")]
        public string? OutletOrientation { get; set; }

        [JsonProperty("outlet_size_type", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("outlet_size_type")]
        public string? OutletSizeType { get; set; }

        [JsonProperty("outside_diameter", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("outside_diameter")]
        public string? OutsideDiameter { get; set; }

        [JsonProperty("pilot_btu", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("pilot_btu")]
        public string? PilotBtu { get; set; }

        [JsonProperty("pilot_duty", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("pilot_duty")]
        public string? PilotDuty { get; set; }

        [JsonProperty("pilot_outlet_size", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("pilot_outlet_size")]
        public string? PilotOutletSize { get; set; }

        [JsonProperty("pilot_tube_length", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("pilot_tube_length")]
        public string? PilotTubeLength { get; set; }

        [JsonProperty("probe_diameter", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("probe_diameter")]
        public string? ProbeDiameter { get; set; }

        [JsonProperty("probe_length", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("probe_length")]
        public string? ProbeLength { get; set; }

        [JsonProperty("reducer_bushing", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("reducer_bushing")]
        public string? ReducerBushing { get; set; }

        [JsonProperty("remote_dial", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("remote_dial")]
        public string? RemoteDial { get; set; }

        [JsonProperty("resistive_watts", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("resistive_watts")]
        public string? ResistiveWatts { get; set; }

        [JsonProperty("rod_angle", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("rod_angle")]
        public string? RodAngle { get; set; }

        [JsonProperty("sensor_type", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("sensor_type")]
        public string? SensorType { get; set; }

        [JsonProperty("service_life", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("service_life")]
        public string? ServiceLife { get; set; }

        [JsonProperty("side_outlet_size_type", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("side_outlet_size_type")]
        public string? SideOutletSizeType { get; set; }

        [JsonProperty("socket_code", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("socket_code")]
        public string? SocketCode { get; set; }

        [JsonProperty("stages", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("stages")]
        public string? Stages { get; set; }

        [JsonProperty("standard_dial", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("standard_dial")]
        public string? StandardDial { get; set; }

        [JsonProperty("status_indicator", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("status_indicator")]
        public string? StatusIndicator { get; set; }

        [JsonProperty("steady_current", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("steady_current")]
        public string? SteadyCurrent { get; set; }

        [JsonProperty("temp_rating", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("temp_rating")]
        public string? TempRating { get; set; }

        [JsonProperty("temperature_range", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("temperature_range")]
        public string? TemperatureRange { get; set; }

        [JsonProperty("terminal_type", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("terminal_type")]
        public string? TerminalType { get; set; }

        [JsonProperty("thickness", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("thickness")]
        public string? Thickness { get; set; }

        [JsonProperty("throw_type", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("throw_type")]
        public string? ThrowType { get; set; }

        [JsonProperty("time_to_temp", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("time_to_temp")]
        public string? TimeToTemp { get; set; }

        [JsonProperty("tip_style", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("tip_style")]
        public string? TipStyle { get; set; }

        [JsonProperty("top_width", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("top_width")]
        public string? TopWidth { get; set; }

        [JsonProperty("torque_increase", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("torque_increase")]
        public string? TorqueIncrease { get; set; }

        [JsonProperty("type_of_gas", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("type_of_gas")]
        public string? TypeOfGas { get; set; }

        [JsonProperty("watts_power", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("watts_power")]
        public string? WattsPower { get; set; }

        [JsonProperty("wheel_type", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("wheel_type")]
        public string? WheelType { get; set; }

        #endregion

        #region Specification Properties

        // Cache property info for performance
        private static readonly PropertyInfo[] CachedProperties = typeof(PartSpecifications)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .ToArray();

        // Cache property names and their formatted versions
        private static readonly Dictionary<string, string> FormattedPropertyNames =
            CachedProperties.ToDictionary(
                p => p.Name,
                p => FormatPropertyName(p)
            );
        #endregion

        #region Dictionary Conversion Methods

        /// <summary>
        /// Converts all non-null and non-default properties to a dictionary
        /// </summary>
        public Dictionary<string, object> ToDictionary(bool includeEmptyStrings = false)
        {
            var result = new Dictionary<string, object>();

            foreach (var prop in CachedProperties)
            {
                var value = prop.GetValue(this);

                if (ShouldIncludeValue(value, includeEmptyStrings))
                {
                    var key = FormattedPropertyNames[prop.Name];
                    result[key] = value;
                }
            }

            return result;
        }

        /// <summary>
        /// Converts all non-null and non-default properties to a string dictionary
        /// </summary>
        public Dictionary<string, string> ToStringDictionary(bool includeEmptyStrings = false)
        {
            return ToDictionary(includeEmptyStrings).ToDictionary(
                kvp => kvp.Key,
                kvp => FormatValue(kvp.Value)
            );
        }

        /// <summary>
        /// Gets a dictionary filtered by property type
        /// </summary>
        public Dictionary<string, T> ToDictionaryOfType<T>(bool includeDefaults = false)
        {
            var result = new Dictionary<string, T>();

            foreach (var prop in CachedProperties)
            {
                if (prop.PropertyType == typeof(T) ||
                    (typeof(T) == typeof(object) && prop.PropertyType.IsAssignableFrom(typeof(T))))
                {
                    var value = prop.GetValue(this);

                    if (value is T typedValue)
                    {
                        if (includeDefaults || !IsDefaultValue(typedValue))
                        {
                            var key = FormattedPropertyNames[prop.Name];
                            result[key] = typedValue;
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Gets specifications grouped by category (based on property name patterns)
        /// </summary>
        public Dictionary<string, Dictionary<string, object>> ToGroupedDictionary()
        {
            var allSpecs = ToDictionary();

            var grouped = new Dictionary<string, Dictionary<string, object>>();

            foreach (var kvp in allSpecs)
            {
                var category = DetermineCategory(kvp.Key);

                if (!grouped.ContainsKey(category))
                    grouped[category] = new Dictionary<string, object>();

                grouped[category][kvp.Key] = kvp.Value;
            }

            return grouped;
        }

        /// <summary>
        /// Gets a formatted string representation of all specifications
        /// </summary>
        public string ToFormattedString(string separator = "\n", string keyValueSeparator = ": ")
        {
            var specs = ToStringDictionary();
            if (!specs.Any())
                return "No specifications available";

            var maxKeyLength = specs.Keys.Max(k => k.Length);
            var formatted = specs.Select(kvp =>
                $"{kvp.Key.PadRight(maxKeyLength)}{keyValueSeparator}{kvp.Value}");

            return string.Join(separator, formatted);
        }

        /// <summary>
        /// Gets specifications as a list of tuples for easy iteration
        /// </summary>
        public List<(string Name, string Value, Type ValueType)> ToSpecificationList()
        {
            var result = new List<(string, string, Type)>();

            foreach (var prop in CachedProperties)
            {
                var value = prop.GetValue(this);

                if (ShouldIncludeValue(value, false))
                {
                    var name = FormattedPropertyNames[prop.Name];
                    var formattedValue = FormatValue(value);
                    result.Add((name, formattedValue, prop.PropertyType));
                }
            }

            return result;
        }

        /// <summary>
        /// Gets only numeric specifications
        /// </summary>
        public Dictionary<string, double> GetNumericSpecifications()
        {
            var result = new Dictionary<string, double>();

            foreach (var prop in CachedProperties)
            {
                var value = prop.GetValue(this);

                if (value != null && IsNumericType(prop.PropertyType))
                {
                    if (TryConvertToDouble(value, out double doubleValue) && doubleValue != 0)
                    {
                        var key = FormattedPropertyNames[prop.Name];
                        result[key] = doubleValue;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Gets specifications matching a filter predicate
        /// </summary>
        public Dictionary<string, object> GetFilteredSpecifications(Func<PropertyInfo, object, bool> filter)
        {
            var result = new Dictionary<string, object>();

            foreach (var prop in CachedProperties)
            {
                var value = prop.GetValue(this);

                if (value != null && filter(prop, value))
                {
                    var key = FormattedPropertyNames[prop.Name];
                    result[key] = value;
                }
            }

            return result;
        }

        #endregion

        #region Helper Methods

        private static bool ShouldIncludeValue(object value, bool includeEmptyStrings)
        {
            if (value == null) return false;

            // Check for default values
            if (IsDefaultValue(value)) return false;

            // Check for empty strings
            if (!includeEmptyStrings && value is string str && string.IsNullOrWhiteSpace(str))
                return false;

            return true;
        }

        private static bool IsDefaultValue(object value)
        {
            if (value == null) return true;

            var type = value.GetType();

            // Handle nullable types
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return false; // If it has a value, it's not default
            }

            // Handle value types
            if (type.IsValueType)
            {
                var defaultValue = Activator.CreateInstance(type);
                return value.Equals(defaultValue);
            }

            // Handle strings
            if (value is string s)
                return string.IsNullOrWhiteSpace(s);

            return false;
        }

        private static string FormatPropertyName(PropertyInfo prop)
        {
            // Check for JsonProperty attribute first
            var jsonProp = prop.GetCustomAttribute<JsonPropertyAttribute>();
            var name = jsonProp?.PropertyName ?? prop.Name;

            // Handle snake_case
            if (name.Contains('_'))
            {
                return string.Join(" ", name.Split('_')
                    .Select(word => char.ToUpper(word[0]) + word.Substring(1).ToLower()));
            }

            // Handle PascalCase
            var formatted = new StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                if (i > 0 && char.IsUpper(name[i]) &&
                    (i + 1 < name.Length && char.IsLower(name[i + 1]) || char.IsLower(name[i - 1])))
                {
                    formatted.Append(' ');
                }
                formatted.Append(name[i]);
            }

            return formatted.ToString();
        }

        private static string FormatValue(object value)
        {
            if (value == null) return string.Empty;

            // Format specific types
            switch (value)
            {
                case DateTime dt:
                    return dt.ToString("yyyy-MM-dd");
                case decimal dec:
                    return dec.ToString("N2");
                case double dbl:
                    return dbl.ToString("N2");
                case float flt:
                    return flt.ToString("N2");
                case bool b:
                    return b ? "Yes" : "No";
                default:
                    return value.ToString() ?? string.Empty;
            }
        }

        private static string DetermineCategory(string propertyName)
        {
            // Categorize based on property name patterns
            var lowerName = propertyName.ToLower();

            if (lowerName.Contains("voltage") || lowerName.Contains("amp") ||
                lowerName.Contains("watt") || lowerName.Contains("electrical"))
                return "Electrical";

            if (lowerName.Contains("dimension") || lowerName.Contains("length") ||
                lowerName.Contains("width") || lowerName.Contains("height") ||
                lowerName.Contains("diameter") || lowerName.Contains("size"))
                return "Dimensions";

            if (lowerName.Contains("motor") || lowerName.Contains("rpm") ||
                lowerName.Contains("hp") || lowerName.Contains("torque"))
                return "Motor";

            if (lowerName.Contains("capacity") || lowerName.Contains("btuh") ||
                lowerName.Contains("ton") || lowerName.Contains("cfm"))
                return "Performance";

            if (lowerName.Contains("temperature") || lowerName.Contains("pressure"))
                return "Operating Conditions";

            if (lowerName.Contains("material") || lowerName.Contains("type") ||
                lowerName.Contains("configuration"))
                return "Construction";

            if (lowerName.Contains("warranty") || lowerName.Contains("service"))
                return "Service";

            return "General";
        }

        private static bool IsNumericType(Type type)
        {
            return type == typeof(int) || type == typeof(int?) ||
                   type == typeof(long) || type == typeof(long?) ||
                   type == typeof(float) || type == typeof(float?) ||
                   type == typeof(double) || type == typeof(double?) ||
                   type == typeof(decimal) || type == typeof(decimal?) ||
                   type == typeof(short) || type == typeof(short?) ||
                   type == typeof(byte) || type == typeof(byte?);
        }

        private static bool TryConvertToDouble(object value, out double result)
        {
            result = 0;

            try
            {
                if (value == null) return false;

                result = Convert.ToDouble(value);
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Utility Methods for Easy Access

        /// <summary>
        /// Gets a specific specification value by its original property name
        /// </summary>
        public T GetSpecificationValue<T>(string propertyName)
        {
            var prop = CachedProperties.FirstOrDefault(p =>
                string.Equals(p.Name, propertyName, StringComparison.OrdinalIgnoreCase));

            if (prop != null)
            {
                var value = prop.GetValue(this);
                if (value is T typedValue)
                    return typedValue;
            }

            return default(T);
        }

        /// <summary>
        /// Checks if a specification has a non-default value
        /// </summary>
        public bool HasSpecification(string propertyName)
        {
            var prop = CachedProperties.FirstOrDefault(p =>
                string.Equals(p.Name, propertyName, StringComparison.OrdinalIgnoreCase));

            if (prop != null)
            {
                var value = prop.GetValue(this);
                return ShouldIncludeValue(value, false);
            }

            return false;
        }

        /// <summary>
        /// Gets the count of non-null, non-default specifications
        /// </summary>
        public int GetSpecificationCount()
        {
            return ToDictionary().Count;
        }

        #endregion
    }

    #endregion

    #region HTTP Get/Post Methods to Bluon API

    protected async Task<string?> BluonGet(string url, CancellationToken cancellationToken = default, int cacheExpirationMins = 60, int timeoutseconds = 15)
    {
        if (client == null)
            return null;

        // Generate a deterministic hash code for the URL - consider this a key or filename.
        int URLDeterministicHash = SQLHashFunctions.GetDeterministicHashCodeSQL(url);

        // Check cache if available
        if (Cache != null)
        {
            try
            {
                var cachedResult = await Cache.GetIfCachedAsync(URLDeterministicHash);
                if (!string.IsNullOrEmpty(cachedResult))
                {
                    return cachedResult;
                }
            }
            catch (Exception ex)
            {
                // Log cache error but continue with API call
                Console.WriteLine($"Cache retrieval error: {ex.Message}");
            }
        }

        LastError = false; LastErrorMessage = string.Empty;
        string? json = null;
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Authorization", $"Bearer {apikey}");
        //request.Headers.Add("x-api-user-id", apiuser);
        //request.Headers.Add("x-api-company-email", "service@interiorcs.com");
        //request.Headers.Add("x-api-company-name", "InteriorClimateSolutions");
        //request.Headers.Add("x-api-company-zip-code", "75247");
        //request.Headers.Add("x-api-phone", "4699498393");
        try
        {
            // Use per-request cancellation token with timeout if different from default
            CancellationTokenSource? timeoutCts = null;
            CancellationToken effectiveToken = cancellationToken;

            if (timeoutseconds != defaultTimeout)
            {
                timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutseconds));
                effectiveToken = timeoutCts.Token;
            }

            try
            {
                HttpResponseMessage response = await client!.SendAsync(request, effectiveToken);
                if (!response.IsSuccessStatusCode)
                {
                    // handle 500, 404, or 300 errors here
                    Console.WriteLine($"BLUON GET Error: {response.StatusCode} - {response.ReasonPhrase}");
                    LastErrorMessage = $"Bluon HTTP Get Error: {response.StatusCode}: {response.ReasonPhrase}";
                    LastError = true;
                    return null;
                }

                json = await response.Content.ReadAsStringAsync(effectiveToken);

                if (string.IsNullOrWhiteSpace(json))
                    return null;

                // Save to cache if connected
                if (Cache != null && !string.IsNullOrEmpty(json))
                {
                    _ = Cache.SaveCacheCopyAsync(URLDeterministicHash, url, json, cacheExpirationMins);
                }
            }
            finally
            {
                timeoutCts?.Dispose();
            }
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            // Handle timeouts here
            LastErrorMessage = $"Bluon Request timed out for {url}";
            LastError = true;
            return null;
        }
        catch (TaskCanceledException)
        {
            // Handle task CANCELLATION (not timeout!)
            LastErrorMessage = $"Bluon Request was cancelled for {url}";
            LastError = true;
            return null;
        }
        catch (OperationCanceledException)
        {
            // Handle operation cancellation
            LastErrorMessage = $"Bluon Request was cancelled for {url}";
            LastError = true;
            return null;
        }
        catch (HttpRequestException)
        {
            // Handle network errors
            LastErrorMessage = $"Bluon Request failed for {url}";
            LastError = true;
            return null;
        }
        catch (Exception ex)
        {
            // Handle all other exceptions
            LastErrorMessage = $"Bluon Request failed for {url} : {ex.InnerException} \r\n {ex.Message}";
            LastError = true;
            return null;
        }
        finally
        {
            request.Dispose();
        }

        if (!string.IsNullOrEmpty(LastErrorMessage))
        {
            Console.WriteLine($"BluonError: {LastErrorMessage}");
        }
        return json;
    }

    protected async Task<string?> BluonPost(string url, FormUrlEncodedContent content, string? jsonBody = null, CancellationToken cancellationToken = default, bool enableCache = false, int cacheExpirationMins = 60, int timeoutseconds = 15)
    {
        if (client == null)
            return null;

        // Only cache if explicitly enabled
        if (enableCache && Cache != null)
        {
            try
            {
                var cachedResult = await Cache.GetIfCachedAsync(SQLHashFunctions.GetDeterministicHashCodeSQL(url));
                if (!string.IsNullOrEmpty(cachedResult))
                {
                    return cachedResult;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Cache retrieval error: {ex.Message}");
            }
        }

        LastError = false;
        string? json = null;
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("Authorization", $"Bearer {apikey}");
        //request.Headers.Add("x-api-user-id", apiuser);
        //request.Headers.Add("x-api-company-email", "service@interiorcs.com");
        //request.Headers.Add("x-api-company-name", "InteriorClimateSolutions");
        //request.Headers.Add("x-api-company-zip-code", "75247");
        //request.Headers.Add("x-api-phone", "4699498393");

        // Add JSON content
        if (content is not null)
        {
            request.Content = content;
        }
        else
        {
            if (!string.IsNullOrEmpty(jsonBody))
            {
                request.Content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json");
            }
        }

        try
        {
            // Use per-request cancellation token with timeout if different from default
            CancellationTokenSource? timeoutCts = null;
            CancellationToken effectiveToken = cancellationToken;

            if (timeoutseconds != defaultTimeout)
            {
                timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutseconds));
                effectiveToken = timeoutCts.Token;
            }

            try
            {
                HttpResponseMessage response = await client!.SendAsync(request, effectiveToken);
                if (!response.IsSuccessStatusCode)
                {
                    // handle 500, 404, or 300 errors here
                    Console.WriteLine($"BLUON POST Error: {response.StatusCode} - {response.ReasonPhrase}");
                    LastErrorMessage = $"HTTP {response.StatusCode}: {response.ReasonPhrase}";
                    LastError = true;

                    // Try to get error details from response body
                    try
                    {
                        var errorContent = await response.Content.ReadAsStringAsync(effectiveToken);
                        if (!string.IsNullOrWhiteSpace(errorContent))
                        {
                            LastErrorMessage += $" - Details: {errorContent}";
                        }
                    }
                    catch { /* Ignore errors reading error content */ }

                    return null;
                }

                json = await response.Content.ReadAsStringAsync(effectiveToken);

                if (string.IsNullOrWhiteSpace(json))
                    return null;

                // Save to cache if enabled and successful
                if (enableCache && Cache != null && !string.IsNullOrEmpty(json))
                {

                    _ = Cache.SaveCacheCopyAsync(SQLHashFunctions.GetDeterministicHashCodeSQL(url), url, json, cacheExpirationMins);

                    //try
                    //{
                    //    await Cache.SaveCacheCopyAsync(SQLHashFunctions.GetDeterministicHashCodeSQL(url), url, json, cacheExpirationMins);
                    //}
                    //catch (Exception ex)
                    //{
                    //    Console.WriteLine($"Cache save error: {ex.Message}");
                    //}
                }
            }
            finally
            {
                timeoutCts?.Dispose();
            }
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            // Handle generic old timeouts specifically
            LastErrorMessage = $"Bluon POST Request timed out for {url}";
            LastError = true;
            return null;
        }
        catch (TaskCanceledException)
        {
            // Handle task cancellation (not timeout)
            LastErrorMessage = $"Bluon POST Request was cancelled for {url}";
            LastError = true;
            return null;
        }
        catch (OperationCanceledException)
        {
            // operation cancellation
            LastErrorMessage = $"Bluon POST Request was cancelled for {url}";
            LastError = true;
            return null;
        }
        catch (HttpRequestException ex)
        {
            // network errors
            LastErrorMessage = $"Bluon POST Request failed (network error) for {url}: {ex.Message}";
            LastError = true;
            return null;
        }
        catch (Exception ex)
        {
            // all other exceptions i didn't imagine sitting here
            LastErrorMessage = $"Bluon POST Request failed for {url} : {ex.InnerException} \r\n {ex.Message}";
            LastError = true;
            return null;
        }
        finally
        {
            request.Dispose();
        }

        return json;
    }

    public async Task<string?> BluonNameplateReader(Byte[]? imageData, string? strFileName="dataplate.jpg", CancellationToken cancellationToken = default, int timeoutseconds = 45)
    {
        if (client is null || imageData is null || imageData is not null && imageData.Length<100)
            return null;

        string url = $"{apiroot}/extended-nameplate-reader";
        LastError = false;
        // To send the request using the HttpRequestMessage (not just client.PostAsync), use:
        // var response = await client.SendAsync(request);

        string? json = null;
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);

        //request.Headers.Add("x-api-phone", "4699498393");
        request.Headers.Accept.Clear();
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Add("Authorization", $"Bearer {apikey}");
        request.Headers.Add("X-CSRF-TOKEN", "");

        using var form = new MultipartFormDataContent();
        using var fileStream = new MemoryStream(imageData ?? Array.Empty<byte>());
        var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");

        // "image" must match the field name expected by Bluon
        form.Add(fileContent, "image", strFileName ?? "dataplate.jpg");

        // Attach the form to the request
        request.Content = form;
       
        try
        {
            // Use per-request cancellation token with timeout if different from default
            CancellationTokenSource? timeoutCts = null;
            CancellationToken effectiveToken = cancellationToken;

            if (timeoutseconds != defaultTimeout)
            {
                timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutseconds));
                effectiveToken = timeoutCts.Token;
            }

            try
            {
                HttpResponseMessage response = await client!.SendAsync(request, effectiveToken);
                if (!response.IsSuccessStatusCode)
                {
                    // handle 500, 404, or 300 errors here
                    Console.WriteLine($"BLUON POST Error: {response.StatusCode} - {response.ReasonPhrase}");
                    LastErrorMessage = $"HTTP {response.StatusCode}: {response.ReasonPhrase}";
                    LastError = true;

                    // Try to get error details from response body
                    try
                    {
                        var errorContent = await response.Content.ReadAsStringAsync(effectiveToken);
                        if (!string.IsNullOrWhiteSpace(errorContent))
                        {
                            LastErrorMessage += $" - Details: {errorContent}";
                        }
                    }
                    catch { /* Ignore errors reading error content */ }

                    return null;
                }

                json = await response.Content.ReadAsStringAsync(effectiveToken);

                if (string.IsNullOrWhiteSpace(json))
                    return null;

                
            }
            finally
            {
                timeoutCts?.Dispose();
            }
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            // Handle generic old timeouts specifically
            LastErrorMessage = $"Bluon POST Request timed out for {url}";
            LastError = true;
            return null;
        }
        catch (TaskCanceledException)
        {
            // Handle task cancellation (not timeout)
            LastErrorMessage = $"Bluon POST Request was cancelled for {url}";
            LastError = true;
            return null;
        }
        catch (OperationCanceledException)
        {
            // operation cancellation
            LastErrorMessage = $"Bluon POST Request was cancelled for {url}";
            LastError = true;
            return null;
        }
        catch (HttpRequestException ex)
        {
            // network errors
            LastErrorMessage = $"Bluon POST Request failed (network error) for {url}: {ex.Message}";
            LastError = true;
            return null;
        }
        catch (Exception ex)
        {
            // all other exceptions i didn't imagine sitting here
            LastErrorMessage = $"Bluon POST Request failed for {url} : {ex.InnerException} \r\n {ex.Message}";
            LastError = true;
            return null;
        }
        finally
        {
            request.Dispose();
        }

        return json;
    }

    public async Task<bool> BluonNameplateReaderCould(Byte[]? imageData)
    {
        using var client = new HttpClient();

        var url = "https://test.hub.bluontoolbox.com/uat/gateway/interior-climate-solutions/extended-nameplate-reader";
        var token = $"{apikey}";
        
        var imagePath = @"dataplate.jpg";

        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", ""); // if required, otherwise can omit

        using var form = new MultipartFormDataContent();
        //await using var fileStream = File.OpenRead(imagePath);
        //var fileContent = new StreamContent(fileStream);

        using var fileStream = new MemoryStream(imageData ?? Array.Empty<byte>());
        var fileContent = new StreamContent(fileStream);

        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");

        // "image" must match the field name expected by Bluon
        form.Add(fileContent, "image", "dataplate.jpg");

        var response = await client.PostAsync(url, form);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        // deserialize to a C# type if you want:
        Console.WriteLine(json);

        return true;
    }
    #endregion

}

public class BluonBrands : BluonBase
{

    #region Constructor and Global Variables

    public BluonBrands(string apikey, string apiroot, string userIdentifier, IBluonCache? cache = null) : base(cache)
    {
        this.apikey = apikey;
        this.apiroot = apiroot;
        this.apiuser = userIdentifier;
    }

    public BluonBrands(string apikey, string apiroot, string userIdentifier) : base()
    {
        this.apikey = apikey;
        this.apiroot = apiroot;
        this.apiuser = userIdentifier;
    }

    public BluonBrands() : base()
    {
    }

    #endregion

    #region BrandModels
    public class BrandModels
    {
        public class Datum
        {
            [JsonProperty("id")]
            [JsonPropertyName("id")]
            public string Id;

            [JsonProperty("model")]
            [JsonPropertyName("model")]
            public string Model;

            [JsonProperty("model_notes")]
            [JsonPropertyName("model_notes")]
            public string? ModelNotes;

            [JsonProperty("manuals_count")]
            [JsonPropertyName("manuals_count")]
            public int ManualsCount;

            [JsonProperty("logo")]
            [JsonPropertyName("logo")]
            public string? Logo;

            [JsonProperty("image")]
            [JsonPropertyName("image")]
            public string? Image;

            [JsonProperty("system_type")]
            [JsonPropertyName("system_type")]
            public string SystemType;

            [JsonProperty("calling_groups")]
            [JsonPropertyName("calling_groups")]
            public string? CallingGroups;

            [JsonProperty("updated_at")]
            [JsonPropertyName("updated_at")]
            public DateTime? UpdatedAt;

            [JsonProperty("functional_parts_count")]
            [JsonPropertyName("functional_parts_count")]
            public int FunctionalPartsCount;

            [JsonProperty("my_parts_count")]
            [JsonPropertyName("my_parts_count")]
            public int MyPartsCount;

            [JsonProperty("my_parts_replacements_count")]
            [JsonPropertyName("my_parts_replacements_count")]
            public int MyPartsReplacementsCount;

            [JsonProperty("root_model")]
            [JsonPropertyName("root_model")]
            public string? RootModel;

            [JsonProperty("brand_name")]
            [JsonPropertyName("brand_name")]
            public string BrandName;

            [JsonProperty("brand_image")]
            [JsonPropertyName("brand_image")]
            public string? BrandImage;

            [JsonProperty("series")]
            [JsonPropertyName("series")]
            public Series? Series;

            [JsonProperty("confidence_score")]
            [JsonPropertyName("confidence_score")]
            public float? ConfidenceScore;
        }

        public class Root
        {
            [JsonProperty("data")]
            [JsonPropertyName("data")]
            public List<Datum>? Data;

            [JsonProperty("links")]
            [JsonPropertyName("links")]
            public Link? Links;

            [JsonProperty("meta")]
            [JsonPropertyName("meta")]
            public Meta? Meta;
        }

        public class Series
        {
            [JsonProperty("id")]
            [JsonPropertyName("id")]
            public int Id;

            [JsonProperty("name")]
            [JsonPropertyName("name")]
            public string Name;

            [JsonProperty("image")]
            [JsonPropertyName("image")]
            public Image Image;
        }


    }
    #endregion

    #region Brands
    public class Brands
    {

        public class Data
        {
            [JsonProperty("id")]
            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [JsonProperty("name")]
            [JsonPropertyName("name")]
            public string? Brand;

            [JsonProperty("image")]
            [JsonPropertyName("image")]
            public Image? Image;
        }

        public class Root
        {
            [JsonProperty("data")]
            [JsonPropertyName("data")]
            public List<Data>? BrandData;

            [JsonProperty("links")]
            [JsonPropertyName("links")]
            public Link? Links;

            [JsonProperty("meta")]
            [JsonPropertyName("meta")]
            public Meta? Meta;
        }

    }
    #endregion

    #region Series by Brand

    public class BrandSeries
    {

        public class Datum
        {
            [JsonProperty("id")]
            [JsonPropertyName("id")]
            public int Id;

            [JsonProperty("name")]
            [JsonPropertyName("name")]
            public string Series = string.Empty;

            [JsonProperty("image")]
            [JsonPropertyName("image")]
            public Image? Image;
        }


        public class Root
        {
            [JsonProperty("data")]
            [JsonPropertyName("data")]
            public List<Datum>? Data;

            [JsonProperty("links")]
            [JsonPropertyName("links")]
            public Link? Links;

            [JsonProperty("meta")]
            [JsonPropertyName("meta")]
            public Meta? Meta;
        }



    }

    #endregion

    #region Oems by Series by Brand

    public class BrandSeriesOems
    {
        public class Datum
        {
            [JsonProperty("id")]
            [JsonPropertyName("id")]
            public string Id;

            [JsonProperty("model")]
            [JsonPropertyName("model")]
            public string Model;

            [JsonProperty("model_notes")]
            [JsonPropertyName("model_notes")]
            public string? ModelNotes;

            [JsonProperty("manuals_count")]
            [JsonPropertyName("manuals_count")]
            public int ManualsCount;

            [JsonProperty("logo")]
            [JsonPropertyName("logo")]
            public string? Logo;

            [JsonProperty("image")]
            [JsonPropertyName("image")]
            public string? Image;

            [JsonProperty("system_type")]
            [JsonPropertyName("system_type")]
            public string SystemType;

            [JsonProperty("calling_groups")]
            [JsonPropertyName("calling_groups")]
            public string? CallingGroups;

            [JsonProperty("updated_at")]
            [JsonPropertyName("updated_at")]
            public DateTime? UpdatedAt;

            [JsonProperty("functional_parts_count")]
            [JsonPropertyName("functional_parts_count")]
            public int FunctionalPartsCount;

            [JsonProperty("my_parts_count")]
            [JsonPropertyName("my_parts_count")]
            public int MyPartsCount;

            [JsonProperty("my_parts_replacements_count")]
            [JsonPropertyName("my_parts_replacements_count")]
            public int MyPartsReplacementsCount;

            [JsonProperty("root_model")]
            [JsonPropertyName("root_model")]
            public string RootModel;

            [JsonProperty("brand_name")]
            [JsonPropertyName("brand_name")]
            public string BrandName;

            [JsonProperty("brand_image")]
            [JsonPropertyName("brand_image")]
            public string? BrandImage;

            [JsonProperty("confidence_score")]
            [JsonPropertyName("confidence_score")]
            public float? ConfidenceScore;
        }


        public class Root
        {
            [JsonProperty("data")]
            [JsonPropertyName("data")]
            public List<Datum>? Data;

            [JsonProperty("links")]
            [JsonPropertyName("links")]
            public Link? Links;

            [JsonProperty("meta")]
            [JsonPropertyName("meta")]
            public Meta? Meta;
        }

    }


    #endregion

    #region Get Parts by Brand and Oem

    public class BrandPartsAndOem
    {
        public class Datum
        {
            [JsonProperty("id")]
            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [JsonProperty("number")]
            [JsonPropertyName("number")]
            public string? Number { get; set; }

            [JsonProperty("type")]
            [JsonPropertyName("type")]
            public string? Type { get; set; }

            [JsonProperty("subtype")]
            [JsonPropertyName("subtype")]
            public string? Subtype { get; set; }

            [JsonProperty("description")]
            [JsonPropertyName("description")]
            public string? Description { get; set; }

            [JsonProperty("brand")]
            [JsonPropertyName("brand")]
            public string? Brand { get; set; }

            [JsonProperty("image")]
            [JsonPropertyName("image")]
            public Image? Image { get; set; }

            [JsonProperty("subcategory")]
            [JsonPropertyName("subcategory")]
            public string? Subcategory { get; set; }

            [JsonProperty("mfg_type")]
            [JsonPropertyName("mfg_type")]
            public string? MfgType { get; set; }

            [JsonProperty("updated_at")]
            [JsonPropertyName("updated_at")]
            public string? UpdatedAt { get; set; }

            [JsonProperty("specifications")]
            [JsonPropertyName("specifications")]
            public PartSpecifications? Specifications { get; set; }

            [JsonProperty("notes")]
            [JsonPropertyName("notes")]
            public string? Notes { get; set; }

            [JsonProperty("replacements_count")]
            [JsonPropertyName("replacements_count")]
            public int ReplacementsCount { get; set; }

            [JsonProperty("replacements_aftermarket_count")]
            [JsonPropertyName("replacements_aftermarket_count")]
            public int ReplacementsAftermarketCount { get; set; }

            [JsonProperty("my_sku")]
            [JsonPropertyName("my_sku")]
            public string? MySku { get; set; }

            [JsonProperty("my_product_url")]
            [JsonPropertyName("my_product_url")]
            public string? MyProductUrl { get; set; }

            [JsonProperty("my_replacements_count")]
            [JsonPropertyName("my_replacements_count")]
            public int MyReplacementsCount { get; set; }
        }

        public class Root
        {
            [JsonProperty("data")]
            [JsonPropertyName("data")]
            public List<Datum>? Data { get; set; }

            [JsonProperty("links")]
            [JsonPropertyName("links")]
            public Link? Links { get; set; }

            [JsonProperty("meta")]
            [JsonPropertyName("meta")]
            public Meta? Meta { get; set; }
        }




    }


    #endregion

    #region Brands Get Data Functions

    public async Task<Brands.Root?> GetBrands(int page = 1, int perpage = 50, CancellationToken cancellationToken = default, int cacheTimeout = 4320)
    {
        string url = $"{apiroot}/brands?page={page}&per_page={perpage}";
        string? jsonreturned = await BluonGet(url, cancellationToken, cacheTimeout);
        if (string.IsNullOrEmpty(jsonreturned))
            return null;
        return JsonConvert.DeserializeObject<Brands.Root>(jsonreturned);
    }

    public async Task<BrandModels.Root?> GetBrandModels(string brand, int page = 1, int perpage = 50, CancellationToken cancellationToken = default, int cacheTimeout = 4320)
    {
        string url = $"{apiroot}/brands/{brand}/oems?page={page}&per_page={perpage}";
        string? jsonreturned = await BluonGet(url, cancellationToken);
        if (string.IsNullOrEmpty(jsonreturned))
            return null;
        return JsonConvert.DeserializeObject<BrandModels.Root>(jsonreturned);
    }

    public async Task<BrandSeries.Root?> GetBrandSeries(string brand, int page = 1, int perpage = 50, CancellationToken cancellationToken = default, int cacheTimeout = 4320)
    {
        string url = $"{apiroot}/brands/{brand}/series?page={page}&per_page={perpage}";
        string? jsonreturned = await BluonGet(url, cancellationToken, cacheTimeout);
        if (string.IsNullOrEmpty(jsonreturned))
            return null;
        return JsonConvert.DeserializeObject<BrandSeries.Root>(jsonreturned);
    }

    public async Task<BrandSeriesOems.Root?> GetBrandSeriesOems(string brand, int seriesId, int page = 1, int perpage = 50, CancellationToken cancellationToken = default, int cacheTimeout = 4320)
    {
        string url = $"{apiroot}/brands/{brand}/series/{seriesId.ToString()}/oems?page={page}&per_page={perpage}";
        string? jsonreturned = await BluonGet(url, cancellationToken, cacheTimeout);
        if (string.IsNullOrEmpty(jsonreturned))
            return null;
        return JsonConvert.DeserializeObject<BrandSeriesOems.Root>(jsonreturned);
    }

    public async Task<BrandPartsAndOem.Root?> GetBrandOemParts(string brand, string oemId, int page = 1, int perpage = 50, CancellationToken cancellationToken = default, int cacheTimeout = 4320)
    {
        string url = $"{apiroot}/brands/{brand}/oems/{oemId}/parts?page={page}&per_page={perpage}";
        string? jsonreturned = await BluonGet(url, cancellationToken, cacheTimeout);
        if (string.IsNullOrEmpty(jsonreturned))
            return null;
        return JsonConvert.DeserializeObject<BrandPartsAndOem.Root>(jsonreturned);
    }

    #endregion

    #region Get All Methods - Sequential

    public async Task<List<Brands.Data>> GetAllBrands(int perpage = 50, CancellationToken cancellationToken = default)
    {
        var allBrands = new List<Brands.Data>();
        int currentPage = 1;
        bool hasMorePages = true;

        while (hasMorePages && !cancellationToken.IsCancellationRequested)
        {
            var response = await GetBrands(currentPage, perpage, cancellationToken);

            if (response?.BrandData != null && response.BrandData.Any())
            {
                allBrands.AddRange(response.BrandData);
                hasMorePages = response.Meta != null && currentPage < response.Meta.LastPage;
                currentPage++;
            }
            else
            {
                hasMorePages = false;
            }
        }

        return allBrands;
    }

    public async Task<List<BrandModels.Datum>> GetAllBrandModels(string brand, int perpage = 50, CancellationToken cancellationToken = default)
    {
        var allModels = new List<BrandModels.Datum>();
        int currentPage = 1;
        bool hasMorePages = true;

        while (hasMorePages && !cancellationToken.IsCancellationRequested)
        {
            var response = await GetBrandModels(brand, currentPage, perpage, cancellationToken);

            if (response?.Data != null && response.Data.Any())
            {
                allModels.AddRange(response.Data);
                hasMorePages = response.Meta != null && currentPage < response.Meta.LastPage;
                currentPage++;
            }
            else
            {
                hasMorePages = false;
            }
        }

        return allModels;
    }

    public async Task<List<BrandSeries.Datum>> GetAllBrandSeries(string brand, int perpage = 50, CancellationToken cancellationToken = default)
    {
        var allSeries = new List<BrandSeries.Datum>();
        int currentPage = 1;
        bool hasMorePages = true;

        while (hasMorePages && !cancellationToken.IsCancellationRequested)
        {
            var response = await GetBrandSeries(brand, currentPage, perpage, cancellationToken);

            if (response?.Data != null && response.Data.Any())
            {
                allSeries.AddRange(response.Data);
                hasMorePages = response.Meta != null && currentPage < response.Meta.LastPage;
                currentPage++;
            }
            else
            {
                hasMorePages = false;
            }
        }

        return allSeries;
    }

    public async Task<List<BrandSeriesOems.Datum>> GetAllBrandSeriesOems(string brand, int seriesId, int perpage = 50, CancellationToken cancellationToken = default)
    {
        var allOems = new List<BrandSeriesOems.Datum>();
        int currentPage = 1;
        bool hasMorePages = true;

        while (hasMorePages && !cancellationToken.IsCancellationRequested)
        {
            var response = await GetBrandSeriesOems(brand, seriesId, currentPage, perpage, cancellationToken);

            if (response?.Data != null && response.Data.Any())
            {
                allOems.AddRange(response.Data);
                hasMorePages = response.Meta != null && currentPage < response.Meta.LastPage;
                currentPage++;
            }
            else
            {
                hasMorePages = false;
            }
        }

        return allOems;
    }

    public async Task<List<BrandPartsAndOem.Datum>> GetAllBrandOemParts(string brand, string oem, int perpage = 50, CancellationToken cancellationToken = default)
    {
        var allParts = new List<BrandPartsAndOem.Datum>();
        int currentPage = 1;
        bool hasMorePages = true;

        while (hasMorePages && !cancellationToken.IsCancellationRequested)
        {
            var response = await GetBrandOemParts(brand, oem, currentPage, perpage, cancellationToken);

            if (response?.Data != null && response.Data.Any())
            {
                allParts.AddRange(response.Data);
                hasMorePages = response.Meta != null && currentPage < response.Meta.LastPage;
                currentPage++;
            }
            else
            {
                hasMorePages = false;
            }
        }

        return allParts;
    }

    #endregion

    #region Get All Methods - Parallel

    public async Task<List<Brands.Data>> GetAllBrandsParallel(int perpage = 50, CancellationToken cancellationToken = default)
    {
        var firstPage = await GetBrands(1, perpage, cancellationToken);
        if (firstPage?.BrandData == null || firstPage.Meta == null)
            return new List<Brands.Data>();

        var allBrands = new List<Brands.Data>();
        allBrands.AddRange(firstPage.BrandData);

        if (firstPage.Meta.LastPage <= 1)
            return allBrands;

        var tasks = new List<Task<Brands.Root?>>();
        for (int page = 2; page <= firstPage.Meta.LastPage; page++)
        {
            tasks.Add(GetBrands(page, perpage, cancellationToken));
            // await Task.Delay(100); // How long do i have to throttle down
        }

        var results = await Task.WhenAll(tasks);

        foreach (var result in results)
        {
            if (result?.BrandData != null)
            {
                allBrands.AddRange(result.BrandData);
            }
        }

        return allBrands;
    }

    public async Task<List<BrandModels.Datum>> GetAllBrandModelsParallel(string brand, int perpage = 50, CancellationToken cancellationToken = default)
    {
        var firstPage = await GetBrandModels(brand, 1, perpage, cancellationToken);
        if (firstPage?.Data == null || firstPage.Meta == null)
            return new List<BrandModels.Datum>();

        var allModels = new List<BrandModels.Datum>();
        allModels.AddRange(firstPage.Data);

        if (firstPage.Meta.LastPage <= 1)
            return allModels;

        var tasks = new List<Task<BrandModels.Root?>>();
        for (int page = 2; page <= firstPage.Meta.LastPage; page++)
        {
            tasks.Add(GetBrandModels(brand, page, perpage, cancellationToken));
        }

        var results = await Task.WhenAll(tasks);

        foreach (var result in results)
        {
            if (result?.Data != null)
            {
                allModels.AddRange(result.Data);
            }
        }

        return allModels;
    }

    public async Task<List<BrandSeries.Datum>> GetAllBrandSeriesParallel(string brand, int perpage = 50, CancellationToken cancellationToken = default)
    {
        var firstPage = await GetBrandSeries(brand, 1, perpage, cancellationToken);
        if (firstPage?.Data == null || firstPage.Meta == null)
            return new List<BrandSeries.Datum>();

        var allSeries = new List<BrandSeries.Datum>();
        allSeries.AddRange(firstPage.Data);

        if (firstPage.Meta.LastPage <= 1)
            return allSeries;

        var tasks = new List<Task<BrandSeries.Root?>>();
        for (int page = 2; page <= firstPage.Meta.LastPage; page++)
        {
            tasks.Add(GetBrandSeries(brand, page, perpage, cancellationToken));
            // add delay?
        }

        var results = await Task.WhenAll(tasks);

        foreach (var result in results)
        {
            if (result?.Data != null)
            {
                allSeries.AddRange(result.Data);
            }
        }

        return allSeries;
    }

    public async Task<List<BrandSeriesOems.Datum>> GetAllBrandSeriesOemsParallel(string brand, int seriesId, int perpage = 50, CancellationToken cancellationToken = default)
    {
        var firstPage = await GetBrandSeriesOems(brand, seriesId, 1, perpage, cancellationToken);
        if (firstPage?.Data == null || firstPage.Meta == null)
            return new List<BrandSeriesOems.Datum>();

        var allOems = new List<BrandSeriesOems.Datum>();
        allOems.AddRange(firstPage.Data);

        if (firstPage.Meta.LastPage <= 1)
            return allOems;

        var tasks = new List<Task<BrandSeriesOems.Root?>>();
        for (int page = 2; page <= firstPage.Meta.LastPage; page++)
        {
            tasks.Add(GetBrandSeriesOems(brand, seriesId, page, perpage, cancellationToken));
        }

        var results = await Task.WhenAll(tasks);

        foreach (var result in results)
        {
            if (result?.Data != null)
            {
                allOems.AddRange(result.Data);
            }
        }

        return allOems;
    }

    public async Task<List<BrandPartsAndOem.Datum>> GetAllBrandOemPartsParallel(string brand, string oem, int perpage = 50, CancellationToken cancellationToken = default)
    {
        var firstPage = await GetBrandOemParts(brand, oem, 1, perpage, cancellationToken);
        if (firstPage?.Data == null || firstPage.Meta == null)
            return new List<BrandPartsAndOem.Datum>();

        var allParts = new List<BrandPartsAndOem.Datum>();
        allParts.AddRange(firstPage.Data);

        if (firstPage.Meta.LastPage <= 1)
            return allParts;

        var tasks = new List<Task<BrandPartsAndOem.Root?>>();
        for (int page = 2; page <= firstPage.Meta.LastPage; page++)
        {
            tasks.Add(GetBrandOemParts(brand, oem, page, perpage, cancellationToken));
        }

        var results = await Task.WhenAll(tasks);

        foreach (var result in results)
        {
            if (result?.Data != null)
            {
                allParts.AddRange(result.Data);
            }
        }

        return allParts;
    }

    #endregion

}

public class BluonParts : BluonBase
{

    #region Constructor and Global Variables

    public BluonParts(string apikey, string apiroot, string userIdentifier, IBluonCache? cache = null) : base(cache)
    {
        this.apikey = apikey;
        this.apiroot = apiroot;
        this.apiuser = userIdentifier;
    }

    public BluonParts(string apikey, string apiroot, string userIdentifier) : base()
    {
        this.apikey = apikey;
        this.apiroot = apiroot;
        this.apiuser = userIdentifier;
    }

    public BluonParts() : base()
    {
    }

    ~BluonParts()
    {
    }

    #endregion

    #region Search for Parts

    public class PartsSearch
    {
        public class Datum
        {
            [JsonProperty("id")]
            [JsonPropertyName("id")]
            public string Id { get; set; }

            [JsonProperty("number")]
            [JsonPropertyName("number")]
            public string? Number { get; set; }

            [JsonProperty("type")]
            [JsonPropertyName("type")]
            public string? Type { get; set; }

            [JsonProperty("subtype")]
            [JsonPropertyName("subtype")]
            public string? Subtype { get; set; }

            [JsonProperty("description")]
            [JsonPropertyName("description")]
            public string? Description { get; set; }

            [JsonProperty("brand")]
            [JsonPropertyName("brand")]
            public string? Brand { get; set; }

            [JsonProperty("image")]
            [JsonPropertyName("image")]
            public Image? Image { get; set; }

            [JsonProperty("subcategory")]
            [JsonPropertyName("subcategory")]
            public string? Subcategory { get; set; }

            [JsonProperty("mfg_type")]
            [JsonPropertyName("mfg_type")]
            public string? MfgType { get; set; }

            [JsonProperty("updated_at")]
            [JsonPropertyName("updated_at")]
            public string? UpdatedAt { get; set; }

            [JsonProperty("specifications")]
            [JsonPropertyName("specifications")]
            public PartSpecifications? Specifications { get; set; }

            [JsonProperty("replacements_count")]
            [JsonPropertyName("replacements_count")]
            public int ReplacementsCount { get; set; }

            [JsonProperty("replacements_aftermarket_count")]
            [JsonPropertyName("replacements_aftermarket_count")]
            public int ReplacementsAftermarketCount { get; set; }

            [JsonProperty("my_sku")]
            [JsonPropertyName("my_sku")]
            public string? MySku { get; set; }

            [JsonProperty("my_product_url")]
            [JsonPropertyName("my_product_url")]
            public string? MyProductUrl { get; set; }

            [JsonProperty("my_replacements_count")]
            [JsonPropertyName("my_replacements_count")]
            public int MyReplacementsCount { get; set; }
        }

        public class Root
        {
            [JsonProperty("data")]
            [JsonPropertyName("data")]
            public List<Datum>? Data { get; set; }

            [JsonProperty("links")]
            [JsonPropertyName("links")]
            public Links? Links { get; set; }

            [JsonProperty("meta")]
            [JsonPropertyName("meta")]
            public Meta? Meta { get; set; }
        }

    }

    #endregion

    #region Get Part Info

    public class PartInfo
    {

        public class Data
        {
            [JsonProperty("id")]
            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [JsonProperty("number")]
            [JsonPropertyName("number")]
            public string? Number { get; set; }

            [JsonProperty("type")]
            [JsonPropertyName("type")]
            public string? Type { get; set; }

            [JsonProperty("subtype")]
            [JsonPropertyName("subtype")]
            public string? Subtype { get; set; }

            [JsonProperty("description")]
            [JsonPropertyName("description")]
            public string? Description { get; set; }

            [JsonProperty("brand")]
            [JsonPropertyName("brand")]
            public string? Brand { get; set; }

            [JsonProperty("image")]
            [JsonPropertyName("image")]
            public Image? Image { get; set; }

            [JsonProperty("subcategory")]
            [JsonPropertyName("subcategory")]
            public string? Subcategory { get; set; }

            [JsonProperty("mfg_type")]
            [JsonPropertyName("mfg_type")]
            public string? MfgType { get; set; }

            [JsonProperty("updated_at")]
            [JsonPropertyName("updated_at")]
            public string? UpdatedAt { get; set; }

            [JsonProperty("my_sku")]
            [JsonPropertyName("my_sku")]
            public string? MySku { get; set; }

            [JsonProperty("my_product_url")]
            [JsonPropertyName("my_product_url")]
            public string? MyProductUrl { get; set; }

            [JsonProperty("my_replacements_count")]
            [JsonPropertyName("my_replacements_count")]
            public int MyReplacementsCount { get; set; }

            [JsonProperty("replacements_count")]
            [JsonPropertyName("replacements_count")]
            public int ReplacementsCount { get; set; }

            [JsonProperty("replacements_aftermarket_count")]
            [JsonPropertyName("replacements_aftermarket_count")]
            public int ReplacementsAftermarketCount { get; set; }

            [JsonProperty("specifications")]
            [JsonPropertyName("specifications")]
            public PartSpecifications? Specifications { get; set; }

            [JsonProperty("notes")]
            [JsonPropertyName("notes")]
            public string? Notes { get; set; }
        }



        public class Root
        {
            [JsonProperty("data")]
            [JsonPropertyName("data")]
            public Data Data { get; set; }
        }





    }

    #endregion

    #region Get Part Alternatives

    public class PartAlternatives
    {
        public class Datum
        {
            [JsonProperty("id")]
            [JsonPropertyName("id")]
            public string? Id { get; set; }

            [JsonProperty("type")]
            [JsonPropertyName("type")]
            public string? Type { get; set; }

            [JsonProperty("note")]
            [JsonPropertyName("note")]
            public string? Note { get; set; }

            [JsonProperty("score")]
            [JsonPropertyName("score")]
            public int? Score { get; set; }

            [JsonProperty("score_detail")]
            [JsonPropertyName("score_detail")]
            public string? ScoreDetail { get; set; }

            [JsonProperty("details")]
            [JsonPropertyName("details")]
            public Details? Details { get; set; }
        }

        public class Details
        {
            [JsonProperty("id")]
            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [JsonProperty("number")]
            [JsonPropertyName("number")]
            public string? Number { get; set; }

            [JsonProperty("type")]
            [JsonPropertyName("type")]
            public string? Type { get; set; }

            [JsonProperty("subtype")]
            [JsonPropertyName("subtype")]
            public string? Subtype { get; set; }

            [JsonProperty("description")]
            [JsonPropertyName("description")]
            public string? Description { get; set; }

            [JsonProperty("brand")]
            [JsonPropertyName("brand")]
            public string? Brand { get; set; }

            [JsonProperty("image")]
            [JsonPropertyName("image")]
            public Image? Image { get; set; }

            [JsonProperty("subcategory")]
            [JsonPropertyName("subcategory")]
            public string? Subcategory { get; set; }

            [JsonProperty("mfg_type")]
            [JsonPropertyName("mfg_type")]
            public string? MfgType { get; set; }

            [JsonProperty("updated_at")]
            [JsonPropertyName("updated_at")]
            public string? UpdatedAt { get; set; }

            [JsonProperty("specifications")]
            [JsonPropertyName("specifications")]
            public PartSpecifications? Specifications { get; set; }

            [JsonProperty("replacements_count")]
            [JsonPropertyName("replacements_count")]
            public int? ReplacementsCount { get; set; }

            [JsonProperty("replacements_aftermarket_count")]
            [JsonPropertyName("replacements_aftermarket_count")]
            public int? ReplacementsAftermarketCount { get; set; }

            [JsonProperty("my_sku")]
            [JsonPropertyName("my_sku")]
            public string? MySku { get; set; }

            [JsonProperty("my_product_url")]
            [JsonPropertyName("my_product_url")]
            public string? MyProductUrl { get; set; }

            [JsonProperty("my_replacements_count")]
            [JsonPropertyName("my_replacements_count")]
            public int? MyReplacementsCount { get; set; }
        }

        public class Root
        {
            [JsonProperty("data")]
            [JsonPropertyName("data")]
            public List<Datum> Data { get; set; }

            [JsonProperty("links")]
            [JsonPropertyName("links")]
            public Link Links { get; set; }

            [JsonProperty("meta")]
            [JsonPropertyName("meta")]
            public Meta Meta { get; set; }
        }

    }

    #endregion

    #region Retrieve Bluon Parts Search Function
    public async Task<BluonParts.PartsSearch.Root?> SearchByPart(string searchString, int page = 1, int perpage = 50, CancellationToken cancellationToken = default, int cacheTimeout = 4320)
    {
        string url = $"{apiroot}/parts?search_string={searchString}&page={page}&per_page={perpage}";
        string? jsonreturned = await BluonGet(url, cancellationToken, cacheTimeout);
        if (string.IsNullOrEmpty(jsonreturned))
            return null;
        return JsonConvert.DeserializeObject<PartsSearch.Root>(jsonreturned);
    }
    #endregion

    #region Retrieve Parts Methods 

    public async Task<PartInfo.Root?> GetPartInfo(string partId, CancellationToken cancellationToken = default, int cacheTimeout = 4320)
    {
        string url = $"{apiroot}/parts/{partId}";
        string? jsonreturned = await BluonGet(url, cancellationToken, cacheTimeout);
        if (string.IsNullOrEmpty(jsonreturned))
            return null;
        return JsonConvert.DeserializeObject<PartInfo.Root>(jsonreturned);
    }

    public async Task<PartAlternatives.Root?> GetPartAlternatives(string partId, int page = 1, int perpage = 50, CancellationToken cancellationToken = default, int cacheTimeout = 4320)
    {
        string url = $"{apiroot}/parts/{partId}/replacements?page={page}&per_page={perpage}";
        string? jsonreturned = await BluonGet(url, cancellationToken, cacheTimeout);
        if (string.IsNullOrEmpty(jsonreturned))
            return null;
        return JsonConvert.DeserializeObject<PartAlternatives.Root>(jsonreturned);
    }

    #endregion

    #region Get All Methods - Sequential

    public async Task<List<PartsSearch.Datum>> GetAllPartsSearch(string searchString, int perpage = 50, CancellationToken cancellationToken = default)
    {
        var allAlternatives = new List<PartsSearch.Datum>();
        int currentPage = 1;
        bool hasMorePages = true;

        while (hasMorePages && !cancellationToken.IsCancellationRequested)
        {
            var response = await SearchByPart(searchString, currentPage, perpage, cancellationToken);

            if (response?.Data != null && response.Data.Any())
            {
                allAlternatives.AddRange(response.Data);
                hasMorePages = response.Meta != null && currentPage < response.Meta.LastPage;
                currentPage++;
            }
            else
            {
                hasMorePages = false;
            }
        }

        return allAlternatives;
    }


    public async Task<List<PartAlternatives.Datum>> GetAllPartAlternatives(string partId, int perpage = 50, CancellationToken cancellationToken = default)
    {
        var allAlternatives = new List<PartAlternatives.Datum>();
        int currentPage = 1;
        bool hasMorePages = true;

        while (hasMorePages && !cancellationToken.IsCancellationRequested)
        {
            var response = await GetPartAlternatives(partId, currentPage, perpage, cancellationToken);

            if (response?.Data != null && response.Data.Any())
            {
                allAlternatives.AddRange(response.Data);
                hasMorePages = response.Meta != null && currentPage < response.Meta.LastPage;
                currentPage++;
            }
            else
            {
                hasMorePages = false;
            }
        }

        return allAlternatives;
    }

    #endregion

    #region Get All Methods - Parallel

    public async Task<List<PartAlternatives.Datum>> GetAllPartAlternativesParallel(string partId, int perpage = 50, CancellationToken cancellationToken = default)
    {
        var firstPage = await GetPartAlternatives(partId, 1, perpage, cancellationToken);
        if (firstPage?.Data == null || firstPage.Meta == null)
            return new List<PartAlternatives.Datum>();

        var allAlternatives = new List<PartAlternatives.Datum>();
        allAlternatives.AddRange(firstPage.Data);

        if (firstPage.Meta.LastPage <= 1)
            return allAlternatives;

        var tasks = new List<Task<PartAlternatives.Root?>>();
        for (int page = 2; page <= firstPage.Meta.LastPage; page++)
        {
            tasks.Add(GetPartAlternatives(partId, page, perpage, cancellationToken));
        }

        var results = await Task.WhenAll(tasks);

        foreach (var result in results)
        {
            if (result?.Data != null)
            {
                allAlternatives.AddRange(result.Data);
            }
        }

        return allAlternatives;
    }

    #endregion

}

public class BluonModels : BluonBase
{

    #region Constructor and Global Variables

    public BluonModels(string apikey, string apiroot, string userIdentifier, IBluonCache? cache = null) : base(cache)
    {
        this.apikey = apikey;
        this.apiroot = apiroot;
        this.apiuser = userIdentifier;
    }

    public BluonModels(string apikey, string apiroot, string userIdentifier) : base()
    {
        this.apikey = apikey;
        this.apiroot = apiroot;
        this.apiuser = userIdentifier;
    }

    public BluonModels() : base() { }
    ~BluonModels() { }

    #endregion

    #region Search for Models

    public class ModelSearch
    {
        public class ModelData
        {
            [JsonProperty("id")]
            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [JsonProperty("model")]
            [JsonPropertyName("model")]
            public string Model { get; set; }

            [JsonProperty("model_notes")]
            [JsonPropertyName("model_notes")]
            public string ModelNotes { get; set; }

            [JsonProperty("manuals_count")]
            [JsonPropertyName("manuals_count")]
            public int? ManualsCount { get; set; }

            [JsonProperty("logo")]
            [JsonPropertyName("logo")]
            public string Logo { get; set; }

            [JsonProperty("image")]
            [JsonPropertyName("image")]
            public string Image { get; set; }

            [JsonProperty("system_type")]
            [JsonPropertyName("system_type")]
            public string SystemType { get; set; }

            [JsonProperty("calling_groups")]
            [JsonPropertyName("calling_groups")]
            public string CallingGroups { get; set; }

            [JsonProperty("updated_at")]
            [JsonPropertyName("updated_at")]
            public DateTime? UpdatedAt { get; set; }

            [JsonProperty("functional_parts_count")]
            [JsonPropertyName("functional_parts_count")]
            public int? FunctionalPartsCount { get; set; }

            [JsonProperty("my_parts_count")]
            [JsonPropertyName("my_parts_count")]
            public int? MyPartsCount { get; set; }

            [JsonProperty("my_parts_replacements_count")]
            [JsonPropertyName("my_parts_replacements_count")]
            public int? MyPartsReplacementsCount { get; set; }

            [JsonProperty("root_model")]
            [JsonPropertyName("root_model")]
            public string RootModel { get; set; }

            [JsonProperty("brand_name")]
            [JsonPropertyName("brand_name")]
            public string BrandName { get; set; }

            [JsonProperty("brand_image")]
            [JsonPropertyName("brand_image")]
            public string BrandImage { get; set; }

            [JsonProperty("series")]
            [JsonPropertyName("series")]
            public Series Series { get; set; }

            [JsonProperty("confidence_score")]
            [JsonPropertyName("confidence_score")]
            public double? ConfidenceScore { get; set; }
        }

        public class Root
        {
            [JsonProperty("data")]
            [JsonPropertyName("data")]
            public List<ModelData>? Data { get; set; }

            [JsonProperty("links")]
            [JsonPropertyName("links")]
            public Link? Links { get; set; }

            [JsonProperty("meta")]
            [JsonPropertyName("meta")]
            public Meta? Meta { get; set; }
        }

        public class Series
        {
            [JsonProperty("id")]
            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [JsonProperty("name")]
            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonProperty("image")]
            [JsonPropertyName("image")]
            public Image? Image { get; set; }
        }


    }

    #endregion

    #region Get Parts for Model
    public class ModelParts
    {
        public class Root
        {
            [JsonProperty("data")]
            [JsonPropertyName("data")]
            public List<Datum>? Data { get; set; }

            [JsonProperty("links")]
            [JsonPropertyName("links")]
            public Link? Links { get; set; }

            [JsonProperty("meta")]
            [JsonPropertyName("meta")]
            public Meta? Meta { get; set; }
        }

        public class Datum
        {
            [JsonProperty("id")]
            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [JsonProperty("number")]
            [JsonPropertyName("number")]
            public string? Number { get; set; }

            [JsonProperty("type")]
            [JsonPropertyName("type")]
            public string? Type { get; set; }

            [JsonProperty("subtype")]
            [JsonPropertyName("subtype")]
            public string? Subtype { get; set; }

            [JsonProperty("description")]
            [JsonPropertyName("description")]
            public string? Description { get; set; }

            [JsonProperty("brand")]
            [JsonPropertyName("brand")]
            public string? Brand { get; set; }

            [JsonProperty("image")]
            [JsonPropertyName("image")]
            public Image? Image { get; set; }

            [JsonProperty("subcategory")]
            [JsonPropertyName("subcategory")]
            public string? Subcategory { get; set; }

            [JsonProperty("mfg_type")]
            [JsonPropertyName("mfg_type")]
            public string? MfgType { get; set; }

            [JsonProperty("updated_at")]
            [JsonPropertyName("updated_at")]
            public string? UpdatedAt { get; set; }

            [JsonProperty("specifications")]
            [JsonPropertyName("specifications")]
            public PartSpecifications? Specifications { get; set; }

            [JsonProperty("notes")]
            [JsonPropertyName("notes")]
            public string? Notes { get; set; }

            [JsonProperty("replacements_count")]
            [JsonPropertyName("replacements_count")]
            public int? ReplacementsCount { get; set; }

            [JsonProperty("replacements_aftermarket_count")]
            [JsonPropertyName("replacements_aftermarket_count")]
            public int? ReplacementsAftermarketCount { get; set; }

            [JsonProperty("my_sku")]
            [JsonPropertyName("my_sku")]
            public string? MySku { get; set; }

            [JsonProperty("my_product_url")]
            [JsonPropertyName("my_product_url")]
            public string? MyProductUrl { get; set; }

            [JsonProperty("my_replacements_count")]
            [JsonPropertyName("my_replacements_count")]
            public int? MyReplacementsCount { get; set; }
        }



    }
    #endregion

    #region Get Manuals for Model

    public class ModelManuals
    {
        public class Root
        {
            [JsonProperty("data")]
            [JsonPropertyName("data")]
            public ModelData? Data { get; set; }
        }
        public class BluonGuideline
        {
            [JsonProperty("id")]
            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [JsonProperty("url")]
            [JsonPropertyName("url")]
            public string? Url { get; set; }

            [JsonProperty("conversions")]
            [JsonPropertyName("conversions")]
            public List<Conversions>? Conversions { get; set; }
        }

        public class ControlsManual
        {
            [JsonProperty("id")]
            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [JsonProperty("url")]
            [JsonPropertyName("url")]
            public string? Url { get; set; }

            [JsonProperty("conversions")]
            [JsonPropertyName("conversions")]
            public List<Conversions>? Conversions { get; set; }
        }

        public class ManualSets
        {
            [JsonProperty("service_facts")]
            [JsonPropertyName("service_facts")]
            public List<ServiceFact> ServiceFacts { get; set; }

            [JsonProperty("product_data")]
            [JsonPropertyName("product_data")]
            public List<ProductDatum> ProductData { get; set; }

            [JsonProperty("iom")]
            [JsonPropertyName("iom")]
            public List<Iom> Iom { get; set; }

            [JsonProperty("wiring_diagram")]
            [JsonPropertyName("wiring_diagram")]
            public List<WiringDiagram> WiringDiagram { get; set; }

            [JsonProperty("nomenclature")]
            [JsonPropertyName("nomenclature")]
            public List<Nomenclature> Nomenclature { get; set; }

            [JsonProperty("parts_manuals")]
            [JsonPropertyName("parts_manuals")]
            public List<PartsManual> PartsManuals { get; set; }

            [JsonProperty("metering_device_manuals")]
            [JsonPropertyName("metering_device_manuals")]
            public List<MeteringDeviceManual> MeteringDeviceManuals { get; set; }

            [JsonProperty("options_accessories_manuals")]
            [JsonPropertyName("options_accessories_manuals")]
            public List<OptionsAccessoriesManual> OptionsAccessoriesManuals { get; set; }

            [JsonProperty("product_brochures")]
            [JsonPropertyName("product_brochures")]
            public List<ProductBrochure> ProductBrochures { get; set; }

            [JsonProperty("schematic_drawings")]
            [JsonPropertyName("schematic_drawings")]
            public List<SchematicDrawing> SchematicDrawings { get; set; }

            [JsonProperty("warranty")]
            [JsonPropertyName("warranty")]
            public List<Warranty> Warranty { get; set; }

            [JsonProperty("bluon_guidelines")]
            [JsonPropertyName("bluon_guidelines")]
            public List<BluonGuideline> BluonGuidelines { get; set; }

            [JsonProperty("diagnostic")]
            [JsonPropertyName("diagnostic")]
            public List<Diagnostic> Diagnostic { get; set; }

            [JsonProperty("misc")]
            [JsonPropertyName("misc")]
            public List<Misc> Misc { get; set; }

            [JsonProperty("controls_manuals")]
            [JsonPropertyName("controls_manuals")]
            public List<ControlsManual> ControlsManuals { get; set; }
        }

        public class ModelData
        {
            [JsonProperty("id")]
            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [JsonProperty("model")]
            [JsonPropertyName("model")]
            public string Model { get; set; }

            [JsonProperty("model_notes")]
            [JsonPropertyName("model_notes")]
            public string ModelNotes { get; set; }

            [JsonProperty("manuals_count")]
            [JsonPropertyName("manuals_count")]
            public int? ManualsCount { get; set; }

            [JsonProperty("logo")]
            [JsonPropertyName("logo")]
            public string Logo { get; set; }

            [JsonProperty("image")]
            [JsonPropertyName("image")]
            public string Image { get; set; }

            [JsonProperty("system_type")]
            [JsonPropertyName("system_type")]
            public string SystemType { get; set; }

            [JsonProperty("calling_groups")]
            [JsonPropertyName("calling_groups")]
            public string CallingGroups { get; set; }

            [JsonProperty("updated_at")]
            [JsonPropertyName("updated_at")]
            public DateTime? UpdatedAt { get; set; }

            [JsonProperty("functional_parts_count")]
            [JsonPropertyName("functional_parts_count")]
            public int? FunctionalPartsCount { get; set; }

            [JsonProperty("my_parts_count")]
            [JsonPropertyName("my_parts_count")]
            public int? MyPartsCount { get; set; }

            [JsonProperty("my_parts_replacements_count")]
            [JsonPropertyName("my_parts_replacements_count")]
            public int? MyPartsReplacementsCount { get; set; }

            [JsonProperty("root_model")]
            [JsonPropertyName("root_model")]
            public string RootModel { get; set; }

            [JsonProperty("brand_name")]
            [JsonPropertyName("brand_name")]
            public string BrandName { get; set; }

            [JsonProperty("brand_image")]
            [JsonPropertyName("brand_image")]
            public string BrandImage { get; set; }

            [JsonProperty("series")]
            [JsonPropertyName("series")]
            public Series Series { get; set; }

            [JsonProperty("links")]
            [JsonPropertyName("links")]
            public Links Links { get; set; }

            [JsonProperty("manuals")]
            [JsonPropertyName("manuals")]
            public ManualSets Manuals { get; set; }

            [JsonProperty("belt_size")]
            [JsonPropertyName("belt_size")]
            public object BeltSize { get; set; }

            [JsonProperty("cfm_range")]
            [JsonPropertyName("cfm_range")]
            public object CfmRange { get; set; }

            [JsonProperty("compressor_sizes")]
            [JsonPropertyName("compressor_sizes")]
            public object CompressorSizes { get; set; }

            [JsonProperty("compressor_type")]
            [JsonPropertyName("compressor_type")]
            public string CompressorType { get; set; }

            [JsonProperty("cooling_btuh")]
            [JsonPropertyName("cooling_btuh")]
            public string CoolingBtuh { get; set; }

            [JsonProperty("filter_size")]
            [JsonPropertyName("filter_size")]
            public string FilterSize { get; set; }

            [JsonProperty("fuel_type")]
            [JsonPropertyName("fuel_type")]
            public object FuelType { get; set; }

            [JsonProperty("heating_btuh")]
            [JsonPropertyName("heating_btuh")]
            public object HeatingBtuh { get; set; }

            [JsonProperty("original_charge_oz")]
            [JsonPropertyName("original_charge_oz")]
            public string OriginalChargeOz { get; set; }

            [JsonProperty("refrigerant")]
            [JsonPropertyName("refrigerant")]
            public string Refrigerant { get; set; }

            [JsonProperty("seer")]
            [JsonPropertyName("seer")]
            public string Seer { get; set; }

            [JsonProperty("seer2")]
            [JsonPropertyName("seer2")]
            public object Seer2 { get; set; }

            [JsonProperty("total_circuits")]
            [JsonPropertyName("total_circuits")]
            public object TotalCircuits { get; set; }

            [JsonProperty("total_compressors")]
            [JsonPropertyName("total_compressors")]
            public int? TotalCompressors { get; set; }

            [JsonProperty("tonnage")]
            [JsonPropertyName("tonnage")]
            public string Tonnage { get; set; }

            [JsonProperty("voltage_phase_hz")]
            [JsonPropertyName("voltage_phase_hz")]
            public string VoltagePhaseHz { get; set; }
        }

        public class Diagnostic
        {
            [JsonProperty("id")]
            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [JsonProperty("url")]
            [JsonPropertyName("url")]
            public string? Url { get; set; }

            [JsonProperty("conversions")]
            [JsonPropertyName("conversions")]
            public List<Conversions>? Conversions { get; set; }
        }

        public class Iom
        {
            [JsonProperty("id")]
            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [JsonProperty("url")]
            [JsonPropertyName("url")]
            public string? Url { get; set; }

            [JsonProperty("conversions")]
            [JsonPropertyName("conversions")]
            public List<Conversions>? Conversions { get; set; }
        }

        public class Links
        {
            [JsonProperty("dynamic_link")]
            [JsonPropertyName("dynamic_link")]
            public string? DynamicLink { get; set; }
        }

        public class Documments
        {
            [JsonProperty("service_facts")]
            [JsonPropertyName("service_facts")]
            public List<ServiceFact>? ServiceFacts { get; set; }

            [JsonProperty("product_data")]
            [JsonPropertyName("product_data")]
            public List<ProductDatum>? ProductData { get; set; }

            [JsonProperty("iom")]
            [JsonPropertyName("iom")]
            public List<Iom>? Iom { get; set; }

            [JsonProperty("wiring_diagram")]
            [JsonPropertyName("wiring_diagram")]
            public List<WiringDiagram>? WiringDiagram { get; set; }

            [JsonProperty("misc")]
            [JsonPropertyName("misc")]
            public List<Misc>? Misc { get; set; }

            [JsonProperty("nomenclature")]
            [JsonPropertyName("nomenclature")]
            public List<Nomenclature>? Nomenclature { get; set; }

            [JsonProperty("parts_manuals")]
            [JsonPropertyName("parts_manuals")]
            public List<PartsManual>? PartsManuals { get; set; }

            [JsonProperty("metering_device_manuals")]
            [JsonPropertyName("metering_device_manuals")]
            public List<MeteringDeviceManual>? MeteringDeviceManuals { get; set; }

            [JsonProperty("options_accessories_manuals")]
            [JsonPropertyName("options_accessories_manuals")]
            public List<OptionsAccessoriesManual>? OptionsAccessoriesManuals { get; set; }

            [JsonProperty("product_brochures")]
            [JsonPropertyName("product_brochures")]
            public List<ProductBrochure>? ProductBrochures { get; set; }

            [JsonProperty("schematic_drawings")]
            [JsonPropertyName("schematic_drawings")]
            public List<SchematicDrawing>? SchematicDrawings { get; set; }

            [JsonProperty("warranty")]
            [JsonPropertyName("warranty")]
            public List<Warranty>? Warranty { get; set; }

            [JsonProperty("bluon_guidelines")]
            [JsonPropertyName("bluon_guidelines")]
            public List<BluonGuideline>? BluonGuidelines { get; set; }

            [JsonProperty("diagnostic")]
            [JsonPropertyName("diagnostic")]
            public List<Diagnostic>? Diagnostic { get; set; }

            [JsonProperty("controls_manuals")]
            [JsonPropertyName("controls_manuals")]
            public List<ControlsManual>? ControlsManuals { get; set; }
        }

        public class MeteringDeviceManual
        {
            [JsonProperty("id")]
            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [JsonProperty("url")]
            [JsonPropertyName("url")]
            public string? Url { get; set; }

            [JsonProperty("conversions")]
            [JsonPropertyName("conversions")]
            public List<Conversions>? Conversions { get; set; }
        }

        public class Misc
        {
            [JsonProperty("id")]
            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [JsonProperty("url")]
            [JsonPropertyName("url")]
            public string? Url { get; set; }

            [JsonProperty("conversions")]
            [JsonPropertyName("conversions")]
            public List<Conversions>? Conversions { get; set; }
        }

        public class Nomenclature
        {
            [JsonProperty("id")]
            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [JsonProperty("url")]
            [JsonPropertyName("url")]
            public string? Url { get; set; }

            [JsonProperty("conversions")]
            [JsonPropertyName("conversions")]
            public List<Conversions>? Conversions { get; set; }
        }

        public class OptionsAccessoriesManual
        {
            [JsonProperty("id")]
            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [JsonProperty("url")]
            [JsonPropertyName("url")]
            public string? Url { get; set; }

            [JsonProperty("conversions")]
            [JsonPropertyName("conversions")]
            public List<Conversions>? Conversions { get; set; }
        }

        public class PartsManual
        {
            [JsonProperty("id")]
            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [JsonProperty("url")]
            [JsonPropertyName("url")]
            public string? Url { get; set; }

            [JsonProperty("conversions")]
            [JsonPropertyName("conversions")]
            public List<Conversions>? Conversions { get; set; }
        }

        public class ProductBrochure
        {
            [JsonProperty("id")]
            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [JsonProperty("url")]
            [JsonPropertyName("url")]
            public string? Url { get; set; }

            [JsonProperty("conversions")]
            [JsonPropertyName("conversions")]
            public List<Conversions>? Conversions { get; set; }
        }

        public class ProductDatum
        {
            [JsonProperty("id")]
            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [JsonProperty("url")]
            [JsonPropertyName("url")]
            public string? Url { get; set; }

            [JsonProperty("conversions")]
            [JsonPropertyName("conversions")]
            public List<Conversions>? Conversions { get; set; }
        }

        public class SchematicDrawing
        {
            [JsonProperty("id")]
            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [JsonProperty("url")]
            [JsonPropertyName("url")]
            public string? Url { get; set; }

            [JsonProperty("conversions")]
            [JsonPropertyName("conversions")]
            public List<Conversions>? Conversions { get; set; }
        }

        public class Series
        {
            [JsonProperty("id")]
            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [JsonProperty("name")]
            [JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;

            [JsonProperty("image")]
            [JsonPropertyName("image")]
            public Image? Image { get; set; }
        }

        public class ServiceFact
        {
            [JsonProperty("id")]
            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [JsonProperty("url")]
            [JsonPropertyName("url")]
            public string? Url { get; set; }

            [JsonProperty("conversions")]
            [JsonPropertyName("conversions")]
            public List<Conversions>? Conversions { get; set; }
        }

        public class Warranty
        {
            [JsonProperty("id")]
            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [JsonProperty("url")]
            [JsonPropertyName("url")]
            public string? Url { get; set; }

            [JsonProperty("conversions")]
            [JsonPropertyName("conversions")]
            public List<Conversions>? Conversions { get; set; }
        }

        public class WiringDiagram
        {
            [JsonProperty("id")]
            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [JsonProperty("url")]
            [JsonPropertyName("url")]
            public string? Url { get; set; }

            [JsonProperty("conversions")]
            [JsonPropertyName("conversions")]
            public List<Conversions>? Conversions { get; set; }
        }


    }

    #endregion

    #region Get information for a model

    public class ModelInfo
    {
        public class BluonGuideline
        {
            [JsonProperty("id")]
            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [JsonProperty("url", NullValueHandling = NullValueHandling.Ignore)]
            [JsonPropertyName("url")]
            public string? Url { get; set; }

            [JsonProperty("conversions", NullValueHandling = NullValueHandling.Ignore)]
            [JsonPropertyName("conversions")]
            public List<Conversions>? Conversions { get; set; }
        }

        public class ControlsManual
        {
            [JsonProperty("id")]
            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [JsonProperty("url", NullValueHandling = NullValueHandling.Ignore)]
            [JsonPropertyName("url")]
            public string? Url { get; set; }

            [JsonProperty("conversions", NullValueHandling = NullValueHandling.Ignore)]
            [JsonPropertyName("conversions")]
            public List<Conversions>? Conversions { get; set; }
        }


        public class Data
        {
            [JsonProperty("id")]
            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [JsonProperty("model")]
            [JsonPropertyName("model")]
            public string? Model { get; set; }

            [JsonProperty("model_notes")]
            [JsonPropertyName("model_notes")]
            public string? ModelNotes { get; set; }

            [JsonProperty("manuals_count")]
            [JsonPropertyName("manuals_count")]
            public int? ManualsCount { get; set; }

            [JsonProperty("logo")]
            [JsonPropertyName("logo")]
            public string? Logo { get; set; }

            [JsonProperty("image")]
            [JsonPropertyName("image")]
            public string? Image { get; set; }

            [JsonProperty("system_type")]
            [JsonPropertyName("system_type")]
            public string? SystemType { get; set; }

            [JsonProperty("calling_groups")]
            [JsonPropertyName("calling_groups")]
            public string? CallingGroups { get; set; }

            [JsonProperty("updated_at")]
            [JsonPropertyName("updated_at")]
            public DateTime? UpdatedAt { get; set; }

            [JsonProperty("functional_parts_count")]
            [JsonPropertyName("functional_parts_count")]
            public int FunctionalPartsCount { get; set; }

            [JsonProperty("my_parts_count")]
            [JsonPropertyName("my_parts_count")]
            public int MyPartsCount { get; set; }

            [JsonProperty("my_parts_replacements_count")]
            [JsonPropertyName("my_parts_replacements_count")]
            public int MyPartsReplacementsCount { get; set; }

            [JsonProperty("root_model")]
            [JsonPropertyName("root_model")]
            public string? RootModel { get; set; }

            [JsonProperty("brand_name")]
            [JsonPropertyName("brand_name")]
            public string? BrandName { get; set; }

            [JsonProperty("brand_image")]
            [JsonPropertyName("brand_image")]
            public string? BrandImage { get; set; }

            [JsonProperty("series")]
            [JsonPropertyName("series")]
            public Series? Series { get; set; }

            [JsonProperty("manuals")]
            [JsonPropertyName("manuals")]
            public Manuals? Manuals { get; set; }

            [JsonProperty("parts")]
            [JsonPropertyName("parts")]
            public Parts? Parts { get; set; }

            [JsonProperty("number")]
            [JsonPropertyName("number")]
            public string? Number { get; set; }

            [JsonProperty("type")]
            [JsonPropertyName("type")]
            public string? Type { get; set; }

            [JsonProperty("subtype")]
            [JsonPropertyName("subtype")]
            public string? Subtype { get; set; }

            [JsonProperty("description")]
            [JsonPropertyName("description")]
            public string? Description { get; set; }

            [JsonProperty("brand")]
            [JsonPropertyName("brand")]
            public string? Brand { get; set; }

            [JsonProperty("subcategory")]
            [JsonPropertyName("subcategory")]
            public string? Subcategory { get; set; }

            [JsonProperty("mfg_type")]
            [JsonPropertyName("mfg_type")]
            public string? MfgType { get; set; }

            [JsonProperty("specifications")]
            [JsonPropertyName("specifications")]
            public PartSpecifications? Specifications { get; set; }

            [JsonProperty("notes")]
            [JsonPropertyName("notes")]
            public string? Notes { get; set; }

            [JsonProperty("replacements_count")]
            [JsonPropertyName("replacements_count")]
            public int ReplacementsCount { get; set; }

            [JsonProperty("replacements_aftermarket_count")]
            [JsonPropertyName("replacements_aftermarket_count")]
            public int ReplacementsAftermarketCount { get; set; }

            [JsonProperty("my_sku")]
            [JsonPropertyName("my_sku")]
            public string? MySku { get; set; }

            [JsonProperty("my_product_url")]
            [JsonPropertyName("my_product_url")]
            public string? MyProductUrl { get; set; }

            [JsonProperty("my_replacements_count")]
            [JsonPropertyName("my_replacements_count")]
            public int MyReplacementsCount { get; set; }
        }

        public class Diagnostic
        {
            [JsonProperty("id")]
            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [JsonProperty("url")]
            [JsonPropertyName("url")]
            public string? Url { get; set; }

            [JsonProperty("conversions")]
            [JsonPropertyName("conversions")]
            public List<Conversions>? Conversions { get; set; }
        }

        public class Iom
        {
            [JsonProperty("id")]
            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [JsonProperty("url")]
            [JsonPropertyName("url")]
            public string? Url { get; set; }

            [JsonProperty("conversions")]
            [JsonPropertyName("conversions")]
            public List<Conversions>? Conversions { get; set; }
        }



        public class Manuals
        {
            [JsonProperty("service_facts")]
            [JsonPropertyName("service_facts")]
            public List<ServiceFact> ServiceFacts { get; set; }

            [JsonProperty("product_data")]
            [JsonPropertyName("product_data")]
            public List<ProductDatum> ProductData { get; set; }

            [JsonProperty("iom")]
            [JsonPropertyName("iom")]
            public List<Iom> Iom { get; set; }

            [JsonProperty("wiring_diagram")]
            [JsonPropertyName("wiring_diagram")]
            public List<WiringDiagram> WiringDiagram { get; set; }

            [JsonProperty("nomenclature")]
            [JsonPropertyName("nomenclature")]
            public List<Nomenclature> Nomenclature { get; set; }

            [JsonProperty("parts_manuals")]
            [JsonPropertyName("parts_manuals")]
            public List<PartsManual> PartsManuals { get; set; }

            [JsonProperty("metering_device_manuals")]
            [JsonPropertyName("metering_device_manuals")]
            public List<MeteringDeviceManual> MeteringDeviceManuals { get; set; }

            [JsonProperty("options_accessories_manuals")]
            [JsonPropertyName("options_accessories_manuals")]
            public List<OptionsAccessoriesManual> OptionsAccessoriesManuals { get; set; }

            [JsonProperty("product_brochures")]
            [JsonPropertyName("product_brochures")]
            public List<ProductBrochure> ProductBrochures { get; set; }

            [JsonProperty("schematic_drawings")]
            [JsonPropertyName("schematic_drawings")]
            public List<SchematicDrawing> SchematicDrawings { get; set; }

            [JsonProperty("warranty")]
            [JsonPropertyName("warranty")]
            public List<Warranty> Warranty { get; set; }

            [JsonProperty("bluon_guidelines")]
            [JsonPropertyName("bluon_guidelines")]
            public List<BluonGuideline> BluonGuidelines { get; set; }

            [JsonProperty("diagnostic")]
            [JsonPropertyName("diagnostic")]
            public List<Diagnostic> Diagnostic { get; set; }

            [JsonProperty("misc")]
            [JsonPropertyName("misc")]
            public List<Misc> Misc { get; set; }

            [JsonProperty("controls_manuals")]
            [JsonPropertyName("controls_manuals")]
            public List<ControlsManual> ControlsManuals { get; set; }
        }



        public class MeteringDeviceManual
        {
            [JsonProperty("id")]
            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [JsonProperty("url")]
            [JsonPropertyName("url")]
            public string? Url { get; set; }

            [JsonProperty("conversions")]
            [JsonPropertyName("conversions")]
            public List<Conversions>? Conversions { get; set; }
        }

        public class Misc
        {
            [JsonProperty("id")]
            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [JsonProperty("url")]
            [JsonPropertyName("url")]
            public string? Url { get; set; }

            [JsonProperty("conversions")]
            [JsonPropertyName("conversions")]
            public List<Conversions>? Conversions { get; set; }
        }

        public class Nomenclature
        {
            [JsonProperty("id")]
            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [JsonProperty("url")]
            [JsonPropertyName("url")]
            public string? Url { get; set; }

            [JsonProperty("conversions")]
            [JsonPropertyName("conversions")]
            public List<Conversions>? Conversions { get; set; }
        }

        public class OptionsAccessoriesManual
        {
            [JsonProperty("id")]
            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [JsonProperty("url")]
            [JsonPropertyName("url")]
            public string? Url { get; set; }

            [JsonProperty("conversions")]
            [JsonPropertyName("conversions")]
            public List<Conversions>? Conversions { get; set; }
        }

        public class PartsDatum
        {
            [JsonProperty("id")]
            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [JsonProperty("number")]
            [JsonPropertyName("number")]
            public string? Number { get; set; }

            [JsonProperty("type")]
            [JsonPropertyName("type")]
            public string? Type { get; set; }

            [JsonProperty("subtype")]
            [JsonPropertyName("subtype")]
            public string? Subtype { get; set; }

            [JsonProperty("description")]
            [JsonPropertyName("description")]
            public string? Description { get; set; }

            [JsonProperty("brand")]
            [JsonPropertyName("brand")]
            public string? Brand { get; set; }

            [JsonProperty("image")]
            [JsonPropertyName("image")]
            public Image? Image { get; set; }

            [JsonProperty("subcategory")]
            [JsonPropertyName("subcategory")]
            public string? Subcategory { get; set; }

            [JsonProperty("mfg_type")]
            [JsonPropertyName("mfg_type")]
            public string? MfgType { get; set; }

            [JsonProperty("updated_at")]
            [JsonPropertyName("updated_at")]
            public string? UpdatedAt { get; set; }

            [JsonProperty("specifications")]
            [JsonPropertyName("specifications")]
            public PartSpecifications? Specifications { get; set; }

            [JsonProperty("notes")]
            [JsonPropertyName("notes")]
            public string? Notes { get; set; }

            [JsonProperty("replacements_count")]
            [JsonPropertyName("replacements_count")]
            public int ReplacementsCount { get; set; }

            [JsonProperty("replacements_aftermarket_count")]
            [JsonPropertyName("replacements_aftermarket_count")]
            public int ReplacementsAftermarketCount { get; set; }

            [JsonProperty("my_sku")]
            [JsonPropertyName("my_sku")]
            public string? MySku { get; set; }

            [JsonProperty("my_product_url")]
            [JsonPropertyName("my_product_url")]
            public string? MyProductUrl { get; set; }

            [JsonProperty("my_replacements_count")]
            [JsonPropertyName("my_replacements_count")]
            public int MyReplacementsCount { get; set; }
        }

        public class Parts
        {
            [JsonProperty("data")]
            [JsonPropertyName("data")]
            public List<PartsDatum> Data { get; set; }

            [JsonProperty("links")]
            [JsonPropertyName("links")]
            public Link? Links { get; set; }

            [JsonProperty("meta")]
            [JsonPropertyName("meta")]
            public Meta? Meta { get; set; }
        }

        public class PartsManual
        {
            [JsonProperty("id")]
            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [JsonProperty("url")]
            [JsonPropertyName("url")]
            public string? Url { get; set; }

            [JsonProperty("conversions")]
            [JsonPropertyName("conversions")]
            public List<Conversions>? Conversions { get; set; }
        }

        public class ProductBrochure
        {
            [JsonProperty("id")]
            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [JsonProperty("url")]
            [JsonPropertyName("url")]
            public string? Url { get; set; }

            [JsonProperty("conversions")]
            [JsonPropertyName("conversions")]
            public List<Conversions>? Conversions { get; set; }
        }

        public class ProductDatum
        {
            [JsonProperty("id")]
            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [JsonProperty("url")]
            [JsonPropertyName("url")]
            public string? Url { get; set; }

            [JsonProperty("conversions")]
            [JsonPropertyName("conversions")]
            public List<Conversions>? Conversions { get; set; }
        }

        public class Root
        {
            [JsonProperty("data")]
            [JsonPropertyName("data")]
            public Data Data { get; set; }
        }

        public class SchematicDrawing
        {
            [JsonProperty("id")]
            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [JsonProperty("url")]
            [JsonPropertyName("url")]
            public string? Url { get; set; }

            [JsonProperty("conversions")]
            [JsonPropertyName("conversions")]
            public List<Conversions>? Conversions { get; set; }
        }

        public class Series
        {
            [JsonProperty("id")]
            [JsonPropertyName("id")]
            public int? Id { get; set; }

            [JsonProperty("name")]
            [JsonPropertyName("name")]
            public string? Name { get; set; }

            [JsonProperty("image")]
            [JsonPropertyName("image")]
            public Image Image { get; set; }
        }

        public class ServiceFact
        {
            [JsonProperty("id")]
            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [JsonProperty("url")]
            [JsonPropertyName("url")]
            public string? Url { get; set; }

            [JsonProperty("conversions")]
            [JsonPropertyName("conversions")]
            public List<Conversions>? Conversions { get; set; }
        }

        public class Warranty
        {
            [JsonProperty("id")]
            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [JsonProperty("url")]
            [JsonPropertyName("url")]
            public string? Url { get; set; }

            [JsonProperty("conversions")]
            [JsonPropertyName("conversions")]
            public List<Conversions>? Conversions { get; set; }
        }

        public class WiringDiagram
        {
            [JsonProperty("id")]
            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [JsonProperty("url")]
            [JsonPropertyName("url")]
            public string? Url { get; set; }

            [JsonProperty("conversions")]
            [JsonPropertyName("conversions")]
            public List<Conversions>? Conversions { get; set; }
        }


    }

    #endregion

    #region Model Retrieve Methods 

    public async Task<ModelSearch.Root?> ModelNameSearch(string search, int page = 1, int perpage = 50, CancellationToken cancellationToken = default, int cacheTimeout = 4320)
    {
        string url = $"{apiroot}/oems?model={Uri.EscapeDataString(search)}&page={page}&per_page={perpage}";
        string? jsonreturned = await BluonGet(url, cancellationToken, cacheTimeout);
        if (string.IsNullOrEmpty(jsonreturned))
            return null;
        return JsonConvert.DeserializeObject<ModelSearch.Root>(jsonreturned);
    }

    public async Task<ModelParts.Root?> GetPartsForModel(string modelId, string brand, int page = 1, int perpage = 50, CancellationToken cancellationToken = default, int cacheTimeout = 4320)
    {
        string url = $"{apiroot}/brands/{brand}/oems/{modelId}/parts?page={page}&per_page={perpage}";
        string? jsonreturned = await BluonGet(url, cancellationToken, cacheTimeout);
        if (string.IsNullOrEmpty(jsonreturned))
            return null;
        return JsonConvert.DeserializeObject<ModelParts.Root>(jsonreturned);
    }

    public async Task<ModelManuals.Root?> GetManualsForModel(string modelId, CancellationToken cancellationToken = default, int cacheTimeout = 4320)
    {
        string url = $"{apiroot}/oems/{modelId}";
        string? jsonreturned = await BluonGet(url, cancellationToken, cacheTimeout);
        if (string.IsNullOrEmpty(jsonreturned))
            return null;
        return JsonConvert.DeserializeObject<ModelManuals.Root>(jsonreturned);
    }

    public async Task<ModelInfo.Root?> GetModelInfo(string modelName, int page = 1, int perpage = 50, CancellationToken cancellationToken = default, int cacheTimeout = 4320)
    {
        string url = $"{apiroot}/oems/by-model/{modelName}?parts_page={page}";
        string? jsonreturned = await BluonGet(url, cancellationToken, cacheTimeout);
        if (string.IsNullOrEmpty(jsonreturned))
            return null;
        return JsonConvert.DeserializeObject<ModelInfo.Root>(jsonreturned);
    }

    public async Task<ModelInfo.Root?> GetModelInfoWithAllParts(string modelName, int perpage = 50, CancellationToken cancellationToken = default, int cacheTimeout = 4320)
    {
        // Get the first page to get the model info
        var firstPage = await GetModelInfo(modelName, 1, perpage, cancellationToken, cacheTimeout);
        if (firstPage?.Data == null)
            return null;

        // If there's only one page of parts, return as is
        if (firstPage.Data.Parts?.Meta == null || firstPage.Data.Parts.Meta.LastPage <= 1)
            return firstPage;

        // Otherwise, collect all parts
        var allParts = new List<ModelInfo.PartsDatum>();
        if (firstPage.Data.Parts?.Data != null)
            allParts.AddRange(firstPage.Data.Parts.Data);

        // Get remaining pages
        for (int page = 2; page <= ((firstPage?.Data?.Parts?.Meta.LastPage) ?? 3) && !cancellationToken.IsCancellationRequested; page++)
        {
            var response = await GetModelInfo(modelName, page, perpage, cancellationToken, cacheTimeout);
            if (response?.Data?.Parts?.Data != null)
            {
                allParts.AddRange(response.Data.Parts.Data);
            }
        }

        // Update the parts list with all collected parts
        firstPage.Data.Parts.Data = allParts;
        if (firstPage.Data.Parts.Meta != null)
        {
            firstPage.Data.Parts.Meta.Total = allParts.Count;
            firstPage.Data.Parts.Meta.To = allParts.Count;
        }

        return firstPage;
    }

    #endregion

    #region Get All Methods - Sequential

    public async Task<List<ModelSearch.ModelData>> GetAllModelSearch(string search, int perpage = 50, CancellationToken cancellationToken = default)
    {
        var allModels = new List<ModelSearch.ModelData>();
        int currentPage = 1;
        bool hasMorePages = true;

        while (hasMorePages && !cancellationToken.IsCancellationRequested)
        {
            var response = await ModelNameSearch(search, currentPage, perpage, cancellationToken);

            if (response?.Data != null && response.Data.Any())
            {
                allModels.AddRange(response.Data);
                hasMorePages = response.Meta != null && currentPage < response.Meta.LastPage;
                currentPage++;
            }
            else
            {
                hasMorePages = false;
            }
        }

        return allModels;
    }

    public async Task<List<ModelParts.Datum>> GetAllPartsForModel(string modelId, string brand, int perpage = 50, CancellationToken cancellationToken = default)
    {
        var allParts = new List<ModelParts.Datum>();
        int currentPage = 1;
        bool hasMorePages = true;

        while (hasMorePages && !cancellationToken.IsCancellationRequested)
        {
            var response = await GetPartsForModel(modelId, brand, currentPage, perpage, cancellationToken);

            if (response?.Data != null && response.Data.Any())
            {
                allParts.AddRange(response.Data);
                hasMorePages = response.Meta != null && currentPage < response.Meta.LastPage;
                currentPage++;
            }
            else
            {
                hasMorePages = false;
            }
        }

        return allParts;
    }

    public async Task<List<ModelInfo.PartsDatum>> GetAllModelInfoParts(string modelName, int perpage = 50, CancellationToken cancellationToken = default)
    {
        var allParts = new List<ModelInfo.PartsDatum>();
        int currentPage = 1;
        bool hasMorePages = true;

        while (hasMorePages && !cancellationToken.IsCancellationRequested)
        {
            var response = await GetModelInfo(modelName, currentPage, perpage, cancellationToken);

            if (response?.Data?.Parts?.Data != null && response.Data.Parts.Data.Any())
            {
                allParts.AddRange(response.Data.Parts.Data);
                hasMorePages = response.Data.Parts.Meta != null && currentPage < response.Data.Parts.Meta.LastPage;
                currentPage++;
            }
            else
            {
                hasMorePages = false;
            }
        }

        return allParts;
    }


    #endregion

    #region Get All Methods - Parallel

    public async Task<List<ModelSearch.ModelData>> GetAllModelSearchParallel(string search, int perpage = 50, CancellationToken cancellationToken = default)
    {
        var firstPage = await ModelNameSearch(search, 1, perpage, cancellationToken);
        if (firstPage?.Data == null || firstPage.Meta == null)
            return new List<ModelSearch.ModelData>();

        var allModels = new List<ModelSearch.ModelData>();
        allModels.AddRange(firstPage.Data);

        if (firstPage.Meta.LastPage <= 1)
            return allModels;

        var tasks = new List<Task<ModelSearch.Root?>>();
        for (int page = 2; page <= firstPage.Meta.LastPage; page++)
        {
            tasks.Add(ModelNameSearch(search, page, perpage, cancellationToken));
        }

        var results = await Task.WhenAll(tasks);

        foreach (var result in results)
        {
            if (result?.Data != null)
            {
                allModels.AddRange(result.Data);
            }
        }

        return allModels;
    }

    public async Task<List<ModelParts.Datum>> GetAllPartsForModelParallel(string modelId, string brand, int perpage = 50, CancellationToken cancellationToken = default)
    {
        var firstPage = await GetPartsForModel(modelId, brand, 1, perpage, cancellationToken);
        if (firstPage?.Data == null || firstPage.Meta == null)
            return new List<ModelParts.Datum>();

        var allParts = new List<ModelParts.Datum>();
        allParts.AddRange(firstPage.Data);

        if (firstPage.Meta.LastPage <= 1)
            return allParts;

        var tasks = new List<Task<ModelParts.Root?>>();
        for (int page = 2; page <= firstPage.Meta.LastPage; page++)
        {
            tasks.Add(GetPartsForModel(modelId, brand, page, perpage, cancellationToken));
        }

        var results = await Task.WhenAll(tasks);

        foreach (var result in results)
        {
            if (result?.Data != null)
            {
                allParts.AddRange(result.Data);
            }
        }

        return allParts;
    }

    public async Task<List<ModelInfo.PartsDatum>> GetAllModelInfoPartsParallel(string modelName, int perpage = 50, CancellationToken cancellationToken = default)
    {
        var firstPage = await GetModelInfo(modelName, 1, perpage, cancellationToken);
        if (firstPage?.Data?.Parts?.Data == null || firstPage.Data.Parts.Meta == null)
            return new List<ModelInfo.PartsDatum>();

        var allParts = new List<ModelInfo.PartsDatum>();
        allParts.AddRange(firstPage.Data.Parts.Data);

        if (firstPage.Data.Parts.Meta.LastPage <= 1)
            return allParts;

        var tasks = new List<Task<ModelInfo.Root?>>();
        for (int page = 2; page <= firstPage.Data.Parts.Meta.LastPage; page++)
        {
            tasks.Add(GetModelInfo(modelName, page, perpage, cancellationToken));
        }

        var results = await Task.WhenAll(tasks);

        foreach (var result in results)
        {
            if (result?.Data?.Parts?.Data != null)
            {
                allParts.AddRange(result.Data.Parts.Data);
            }
        }

        return allParts;
    }

    public async Task<ModelInfo.Root?> GetModelInfoWithAllPartsParallel(string modelName, int perpage = 50, CancellationToken cancellationToken = default)
    {
        // Get the first page to get the model info
        var firstPage = await GetModelInfo(modelName, 1, perpage, cancellationToken);
        if (firstPage?.Data == null)
            return null;

        // If there's only one page of parts, return as is
        if (firstPage.Data.Parts?.Meta == null || firstPage.Data.Parts.Meta.LastPage <= 1)
            return firstPage;

        // Otherwise, collect all parts in parallel
        var allParts = new List<ModelInfo.PartsDatum>();
        if (firstPage.Data.Parts?.Data != null)
            allParts.AddRange(firstPage.Data.Parts.Data);

        // Get remaining pages in parallel
        var tasks = new List<Task<ModelInfo.Root?>>();
        for (int page = 2; page <= firstPage.Data.Parts.Meta.LastPage; page++)
        {
            tasks.Add(GetModelInfo(modelName, page, perpage, cancellationToken));
        }

        var results = await Task.WhenAll(tasks);

        foreach (var result in results)
        {
            if (result?.Data?.Parts?.Data != null)
            {
                allParts.AddRange(result.Data.Parts.Data);
            }
        }

        // Update the parts list with all collected parts
        firstPage.Data.Parts.Data = allParts;
        if (firstPage.Data.Parts.Meta != null)
        {
            firstPage.Data.Parts.Meta.Total = allParts.Count;
            firstPage.Data.Parts.Meta.To = allParts.Count;
        }

        return firstPage;
    }


    #endregion
}

public class BluonSerialDecoder : BluonBase
{

    #region Constructor and Global Variables

    public BluonSerialDecoder(string apikey, string apiroot, string userIdentifier, IBluonCache? cache = null) : base(cache)
    {
        this.apikey = apikey;
        this.apiroot = apiroot;
        this.apiuser = userIdentifier;
    }

    public BluonSerialDecoder(string apikey, string apiroot, string userIdentifier) : base()
    {
        this.apikey = apikey;
        this.apiroot = apiroot;
        this.apiuser = userIdentifier;
    }

    public BluonSerialDecoder() : base() { }
    ~BluonSerialDecoder() { }

    #endregion

    #region Decoded Info

    public class DecodedData
    {

        public class Data
        {
            [JsonProperty("manufacturing_date")]
            [JsonPropertyName("manufacturing_date")]
            public List<string?> ManufacturingDate { get; set; }

            [JsonProperty("warranty")]
            [JsonPropertyName("warranty")]
            public Warranty? Warranty { get; set; }

            [JsonProperty("warranty_prediction")]
            [JsonPropertyName("warranty_prediction")]
            public string? WarrantyPrediction { get; set; }
        }

        public class Root
        {
            [JsonProperty("data")]
            [JsonPropertyName("data")]
            public Data? Data { get; set; }
        }

        public class Warranty
        {
            [JsonProperty("standard_warranty_registered")]
            [JsonPropertyName("standard_warranty_registered")]
            public int StandardWarrantyRegistered { get; set; }

            [JsonProperty("standard_warranty_unregistered")]
            [JsonPropertyName("standard_warranty_unregistered")]
            public int StandardWarrantyUnregistered { get; set; }

            [JsonProperty("parts_warranty_registered")]
            [JsonPropertyName("parts_warranty_registered")]
            public int PartsWarrantyRegistered { get; set; }

            [JsonProperty("parts_warranty_unregistered")]
            [JsonPropertyName("parts_warranty_unregistered")]
            public int PartsWarrantyUnregistered { get; set; }

            [JsonProperty("compressor_warranty")]
            [JsonPropertyName("compressor_warranty")]
            public string? CompressorWarranty { get; set; }

            [JsonProperty("heat_exchanger_warranty")]
            [JsonPropertyName("heat_exchanger_warranty")]
            public string? HeatExchangerWarranty { get; set; }

            [JsonProperty("lifetime_warranty")]
            [JsonPropertyName("lifetime_warranty")]
            public string? LifetimeWarranty { get; set; }

            [JsonProperty("no_hassle_warranty")]
            [JsonPropertyName("no_hassle_warranty")]
            public string? NoHassleWarranty { get; set; }

            [JsonProperty("labor_warranty")]
            [JsonPropertyName("labor_warranty")]
            public string? LaborWarranty { get; set; }

            [JsonProperty("extended_warranty")]
            [JsonPropertyName("extended_warranty")]
            public string? ExtendedWarranty { get; set; }

            [JsonProperty("conditional_unit_replacement_warranty")]
            [JsonPropertyName("conditional_unit_replacement_warranty")]
            public string? ConditionalUnitReplacementWarranty { get; set; }

            [JsonProperty("transferrable")]
            [JsonPropertyName("transferrable")]
            public string? Transferrable { get; set; }

            [JsonProperty("warranty_details")]
            [JsonPropertyName("warranty_details")]
            public string? WarrantyDetails { get; set; }
        }
    }
    #endregion

    #region Bluon Serial Decoder
    public async Task<BluonSerialDecoder.DecodedData.Root?> DecodeSerial(string brand, string serial)
    {
        string url = $"{apiroot}/serial-decoder?serial_number={serial}&brand={brand}";
        string? jsonreturned = await BluonGet(url);
        if (string.IsNullOrEmpty(jsonreturned))
            return null;
        return JsonConvert.DeserializeObject<BluonSerialDecoder.DecodedData.Root>(jsonreturned);
    }

    public async Task<DecodedData.Root?> DecodeSerial(string brand, string serial, CancellationToken cancellationToken = default)
    {
        string url = $"{apiroot}/serial-decoder?serial_number={serial}&brand={brand}";
        string? jsonreturned = await BluonGet(url, cancellationToken);
        if (string.IsNullOrEmpty(jsonreturned))
            return null;
        return JsonConvert.DeserializeObject<DecodedData.Root>(jsonreturned);
    }

    #endregion
}

public class BluonAgeAndWarranty : BluonBase
{

    #region Constructor and Global Variables

    public BluonAgeAndWarranty(string apikey, string apiroot, string userIdentifier, IBluonCache? cache = null) : base(cache)
    {
        this.apikey = apikey;
        this.apiroot = apiroot;
        this.apiuser = userIdentifier;
    }

    public BluonAgeAndWarranty(string apikey, string apiroot, string userIdentifier) : base()
    {
        this.apikey = apikey;
        this.apiroot = apiroot;
        this.apiuser = userIdentifier;
    }

    public BluonAgeAndWarranty() : base() { }
    ~BluonAgeAndWarranty() { }

    #endregion

    #region Decoded Info

    public class AgeWarrantyData
    {

        public class Data
        {
            [JsonProperty("brand")]
            [JsonPropertyName("brand")]
            public string? Brand { get; set; }

            [JsonProperty("serial_number")]
            [JsonPropertyName("serial_number")]
            public string? SerialNumber { get; set; }

            [JsonProperty("manufacturing_date")]
            [JsonPropertyName("manufacturing_date")]
            public string? ManufacturingDate { get; set; }

            [JsonProperty("warranty")]
            [JsonPropertyName("warranty")]
            public Warranty? Warranty { get; set; }
        }

        public class Root
        {
            [JsonProperty("data")]
            [JsonPropertyName("data")]
            public Data? Data { get; set; }
        }

        public class Warranty
        {
            [JsonProperty("standard_warranty_registered")]
            [JsonPropertyName("standard_warranty_registered")]
            public string? StandardWarrantyRegistered { get; set; }

            [JsonProperty("standard_warranty_unregistered")]
            [JsonPropertyName("standard_warranty_unregistered")]
            public string? StandardWarrantyUnregistered { get; set; }

            [JsonProperty("warranty_details")]
            [JsonPropertyName("warranty_details")]
            public string? WarrantyDetails { get; set; }
        }


    }
    #endregion

    #region Bluon Age and Warranty Gets
    public async Task<BluonAgeAndWarranty.AgeWarrantyData.Root?> AgeAndWarranty(string brand, string serial)
    {
        CancellationToken cancellationToken = new CancellationToken();
        string url = $"{apiroot}/age-and-warranty";
        
        var fieldcollection = new List<KeyValuePair<string, string>>();
            fieldcollection.Add(new("brand", brand));
            fieldcollection.Add(new("serial_number", serial));
        FormUrlEncodedContent content = new FormUrlEncodedContent(fieldcollection);

        string? jsonreturned = await BluonPost(url, content,null, cancellationToken);
        if (string.IsNullOrEmpty(jsonreturned))
            return null;
        return JsonConvert.DeserializeObject<BluonAgeAndWarranty.AgeWarrantyData.Root>(jsonreturned);
    }

    

    #endregion
}


public class BluonNameplateReader : BluonBase
{

    #region Constructor and Global Variables

    public BluonNameplateReader(string apikey, string apiroot, string userIdentifier, IBluonCache? cache = null) : base(cache)
    {
        this.apikey = apikey;
        this.apiroot = apiroot;
        this.apiuser = userIdentifier;
    }

    public BluonNameplateReader(string apikey, string apiroot, string userIdentifier) : base()
    {
        this.apikey = apikey;
        this.apiroot = apiroot;
        this.apiuser = userIdentifier;
    }

    public BluonNameplateReader() : base() { }
    ~BluonNameplateReader() { }

    #endregion

    #region Decoded Info

    public class BluonNameplateReaderData
    {

        public class Data
        {
            [JsonProperty("brand")]
            [JsonPropertyName("brand")]
            public string? Brand { get; set; }

            [JsonProperty("serial_number")]
            [JsonPropertyName("serial_number")]
            public string? SerialNumber { get; set; }

            [JsonProperty("manufacturing_date")]
            [JsonPropertyName("manufacturing_date")]
            public string? ManufacturingDate { get; set; }

            [JsonProperty("warranty")]
            [JsonPropertyName("warranty")]
            public Warranty? Warranty { get; set; }
        }

        public class Root
        {
            [JsonProperty("data")]
            [JsonPropertyName("data")]
            public Data? Data { get; set; }
        }

        public class Warranty
        {
            [JsonProperty("standard_warranty_registered")]
            [JsonPropertyName("standard_warranty_registered")]
            public string? StandardWarrantyRegistered { get; set; }

            [JsonProperty("standard_warranty_unregistered")]
            [JsonPropertyName("standard_warranty_unregistered")]
            public string? StandardWarrantyUnregistered { get; set; }

            [JsonProperty("warranty_details")]
            [JsonPropertyName("warranty_details")]
            public string? WarrantyDetails { get; set; }
        }


    }
    #endregion

    #region Bluon Nameplate Reader 
    public async Task<BluonNameplateReaderData.Root?> ProcessNameplate(Byte[]? dataplateImage, string? strName = "dataplate.jpg")
    {
        CancellationToken cancellationToken = new CancellationToken();
        

        string? jsonreturned = await BluonNameplateReader(dataplateImage,strName,cancellationToken,45);
        
        if (string.IsNullOrEmpty(jsonreturned))
            return null;
        
        return JsonConvert.DeserializeObject<BluonNameplateReaderData.Root>(jsonreturned);
    }



    #endregion
}


public interface IBluonCache
{

    #region Cache Interface Methods
    // Retrieves cached content if available and not expired
    Task<string?> GetIfCachedAsync(int urlHash, bool allowStale = false);

    Task<string?> GetIfCachedByURLAsync(string url, bool allowStale = false);

    // Saves content to cache with expiration
    Task SaveCacheCopyAsync(int urlHash, string url, string json, int expirationMinutes = 60);

    #endregion

}

