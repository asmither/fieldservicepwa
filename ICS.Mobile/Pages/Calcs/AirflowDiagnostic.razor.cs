using Microsoft.AspNetCore.Components;

using static ICS.Mobile.DataModels.Custom.HVACCalculations;


namespace ICS.Mobile.Pages.AirflowDiagnostic
{
    public partial class AirflowDiagPage : PageBase
    {
        protected AirflowDiagnosticInputs Inputs { get; set; } = new();
        protected AirflowDiagnosticResults? Results { get; set; }

        protected override void OnInitialized()
        {
            // Initialize with realistic commercial scenario - 5 ton office building unit
            Inputs = new AirflowDiagnosticInputs
            {
                SystemCapacity = 60000, // 5 ton commercial
                SystemType = "Packaged Rooftop Unit",
                ApplicationType = "Office Building",
                RefrigerantType = "R-410A",
                SystemAge = 6,
                NumberOfZones = 4,
                BuildingSquareFootage = 3000,
                OccupancySchedule = 50, // Standard office hours
                HasEconomizer = true,
                IsVAVSystem = false,
                IsConstantVolume = true,
                SupplyAirTemp = 56,
                ReturnAirTemp = 74,
                AmbientTemp = 95,
                CoilTemp = 45,
                MixedAirTemp = 78,
                StaticPressure = 1.2, // Commercial range
                VelocityPressure = 0.12,
                FilterPressureDrop = 0.25,
                CoilPressureDrop = 0.35,
                DuctworkPressureDrop = 0.6,
                BlowerAmpDraw = 11.2,
                RatedBlowerAmps = 13.5,
                SupplyVoltage = 480, // Commercial voltage
                BlowerMotorHP = 3.0,
                RuntimePercentage = 70,
                DuctLeakageEstimate = 10, // Better than residential
                FilterCondition = "Dirty",
                FilterType = "Pleated",
                CoilCondition = "Moderately Dirty",
                MinimumVentilationCFM = 600, // ASHRAE 62.1
                MeasurementMethod = "None",
                IsR22System = false
            };
        }

        protected void OnInputChanged()
        {
            // Auto-run diagnostic when critical inputs change
            if (ShouldAutoRunDiagnostic())
            {
                RunDiagnostic();
            }
        }
        public void HomePageClick()
        {
            PageNavManager.NavigateTo("/calcmenu");
        }
        protected void RunDiagnostic()
        {
            try
            {
                var systemTons = Inputs.SystemCapacity / 12000.0;
                var tempDiff = Inputs.ReturnAirTemp - Inputs.SupplyAirTemp;
                var nominalCFM = GetCommercialNominalCFM();
                var estimatedCFM = CalculateEstimatedCFM(tempDiff);
                var cfmPerTon = systemTons > 0 ? estimatedCFM / systemTons : 0;
                var cfmPerSqFt = Inputs.BuildingSquareFootage > 0 ? estimatedCFM / Inputs.BuildingSquareFootage : 0;

                // Run all commercial diagnostic algorithms
                var allDiagnostics = new List<DiagnosticResult>();

                // Core commercial airflow diagnostics
                allDiagnostics.AddRange(DiagnoseCommercialAirflowIssues(cfmPerTon, estimatedCFM, nominalCFM, systemTons));
                allDiagnostics.AddRange(DiagnoseCommercialFilterIssues(systemTons));
                allDiagnostics.AddRange(DiagnoseCommercialCoilIssues(tempDiff, systemTons));
                allDiagnostics.AddRange(DiagnoseCommercialDuctIssues(systemTons));
                allDiagnostics.AddRange(DiagnoseCommercialBlowerIssues(systemTons));
                allDiagnostics.AddRange(DiagnoseCommercialLoadIssues(tempDiff, cfmPerSqFt));
                allDiagnostics.AddRange(DiagnoseVentilationCompliance(estimatedCFM));
                allDiagnostics.AddRange(DiagnoseEconomizerIssues());
                allDiagnostics.AddRange(DiagnoseVAVSystemIssues());

                // R-22 specific diagnostics for commercial
                if (Inputs.IsR22System || Inputs.RefrigerantType == "R-22")
                {
                    allDiagnostics.AddRange(DiagnoseCommercialR22Issues(systemTons));
                }

                // Filter and rank diagnostics by commercial importance
                var validDiagnostics = allDiagnostics
                    .Where(d => d.ConfidenceLevel >= 25) // Slightly lower threshold for commercial complexity
                    .OrderByDescending(d => d.Severity * d.ConfidenceLevel * (d.AffectsCodeCompliance ? 1.5 : 1.0))
                    .ToList();

                // Calculate commercial system health metrics
                var overallHealth = CalculateCommercialSystemHealth(validDiagnostics, cfmPerTon, systemTons);
                var airflowEfficiency = CalculateCommercialAirflowEfficiency(cfmPerTon, systemTons);
                var ventilationEfficiency = CalculateVentilationEfficiency(estimatedCFM);
                var economizerEfficiency = CalculateEconomizerEfficiency();
                var efficiencyLoss = CalculateCommercialEfficiencyLoss(validDiagnostics, systemTons);
                var annualCostImpact = CalculateCommercialAnnualCostImpact(efficiencyLoss, systemTons);
                var demandChargeImpact = CalculateDemandChargeImpact(efficiencyLoss, systemTons);
                var maintenanceCostImpact = CalculateMaintenanceCostImpact(validDiagnostics, systemTons);

                // Generate commercial repair recommendations
                var repairPriorities = GenerateCommercialRepairRecommendations(validDiagnostics, systemTons);

                Results = new AirflowDiagnosticResults
                {
                    NominalCFM = nominalCFM,
                    EstimatedCFM = estimatedCFM,
                    CFMPerTon = cfmPerTon,
                    CFMPerSquareFoot = cfmPerSqFt,
                    TemperatureDifferential = tempDiff,
                    SystemTons = systemTons,
                    VentilationEfficiency = ventilationEfficiency,
                    EconomizerEfficiency = economizerEfficiency,
                    TotalSystemEfficiency = (airflowEfficiency + ventilationEfficiency + economizerEfficiency) / 3,
                    PrimaryResult = validDiagnostics.FirstOrDefault() ?? CreateCommercialNormalResult(),
                    AllDiagnostics = validDiagnostics,
                    OverallSystemHealth = overallHealth,
                    AirflowEfficiency = airflowEfficiency,
                    EnergyEfficiencyImpact = efficiencyLoss,
                    RepairPriorities = repairPriorities,
                    EstimatedEfficiencyLoss = efficiencyLoss,
                    AnnualCostImpact = annualCostImpact,
                    DemandChargeImpact = demandChargeImpact,
                    MaintenanceCostImpact = maintenanceCostImpact
                };

                StateHasChanged();
            }
            catch (Exception ex)
            {
                Results = null;
                Console.WriteLine($"Commercial diagnostic error: {ex.Message}");
            }
        }

