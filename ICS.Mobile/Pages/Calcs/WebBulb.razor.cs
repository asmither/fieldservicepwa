using static ICS.Mobile.DataModels.Custom.HVACCalculations;


namespace ICS.Mobile.Pages.WetBulb
{
    public partial class WetBulbCalcPage : PageBase
    {
        protected WetBulbInputs Inputs { get; set; } = new();
        protected WetBulbResults? Results { get; set; }

        protected override void OnInitialized()
        {
            // Initialize with typical commercial scenario
            Inputs = new WetBulbInputs
            {
                CalculationMode = "Wet Bulb to Enthalpy",
                InputWetBulb = 67.5, // Common design wet bulb
                InputEnthalpy = 30.0,
                MeasurementLocation = "Return Air",
                DryBulbTemp = 75.0,
                RelativeHumidity = 50.0,
                Altitude = 0,
                EquipmentType = "Rooftop Unit",
                SystemCFM = 0,
                MeasurementNotes = string.Empty
            };
        }

        protected void OnInputChanged()
        {
            // Auto-calculate when valid inputs are entered
            if (ShouldAutoCalculate())
            {
                PerformCalculation();
            }
            else
            {
                Results = null;
                StateHasChanged();
            }
        }

        public void HomePageClick()
        {
            PageNavManager.NavigateTo("/calcmenu");
        }

        protected void PerformCalculation()
        {
            try
            {
                var wetBulbData = GetVerifiedWetBulbData();

                if (Inputs.CalculationMode == "Wet Bulb to Enthalpy")
                {
                    Results = PerformWetBulbToEnthalpyCalculation(wetBulbData);
                }
                else
                {
                    Results = PerformEnthalpyToWetBulbCalculation(wetBulbData);
                }

                // Add psychrometric calculations if dry bulb is provided
                if (Results.IsValidCalculation && Inputs.DryBulbTemp.HasValue)
                {
                    CalculateAdditionalProperties();
                }

                // Calculate system capacity if CFM is provided
                if (Results.IsValidCalculation && Inputs.SystemCFM > 0)
                {
                    CalculateSystemCapacity();
                }

                StateHasChanged();
            }
            catch (Exception ex)
            {
                Results = CreateErrorResult($"Calculation error: {ex.Message}");
                StateHasChanged();
            }
        }

        #region Calculation Methods

        private WetBulbResults PerformWetBulbToEnthalpyCalculation(List<WetBulbDataPoint> dataPoints)
        {
            var inputWetBulb = Inputs.InputWetBulb;

            // Find enthalpy using interpolation
            var (enthalpy, dataQuality, error) = InterpolateEnthalpyFromWetBulb(dataPoints, inputWetBulb);

            if (double.IsNaN(enthalpy))
            {
                return CreateErrorResult("Wet bulb temperature outside valid range (35°F - 85°F)");
            }

            // Calculate component heat values
            var sensibleHeat = CalculateSensibleHeat(inputWetBulb);
            var latentHeat = enthalpy - sensibleHeat;

            return new WetBulbResults
            {
                ResultEnthalpy = enthalpy,
                ResultWetBulb = inputWetBulb,
                IsValidCalculation = true,
                CalculationMethod = "Wet Bulb to Enthalpy Interpolation",
                SensibleHeat = sensibleHeat,
                LatentHeat = latentHeat,
                MoistureContent = CalculateMoistureContent(inputWetBulb),
                OperatingNotes = GenerateOperatingNotes(inputWetBulb, enthalpy),
                DataQuality = dataQuality,
                InterpolationError = error,
                AirDensity = CalculateAirDensity(inputWetBulb, Inputs.Altitude ?? 0)
            };
        }

        private WetBulbResults PerformEnthalpyToWetBulbCalculation(List<WetBulbDataPoint> dataPoints)
        {
            var inputEnthalpy = Inputs.InputEnthalpy;

            // Find wet bulb using interpolation
            var (wetBulb, dataQuality, error) = InterpolateWetBulbFromEnthalpy(dataPoints, inputEnthalpy);

            if (double.IsNaN(wetBulb))
            {
                return CreateErrorResult("Enthalpy outside valid range (13 - 44 BTU/lb)");
            }

            // Calculate component heat values
            var sensibleHeat = CalculateSensibleHeat(wetBulb);
            var latentHeat = inputEnthalpy - sensibleHeat;

            return new WetBulbResults
            {
                ResultEnthalpy = inputEnthalpy,
                ResultWetBulb = wetBulb,
                IsValidCalculation = true,
                CalculationMethod = "Enthalpy to Wet Bulb Interpolation",
                SensibleHeat = sensibleHeat,
                LatentHeat = latentHeat,
                MoistureContent = CalculateMoistureContent(wetBulb),
                OperatingNotes = GenerateOperatingNotes(wetBulb, inputEnthalpy),
                DataQuality = dataQuality,
                InterpolationError = error * 0.1, // Convert to temperature error
                AirDensity = CalculateAirDensity(wetBulb, Inputs.Altitude ?? 0)
            };
        }

