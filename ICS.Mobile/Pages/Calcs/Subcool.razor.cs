using Microsoft.AspNetCore.Components;

using static ICS.Mobile.DataModels.Custom.HVACCalculations;


namespace ICS.Mobile.Pages.Subcool
{
    public partial class SubCoolCalcPage : PageBase
    {
        protected SubcoolInputs Inputs { get; set; } = new();
        protected SubcoolResults? Results { get; set; }

        public void HomePageClick()
        {
            PageNavManager.NavigateTo("/calcmenu");
        }
        protected override void OnInitialized()
        {
            // Initialize with realistic commercial scenario - 15 ton rooftop unit
            Inputs = new SubcoolInputs
            {
                SystemCapacity = 180000, // 15 ton commercial
                SystemType = "Packaged Rooftop Unit",
                RefrigerantType = "R-410A",
                ApplicationType = "Office Building",
                SystemAge = 4,
                MeasurementMethod = "Pressure and Temperature",
                CondensingPressure = 278, // PSIG - typical for R-410A at 105°F
                PressureUnit = "PSIG",
                LiquidLineTemp = 93, // °F - giving us about 12°F subcool
                CondensingTemp = 105,
                TemperatureUnit = "Fahrenheit",
                AmbientTemp = 95,
                IndoorTemp = 75,
                SupplyAirTemp = 55,
                ReturnAirTemp = 75,
                EvaporatorPressure = 118,
                SuperheatValue = 12,
                HasReceiver = true,
                IsChillerSystem = false,
                IsHeatPumpMode = false,
                CondenserType = "Air Cooled",
                CondenserApproach = 10
            };
        }

        protected void OnInputChanged()
        {
            // Auto-calculate when critical inputs change
            if (ShouldAutoCalculate())
            {
                CalculateSubcool();
            }
        }

        protected void CalculateSubcool()
        {
            try
            {
                var systemTons = Inputs.SystemCapacity / 12000.0;
                var systemSizeCategory = GetSystemSizeCategory(systemTons);

                double bubblePointTemp;
                double actualLiquidTemp = Inputs.LiquidLineTemp;
                double pressurePSIA;

                // Calculate bubble point temperature based on measurement method
                if (Inputs.MeasurementMethod == "Pressure and Temperature")
                {
                    // Convert pressure to PSIA
                    pressurePSIA = ConvertPressureToPSIA(Inputs.CondensingPressure, Inputs.PressureUnit);

                    // Look up saturation temperature from pressure
                    bubblePointTemp = GetSaturationTemperature(Inputs.RefrigerantType, pressurePSIA);
                }
                else
                {
                    // Use provided condensing temperature
                    bubblePointTemp = Inputs.CondensingTemp;
                    pressurePSIA = GetSaturationPressure(Inputs.RefrigerantType, bubblePointTemp);
                }

                // Calculate subcooling
                var calculatedSubcooling = bubblePointTemp - actualLiquidTemp;

                // Get optimal subcooling ranges
                var optimalRanges = GetOptimalSubcoolingRange(Inputs.SystemType, systemTons);
                var subcoolingEfficiency = CalculateSubcoolingEfficiency(calculatedSubcooling, optimalRanges);

                // Assess charge level and performance impact
                var chargeAssessment = AssessChargeLevel(calculatedSubcooling, optimalRanges, systemTons);
                var performanceImpact = CalculatePerformanceImpact(calculatedSubcooling, optimalRanges, systemTons);

                // Run diagnostic algorithms
                var diagnostics = RunSubcoolDiagnostics(calculatedSubcooling, optimalRanges, systemTons, bubblePointTemp);

                // Calculate cost impacts
                var costImpacts = CalculateCostImpacts(calculatedSubcooling, optimalRanges, systemTons, performanceImpact);

                // Generate recommendations
                var recommendedAction = GenerateRecommendations(calculatedSubcooling, optimalRanges, diagnostics, systemTons);

                // System health scoring
                var systemHealth = CalculateSystemHealth(calculatedSubcooling, optimalRanges, diagnostics, systemTons);

                // Check for warnings
                var warnings = GenerateSystemWarnings(calculatedSubcooling, optimalRanges, systemTons, Inputs);

                Results = new SubcoolResults
                {
                    CalculatedSubcooling = calculatedSubcooling,
                    BubblePointTemperature = bubblePointTemp,
                    ActualLiquidTemp = actualLiquidTemp,
                    CondensingPressurePSIA = pressurePSIA,
                    SystemTons = systemTons,
                    SystemSizeCategory = systemSizeCategory,
                    OptimalSubcoolingLow = optimalRanges.MinSubcool,
                    OptimalSubcoolingHigh = optimalRanges.MaxSubcool,
                    SubcoolingEfficiency = subcoolingEfficiency,
                    CapacityImpact = performanceImpact.CapacityImpact,
                    EfficiencyImpact = performanceImpact.EfficiencyImpact,
                    EstimatedChargeLevel = chargeAssessment.ChargeLevel,
                    ChargeStatus = chargeAssessment.Status,
                    PrimaryDiagnostic = diagnostics.FirstOrDefault() ?? CreateNormalDiagnostic(),
                    AllDiagnostics = diagnostics,
                    AnnualEnergyImpact = costImpacts.AnnualEnergyCost,
                    CapacityLoss = performanceImpact.CapacityLoss,
                    RefrigerantCostImpact = costImpacts.RefrigerantCost,
                    RequiresImmediateAttention = diagnostics.Any(d => d.Severity >= 8),
                    RecommendedAction = recommendedAction,
                    SystemWarnings = warnings,
                    SystemHealthScore = systemHealth
                };

                StateHasChanged();
            }
            catch (Exception ex)
            {
                Results = null;
                Console.WriteLine($"Subcool calculation error: {ex.Message}");
            }
        }