        #region Commercial Diagnostic Algorithms

        private List<DiagnosticResult> DiagnoseCommercialAirflowIssues(double cfmPerTon, double estimatedCFM, double nominalCFM, double systemTons)
        {
            var diagnostics = new List<DiagnosticResult>();
            var thresholds = GetCommercialThresholds(systemTons);

            // Critical airflow issues for commercial systems
            if (cfmPerTon < thresholds.LowCFMPerTon * 0.7) // 70% of minimum
            {
                diagnostics.Add(new DiagnosticResult
                {
                    Id = "commercial-airflow-critical",
                    Title = "Critical Commercial Airflow Deficiency",
                    Status = "red",
                    Description = $"Commercial system has critically low airflow at {cfmPerTon:F0} CFM/ton for a {systemTons:F0}-ton system. This severely impacts occupant comfort, energy efficiency, and may violate building codes.",
                    ConfidenceLevel = 95,
                    Severity = 10,
                    Category = "Airflow",
                    AirflowImpact = nominalCFM - estimatedCFM,
                    AffectsOccupantComfort = true,
                    AffectsCodeCompliance = true,
                    BusinessImpact = "Significant impact on tenant comfort, energy costs, and potential code violations",
                    EstimatedDowntime = systemTons > 20 ? 8 : 4, // Larger systems need more time
                    Symptoms = new List<string> { "Poor tenant comfort", "High energy bills", "Uneven temperatures", "Excessive runtime", "Humidity issues" },
                    PossibleCauses = new List<string> { "Severely blocked air filters", "Dirty evaporator coils", "Major duct obstructions", "Failing blower motor", "Undersized ductwork" },
                    RecommendedActions = new List<string> { "Immediate filter replacement", "Professional coil cleaning", "Comprehensive duct inspection", "Motor performance testing" }
                });
            }
            else if (cfmPerTon < thresholds.LowCFMPerTon)
            {
                diagnostics.Add(new DiagnosticResult
                {
                    Id = "commercial-airflow-low",
                    Title = $"Low Airflow - {GetSystemSizeCategory()} System",
                    Status = "red",
                    Description = $"Commercial system airflow is below acceptable levels at {cfmPerTon:F0} CFM/ton. Target for {GetSystemSizeCategory().ToLower()} systems is {thresholds.OptimalCFMPerTon:F0} CFM/ton.",
                    ConfidenceLevel = 90,
                    Severity = 8,
                    Category = "Airflow",
                    AirflowImpact = nominalCFM - estimatedCFM,
                    AffectsOccupantComfort = true,
                    BusinessImpact = "Reduced tenant comfort and increased energy costs",
                    Symptoms = new List<string> { "Reduced cooling capacity", "High static pressure", "Tenant complaints", "Extended run times" },
                    PossibleCauses = new List<string> { "Dirty air filters", "Partially blocked coils", "Duct restrictions", "Belt issues" },
                    RecommendedActions = new List<string> { "Replace filters", "Clean coils", "Check ductwork", "Inspect blower components" }
                });
            }
            else if (cfmPerTon > thresholds.HighCFMPerTon)
            {
                var excessAirflow = cfmPerTon - thresholds.OptimalCFMPerTon;
                diagnostics.Add(new DiagnosticResult
                {
                    Id = "commercial-airflow-high",
                    Title = "High Airflow - Potential Efficiency Loss",
                    Status = "yellow",
                    Description = $"System airflow is {excessAirflow:F0} CFM/ton above optimal. This may indicate duct leaks, low static pressure, or oversized fan for commercial application.",
                    ConfidenceLevel = 80,
                    Severity = 5,
                    Category = "Efficiency",
                    AirflowImpact = estimatedCFM - nominalCFM,
                    BusinessImpact = "Higher than necessary energy costs and poor humidity control",
                    Symptoms = new List<string> { "Low static pressure", "Poor dehumidification", "High energy consumption", "Short cycling" },
                    PossibleCauses = new List<string> { "Duct leaks", "Oversized fan", "Low system resistance", "VFD settings" },
                    RecommendedActions = new List<string> { "Duct leakage testing", "Fan performance verification", "Static pressure analysis", "Control system check" }
                });
            }

            return diagnostics;
        }

