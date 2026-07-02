using Microsoft.AspNetCore.Components;

using static ICS.Mobile.DataModels.Custom.HVACCalculations;


namespace ICS.Mobile.Pages.Airflow
{
    public partial class AirflowCalcPage : PageBase
    {
        protected AirflowInputs Inputs { get; set; } = new();
        protected AirflowResults? Results { get; set; }

        protected override void OnInitialized()
        {
            // Initialize with default values
            Inputs = new AirflowInputs
            {
                SystemCapacity = 36000,
                SupplyAirTemp = 55,
                ReturnAirTemp = 75,
                SystemType = "Air Conditioning",
                RefrigerantType = "R-410A",
                StaticPressure = 0.5,
                DuctDiameter = 12,
                MeasuredCFM = 0,
                IsR22System = false
            };
        }

        protected void OnInputChanged()
        {
            // Auto-calculate when inputs change
            CalculateAirflow();
        }
        public void HomePageClick()
        {
            PageNavManager.NavigateTo("/calcmenu");
        }
        protected void CalculateAirflow()
        {
            try
            {
                var systemTypeProps = GetSystemTypeProperties();
                var staticPressureGuide = GetStaticPressureGuidelines();

                // Convert BTU/hr to tons
                var systemTons = Inputs.SystemCapacity / 12000.0;

                // Calculate temperature differential
                var temperatureDiff = Inputs.ReturnAirTemp - Inputs.SupplyAirTemp;

                // Calculate CFM using the standard formula: CFM = BTU/hr ÷ (1.08 × ΔT)
                var calculatedCFM = temperatureDiff > 0 ? Inputs.SystemCapacity / (1.08 * temperatureDiff) : 0;

                // Calculate CFM per ton
                var cfmPerTon = systemTons > 0 ? calculatedCFM / systemTons : 0;

                // Get target CFM/ton based on system type
                var targetProps = systemTypeProps[Inputs.SystemType];
                var targetCFMPerTon = targetProps.IdealCFMPerTon;

                // Calculate airflow efficiency
                var airflowEfficiency = targetCFMPerTon > 0 ? (cfmPerTon / targetCFMPerTon) * 100 : 0;

                // Calculate velocity if duct diameter is provided
                var velocity = 0.0;
                if (Inputs.DuctDiameter > 0)
                {
                    var ductArea = Math.PI * Math.Pow(Inputs.DuctDiameter / 12.0 / 2.0, 2); // Convert to sq ft
                    velocity = ductArea > 0 ? calculatedCFM / ductArea : 0;
                }

                // Determine airflow status
                string airflowStatus, recommendation;

                if (cfmPerTon < targetProps.MinCFMPerTon)
                {
                    airflowStatus = "LOW AIRFLOW";
                    recommendation = $"Airflow is below optimal levels. Target: {targetCFMPerTon} CFM/ton. Check for dirty filters, blocked ducts, or undersized ductwork.";
                }
                else if (cfmPerTon > targetProps.MaxCFMPerTon)
                {
                    airflowStatus = "HIGH AIRFLOW";
                    recommendation = $"Airflow is higher than optimal. This may indicate oversized fan, low static pressure, or duct leaks.";
                }
                else
                {
                    airflowStatus = "OPTIMAL AIRFLOW";
                    recommendation = $"Airflow is within acceptable range for {Inputs.SystemType.ToLower()} systems.";
                }

                // Generate warnings
                var warnings = new List<string>();
                var r22Issues = new List<string>();

                // Temperature differential warnings
                if (temperatureDiff < 15)
                    warnings.Add("Low temperature differential may indicate insufficient airflow or system issues");
                if (temperatureDiff > 25)
                    warnings.Add("High temperature differential may indicate excessive airflow or coil issues");

                // Static pressure warnings
                if (Inputs.StaticPressure < staticPressureGuide.Low)
                    warnings.Add("Low static pressure may indicate duct leaks or oversized ductwork");
                if (Inputs.StaticPressure > staticPressureGuide.High)
                    warnings.Add("High static pressure indicates airflow restriction - check filters and ductwork");
                if (Inputs.StaticPressure > staticPressureGuide.Excessive)
                    warnings.Add("Excessive static pressure - immediate attention required");

                // Velocity warnings
                if (velocity > 1000)
                    warnings.Add("High duct velocity may cause noise and pressure loss");
                if (velocity < 300 && velocity > 0)
                    warnings.Add("Very low duct velocity - check for oversized ducts");

                // R-22 specific analysis
                if (Inputs.IsR22System)
                {
                    if (cfmPerTon < 350)
                        r22Issues.Add("R-22 systems commonly develop low airflow due to aging components");
                    if (Inputs.StaticPressure > 0.7)
                        r22Issues.Add("High static pressure common in aging R-22 systems - check evaporator coil cleanliness");
                    if (temperatureDiff > 22)
                        r22Issues.Add("High temperature differential may indicate dirty evaporator coil or low refrigerant");

                    // Additional R-22 system checks
                    r22Issues.Add("Recommend thorough coil cleaning and filter replacement for R-22 systems");
                    if (DateTime.Now.Year > 2020)
                        r22Issues.Add("Consider system replacement - R-22 refrigerant is expensive and being phased out");
                }

                // Measured CFM comparison
                if (Inputs.MeasuredCFM > 0)
                {
                    var measuredCFMPerTon = Inputs.MeasuredCFM / systemTons;
                    var difference = Math.Abs(calculatedCFM - Inputs.MeasuredCFM);
                    var percentDiff = (difference / calculatedCFM) * 100;

                    if (percentDiff > 15)
                        warnings.Add($"Measured CFM ({Inputs.MeasuredCFM:F0}) differs significantly from calculated CFM - verify measurements");
                }

                Results = new AirflowResults
                {
                    SystemTons = systemTons,
                    TemperatureDifferential = temperatureDiff,
                    CalculatedCFM = calculatedCFM,
                    CFMPerTon = cfmPerTon,
                    TargetCFMPerTon = targetCFMPerTon,
                    AirflowEfficiency = airflowEfficiency,
                    Velocity = velocity,
                    AirflowStatus = airflowStatus,
                    Recommendation = recommendation,
                    Warnings = warnings,
                    R22SpecificIssues = r22Issues,
                    StaticPressureStatus = Inputs.StaticPressure
                };

                StateHasChanged();
            }
            catch (Exception ex)
            {
                // Handle calculation errors
                Results = null;
                Console.WriteLine($"Airflow calculation error: {ex.Message}");
            }
        }