        private (double result, string quality, double error) InterpolateEnthalpyFromWetBulb(
            List<WetBulbDataPoint> dataPoints, double wetBulbTemp)
        {
            // Sort data points by wet bulb temperature
            var sortedPoints = dataPoints.OrderBy(p => p.WetBulbTempF).ToList();

            // Check if exact match exists
            var exactMatch = sortedPoints.FirstOrDefault(p => Math.Abs(p.WetBulbTempF - wetBulbTemp) < 0.05);
            if (exactMatch != null)
            {
                return (exactMatch.Enthalpy, "Exact", 0.0);
            }

            // Check for extrapolation cases
            if (wetBulbTemp < sortedPoints.First().WetBulbTempF)
            {
                // Extrapolate below range
                var p1 = sortedPoints[0];
                var p2 = sortedPoints[1];
                var slope = (p2.Enthalpy - p1.Enthalpy) / (p2.WetBulbTempF - p1.WetBulbTempF);
                var enthalpy = p1.Enthalpy + slope * (wetBulbTemp - p1.WetBulbTempF);
                var error = Math.Abs(wetBulbTemp - p1.WetBulbTempF) * 0.05;
                return (enthalpy, "Extrapolated (Low)", error);
            }

            if (wetBulbTemp > sortedPoints.Last().WetBulbTempF)
            {
                // Extrapolate above range
                var p1 = sortedPoints[sortedPoints.Count - 2];
                var p2 = sortedPoints[sortedPoints.Count - 1];
                var slope = (p2.Enthalpy - p1.Enthalpy) / (p2.WetBulbTempF - p1.WetBulbTempF);
                var enthalpy = p2.Enthalpy + slope * (wetBulbTemp - p2.WetBulbTempF);
                var error = Math.Abs(wetBulbTemp - p2.WetBulbTempF) * 0.05;
                return (enthalpy, "Extrapolated (High)", error);
            }

            // Linear interpolation between two points
            for (int i = 0; i < sortedPoints.Count - 1; i++)
            {
                var p1 = sortedPoints[i];
                var p2 = sortedPoints[i + 1];

                if (wetBulbTemp >= p1.WetBulbTempF && wetBulbTemp <= p2.WetBulbTempF)
                {
                    var ratio = (wetBulbTemp - p1.WetBulbTempF) / (p2.WetBulbTempF - p1.WetBulbTempF);
                    var enthalpy = p1.Enthalpy + ratio * (p2.Enthalpy - p1.Enthalpy);
                    var error = Math.Abs(p2.Enthalpy - p1.Enthalpy) * 0.01; // 1% of enthalpy span
                    return (enthalpy, "Interpolated", error);
                }
            }

            return (double.NaN, "Error", 0.0);
        }

        private (double result, string quality, double error) InterpolateWetBulbFromEnthalpy(
            List<WetBulbDataPoint> dataPoints, double enthalpy)
        {
            // Sort data points by enthalpy
            var sortedPoints = dataPoints.OrderBy(p => p.Enthalpy).ToList();

            // Check if exact match exists
            var exactMatch = sortedPoints.FirstOrDefault(p => Math.Abs(p.Enthalpy - enthalpy) < 0.01);
            if (exactMatch != null)
            {
                return (exactMatch.WetBulbTempF, "Exact", 0.0);
            }

            // Check for extrapolation cases
            if (enthalpy < sortedPoints.First().Enthalpy)
            {
                // Extrapolate below range
                var p1 = sortedPoints[0];
                var p2 = sortedPoints[1];
                var slope = (p2.WetBulbTempF - p1.WetBulbTempF) / (p2.Enthalpy - p1.Enthalpy);
                var wetBulb = p1.WetBulbTempF + slope * (enthalpy - p1.Enthalpy);
                var error = Math.Abs(enthalpy - p1.Enthalpy) * 0.5;
                return (wetBulb, "Extrapolated (Low)", error);
            }

            if (enthalpy > sortedPoints.Last().Enthalpy)
            {
                // Extrapolate above range
                var p1 = sortedPoints[sortedPoints.Count - 2];
                var p2 = sortedPoints[sortedPoints.Count - 1];
                var slope = (p2.WetBulbTempF - p1.WetBulbTempF) / (p2.Enthalpy - p1.Enthalpy);
                var wetBulb = p2.WetBulbTempF + slope * (enthalpy - p2.Enthalpy);
                var error = Math.Abs(enthalpy - p2.Enthalpy) * 0.5;
                return (wetBulb, "Extrapolated (High)", error);
            }

            // Linear interpolation between two points
            for (int i = 0; i < sortedPoints.Count - 1; i++)
            {
                var p1 = sortedPoints[i];
                var p2 = sortedPoints[i + 1];

                if (enthalpy >= p1.Enthalpy && enthalpy <= p2.Enthalpy)
                {
                    var ratio = (enthalpy - p1.Enthalpy) / (p2.Enthalpy - p1.Enthalpy);
                    var wetBulb = p1.WetBulbTempF + ratio * (p2.WetBulbTempF - p1.WetBulbTempF);
                    var error = Math.Abs(p2.WetBulbTempF - p1.WetBulbTempF) * 0.01; // 1% of temperature span
                    return (wetBulb, "Interpolated", error);
                }
            }

            return (double.NaN, "Error", 0.0);
        }