        private List<DiagnosticResult> DiagnoseCommercialFilterIssues(double systemTons)
        {
            var diagnostics = new List<DiagnosticResult>();
            var costMultiplier = GetCommercialCostMultiplier(systemTons);

            if (Inputs.FilterCondition == "Extremely Dirty" || Inputs.FilterPressureDrop > GetMaxFilterDrop(systemTons))
            {
                diagnostics.Add(new DiagnosticResult
                {
                    Id = "commercial-filter-critical",
                    Title = "Critical Filter Blockage - Commercial System",
                    Status = "red",
                    Description = $"Commercial air filters are severely restricting airflow in this {systemTons:F0}-ton system. Immediate replacement required to prevent equipment damage.",
                    ConfidenceLevel = 95,
                    Severity = 9,
                    Category = "Airflow",
                    AffectsOccupantComfort = true,
                    BusinessImpact = "Equipment damage risk, poor air quality, tenant complaints",
                    EstimatedDowntime = 2,
                    Symptoms = new List<string> { "Very high filter pressure drop", "Reduced airflow", "Poor indoor air quality", "Equipment strain" },
                    PossibleCauses = new List<string> { "Overdue filter maintenance", "Wrong filter type", "Excessive dust loading", "Poor maintenance schedule" },
                    RecommendedActions = new List<string> { "Immediate filter replacement", "Review maintenance schedule", "Consider higher-efficiency filters", "Install pressure monitoring" }
                });
            }
            else if (Inputs.FilterCondition == "Dirty" || Inputs.FilterPressureDrop > GetMaxFilterDrop(systemTons) * 0.7)
            {
                diagnostics.Add(new DiagnosticResult
                {
                    Id = "commercial-filter-dirty",
                    Title = "Commercial Filter Maintenance Required",
                    Status = "yellow",
                    Description = $"Commercial filters need replacement to maintain optimal performance in {systemTons:F0}-ton system.",
                    ConfidenceLevel = 85,
                    Severity = 4,
                    Category = "Maintenance",
                    BusinessImpact = "Gradual increase in energy costs and decreased air quality",
                    Symptoms = new List<string> { "Elevated filter pressure drop", "Gradual airflow reduction" },
                    RecommendedActions = new List<string> { "Schedule filter replacement", "Review filter specifications", "Consider automated monitoring" }
                });
            }

            // Filter type appropriateness for commercial application
            if (Inputs.ApplicationType == "Healthcare" && Inputs.FilterType != "HEPA")
            {
                diagnostics.Add(new DiagnosticResult
                {
                    Id = "healthcare-filter-inadequate",
                    Title = "Inadequate Filtration for Healthcare",
                    Status = "red",
                    Description = "Healthcare facilities typically require HEPA filtration for infection control.",
                    ConfidenceLevel = 90,
                    Severity = 8,
                    Category = "Compliance",
                    AffectsCodeCompliance = true,
                    BusinessImpact = "Potential health code violations and infection control issues",
                    RecommendedActions = new List<string> { "Upgrade to HEPA filtration", "Consult infection control guidelines", "Professional air quality assessment" }
                });
            }

            return diagnostics;
        }

        private List<DiagnosticResult> DiagnoseCommercialCoilIssues(double tempDiff, double systemTons)
        {
            var diagnostics = new List<DiagnosticResult>();

            if (Inputs.CoilCondition == "Very Dirty" || Inputs.CoilPressureDrop > GetMaxCoilDrop(systemTons))
            {
                diagnostics.Add(new DiagnosticResult
                {
                    Id = "commercial-coil-fouled",
                    Title = $"Severely Fouled Evaporator Coil - {systemTons:F0} Ton System",
                    Status = "red",
                    Description = $"Commercial evaporator coil is severely restricting airflow and reducing capacity. Cleaning required to restore {(systemTons * 12000):F0} BTU/hr capacity.",
                    ConfidenceLevel = 90,
                    Severity = 8,
                    Category = "Efficiency",
                    AffectsOccupantComfort = true,
                    BusinessImpact = "Significant energy waste, poor comfort, potential equipment damage",
                    EstimatedDowntime = systemTons > 30 ? 12 : 6, // Larger coils take longer
                    Symptoms = new List<string> { "High coil pressure drop", "Poor cooling performance", "Ice formation risk", "High energy consumption" },
                    PossibleCauses = new List<string> { "Poor filter maintenance", "Environmental contamination", "Biological growth", "Lack of preventive maintenance" },
                    RecommendedActions = new List<string> { "Professional coil cleaning", "Improve filter maintenance", "Consider coil coatings", "Implement preventive maintenance schedule" }
                });
            }

            // Ice formation on commercial systems is more serious
            if (Inputs.IceFormation || Inputs.CoilTemp < 32)
            {
                diagnostics.Add(new DiagnosticResult
                {
                    Id = "commercial-coil-freezing",
                    Title = "Commercial Coil Freezing - System Shutdown Required",
                    Status = "red",
                    Description = $"Evaporator coil freezing in {systemTons:F0}-ton commercial system. Immediate shutdown required to prevent compressor damage.",
                    ConfidenceLevel = 98,
                    Severity = 10,
                    Category = "Critical",
                    AffectsOccupantComfort = true,
                    BusinessImpact = "Loss of cooling, potential equipment damage, business disruption",
                    EstimatedDowntime = 24, // Longer for commercial systems
                    Symptoms = new List<string> { "Ice formation", "No cooling capacity", "Water damage risk", "Compressor damage risk" },
                    PossibleCauses = new List<string> { "Severely restricted airflow", "Low refrigerant charge", "Dirty coil", "Control system failure", "TXV malfunction" },
                    RecommendedActions = new List<string> { "Immediate system shutdown", "Professional diagnostic", "Address airflow restrictions", "Refrigerant system check", "Control system inspection" }
                });
            }

            return diagnostics;
        }

        private List<DiagnosticResult> DiagnoseCommercialDuctIssues(double systemTons)
        {
            var diagnostics = new List<DiagnosticResult>();
            var thresholds = GetCommercialThresholds(systemTons);

            if (Inputs.StaticPressure > thresholds.MaxStaticPressure)
            {
                var excessPressure = Inputs.StaticPressure - thresholds.MaxStaticPressure;
                diagnostics.Add(new DiagnosticResult
                {
                    Id = "commercial-static-excessive",
                    Title = $"Excessive Static Pressure - {GetSystemSizeCategory()} System",
                    Status = "red",
                    Description = $"Static pressure of {Inputs.StaticPressure:F2}\" WC exceeds maximum for {GetSystemSizeCategory().ToLower()} systems ({thresholds.MaxStaticPressure:F1}\" WC). Fan motor overload risk.",
                    ConfidenceLevel = 95,
                    Severity = 9,
                    Category = "Airflow",
                    AirflowImpact = CalculateAirflowLossFromPressure(excessPressure, systemTons),
                    BusinessImpact = "High energy costs, equipment damage risk, poor performance",
                    EstimatedDowntime = systemTons > 50 ? 16 : 8,
                    Symptoms = new List<string> { "Very high static pressure", "Motor overload", "Poor airflow", "High energy consumption" },
                    PossibleCauses = new List<string> { "Undersized ductwork", "Multiple restrictions", "Closed dampers", "Design deficiencies" },
                    RecommendedActions = new List<string> { "Comprehensive duct analysis", "Airflow balancing", "Duct modifications", "Control system check" }
                });
            }

            // Commercial duct leakage analysis
            if (Inputs.DuctLeakageEstimate > GetMaxDuctLeakage(systemTons))
            {
                var annualCost = CalculateDuctLeakageCost(Inputs.DuctLeakageEstimate, systemTons);
                diagnostics.Add(new DiagnosticResult
                {
                    Id = "commercial-duct-leakage",
                    Title = "Significant Commercial Duct Leakage",
                    Status = "yellow",
                    Description = $"Estimated {Inputs.DuctLeakageEstimate}% duct leakage in {systemTons:F0}-ton system is costing approximately ${annualCost:F0} annually in energy waste.",
                    ConfidenceLevel = 75,
                    Severity = 6,
                    Category = "Efficiency",
                    BusinessImpact = $"${annualCost:F0} annual energy waste, poor zone control",
                    Symptoms = new List<string> { "Uneven temperatures", "High energy bills", "Poor humidity control", "Difficulty maintaining setpoints" },
                    RecommendedActions = new List<string> { "Professional duct testing", "Targeted duct sealing", "Zone balancing", "Return on investment analysis" }
                });
            }

            return diagnostics;
        }