        #region Pressure-Temperature Calculations

        private double ConvertPressureToPSIA(double pressure, string unit)
        {
            return unit switch
            {
                "PSIG" => pressure + 14.7, // Convert gauge to absolute
                "PSIA" => pressure,
                "kPa" => pressure * 0.145038, // Convert kPa to PSIA
                _ => pressure + 14.7 // Default to PSIG
            };
        }

        private double GetSaturationTemperature(string refrigerant, double pressurePSIA)
        {
            var ptData = new RefrigerantPTData();
            if (!ptData.RefrigerantData.ContainsKey(refrigerant))
                return 0; // Unknown refrigerant

            var dataPoints = ptData.RefrigerantData[refrigerant];

            // Find the temperature using linear interpolation
            return InterpolateTemperatureFromPressure(dataPoints, pressurePSIA);
        }

        private double GetSaturationPressure(string refrigerant, double temperatureF)
        {
            var ptData = new RefrigerantPTData();
            if (!ptData.RefrigerantData.ContainsKey(refrigerant))
                return 0; // Unknown refrigerant

            var dataPoints = ptData.RefrigerantData[refrigerant];

            // Find the pressure using linear interpolation
            return InterpolatePressureFromTemperature(dataPoints, temperatureF);
        }

        private double InterpolateTemperatureFromPressure(List<PressureTemperaturePoint> dataPoints, double pressurePSIA)
        {
            // Handle out-of-range values
            if (pressurePSIA <= dataPoints.First().PressurePSIA)
                return dataPoints.First().TempF;
            if (pressurePSIA >= dataPoints.Last().PressurePSIA)
                return dataPoints.Last().TempF;

            // Find surrounding points
            for (int i = 0; i < dataPoints.Count - 1; i++)
            {
                var p1 = dataPoints[i];
                var p2 = dataPoints[i + 1];

                if (pressurePSIA >= p1.PressurePSIA && pressurePSIA <= p2.PressurePSIA)
                {
                    // Linear interpolation
                    var ratio = (pressurePSIA - p1.PressurePSIA) / (p2.PressurePSIA - p1.PressurePSIA);
                    return p1.TempF + ratio * (p2.TempF - p1.TempF);
                }
            }

            return dataPoints.First().TempF; // Fallback
        }

