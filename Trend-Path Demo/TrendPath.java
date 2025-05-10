import java.util.*;

public class TrendPath {

    private static final double BASE_VIRAL = 1.2;
    private static final double MIN_BASE = 0.5;
    private static final double MAX_BASE = 1.5;
    private static final double MIN_VOLATILITY = 0.3;
    private static final double MAX_VOLATILITY = 1.2;
    private static final double MAX_SIGMA = 100.0; // assumed max σ for scaling
    private static final int LONG_TERM_WINDOW = 48;

    public static class Product {
        public String name;
        public List<Integer> demandHistory;
        public int stockUnits;
        public boolean isSpike;

        public Product(String name, List<Integer> demandHistory, int stockUnits) {
            this.name = name;
            this.demandHistory = demandHistory;
            this.stockUnits = stockUnits;
            this.isSpike = false;
        }
    }

    public static class Edge {
        public String from;
        public String to;
        public double cost;

        public Edge(String from, String to, double cost) {
            this.from = from;
            this.to = to;
            this.cost = cost;
        }
    }

    public static class Graph {
        public Map<String, List<Edge>> adjacencyList = new HashMap<>();

        public void addEdge(String from, String to, double cost) {
            adjacencyList.computeIfAbsent(from, k -> new ArrayList<>()).add(new Edge(from, to, cost));
        }

        public List<Edge> getEdges(String from) {
            return adjacencyList.getOrDefault(from, new ArrayList<>());
        }
    }

    public static class Result {
        public boolean willDeplete;
        public int depletionDays;
        public Map<String, Double> adjustedForecasts;
        public Map<String, List<String>> optimizedPaths;

        public Result(boolean willDeplete, int depletionDays, Map<String, Double> adjustedForecasts,
                Map<String, List<String>> optimizedPaths) {
            this.willDeplete = willDeplete;
            this.depletionDays = depletionDays;
            this.adjustedForecasts = adjustedForecasts;
            this.optimizedPaths = optimizedPaths;
        }
    }

    public static Result runTrendPath(List<Product> products, Graph productGraph) {
        Map<String, Double> adjustedForecasts = new HashMap<>();
        Map<String, List<String>> optimizedPaths = new HashMap<>();
        boolean depletionWarning = false;
        int depletionDays = Integer.MAX_VALUE; // Initialize with a large number

        for (Product product : products) {
            // Step 1–2: Basic stats
            double shortTermAvg = average(
                    product.demandHistory.subList(product.demandHistory.size() - 3, product.demandHistory.size()));
            double prevAvg = average(
                    product.demandHistory.subList(product.demandHistory.size() - 8, product.demandHistory.size() - 3));
            double sigma = stdDev(
                    product.demandHistory.subList(product.demandHistory.size() - 8, product.demandHistory.size()));
            double longTermSigma = stdDev(product.demandHistory.subList(
                    Math.max(0, product.demandHistory.size() - LONG_TERM_WINDOW), product.demandHistory.size()));

            // Step 3: Virality detection
            double viralThreshold = BASE_VIRAL + (longTermSigma > 0 ? (sigma / longTermSigma) : 0);
            if (prevAvg > 0 && (shortTermAvg / prevAvg) > viralThreshold) {
                shortTermAvg = average(
                        product.demandHistory.subList(product.demandHistory.size() - 1, product.demandHistory.size()));
                prevAvg = average(product.demandHistory.subList(product.demandHistory.size() - 3,
                        product.demandHistory.size() - 1));
                sigma = stdDev(
                        product.demandHistory.subList(product.demandHistory.size() - 3, product.demandHistory.size()));
            }

            // Step 4: Dynamic thresholds
            double baseSensitivity = MIN_BASE + (MAX_BASE - MIN_BASE) * (sigma / MAX_SIGMA);
            double volatilityFactor = MIN_VOLATILITY + (MAX_VOLATILITY - MIN_VOLATILITY) * (sigma / MAX_SIGMA);
            double k = baseSensitivity + (volatilityFactor * sigma);
            double dynamicThreshold = prevAvg + (k * sigma);

            // Step 5: Spike detection
            if (shortTermAvg > dynamicThreshold) {
                product.isSpike = true;
            }

            // Step 6: Adjusted paths (Bellman-Ford)
            Map<String, Double> distances = new HashMap<>();
            Map<String, String> predecessors = new HashMap<>();

            // Ensure every product is in the distances map
            for (Product p : products) {
                distances.put(p.name, Double.POSITIVE_INFINITY);
            }
            distances.put(product.name, 0.0); // Set the current product distance to 0

            for (int i = 0; i < products.size() - 1; i++) {
                for (Edge edge : getAllEdges(productGraph)) {
                    double penalty = 0.0;
                    if (isProductSpike(products, edge.to))
                        penalty = 10.0;
                    // Ensure the "from" node exists in the distances map
                    if (distances.get(edge.from) != null) {
                        double newCost = distances.get(edge.from) + edge.cost + penalty;
                        if (newCost < distances.get(edge.to)) {
                            distances.put(edge.to, newCost);
                            predecessors.put(edge.to, edge.from);
                        }
                    }
                }
            }

            // Step 7: Adjust forecast based on spike exposure
            int spikeCount = 0;
            String node = product.name;
            while (predecessors.containsKey(node)) {
                if (isProductSpike(products, node))
                    spikeCount++;
                node = predecessors.get(node);
            }
            double adjustedForecast = shortTermAvg * (1 + 0.1 * spikeCount);
            adjustedForecasts.put(product.name, adjustedForecast);

            // Step 8: Depletion estimation
            int estimatedDays = (int) (product.stockUnits / adjustedForecast);
            if (estimatedDays < 2)
                depletionWarning = true;

            depletionDays = Math.min(depletionDays, estimatedDays);

            optimizedPaths.put(product.name, buildPath(predecessors, product.name));
        }

        return new Result(depletionWarning, depletionDays, adjustedForecasts, optimizedPaths);
    }

    private static double average(List<Integer> data) {
        return data.stream().mapToInt(i -> i).average().orElse(0.0);
    }

    private static double stdDev(List<Integer> data) {
        double mean = average(data);
        double variance = data.stream().mapToDouble(i -> Math.pow(i - mean, 2)).average().orElse(0.0);
        return Math.sqrt(variance);
    }

    private static boolean isProductSpike(List<Product> products, String name) {
        return products.stream().anyMatch(p -> p.name.equals(name) && p.isSpike);
    }

    private static List<Edge> getAllEdges(Graph graph) {
        List<Edge> all = new ArrayList<>();
        for (List<Edge> edges : graph.adjacencyList.values())
            all.addAll(edges);
        return all;
    }

    private static List<String> buildPath(Map<String, String> predecessors, String target) {
        LinkedList<String> path = new LinkedList<>();
        while (target != null) {
            path.addFirst(target);
            target = predecessors.get(target);
        }
        return path;
    }
}