        protected RenderFragment GetStatusIcon()
        {
            if (Results == null)
            {
                return builder =>
                {
                    builder.OpenElement(0, "svg");
                    builder.AddAttribute(1, "class", "me-2");
                    builder.AddAttribute(2, "width", "20");
                    builder.AddAttribute(3, "height", "20");
                    builder.AddAttribute(4, "fill", "none");
                    builder.AddAttribute(5, "stroke", "currentColor");
                    builder.AddAttribute(6, "viewBox", "0 0 24 24");
                    builder.OpenElement(7, "path");
                    builder.AddAttribute(8, "stroke-linecap", "round");
                    builder.AddAttribute(9, "stroke-linejoin", "round");
                    builder.AddAttribute(10, "stroke-width", "2");
                    builder.AddAttribute(11, "d", "M13 10V3L4 14h7v7l9-11h-7z");
                    builder.CloseElement();
                    builder.CloseElement();
                };
            }

            return Results.AirflowStatus switch
            {
                "OPTIMAL AIRFLOW" => builder =>
                {
                    builder.OpenElement(0, "svg");
                    builder.AddAttribute(1, "class", "me-2 text-success");
                    builder.AddAttribute(2, "width", "20");
                    builder.AddAttribute(3, "height", "20");
                    builder.AddAttribute(4, "fill", "none");
                    builder.AddAttribute(5, "stroke", "currentColor");
                    builder.AddAttribute(6, "viewBox", "0 0 24 24");
                    builder.OpenElement(7, "path");
                    builder.AddAttribute(8, "stroke-linecap", "round");
                    builder.AddAttribute(9, "stroke-linejoin", "round");
                    builder.AddAttribute(10, "stroke-width", "2");
                    builder.AddAttribute(11, "d", "M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z");
                    builder.CloseElement();
                    builder.CloseElement();
                }
                ,
                "LOW AIRFLOW" => builder =>
                {
                    builder.OpenElement(0, "svg");
                    builder.AddAttribute(1, "class", "me-2 text-danger");
                    builder.AddAttribute(2, "width", "20");
                    builder.AddAttribute(3, "height", "20");
                    builder.AddAttribute(4, "fill", "none");
                    builder.AddAttribute(5, "stroke", "currentColor");
                    builder.AddAttribute(6, "viewBox", "0 0 24 24");
                    builder.OpenElement(7, "path");
                    builder.AddAttribute(8, "stroke-linecap", "round");
                    builder.AddAttribute(9, "stroke-linejoin", "round");
                    builder.AddAttribute(10, "stroke-width", "2");
                    builder.AddAttribute(11, "d", "M10 14l2-2m0 0l2-2m-2 2l-2-2m2 2l2 2m7-2a9 9 0 11-18 0 9 9 0 0118 0z");
                    builder.CloseElement();
                    builder.CloseElement();
                }
                ,
                "HIGH AIRFLOW" => builder =>
                {
                    builder.OpenElement(0, "svg");
                    builder.AddAttribute(1, "class", "me-2 text-warning");
                    builder.AddAttribute(2, "width", "20");
                    builder.AddAttribute(3, "height", "20");
                    builder.AddAttribute(4, "fill", "none");
                    builder.AddAttribute(5, "stroke", "currentColor");
                    builder.AddAttribute(6, "viewBox", "0 0 24 24");
                    builder.OpenElement(7, "path");
                    builder.AddAttribute(8, "stroke-linecap", "round");
                    builder.AddAttribute(9, "stroke-linejoin", "round");
                    builder.AddAttribute(10, "stroke-width", "2");
                    builder.AddAttribute(11, "d", "M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.728-.833-2.498 0L4.316 15.5c-.77.833.192 2.5 1.732 2.5z");
                    builder.CloseElement();
                    builder.CloseElement();
                }
                ,
                _ => builder =>
                {
                    builder.OpenElement(0, "svg");
                    builder.AddAttribute(1, "class", "me-2");
                    builder.AddAttribute(2, "width", "20");
                    builder.AddAttribute(3, "height", "20");
                    builder.AddAttribute(4, "fill", "none");
                    builder.AddAttribute(5, "stroke", "currentColor");
                    builder.AddAttribute(6, "viewBox", "0 0 24 24");
                    builder.OpenElement(7, "path");
                    builder.AddAttribute(8, "stroke-linecap", "round");
                    builder.AddAttribute(9, "stroke-linejoin", "round");
                    builder.AddAttribute(10, "stroke-width", "2");
                    builder.AddAttribute(11, "d", "M13 10V3L4 14h7v7l9-11h-7z");
                    builder.CloseElement();
                    builder.CloseElement();
                }
            };
        }