        private double InterpolatePressureFromTemperature(List<PressureTemperaturePoint> dataPoints, double temperatureF)
        {
            // Handle out-of-range values
            if (temperatureF <= dataPoints.First().TempF)
                return dataPoints.First().PressurePSIA;
            if (temperatureF >= dataPoints.Last().TempF)
                return dataPoints.Last().PressurePSIA;

            // Find surrounding points
            for (int i = 0; i < dataPoints.Count - 1; i++)
            {
                var p1 = dataPoints[i];
                var p2 = dataPoints[i + 1];

                if (temperatureF >= p1.TempF && temperatureF <= p2.TempF)
                {
                    // Linear interpolation
                    var ratio = (temperatureF - p1.TempF) / (p2.TempF - p1.TempF);
                    return p1.PressurePSIA + ratio * (p2.PressurePSIA - p1.PressurePSIA);
                }
            }

            return dataPoints.First().PressurePSIA; // Fallback
        }

        #endregion

        #region Commercial Analysis Methods

        private SubcoolRange GetOptimalSubcoolingRange(string systemType, double systemTons)
        {
            var standards = new CommercialSubcoolStandards();

            // First try system type specific
            if (standards.StandardsBySystemType.ContainsKey(systemType))
            {
                return standards.StandardsBySystemType[systemType];
            }

            // Fall back to capacity-based standards
            var sizeCategory = GetSystemSizeCategory(systemTons);
            if (standards.StandardsByCapacity.ContainsKey(sizeCategory))
            {
                return standards.StandardsByCapacity[sizeCategory];
            }

            // Default commercial range
            return new SubcoolRange { MinSubcool = 8, OptimalSubcool = 12, MaxSubcool = 18 };
        }

        private double CalculateSubcoolingEfficiency(double actualSubcool, SubcoolRange optimalRange)
        {
            if (actualSubcool < 0) return 0; // Invalid subcooling

            // Calculate efficiency based on how close to optimal range
            if (actualSubcool >= optimalRange.MinSubcool && actualSubcool <= optimalRange.MaxSubcool)
            {
                // Within range - calculate how close to optimal
                var distanceFromOptimal = Math.Abs(actualSubcool - optimalRange.OptimalSubcool);
                var rangeSpread = (optimalRange.MaxSubcool - optimalRange.MinSubcool) / 2;
                return Math.Max(85, 100 - (distanceFromOptimal / rangeSpread * 15));
            }
            else if (actualSubcool < optimalRange.MinSubcool)
            {
                // Below range - efficiency drops quickly
                var deficiency = optimalRange.MinSubcool - actualSubcool;
                return Math.Max(0, 85 - (deficiency * 8)); // 8% per degree below
            }
            else
            {
                // Above range - efficiency drops gradually
                var excess = actualSubcool - optimalRange.MaxSubcool;
                return Math.Max(50, 85 - (excess * 3)); // 3% per degree above
            }
        }

        private (double ChargeLevel, string Status) AssessChargeLevel(double actualSubcool, SubcoolRange optimalRange, double systemTons)
        {
            var chargeLevel = 100.0; // Start at 100%

            if (actualSubcool < optimalRange.MinSubcool)
            {
                // Low subcooling indicates undercharge
                var deficiency = optimalRange.MinSubcool - actualSubcool;
                chargeLevel = Math.Max(60, 100 - (deficiency * 4)); // 4% per degree low

                return (chargeLevel, actualSubcool < optimalRange.MinSubcool / 2 ? "Severely Undercharged" : "Undercharged");
            }
            else if (actualSubcool > optimalRange.MaxSubcool)
            {
                // High subcooling indicates overcharge
                var excess = actualSubcool - optimalRange.MaxSubcool;
                chargeLevel = Math.Min(130, 100 + (excess * 2)); // 2% per degree high

                return (chargeLevel, excess > 10 ? "Severely Overcharged" : "Overcharged");
            }
            else
            {
                return (chargeLevel, "Optimal");
            }
        }