        #endregion

        #region Psychrometric Calculations

        private void CalculateAdditionalProperties()
        {
            if (Results == null || !Inputs.DryBulbTemp.HasValue) return;

            var dryBulb = Inputs.DryBulbTemp.Value;
            var wetBulb = Results.ResultWetBulb;

            // Calculate dew point temperature
            Results.DewPointTemp = CalculateDewPoint(dryBulb, wetBulb);

            // Calculate humidity ratio
            Results.HumidityRatio = CalculateHumidityRatio(wetBulb, Inputs.Altitude ?? 0);

            // Calculate specific volume
            Results.SpecificVolume = CalculateSpecificVolume(dryBulb, Results.HumidityRatio.Value, Inputs.Altitude ?? 0);

            // Update moisture content in grains
            Results.MoistureContent = Results.HumidityRatio.Value * 7000; // Convert lb/lb to grains/lb
        }

        private void CalculateSystemCapacity()
        {
            if (Results == null || Inputs.SystemCFM <= 0) return;

            var cfm = Inputs.SystemCFM;
            var airDensity = Results.AirDensity;

            // Calculate mass flow rate (lb/min)
            var massFlow = cfm * airDensity;

            // For cooling capacity calculation, we need entering and leaving conditions
            // This is a simplified calculation assuming standard conditions
            if (Inputs.MeasurementLocation == "Cooling Coil Entering" ||
                Inputs.MeasurementLocation == "Mixed Air")
            {
                // Assume typical leaving conditions
                var leavingEnthalpy = 23.0; // Typical 55°F saturated air
                var deltaH = Results.ResultEnthalpy - leavingEnthalpy;

                if (deltaH > 0)
                {
                    Results.TotalCoolingCapacity = massFlow * deltaH * 60; // BTU/hr

                    // Calculate sensible heat ratio (approximate)
                    var sensibleDelta = 1.08 * cfm * 20; // Assuming 20°F delta T
                    Results.SensibleHeatRatio = Math.Min(sensibleDelta / Results.TotalCoolingCapacity, 1.0);
                }
            }
        }

        private double CalculateSensibleHeat(double wetBulbTemp)
        {
            // Simplified calculation for sensible heat component
            // Assumes standard atmospheric pressure
            return 0.24 * wetBulbTemp; // BTU/lb
        }

        private double CalculateMoistureContent(double wetBulbTemp)
        {
            // Simplified correlation for moisture content in grains/lb
            // Based on typical psychrometric relationships
            return 7.5 * Math.Pow(1.8 * wetBulbTemp / 100, 2.5);
        }

        private double CalculateAirDensity(double wetBulbTemp, double altitude)
        {
            // Calculate air density considering altitude
            var standardDensity = 0.075; // lb/ft³ at sea level
            var altitudeFactor = Math.Pow((1 - 0.0000068756 * altitude), 5.2559);
            var temperatureFactor = 530 / (wetBulbTemp + 460); // Rankine temperature

            return standardDensity * altitudeFactor * temperatureFactor;
        }

        private double CalculateDewPoint(double dryBulb, double wetBulb)
        {
            // Simplified dew point calculation
            var depression = dryBulb - wetBulb;
            return wetBulb - (depression * 0.36);
        }

        private double CalculateHumidityRatio(double wetBulbTemp, double altitude)
        {
            // Simplified humidity ratio calculation
            var saturationPressure = 0.01 * Math.Exp(17.269 * wetBulbTemp / (237.3 + wetBulbTemp));
            var atmosphericPressure = 14.696 * Math.Pow((1 - 0.0000068756 * altitude), 5.2559);

            return 0.622 * saturationPressure / (atmosphericPressure - saturationPressure);
        }

        private double CalculateSpecificVolume(double dryBulb, double humidityRatio, double altitude)
        {
            // Calculate specific volume in ft³/lb dry air
            var atmosphericPressure = 14.696 * Math.Pow((1 - 0.0000068756 * altitude), 5.2559);
            var rankineTemp = dryBulb + 459.67;

            return 0.370486 * rankineTemp * (1 + 1.6078 * humidityRatio) / atmosphericPressure;
        }

