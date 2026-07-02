using static ICS.Mobile.DataModels.Custom.HVACCalculations;


namespace ICS.Mobile.Pages.RPTChart
{
    public partial class RPTCalcPage : PageBase
    {
        protected RPTInputs Inputs { get; set; } = new();
        protected RPTResults? Results { get; set; }

        protected override void OnInitialized()
        {
            // Initialize with common commercial scenario
            Inputs = new RPTInputs
            {
                RefrigerantType = "R-410A",
                InputPressure = 278, // Common condensing pressure
                PressureUnit = "PSIG",
                LookupMode = "Pressure to Temperature",
                InputTemperature = 40,
                SystemType = "Rooftop Unit",
                SystemCapacity = 180000, // 15 ton
                ApplicationNotes = string.Empty
            };
        }

        protected void OnInputChanged()
        {
            // Auto-lookup when valid inputs are entered
            if (ShouldAutoLookup())
            {
                PerformLookup();
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
        protected void PerformLookup()
        {
            try
            {
                var refrigerantData = GetVerifiedRefrigerantData();

                if (!refrigerantData.ContainsKey(Inputs.RefrigerantType))
                {
                    Results = CreateErrorResult("Refrigerant data not available");
                    StateHasChanged();
                    return;
                }

                var dataPoints = refrigerantData[Inputs.RefrigerantType];
                var refrigerantInfo = GetRefrigerantInfo(Inputs.RefrigerantType);

                if (Inputs.LookupMode == "Pressure to Temperature")
                {
                    Results = PerformPressureToTemperatureLookup(dataPoints, refrigerantInfo);
                }
                else
                {
                    Results = PerformTemperatureToPressureLookup(dataPoints, refrigerantInfo);
                }

                StateHasChanged();
            }
            catch (Exception ex)
            {
                Results = CreateErrorResult($"Lookup error: {ex.Message}");
                StateHasChanged();
            }
        }

        #region Lookup Methods

        private RPTResults PerformPressureToTemperatureLookup(List<RPTDataPoint> dataPoints, RefrigerantInfo refInfo)
        {
            var inputPressure = ConvertToGaugePressure(Inputs.InputPressure, Inputs.PressureUnit);

            // Find temperature using interpolation
            var (temperature, dataQuality, error) = InterpolateTemperatureFromPressure(dataPoints, inputPressure);

            if (double.IsNaN(temperature))
            {
                return CreateErrorResult("Input pressure outside refrigerant operating range");
            }

            return new RPTResults
            {
                ResultTemperature = temperature,
                ResultPressure = inputPressure,
                RefrigerantName = refInfo.FullName,
                RefrigerantType = Inputs.RefrigerantType,
                IsValidLookup = true,
                LookupMethod = "Pressure to Temperature Interpolation",
                RefrigerantProperties = refInfo.Notes,
                SafetyClassification = refInfo.SafetyClass,
                TypicalApplications = refInfo.TypicalUse,
                OperatingNotes = GenerateOperatingNotes(temperature, inputPressure, refInfo),
                PressurePSIA = inputPressure + 14.7,
                DataQuality = dataQuality,
                InterpolationError = error
            };
        }

        private RPTResults PerformTemperatureToPressureLookup(List<RPTDataPoint> dataPoints, RefrigerantInfo refInfo)
        {
            var inputTemperature = Inputs.InputTemperature;

            // Find pressure using interpolation
            var (pressure, dataQuality, error) = InterpolatePressureFromTemperature(dataPoints, inputTemperature);

            if (double.IsNaN(pressure))
            {
                return CreateErrorResult("Input temperature outside refrigerant operating range");
            }

            return new RPTResults
            {
                ResultTemperature = inputTemperature,
                ResultPressure = pressure,
                RefrigerantName = refInfo.FullName,
                RefrigerantType = Inputs.RefrigerantType,
                IsValidLookup = true,
                LookupMethod = "Temperature to Pressure Interpolation",
                RefrigerantProperties = refInfo.Notes,
                SafetyClassification = refInfo.SafetyClass,
                TypicalApplications = refInfo.TypicalUse,
                OperatingNotes = GenerateOperatingNotes(inputTemperature, pressure, refInfo),
                PressurePSIA = pressure + 14.7,
                DataQuality = dataQuality,
                InterpolationError = error
            };
        }

        private (double result, string quality, double error) InterpolateTemperatureFromPressure(List<RPTDataPoint> dataPoints, double pressurePSIG)
        {
            // Sort data points by pressure
            var sortedPoints = dataPoints.OrderBy(p => p.PressurePSIG).ToList();

            // Check if exact match exists
            var exactMatch = sortedPoints.FirstOrDefault(p => Math.Abs(p.PressurePSIG - pressurePSIG) < 0.1);
            if (exactMatch != null)
            {
                return (exactMatch.TempF, "Exact", 0.0);
            }

            // Check for extrapolation cases
            if (pressurePSIG < sortedPoints.First().PressurePSIG)
            {
                // Extrapolate below range
                var p1 = sortedPoints[0];
                var p2 = sortedPoints[1];
                var slope = (p2.TempF - p1.TempF) / (p2.PressurePSIG - p1.PressurePSIG);
                var temperature = p1.TempF + slope * (pressurePSIG - p1.PressurePSIG);
                return (temperature, "Extrapolated (Low)", Math.Abs(pressurePSIG - p1.PressurePSIG) * 0.1);
            }

            if (pressurePSIG > sortedPoints.Last().PressurePSIG)
            {
                // Extrapolate above range
                var p1 = sortedPoints[sortedPoints.Count - 2];
                var p2 = sortedPoints[sortedPoints.Count - 1];
                var slope = (p2.TempF - p1.TempF) / (p2.PressurePSIG - p1.PressurePSIG);
                var temperature = p2.TempF + slope * (pressurePSIG - p2.PressurePSIG);
                return (temperature, "Extrapolated (High)", Math.Abs(pressurePSIG - p2.PressurePSIG) * 0.1);
            }

            // Linear interpolation between two points
            for (int i = 0; i < sortedPoints.Count - 1; i++)
            {
                var p1 = sortedPoints[i];
                var p2 = sortedPoints[i + 1];

                if (pressurePSIG >= p1.PressurePSIG && pressurePSIG <= p2.PressurePSIG)
                {
                    var ratio = (pressurePSIG - p1.PressurePSIG) / (p2.PressurePSIG - p1.PressurePSIG);
                    var temperature = p1.TempF + ratio * (p2.TempF - p1.TempF);
                    var error = Math.Abs(p2.TempF - p1.TempF) * 0.01; // 1% of temperature span
                    return (temperature, "Interpolated", error);
                }
            }

            return (double.NaN, "Error", 0.0);
        }

        private (double result, string quality, double error) InterpolatePressureFromTemperature(List<RPTDataPoint> dataPoints, double temperatureF)
        {
            // Sort data points by temperature
            var sortedPoints = dataPoints.OrderBy(p => p.TempF).ToList();

            // Check if exact match exists
            var exactMatch = sortedPoints.FirstOrDefault(p => Math.Abs(p.TempF - temperatureF) < 0.1);
            if (exactMatch != null)
            {
                return (exactMatch.PressurePSIG, "Exact", 0.0);
            }

            // Check for extrapolation cases
            if (temperatureF < sortedPoints.First().TempF)
            {
                // Extrapolate below range
                var p1 = sortedPoints[0];
                var p2 = sortedPoints[1];
                var slope = (p2.PressurePSIG - p1.PressurePSIG) / (p2.TempF - p1.TempF);
                var pressure = p1.PressurePSIG + slope * (temperatureF - p1.TempF);
                return (pressure, "Extrapolated (Low)", Math.Abs(temperatureF - p1.TempF) * 0.5);
            }

            if (temperatureF > sortedPoints.Last().TempF)
            {
                // Extrapolate above range
                var p1 = sortedPoints[sortedPoints.Count - 2];
                var p2 = sortedPoints[sortedPoints.Count - 1];
                var slope = (p2.PressurePSIG - p1.PressurePSIG) / (p2.TempF - p1.TempF);
                var pressure = p2.PressurePSIG + slope * (temperatureF - p2.TempF);
                return (pressure, "Extrapolated (High)", Math.Abs(temperatureF - p2.TempF) * 0.5);
            }

            // Linear interpolation between two points
            for (int i = 0; i < sortedPoints.Count - 1; i++)
            {
                var p1 = sortedPoints[i];
                var p2 = sortedPoints[i + 1];

                if (temperatureF >= p1.TempF && temperatureF <= p2.TempF)
                {
                    var ratio = (temperatureF - p1.TempF) / (p2.TempF - p1.TempF);
                    var pressure = p1.PressurePSIG + ratio * (p2.PressurePSIG - p1.PressurePSIG);
                    var error = Math.Abs(p2.PressurePSIG - p1.PressurePSIG) * 0.01; // 1% of pressure span
                    return (pressure, "Interpolated", error);
                }
            }

            return (double.NaN, "Error", 0.0);
        }

        #endregion

        #region Data and Helper Methods

        private Dictionary<string, List<RPTDataPoint>> GetVerifiedRefrigerantData()
        {
            return new Dictionary<string, List<RPTDataPoint>>
            {
                ["R-134A"] = new List<RPTDataPoint>
                    {
                    new() { TempF = -48, PressurePSIG = 1.6 },
                    new() { TempF = -44, PressurePSIG = 1.1 },
                    new() { TempF = -40, PressurePSIG = 3.3 },
                    new() { TempF = -36, PressurePSIG = 5.6 },
                    new() { TempF = -32, PressurePSIG = 8.2 },
                    new() { TempF = -28, PressurePSIG = 11.1 },
                    new() { TempF = -24, PressurePSIG = 14.1 },
                    new() { TempF = -20, PressurePSIG = 17.5 },
                    new() { TempF = -16, PressurePSIG = 21.2 },
                    new() { TempF = -12, PressurePSIG = 25.2 },
                    new() { TempF = -8, PressurePSIG = 29.5 },
                    new() { TempF = -4, PressurePSIG = 34.2 },
                    new() { TempF = 0, PressurePSIG = 39.1 },
                    new() { TempF = 4, PressurePSIG = 44.6 },
                    new() { TempF = 8, PressurePSIG = 50.3 },
                    new() { TempF = 12, PressurePSIG = 56.6 },
                    new() { TempF = 16, PressurePSIG = 63.2 },
                    new() { TempF = 20, PressurePSIG = 70.4 },
                    new() { TempF = 24, PressurePSIG = 78.1 },
                    new() { TempF = 28, PressurePSIG = 86.4 },
                    new() { TempF = 32, PressurePSIG = 95.3 },
                    new() { TempF = 36, PressurePSIG = 104.9 },
                    new() { TempF = 40, PressurePSIG = 115.1 },
                    new() { TempF = 44, PressurePSIG = 126.0 },
                    new() { TempF = 48, PressurePSIG = 137.6 },
                    new() { TempF = 52, PressurePSIG = 150.0 },
                    new() { TempF = 56, PressurePSIG = 163.2 },
                    new() { TempF = 60, PressurePSIG = 177.3 },
                    new() { TempF = 64, PressurePSIG = 192.1 },
                    new() { TempF = 68, PressurePSIG = 208.0 },
                    new() { TempF = 72, PressurePSIG = 224.7 },
                    new() { TempF = 76, PressurePSIG = 242.6 },
                    new() { TempF = 80, PressurePSIG = 261.6 },
                    new() { TempF = 84, PressurePSIG = 281.7 },
                    new() { TempF = 88, PressurePSIG = 303.2 },
                    new() { TempF = 92, PressurePSIG = 326.0 },
                    new() { TempF = 96, PressurePSIG = 350.0 },
                    new() { TempF = 100, PressurePSIG = 375.8 },
                    new() { TempF = 104, PressurePSIG = 402.5 },
                    new() { TempF = 108, PressurePSIG = 431.4 },
                    new() { TempF = 112, PressurePSIG = 461.1 },
                    new() { TempF = 116, PressurePSIG = 493.2 },
                    new() { TempF = 120, PressurePSIG = 526.3 },
                    new() { TempF = 124, PressurePSIG = 562.1 },
                    new() { TempF = 128, PressurePSIG = 599.5 },
                    new() { TempF = 132, PressurePSIG = 639.5 },
                    new() { TempF = 136, PressurePSIG = 681.9 },
                    new() { TempF = 140, PressurePSIG = 727.0 },
                    new() { TempF = 144, PressurePSIG = 775.0 },
                    new() { TempF = 148, PressurePSIG = 423 }
                    },

                ["R-410A"] = new List<RPTDataPoint>
                    {
                    new() { TempF = -48, PressurePSIG = 6 },
                    new() { TempF = -44, PressurePSIG = 8.3 },
                    new() { TempF = -40, PressurePSIG = 10.8 },
                    new() { TempF = -36, PressurePSIG = 13.4 },
                    new() { TempF = -32, PressurePSIG = 16.3 },
                    new() { TempF = -28, PressurePSIG = 19.4 },
                    new() { TempF = -24, PressurePSIG = 22.7 },
                    new() { TempF = -20, PressurePSIG = 26.3 },
                    new() { TempF = -16, PressurePSIG = 30.2 },
                    new() { TempF = -12, PressurePSIG = 34.3 },
                    new() { TempF = -8, PressurePSIG = 38.8 },
                    new() { TempF = -4, PressurePSIG = 43.6 },
                    new() { TempF = 0, PressurePSIG = 48.8 },
                    new() { TempF = 4, PressurePSIG = 54.3 },
                    new() { TempF = 8, PressurePSIG = 60.4 },
                    new() { TempF = 12, PressurePSIG = 66.9 },
                    new() { TempF = 16, PressurePSIG = 73.9 },
                    new() { TempF = 20, PressurePSIG = 81.4 },
                    new() { TempF = 24, PressurePSIG = 89.5 },
                    new() { TempF = 28, PressurePSIG = 98.2 },
                    new() { TempF = 32, PressurePSIG = 107.5 },
                    new() { TempF = 36, PressurePSIG = 117.5 },
                    new() { TempF = 40, PressurePSIG = 128.2 },
                    new() { TempF = 44, PressurePSIG = 139.6 },
                    new() { TempF = 48, PressurePSIG = 151.8 },
                    new() { TempF = 52, PressurePSIG = 164.8 },
                    new() { TempF = 56, PressurePSIG = 178.7 },
                    new() { TempF = 60, PressurePSIG = 193.5 },
                    new() { TempF = 64, PressurePSIG = 209.2 },
                    new() { TempF = 68, PressurePSIG = 225.9 },
                    new() { TempF = 72, PressurePSIG = 243.7 },
                    new() { TempF = 76, PressurePSIG = 262.6 },
                    new() { TempF = 80, PressurePSIG = 282.7 },
                    new() { TempF = 84, PressurePSIG = 304.0 },
                    new() { TempF = 88, PressurePSIG = 326.6 },
                    new() { TempF = 92, PressurePSIG = 350.6 },
                    new() { TempF = 96, PressurePSIG = 376 },
                    new() { TempF = 100, PressurePSIG = 402.9 },
                    new() { TempF = 104, PressurePSIG = 431.2 },
                    new() { TempF = 108, PressurePSIG = 461.4 },
                    new() { TempF = 112, PressurePSIG = 492.9 },
                    new() { TempF = 116, PressurePSIG = 526.5 },
                    new() { TempF = 120, PressurePSIG = 561.8 },
                    new() { TempF = 124, PressurePSIG = 599.2 },
                    new() { TempF = 128, PressurePSIG = 638.5 },
                    new() { TempF = 132, PressurePSIG = 680.1 },
                    new() { TempF = 136, PressurePSIG = 723.9 },
                    new() { TempF = 140, PressurePSIG = 770.2 },
                    new() { TempF = 144, PressurePSIG = 819.1 },
                    new() { TempF = 148, PressurePSIG = 599 }
                    },

                ["R-32"] = new List<RPTDataPoint>
                    {
                    new() { TempF = -48, PressurePSIG = 6.2 },
                    new() { TempF = -44, PressurePSIG = 8.5 },
                    new() { TempF = -40, PressurePSIG = 11 },
                    new() { TempF = -36, PressurePSIG = 13.7 },
                    new() { TempF = -32, PressurePSIG = 16.6 },
                    new() { TempF = -28, PressurePSIG = 19.8 },
                    new() { TempF = -24, PressurePSIG = 23.2 },
                    new() { TempF = -20, PressurePSIG = 26.8 },
                    new() { TempF = -16, PressurePSIG = 30.7 },
                    new() { TempF = -12, PressurePSIG = 34.9 },
                    new() { TempF = -8, PressurePSIG = 39.4 },
                    new() { TempF = -4, PressurePSIG = 44.2 },
                    new() { TempF = 0, PressurePSIG = 49.3 },
                    new() { TempF = 4, PressurePSIG = 54.8 },
                    new() { TempF = 8, PressurePSIG = 60.7 },
                    new() { TempF = 12, PressurePSIG = 67 },
                    new() { TempF = 16, PressurePSIG = 73.6 },
                    new() { TempF = 20, PressurePSIG = 80.8 },
                    new() { TempF = 24, PressurePSIG = 88.3 },
                    new() { TempF = 28, PressurePSIG = 96.4 },
                    new() { TempF = 32, PressurePSIG = 105 },
                    new() { TempF = 36, PressurePSIG = 114.1 },
                    new() { TempF = 40, PressurePSIG = 123.8 },
                    new() { TempF = 44, PressurePSIG = 134.1 },
                    new() { TempF = 48, PressurePSIG = 145 },
                    new() { TempF = 52, PressurePSIG = 156.5 },
                    new() { TempF = 56, PressurePSIG = 168.8 },
                    new() { TempF = 60, PressurePSIG = 181.7 },
                    new() { TempF = 64, PressurePSIG = 195.5 },
                    new() { TempF = 68, PressurePSIG = 210 },
                    new() { TempF = 72, PressurePSIG = 225.3 },
                    new() { TempF = 76, PressurePSIG = 241.5 },
                    new() { TempF = 80, PressurePSIG = 258.6 },
                    new() { TempF = 84, PressurePSIG = 276.6 },
                    new() { TempF = 88, PressurePSIG = 295.6 },
                    new() { TempF = 92, PressurePSIG = 315.7 },
                    new() { TempF = 96, PressurePSIG = 336.7 },
                    new() { TempF = 100, PressurePSIG = 358.9 },
                    new() { TempF = 104, PressurePSIG = 382.3 },
                    new() { TempF = 108, PressurePSIG = 406.9 },
                    new() { TempF = 112, PressurePSIG = 432.8 },
                    new() { TempF = 116, PressurePSIG = 460 },
                    new() { TempF = 120, PressurePSIG = 488.7 },
                    new() { TempF = 124, PressurePSIG = 518.8 },
                    new() { TempF = 128, PressurePSIG = 550.5 },
                    new() { TempF = 132, PressurePSIG = 583.8 },
                    new() { TempF = 136, PressurePSIG = 618.9 },
                    new() { TempF = 140, PressurePSIG = 655.8 },
                    new() { TempF = 144, PressurePSIG = 694.8 },
                    new() { TempF = 148, PressurePSIG = 613.6 }
                    },

                ["R-454B"] = new List<RPTDataPoint>
                    {
                    new() { TempF = -48, PressurePSIG = 4.1 },
                    new() { TempF = -44, PressurePSIG = 6.2 },
                    new() { TempF = -40, PressurePSIG = 8.4 },
                    new() { TempF = -36, PressurePSIG = 10.8 },
                    new() { TempF = -32, PressurePSIG = 13.4 },
                    new() { TempF = -28, PressurePSIG = 16.3 },
                    new() { TempF = -24, PressurePSIG = 19.3 },
                    new() { TempF = -20, PressurePSIG = 22.6 },
                    new() { TempF = -16, PressurePSIG = 26.1 },
                    new() { TempF = -12, PressurePSIG = 29.9 },
                    new() { TempF = -8, PressurePSIG = 34 },
                    new() { TempF = -4, PressurePSIG = 38.4 },
                    new() { TempF = 0, PressurePSIG = 43.1 },
                    new() { TempF = 4, PressurePSIG = 48.1 },
                    new() { TempF = 8, PressurePSIG = 53.5 },
                    new() { TempF = 12, PressurePSIG = 59.3 },
                    new() { TempF = 16, PressurePSIG = 65.3 },
                    new() { TempF = 20, PressurePSIG = 71.9 },
                    new() { TempF = 24, PressurePSIG = 78.7 },
                    new() { TempF = 28, PressurePSIG = 86.1 },
                    new() { TempF = 32, PressurePSIG = 94 },
                    new() { TempF = 36, PressurePSIG = 102.3 },
                    new() { TempF = 40, PressurePSIG = 111.3 },
                    new() { TempF = 44, PressurePSIG = 120.7 },
                    new() { TempF = 48, PressurePSIG = 130.8 },
                    new() { TempF = 52, PressurePSIG = 141.5 },
                    new() { TempF = 56, PressurePSIG = 152.8 },
                    new() { TempF = 60, PressurePSIG = 164.8 },
                    new() { TempF = 64, PressurePSIG = 177.5 },
                    new() { TempF = 68, PressurePSIG = 191 },
                    new() { TempF = 72, PressurePSIG = 205.1 },
                    new() { TempF = 76, PressurePSIG = 220.1 },
                    new() { TempF = 80, PressurePSIG = 235.9 },
                    new() { TempF = 84, PressurePSIG = 252.6 },
                    new() { TempF = 88, PressurePSIG = 270.2 },
                    new() { TempF = 92, PressurePSIG = 288.7 },
                    new() { TempF = 96, PressurePSIG = 308.4 },
                    new() { TempF = 100, PressurePSIG = 329.0 },
                    new() { TempF = 104, PressurePSIG = 350.7 },
                    new() { TempF = 108, PressurePSIG = 373.6 },
                    new() { TempF = 112, PressurePSIG = 397.5 },
                    new() { TempF = 116, PressurePSIG = 422.9 },
                    new() { TempF = 120, PressurePSIG = 449.2 },
                    new() { TempF = 124, PressurePSIG = 477.2 },
                    new() { TempF = 128, PressurePSIG = 506.4 },
                    new() { TempF = 132, PressurePSIG = 537.1 },
                    new() { TempF = 136, PressurePSIG = 569.3 },
                    new() { TempF = 140, PressurePSIG = 603.2 },
                    new() { TempF = 144, PressurePSIG = 638.8 },
                    new() { TempF = 148, PressurePSIG = 551.5 }
                    },

                ["R-22"] = new List<RPTDataPoint>
                    {
                    new() { TempF = -48, PressurePSIG = 4.8 },
                    new() { TempF = -44, PressurePSIG = 1.9 },
                    new() { TempF = -40, PressurePSIG = 0.6 },
                    new() { TempF = -36, PressurePSIG = 2.2 },
                    new() { TempF = -32, PressurePSIG = 4 },
                    new() { TempF = -28, PressurePSIG = 5.9 },
                    new() { TempF = -24, PressurePSIG = 8 },
                    new() { TempF = -20, PressurePSIG = 10.2 },
                    new() { TempF = -16, PressurePSIG = 12.6 },
                    new() { TempF = -12, PressurePSIG = 15.1 },
                    new() { TempF = -8, PressurePSIG = 18 },
                    new() { TempF = -4, PressurePSIG = 21 },
                    new() { TempF = 0, PressurePSIG = 24.2 },
                    new() { TempF = 4, PressurePSIG = 27.7 },
                    new() { TempF = 8, PressurePSIG = 31.3 },
                    new() { TempF = 12, PressurePSIG = 35.3 },
                    new() { TempF = 16, PressurePSIG = 39.4 },
                    new() { TempF = 20, PressurePSIG = 43.9 },
                    new() { TempF = 24, PressurePSIG = 48.5 },
                    new() { TempF = 28, PressurePSIG = 53.6 },
                    new() { TempF = 32, PressurePSIG = 58.8 },
                    new() { TempF = 36, PressurePSIG = 64.4 },
                    new() { TempF = 40, PressurePSIG = 70.4 },
                    new() { TempF = 44, PressurePSIG = 76.6 },
                    new() { TempF = 48, PressurePSIG = 83.4 },
                    new() { TempF = 52, PressurePSIG = 90.4 },
                    new() { TempF = 56, PressurePSIG = 98 },
                    new() { TempF = 60, PressurePSIG = 106 },
                    new() { TempF = 64, PressurePSIG = 114.4 },
                    new() { TempF = 68, PressurePSIG = 123.3 },
                    new() { TempF = 72, PressurePSIG = 132.7 },
                    new() { TempF = 76, PressurePSIG = 142.6 },
                    new() { TempF = 80, PressurePSIG = 153 },
                    new() { TempF = 84, PressurePSIG = 164.1 },
                    new() { TempF = 88, PressurePSIG = 175.6 },
                    new() { TempF = 92, PressurePSIG = 187.8 },
                    new() { TempF = 96, PressurePSIG = 200.6 },
                    new() { TempF = 100, PressurePSIG = 214.1 },
                    new() { TempF = 104, PressurePSIG = 228.3 },
                    new() { TempF = 108, PressurePSIG = 243.2 },
                    new() { TempF = 112, PressurePSIG = 259 },
                    new() { TempF = 116, PressurePSIG = 275.5 },
                    new() { TempF = 120, PressurePSIG = 292.9 },
                    new() { TempF = 124, PressurePSIG = 311.1 },
                    new() { TempF = 128, PressurePSIG = 330.3 },
                    new() { TempF = 132, PressurePSIG = 350.5 },
                    new() { TempF = 136, PressurePSIG = 371.7 },
                    new() { TempF = 140, PressurePSIG = 394 },
                    new() { TempF = 144, PressurePSIG = 417.5 },
                    new() { TempF = 148, PressurePSIG = 372.5 }
                    },

                ["R-427A"] = new List<RPTDataPoint>
                    {
                    new() { TempF = -48, PressurePSIG = 10.2 },
                    new() { TempF = -44, PressurePSIG = 7.8 },
                    new() { TempF = -40, PressurePSIG = 5.1 },
                    new() { TempF = -36, PressurePSIG = 2.2 },
                    new() { TempF = -32, PressurePSIG = 0.9 },
                    new() { TempF = -28, PressurePSIG = 3.2 },
                    new() { TempF = -24, PressurePSIG = 5.7 },
                    new() { TempF = -20, PressurePSIG = 8.3 },
                    new() { TempF = -16, PressurePSIG = 11.1 },
                    new() { TempF = -12, PressurePSIG = 14.1 },
                    new() { TempF = -8, PressurePSIG = 17.4 },
                    new() { TempF = -4, PressurePSIG = 20.8 },
                    new() { TempF = 0, PressurePSIG = 24.5 },
                    new() { TempF = 4, PressurePSIG = 28.5 },
                    new() { TempF = 8, PressurePSIG = 32.7 },
                    new() { TempF = 12, PressurePSIG = 37.2 },
                    new() { TempF = 16, PressurePSIG = 42 },
                    new() { TempF = 20, PressurePSIG = 47.1 },
                    new() { TempF = 24, PressurePSIG = 52.6 },
                    new() { TempF = 28, PressurePSIG = 58.5 },
                    new() { TempF = 32, PressurePSIG = 64.7 },
                    new() { TempF = 36, PressurePSIG = 71.3 },
                    new() { TempF = 40, PressurePSIG = 78.4 },
                    new() { TempF = 44, PressurePSIG = 85.9 },
                    new() { TempF = 48, PressurePSIG = 93.9 },
                    new() { TempF = 52, PressurePSIG = 102.4 },
                    new() { TempF = 56, PressurePSIG = 111.4 },
                    new() { TempF = 60, PressurePSIG = 121 },
                    new() { TempF = 64, PressurePSIG = 131.2 },
                    new() { TempF = 68, PressurePSIG = 142 },
                    new() { TempF = 72, PressurePSIG = 153.5 },
                    new() { TempF = 76, PressurePSIG = 165.6 },
                    new() { TempF = 80, PressurePSIG = 178.5 },
                    new() { TempF = 84, PressurePSIG = 192.1 },
                    new() { TempF = 88, PressurePSIG = 206.5 },
                    new() { TempF = 92, PressurePSIG = 221.8 },
                    new() { TempF = 96, PressurePSIG = 237.9 },
                    new() { TempF = 100, PressurePSIG = 255 },
                    new() { TempF = 104, PressurePSIG = 273.1 },
                    new() { TempF = 108, PressurePSIG = 292.1 },
                    new() { TempF = 112, PressurePSIG = 312.3 },
                    new() { TempF = 116, PressurePSIG = 333.6 },
                    new() { TempF = 120, PressurePSIG = 356 },
                    new() { TempF = 124, PressurePSIG = 379.8 },
                    new() { TempF = 128, PressurePSIG = 404.8 },
                    new() { TempF = 132, PressurePSIG = 431.3 },
                    new() { TempF = 136, PressurePSIG = 459.3 },
                    new() { TempF = 140, PressurePSIG = 488.9 },
                    new() { TempF = 144, PressurePSIG = 520.2 },
                    new() { TempF = 148, PressurePSIG = 408.9 }
                    },

                ["R-407C"] = new List<RPTDataPoint>
                    {
                    new() { TempF = -48, PressurePSIG = 9.8 },
                    new() { TempF = -44, PressurePSIG = 7.3 },
                    new() { TempF = -40, PressurePSIG = 4.6 },
                    new() { TempF = -36, PressurePSIG = 1.6 },
                    new() { TempF = -32, PressurePSIG = 0.3 },
                    new() { TempF = -28, PressurePSIG = 2.6 },
                    new() { TempF = -24, PressurePSIG = 5.1 },
                    new() { TempF = -20, PressurePSIG = 7.8 },
                    new() { TempF = -16, PressurePSIG = 10.6 },
                    new() { TempF = -12, PressurePSIG = 13.6 },
                    new() { TempF = -8, PressurePSIG = 16.9 },
                    new() { TempF = -4, PressurePSIG = 20.4 },
                    new() { TempF = 0, PressurePSIG = 24.2 },
                    new() { TempF = 4, PressurePSIG = 28.2 },
                    new() { TempF = 8, PressurePSIG = 32.5 },
                    new() { TempF = 12, PressurePSIG = 37.1 },
                    new() { TempF = 16, PressurePSIG = 42 },
                    new() { TempF = 20, PressurePSIG = 47.2 },
                    new() { TempF = 24, PressurePSIG = 52.8 },
                    new() { TempF = 28, PressurePSIG = 58.8 },
                    new() { TempF = 32, PressurePSIG = 65.2 },
                    new() { TempF = 36, PressurePSIG = 72 },
                    new() { TempF = 40, PressurePSIG = 79.2 },
                    new() { TempF = 44, PressurePSIG = 86.9 },
                    new() { TempF = 48, PressurePSIG = 95.1 },
                    new() { TempF = 52, PressurePSIG = 103.9 },
                    new() { TempF = 56, PressurePSIG = 113.2 },
                    new() { TempF = 60, PressurePSIG = 123.1 },
                    new() { TempF = 64, PressurePSIG = 133.6 },
                    new() { TempF = 68, PressurePSIG = 144.8 },
                    new() { TempF = 72, PressurePSIG = 156.7 },
                    new() { TempF = 76, PressurePSIG = 169.3 },
                    new() { TempF = 80, PressurePSIG = 182.7 },
                    new() { TempF = 84, PressurePSIG = 196.9 },
                    new() { TempF = 88, PressurePSIG = 212 },
                    new() { TempF = 92, PressurePSIG = 227.9 },
                    new() { TempF = 96, PressurePSIG = 244.8 },
                    new() { TempF = 100, PressurePSIG = 262.7 },
                    new() { TempF = 104, PressurePSIG = 281.7 },
                    new() { TempF = 108, PressurePSIG = 301.7 },
                    new() { TempF = 112, PressurePSIG = 322.9 },
                    new() { TempF = 116, PressurePSIG = 345.3 },
                    new() { TempF = 120, PressurePSIG = 369 },
                    new() { TempF = 124, PressurePSIG = 394.1 },
                    new() { TempF = 128, PressurePSIG = 420.6 },
                    new() { TempF = 132, PressurePSIG = 448.7 },
                    new() { TempF = 136, PressurePSIG = 478.3 },
                    new() { TempF = 140, PressurePSIG = 509.7 },
                    new() { TempF = 144, PressurePSIG = 542.9 },
                    new() { TempF = 148, PressurePSIG = 427 }
                    },

                ["R-422B"] = new List<RPTDataPoint>
                    {
                    new() { TempF = -48, PressurePSIG = 10.3 },
                    new() { TempF = -44, PressurePSIG = 8 },
                    new() { TempF = -40, PressurePSIG = 5.4 },
                    new() { TempF = -36, PressurePSIG = 2.6 },
                    new() { TempF = -32, PressurePSIG = 0.4 },
                    new() { TempF = -28, PressurePSIG = 2.8 },
                    new() { TempF = -24, PressurePSIG = 5.4 },
                    new() { TempF = -20, PressurePSIG = 8.1 },
                    new() { TempF = -16, PressurePSIG = 11 },
                    new() { TempF = -12, PressurePSIG = 14.1 },
                    new() { TempF = -8, PressurePSIG = 17.5 },
                    new() { TempF = -4, PressurePSIG = 21 },
                    new() { TempF = 0, PressurePSIG = 24.9 },
                    new() { TempF = 4, PressurePSIG = 28.9 },
                    new() { TempF = 8, PressurePSIG = 33.3 },
                    new() { TempF = 12, PressurePSIG = 37.9 },
                    new() { TempF = 16, PressurePSIG = 42.9 },
                    new() { TempF = 20, PressurePSIG = 48.1 },
                    new() { TempF = 24, PressurePSIG = 53.8 },
                    new() { TempF = 28, PressurePSIG = 59.8 },
                    new() { TempF = 32, PressurePSIG = 66.2 },
                    new() { TempF = 36, PressurePSIG = 73 },
                    new() { TempF = 40, PressurePSIG = 80.3 },
                    new() { TempF = 44, PressurePSIG = 88 },
                    new() { TempF = 48, PressurePSIG = 96.2 },
                    new() { TempF = 52, PressurePSIG = 105 },
                    new() { TempF = 56, PressurePSIG = 114.3 },
                    new() { TempF = 60, PressurePSIG = 124.2 },
                    new() { TempF = 64, PressurePSIG = 134.7 },
                    new() { TempF = 68, PressurePSIG = 145.8 },
                    new() { TempF = 72, PressurePSIG = 157.7 },
                    new() { TempF = 76, PressurePSIG = 170.2 },
                    new() { TempF = 80, PressurePSIG = 183.6 },
                    new() { TempF = 84, PressurePSIG = 197.7 },
                    new() { TempF = 88, PressurePSIG = 212.6 },
                    new() { TempF = 92, PressurePSIG = 228.5 },
                    new() { TempF = 96, PressurePSIG = 245.3 },
                    new() { TempF = 100, PressurePSIG = 263 },
                    new() { TempF = 104, PressurePSIG = 281.8 },
                    new() { TempF = 108, PressurePSIG = 301.7 },
                    new() { TempF = 112, PressurePSIG = 322.7 },
                    new() { TempF = 116, PressurePSIG = 344.9 },
                    new() { TempF = 120, PressurePSIG = 368.4 },
                    new() { TempF = 124, PressurePSIG = 393.2 },
                    new() { TempF = 128, PressurePSIG = 419.4 },
                    new() { TempF = 132, PressurePSIG = 447.2 },
                    new() { TempF = 136, PressurePSIG = 476.5 },
                    new() { TempF = 140, PressurePSIG = 507.5 },
                    new() { TempF = 144, PressurePSIG = 540.4 },
                    new() { TempF = 148, PressurePSIG = 377.7 }
                    },

                ["R-438A"] = new List<RPTDataPoint>
                    {
                    new() { TempF = -48, PressurePSIG = 10.2 },
                    new() { TempF = -44, PressurePSIG = 7.9 },
                    new() { TempF = -40, PressurePSIG = 5.2 },
                    new() { TempF = -36, PressurePSIG = 2.1 },
                    new() { TempF = -32, PressurePSIG = 0.8 },
                    new() { TempF = -28, PressurePSIG = 3.1 },
                    new() { TempF = -24, PressurePSIG = 5.6 },
                    new() { TempF = -20, PressurePSIG = 8.3 },
                    new() { TempF = -16, PressurePSIG = 11.2 },
                    new() { TempF = -12, PressurePSIG = 14.3 },
                    new() { TempF = -8, PressurePSIG = 17.6 },
                    new() { TempF = -4, PressurePSIG = 21.2 },
                    new() { TempF = 0, PressurePSIG = 25 },
                    new() { TempF = 4, PressurePSIG = 29.1 },
                    new() { TempF = 8, PressurePSIG = 33.4 },
                    new() { TempF = 12, PressurePSIG = 38.1 },
                    new() { TempF = 16, PressurePSIG = 43 },
                    new() { TempF = 20, PressurePSIG = 48.3 },
                    new() { TempF = 24, PressurePSIG = 53.9 },
                    new() { TempF = 28, PressurePSIG = 60 },
                    new() { TempF = 32, PressurePSIG = 66.3 },
                    new() { TempF = 36, PressurePSIG = 73.2 },
                    new() { TempF = 40, PressurePSIG = 80.4 },
                    new() { TempF = 44, PressurePSIG = 88.2 },
                    new() { TempF = 48, PressurePSIG = 96.4 },
                    new() { TempF = 52, PressurePSIG = 105.1 },
                    new() { TempF = 56, PressurePSIG = 114.4 },
                    new() { TempF = 60, PressurePSIG = 124.3 },
                    new() { TempF = 64, PressurePSIG = 134.8 },
                    new() { TempF = 68, PressurePSIG = 146 },
                    new() { TempF = 72, PressurePSIG = 157.8 },
                    new() { TempF = 76, PressurePSIG = 170.4 },
                    new() { TempF = 80, PressurePSIG = 183.7 },
                    new() { TempF = 84, PressurePSIG = 197.8 },
                    new() { TempF = 88, PressurePSIG = 212.8 },
                    new() { TempF = 92, PressurePSIG = 228.6 },
                    new() { TempF = 96, PressurePSIG = 245.4 },
                    new() { TempF = 100, PressurePSIG = 263.2 },
                    new() { TempF = 104, PressurePSIG = 282 },
                    new() { TempF = 108, PressurePSIG = 301.9 },
                    new() { TempF = 112, PressurePSIG = 322.9 },
                    new() { TempF = 116, PressurePSIG = 345.1 },
                    new() { TempF = 120, PressurePSIG = 368.6 },
                    new() { TempF = 124, PressurePSIG = 393.4 },
                    new() { TempF = 128, PressurePSIG = 419.6 },
                    new() { TempF = 132, PressurePSIG = 447.4 },
                    new() { TempF = 136, PressurePSIG = 476.7 },
                    new() { TempF = 140, PressurePSIG = 507.8 },
                    new() { TempF = 144, PressurePSIG = 540.7 },
                    new() { TempF = 148, PressurePSIG = 378.1 }
                    },

                ["R-404A"] = new List<RPTDataPoint>
                    {
                    new() { TempF = -48, PressurePSIG = 1.3 },
                    new() { TempF = -44, PressurePSIG = 3 },
                    new() { TempF = -40, PressurePSIG = 4.9 },
                    new() { TempF = -36, PressurePSIG = 7 },
                    new() { TempF = -32, PressurePSIG = 9.3 },
                    new() { TempF = -28, PressurePSIG = 11.8 },
                    new() { TempF = -24, PressurePSIG = 14.5 },
                    new() { TempF = -20, PressurePSIG = 17.4 },
                    new() { TempF = -16, PressurePSIG = 20.6 },
                    new() { TempF = -12, PressurePSIG = 24 },
                    new() { TempF = -8, PressurePSIG = 27.6 },
                    new() { TempF = -4, PressurePSIG = 31.5 },
                    new() { TempF = 0, PressurePSIG = 35.7 },
                    new() { TempF = 4, PressurePSIG = 40.2 },
                    new() { TempF = 8, PressurePSIG = 45 },
                    new() { TempF = 12, PressurePSIG = 50.2 },
                    new() { TempF = 16, PressurePSIG = 55.7 },
                    new() { TempF = 20, PressurePSIG = 61.5 },
                    new() { TempF = 24, PressurePSIG = 67.8 },
                    new() { TempF = 28, PressurePSIG = 74.4 },
                    new() { TempF = 32, PressurePSIG = 81.5 },
                    new() { TempF = 36, PressurePSIG = 89 },
                    new() { TempF = 40, PressurePSIG = 97 },
                    new() { TempF = 44, PressurePSIG = 105.4 },
                    new() { TempF = 48, PressurePSIG = 114.4 },
                    new() { TempF = 52, PressurePSIG = 123.9 },
                    new() { TempF = 56, PressurePSIG = 134 },
                    new() { TempF = 60, PressurePSIG = 144.6 },
                    new() { TempF = 64, PressurePSIG = 155.9 },
                    new() { TempF = 68, PressurePSIG = 167.7 },
                    new() { TempF = 72, PressurePSIG = 180.3 },
                    new() { TempF = 76, PressurePSIG = 193.5 },
                    new() { TempF = 80, PressurePSIG = 207.5 },
                    new() { TempF = 84, PressurePSIG = 222.2 },
                    new() { TempF = 88, PressurePSIG = 237.7 },
                    new() { TempF = 92, PressurePSIG = 254 },
                    new() { TempF = 96, PressurePSIG = 271.2 },
                    new() { TempF = 100, PressurePSIG = 289.3 },
                    new() { TempF = 104, PressurePSIG = 308.4 },
                    new() { TempF = 108, PressurePSIG = 328.5 },
                    new() { TempF = 112, PressurePSIG = 349.6 },
                    new() { TempF = 116, PressurePSIG = 371.9 },
                    new() { TempF = 120, PressurePSIG = 395.4 },
                    new() { TempF = 124, PressurePSIG = 420.1 },
                    new() { TempF = 128, PressurePSIG = 446.1 },
                    new() { TempF = 132, PressurePSIG = 473.5 },
                    new() { TempF = 136, PressurePSIG = 502.4 },
                    new() { TempF = 140, PressurePSIG = 532.9 },
                    new() { TempF = 144, PressurePSIG = 565 },
                    new() { TempF = 148, PressurePSIG = 377.7 }
                    },

                ["R-448A"] = new List<RPTDataPoint>
                {
                    new() { TempF = -48, PressurePSIG = 6.1 },
                    new() { TempF = -44, PressurePSIG = 3.3 },
                    new() { TempF = -40, PressurePSIG = 0 },
                    new() { TempF = -36, PressurePSIG = 1.6 },
                    new() { TempF = -32, PressurePSIG = 3.9 },
                    new() { TempF = -28, PressurePSIG = 6.5 },
                    new() { TempF = -24, PressurePSIG = 9.2 },
                    new() { TempF = -20, PressurePSIG = 12.2 },
                    new() { TempF = -16, PressurePSIG = 15.3 },
                    new() { TempF = -12, PressurePSIG = 18.7 },
                    new() { TempF = -8, PressurePSIG = 22.3 },
                    new() { TempF = -4, PressurePSIG = 26.2 },
                    new() { TempF = 0, PressurePSIG = 30.3 },
                    new() { TempF = 4, PressurePSIG = 34.6 },
                    new() { TempF = 8, PressurePSIG = 39.3 },
                    new() { TempF = 12, PressurePSIG = 44.3 },
                    new() { TempF = 16, PressurePSIG = 49.5 },
                    new() { TempF = 20, PressurePSIG = 55.2 },
                    new() { TempF = 24, PressurePSIG = 61.1 },
                    new() { TempF = 28, PressurePSIG = 67.5 },
                    new() { TempF = 32, PressurePSIG = 74.3 },
                    new() { TempF = 36, PressurePSIG = 81.5 },
                    new() { TempF = 40, PressurePSIG = 89.1 },
                    new() { TempF = 44, PressurePSIG = 97.2 },
                    new() { TempF = 48, PressurePSIG = 105.8 },
                    new() { TempF = 52, PressurePSIG = 114.9 },
                    new() { TempF = 56, PressurePSIG = 124.6 },
                    new() { TempF = 60, PressurePSIG = 134.9 },
                    new() { TempF = 64, PressurePSIG = 145.7 },
                    new() { TempF = 68, PressurePSIG = 157.2 },
                    new() { TempF = 72, PressurePSIG = 169.4 },
                    new() { TempF = 76, PressurePSIG = 182.2 },
                    new() { TempF = 80, PressurePSIG = 195.8 },
                    new() { TempF = 84, PressurePSIG = 210.2 },
                    new() { TempF = 88, PressurePSIG = 225.3 },
                    new() { TempF = 92, PressurePSIG = 241.3 },
                    new() { TempF = 96, PressurePSIG = 258.2 },
                    new() { TempF = 100, PressurePSIG = 276 },
                    new() { TempF = 104, PressurePSIG = 294.8 },
                    new() { TempF = 108, PressurePSIG = 314.7 },
                    new() { TempF = 112, PressurePSIG = 335.6 },
                    new() { TempF = 116, PressurePSIG = 357.7 },
                    new() { TempF = 120, PressurePSIG = 381 },
                    new() { TempF = 124, PressurePSIG = 405.5 },
                    new() { TempF = 128, PressurePSIG = 431.4 },
                    new() { TempF = 132, PressurePSIG = 458.6 },
                    new() { TempF = 136, PressurePSIG = 487.4 },
                    new() { TempF = 140, PressurePSIG = 517.6 },
                    new() { TempF = 144, PressurePSIG = 549.5 },
                    new() { TempF = 148, PressurePSIG = 401.5 }
                    },

                ["R-449A"] = new List<RPTDataPoint>
                    {
                    new() { TempF = -48, PressurePSIG = 6.1 },
                    new() { TempF = -44, PressurePSIG = 3.3 },
                    new() { TempF = -40, PressurePSIG = 0.2 },
                    new() { TempF = -36, PressurePSIG = 1.6 },
                    new() { TempF = -32, PressurePSIG = 4 },
                    new() { TempF = -28, PressurePSIG = 6.5 },
                    new() { TempF = -24, PressurePSIG = 9.3 },
                    new() { TempF = -20, PressurePSIG = 12.3 },
                    new() { TempF = -16, PressurePSIG = 15.5 },
                    new() { TempF = -12, PressurePSIG = 18.9 },
                    new() { TempF = -8, PressurePSIG = 22.5 },
                    new() { TempF = -4, PressurePSIG = 26.4 },
                    new() { TempF = 0, PressurePSIG = 30.6 },
                    new() { TempF = 4, PressurePSIG = 35 },
                    new() { TempF = 8, PressurePSIG = 39.7 },
                    new() { TempF = 12, PressurePSIG = 44.8 },
                    new() { TempF = 16, PressurePSIG = 50.1 },
                    new() { TempF = 20, PressurePSIG = 55.8 },
                    new() { TempF = 24, PressurePSIG = 61.9 },
                    new() { TempF = 28, PressurePSIG = 68.4 },
                    new() { TempF = 32, PressurePSIG = 75.2 },
                    new() { TempF = 36, PressurePSIG = 82.6 },
                    new() { TempF = 40, PressurePSIG = 90.3 },
                    new() { TempF = 44, PressurePSIG = 98.6 },
                    new() { TempF = 48, PressurePSIG = 107.4 },
                    new() { TempF = 52, PressurePSIG = 116.7 },
                    new() { TempF = 56, PressurePSIG = 126.6 },
                    new() { TempF = 60, PressurePSIG = 137.1 },
                    new() { TempF = 64, PressurePSIG = 148.2 },
                    new() { TempF = 68, PressurePSIG = 160 },
                    new() { TempF = 72, PressurePSIG = 172.5 },
                    new() { TempF = 76, PressurePSIG = 185.7 },
                    new() { TempF = 80, PressurePSIG = 199.6 },
                    new() { TempF = 84, PressurePSIG = 214.4 },
                    new() { TempF = 88, PressurePSIG = 230 },
                    new() { TempF = 92, PressurePSIG = 246.5 },
                    new() { TempF = 96, PressurePSIG = 263.9 },
                    new() { TempF = 100, PressurePSIG = 282.3 },
                    new() { TempF = 104, PressurePSIG = 301.7 },
                    new() { TempF = 108, PressurePSIG = 322.2 },
                    new() { TempF = 112, PressurePSIG = 343.8 },
                    new() { TempF = 116, PressurePSIG = 366.6 },
                    new() { TempF = 120, PressurePSIG = 390.7 },
                    new() { TempF = 124, PressurePSIG = 416.1 },
                    new() { TempF = 128, PressurePSIG = 442.9 },
                    new() { TempF = 132, PressurePSIG = 471.2 },
                    new() { TempF = 136, PressurePSIG = 501 },
                    new() { TempF = 140, PressurePSIG = 532.5 },
                    new() { TempF = 144, PressurePSIG = 565.7 },
                    new() { TempF = 148, PressurePSIG = 450.8 }
                    },

                ["R-454C"] = new List<RPTDataPoint>
                    {
                    new() { TempF = -48, PressurePSIG = 0.8 },
                    new() { TempF = -44, PressurePSIG = 2.5 },
                    new() { TempF = -40, PressurePSIG = 4.3 },
                    new() { TempF = -36, PressurePSIG = 6.2 },
                    new() { TempF = -32, PressurePSIG = 8.3 },
                    new() { TempF = -28, PressurePSIG = 10.6 },
                    new() { TempF = -24, PressurePSIG = 13.2 },
                    new() { TempF = -20, PressurePSIG = 15.9 },
                    new() { TempF = -16, PressurePSIG = 18.9 },
                    new() { TempF = -12, PressurePSIG = 22.1 },
                    new() { TempF = -8, PressurePSIG = 25.5 },
                    new() { TempF = -4, PressurePSIG = 29.2 },
                    new() { TempF = 0, PressurePSIG = 33.2 },
                    new() { TempF = 4, PressurePSIG = 37.4 },
                    new() { TempF = 8, PressurePSIG = 42 },
                    new() { TempF = 12, PressurePSIG = 46.9 },
                    new() { TempF = 16, PressurePSIG = 52.1 },
                    new() { TempF = 20, PressurePSIG = 57.6 },
                    new() { TempF = 24, PressurePSIG = 63.5 },
                    new() { TempF = 28, PressurePSIG = 69.8 },
                    new() { TempF = 32, PressurePSIG = 76.5 },
                    new() { TempF = 36, PressurePSIG = 83.6 },
                    new() { TempF = 40, PressurePSIG = 91.2 },
                    new() { TempF = 44, PressurePSIG = 99.2 },
                    new() { TempF = 48, PressurePSIG = 107.8 },
                    new() { TempF = 52, PressurePSIG = 116.8 },
                    new() { TempF = 56, PressurePSIG = 126.5 },
                    new() { TempF = 60, PressurePSIG = 136.7 },
                    new() { TempF = 64, PressurePSIG = 147.5 },
                    new() { TempF = 68, PressurePSIG = 159 },
                    new() { TempF = 72, PressurePSIG = 171.1 },
                    new() { TempF = 76, PressurePSIG = 183.9 },
                    new() { TempF = 80, PressurePSIG = 197.5 },
                    new() { TempF = 84, PressurePSIG = 211.9 },
                    new() { TempF = 88, PressurePSIG = 227.1 },
                    new() { TempF = 92, PressurePSIG = 243.1 },
                    new() { TempF = 96, PressurePSIG = 260.1 },
                    new() { TempF = 100, PressurePSIG = 278 },
                    new() { TempF = 104, PressurePSIG = 296.9 },
                    new() { TempF = 108, PressurePSIG = 316.9 },
                    new() { TempF = 112, PressurePSIG = 338 },
                    new() { TempF = 116, PressurePSIG = 360.2 },
                    new() { TempF = 120, PressurePSIG = 383.6 },
                    new() { TempF = 124, PressurePSIG = 408.3 },
                    new() { TempF = 128, PressurePSIG = 434.3 },
                    new() { TempF = 132, PressurePSIG = 461.8 },
                    new() { TempF = 136, PressurePSIG = 490.7 },
                    new() { TempF = 140, PressurePSIG = 521.2 },
                    new() { TempF = 144, PressurePSIG = 553.4 },
                    new() { TempF = 148, PressurePSIG = 410.9 }
                    },

                ["R-513A"] = new List<RPTDataPoint>
                    {
                    new() { TempF = -48, PressurePSIG = 15.7 },
                    new() { TempF = -44, PressurePSIG = 14.1 },
                    new() { TempF = -40, PressurePSIG = 12.3 },
                    new() { TempF = -36, PressurePSIG = 10 },
                    new() { TempF = -32, PressurePSIG = 7.4 },
                    new() { TempF = -28, PressurePSIG = 4.2 },
                    new() { TempF = -24, PressurePSIG = 0.4 },
                    new() { TempF = -20, PressurePSIG = 1.8 },
                    new() { TempF = -16, PressurePSIG = 4.2 },
                    new() { TempF = -12, PressurePSIG = 6.8 },
                    new() { TempF = -8, PressurePSIG = 9.5 },
                    new() { TempF = -4, PressurePSIG = 12.4 },
                    new() { TempF = 0, PressurePSIG = 15.5 },
                    new() { TempF = 4, PressurePSIG = 18.8 },
                    new() { TempF = 8, PressurePSIG = 22.3 },
                    new() { TempF = 12, PressurePSIG = 26.1 },
                    new() { TempF = 16, PressurePSIG = 30.1 },
                    new() { TempF = 20, PressurePSIG = 34.4 },
                    new() { TempF = 24, PressurePSIG = 38.9 },
                    new() { TempF = 28, PressurePSIG = 43.8 },
                    new() { TempF = 32, PressurePSIG = 48.9 },
                    new() { TempF = 36, PressurePSIG = 54.4 },
                    new() { TempF = 40, PressurePSIG = 60.3 },
                    new() { TempF = 44, PressurePSIG = 66.5 },
                    new() { TempF = 48, PressurePSIG = 73.1 },
                    new() { TempF = 52, PressurePSIG = 80.1 },
                    new() { TempF = 56, PressurePSIG = 87.5 },
                    new() { TempF = 60, PressurePSIG = 95.4 },
                    new() { TempF = 64, PressurePSIG = 103.7 },
                    new() { TempF = 68, PressurePSIG = 112.6 },
                    new() { TempF = 72, PressurePSIG = 122 },
                    new() { TempF = 76, PressurePSIG = 131.9 },
                    new() { TempF = 80, PressurePSIG = 142.4 },
                    new() { TempF = 84, PressurePSIG = 153.5 },
                    new() { TempF = 88, PressurePSIG = 165.2 },
                    new() { TempF = 92, PressurePSIG = 177.6 },
                    new() { TempF = 96, PressurePSIG = 190.7 },
                    new() { TempF = 100, PressurePSIG = 204.6 },
                    new() { TempF = 104, PressurePSIG = 219.2 },
                    new() { TempF = 108, PressurePSIG = 234.6 },
                    new() { TempF = 112, PressurePSIG = 250.9 },
                    new() { TempF = 116, PressurePSIG = 268.1 },
                    new() { TempF = 120, PressurePSIG = 286.2 },
                    new() { TempF = 124, PressurePSIG = 305.4 },
                    new() { TempF = 128, PressurePSIG = 325.5 },
                    new() { TempF = 132, PressurePSIG = 346.8 },
                    new() { TempF = 136, PressurePSIG = 369.2 },
                    new() { TempF = 140, PressurePSIG = 392.8 },
                    new() { TempF = 144, PressurePSIG = 417.8 },
                    new() { TempF = 148, PressurePSIG = 264.6 }
                    }
            };
        }

        private RefrigerantInfo GetRefrigerantInfo(string refrigerantType)
        {
            var refrigerantProperties = new Dictionary<string, RefrigerantInfo>
            {
                ["R-410A"] = new RefrigerantInfo
                {
                    FullName = "R-410A (Difluoromethane/Pentafluoroethane)",
                    SafetyClass = "A1 (Non-toxic, Non-flammable)",
                    TypicalUse = "Standard commercial HVAC, rooftop units, split systems",
                    GWP = 2088,
                    ODP = 0,
                    OperatingRange = "-48°F to 120°F",
                    Notes = "Most common commercial refrigerant. Higher operating pressures than R-22. Excellent performance and reliability."
                },
                ["R-22"] = new RefrigerantInfo
                {
                    FullName = "R-22 (Chlorodifluoromethane)",
                    SafetyClass = "A1 (Non-toxic, Non-flammable)",
                    TypicalUse = "Legacy HVAC systems (being phased out)",
                    GWP = 1810,
                    ODP = 0.055,
                    OperatingRange = "-48°F to 120°F",
                    Notes = "HCFC refrigerant being phased out. Very expensive due to limited production. Service existing equipment only."
                },
                ["R-134A"] = new RefrigerantInfo
                {
                    FullName = "R-134A (Tetrafluoroethane)",
                    SafetyClass = "A1 (Non-toxic, Non-flammable)",
                    TypicalUse = "Chiller applications, automotive AC, medium temperature refrigeration",
                    GWP = 1430,
                    ODP = 0,
                    OperatingRange = "-40°F to 120°F",
                    Notes = "Common in chiller applications. Lower operating pressures. Being phased down under Kigali Amendment."
                },
                ["R-32"] = new RefrigerantInfo
                {
                    FullName = "R-32 (Difluoromethane)",
                    SafetyClass = "A2L (Non-toxic, Mildly flammable)",
                    TypicalUse = "High-efficiency HVAC systems, VRF systems",
                    GWP = 675,
                    ODP = 0,
                    OperatingRange = "-48°F to 120°F",
                    Notes = "Lower GWP alternative with higher efficiency. Requires A2L safety protocols and compatible equipment."
                },
                ["R-454B"] = new RefrigerantInfo
                {
                    FullName = "R-454B (Difluoromethane/Trifluoroiodomethane blend)",
                    SafetyClass = "A2L (Non-toxic, Mildly flammable)",
                    TypicalUse = "R-410A replacement in new commercial equipment",
                    GWP = 466,
                    ODP = 0,
                    OperatingRange = "-48°F to 120°F",
                    Notes = "Low GWP R-410A alternative. Similar performance characteristics. Requires A2L-rated equipment."
                }
            };

            return refrigerantProperties.GetValueOrDefault(refrigerantType, new RefrigerantInfo
            {
                FullName = refrigerantType,
                SafetyClass = "Check safety data sheet",
                TypicalUse = "Commercial/industrial applications",
                Notes = "Verify refrigerant specifications with manufacturer data"
            });
        }

        private double ConvertToGaugePressure(double pressure, string unit)
        {
            return unit switch
            {
                "PSIA" => pressure - 14.7,
                "PSIG" => pressure,
                _ => pressure
            };
        }

        private List<string> GenerateOperatingNotes(double temperature, double pressure, RefrigerantInfo refInfo)
        {
            var notes = new List<string>();

            // Temperature-based notes
            if (temperature < 0)
                notes.Add("Low temperature operation - check for proper oil return");
            else if (temperature > 100)
                notes.Add("High temperature operation - verify condenser performance");

            // Pressure-based notes
            if (pressure > 400)
                notes.Add("High pressure - ensure system components are rated for operating pressure");
            else if (pressure < 20)
                notes.Add("Low pressure - may indicate undercharge or low ambient conditions");

            // Refrigerant-specific notes
            if (refInfo.GWP > 2000)
                notes.Add("High GWP refrigerant - consider future replacement planning");

            if (refInfo.SafetyClass.Contains("A2L"))
                notes.Add("A2L refrigerant - follow proper handling and leak detection protocols");

            if (refInfo.FullName.Contains("R-22"))
                notes.Add("R-22 system - minimize refrigerant releases, consider replacement timing");

            return notes;
        }

        private RPTResults CreateErrorResult(string errorMessage)
        {
            return new RPTResults
            {
                IsValidLookup = false,
                RefrigerantType = Inputs.RefrigerantType,
                LookupMethod = errorMessage,
                DataQuality = "Error"
            };
        }

        private bool ShouldAutoLookup()
        {
            if (Inputs.LookupMode == "Pressure to Temperature")
            {
                return Inputs.InputPressure > 0;
            }
            else
            {
                return Math.Abs(Inputs.InputTemperature) < 200; // Reasonable temperature range
            }
        }

        #endregion

        #region UI Helper Methods

        protected string GetRefrigerantDescription()
        {
            var refInfo = GetRefrigerantInfo(Inputs.RefrigerantType);
            return $"{refInfo.SafetyClass} - {refInfo.TypicalUse}";
        }

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

        protected string GetSystemContextAdvice()
        {
            if (Results == null || !Results.IsValidLookup) return string.Empty;

            var advice = Inputs.SystemType switch
            {
                "Rooftop Unit" => "Typical condenser saturation temperature. Check ambient conditions and condenser cleanliness.",
                "Split System" => "Monitor for proper refrigerant charge and TXV operation.",
                "Chiller" => "Precision temperature control critical. Verify water temperatures and flow rates.",
                "Heat Pump" => "Check operation in both heating and cooling modes.",
                "VRF System" => "Multi-zone system - verify individual zone operation.",
                "Process Cooling" => "Maintain stable temperatures for process requirements.",
                _ => "Verify system-specific operating parameters."
            };

            return advice;
        }

        #endregion
    }
}