        private (double CapacityImpact, double EfficiencyImpact, double CapacityLoss) CalculatePerformanceImpact(double actualSubcool, SubcoolRange optimalRange, double systemTons)
        {
            double capacityImpact = 0;
            double efficiencyImpact = 0;

            if (actualSubcool < optimalRange.MinSubcool)
            {
                // Undercharged - capacity and efficiency loss
                var deficiency = optimalRange.MinSubcool - actualSubcool;
                capacityImpact = Math.Max(-25, -deficiency * 1.5); // 1.5% capacity loss per degree
                efficiencyImpact = Math.Max(-20, -deficiency * 1.2); // 1.2% efficiency loss per degree
            }
            else if (actualSubcool > optimalRange.MaxSubcool)
            {
                // Overcharged - efficiency loss, some capacity loss
                var excess = actualSubcool - optimalRange.MaxSubcool;
                capacityImpact = Math.Max(-15, -excess * 0.8); // 0.8% capacity loss per degree
                efficiencyImpact = Math.Max(-30, -excess * 2.0); // 2.0% efficiency loss per degree
            }

            var capacityLoss = Math.Abs(capacityImpact) * Inputs.SystemCapacity / 100.0;

            return (capacityImpact, efficiencyImpact, capacityLoss);
        }

        private List<SubcoolDiagnosticResult> RunSubcoolDiagnostics(double actualSubcool, SubcoolRange optimalRange, double systemTons, double bubblePointTemp)
        {
            var diagnostics = new List<SubcoolDiagnosticResult>();

            // Critical subcooling issues
            if (actualSubcool < 2)
            {
                diagnostics.Add(new SubcoolDiagnosticResult
                {
                    Id = "critically-low-subcool",
                    Title = "Critically Low Subcooling",
                    Status = "red",
                    Description = $"Subcooling of {actualSubcool:F1}°F is critically low for commercial system. Risk of liquid flashing and TXV hunting.",
                    ConfidenceLevel = 95,
                    Severity = 9,
                    Category = "Critical",
                    AffectsCapacity = true,
                    AffectsEfficiency = true,
                    RequiresServiceCall = true,
                    BusinessImpact = "Equipment damage risk, poor cooling performance, high energy costs",
                    EstimatedCost = GetServiceCost("System Evacuation and Recharge", systemTons),
                    Symptoms = new List<string> { "TXV hunting", "Inconsistent cooling", "High superheat", "Compressor short cycling" },
                    PossibleCauses = new List<string> { "Major refrigerant leak", "Undercharged system", "Metering device issues" },
                    RecommendedActions = new List<string> { "Immediate leak detection", "System recharge", "Check TXV operation", "Pressure test system" }
                });
            }
            else if (actualSubcool < optimalRange.MinSubcool)
            {
                var severity = actualSubcool < optimalRange.MinSubcool / 2 ? 8 : 6;
                diagnostics.Add(new SubcoolDiagnosticResult
                {
                    Id = "low-subcool",
                    Title = $"Low Subcooling - {GetSystemSizeCategory(systemTons)}",
                    Status = severity >= 8 ? "red" : "yellow",
                    Description = $"Subcooling of {actualSubcool:F1}°F is below optimal range ({optimalRange.MinSubcool:F0}-{optimalRange.MaxSubcool:F0}°F) for {Inputs.SystemType}.",
                    ConfidenceLevel = 90,
                    Severity = severity,
                    Category = "Charge",
                    AffectsCapacity = true,
                    AffectsEfficiency = true,
                    RequiresServiceCall = severity >= 8,
                    BusinessImpact = "Reduced cooling capacity and higher energy costs",
                    EstimatedCost = GetServiceCost("Basic Charge Adjustment", systemTons),
                    Symptoms = new List<string> { "Reduced cooling capacity", "Higher than normal head pressure", "TXV instability" },
                    PossibleCauses = new List<string> { "Refrigerant leak", "Undercharged system", "Oversized TXV" },
                    RecommendedActions = new List<string> { "Check for leaks", "Add refrigerant as needed", "Verify TXV sizing" }
                });
            }
            else if (actualSubcool > optimalRange.MaxSubcool + 10)
            {
                diagnostics.Add(new SubcoolDiagnosticResult
                {
                    Id = "high-subcool",
                    Title = "Excessive Subcooling",
                    Status = "red",
                    Description = $"Subcooling of {actualSubcool:F1}°F is excessively high, indicating overcharge or restriction.",
                    ConfidenceLevel = 90,
                    Severity = 8,
                    Category = "Charge",
                    AffectsCapacity = true,
                    AffectsEfficiency = true,
                    RequiresServiceCall = true,
                    BusinessImpact = "High head pressure, compressor damage risk, poor efficiency",
                    EstimatedCost = GetServiceCost("Basic Charge Adjustment", systemTons),
                    Symptoms = new List<string> { "Very high head pressure", "High energy consumption", "Reduced capacity" },
                    PossibleCauses = new List<string> { "Overcharged system", "Restricted TXV", "Restricted liquid line" },
                    RecommendedActions = new List<string> { "Check refrigerant charge", "Inspect TXV", "Check for restrictions" }
                });
            }
            else if (actualSubcool > optimalRange.MaxSubcool)
            {
                diagnostics.Add(new SubcoolDiagnosticResult
                {
                    Id = "elevated-subcool",
                    Title = "Elevated Subcooling",
                    Status = "yellow",
                    Description = $"Subcooling of {actualSubcool:F1}°F is above optimal range, indicating possible overcharge.",
                    ConfidenceLevel = 80,
                    Severity = 5,
                    Category = "Charge",
                    AffectsEfficiency = true,
                    BusinessImpact = "Slightly reduced efficiency and higher operating costs",
                    EstimatedCost = GetServiceCost("Basic Charge Adjustment", systemTons),
                    Symptoms = new List<string> { "Slightly high head pressure", "Higher energy consumption" },
                    PossibleCauses = new List<string> { "Slightly overcharged", "High ambient conditions" },
                    RecommendedActions = new List<string> { "Verify charge level", "Check operating conditions" }
                });
            }

            // Environmental condition checks
            if (Inputs.AmbientTemp > 100 && actualSubcool < optimalRange.OptimalSubcool)
            {
                diagnostics.Add(new SubcoolDiagnosticResult
                {
                    Id = "high-ambient-low-subcool",
                    Title = "Inadequate Subcooling for High Ambient",
                    Status = "yellow",
                    Description = $"At {Inputs.AmbientTemp:F0}°F ambient, subcooling should be higher for optimal performance.",
                    ConfidenceLevel = 75,
                    Severity = 6,
                    Category = "Performance",
                    AffectsEfficiency = true,
                    BusinessImpact = "Poor performance in hot weather conditions",
                    Symptoms = new List<string> { "Poor cooling on hot days", "High energy consumption" },
                    RecommendedActions = new List<string> { "Check refrigerant charge", "Verify condenser operation" }
                });
            }

            // System-specific diagnostics
            if (Inputs.IsChillerSystem && actualSubcool > 15)
            {
                diagnostics.Add(new SubcoolDiagnosticResult
                {
                    Id = "chiller-high-subcool",
                    Title = "High Subcooling for Chiller System",
                    Status = "yellow",
                    Description = "Chiller systems typically operate with lower subcooling (5-12°F) for optimal efficiency.",
                    ConfidenceLevel = 85,
                    Severity = 5,
                    Category = "Efficiency",
                    AffectsEfficiency = true,
                    BusinessImpact = "Reduced chiller efficiency",
                    RecommendedActions = new List<string> { "Review charge level", "Check for restrictions", "Verify design parameters" }
                });
            }

            return diagnostics.OrderByDescending(d => d.Severity).ToList();
        }