        #endregion

        #region Data and Helper Methods

        private List<WetBulbDataPoint> GetVerifiedWetBulbData()
        {
            // Full dataset with 510 points from 35°F to 85°F in 0.1°F increments
            var dataPoints = new List<WetBulbDataPoint>();

            // Generate full dataset programmatically based on psychrometric relationships
            for (double wetBulb = 35.0; wetBulb <= 85.0; wetBulb += 0.1)
            {
                // Calculate enthalpy using standard psychrometric equation
                // h = 0.24*T + W*(1061 + 0.444*T) where W is humidity ratio at saturation
                var saturationPressure = CalculateSaturationPressure(wetBulb);
                var humidityRatio = 0.622 * saturationPressure / (14.696 - saturationPressure);
                var enthalpy = 0.24 * wetBulb + humidityRatio * (1061 + 0.444 * wetBulb);

                dataPoints.Add(new WetBulbDataPoint
                {
                    WetBulbTempF = Math.Round(wetBulb, 1),
                    Enthalpy = Math.Round(enthalpy, 2)
                });
            }

            return dataPoints;
        }

        private double CalculateSaturationPressure(double temperature)
        {
            // Antoine equation for water vapor pressure
            var celsius = (temperature - 32) * 5 / 9;
            return 0.08859 * Math.Exp(17.269 * celsius / (celsius + 237.3));
        }

        private List<string> GenerateOperatingNotes(double wetBulb, double enthalpy)
        {
            var notes = new List<string>();

            // Wet bulb temperature analysis
            if (wetBulb < 50)
                notes.Add("Very low wet bulb - check for over-dehumidification or low load conditions");
            else if (wetBulb < 55)
                notes.Add("Low wet bulb - good dehumidification, verify comfort");
            else if (wetBulb > 70)
                notes.Add("High wet bulb - potential comfort issues, check cooling capacity");
            else if (wetBulb > 75)
                notes.Add("Very high wet bulb - excessive moisture load or undersized equipment");

            // Enthalpy analysis
            if (enthalpy < 20)
                notes.Add("Low enthalpy - winter conditions or aggressive cooling");
            else if (enthalpy > 35)
                notes.Add("High enthalpy - significant cooling load required");
            else if (enthalpy > 40)
                notes.Add("Very high enthalpy - extreme conditions, verify equipment capacity");

            // Location-specific notes
            if (Inputs.MeasurementLocation == "Return Air" && wetBulb > 67)
                notes.Add("High return air moisture - check for infiltration or internal loads");
            else if (Inputs.MeasurementLocation == "Supply Air" && wetBulb > 58)
                notes.Add("High supply wet bulb - coil may need cleaning or refrigerant check");
            else if (Inputs.MeasurementLocation == "Outside Air" && enthalpy > 38)
                notes.Add("High outdoor enthalpy - consider economizer lockout");

            return notes;
        }

        private WetBulbResults CreateErrorResult(string errorMessage)
        {
            return new WetBulbResults
            {
                IsValidCalculation = false,
                ErrorMessage = errorMessage,
                DataQuality = "Error"
            };
        }

        private bool ShouldAutoCalculate()
        {
            if (Inputs.CalculationMode == "Wet Bulb to Enthalpy")
            {
                return Inputs.InputWetBulb >= 30 && Inputs.InputWetBulb <= 90;
            }
            else
            {
                return Inputs.InputEnthalpy >= 10 && Inputs.InputEnthalpy <= 50;
            }
        }

        #endregion

        #region UI Helper Methods

        protected string GetDataQualityClass()
        {
            if (Results == null) return "text-secondary";

            return Results.DataQuality switch
            {
                "Exact" => "text-success",
                "Interpolated" => "text-primary",
                "Extrapolated (Low)" or "Extrapolated (High)" => "text-warning",
                "Error" => "text-danger",
                _ => "text-secondary"
            };
        }

        protected string GetLocationAdvice()
        {
            if (Results == null || !Results.IsValidCalculation) return string.Empty;

            var advice = Inputs.MeasurementLocation switch
            {
                "Return Air" => "Monitor space conditions and occupant comfort. Target 62-67°F WB for comfort.",
                "Outside Air" => "Consider economizer operation when OA enthalpy is below return air.",
                "Mixed Air" => "Verify damper operation and mixing effectiveness.",
                "Supply Air" => "Typical supply should be 55-58°F WB for proper dehumidification.",
                "Cooling Coil Entering" => "Check filter condition and return air mixing.",
                "Cooling Coil Leaving" => "Verify coil performance and refrigerant charge.",
                "Space Condition" => "Ensure proper air distribution and avoid stratification.",
                _ => "Document measurement conditions for trending."
            };

            return advice;
        }

        #endregion
    }
    
    #region Data Models

    
    #endregion
}