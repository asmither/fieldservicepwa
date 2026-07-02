//using ICS.Mobile.Helpers;
//using System.Text;

//namespace ICS.Mobile.Theme
//{
//    public class ThemeVariables
//    {
//        private readonly Dictionary<ThemeIds, ThemeVariable> LightThemeVariables;
//        private readonly Dictionary<ThemeIds, ThemeVariable> DarkThemeVariables;
//        private Dictionary<ThemeIds, ThemeVariable> themeVariables;
//        private readonly JSStorage js;

//        public ThemeVariables(JSStorage js)
//        {
//            //TODO: Copntinue filling out values according to theme.css

//            LightThemeVariables = new()
//            {
//                { ThemeIds.PrimaryBackgroundColor, ThemeVariable.Create(ThemeIds.PrimaryBackgroundColor,"#FFFFFF") },
//                { ThemeIds.PrimaryForegroundColor, ThemeVariable.Create(ThemeIds.PrimaryForegroundColor,"#000000") },
//                { ThemeIds.PrimaryBorder, ThemeVariable.Create(ThemeIds.PrimaryBorder,"#1px solid #666666") },
                
//                { ThemeIds.HomeMenuBackgroundColor, ThemeVariable.Create(ThemeIds.HomeMenuBackgroundColor,"#FFFFFF") },
//                { ThemeIds.HomeMenuForegroundColor, ThemeVariable.Create(ThemeIds.HomeMenuForegroundColor,"#000000") },
//                { ThemeIds.HomeMenuBorder, ThemeVariable.Create(ThemeIds.HomeMenuBorder,"#1px solid #666666") },
//                { ThemeIds.HomeMenuSelectedColor, ThemeVariable.Create(ThemeIds.HomeMenuSelectedColor,"#1px solid #666666") },

//                { ThemeIds.MenuBackgroundColor, ThemeVariable.Create(ThemeIds.MenuBackgroundColor,"#FFFFFF") },
//                { ThemeIds.MenuForegroundColor, ThemeVariable.Create(ThemeIds.MenuForegroundColor,"#000000") },
//                { ThemeIds.MenuBorder, ThemeVariable.Create(ThemeIds.MenuBorder,"#1px solid #666666") },

//                {ThemeIds.OkButtonBackgroundColor,ThemeVariable.Create(ThemeIds.OkButtonBackgroundColor,"green") },
//                {ThemeIds.OkButtonForegroundColor,ThemeVariable.Create(ThemeIds.OkButtonForegroundColor,"white") },
//                {ThemeIds.OkButtonBorder,ThemeVariable.Create(ThemeIds.OkButtonBackgroundColor,"#1px solid black") },

//                {ThemeIds.CancelButtonBackgroundColor,ThemeVariable.Create(ThemeIds.CancelButtonBackgroundColor,"red") },
//                {ThemeIds.CancelButtonForegroundColor,ThemeVariable.Create(ThemeIds.CancelButtonForegroundColor,"white") },
//                {ThemeIds.CancelButtonBorder,ThemeVariable.Create(ThemeIds.OkButtonBackgroundColor,"#1px solid black") },

//                {ThemeIds.SelectItemBackgroundColor,ThemeVariable.Create(ThemeIds.SelectItemBackgroundColor,"white") },
//                {ThemeIds.SelectItemForegroundColor,ThemeVariable.Create(ThemeIds.SelectItemForegroundColor,"black") },
//                {ThemeIds.SelectItemBorder,ThemeVariable.Create(ThemeIds.OkButtonBackgroundColor,"#1px solid black") },

//                {ThemeIds.SelectedItemBackgroundColor,ThemeVariable.Create(ThemeIds.SelectedItemBackgroundColor,"green") },
//                {ThemeIds.SelectedItemForegroundColor,ThemeVariable.Create(ThemeIds.SelectedItemForegroundColor,"white") },
//                {ThemeIds.SelectedItemBorder,ThemeVariable.Create(ThemeIds.SelectedItemBorder,"#1px solid black") },

//            };

//            DarkThemeVariables = new()
//            {
//                { ThemeIds.PrimaryBackgroundColor, ThemeVariable.Create(ThemeIds.PrimaryBackgroundColor,"#000000") },
//                { ThemeIds.PrimaryForegroundColor, ThemeVariable.Create(ThemeIds.PrimaryForegroundColor,"#FFFFFF") },
//                { ThemeIds.PrimaryBorder, ThemeVariable.Create(ThemeIds.PrimaryBorder,"#1px solid #666666") },
                
//                { ThemeIds.HomeMenuBackgroundColor, ThemeVariable.Create(ThemeIds.HomeMenuBackgroundColor,"#000000") },
//                { ThemeIds.HomeMenuForegroundColor, ThemeVariable.Create(ThemeIds.HomeMenuForegroundColor,"#FFFFFF") },
//                { ThemeIds.HomeMenuBorder, ThemeVariable.Create(ThemeIds.HomeMenuBorder,"#1px solid #666666") },
//                { ThemeIds.HomeMenuSelectedColor, ThemeVariable.Create(ThemeIds.HomeMenuSelectedColor,"#000000") },