        private (double AnnualEnergyCost, double RefrigerantCost) CalculateCostImpacts(double actualSubcool, SubcoolRange optimalRange, double systemTons, (double CapacityImpact, double EfficiencyImpact, double CapacityLoss) performanceImpact)
        {
            var annualEnergyCost = 0.0;
            var refrigerantCost = 0.0;

            // Calculate annual energy cost impact
            if (Math.Abs(performanceImpact.EfficiencyImpact) > 1)
            {
                var baseAnnualCost = GetBaseAnnualCost(systemTons);
                annualEnergyCost = baseAnnualCost * Math.Abs(performanceImpact.EfficiencyImpact) / 100.0;
            }

            // Calculate refrigerant cost for adjustment
            if (actualSubcool < optimalRange.MinSubcool || actualSubcool > optimalRange.MaxSubcool)
            {
                var costFactors = new SubcoolCostFactors();
                var refrigerantCostPerLb = costFactors.RefrigerantCostPerPound.GetValueOrDefault(Inputs.RefrigerantType, 15.0);
                var sizeCategory = GetSystemSizeCategory(systemTons);
                var typicalCharge = costFactors.TypicalChargeBySize.GetValueOrDefault(sizeCategory, 50);

                // Estimate percentage of charge that needs adjustment
                var chargeAdjustmentPercent = Math.Abs(actualSubcool - optimalRange.OptimalSubcool) * 2; // 2% per degree off
                chargeAdjustmentPercent = Math.Min(chargeAdjustmentPercent, 30); // Max 30% adjustment

                refrigerantCost = typicalCharge * chargeAdjustmentPercent / 100.0 * refrigerantCostPerLb;
            }

            return (annualEnergyCost, refrigerantCost);
        }

