using System;
using System.Collections.Generic;
using System.Linq;

namespace TrendPathAlgorithm
{
    // Represents a product in the inventory
    public class Product
    {
        public string Name { get; set; }
        public double CurrentStock { get; set; }
        public List<double> DemandHistory { get; set; } = new List<double>();
        public bool IsSpike { get; set; }
        public int DaysToDepletion { get; set; }

        public Product(string name, double stock, List<double> history)
        {
            Name = name;
            CurrentStock = stock;
            DemandHistory = history;
        }
    }

    // Represents a supply path between nodes (products/suppliers)
    public class SupplyPath
    {
        public string Source { get; set; }
        public string Destination { get; set; }
        public double BaseCost { get; set; }
        public double AdjustedCost { get; set; }

        public SupplyPath(string source, string destination, double cost)
        {
            Source = source;
            Destination = destination;
            BaseCost = cost;
            AdjustedCost = cost;
        }
    }

    // Main algorithm implementation
    public class TrendPath
    {
        private Dictionary<string, Product> _products = new Dictionary<string, Product>();
        private List<SupplyPath> _supplyPaths = new List<SupplyPath>();
        private Dictionary<string, List<string>> _productGraph = new Dictionary<string, List<string>>();
        private Dictionary<string, Dictionary<string, double>> _shortestPaths = new Dictionary<string, Dictionary<string, double>>();
        private Dictionary<string, Dictionary<string, string>> _predecessors = new Dictionary<string, Dictionary<string, string>>();

        // Constants for threshold calculations
        private const double MinBase = 0.5;
        private const double MaxBase = 2.0;
        private const double MinVolatility = 0.1;
        private const double MaxVolatility = 1.5;
        private const double BaseViral = 2.0;
        private const double SpikePenalty = 5.0;

        public TrendPath(Dictionary<string, Product> products, List<SupplyPath> paths)
        {
            _products = products;
            _supplyPaths = paths;
            
            // Build the product graph from supply paths
            BuildProductGraph();
        }

        private void BuildProductGraph()
        {
            foreach (var path in _supplyPaths)
            {
                if (!_productGraph.ContainsKey(path.Source))
                {
                    _productGraph[path.Source] = new List<string>();
                }
                _productGraph[path.Source].Add(path.Destination);
            }
        }

        // Main method to run the Trend-Path algorithm
        public void RunTrendPathAlgorithm()
        {
            // Step 1 & 2: Analyze demand and calculate statistics
            AnalyzeDemandStatistics();
            
            // Step 3 & 4: Detect virality and calculate dynamic thresholds
            DetectViralityAndSpikes();
            
            // Step 5 & 6: Adjust path costs and find optimal paths
            AdjustPathCostsAndFindOptimalPaths();
            
            // Step 7 & 8: Adjust forecasts and estimate depletion
            AdjustForecastsAndEstimateDepletion();
        }