        private List<DiagnosticResult> DiagnoseCommercialBlowerIssues(double systemTons)
        {
            var diagnostics = new List<DiagnosticResult>();
            var ampRatio = Inputs.RatedBlowerAmps > 0 ? Inputs.BlowerAmpDraw / Inputs.RatedBlowerAmps : 1;

            // Commercial motor analysis
            if (ampRatio < 0.4) // Very low for commercial
            {
                diagnostics.Add(new DiagnosticResult
                {
                    Id = "commercial-motor-underperforming",
                    Title = $"Commercial Blower Motor Underperformance - {Inputs.BlowerMotorHP} HP",
                    Status = "red",
                    Description = $"Commercial blower motor drawing only {Inputs.BlowerAmpDraw}A of rated {Inputs.RatedBlowerAmps}A indicates serious performance issues in {systemTons:F0}-ton system.",
                    ConfidenceLevel = 85,
                    Severity = 8,
                    Category = "Equipment",
                    BusinessImpact = "Poor airflow delivery, potential motor failure, system inefficiency",
                    EstimatedDowntime = 8,
                    Symptoms = new List<string> { "Very low amp draw", "Reduced airflow", "Poor performance", "Motor may be failing" },
                    PossibleCauses = new List<string> { "Motor degradation", "VFD issues", "Belt problems", "Bearing failure", "Control system problems" },
                    RecommendedActions = new List<string> { "Motor performance testing", "VFD diagnosis", "Bearing inspection", "Control system check", "Consider motor replacement" }
                });
            }
            else if (ampRatio > 1.2) // High for commercial (more tolerance than residential)
            {
                diagnostics.Add(new DiagnosticResult
                {
                    Id = "commercial-motor-overloaded",
                    Title = $"Commercial Motor Overload - {Inputs.BlowerMotorHP} HP System",
                    Status = "red",
                    Description = $"Commercial blower motor drawing {Inputs.BlowerAmpDraw}A exceeds rated {Inputs.RatedBlowerAmps}A. Immediate attention required to prevent motor failure.",
                    ConfidenceLevel = 95,
                    Severity = 9,
                    Category = "Equipment",
                    BusinessImpact = "Motor failure risk, high energy costs, potential system shutdown",
                    EstimatedDowntime = 16,
                    Symptoms = new List<string> { "High amp draw", "Motor overheating", "Potential failure", "High energy consumption" },
                    PossibleCauses = new List<string> { "Dirty blower wheel", "High static pressure", "Motor bearing issues", "Belt problems", "System restrictions" },
                    RecommendedActions = new List<string> { "Immediate load reduction", "Clean blower assembly", "Check system restrictions", "Motor bearing service", "Static pressure reduction" }
                });
            }

            return diagnostics;
        }

        private List<DiagnosticResult> DiagnoseVentilationCompliance(double estimatedCFM)
        {
            var diagnostics = new List<DiagnosticResult>();

            if (Inputs.MinimumVentilationCFM > 0 && estimatedCFM < Inputs.MinimumVentilationCFM)
            {
                var shortfall = Inputs.MinimumVentilationCFM - estimatedCFM;
                diagnostics.Add(new DiagnosticResult
                {
                    Id = "ventilation-noncompliance",
                    Title = "ASHRAE 62.1 Ventilation Deficiency",
                    Status = "red",
                    Description = $"System provides {estimatedCFM:F0} CFM but requires {Inputs.MinimumVentilationCFM:F0} CFM per ASHRAE 62.1. Shortfall of {shortfall:F0} CFM may violate building codes.",
                    ConfidenceLevel = 95,
                    Severity = 9,
                    Category = "Compliance",
                    AffectsCodeCompliance = true,
                    AffectsOccupantComfort = true,
                    BusinessImpact = "Code violations, poor indoor air quality, health department issues",
                    Symptoms = new List<string> { "Inadequate fresh air", "Poor indoor air quality", "Occupant complaints", "Code compliance issues" },
                    PossibleCauses = new List<string> { "Undersized system", "Blocked outdoor air intake", "Faulty economizer", "Incorrect system settings" },
                    RecommendedActions = new List<string> { "Verify outdoor air intake", "Check economizer operation", "Review system capacity", "Consult ventilation engineer" }
                });
            }

            // Check application-specific requirements
            var appStandards = GetApplicationStandards();
            if (appStandards.ContainsKey(Inputs.ApplicationType))
            {
                var standards = appStandards[Inputs.ApplicationType];
                var requiredVentilation = CalculateRequiredVentilation(standards);

                if (estimatedCFM < requiredVentilation)
                {
                    diagnostics.Add(new DiagnosticResult
                    {
                        Id = "application-ventilation-deficiency",
                        Title = $"Inadequate Ventilation for {Inputs.ApplicationType}",
                        Status = "yellow",
                        Description = $"{Inputs.ApplicationType} applications typically require {requiredVentilation:F0} CFM based on occupancy and space requirements.",
                        ConfidenceLevel = 80,
                        Severity = 6,
                        Category = "Compliance",
                        AffectsOccupantComfort = true,
                        BusinessImpact = "Poor indoor air quality, potential health issues",
                        RecommendedActions = new List<string> { "Review occupancy calculations", "Verify ventilation requirements", "Consider system upgrades" }
                    });
                }
            }

            return diagnostics;
        }