        private string GenerateRecommendations(double actualSubcool, SubcoolRange optimalRange, List<SubcoolDiagnosticResult> diagnostics, double systemTons)
        {
            var primaryDiagnostic = diagnostics.FirstOrDefault();
            if (primaryDiagnostic != null && primaryDiagnostic.RecommendedActions.Any())
            {
                return primaryDiagnostic.RecommendedActions.First();
            }

            if (actualSubcool < optimalRange.MinSubcool)
            {
                return "Check for refrigerant leaks and add refrigerant as needed to achieve proper subcooling.";
            }
            else if (actualSubcool > optimalRange.MaxSubcool)
            {
                return "Check for overcharge condition and remove excess refrigerant to optimize subcooling.";
            }
            else
            {
                return "Subcooling is within acceptable range. Continue regular maintenance schedule.";
            }
        }

        private double CalculateSystemHealth(double actualSubcool, SubcoolRange optimalRange, List<SubcoolDiagnosticResult> diagnostics, double systemTons)
        {
            var baseHealth = 100.0;

            // Reduce health based on subcooling deviation
            if (actualSubcool < optimalRange.MinSubcool)
            {
                var deficiency = optimalRange.MinSubcool - actualSubcool;
                baseHealth -= deficiency * 5; // 5 points per degree low
            }
            else if (actualSubcool > optimalRange.MaxSubcool)
            {
                var excess = actualSubcool - optimalRange.MaxSubcool;
                baseHealth -= excess * 3; // 3 points per degree high
            }

            // Factor in diagnostic severity
            foreach (var diagnostic in diagnostics)
            {
                baseHealth -= diagnostic.Severity * 2;
            }

            // Age and system size factors
            if (Inputs.SystemAge > 10) baseHealth -= 5;
            if (systemTons > 50 && diagnostics.Any(d => d.Severity >= 7)) baseHealth -= 10; // Large systems are more critical

            return Math.Max(0, Math.Min(100, baseHealth));
        }