        private void AnalyzeDemandStatistics()
        {
            foreach (var product in _products.Values)
            {
                if (product.DemandHistory.Count < 8) // Need at least 8 hours of data
                {
                    Console.WriteLine($"Warning: Not enough demand history for {product.Name}");
                    continue;
                }
                
                // Get the relevant windows from the demand history
                // Most recent 3 hours (initial window)
                var initialWindow = product.DemandHistory.Skip(product.DemandHistory.Count - 3).ToList();
                
                // Previous 5 hours before initial window
                var previousWindow = product.DemandHistory.Skip(product.DemandHistory.Count - 8).Take(5).ToList();
                
                // Long term window (48 hours if available, otherwise as much as we have)
                var longTermWindow = product.DemandHistory.Count >= 48 
                    ? product.DemandHistory.Skip(product.DemandHistory.Count - 48).ToList()
                    : product.DemandHistory.ToList();
                
                // Calculate statistics
                double shortTermAvg = initialWindow.Average();
                double prevAvg = previousWindow.Average();
                
                // Combined window for standard deviation
                var combinedWindow = initialWindow.Concat(previousWindow).ToList();
                double combinedAverage = combinedWindow.Average();
                double sigma = Math.Sqrt(combinedWindow.Sum(d => Math.Pow(d - combinedAverage, 2)) / combinedWindow.Count);
                
                // Long term volatility
                double longTermAvg = longTermWindow.Average();
                double longTermSigma = Math.Sqrt(longTermWindow.Sum(d => Math.Pow(d - longTermAvg, 2)) / longTermWindow.Count);
                
                // Store these statistics in the product for later use
                product.IsSpike = false; // Reset spike flag
                
                // Step 3: Detect virality signal
                double dynamicViralThreshold = BaseViral + (1.5 * (sigma / Math.Max(0.001, longTermSigma)));
                
                if ((shortTermAvg / Math.Max(0.001, prevAvg)) > dynamicViralThreshold)
                {
                    // Recalculate with shorter windows for viral products
                    initialWindow = product.DemandHistory.Skip(product.DemandHistory.Count - 1).ToList(); // Last hour
                    previousWindow = product.DemandHistory.Skip(product.DemandHistory.Count - 3).Take(2).ToList(); // 2 hours before
                    
                    shortTermAvg = initialWindow.Average();
                    prevAvg = previousWindow.Average();
                    
                    combinedWindow = initialWindow.Concat(previousWindow).ToList();
                    combinedAverage = combinedWindow.Average();
                    sigma = Math.Sqrt(combinedWindow.Sum(d => Math.Pow(d - combinedAverage, 2)) / combinedWindow.Count);
                }
                
                // Step 4: Calculate dynamic thresholds
                double maxSigma = Math.Max(sigma, longTermSigma); // To normalize the standard deviation
                double baseSensitivityFactor = MinBase + (MaxBase - MinBase) * (sigma / Math.Max(0.001, maxSigma));
                double volatilityFactor = MinVolatility + (MaxVolatility - MinVolatility) * (sigma / Math.Max(0.001, maxSigma));
                
                double k = baseSensitivityFactor + (volatilityFactor * sigma);
                double dynamicThreshold = prevAvg + (k * sigma);
                
                // Step 5: Detect spikes
                if (shortTermAvg > dynamicThreshold)
                {
                    product.IsSpike = true;
                    Console.WriteLine($"Spike detected for {product.Name}! Short-term avg: {shortTermAvg:F2}, Threshold: {dynamicThreshold:F2}");
                }
            }
        }

        private void DetectViralityAndSpikes()
        {
            // This functionality is already covered in AnalyzeDemandStatistics method
        }

        private void AdjustPathCostsAndFindOptimalPaths()
        {
            // Adjust path costs based on spike detection
            foreach (var path in _supplyPaths)
            {
                // Reset the adjusted cost to base cost
                path.AdjustedCost = path.BaseCost;
                
                // Add spike penalty if destination product is marked as spike
                if (_products.ContainsKey(path.Destination) && _products[path.Destination].IsSpike)
                {
                    path.AdjustedCost = path.BaseCost + SpikePenalty;
                }
            }
            
            // Run Bellman-Ford to find shortest paths
            BellmanFordPathfinding();
        }

        private void BellmanFordPathfinding()
        {
            // Initialize dictionaries
            _shortestPaths.Clear();
            _predecessors.Clear();
            
            // Find unique nodes (products and suppliers)
            HashSet<string> allNodes = new HashSet<string>();
            foreach (var path in _supplyPaths)
            {
                allNodes.Add(path.Source);
                allNodes.Add(path.Destination);
            }
            
            // Use each node as a source and find shortest paths to all other nodes
            foreach (var source in allNodes)
            {
                // Initialize distances and predecessors
                Dictionary<string, double> distances = new Dictionary<string, double>();
                Dictionary<string, string> predecessorsFromSource = new Dictionary<string, string>();
                
                foreach (var node in allNodes)
                {
                    distances[node] = double.MaxValue;
                    predecessorsFromSource[node] = null;
                }
                
                distances[source] = 0;
                
                // Relaxation step (|V|-1 times)
                for (int i = 0; i < allNodes.Count - 1; i++)
                {
                    foreach (var path in _supplyPaths)
                    {
                        if (distances[path.Source] != double.MaxValue &&
                            distances[path.Source] + path.AdjustedCost < distances[path.Destination])
                        {
                            distances[path.Destination] = distances[path.Source] + path.AdjustedCost;
                            predecessorsFromSource[path.Destination] = path.Source;
                        }
                    }
                }
                
                // Store results
                _shortestPaths[source] = distances;
                _predecessors[source] = predecessorsFromSource;
            }
        }

