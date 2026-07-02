using Microsoft.AspNetCore.Components;

using static ICS.Mobile.DataModels.Custom.HVACCalculations;


namespace ICS.Mobile.Pages.Calcs
{
    public partial class TXVCalcPage : PageBase
    {
        protected TXVInputs Inputs { get; set; } = new();
        protected TXVResults? Results { get; set; }

        protected override void OnInitialized()
        {
            // Initialize with default values
            Inputs = new TXVInputs
            {
                SystemCapacity = 36000,
                RefrigerantType = "R-410A",
                EvaporatorTemp = 40,
                CondensingTemp = 110,
                Subcooling = 10,
                Superheat = 10,
                TxvRating = 4,
                PressureDrop = 80
            };
        }

        protected void OnInputChanged()
        {
            // Auto-calculate when inputs change
            CalculateTXVSizing();
        }
        public void HomePageClick()
        {
            PageNavManager.NavigateTo("/calcmenu");
        }
        protected void CalculateTXVSizing()
        {
            try
            {
                var refrigerantProperties = GetRefrigerantProperties();

                // Convert BTU/hr to tons
                var systemTons = Inputs.SystemCapacity / 12000.0;

                // Get refrigerant properties
                var refProps = refrigerantProperties[Inputs.RefrigerantType];

                // Calculate temperature differential
                var tempDiff = Inputs.CondensingTemp - Inputs.EvaporatorTemp;

                // Calculate required refrigerant flow rate (simplified formula)
                var baseFlowRate = systemTons * 1000; // lb/hr (simplified)
                var adjustedFlowRate = baseFlowRate * refProps.DensityFactor;

                // Calculate pressure differential across TXV
                var satPressureHigh = 150 + (Inputs.CondensingTemp - 80) * 2.5; // Simplified pressure calculation
                var satPressureLow = 50 + (Inputs.EvaporatorTemp - 40) * 1.8;
                var actualPressureDrop = satPressureHigh - satPressureLow;

                // TXV capacity calculation based on pressure drop and flow
                var txvCapacityFactor = Math.Sqrt(Inputs.PressureDrop / 80.0) * refProps.PressureFactor;
                var requiredTXVCapacity = systemTons / txvCapacityFactor;

                // Sizing analysis
                var sizingRatio = Inputs.TxvRating / requiredTXVCapacity;

                string sizingStatus, recommendation;

                if (sizingRatio < 0.8)
                {
                    sizingStatus = "UNDERSIZED";
                    recommendation = "TXV is undersized. This may cause poor evaporator performance, high superheat, and reduced system capacity.";
                }
                else if (sizingRatio > 1.3)
                {
                    sizingStatus = "OVERSIZED";
                    recommendation = "TXV is oversized. This may cause hunting, unstable operation, and potential floodback.";
                }
                else
                {
                    sizingStatus = "PROPERLY SIZED";
                    recommendation = "TXV is appropriately sized for this application.";
                }

                // Additional checks
                var warnings = new List<string>();

                if (Inputs.Superheat < 5)
                    warnings.Add("Low superheat may indicate oversized TXV or other issues");
                if (Inputs.Superheat > 20)
                    warnings.Add("High superheat may indicate undersized TXV or low refrigerant charge");
                if (Inputs.Subcooling < 5)
                    warnings.Add("Low subcooling may affect TXV performance");
                if (Inputs.Subcooling > 20)
                    warnings.Add("High subcooling may indicate overcharge");
                if (tempDiff > 80)
                    warnings.Add("High temperature differential may stress the TXV");

                Results = new TXVResults
                {
                    SystemTons = systemTons,
                    AdjustedFlowRate = adjustedFlowRate,
                    ActualPressureDrop = actualPressureDrop,
                    RequiredTXVCapacity = requiredTXVCapacity,
                    SizingRatio = sizingRatio,
                    SizingStatus = sizingStatus,
                    Recommendation = recommendation,
                    Warnings = warnings,
                    TxvCapacityFactor = txvCapacityFactor
                };

                StateHasChanged();
            }
            catch (Exception ex)
            {
                // Handle calculation errors
                Results = null;
                // You could add logging here if needed
                Console.WriteLine($"Calculation error: {ex.Message}");
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
                    builder.AddAttribute(11, "d", "M9 7h6m0 10v-3m-3 3h.01M9 17h.01M9 14h.01M12 14h.01M15 11h.01M12 11h.01M9 11h.01M7 21h10a2 2 0 002-2V5a2 2 0 00-2-2H7a2 2 0 00-2 2v14a2 2 0 002 2z");
                    builder.CloseElement();
                    builder.CloseElement();
                };
            }

            return Results.SizingStatus switch
            {
                "PROPERLY SIZED" => builder =>
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
                "UNDERSIZED" => builder =>
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
                "OVERSIZED" => builder =>
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
                    builder.AddAttribute(11, "d", "M9 7h6m0 10v-3m-3 3h.01M9 17h.01M9 14h.01M12 14h.01M15 11h.01M12 11h.01M9 11h.01M7 21h10a2 2 0 002-2V5a2 2 0 00-2-2H7a2 2 0 00-2 2v14a2 2 0 002 2z");
                    builder.CloseElement();
                    builder.CloseElement();
                }
            };
        }

        // Bootstrap-specific helper methods
        protected string GetStatusAlertClass()
        {
            if (Results == null) return "alert-light";

            return Results.SizingStatus switch
            {
                "PROPERLY SIZED" => "alert-success",
                "UNDERSIZED" => "alert-danger",
                "OVERSIZED" => "alert-warning",
                _ => "alert-light"
            };
        }

        protected string GetStatusColorClass()
        {
            if (Results == null) return "";

            return Results.SizingStatus switch
            {
                "PROPERLY SIZED" => "text-success",
                "UNDERSIZED" => "text-danger",
                "OVERSIZED" => "text-warning",
                _ => ""
            };
        }

        private Dictionary<string, RefrigerantProperties> GetRefrigerantProperties()
        {
            return new Dictionary<string, RefrigerantProperties>
            {
                { "R-410A", new RefrigerantProperties { DensityFactor = 1.0, PressureFactor = 1.0 } },
                { "R-22", new RefrigerantProperties { DensityFactor = 0.85, PressureFactor = 0.9 } },
                { "R-134a", new RefrigerantProperties { DensityFactor = 0.75, PressureFactor = 0.8 } },
                { "R-407C", new RefrigerantProperties { DensityFactor = 0.95, PressureFactor = 0.95 } }
            };
        }
    }
}