        private List<DiagnosticResult> DiagnoseEconomizerIssues()
        {
            var diagnostics = new List<DiagnosticResult>();

            if (Inputs.HasEconomizer)
            {
                if (Inputs.EconomizerStuck)
                {
                    diagnostics.Add(new DiagnosticResult
                    {
                        Id = "economizer-malfunction",
                        Title = "Economizer System Malfunction",
                        Status = "red",
                        Description = "Economizer system is not operating correctly, preventing free cooling and increasing energy costs.",
                        ConfidenceLevel = 90,
                        Severity = 7,
                        Category = "Efficiency",
                        BusinessImpact = "Lost energy savings, higher cooling costs",
                        EstimatedDowntime = 4,
                        Symptoms = new List<string> { "Economizer not responding", "No free cooling", "High energy consumption" },
                        PossibleCauses = new List<string> { "Stuck dampers", "Faulty actuators", "Control system issues", "Sensor problems" },
                        RecommendedActions = new List<string> { "Inspect damper operation", "Check actuators", "Verify control sequences", "Calibrate sensors" }
                    });
                }

                // Check if economizer operation makes sense
                if (Inputs.AmbientTemp < Inputs.ReturnAirTemp - 5 && Math.Abs(Inputs.MixedAirTemp - Inputs.ReturnAirTemp) < 2)
                {
                    diagnostics.Add(new DiagnosticResult
                    {
                        Id = "economizer-not-operating",
                        Title = "Economizer Not Utilizing Free Cooling",
                        Status = "yellow",
                        Description = $"Outdoor conditions favor economizer operation (OA: {Inputs.AmbientTemp:F0}°F, RA: {Inputs.ReturnAirTemp:F0}°F) but mixed air temperature suggests minimal outdoor air usage.",
                        ConfidenceLevel = 75,
                        Severity = 5,
                        Category = "Efficiency",
                        BusinessImpact = "Missed energy savings opportunities",
                        RecommendedActions = new List<string> { "Check economizer controls", "Verify sensor calibration", "Review control sequences" }
                    });
                }
            }

            return diagnostics;
        }

        private List<DiagnosticResult> DiagnoseVAVSystemIssues()
        {
            var diagnostics = new List<DiagnosticResult>();

            if (Inputs.IsVAVSystem)
            {
                // VAV systems should have lower static pressure at part load
                if (Inputs.RuntimePercentage < 70 && Inputs.StaticPressure > 2.0)
                {
                    diagnostics.Add(new DiagnosticResult
                    {
                        Id = "vav-high-static-part-load",
                        Title = "VAV System High Static at Part Load",
                        Status = "yellow",
                        Description = $"VAV system shows high static pressure ({Inputs.StaticPressure:F1}\" WC) at part load operation ({Inputs.RuntimePercentage}% runtime). VFD may not be functioning properly.",
                        ConfidenceLevel = 80,
                        Severity = 6,
                        Category = "Efficiency",
                        BusinessImpact = "Higher than necessary energy consumption",
                        Symptoms = new List<string> { "High static pressure at part load", "VFD not reducing speed", "High energy consumption" },
                        RecommendedActions = new List<string> { "Check VFD operation", "Verify control sequences", "Calibrate pressure sensors", "Review setpoints" }
                    });
                }

                // VAV systems need proper minimum airflow
                var estimatedCFM = CalculateEstimatedCFM(Inputs.ReturnAirTemp - Inputs.SupplyAirTemp);
                if (estimatedCFM < Inputs.MinimumVentilationCFM * 1.2) // VAV needs extra for turndown
                {
                    diagnostics.Add(new DiagnosticResult
                    {
                        Id = "vav-insufficient-turndown",
                        Title = "VAV System Insufficient Minimum Airflow",
                        Status = "yellow",
                        Description = "VAV system may not provide adequate ventilation air at minimum flow conditions.",
                        ConfidenceLevel = 70,
                        Severity = 5,
                        Category = "Compliance",
                        AffectsCodeCompliance = true,
                        RecommendedActions = new List<string> { "Review minimum flow setpoints", "Check VAV box programming", "Verify ventilation calculations" }
                    });
                }
            }

            return diagnostics;
        }

        private List<DiagnosticResult> DiagnoseCommercialLoadIssues(double tempDiff, double cfmPerSqFt)
        {
            var diagnostics = new List<DiagnosticResult>();

            // Commercial load density check
            if (cfmPerSqFt > 0)
            {
                if (cfmPerSqFt < 0.5 && Inputs.ApplicationType != "Warehouse")
                {
                    diagnostics.Add(new DiagnosticResult
                    {
                        Id = "commercial-low-airflow-density",
                        Title = "Low Airflow Density for Commercial Application",
                        Status = "yellow",
                        Description = $"Airflow density of {cfmPerSqFt:F2} CFM/sq ft is low for {Inputs.ApplicationType} application. Typical range is 0.75-2.0 CFM/sq ft.",
                        ConfidenceLevel = 75,
                        Severity = 5,
                        Category = "Load",
                        BusinessImpact = "Potential comfort issues, inadequate air circulation",
                        RecommendedActions = new List<string> { "Review load calculations", "Check system sizing", "Verify space requirements" }
                    });
                }
                else if (cfmPerSqFt > 3.0)
                {
                    diagnostics.Add(new DiagnosticResult
                    {
                        Id = "commercial-high-airflow-density",
                        Title = "High Airflow Density - Energy Waste",
                        Status = "yellow",
                        Description = $"Airflow density of {cfmPerSqFt:F2} CFM/sq ft is high for {Inputs.ApplicationType}. This may indicate oversized system or duct leaks.",
                        ConfidenceLevel = 70,
                        Severity = 4,
                        Category = "Efficiency",
                        BusinessImpact = "Higher than necessary energy costs",
                        RecommendedActions = new List<string> { "Check for duct leaks", "Review system sizing", "Consider controls optimization" }
                    });
                }
            }

            return diagnostics;
        }