        private void AdjustForecastsAndEstimateDepletion()
        {
            string mainSupplier = FindMainSupplier();
            
            foreach (var product in _products.Values)
            {
                // Calculate baseline forecast (average of the most recent data)
                int forecastWindow = Math.Min(product.DemandHistory.Count, 24); // Use last 24 data points or all if less
                double baselineForecast = product.DemandHistory.Skip(product.DemandHistory.Count - forecastWindow).Average();
                
                // Adjust forecast based on spike exposure in the supply path
                double adjustedForecast = baselineForecast;
                
                // If there's a path from the main supplier to this product
                if (mainSupplier != null && _shortestPaths.ContainsKey(mainSupplier) &&
                    _shortestPaths[mainSupplier].ContainsKey(product.Name))
                {
                    // Count spike nodes in the path
                    int spikeNodesInPath = CountSpikeNodesInPath(mainSupplier, product.Name);
                    
                    // Increase forecast based on spike nodes
                    double spikeFactor = 1.0 + (0.2 * spikeNodesInPath); // 20% increase per spike node
                    adjustedForecast = baselineForecast * spikeFactor;
                }
                
                // Estimate days to depletion (assuming constant demand)
                product.DaysToDepletion = adjustedForecast > 0
                    ? (int)Math.Floor(product.CurrentStock / adjustedForecast)
                    : int.MaxValue;
                
                Console.WriteLine($"{product.Name}: Baseline forecast: {baselineForecast:F2} units/hour, " +
                                 $"Adjusted forecast: {adjustedForecast:F2} units/hour, " +
                                 $"Days to depletion: {product.DaysToDepletion}");
            }
        }

        private string FindMainSupplier()
        {
            // Find the node that has outgoing paths but no incoming paths (likely a supplier/warehouse)
            HashSet<string> nodesWithOutgoing = new HashSet<string>();
            HashSet<string> nodesWithIncoming = new HashSet<string>();
            
            foreach (var path in _supplyPaths)
            {
                nodesWithOutgoing.Add(path.Source);
                nodesWithIncoming.Add(path.Destination);
            }
            
            // Find nodes that have outgoing but no incoming edges
            var possibleSuppliers = nodesWithOutgoing.Except(nodesWithIncoming).ToList();
            
            return possibleSuppliers.FirstOrDefault();
        }

        private int CountSpikeNodesInPath(string source, string destination)
        {
            int spikeCount = 0;
            string current = destination;
            
            while (current != source && _predecessors[source].ContainsKey(current) && _predecessors[source][current] != null)
            {
                // Check if current node is a product with a spike
                if (_products.ContainsKey(current) && _products[current].IsSpike)
                {
                    spikeCount++;
                }
                
                // Move to predecessor
                current = _predecessors[source][current];
            }
            
            return spikeCount;
        }
        
        // Get the results for all products
        public List<(string ProductName, bool DepletionWarning, int DaysToDepletion, string OptimalPath)> GetResults()
        {
            List<(string, bool, int, string)> results = new List<(string, bool, int, string)>();
            string mainSupplier = FindMainSupplier();
            
            foreach (var product in _products.Values)
            {
                // Depletion warning if product will deplete within 2 days
                bool depletionWarning = product.DaysToDepletion <= 2;
                
                // Find optimal path from supplier to product
                string optimalPath = "No path found";
                if (mainSupplier != null)
                {
                    optimalPath = FindOptimalPath(mainSupplier, product.Name);
                }
                
                results.Add((product.Name, depletionWarning, product.DaysToDepletion, optimalPath));
            }
            
            return results;
        }
        
