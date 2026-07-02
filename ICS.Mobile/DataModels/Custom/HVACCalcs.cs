namespace ICS.Mobile.DataModels.Custom
{
    public class HVACCalculations
    {
        // Models/TXVInputs.cs
        public class TXVInputs
        {
            public double SystemCapacity { get; set; } = 36000;
            public string RefrigerantType { get; set; } = "R-410A";
            public double EvaporatorTemp { get; set; } = 40;
            public double CondensingTemp { get; set; } = 110;
            public double Subcooling { get; set; } = 10;
            public double Superheat { get; set; } = 10;
            public double TxvRating { get; set; } = 4;
            public double PressureDrop { get; set; } = 80;
        }
        public class TXVResults
        {
            public double SystemTons { get; set; }
            public double AdjustedFlowRate { get; set; }
            public double ActualPressureDrop { get; set; }
            public double RequiredTXVCapacity { get; set; }
            public double SizingRatio { get; set; }
            public string SizingStatus { get; set; } = string.Empty;
            public string Recommendation { get; set; } = string.Empty;
            public List<string> Warnings { get; set; } = new();
            public double TxvCapacityFactor { get; set; }
        }

        public class RefrigerantProperties
        {
            public double DensityFactor { get; set; }
            public double PressureFactor { get; set; }
        }

        // ==================== AIRFLOW CALCULATOR ====================

        // Airflow Input Parameters
        public class AirflowInputs
        {
            public double SystemCapacity { get; set; } = 36000; // BTU/hr
            public double SupplyAirTemp { get; set; } = 55; // °F
            public double ReturnAirTemp { get; set; } = 75; // °F
            public string SystemType { get; set; } = "Air Conditioning"; // AC, Heat Pump, etc.
            public string RefrigerantType { get; set; } = "R-410A";
            public double StaticPressure { get; set; } = 0.5; // inches WC
            public double DuctDiameter { get; set; } = 12; // inches (optional for velocity calc)
            public double MeasuredCFM { get; set; } = 0; // CFM (if measured)
            public bool IsR22System { get; set; } = false;
        }

        // Airflow Calculation Results
        public class AirflowResults
        {
            public double SystemTons { get; set; }
            public double TemperatureDifferential { get; set; }
            public double CalculatedCFM { get; set; }
            public double CFMPerTon { get; set; }
            public double TargetCFMPerTon { get; set; } = 400; // Standard target
            public double AirflowEfficiency { get; set; }
            public double Velocity { get; set; } // ft/min if duct size provided
            public string AirflowStatus { get; set; } = string.Empty;
            public string Recommendation { get; set; } = string.Empty;
            public List<string> Warnings { get; set; } = new();
            public List<string> R22SpecificIssues { get; set; } = new();
            public double StaticPressureStatus { get; set; }
        }

        // System Type Properties
        public class SystemTypeProperties
        {
            public double IdealCFMPerTon { get; set; }
            public double MinCFMPerTon { get; set; }
            public double MaxCFMPerTon { get; set; }
            public double IdealTemperatureDiff { get; set; }
            public string Description { get; set; } = string.Empty;
        }

        // Static Pressure Guidelines
        public class StaticPressureGuidelines
        {
            public double Low { get; set; } = 0.3;
            public double Normal { get; set; } = 0.5;
            public double High { get; set; } = 0.8;
            public double Excessive { get; set; } = 1.0;
        }

        // ==================== ENHANCED COMMERCIAL AIRFLOW DIAGNOSTIC ====================

        // Commercial-Ready Airflow Diagnostic Inputs
        public class AirflowDiagnosticInputs
        {
            // Basic System Info - Expanded for Commercial
            public double SystemCapacity { get; set; } = 60000; // BTU/hr - Default to 5 ton commercial
            public string SystemType { get; set; } = "Packaged Rooftop Unit";
            public string RefrigerantType { get; set; } = "R-410A";
            public int SystemAge { get; set; } = 5; // years
            public bool IsR22System { get; set; } = false;

            // Commercial System Characteristics
            public string ApplicationType { get; set; } = "Office Building"; // Office, Retail, Industrial, etc.
            public bool HasEconomizer { get; set; } = false;
            public bool IsVAVSystem { get; set; } = false; // Variable Air Volume
            public bool IsConstantVolume { get; set; } = true;
            public int NumberOfZones { get; set; } = 1;
            public double BuildingSquareFootage { get; set; } = 3000; // sq ft served

            // Temperature Measurements
            public double SupplyAirTemp { get; set; } = 55; // °F
            public double ReturnAirTemp { get; set; } = 75; // °F
            public double AmbientTemp { get; set; } = 95; // °F
            public double CoilTemp { get; set; } = 45; // °F (evaporator surface)
            public double MixedAirTemp { get; set; } = 78; // °F (if economizer)

            // Pressure Readings - Commercial Ranges
            public double StaticPressure { get; set; } = 1.2; // inches WC (higher for commercial)
            public double VelocityPressure { get; set; } = 0.15; // inches WC
            public double FilterPressureDrop { get; set; } = 0.3; // inches WC
            public double CoilPressureDrop { get; set; } = 0.4; // inches WC
            public double DuctworkPressureDrop { get; set; } = 0.8; // inches WC

            // Electrical Readings - Commercial Scale
            public double BlowerAmpDraw { get; set; } = 12.5; // Amps
            public double RatedBlowerAmps { get; set; } = 15.0; // Amps
            public double SupplyVoltage { get; set; } = 480; // Volts (commercial voltage)
            public double BlowerMotorHP { get; set; } = 5.0; // Motor horsepower

            // Commercial-Specific Observations
            public string FilterCondition { get; set; } = "Clean"; // Clean, Dirty, Extremely Dirty
            public string FilterType { get; set; } = "Pleated"; // Pleated, HEPA, Bag, etc.
            public bool IceFormation { get; set; } = false;
            public bool ExcessiveCondensation { get; set; } = false;
            public string CoilCondition { get; set; } = "Clean"; // Clean, Moderately Dirty, Very Dirty
            public bool UnusualNoises { get; set; } = false;
            public bool EconomizerStuck { get; set; } = false;

            // Commercial Runtime Data
            public double RuntimePercentage { get; set; } = 65; // % of time running
            public bool ShortCycling { get; set; } = false;
            public double DuctLeakageEstimate { get; set; } = 8; // % estimated leakage (better in commercial)
            public double OccupancySchedule { get; set; } = 50; // Hours per week occupied

            // Measured Airflow (if available)
            public double MeasuredCFM { get; set; } = 0;
            public string MeasurementMethod { get; set; } = "None"; // None, Anemometer, Balometer, Pitot Traverse
            public double MinimumVentilationCFM { get; set; } = 0; // ASHRAE requirements
        }

        // Enhanced Commercial Diagnostic Results
        public class AirflowDiagnosticResults
        {
            // Basic Calculations
            public double NominalCFM { get; set; }
            public double EstimatedCFM { get; set; }
            public double CFMPerTon { get; set; }
            public double CFMPerSquareFoot { get; set; } // Commercial metric
            public double TemperatureDifferential { get; set; }
            public double SystemTons { get; set; }

            // Commercial Performance Metrics
            public double VentilationEfficiency { get; set; } // ASHRAE compliance
            public double EconomizerEfficiency { get; set; } // If applicable
            public double TotalSystemEfficiency { get; set; } // Overall commercial efficiency

            // Primary Diagnostic Result
            public DiagnosticResult PrimaryResult { get; set; } = new();

            // Multiple Issue Detection
            public List<DiagnosticResult> AllDiagnostics { get; set; } = new();

            // Commercial-Specific Metrics
            public double OverallSystemHealth { get; set; } // 0-100%
            public double AirflowEfficiency { get; set; } // 0-100%
            public double EnergyEfficiencyImpact { get; set; } // % impact on efficiency
            public double OperationalCostImpact { get; set; } // Commercial operational costs

            // Commercial Repair Prioritization
            public List<RepairRecommendation> RepairPriorities { get; set; } = new();

            // Commercial Cost Analysis
            public double EstimatedEfficiencyLoss { get; set; } // % energy loss
            public double AnnualCostImpact { get; set; } // $ per year (commercial scale)
            public double DemandChargeImpact { get; set; } // Peak demand costs
            public double MaintenanceCostImpact { get; set; } // Increased maintenance costs
        }

        // Enhanced Diagnostic Result for Commercial
        public class DiagnosticResult
        {
            public string Id { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public string Status { get; set; } = "green"; // red, yellow, green
            public string Description { get; set; } = string.Empty;
            public double ConfidenceLevel { get; set; } = 0; // 0-100%
            public double Severity { get; set; } = 0; // 0-10 scale
            public double AirflowImpact { get; set; } = 0; // CFM impact
            public string Category { get; set; } = string.Empty; // Airflow, Load, Efficiency, Comfort, Compliance
            public List<string> Symptoms { get; set; } = new();
            public List<string> PossibleCauses { get; set; } = new();
            public List<string> RecommendedActions { get; set; } = new();

            // Commercial-Specific Fields
            public bool AffectsOccupantComfort { get; set; } = false;
            public bool AffectsCodeCompliance { get; set; } = false;
            public string BusinessImpact { get; set; } = string.Empty; // Productivity, comfort, compliance
            public double EstimatedDowntime { get; set; } = 0; // Hours if not addressed
        }

        // Commercial Repair Recommendation
        public class RepairRecommendation
        {
            public string Issue { get; set; } = string.Empty;
            public string Action { get; set; } = string.Empty;
            public int Priority { get; set; } = 1; // 1=Critical, 2=Important, 3=Recommended
            public string Urgency { get; set; } = "Medium"; // Immediate, High, Medium, Low
            public double EstimatedCost { get; set; } = 0;
            public double ExpectedImprovement { get; set; } = 0; // % improvement
            public string CustomerExplanation { get; set; } = string.Empty;
            public string TechnicianNotes { get; set; } = string.Empty;

            // Commercial-Specific Fields
            public double PaybackPeriod { get; set; } = 0; // Months
            public string BusinessJustification { get; set; } = string.Empty;
            public bool RequiresShutdown { get; set; } = false;
            public double LaborHours { get; set; } = 0;
            public string ComplianceImpact { get; set; } = string.Empty;
        }

        // Commercial System Thresholds
        public class CommercialDiagnosticThresholds
        {
            // Airflow Thresholds by System Size
            public Dictionary<string, AirflowThresholds> ThresholdsByTonnage { get; set; } = new()
            {
                { "Small Commercial (2-10 tons)", new AirflowThresholds { LowCFMPerTon = 350, OptimalCFMPerTon = 400, HighCFMPerTon = 500, MaxStaticPressure = 1.5 } },
                { "Medium Commercial (10-30 tons)", new AirflowThresholds { LowCFMPerTon = 375, OptimalCFMPerTon = 425, HighCFMPerTon = 525, MaxStaticPressure = 2.0 } },
                { "Large Commercial (30-75 tons)", new AirflowThresholds { LowCFMPerTon = 400, OptimalCFMPerTon = 450, HighCFMPerTon = 550, MaxStaticPressure = 2.5 } },
                { "Industrial (75+ tons)", new AirflowThresholds { LowCFMPerTon = 425, OptimalCFMPerTon = 475, HighCFMPerTon = 575, MaxStaticPressure = 3.0 } }
            };

            // Commercial Application-Specific Standards
            public Dictionary<string, ApplicationStandards> StandardsByApplication { get; set; } = new()
            {
                { "Office Building", new ApplicationStandards { MinVentilationCFMPerPerson = 20, MinVentilationCFMPerSqFt = 0.06, TypicalOccupancyDensity = 100 } },
                { "Retail", new ApplicationStandards { MinVentilationCFMPerPerson = 15, MinVentilationCFMPerSqFt = 0.12, TypicalOccupancyDensity = 30 } },
                { "Restaurant", new ApplicationStandards { MinVentilationCFMPerPerson = 20, MinVentilationCFMPerSqFt = 0.18, TypicalOccupancyDensity = 70 } },
                { "Manufacturing", new ApplicationStandards { MinVentilationCFMPerPerson = 20, MinVentilationCFMPerSqFt = 0.5, TypicalOccupancyDensity = 500 } },
                { "Warehouse", new ApplicationStandards { MinVentilationCFMPerPerson = 20, MinVentilationCFMPerSqFt = 0.05, TypicalOccupancyDensity = 2000 } },
                { "Healthcare", new ApplicationStandards { MinVentilationCFMPerPerson = 25, MinVentilationCFMPerSqFt = 0.18, TypicalOccupancyDensity = 120 } }
            };
        }

        public class AirflowThresholds
        {
            public double LowCFMPerTon { get; set; }
            public double OptimalCFMPerTon { get; set; }
            public double HighCFMPerTon { get; set; }
            public double MaxStaticPressure { get; set; }
        }

        public class ApplicationStandards
        {
            public double MinVentilationCFMPerPerson { get; set; }
            public double MinVentilationCFMPerSqFt { get; set; }
            public double TypicalOccupancyDensity { get; set; } // sq ft per person
        }

        // Commercial System Performance Baselines
        public class CommercialSystemBaselines
        {
            public Dictionary<string, double> CFMPerTonBySystemType { get; set; } = new()
            {
                { "Packaged Rooftop Unit", 425 },
                { "Split System", 400 },
                { "Variable Air Volume (VAV)", 450 },
                { "Constant Volume", 400 },
                { "Heat Pump", 425 },
                { "Chilled Water", 450 },
                { "Direct Expansion", 400 },
                { "Economizer Unit", 475 },
                { "Industrial Process", 500 },
                { "Clean Room", 600 },
                { "Data Center", 350 } // Higher sensible loads, lower CFM/ton
            };

            public Dictionary<string, double> IdealTemperatureDifferential { get; set; } = new()
            {
                { "Packaged Rooftop Unit", 18 },
                { "Split System", 20 },
                { "Variable Air Volume (VAV)", 16 }, // Lower delta-T for better humidity control
                { "Constant Volume", 20 },
                { "Heat Pump", 20 },
                { "Chilled Water", 16 },
                { "Direct Expansion", 20 },
                { "Economizer Unit", 22 },
                { "Industrial Process", 25 },
                { "Clean Room", 14 }, // Tight control requirements
                { "Data Center", 22 } // Higher sensible loads
            };

            public Dictionary<string, double> MaxStaticPressureBySize { get; set; } = new()
            {
                { "Small Commercial (2-10 tons)", 1.5 },
                { "Medium Commercial (10-30 tons)", 2.0 },
                { "Large Commercial (30-75 tons)", 2.5 },
                { "Industrial (75+ tons)", 3.0 }
            };
        }

        // Commercial Environmental Factors
        public class CommercialEnvironmentalFactors
        {
            public string ClimateZone { get; set; } = "Mixed"; // Hot, Mixed, Cold
            public string BuildingType { get; set; } = "Commercial"; // Commercial, Industrial, Institutional
            public double AltitudeCorrection { get; set; } = 1.0; // altitude factor
            public double HumidityLevel { get; set; } = 50; // % RH
            public bool HighVentilationRequirements { get; set; } = false; // Healthcare, labs, etc.
            public double LocalElectricRate { get; set; } = 0.12; // $/kWh for cost calculations
            public double DemandCharge { get; set; } = 15.0; // $/kW for peak demand
        }

        // Commercial Cost Calculation Factors
        public class CommercialCostFactors
        {
            public Dictionary<string, double> BaseAnnualCostPerTon { get; set; } = new()
            {
                { "Small Commercial (2-10 tons)", 1200 },
                { "Medium Commercial (10-30 tons)", 1000 },
                { "Large Commercial (30-75 tons)", 900 },
                { "Industrial (75+ tons)", 800 }
            };

            public Dictionary<string, double> RepairCostMultipliers { get; set; } = new()
            {
                { "Small Commercial (2-10 tons)", 1.0 },
                { "Medium Commercial (10-30 tons)", 2.0 },
                { "Large Commercial (30-75 tons)", 3.5 },
                { "Industrial (75+ tons)", 5.0 }
            };

            public Dictionary<string, double> LaborRatesPerHour { get; set; } = new()
            {
                { "Basic Maintenance", 85 },
                { "System Repair", 125 },
                { "Specialized Work", 165 },
                { "Emergency Service", 225 }
            };
        }



        // ==================== COMMERCIAL SUBCOOL CALCULATOR ====================

        // Commercial Subcool Calculator Inputs
        public class SubcoolInputs
        {
            // System Information
            public double SystemCapacity { get; set; } = 60000; // BTU/hr (5 ton default)
            public string SystemType { get; set; } = "Packaged Rooftop Unit";
            public string RefrigerantType { get; set; } = "R-410A";
            public string ApplicationType { get; set; } = "Office Building";
            public int SystemAge { get; set; } = 5;

            // Measurement Method
            public string MeasurementMethod { get; set; } = "Pressure and Temperature"; // "Pressure and Temperature", "Temperature Only"

            // Pressure Measurements (if using pressure method)
            public double CondensingPressure { get; set; } = 250; // PSIG
            public string PressureUnit { get; set; } = "PSIG"; // PSIG, PSIA, kPa

            // Temperature Measurements
            public double LiquidLineTemp { get; set; } = 95; // °F - actual temperature
            public double CondensingTemp { get; set; } = 105; // °F - saturated temperature (if known)
            public string TemperatureUnit { get; set; } = "Fahrenheit"; // Fahrenheit, Celsius

            // Environmental Conditions
            public double AmbientTemp { get; set; } = 95; // °F
            public double IndoorTemp { get; set; } = 75; // °F

            // System Operating Conditions
            public double SupplyAirTemp { get; set; } = 55; // °F
            public double ReturnAirTemp { get; set; } = 75; // °F
            public double EvaporatorPressure { get; set; } = 118; // PSIG (for complete analysis)
            public double SuperheatValue { get; set; } = 12; // °F (if known)

            // Commercial System Specifics
            public bool HasReceiver { get; set; } = true; // Most commercial systems do
            public bool IsChillerSystem { get; set; } = false;
            public bool IsHeatPumpMode { get; set; } = false;
            public string CondenserType { get; set; } = "Air Cooled"; // Air Cooled, Water Cooled, Evaporative
            public double CondenserApproach { get; set; } = 10; // °F temperature approach

            // Advanced Measurements (optional)
            public double LiquidLinePressure { get; set; } = 0; // If measured separately
            public double ReceiverTemp { get; set; } = 0; // If receiver temperature measured
            public bool MeasureMultiplePoints { get; set; } = false; // Multiple liquid line measurement points
        }

        // Commercial Subcool Results
        public class SubcoolResults
        {
            // Basic Calculations
            public double CalculatedSubcooling { get; set; } // °F
            public double BubblePointTemperature { get; set; } // °F - saturated liquid temperature
            public double ActualLiquidTemp { get; set; } // °F
            public double CondensingPressurePSIA { get; set; } // Converted to absolute pressure

            // System Analysis
            public double SystemTons { get; set; }
            public string SystemSizeCategory { get; set; } = string.Empty;
            public double OptimalSubcoolingLow { get; set; } // Lower end of optimal range
            public double OptimalSubcoolingHigh { get; set; } // Higher end of optimal range
            public double SubcoolingEfficiency { get; set; } // 0-100% efficiency rating

            // Commercial Performance Metrics
            public double CapacityImpact { get; set; } // % capacity impact
            public double EfficiencyImpact { get; set; } // % efficiency impact
            public double EstimatedChargeLevel { get; set; } // % of optimal charge
            public string ChargeStatus { get; set; } = string.Empty; // Undercharged, Optimal, Overcharged

            // Diagnostic Results
            public SubcoolDiagnosticResult PrimaryDiagnostic { get; set; } = new();
            public List<SubcoolDiagnosticResult> AllDiagnostics { get; set; } = new();

            // Commercial Cost Impact
            public double AnnualEnergyImpact { get; set; } // $ per year
            public double CapacityLoss { get; set; } // BTU/hr lost capacity
            public double RefrigerantCostImpact { get; set; } // $ for refrigerant adjustment

            // System Health Indicators
            public bool RequiresImmediateAttention { get; set; } = false;
            public string RecommendedAction { get; set; } = string.Empty;
            public List<string> SystemWarnings { get; set; } = new();
            public double SystemHealthScore { get; set; } // 0-100%
        }

        // Subcool Diagnostic Result
        public class SubcoolDiagnosticResult
        {
            public string Id { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public string Status { get; set; } = "green"; // red, yellow, green
            public string Description { get; set; } = string.Empty;
            public double ConfidenceLevel { get; set; } = 0; // 0-100%
            public double Severity { get; set; } = 0; // 0-10 scale
            public string Category { get; set; } = string.Empty; // Charge, Performance, Equipment, Safety

            // Commercial-Specific
            public bool AffectsCapacity { get; set; } = false;
            public bool AffectsEfficiency { get; set; } = false;
            public bool RequiresServiceCall { get; set; } = false;
            public string BusinessImpact { get; set; } = string.Empty;
            public double EstimatedCost { get; set; } = 0;

            public List<string> Symptoms { get; set; } = new();
            public List<string> PossibleCauses { get; set; } = new();
            public List<string> RecommendedActions { get; set; } = new();
        }

        // Commercial Refrigerant Properties
        public class CommercialRefrigerantProperties
        {
            public string RefrigerantName { get; set; } = string.Empty;
            public string ChemicalFormula { get; set; } = string.Empty;
            public bool IsCommercialGrade { get; set; } = true;
            public bool IsLowGWP { get; set; } = false; // Global Warming Potential
            public string TypicalApplications { get; set; } = string.Empty;
            public string SafetyClassification { get; set; } = string.Empty; // A1, A2L, etc.

            // Pressure-Temperature Relationship Coefficients (for approximation)
            public double PressureTempCoeffA { get; set; } = 0;
            public double PressureTempCoeffB { get; set; } = 0;
            public double PressureTempCoeffC { get; set; } = 0;

            // Operating Ranges
            public double MinOperatingTemp { get; set; } = -40; // °F
            public double MaxOperatingTemp { get; set; } = 120; // °F
            public double CriticalTemp { get; set; } = 200; // °F
            public double CriticalPressure { get; set; } = 500; // PSIA

            // Commercial System Preferences
            public List<string> OptimalSystemTypes { get; set; } = new();
            public Dictionary<string, SubcoolRange> SubcoolRangesBySystem { get; set; } = new();
        }

        // Subcool Range for Different System Types
        public class SubcoolRange
        {
            public double MinSubcool { get; set; } = 5; // °F
            public double OptimalSubcool { get; set; } = 10; // °F
            public double MaxSubcool { get; set; } = 15; // °F
            public string SystemCharacteristics { get; set; } = string.Empty;
        }

        // Commercial System Subcool Standards
        public class CommercialSubcoolStandards
        {
            public Dictionary<string, SubcoolRange> StandardsBySystemType { get; set; } = new()
            {
                { "Packaged Rooftop Unit", new SubcoolRange { MinSubcool = 8, OptimalSubcool = 12, MaxSubcool = 18, SystemCharacteristics = "Standard commercial cooling" } },
                { "Air Cooled Chiller", new SubcoolRange { MinSubcool = 5, OptimalSubcool = 8, MaxSubcool = 12, SystemCharacteristics = "Precise temperature control" } },
                { "Water Cooled Chiller", new SubcoolRange { MinSubcool = 4, OptimalSubcool = 7, MaxSubcool = 10, SystemCharacteristics = "High efficiency operation" } },
                { "Heat Pump", new SubcoolRange { MinSubcool = 6, OptimalSubcool = 10, MaxSubcool = 16, SystemCharacteristics = "Dual mode operation" } },
                { "Process Cooling", new SubcoolRange { MinSubcool = 10, OptimalSubcool = 15, MaxSubcool = 20, SystemCharacteristics = "Industrial applications" } },
                { "Data Center Cooling", new SubcoolRange { MinSubcool = 8, OptimalSubcool = 12, MaxSubcool = 16, SystemCharacteristics = "Reliable operation critical" } },
                { "Supermarket Refrigeration", new SubcoolRange { MinSubcool = 12, OptimalSubcool = 18, MaxSubcool = 25, SystemCharacteristics = "Large refrigerant charges" } },
                { "Industrial Refrigeration", new SubcoolRange { MinSubcool = 15, OptimalSubcool = 20, MaxSubcool = 30, SystemCharacteristics = "High capacity systems" } }
            };

            public Dictionary<string, SubcoolRange> StandardsByCapacity { get; set; } = new()
            {
                { "Small Commercial (2-10 tons)", new SubcoolRange { MinSubcool = 8, OptimalSubcool = 12, MaxSubcool = 18 } },
                { "Medium Commercial (10-30 tons)", new SubcoolRange { MinSubcool = 10, OptimalSubcool = 14, MaxSubcool = 20 } },
                { "Large Commercial (30-75 tons)", new SubcoolRange { MinSubcool = 12, OptimalSubcool = 16, MaxSubcool = 22 } },
                { "Industrial (75+ tons)", new SubcoolRange { MinSubcool = 15, OptimalSubcool = 20, MaxSubcool = 28 } }
            };
        }

        // Refrigerant Pressure-Temperature Data Points
        public class RefrigerantPTData
        {
            public Dictionary<string, List<PressureTemperaturePoint>> RefrigerantData { get; set; } = new()
            {
                // R-410A (Most common commercial)
                { "R-410A", new List<PressureTemperaturePoint>
                    {
                        new() { TempF = 40, PressurePSIA = 118.8 },
                        new() { TempF = 50, PressurePSIA = 147.8 },
                        new() { TempF = 60, PressurePSIA = 181.8 },
                        new() { TempF = 70, PressurePSIA = 221.4 },
                        new() { TempF = 80, PressurePSIA = 267.0 },
                        new() { TempF = 90, PressurePSIA = 319.2 },
                        new() { TempF = 100, PressurePSIA = 378.6 },
                        new() { TempF = 110, PressurePSIA = 446.4 },
                        new() { TempF = 120, PressurePSIA = 523.1 },
                        new() { TempF = 130, PressurePSIA = 609.2 }
                    }
                },
                
                // R-22 (Legacy commercial systems)
                { "R-22", new List<PressureTemperaturePoint>
                    {
                        new() { TempF = 40, PressurePSIA = 83.2 },
                        new() { TempF = 50, PressurePSIA = 102.6 },
                        new() { TempF = 60, PressurePSIA = 125.4 },
                        new() { TempF = 70, PressurePSIA = 151.9 },
                        new() { TempF = 80, PressurePSIA = 182.4 },
                        new() { TempF = 90, PressurePSIA = 217.2 },
                        new() { TempF = 100, PressurePSIA = 256.6 },
                        new() { TempF = 110, PressurePSIA = 301.0 },
                        new() { TempF = 120, PressurePSIA = 350.7 },
                        new() { TempF = 130, PressurePSIA = 406.2 }
                    }
                },
                
                // R-134a (Chiller applications)
                { "R-134a", new List<PressureTemperaturePoint>
                    {
                        new() { TempF = 40, PressurePSIA = 51.2 },
                        new() { TempF = 50, PressurePSIA = 63.2 },
                        new() { TempF = 60, PressurePSIA = 77.2 },
                        new() { TempF = 70, PressurePSIA = 93.3 },
                        new() { TempF = 80, PressurePSIA = 111.9 },
                        new() { TempF = 90, PressurePSIA = 133.1 },
                        new() { TempF = 100, PressurePSIA = 157.1 },
                        new() { TempF = 110, PressurePSIA = 184.2 },
                        new() { TempF = 120, PressurePSIA = 214.7 },
                        new() { TempF = 130, PressurePSIA = 248.9 }
                    }
                },
                
                // R-407C (R-22 replacement)
                { "R-407C", new List<PressureTemperaturePoint>
                    {
                        new() { TempF = 40, PressurePSIA = 82.1 },
                        new() { TempF = 50, PressurePSIA = 101.2 },
                        new() { TempF = 60, PressurePSIA = 123.4 },
                        new() { TempF = 70, PressurePSIA = 149.1 },
                        new() { TempF = 80, PressurePSIA = 178.6 },
                        new() { TempF = 90, PressurePSIA = 212.3 },
                        new() { TempF = 100, PressurePSIA = 250.4 },
                        new() { TempF = 110, PressurePSIA = 293.2 },
                        new() { TempF = 120, PressurePSIA = 341.1 },
                        new() { TempF = 130, PressurePSIA = 394.3 }
                    }
                },
                
                // R-32 (New high-efficiency systems)
                { "R-32", new List<PressureTemperaturePoint>
                    {
                        new() { TempF = 40, PressurePSIA = 136.2 },
                        new() { TempF = 50, PressurePSIA = 168.4 },
                        new() { TempF = 60, PressurePSIA = 206.8 },
                        new() { TempF = 70, PressurePSIA = 251.9 },
                        new() { TempF = 80, PressurePSIA = 304.2 },
                        new() { TempF = 90, PressurePSIA = 364.3 },
                        new() { TempF = 100, PressurePSIA = 432.8 },
                        new() { TempF = 110, PressurePSIA = 510.4 },
                        new() { TempF = 120, PressurePSIA = 597.8 },
                        new() { TempF = 130, PressurePSIA = 695.7 }
                    }
                },
                
                // R-454B (Low GWP replacement)
                { "R-454B", new List<PressureTemperaturePoint>
                    {
                        new() { TempF = 40, PressurePSIA = 120.5 },
                        new() { TempF = 50, PressurePSIA = 149.8 },
                        new() { TempF = 60, PressurePSIA = 184.2 },
                        new() { TempF = 70, PressurePSIA = 224.1 },
                        new() { TempF = 80, PressurePSIA = 270.0 },
                        new() { TempF = 90, PressurePSIA = 322.4 },
                        new() { TempF = 100, PressurePSIA = 382.0 },
                        new() { TempF = 110, PressurePSIA = 449.3 },
                        new() { TempF = 120, PressurePSIA = 525.1 },
                        new() { TempF = 130, PressurePSIA = 610.0 }
                    }
                },
                
                // R-513A (Low GWP R-134a replacement)
                { "R-513A", new List<PressureTemperaturePoint>
                    {
                        new() { TempF = 40, PressurePSIA = 52.8 },
                        new() { TempF = 50, PressurePSIA = 65.1 },
                        new() { TempF = 60, PressurePSIA = 79.4 },
                        new() { TempF = 70, PressurePSIA = 95.9 },
                        new() { TempF = 80, PressurePSIA = 114.8 },
                        new() { TempF = 90, PressurePSIA = 136.4 },
                        new() { TempF = 100, PressurePSIA = 160.9 },
                        new() { TempF = 110, PressurePSIA = 188.5 },
                        new() { TempF = 120, PressurePSIA = 219.6 },
                        new() { TempF = 130, PressurePSIA = 254.4 }
                    }
                }
            };
        }

        public class PressureTemperaturePoint
        {
            public double TempF { get; set; }
            public double PressurePSIA { get; set; }
        }

        // Commercial Cost Factors for Subcool Issues
        public class SubcoolCostFactors
        {
            public Dictionary<string, double> RefrigerantCostPerPound { get; set; } = new()
            {
                { "R-410A", 12.50 },
                { "R-22", 45.00 }, // Expensive due to phaseout
                { "R-134a", 15.00 },
                { "R-407C", 18.00 },
                { "R-32", 20.00 },
                { "R-454B", 35.00 }, // New technology premium
                { "R-513A", 25.00 }
            };

            public Dictionary<string, double> TypicalChargeBySize { get; set; } = new()
            {
                { "Small Commercial (2-10 tons)", 25 }, // lbs average
                { "Medium Commercial (10-30 tons)", 75 }, // lbs average
                { "Large Commercial (30-75 tons)", 200 }, // lbs average
                { "Industrial (75+ tons)", 500 } // lbs average
            };

            public Dictionary<string, double> ServiceCallCosts { get; set; } = new()
            {
                { "Basic Charge Adjustment", 350 },
                { "Leak Detection and Repair", 850 },
                { "Major Leak Repair", 1500 },
                { "System Evacuation and Recharge", 1200 },
                { "Receiver Replacement", 2500 }
            };
        }

        // ==================== REFRIGERANT PRESSURE-TEMPERATURE CHART ====================

        // RPT Chart Inputs
        public class RPTInputs
        {
            public string RefrigerantType { get; set; } = "R-410A";
            public double InputPressure { get; set; } = 100; // PSIG
            public string PressureUnit { get; set; } = "PSIG"; // PSIG, PSIA
            public string LookupMode { get; set; } = "Pressure to Temperature"; // "Pressure to Temperature", "Temperature to Pressure"
            public double InputTemperature { get; set; } = 40; // °F (for reverse lookup)

            // System Context (optional)
            public string SystemType { get; set; } = "Unknown";
            public double SystemCapacity { get; set; } = 0; // BTU/hr
            public string ApplicationNotes { get; set; } = string.Empty;
        }

        // RPT Chart Results
        public class RPTResults
        {
            public double ResultTemperature { get; set; } // °F
            public double ResultPressure { get; set; } // PSIG
            public string RefrigerantName { get; set; } = string.Empty;
            public string RefrigerantType { get; set; } = string.Empty;
            public bool IsValidLookup { get; set; } = true;
            public string LookupMethod { get; set; } = string.Empty;

            // Additional Information
            public string RefrigerantProperties { get; set; } = string.Empty;
            public string SafetyClassification { get; set; } = string.Empty;
            public string TypicalApplications { get; set; } = string.Empty;
            public List<string> OperatingNotes { get; set; } = new();

            // Related Values (for context)
            public double PressurePSIA { get; set; } // Absolute pressure
            public string DataQuality { get; set; } = string.Empty; // Interpolated, Exact, Extrapolated
            public double InterpolationError { get; set; } = 0; // Estimated error in interpolation
        }

        // ==================== WET BULB TO ENTHALPY CALCULATOR ====================

        

        // ==================== VERIFIED REFRIGERANT DATA FROM JIM ====================

        // Extracted Refrigerant Pressure-Temperature Data (Verified from Excel)
        public class VerifiedRefrigerantData
        {
            public Dictionary<string, List<RPTDataPoint>> RefrigerantPTData { get; set; } = new()
            {
                // R-134A Data Points (Extracted from Excel)
                { "R-134A", new List<RPTDataPoint>
                    {
                        new() { TempF = -48, PressurePSIG = 1.6 },
                        new() { TempF = -44, PressurePSIG = 1.1 },
                        new() { TempF = -40, PressurePSIG = 3.3 },
                        new() { TempF = -36, PressurePSIG = 5.6 },
                        new() { TempF = -32, PressurePSIG = 8.2 },
                        new() { TempF = -28, PressurePSIG = 11.0 },
                        new() { TempF = -24, PressurePSIG = 14.1 },
                        new() { TempF = -20, PressurePSIG = 17.5 },
                        new() { TempF = -16, PressurePSIG = 21.2 },
                        new() { TempF = -12, PressurePSIG = 25.2 },
                        new() { TempF = -8, PressurePSIG = 29.5 },
                        new() { TempF = -4, PressurePSIG = 34.1 },
                        new() { TempF = 0, PressurePSIG = 39.1 },
                        new() { TempF = 4, PressurePSIG = 44.5 },
                        new() { TempF = 8, PressurePSIG = 50.3 },
                        new() { TempF = 12, PressurePSIG = 56.5 },
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
                        new() { TempF = 60, PressurePSIG = 177.2 },
                        new() { TempF = 64, PressurePSIG = 192.1 },
                        new() { TempF = 68, PressurePSIG = 207.9 },
                        new() { TempF = 72, PressurePSIG = 224.7 },
                        new() { TempF = 76, PressurePSIG = 242.6 },
                        new() { TempF = 80, PressurePSIG = 261.6 },
                        new() { TempF = 84, PressurePSIG = 281.8 },
                        new() { TempF = 88, PressurePSIG = 303.2 },
                        new() { TempF = 92, PressurePSIG = 325.9 },
                        new() { TempF = 96, PressurePSIG = 350.0 },
                        new() { TempF = 100, PressurePSIG = 375.5 },
                        new() { TempF = 104, PressurePSIG = 402.5 },
                        new() { TempF = 108, PressurePSIG = 431.0 },
                        new() { TempF = 112, PressurePSIG = 461.1 },
                        new() { TempF = 116, PressurePSIG = 492.8 },
                        new() { TempF = 120, PressurePSIG = 526.3 }
                    }
                },

                // R-410A Data Points (Most Common Commercial)
                { "R-410A", new List<RPTDataPoint>
                    {
                        new() { TempF = -48, PressurePSIG = 6.0 },
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
                        new() { TempF = 4, PressurePSIG = 54.4 },
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
                        new() { TempF = 96, PressurePSIG = 376.0 },
                        new() { TempF = 100, PressurePSIG = 402.8 },
                        new() { TempF = 104, PressurePSIG = 431.2 },
                        new() { TempF = 108, PressurePSIG = 461.2 },
                        new() { TempF = 112, PressurePSIG = 492.9 },
                        new() { TempF = 116, PressurePSIG = 526.4 },
                        new() { TempF = 120, PressurePSIG = 561.8 }
                    }
                },

                // R-22 Data Points (Legacy Systems)
                { "R-22", new List<RPTDataPoint>
                    {
                        new() { TempF = -48, PressurePSIG = 4.8 },
                        new() { TempF = -44, PressurePSIG = 1.9 },
                        new() { TempF = -40, PressurePSIG = 0.6 },
                        new() { TempF = -36, PressurePSIG = 2.2 },
                        new() { TempF = -32, PressurePSIG = 4.0 },
                        new() { TempF = -28, PressurePSIG = 5.9 },
                        new() { TempF = -24, PressurePSIG = 8.0 },
                        new() { TempF = -20, PressurePSIG = 10.2 },
                        new() { TempF = -16, PressurePSIG = 12.6 },
                        new() { TempF = -12, PressurePSIG = 15.2 },
                        new() { TempF = -8, PressurePSIG = 18.0 },
                        new() { TempF = -4, PressurePSIG = 21.0 },
                        new() { TempF = 0, PressurePSIG = 24.2 },
                        new() { TempF = 4, PressurePSIG = 27.6 },
                        new() { TempF = 8, PressurePSIG = 31.3 },
                        new() { TempF = 12, PressurePSIG = 35.2 },
                        new() { TempF = 16, PressurePSIG = 39.4 },
                        new() { TempF = 20, PressurePSIG = 43.8 },
                        new() { TempF = 24, PressurePSIG = 48.5 },
                        new() { TempF = 28, PressurePSIG = 53.5 },
                        new() { TempF = 32, PressurePSIG = 58.8 },
                        new() { TempF = 36, PressurePSIG = 64.4 },
                        new() { TempF = 40, PressurePSIG = 70.4 },
                        new() { TempF = 44, PressurePSIG = 76.7 },
                        new() { TempF = 48, PressurePSIG = 83.4 },
                        new() { TempF = 52, PressurePSIG = 90.5 },
                        new() { TempF = 56, PressurePSIG = 98.0 },
                        new() { TempF = 60, PressurePSIG = 106.0 },
                        new() { TempF = 64, PressurePSIG = 114.4 },
                        new() { TempF = 68, PressurePSIG = 123.3 },
                        new() { TempF = 72, PressurePSIG = 132.7 },
                        new() { TempF = 76, PressurePSIG = 142.6 },
                        new() { TempF = 80, PressurePSIG = 153.0 },
                        new() { TempF = 84, PressurePSIG = 164.0 },
                        new() { TempF = 88, PressurePSIG = 175.6 },
                        new() { TempF = 92, PressurePSIG = 187.8 },
                        new() { TempF = 96, PressurePSIG = 200.6 },
                        new() { TempF = 100, PressurePSIG = 214.1 },
                        new() { TempF = 104, PressurePSIG = 228.3 },
                        new() { TempF = 108, PressurePSIG = 243.3 },
                        new() { TempF = 112, PressurePSIG = 259.0 },
                        new() { TempF = 116, PressurePSIG = 275.5 },
                        new() { TempF = 120, PressurePSIG = 292.9 }
                    }
                },

                // R-32 Data Points (High Efficiency)
                { "R-32", new List<RPTDataPoint>
                    {
                        new() { TempF = -48, PressurePSIG = 6.2 },
                        new() { TempF = -44, PressurePSIG = 8.5 },
                        new() { TempF = -40, PressurePSIG = 11.0 },
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
                        new() { TempF = 12, PressurePSIG = 66.9 },
                        new() { TempF = 16, PressurePSIG = 73.6 },
                        new() { TempF = 20, PressurePSIG = 80.7 },
                        new() { TempF = 24, PressurePSIG = 88.3 },
                        new() { TempF = 28, PressurePSIG = 96.4 },
                        new() { TempF = 32, PressurePSIG = 105.0 },
                        new() { TempF = 36, PressurePSIG = 114.1 },
                        new() { TempF = 40, PressurePSIG = 123.8 },
                        new() { TempF = 44, PressurePSIG = 134.1 },
                        new() { TempF = 48, PressurePSIG = 145.0 },
                        new() { TempF = 52, PressurePSIG = 156.6 },
                        new() { TempF = 56, PressurePSIG = 168.8 },
                        new() { TempF = 60, PressurePSIG = 181.8 },
                        new() { TempF = 64, PressurePSIG = 195.5 },
                        new() { TempF = 68, PressurePSIG = 210.0 },
                        new() { TempF = 72, PressurePSIG = 225.3 },
                        new() { TempF = 76, PressurePSIG = 241.5 },
                        new() { TempF = 80, PressurePSIG = 258.6 },
                        new() { TempF = 84, PressurePSIG = 276.6 },
                        new() { TempF = 88, PressurePSIG = 295.6 },
                        new() { TempF = 92, PressurePSIG = 315.6 },
                        new() { TempF = 96, PressurePSIG = 336.7 },
                        new() { TempF = 100, PressurePSIG = 358.9 },
                        new() { TempF = 104, PressurePSIG = 382.3 },
                        new() { TempF = 108, PressurePSIG = 406.9 },
                        new() { TempF = 112, PressurePSIG = 432.8 },
                        new() { TempF = 116, PressurePSIG = 460.1 },
                        new() { TempF = 120, PressurePSIG = 488.7 }
                    }
                },

                // R-454B Data Points (Low GWP R-410A Replacement)
                { "R-454B", new List<RPTDataPoint>
                    {
                        new() { TempF = -48, PressurePSIG = 4.1 },
                        new() { TempF = -44, PressurePSIG = 6.2 },
                        new() { TempF = -40, PressurePSIG = 8.4 },
                        new() { TempF = -36, PressurePSIG = 10.8 },
                        new() { TempF = -32, PressurePSIG = 13.4 },
                        new() { TempF = -28, PressurePSIG = 16.2 },
                        new() { TempF = -24, PressurePSIG = 19.3 },
                        new() { TempF = -20, PressurePSIG = 22.6 },
                        new() { TempF = -16, PressurePSIG = 26.1 },
                        new() { TempF = -12, PressurePSIG = 29.9 },
                        new() { TempF = -8, PressurePSIG = 34.0 },
                        new() { TempF = -4, PressurePSIG = 38.4 },
                        new() { TempF = 0, PressurePSIG = 43.1 },
                        new() { TempF = 4, PressurePSIG = 48.1 },
                        new() { TempF = 8, PressurePSIG = 53.5 },
                        new() { TempF = 12, PressurePSIG = 59.2 },
                        new() { TempF = 16, PressurePSIG = 65.3 },
                        new() { TempF = 20, PressurePSIG = 71.8 },
                        new() { TempF = 24, PressurePSIG = 78.7 },
                        new() { TempF = 28, PressurePSIG = 86.1 },
                        new() { TempF = 32, PressurePSIG = 94.0 },
                        new() { TempF = 36, PressurePSIG = 102.4 },
                        new() { TempF = 40, PressurePSIG = 111.3 },
                        new() { TempF = 44, PressurePSIG = 120.8 },
                        new() { TempF = 48, PressurePSIG = 130.8 },
                        new() { TempF = 52, PressurePSIG = 141.5 },
                        new() { TempF = 56, PressurePSIG = 152.8 },
                        new() { TempF = 60, PressurePSIG = 164.8 },
                        new() { TempF = 64, PressurePSIG = 177.5 },
                        new() { TempF = 68, PressurePSIG = 190.9 },
                        new() { TempF = 72, PressurePSIG = 205.1 },
                        new() { TempF = 76, PressurePSIG = 220.1 },
                        new() { TempF = 80, PressurePSIG = 235.9 },
                        new() { TempF = 84, PressurePSIG = 252.6 },
                        new() { TempF = 88, PressurePSIG = 270.2 },
                        new() { TempF = 92, PressurePSIG = 288.8 },
                        new() { TempF = 96, PressurePSIG = 308.4 },
                        new() { TempF = 100, PressurePSIG = 329.0 },
                        new() { TempF = 104, PressurePSIG = 350.7 },
                        new() { TempF = 108, PressurePSIG = 373.5 },
                        new() { TempF = 112, PressurePSIG = 397.5 },
                        new() { TempF = 116, PressurePSIG = 422.7 },
                        new() { TempF = 120, PressurePSIG = 449.2 }
                    }
                }

                // Note: Additional refrigerants (R-427A, R-407C, R-422B, R-438A, R-404A, R-448A, R-449A, R-454C, R-513A) 
                // would be added here with their complete data sets from the Excel file
            };

            // Refrigerant Properties
            public Dictionary<string, RefrigerantInfo> RefrigerantProperties { get; set; } = new()
            {
                { "R-134A", new RefrigerantInfo
                    {
                        FullName = "R-134A (Tetrafluoroethane)",
                        SafetyClass = "A1 (Non-toxic, Non-flammable)",
                        TypicalUse = "Chiller applications, automotive AC",
                        GWP = 1430,
                        ODP = 0,
                        OperatingRange = "-40°F to 120°F",
                        Notes = "HFC refrigerant, being phased down under Kigali Amendment"
                    }
                },
                { "R-410A", new RefrigerantInfo
                    {
                        FullName = "R-410A (Difluoromethane/Pentafluoroethane)",
                        SafetyClass = "A1 (Non-toxic, Non-flammable)",
                        TypicalUse = "Standard commercial HVAC, residential systems",
                        GWP = 2088,
                        ODP = 0,
                        OperatingRange = "-48°F to 120°F",
                        Notes = "Most common commercial refrigerant, higher pressures than R-22"
                    }
                },
                { "R-22", new RefrigerantInfo
                    {
                        FullName = "R-22 (Chlorodifluoromethane)",
                        SafetyClass = "A1 (Non-toxic, Non-flammable)",
                        TypicalUse = "Legacy HVAC systems",
                        GWP = 1810,
                        ODP = 0.055,
                        OperatingRange = "-48°F to 120°F",
                        Notes = "HCFC being phased out, very expensive, limited availability"
                    }
                },
                { "R-32", new RefrigerantInfo
                    {
                        FullName = "R-32 (Difluoromethane)",
                        SafetyClass = "A2L (Non-toxic, Mildly flammable)",
                        TypicalUse = "High-efficiency HVAC systems",
                        GWP = 675,
                        ODP = 0,
                        OperatingRange = "-48°F to 120°F",
                        Notes = "Lower GWP, higher efficiency, requires A2L safety protocols"
                    }
                },
                { "R-454B", new RefrigerantInfo
                    {
                        FullName = "R-454B (Difluoromethane/Trifluoroiodomethane)",
                        SafetyClass = "A2L (Non-toxic, Mildly flammable)",
                        TypicalUse = "R-410A replacement in new equipment",
                        GWP = 466,
                        ODP = 0,
                        OperatingRange = "-48°F to 120°F",
                        Notes = "Low GWP R-410A alternative, requires compatible equipment"
                    }
                }
            };
        }

        public class RPTDataPoint
        {
            public double TempF { get; set; }
            public double PressurePSIG { get; set; }
        }

        public class RefrigerantInfo
        {
            public string FullName { get; set; } = string.Empty;
            public string SafetyClass { get; set; } = string.Empty;
            public string TypicalUse { get; set; } = string.Empty;
            public int GWP { get; set; } = 0; // Global Warming Potential
            public double ODP { get; set; } = 0; // Ozone Depletion Potential
            public string OperatingRange { get; set; } = string.Empty;
            public string Notes { get; set; } = string.Empty;
        }

        // ==================== VERIFIED WET BULB DATA ====================

        // Wet Bulb to Enthalpy Data 
        public class WetBulbInputs
        {
            public string CalculationMode { get; set; } = "Wet Bulb to Enthalpy";
            public double InputWetBulb { get; set; } = 67.5;
            public double InputEnthalpy { get; set; } = 30.0;

            // Measurement Context
            public string MeasurementLocation { get; set; } = "Unknown";
            public double? DryBulbTemp { get; set; }
            public double? RelativeHumidity { get; set; }
            public double? Altitude { get; set; }

            // System Information
            public string EquipmentType { get; set; } = "Unknown";
            public double SystemCFM { get; set; } = 0;
            public string MeasurementNotes { get; set; } = string.Empty;
        }

        public class WetBulbResults
        {
            public double ResultWetBulb { get; set; }
            public double ResultEnthalpy { get; set; }
            public bool IsValidCalculation { get; set; } = true;
            public string CalculationMethod { get; set; } = string.Empty;
            public string ErrorMessage { get; set; } = string.Empty;

            // Psychrometric Properties
            public double SensibleHeat { get; set; }
            public double LatentHeat { get; set; }
            public double MoistureContent { get; set; } // grains/lb
            public double? DewPointTemp { get; set; }
            public double? SpecificVolume { get; set; }
            public double? HumidityRatio { get; set; }
            public double AirDensity { get; set; }

            // System Calculations
            public double TotalCoolingCapacity { get; set; } // BTU/hr
            public double SensibleHeatRatio { get; set; }

            // Quality and Notes
            public List<string> OperatingNotes { get; set; } = new();
            public string DataQuality { get; set; } = string.Empty;
            public double InterpolationError { get; set; } = 0;
        }


        public class VerifiedWetBulbData
        {
            public List<WetBulbDataPoint> WetBulbEnthalpyData { get; set; } = new()
            {
                // Full dataset from 35.0°F to 85.0°F in 0.1°F increments (510 points)
                // Showing key ranges for commercial HVAC applications
                
                // Winter/Low Range (35-45°F)
                new() { WetBulbTempF = 35.0, Enthalpy = 13.01 },
                new() { WetBulbTempF = 35.5, Enthalpy = 13.23 },
                new() { WetBulbTempF = 36.0, Enthalpy = 13.45 },
                new() { WetBulbTempF = 36.5, Enthalpy = 13.67 },
                new() { WetBulbTempF = 37.0, Enthalpy = 13.89 },
                new() { WetBulbTempF = 37.5, Enthalpy = 14.12 },
                new() { WetBulbTempF = 38.0, Enthalpy = 14.35 },
                new() { WetBulbTempF = 38.5, Enthalpy = 14.58 },
                new() { WetBulbTempF = 39.0, Enthalpy = 14.81 },
                new() { WetBulbTempF = 39.5, Enthalpy = 15.05 },
                new() { WetBulbTempF = 40.0, Enthalpy = 15.29 },
                new() { WetBulbTempF = 41.0, Enthalpy = 15.77 },
                new() { WetBulbTempF = 42.0, Enthalpy = 16.26 },
                new() { WetBulbTempF = 43.0, Enthalpy = 16.76 },
                new() { WetBulbTempF = 44.0, Enthalpy = 17.27 },
                new() { WetBulbTempF = 45.0, Enthalpy = 17.79 },
                
                // Dehumidification Range (45-55°F)
                new() { WetBulbTempF = 46.0, Enthalpy = 18.32 },
                new() { WetBulbTempF = 47.0, Enthalpy = 18.86 },
                new() { WetBulbTempF = 48.0, Enthalpy = 19.41 },
                new() { WetBulbTempF = 49.0, Enthalpy = 19.97 },
                new() { WetBulbTempF = 50.0, Enthalpy = 20.54 },
                new() { WetBulbTempF = 51.0, Enthalpy = 21.12 },
                new() { WetBulbTempF = 52.0, Enthalpy = 21.71 },
                new() { WetBulbTempF = 53.0, Enthalpy = 22.31 },
                new() { WetBulbTempF = 54.0, Enthalpy = 22.93 },
                new() { WetBulbTempF = 55.0, Enthalpy = 23.55 },
                
                // Supply Air Range (55-60°F)
                new() { WetBulbTempF = 55.5, Enthalpy = 23.87 },
                new() { WetBulbTempF = 56.0, Enthalpy = 24.19 },
                new() { WetBulbTempF = 56.5, Enthalpy = 24.52 },
                new() { WetBulbTempF = 57.0, Enthalpy = 24.85 },
                new() { WetBulbTempF = 57.5, Enthalpy = 25.18 },
                new() { WetBulbTempF = 58.0, Enthalpy = 25.52 },
                new() { WetBulbTempF = 58.5, Enthalpy = 25.86 },
                new() { WetBulbTempF = 59.0, Enthalpy = 26.21 },
                new() { WetBulbTempF = 59.5, Enthalpy = 26.56 },
                new() { WetBulbTempF = 60.0, Enthalpy = 26.91 },
                
                // Comfort Zone (60-70°F)
                new() { WetBulbTempF = 61.0, Enthalpy = 27.63 },
                new() { WetBulbTempF = 62.0, Enthalpy = 28.36 },
                new() { WetBulbTempF = 63.0, Enthalpy = 29.11 },
                new() { WetBulbTempF = 64.0, Enthalpy = 29.87 },
                new() { WetBulbTempF = 65.0, Enthalpy = 30.65 },
                new() { WetBulbTempF = 66.0, Enthalpy = 31.44 },
                new() { WetBulbTempF = 67.0, Enthalpy = 32.25 },
                new() { WetBulbTempF = 67.5, Enthalpy = 32.66 }, // Common design condition
                new() { WetBulbTempF = 68.0, Enthalpy = 33.08 },
                new() { WetBulbTempF = 69.0, Enthalpy = 33.92 },
                new() { WetBulbTempF = 70.0, Enthalpy = 34.78 },
                
                // High Load Range (70-80°F)
                new() { WetBulbTempF = 71.0, Enthalpy = 35.66 },
                new() { WetBulbTempF = 72.0, Enthalpy = 36.55 },
                new() { WetBulbTempF = 73.0, Enthalpy = 37.47 },
                new() { WetBulbTempF = 74.0, Enthalpy = 38.40 },
                new() { WetBulbTempF = 75.0, Enthalpy = 39.36 },
                new() { WetBulbTempF = 76.0, Enthalpy = 40.34 },
                new() { WetBulbTempF = 77.0, Enthalpy = 41.34 },
                new() { WetBulbTempF = 78.0, Enthalpy = 42.36 }, // Summer design
                new() { WetBulbTempF = 79.0, Enthalpy = 43.41 },
                new() { WetBulbTempF = 80.0, Enthalpy = 44.48 },
                
                // Extreme Conditions (80-85°F)
                new() { WetBulbTempF = 81.0, Enthalpy = 45.58 },
                new() { WetBulbTempF = 82.0, Enthalpy = 46.70 },
                new() { WetBulbTempF = 83.0, Enthalpy = 47.85 },
                new() { WetBulbTempF = 84.0, Enthalpy = 49.03 },
                new() { WetBulbTempF = 85.0, Enthalpy = 50.24 }
                
                // Note: In production, all 510 data points would be included
                // This sample shows key ranges for commercial HVAC applications
            };

            public WetBulbDataPoint FindNearestDataPoint(double wetBulbTemp)
            {
                return WetBulbEnthalpyData
                    .OrderBy(p => Math.Abs(p.WetBulbTempF - wetBulbTemp))
                    .First();
            }

            public List<WetBulbDataPoint> GetInterpolationPoints(double value, bool byWetBulb = true)
            {
                var sortedData = byWetBulb
                    ? WetBulbEnthalpyData.OrderBy(p => p.WetBulbTempF).ToList()
                    : WetBulbEnthalpyData.OrderBy(p => p.Enthalpy).ToList();

                for (int i = 0; i < sortedData.Count - 1; i++)
                {
                    var current = byWetBulb ? sortedData[i].WetBulbTempF : sortedData[i].Enthalpy;
                    var next = byWetBulb ? sortedData[i + 1].WetBulbTempF : sortedData[i + 1].Enthalpy;

                    if (value >= current && value <= next)
                    {
                        return new List<WetBulbDataPoint> { sortedData[i], sortedData[i + 1] };
                    }
                }

                return new List<WetBulbDataPoint>();
            }
        }

        public class WetBulbDataPoint
        {
            public double WetBulbTempF { get; set; }
            public double Enthalpy { get; set; } // BTU/lb dry air
        }

        // Psychrometric Constants and Helpers
        public static class PsychrometricConstants
        {
            public const double StandardAtmosphericPressure = 14.696; // psia
            public const double SpecificHeatDryAir = 0.24; // BTU/lb·°F
            public const double SpecificHeatWaterVapor = 0.444; // BTU/lb·°F
            public const double LatentHeatVaporization = 1061; // BTU/lb at 32°F
            public const double GasConstantDryAir = 53.35; // ft·lbf/lb·°R
            public const double GasConstantWaterVapor = 85.78; // ft·lbf/lb·°R
            public const double GrainsPerPound = 7000; // Conversion factor
        }

        // Design Conditions for Common Cities
        public class DesignConditions
        {
            public string City { get; set; } = string.Empty;
            public double SummerDBTemp { get; set; } // °F
            public double SummerWBTemp { get; set; } // °F
            public double WinterDBTemp { get; set; } // °F
            public double Altitude { get; set; } // ft
        }

        public static List<DesignConditions> CommonDesignConditions = new()
        {
            new() { City = "Atlanta, GA", SummerDBTemp = 92, SummerWBTemp = 74, WinterDBTemp = 22, Altitude = 1050 },
            new() { City = "Chicago, IL", SummerDBTemp = 91, SummerWBTemp = 75, WinterDBTemp = -4, Altitude = 595 },
            new() { City = "Dallas, TX", SummerDBTemp = 100, SummerWBTemp = 78, WinterDBTemp = 22, Altitude = 435 },
            new() { City = "Denver, CO", SummerDBTemp = 91, SummerWBTemp = 59, WinterDBTemp = -2, Altitude = 5280 },
            new() { City = "Houston, TX", SummerDBTemp = 95, SummerWBTemp = 78, WinterDBTemp = 29, Altitude = 50 },
            new() { City = "Los Angeles, CA", SummerDBTemp = 83, SummerWBTemp = 69, WinterDBTemp = 43, Altitude = 340 },
            new() { City = "Miami, FL", SummerDBTemp = 91, SummerWBTemp = 78, WinterDBTemp = 47, Altitude = 10 },
            new() { City = "New York, NY", SummerDBTemp = 91, SummerWBTemp = 74, WinterDBTemp = 13, Altitude = 35 },
            new() { City = "Phoenix, AZ", SummerDBTemp = 110, SummerWBTemp = 71, WinterDBTemp = 34, Altitude = 1090 },
            new() { City = "Seattle, WA", SummerDBTemp = 84, SummerWBTemp = 66, WinterDBTemp = 24, Altitude = 175 }
        };

        // Equipment Performance Factors
        public class CoolingPerformanceFactors
        {
            public double WetBulbEntering { get; set; }
            public double CapacityMultiplier { get; set; }
            public double PowerMultiplier { get; set; }
        }

        public static List<CoolingPerformanceFactors> TypicalPerformanceFactors = new()
        {
            new() { WetBulbEntering = 57, CapacityMultiplier = 0.82, PowerMultiplier = 0.88 },
            new() { WetBulbEntering = 62, CapacityMultiplier = 0.91, PowerMultiplier = 0.94 },
            new() { WetBulbEntering = 67, CapacityMultiplier = 1.00, PowerMultiplier = 1.00 }, // ARI Standard
            new() { WetBulbEntering = 72, CapacityMultiplier = 1.09, PowerMultiplier = 1.06 },
            new() { WetBulbEntering = 77, CapacityMultiplier = 1.18, PowerMultiplier = 1.12 },
            new() { WetBulbEntering = 82, CapacityMultiplier = 1.27, PowerMultiplier = 1.18 }
        };

        // Common HVAC Calculations
        public static class HVACFormulas
        {
            // Total cooling load from air conditions
            public static double TotalCoolingBTUH(double cfm, double enthalpyIn, double enthalpyOut)
            {
                return 4.5 * cfm * (enthalpyIn - enthalpyOut);
            }

            // Sensible cooling load
            public static double SensibleCoolingBTUH(double cfm, double tempIn, double tempOut)
            {
                return 1.08 * cfm * (tempIn - tempOut);
            }

            // Latent cooling load
            public static double LatentCoolingBTUH(double cfm, double grainsIn, double grainsOut)
            {
                return 0.68 * cfm * (grainsIn - grainsOut);
            }

            // Sensible heat ratio
            public static double SensibleHeatRatio(double sensibleBTUH, double totalBTUH)
            {
                return totalBTUH > 0 ? sensibleBTUH / totalBTUH : 0;
            }

            // Air density at altitude
            public static double AirDensityAtAltitude(double altitudeFt, double tempF)
            {
                var standardDensity = 0.075; // lb/ft³ at sea level, 70°F
                var altitudeFactor = Math.Pow((1 - 0.0000068756 * altitudeFt), 5.2559);
                var tempRankine = tempF + 459.67;
                var tempFactor = 530 / tempRankine;

                return standardDensity * altitudeFactor * tempFactor;
            }
        }
    }
}