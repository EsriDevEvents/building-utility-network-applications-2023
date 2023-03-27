using Esri.ArcGISRuntime;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Security;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using Esri.ArcGISRuntime.UtilityNetworks;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;

namespace TraceToolWpf
{
    public partial class MainWindow : Window
    {
        private const string WebmapURL = "https://www.arcgis.com/home/item.html?id=471eb0bf37074b1fbb972b1da70fb310";
        private const string PortalURL = "https://sampleserver7.arcgisonline.com/portal/sharing/rest";
        private const string Username = "viewer01";
        private const string Password = "I68VGU^nMurF";

        private string? DeviceLayerName;

        public MainWindow()
        {
            InitializeComponent();

            UtilityNetworkTraceTool.UtilityNetworkChanged += OnUtilityNetworkChanged;
            UtilityNetworkTraceTool.UtilityNetworkTraceCompleted += OnTraceCompleted;
            Initialize();
        }

        private async void Initialize()
        {
            try
            {
                if (MyMapView.Map is null)
                {
                    if (!AuthenticationManager.Current.Credentials.Any())
                    {
                        var portalCredential = await AuthenticationManager.Current.GenerateCredentialAsync(
                            new Uri(PortalURL),
                            Username,
                            Password);
                        AuthenticationManager.Current.AddCredential(portalCredential);
                    }

                    MyMapView.Map = new Map(new Uri(WebmapURL));

                }
                else
                {
                    MyMapView.Map = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Initializing sample failed: {ex.Message}", ex.GetType().Name);
            }
        }

        private async void OnUtilityNetworkChanged(object? sender, UtilityNetworkChangedEventArgs e)
        {
            if (e.UtilityNetwork is null)
                return;

            if (e.UtilityNetwork.LoadStatus != LoadStatus.Loaded)
                await e.UtilityNetwork.LoadAsync();

            if (e.UtilityNetwork.Definition?.NetworkSources.FirstOrDefault(
                ns => ns.SourceUsageType == UtilityNetworkSourceUsageType.Device)
                is not UtilityNetworkSource networkSource)
                return;

            DeviceLayerName = networkSource.Name;
        }

        private void OnTraceCompleted(object? sender, UtilityNetworkTraceCompletedEventArgs e)
        {
            Debug.WriteLine($"\nPARAMETERS:\n\tTraceType: {e.Parameters.TraceType}");

            if (e.Error is not null)
            {
                Debug.WriteLine($"\nERROR:\n\t{e.Error}");
            }
            else if (e.Results is not null)
            {
                var summary = new StringBuilder();
                foreach (var result in e.Results)
                {
                    if (result.Warnings.Count > 0)
                    {
                        summary.AppendLine($"\nWARNINGS:\n\t{string.Join("\n", result.Warnings)}");
                    }
                    if (result is UtilityElementTraceResult elementResult)
                    {
                        summary.AppendLine($"\nELEMENT RESULT:\n\t{elementResult.Elements.Count} element(s) found.");
                    }
                    else if (result is UtilityFunctionTraceResult functionResult)
                    {
                        summary.AppendLine($"\nFUNCTION RESULT:\n\t{functionResult.FunctionOutputs.Count} functions(s) reported.");
                    }
                    else if (result is UtilityGeometryTraceResult geometryResult)
                    {
                        summary.AppendLine($"\nGEOMETRY RESULT: " +
                            $"\n\t{geometryResult.Multipoint?.Points.Count ?? 0} multipoint(s) found." +
                            $"\n\t{geometryResult.Polyline?.Parts?.Count ?? 0} polyline(s) found." +
                            $"\n\t{geometryResult.Polygon?.Parts?.Count ?? 0} polygon(s) found.");
                    }
                }
                if (summary.Length > 0)
                    Debug.WriteLine(summary.ToString());
            }
        }

        private FeatureLayer? GetDeviceLayer(LayerCollection layers)
        {
            foreach (var layer in layers)
            {
                if (layer is FeatureLayer featureLayer)
                {
                    if (layer.Name == DeviceLayerName)
                        return featureLayer;
                    continue;
                }
                else if (layer is GroupLayer groupLayer)
                {
                    return GetDeviceLayer(groupLayer.Layers);
                }
            }
            return null;
        }

        private async void OnAddStartingPoints(object sender, RoutedEventArgs e)
        {
            if (MyMapView?.Map is null || Query?.Text?.Trim() is not string query)
                return;

            var layer = GetDeviceLayer(MyMapView.Map.OperationalLayers);
            if (layer?.FeatureTable is not ArcGISFeatureTable table)
                return;

            try
            {
                var features = await table.QueryFeaturesAsync(new QueryParameters()
                {
                    WhereClause = query
                });

                foreach (ArcGISFeature f in features)
                {
                    if (f.LoadStatus != LoadStatus.Loaded)
                        await f.LoadAsync();

                    UtilityNetworkTraceTool.AddStartingPoint(f, f.Geometry as MapPoint);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, $"Add Starting Points failed: {ex.GetType().Name}");
            }
        }
    }
}