//                { ThemeIds.MenuBackgroundColor, ThemeVariable.Create(ThemeIds.MenuBackgroundColor,"#000000") },
//                { ThemeIds.MenuForegroundColor, ThemeVariable.Create(ThemeIds.MenuForegroundColor,"#FFFFFF") },
//                { ThemeIds.MenuBorder, ThemeVariable.Create(ThemeIds.MenuBorder,"#1px solid #666666") },

//                { ThemeIds.OkButtonBackgroundColor,ThemeVariable.Create(ThemeIds.OkButtonBackgroundColor,"green") },
//                { ThemeIds.OkButtonForegroundColor,ThemeVariable.Create(ThemeIds.OkButtonForegroundColor,"white") },
//                { ThemeIds.OkButtonBorder,ThemeVariable.Create(ThemeIds.OkButtonBackgroundColor,"#1px solid white") },

//                {ThemeIds.CancelButtonBackgroundColor,ThemeVariable.Create(ThemeIds.CancelButtonBackgroundColor,"red") },
//                {ThemeIds.CancelButtonForegroundColor,ThemeVariable.Create(ThemeIds.CancelButtonForegroundColor,"white") },
//                {ThemeIds.CancelButtonBorder,ThemeVariable.Create(ThemeIds.OkButtonBackgroundColor,"#1px solid black") },

//                {ThemeIds.SelectedItemBackgroundColor,ThemeVariable.Create(ThemeIds.SelectedItemBackgroundColor,"black") },
//                {ThemeIds.SelectedItemForegroundColor,ThemeVariable.Create(ThemeIds.SelectedItemForegroundColor,"white") },
//                {ThemeIds.SelectedItemBorder,ThemeVariable.Create(ThemeIds.SelectedItemBorder,"#1px solid white") },

//                {ThemeIds.SelectItemBackgroundColor,ThemeVariable.Create(ThemeIds.SelectItemBackgroundColor,"black") },
//                {ThemeIds.SelectItemForegroundColor,ThemeVariable.Create(ThemeIds.SelectItemForegroundColor,"white") },
//                {ThemeIds.SelectItemBorder,ThemeVariable.Create(ThemeIds.OkButtonBackgroundColor,"#1px solid black") },

//                {ThemeIds.SelectedItemBackgroundColor,ThemeVariable.Create(ThemeIds.SelectedItemBackgroundColor,"green") },
//                {ThemeIds.SelectedItemForegroundColor,ThemeVariable.Create(ThemeIds.SelectedItemForegroundColor,"black") },
//                {ThemeIds.SelectedItemBorder,ThemeVariable.Create(ThemeIds.SelectedItemBorder,"#1px solid black") },


//            };

//            if (GetTheme() == Themes.Light)
//            {
//                themeVariables = LightThemeVariables;
//            }
//            else
//            {
//                themeVariables = DarkThemeVariables;
//            }

//            this.js = js;
//        }
//        public ThemeVariable this[ThemeIds id]
//        {
//            get
//            {
//                return themeVariables[id];
//            }
//        }

//        private Themes _Theme = Themes.Light;
//        public Themes Theme
//        {
//            set
//            {
//                if (_Theme != value)
//                {
//                    _Theme = value;
//                    SetTheme(value);
//                }
//            }
//            get
//            {
//                return _Theme;
//            }
//        }
//        private void SetTheme(Themes value)
//        {
//            try
//            {
//                var task = js.Set<int>("Theme", (int)value);
//                task.GetAwaiter().GetResult();
//                if (value == Themes.Light)
//                {
//                    themeVariables = LightThemeVariables;
//                }
//                else
//                {
//                    themeVariables = DarkThemeVariables;
//                }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine(ex.Message);
//            }

//        }

//        private async Task SetVariables()
//        {
//            await Task.Delay(1);
//            foreach (var rootVariable in themeVariables)
//            {
//                // TODO: JS.SetRootVariables?
//            }
//        }

//        public string GetThemeCSS()
//        {
//            StringBuilder bldr = new StringBuilder();
//            bldr.AppendLine(":root{");
//            foreach (var rootVariable in themeVariables)
//            {
//                bldr.AppendLine($"{rootVariable.Value.CssName}: {rootVariable.Value.CssValue};");
//            }
//            bldr.AppendLine("}");
//            return bldr.ToString();
//        }

//        private Themes GetTheme()
//        {
//            Themes result = Themes.Light;

//            try
//            {
//                Task<int?> task = js.Get<int?>("Theme");
//                int? value = task.GetAwaiter().GetResult();
//                if (value.HasValue)
//                {
//                    result = (Themes)value;
//                }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine(ex.Message);
//            }

//            return result;
//        }
//    }
//}