        private List<string> GenerateSystemWarnings(double actualSubcool, SubcoolRange optimalRange, double systemTons, SubcoolInputs inputs)
        {
            var warnings = new List<string>();

            // R-22 system warnings
            if (inputs.RefrigerantType == "R-22" && inputs.SystemAge > 8)
            {
                warnings.Add("R-22 systems require careful charge management due to refrigerant costs and availability.");
            }

            // Large system warnings
            if (systemTons > 30 && Math.Abs(actualSubcool - optimalRange.OptimalSubcool) > 5)
            {
                warnings.Add("Large commercial systems require precise subcooling for optimal performance and efficiency.");
            }

            // Chiller-specific warnings
            if (inputs.IsChillerSystem && actualSubcool > 12)
            {
                warnings.Add("Chiller systems are sensitive to overcharge conditions - monitor subcooling closely.");
            }

            // Heat pump warnings
            if (inputs.IsHeatPumpMode)
            {
                warnings.Add("Heat pump mode subcooling values may differ from cooling mode - verify mode of operation.");
            }

            // Environmental warnings
            if (inputs.AmbientTemp > 105)
            {
                warnings.Add("High ambient temperatures may require higher subcooling for proper TXV operation.");
            }

            return warnings;
        }

        private SubcoolDiagnosticResult CreateNormalDiagnostic()
        {
            return new SubcoolDiagnosticResult
            {
                Id = "normal-subcool",
                Title = "Optimal Subcooling",
                Status = "green",
                Description = "Subcooling is within the optimal range for this commercial system type.",
                ConfidenceLevel = 90,
                Severity = 0,
                Category = "Normal",
                BusinessImpact = "System operating at optimal efficiency"
            };
        }

        #endregion

        #region Helper Methods

        private bool ShouldAutoCalculate()
        {
            if (Inputs.MeasurementMethod == "Pressure and Temperature")
            {
                return Inputs.CondensingPressure > 0 && Inputs.LiquidLineTemp > 0;
            }
            else
            {
                return Inputs.CondensingTemp > 0 && Inputs.LiquidLineTemp > 0;
            }
        }

        private string GetSystemSizeCategory(double systemTons = 0)
        {
            if (systemTons == 0) systemTons = Inputs.SystemCapacity / 12000.0;

            return systemTons switch
            {
                <= 10 => "Small Commercial (2-10 tons)",
                <= 30 => "Medium Commercial (10-30 tons)",
                <= 75 => "Large Commercial (30-75 tons)",
                _ => "Industrial (75+ tons)"
            };
        }

        private double GetBaseAnnualCost(double systemTons)
        {
            var sizeCategory = GetSystemSizeCategory(systemTons);
            var costPerTon = sizeCategory switch
            {
                "Small Commercial (2-10 tons)" => 1200,
                "Medium Commercial (10-30 tons)" => 1000,
                "Large Commercial (30-75 tons)" => 900,
                "Industrial (75+ tons)" => 800,
                _ => 1000
            };

            return systemTons * costPerTon;
        }

        private double GetServiceCost(string serviceType, double systemTons)
        {
            var costFactors = new SubcoolCostFactors();
            var baseCost = costFactors.ServiceCallCosts.GetValueOrDefault(serviceType, 500);

            // Scale cost by system size
            var sizeMultiplier = systemTons switch
            {
                <= 10 => 1.0,
                <= 30 => 1.5,
                <= 75 => 2.5,
                _ => 4.0
            };

            return baseCost * sizeMultiplier;
        }

        protected string GetRefrigerantDescription()
        {
            return Inputs.RefrigerantType switch
            {
                "R-410A" => "Standard commercial refrigerant - HFC blend",
                "R-22" => "Legacy refrigerant - being phased out, expensive",
                "R-134a" => "Common in chiller applications - HFC",
                "R-407C" => "R-22 replacement - HFC blend",
                "R-32" => "High efficiency, mildly flammable (A2L)",
                "R-454B" => "Low GWP R-410A replacement - A2L refrigerant",
                "R-513A" => "Low GWP R-134a replacement - A1 refrigerant",
                _ => "Commercial refrigerant"
            };
        }