        private string FindOptimalPath(string source, string destination)
        {
            if (!_predecessors.ContainsKey(source) || !_predecessors[source].ContainsKey(destination))
            {
                return "No path found";
            }
            
            List<string> path = new List<string>();
            string current = destination;
            
            // Reconstruct path from destination to source
            while (current != source && _predecessors[source].ContainsKey(current) && _predecessors[source][current] != null)
            {
                path.Add(current);
                current = _predecessors[source][current];
            }
            
            if (current == source)
            {
                path.Add(source);
                path.Reverse(); // Get path from source to destination
                return string.Join(" -> ", path);
            }
            
            return "No complete path found";
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Fast Food Chain - Trend-Path Algorithm Demo");
            Console.WriteLine("-------------------------------------------\n");
            
            // Create products with sample data
            Dictionary<string, Product> products = new Dictionary<string, Product>
            {
                // Format: Product name, Current stock, Demand history (hourly demand for past hours)
                { "Burger", new Product("Burger", 500, GenerateDemandData(100, 20, 72, spikeAt: 68)) },
                { "Fries", new Product("Fries", 300, GenerateDemandData(150, 30, 72, spikeAt: 70)) },
                { "Soda", new Product("Soda", 800, GenerateDemandData(80, 15, 72)) },
                { "Chicken", new Product("Chicken", 200, GenerateDemandData(50, 10, 72, spikeAt: 65)) },
                { "Salad", new Product("Salad", 150, GenerateDemandData(30, 8, 72)) }
            };
            
            // Create supply paths
            List<SupplyPath> supplyPaths = new List<SupplyPath>
            {
                // Format: Source, Destination, Base Cost
                new SupplyPath("Warehouse", "Beef", 2.0),
                new SupplyPath("Warehouse", "Potatoes", 1.5),
                new SupplyPath("Warehouse", "ChickenSupply", 3.0),
                new SupplyPath("Warehouse", "Vegetables", 2.0),
                new SupplyPath("Warehouse", "SodaSyrup", 1.0),
                new SupplyPath("Beef", "Burger", 1.0),
                new SupplyPath("Potatoes", "Fries", 0.5),
                new SupplyPath("ChickenSupply", "Chicken", 1.0),
                new SupplyPath("SodaSyrup", "Soda", 0.5),
                new SupplyPath("Vegetables", "Salad", 1.0),
                new SupplyPath("Vegetables", "Burger", 0.2),  // Vegetables also used in burgers
                new SupplyPath("Vegetables", "Chicken", 0.2), // Vegetables also used in chicken sandwiches
            };
            
            // Initialize and run the Trend-Path algorithm
            TrendPath trendPath = new TrendPath(products, supplyPaths);
            trendPath.RunTrendPathAlgorithm();
            
            // Get and display results
            Console.WriteLine("\nTrend-Path Algorithm Results:");
            Console.WriteLine("-----------------------------");
            var results = trendPath.GetResults();
            
            foreach (var (productName, depletionWarning, daysToDepletion, optimalPath) in results)
            {
                Console.WriteLine($"\nProduct: {productName}");
                Console.WriteLine($"Depletion Warning: {(depletionWarning ? "YES - RESTOCK SOON!" : "No")}");
                Console.WriteLine($"Days to Depletion: {daysToDepletion}");
                Console.WriteLine($"Optimal Supply Path: {optimalPath}");
            }
        }
        
        // Helper method to generate realistic demand data with optional spike
        private static List<double> GenerateDemandData(double baseDemand, double variation, int hours, int? spikeAt = null)
        {
            List<double> data = new List<double>();
            Random random = new Random();
            
            for (int i = 0; i < hours; i++)
            {
                double hourlyFactor = 1.0;
                
                // Model time-of-day effects (busier during lunch and dinner)
                int hourOfDay = i % 24;
                if (hourOfDay >= 11 && hourOfDay <= 13) // Lunch rush
                {
                    hourlyFactor = 1.5;
                }
                else if (hourOfDay >= 17 && hourOfDay <= 19) // Dinner rush
                {
                    hourlyFactor = 1.8;
                }
                else if (hourOfDay >= 22 || hourOfDay <= 5) // Late night/early morning
                {
                    hourlyFactor = 0.4;
                }
                
                // Weekend effect - busier on weekends
                int dayOfWeek = (i / 24) % 7;
                if (dayOfWeek >= 5) // Saturday and Sunday
                {
                    hourlyFactor *= 1.3;
                }
                
                // Add spike if specified
                if (spikeAt.HasValue && i == spikeAt.Value)
                {
                    hourlyFactor *= 2.5; // Major spike
                }
                
                // Calculate demand with some randomness
                double demand = baseDemand * hourlyFactor + random.NextDouble() * variation * 2 - variation;
                data.Add(Math.Max(0, demand)); // Ensure no negative demand
            }
            
            return data;
        }
    }
}