        private List<DiagnosticResult> DiagnoseCommercialR22Issues(double systemTons)
        {
            var diagnostics = new List<DiagnosticResult>();

            if (Inputs.SystemAge > 8 && systemTons > 10) // Large commercial R-22 systems
            {
                var replacementCost = EstimateSystemReplacementCost(systemTons);
                var annualR22Cost = EstimateAnnualR22Costs(systemTons);

                diagnostics.Add(new DiagnosticResult
                {
                    Id = "commercial-r22-replacement-analysis",
                    Title = "Large Commercial R-22 System Replacement Analysis",
                    Status = "yellow",
                    Description = $"Large commercial R-22 system ({systemTons:F0} tons, {Inputs.SystemAge} years old) faces increasing service costs and limited refrigerant availability.",
                    ConfidenceLevel = 90,
                    Severity = 6,
                    Category = "Strategic",
                    BusinessImpact = $"Annual R-22 costs: ${annualR22Cost:N0}, System replacement: ${replacementCost:N0}",
                    Symptoms = new List<string> { "High R-22 costs", "Limited service options", "Compliance concerns", "Increasing maintenance needs" },
                    RecommendedActions = new List<string> { "Conduct replacement feasibility study", "Budget for system replacement", "Minimize refrigerant leaks", "Consider phased replacement" }
                });
            }

            return diagnostics;
        }

        #endregion

        #region Commercial Calculation Methods

        private double GetCommercialNominalCFM()
        {
            var systemTons = Inputs.SystemCapacity / 12000.0;
            var baselines = new CommercialSystemBaselines();
            var targetCFMPerTon = baselines.CFMPerTonBySystemType.GetValueOrDefault(Inputs.SystemType, 425);
            return systemTons * targetCFMPerTon;
        }

        private AirflowThresholds GetCommercialThresholds(double systemTons)
        {
            var thresholds = new CommercialDiagnosticThresholds();
            var category = GetSystemSizeCategory(systemTons);
            return thresholds.ThresholdsByTonnage.GetValueOrDefault(category,
                new AirflowThresholds { LowCFMPerTon = 375, OptimalCFMPerTon = 425, HighCFMPerTon = 525, MaxStaticPressure = 2.0 });
        }

        protected string GetSystemSizeCategory(double systemTons = 0)
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

        private double GetCommercialCostMultiplier(double systemTons)
        {
            var factors = new CommercialCostFactors();
            var category = GetSystemSizeCategory(systemTons);
            return factors.RepairCostMultipliers.GetValueOrDefault(category, 1.0);
        }

        private double GetMaxFilterDrop(double systemTons)
        {
            return systemTons switch
            {
                <= 10 => 0.4,
                <= 30 => 0.5,
                <= 75 => 0.6,
                _ => 0.8
            };
        }

        private double GetMaxCoilDrop(double systemTons)
        {
            return systemTons switch
            {
                <= 10 => 0.5,
                <= 30 => 0.6,
                <= 75 => 0.8,
                _ => 1.0
            };
        }

        private double GetMaxDuctLeakage(double systemTons)
        {
            return systemTons switch
            {
                <= 10 => 15,
                <= 30 => 12,
                <= 75 => 10,
                _ => 8
            };
        }

        private double CalculateEstimatedCFM(double tempDiff)
        {
            return tempDiff > 0 ? Inputs.SystemCapacity / (1.08 * tempDiff) : 0;
        }

        private double CalculateCommercialSystemHealth(List<DiagnosticResult> diagnostics, double cfmPerTon, double systemTons)
        {
            var baseHealth = 100.0;
            var thresholds = GetCommercialThresholds(systemTons);

            // Commercial systems are more robust, so base penalties differently
            foreach (var diagnostic in diagnostics)
            {
                var impact = (diagnostic.Severity * diagnostic.ConfidenceLevel / 100.0) * 1.5; // Reduced from residential
                baseHealth -= impact;

                // Extra penalty for code compliance issues
                if (diagnostic.AffectsCodeCompliance) baseHealth -= 5;
            }

            // Commercial-specific penalties
            if (cfmPerTon < thresholds.LowCFMPerTon * 0.7) baseHealth -= 15;
            if (Inputs.StaticPressure > thresholds.MaxStaticPressure) baseHealth -= 10;
            if (Inputs.SystemAge > 15 && systemTons > 20) baseHealth -= 8; // Large old systems

            return Math.Max(0, Math.Min(100, baseHealth));
        }

        private double CalculateCommercialAirflowEfficiency(double cfmPerTon, double systemTons)
        {
            var thresholds = GetCommercialThresholds(systemTons);
            return Math.Min(100, (cfmPerTon / thresholds.OptimalCFMPerTon) * 100);
        }

        private double CalculateVentilationEfficiency(double estimatedCFM)
        {
            if (Inputs.MinimumVentilationCFM <= 0) return 100;
            return Math.Min(100, (estimatedCFM / Inputs.MinimumVentilationCFM) * 100);
        }

        private double CalculateEconomizerEfficiency()
        {
            if (!Inputs.HasEconomizer) return 100; // N/A
            if (Inputs.EconomizerStuck) return 0;

            // Simple efficiency based on outdoor conditions and mixed air temperature
            if (Inputs.AmbientTemp < Inputs.ReturnAirTemp - 5)
            {
                var expectedMixed = (Inputs.AmbientTemp + Inputs.ReturnAirTemp) / 2;
                var actualBenefit = Math.Abs(Inputs.MixedAirTemp - expectedMixed);
                return Math.Max(0, 100 - (actualBenefit * 10));
            }

            return 85; // Reasonable efficiency when not in economizer mode
        }

