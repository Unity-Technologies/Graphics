namespace UnityEngine.Rendering
{
    /// <summary>
    /// Debug Display Settings HDR Output
    /// </summary>
    public class DebugDisplaySettingsHDROutput
    {
        static class Strings
        {
            public static readonly string hdrOutputAPI = "HDROutputSettings";
            public static readonly string displayName = "Display ";
            public static readonly string displayMain = " (main)";
            public static readonly string hdrActive = "HDR Output Active";
            public static readonly string hdrAvailable = "HDR Output Available";
            public static readonly string gamut = "Display Color Gamut";
            public static readonly string format = "Display Buffer Graphics Format";
            public static readonly string autoHdrTonemapping = "Automatic HDR Tonemapping";
            public static readonly string paperWhite = "Paper White Nits";
            public static readonly string minLuminance = "Min Tone Map Luminance";
            public static readonly string maxLuminance = "Max Tone Map Luminance";
            public static readonly string maxFullFrameLuminance = "Max Full Frame Tone Map Luminance";
            public static readonly string modeChangeRequested = "HDR Mode Change Requested";
            public static readonly string notAvailable = "N/A";
        }

        /// <summary>
        /// Creates a table of values from the HDROutputSettings API.
        /// </summary>
        /// <returns>A table containing the values from the HDROutputSettings API.</returns>
        public static DebugUI.Table CreateHDROuputDisplayTable()
        {
            //Create table and rows
            var table = new DebugUI.Table()
            {
                displayName = Strings.hdrOutputAPI,
                isReadOnly = true
            };

            var row_hdrActive = new DebugUI.Table.Row()
            {
                displayName = Strings.hdrActive,
                opened = true
            };

            var row_hdrAvailable = new DebugUI.Table.Row()
            {
                displayName = Strings.hdrAvailable,
                opened = true
            };

            var row_gamut = new DebugUI.Table.Row()
            {
                displayName = Strings.gamut,
                opened = false
            };

            var row_format = new DebugUI.Table.Row()
            {
                displayName = Strings.format,
                opened = false
            };

            var row_autoHdrTonemapping = new DebugUI.Table.Row()
            {
                displayName = Strings.autoHdrTonemapping,
                opened = false
            };

            var row_paperWhite = new DebugUI.Table.Row()
            {
                displayName = Strings.paperWhite,
                opened = false
            };

            var row_minLuminance = new DebugUI.Table.Row()
            {
                displayName = Strings.minLuminance,
                opened = false
            };

            var row_maxLuminance = new DebugUI.Table.Row()
            {
                displayName = Strings.maxLuminance,
                opened = false
            };

            var row_maxFullFrameLuminance = new DebugUI.Table.Row()
            {
                displayName = Strings.maxFullFrameLuminance,
                opened = false
            };

            var row_modeChangeRequested = new DebugUI.Table.Row()
            {
                displayName = Strings.modeChangeRequested,
                opened = false
            };

            //Iterate through all displays
            HDROutputSettings[] displays = HDROutputSettings.displays;
            for(int i=0; i<displays.Length; i++)
            {
                var d = displays[i];

                //Check if main display
                int idName = i + 1;
                var name = Strings.displayName + idName;
                if(HDROutputSettings.main == d)
                {
                    name += Strings.displayMain;
                }

                //Fill rows

                row_hdrActive.children.Add
                (
                    new DebugUI.Value()
                    {   
                        displayName = name,
                        getter = () => 
                        {
                            return d.active;
                        }
                    }
                );

                row_hdrAvailable.children.Add
                (
                    new DebugUI.Value()
                    {   
                        displayName = name,
                        getter = () => 
                        {
                            return d.available;
                        }
                    }
                );

                row_gamut.children.Add
                (
                    new DebugUI.Value()
                    {   
                        displayName = name,
                        getter = () => 
                        {
                            if(d.available)
                            {
                                return d.displayColorGamut;
                            }
                            return Strings.notAvailable;
                        }
                    }
                );

                row_format.children.Add
                (
                    new DebugUI.Value()
                    {   
                        displayName = name,
                        getter = () => 
                        {
                            if(d.available)
                            {
                                return d.graphicsFormat;
                            }
                            return Strings.notAvailable;
                        }
                    }
                );

                row_autoHdrTonemapping.children.Add
                (
                    new DebugUI.Value()
                    {   
                        displayName = name,
                        getter = () => 
                        {
                            if(d.available)
                            {
                                return d.automaticHDRTonemapping;
                            }
                            return Strings.notAvailable;
                        }
                    }
                );

                row_paperWhite.children.Add
                (
                    new DebugUI.Value()
                    {   
                        displayName = name,
                        getter = () => 
                        {
                            if(d.available)
                            {
                                return d.paperWhiteNits;
                            }
                            return Strings.notAvailable;
                        }
                    }
                );

                row_minLuminance.children.Add
                (
                    new DebugUI.Value()
                    {   
                        displayName = name,
                        getter = () => 
                        {
                            if(d.available)
                            {
                                return d.minToneMapLuminance;
                            }
                            return Strings.notAvailable;
                        }
                    }
                );

                row_maxLuminance.children.Add
                (
                    new DebugUI.Value()
                    {   
                        displayName = name,
                        getter = () => 
                        {
                            if(d.available)
                            {
                                return d.maxToneMapLuminance;
                            }
                            return Strings.notAvailable;
                        }
                    }
                );

                row_maxFullFrameLuminance.children.Add
                (
                    new DebugUI.Value()
                    {   
                        displayName = name,
                        getter = () => 
                        {
                            if(d.available)
                            {
                                return d.maxFullFrameToneMapLuminance;
                            }
                            return Strings.notAvailable;
                        }
                    }
                );

                row_modeChangeRequested.children.Add
                (
                    new DebugUI.Value()
                    {   
                        displayName = name,
                        getter = () => 
                        {
                            if(d.available)
                            {
                                return d.HDRModeChangeRequested;
                            }
                            return Strings.notAvailable;
                        }
                    }
                );
            }

            //Add rows to table
            table.children.Add(row_hdrActive);
            table.children.Add(row_hdrAvailable);
            table.children.Add(row_gamut);
            table.children.Add(row_format);
            table.children.Add(row_autoHdrTonemapping);
            table.children.Add(row_paperWhite);
            table.children.Add(row_minLuminance);
            table.children.Add(row_maxLuminance);
            table.children.Add(row_maxFullFrameLuminance);
            table.children.Add(row_modeChangeRequested);

            return table;
        }
    }
}