        // Bootstrap helper methods
        protected string GetStatusAlertClass()
        {
            if (Results == null) return "alert-light";

            return Results.AirflowStatus switch
            {
                "OPTIMAL AIRFLOW" => "alert-success",
                "LOW AIRFLOW" => "alert-danger",
                "HIGH AIRFLOW" => "alert-warning",
                _ => "alert-light"
            };
        }

        protected string GetStatusColorClass()
        {
            if (Results == null) return "";

            return Results.AirflowStatus switch
            {
                "OPTIMAL AIRFLOW" => "text-success",
                "LOW AIRFLOW" => "text-danger",
                "HIGH AIRFLOW" => "text-warning",
                _ => ""
            };
        }

        protected string GetStaticPressureColorClass()
        {
            var guide = GetStaticPressureGuidelines();

            if (Inputs.StaticPressure < guide.Low) return "text-warning";
            if (Inputs.StaticPressure > guide.Excessive) return "text-danger";
            if (Inputs.StaticPressure > guide.High) return "text-warning";
            return "text-success";
        }

        protected string GetStaticPressureStatus()
        {
            var guide = GetStaticPressureGuidelines();

            if (Inputs.StaticPressure < guide.Low) return "Low";
            if (Inputs.StaticPressure > guide.Excessive) return "Excessive";
            if (Inputs.StaticPressure > guide.High) return "High";
            return "Normal";
        }

        private Dictionary<string, SystemTypeProperties> GetSystemTypeProperties()
        {
            return new Dictionary<string, SystemTypeProperties>
            {
                { "Air Conditioning", new SystemTypeProperties
                    {
                        IdealCFMPerTon = 400,
                        MinCFMPerTon = 350,
                        MaxCFMPerTon = 450,
                        IdealTemperatureDiff = 20,
                        Description = "Standard cooling system"
                    }
                },
                { "Heat Pump", new SystemTypeProperties
                    {
                        IdealCFMPerTon = 400,
                        MinCFMPerTon = 350,
                        MaxCFMPerTon = 450,
                        IdealTemperatureDiff = 20,
                        Description = "Heat pump system"
                    }
                },
                { "Furnace", new SystemTypeProperties
                    {
                        IdealCFMPerTon = 350,
                        MinCFMPerTon = 300,
                        MaxCFMPerTon = 400,
                        IdealTemperatureDiff = 40,
                        Description = "Forced air heating system"
                    }
                },
                { "Package Unit", new SystemTypeProperties
                    {
                        IdealCFMPerTon = 400,
                        MinCFMPerTon = 350,
                        MaxCFMPerTon = 450,
                        IdealTemperatureDiff = 20,
                        Description = "Self-contained unit"
                    }
                }
            };
        }

        private StaticPressureGuidelines GetStaticPressureGuidelines()
        {
            return new StaticPressureGuidelines
            {
                Low = 0.3,
                Normal = 0.5,
                High = 0.8,
                Excessive = 1.0
            };
        }
    }
}