        private double CalculateCommercialEfficiencyLoss(List<DiagnosticResult> diagnostics, double systemTons)
        {
            var baseLoss = diagnostics.Sum(d => d.Severity * 0.6); // Commercial systems more robust

            // Scale by system size - larger systems have more impact
            var sizeMultiplier = systemTons switch
            {
                <= 10 => 1.0,
                <= 30 => 1.2,
                <= 75 => 1.4,
                _ => 1.6
            };

            return Math.Min(40, baseLoss * sizeMultiplier);
        }

        private double CalculateCommercialAnnualCostImpact(double efficiencyLoss, double systemTons)
        {
            var factors = new CommercialCostFactors();
            var category = GetSystemSizeCategory(systemTons);
            var baseAnnualCost = factors.BaseAnnualCostPerTon.GetValueOrDefault(category, 1000) * systemTons;
            return baseAnnualCost * (efficiencyLoss / 100.0);
        }

        private double CalculateDemandChargeImpact(double efficiencyLoss, double systemTons)
        {
            // Commercial demand charges for peak usage
            var peakKW = systemTons * 1.2; // Rough estimate: 1.2 kW per ton
            var demandRate = 15.0; // $/kW typical commercial rate
            var efficiencyImpact = efficiencyLoss / 100.0;
            return peakKW * demandRate * efficiencyImpact * 12; // Annual impact
        }

        private double CalculateMaintenanceCostImpact(List<DiagnosticResult> diagnostics, double systemTons)
        {
            var baseMaintenanceCost = systemTons * 200; // $200 per ton annually
            var increasePercentage = diagnostics.Count(d => d.Severity >= 6) * 0.15; // 15% per major issue
            return baseMaintenanceCost * increasePercentage;
        }

        private List<RepairRecommendation> GenerateCommercialRepairRecommendations(List<DiagnosticResult> diagnostics, double systemTons)
        {
            var recommendations = new List<RepairRecommendation>();
            var costFactors = new CommercialCostFactors();
            var costMultiplier = GetCommercialCostMultiplier(systemTons);

            foreach (var diagnostic in diagnostics.Take(5))
            {
                var priority = diagnostic.Severity >= 8 ? 1 : diagnostic.Severity >= 5 ? 2 : 3;
                var urgency = diagnostic.Status == "red" ? "High" : diagnostic.Status == "yellow" ? "Medium" : "Low";

                // Commercial-specific urgency adjustments
                if (diagnostic.AffectsCodeCompliance) urgency = "High";
                if (diagnostic.EstimatedDowntime > 8) urgency = "High";

                var estimatedCost = EstimateCommercialRepairCost(diagnostic.Id, systemTons);
                var laborHours = EstimateLaborHours(diagnostic.Id, systemTons);
                var paybackPeriod = CalculatePaybackPeriod(estimatedCost, diagnostic.Severity, systemTons);

                recommendations.Add(new RepairRecommendation
                {
                    Issue = diagnostic.Title,
                    Action = diagnostic.RecommendedActions.FirstOrDefault() ?? "Professional commercial system inspection",
                    Priority = priority,
                    Urgency = urgency,
                    EstimatedCost = estimatedCost,
                    ExpectedImprovement = diagnostic.Severity * 8, // Commercial improvement estimate
                    CustomerExplanation = GetCommercialCustomerExplanation(diagnostic),
                    TechnicianNotes = $"Confidence: {diagnostic.ConfidenceLevel}%, Severity: {diagnostic.Severity}/10, Category: {diagnostic.Category}",
                    PaybackPeriod = paybackPeriod,
                    BusinessJustification = GetBusinessJustification(diagnostic, estimatedCost, systemTons),
                    RequiresShutdown = diagnostic.EstimatedDowntime > 0,
                    LaborHours = laborHours,
                    ComplianceImpact = diagnostic.AffectsCodeCompliance ? "Code compliance issue" : ""
                });
            }

            return recommendations.OrderBy(r => r.Priority)
                                 .ThenBy(r => r.PaybackPeriod)
                                 .ThenByDescending(r => r.ExpectedImprovement)
                                 .ToList();
        }

        private DiagnosticResult CreateCommercialNormalResult()
        {
            return new DiagnosticResult
            {
                Id = "commercial-normal-operation",
                Title = "Commercial System Operating Normally",
                Status = "green",
                Description = "Commercial HVAC system appears to be operating within acceptable parameters for the application.",
                ConfidenceLevel = 85,
                Severity = 0,
                Category = "Normal",
                BusinessImpact = "System operating efficiently within normal parameters"
            };
        }

        #endregion

        #region Commercial Helper Methods

        private bool ShouldAutoRunDiagnostic()
        {
            return Inputs.SystemCapacity > 0 &&
                   Inputs.SupplyAirTemp > 0 &&
                   Inputs.ReturnAirTemp > 0 &&
                   Inputs.SystemCapacity >= 24000; // Minimum commercial size
        }

        private Dictionary<string, ApplicationStandards> GetApplicationStandards()
        {
            var thresholds = new CommercialDiagnosticThresholds();
            return thresholds.StandardsByApplication;
        }

        private double CalculateRequiredVentilation(ApplicationStandards standards)
        {
            var occupantCount = Inputs.BuildingSquareFootage / standards.TypicalOccupancyDensity;
            var ventilationByOccupant = occupantCount * standards.MinVentilationCFMPerPerson;
            var ventilationByArea = Inputs.BuildingSquareFootage * standards.MinVentilationCFMPerSqFt;
            return Math.Max(ventilationByOccupant, ventilationByArea);
        }

        private double CalculateAirflowLossFromPressure(double excessPressure, double systemTons)
        {
            // Simplified calculation - excess pressure reduces airflow
            return excessPressure * systemTons * 50; // Rough estimate
        }

        private double CalculateDuctLeakageCost(double leakagePercent, double systemTons)
        {
            var baseAnnualCost = systemTons * 1000; // $1000 per ton annually
            return baseAnnualCost * (leakagePercent / 100.0) * 0.3; // 30% of leakage impacts energy
        }