        #endregion

        #region UI Helper Methods

        protected string GetSubcoolHeaderClass()
        {
            if (Results == null) return "bg-secondary";

            var optimalRange = GetOptimalSubcoolingRange(Inputs.SystemType, Results.SystemTons);
            var subcool = Results.CalculatedSubcooling;

            if (subcool < 2 || subcool > optimalRange.MaxSubcool + 10)
                return "bg-danger";
            else if (subcool < optimalRange.MinSubcool || subcool > optimalRange.MaxSubcool)
                return "bg-warning";
            else
                return "bg-success";
        }

        protected string GetSubcoolProgressClass()
        {
            if (Results == null) return "bg-secondary";
            return Results.SubcoolingEfficiency switch
            {
                >= 85 => "bg-success",
                >= 70 => "bg-warning",
                _ => "bg-danger"
            };
        }

        protected RenderFragment GetSubcoolIcon()
        {
            if (Results == null) return builder => { };

            var status = Results.PrimaryDiagnostic.Status;
            return status switch
            {
                "green" => builder => builder.AddMarkupContent(0, "<svg class=\"me-2\" width=\"20\" height=\"20\" fill=\"currentColor\" viewBox=\"0 0 24 24\"><path d=\"M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-2 15l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z\"/></svg>"),
                "yellow" => builder => builder.AddMarkupContent(0, "<svg class=\"me-2\" width=\"20\" height=\"20\" fill=\"currentColor\" viewBox=\"0 0 24 24\"><path d=\"M1 21h22L12 2 1 21zm12-3h-2v-2h2v2zm0-4h-2v-4h2v4z\"/></svg>"),
                "red" => builder => builder.AddMarkupContent(0, "<svg class=\"me-2\" width=\"20\" height=\"20\" fill=\"currentColor\" viewBox=\"0 0 24 24\"><path d=\"M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm5 11H7v-2h10v2z\"/></svg>"),
                _ => builder => builder.AddMarkupContent(0, "<svg class=\"me-2\" width=\"20\" height=\"20\" fill=\"currentColor\" viewBox=\"0 0 24 24\"><circle cx=\"12\" cy=\"12\" r=\"10\"/></svg>")
            };
        }

        protected string GetDiagnosticHeaderClass(string status)
        {
            return status switch
            {
                "red" => "bg-danger",
                "yellow" => "bg-warning",
                "green" => "bg-success",
                _ => "bg-secondary"
            };
        }

        protected RenderFragment GetDiagnosticIcon(string status)
        {
            return status switch
            {
                "red" => builder => builder.AddMarkupContent(0, "<svg class=\"me-2\" width=\"20\" height=\"20\" fill=\"currentColor\" viewBox=\"0 0 24 24\"><path d=\"M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm5 11H7v-2h10v2z\"/></svg>"),
                "yellow" => builder => builder.AddMarkupContent(0, "<svg class=\"me-2\" width=\"20\" height=\"20\" fill=\"currentColor\" viewBox=\"0 0 24 24\"><path d=\"M1 21h22L12 2 1 21zm12-3h-2v-2h2v2zm0-4h-2v-4h2v4z\"/></svg>"),
                "green" => builder => builder.AddMarkupContent(0, "<svg class=\"me-2\" width=\"20\" height=\"20\" fill=\"currentColor\" viewBox=\"0 0 24 24\"><path d=\"M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-2 15l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z\"/></svg>"),
                _ => builder => builder.AddMarkupContent(0, "<svg class=\"me-2\" width=\"20\" height=\"20\" fill=\"currentColor\" viewBox=\"0 0 24 24\"><circle cx=\"12\" cy=\"12\" r=\"10\"/></svg>")
            };
        }

        protected string GetHealthColorClass()
        {
            if (Results == null) return "text-secondary";
            return Results.SystemHealthScore switch
            {
                >= 80 => "text-success",
                >= 60 => "text-warning",
                _ => "text-danger"
            };
        }

        #endregion
    }
}