        private double EstimateCommercialRepairCost(string diagnosticId, double systemTons)
        {
            var costMultiplier = GetCommercialCostMultiplier(systemTons);

            var baseCost = diagnosticId switch
            {
                "commercial-filter-dirty" or "commercial-filter-critical" => 150 * costMultiplier,
                "commercial-coil-fouled" => 800 * costMultiplier,
                "commercial-coil-freezing" => 2500 * costMultiplier,
                "commercial-motor-overloaded" or "commercial-motor-underperforming" => 1500 * costMultiplier,
                "commercial-static-excessive" => 3500 * costMultiplier,
                "commercial-duct-leakage" => 2000 * costMultiplier,
                "economizer-malfunction" => 1200 * costMultiplier,
                "vav-high-static-part-load" => 800 * costMultiplier,
                "commercial-r22-replacement-analysis" => EstimateSystemReplacementCost(systemTons),
                _ => 500 * costMultiplier
            };

            return baseCost;
        }

        private double EstimateLaborHours(string diagnosticId, double systemTons)
        {
            var baseHours = diagnosticId switch
            {
                "commercial-filter-dirty" => 1,
                "commercial-filter-critical" => 2,
                "commercial-coil-fouled" => systemTons > 30 ? 12 : 6,
                "commercial-coil-freezing" => systemTons > 30 ? 16 : 8,
                "commercial-motor-overloaded" => systemTons > 30 ? 8 : 4,
                "commercial-static-excessive" => systemTons > 30 ? 20 : 12,
                "economizer-malfunction" => 6,
                _ => 4
            };

            return baseHours;
        }

        private double CalculatePaybackPeriod(double cost, double severity, double systemTons)
        {
            var monthlySavings = (severity * systemTons * 50) / 12; // Rough savings estimate
            return monthlySavings > 0 ? cost / monthlySavings : 999;
        }

        private string GetCommercialCustomerExplanation(DiagnosticResult diagnostic)
        {
            return diagnostic.Id switch
            {
                "commercial-filter-critical" => "Dirty filters are blocking airflow, reducing comfort and dramatically increasing energy costs for your facility.",
                "commercial-coil-fouled" => "Dirty coils reduce cooling efficiency and can lead to system failure, affecting tenant comfort and increasing operating expenses.",
                "commercial-airflow-critical" => "Very low airflow prevents proper building comfort and dramatically increases energy costs.",
                "commercial-static-excessive" => "High pressure in ductwork strains equipment, increases energy costs, and reduces system lifespan.",
                "ventilation-noncompliance" => "Inadequate ventilation may violate building codes and affect indoor air quality for occupants.",
                _ => "This issue is affecting your building's comfort, energy efficiency, and operating costs."
            };
        }

        private string GetBusinessJustification(DiagnosticResult diagnostic, double cost, double systemTons)
        {
            var annualSavings = diagnostic.Severity * systemTons * 200; // Rough annual savings

            return diagnostic.Category switch
            {
                "Compliance" => $"Required for code compliance and occupant health",
                "Critical" => $"Prevents equipment damage and business disruption",
                "Efficiency" => $"Estimated annual savings: ${annualSavings:F0}",
                "Airflow" => $"Improves comfort and reduces energy costs",
                _ => $"Enhances system reliability and efficiency"
            };
        }

        private double EstimateSystemReplacementCost(double systemTons)
        {
            // Commercial system replacement costs
            return systemTons switch
            {
                <= 10 => systemTons * 8000,
                <= 30 => systemTons * 7000,
                <= 75 => systemTons * 6500,
                _ => systemTons * 6000
            };
        }

        private double EstimateAnnualR22Costs(double systemTons)
        {
            // R-22 refrigerant and service costs for commercial systems
            var baseR22Cost = systemTons * 400; // Higher for commercial due to system complexity
            return baseR22Cost * (1 + (Inputs.SystemAge - 10) * 0.2); // Increases with age
        }

        #endregion
       
        #region UI Helper Methods (unchanged but added GetSystemSizeCategory method)

        protected string GetOverallHealthClass()
        {
            if (Results == null) return "bg-secondary";
            return Results.OverallSystemHealth switch
            {
                >= 80 => "bg-success",
                >= 60 => "bg-warning",
                _ => "bg-danger"
            };
        }

        protected string GetHealthProgressClass()
        {
            if (Results == null) return "bg-secondary";
            return Results.OverallSystemHealth switch
            {
                >= 80 => "bg-success",
                >= 60 => "bg-warning",
                _ => "bg-danger"
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
                "red" => builder => builder.AddMarkupContent(0, "<svg class=\"me-2\" width=\"20\" height=\"20\" fill=\"currentColor\" viewBox=\"0 0 24 24\"><path d=\"M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-2 15l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z\"/></svg>"),
                "yellow" => builder => builder.AddMarkupContent(0, "<svg class=\"me-2\" width=\"20\" height=\"20\" fill=\"currentColor\" viewBox=\"0 0 24 24\"><path d=\"M1 21h22L12 2 1 21zm12-3h-2v-2h2v2zm0-4h-2v-4h2v4z\"/></svg>"),
                "green" => builder => builder.AddMarkupContent(0, "<svg class=\"me-2\" width=\"20\" height=\"20\" fill=\"currentColor\" viewBox=\"0 0 24 24\"><path d=\"M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-2 15l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z\"/></svg>"),
                _ => builder => builder.AddMarkupContent(0, "<svg class=\"me-2\" width=\"20\" height=\"20\" fill=\"currentColor\" viewBox=\"0 0 24 24\"><circle cx=\"12\" cy=\"12\" r=\"10\"/></svg>")
            };
        }

        protected string GetStatusTextClass(string status)
        {
            return status switch
            {
                "red" => "text-danger",
                "yellow" => "text-warning",
                "green" => "text-success",
                _ => "text-secondary"
            };
        }

        protected string GetStatusBadgeClass(string status)
        {
            return status switch
            {
                "red" => "bg-danger",
                "yellow" => "bg-warning text-dark",
                "green" => "bg-success",
                _ => "bg-secondary"
            };
        }

        protected string GetPriorityBadgeClass(int priority)
        {
            return priority switch
            {
                1 => "bg-danger",
                2 => "bg-warning text-dark",
                3 => "bg-info",
                _ => "bg-secondary"
            };
        }

        #endregion
    }
}