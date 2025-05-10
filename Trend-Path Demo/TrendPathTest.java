
//*---- MAIN TEST--- */
/*import java.util.*;

public class TrendPathTest {
    public static void main(String[] args) {
        // Simulate fast food chain scenario

        // Demand history (hourly data): burger, fries, soda
        List<TrendPath.Product> products = Arrays.asList(
                new TrendPath.Product("Burger", Arrays.asList(10, 12, 11, 13, 50, 55, 60, 65, 70, 75), 100),
                new TrendPath.Product("Fries", Arrays.asList(8, 9, 7, 10, 45, 48, 50, 52, 55, 58), 80),
                new TrendPath.Product("Soda", Arrays.asList(5, 5, 6, 5, 30, 32, 33, 35, 36, 38), 60));

        // Create product graph: central kitchen supplies all items
        TrendPath.Graph productGraph = new TrendPath.Graph();
        productGraph.addEdge("Kitchen", "Burger", 5.0);
        productGraph.addEdge("Kitchen", "Fries", 4.0);
        productGraph.addEdge("Kitchen", "Soda", 3.0);

        // Optional cross-product delivery dependencies
        productGraph.addEdge("Burger", "Fries", 2.0);
        productGraph.addEdge("Fries", "Soda", 1.5);

        // Run TrendPath algorithm
        TrendPath.Result result = TrendPath.runTrendPath(products, productGraph);

        // Print results
        System.out.println("Depletion Warning: " + result.willDeplete);

        System.out.println("\nAdjusted Forecasts:");
        for (Map.Entry<String, Double> entry : result.adjustedForecasts.entrySet()) {
            System.out.printf("%s: %.2f units/day\n", entry.getKey(), entry.getValue());
        }

        System.out.println("\nOptimized Paths:");
        for (Map.Entry<String, List<String>> entry : result.optimizedPaths.entrySet()) {
            System.out.println(entry.getKey() + ": " + String.join(" -> ", entry.getValue()));
        }
    }
}*/

//-- END OF TEST --- */

//-- This test simulates a fast food chain scenario where the demand for burgers, fries, and soda is tracked.
//-- The product graph represents the supply chain from a central kitchen to various products.                      
//-- The TrendPath algorithm is run to determine if any product will deplete and to optimize the supply paths.
//-- The results are printed, showing whether there is a depletion warning, the adjusted forecasts for each product,
//-- and the optimized paths for product supply. The test uses a simple demand history and a basic product graph.
//-- The test is designed to validate the functionality of the TrendPath algorithm in a real-world scenario.                

//-- TEST CASE:1 

import java.util.*;

public class TrendPathTest {
    public static void main(String[] args) {
        // Test Case 1: Mild Spike, No Viral Escalation

        // Demand history (hourly data): product with a mild spike in the last few hours
        List<TrendPath.Product> products = Arrays.asList(
                new TrendPath.Product("Burger", Arrays.asList(10, 12, 11, 13, 50, 55, 60, 65, 70, 75), 100),
                new TrendPath.Product("Fries", Arrays.asList(8, 9, 7, 10, 45, 48, 50, 52, 55, 58), 80),
                new TrendPath.Product("Soda", Arrays.asList(5, 5, 6, 5, 30, 32, 33, 35, 36, 38), 60));

        // Create product graph: central kitchen supplies all items
        TrendPath.Graph productGraph = new TrendPath.Graph();
        productGraph.addEdge("Kitchen", "Burger", 5.0);
        productGraph.addEdge("Kitchen", "Fries", 4.0);
        productGraph.addEdge("Kitchen", "Soda", 3.0);

        // Optional cross-product delivery dependencies
        productGraph.addEdge("Burger", "Fries", 2.0);
        productGraph.addEdge("Fries", "Soda", 1.5);

        // Run TrendPath algorithm
        TrendPath.Result result = TrendPath.runTrendPath(products, productGraph);

        // Print results
        System.out.println("Depletion Warning: " + result.willDeplete);
        System.out.println("Depletion Days: " + result.depletionDays);

        System.out.println("\nAdjusted Forecasts:");
        for (Map.Entry<String, Double> entry : result.adjustedForecasts.entrySet()) {
            System.out.printf("%s: %.2f units/day\n", entry.getKey(), entry.getValue());
        }

        System.out.println("\nOptimized Paths:");
        for (Map.Entry<String, List<String>> entry : result.optimizedPaths.entrySet()) {
            System.out.println(entry.getKey() + ": " + String.join(" -> ", entry.getValue()));
        }
    }
}

// -- TEST CASE:2
/*
 * package Demo;
 * 
 * import java.util.*;
 * 
 * public class TrendPathTest {
 * public static void main(String[] args) {
 * // Test Case 2: Viral Surge Triggers Escalation
 * 
 * // Demand history (hourly data): product with a significant viral surge
 * List<TrendPath.Product> products = Arrays.asList(
 * new TrendPath.Product("Burger", Arrays.asList(10, 12, 11, 13, 50, 55, 60,
 * 200, 220, 250), 100),
 * new TrendPath.Product("Fries", Arrays.asList(8, 9, 7, 10, 45, 48, 50, 52, 55,
 * 58), 80),
 * new TrendPath.Product("Soda", Arrays.asList(5, 5, 6, 5, 30, 32, 33, 35, 36,
 * 38), 60));
 * 
 * // Create product graph: central kitchen supplies all items
 * TrendPath.Graph productGraph = new TrendPath.Graph();
 * productGraph.addEdge("Kitchen", "Burger", 5.0);
 * productGraph.addEdge("Kitchen", "Fries", 4.0);
 * productGraph.addEdge("Kitchen", "Soda", 3.0);
 * 
 * // Optional cross-product delivery dependencies
 * productGraph.addEdge("Burger", "Fries", 2.0);
 * productGraph.addEdge("Fries", "Soda", 1.5);
 * 
 * // Run TrendPath algorithm
 * TrendPath.Result result = TrendPath.runTrendPath(products, productGraph);
 * 
 * // Print results
 * System.out.println("Depletion Warning: " + result.willDeplete);
 * System.out.println("Depletion Days: " + result.depletionDays);
 * 
 * System.out.println("\nAdjusted Forecasts:");
 * for (Map.Entry<String, Double> entry : result.adjustedForecasts.entrySet()) {
 * System.out.printf("%s: %.2f units/day\n", entry.getKey(), entry.getValue());
 * }
 * 
 * System.out.println("\nOptimized Paths:");
 * for (Map.Entry<String, List<String>> entry :
 * result.optimizedPaths.entrySet()) {
 * System.out.println(entry.getKey() + ": " + String.join(" -> ",
 * entry.getValue()));
 * }
 * }
 * }
 */

// -- TEST CASE:3

/*
 * package Demo;
 * 
 * import java.util.*;
 * 
 * public class TrendPathTest {
 * public static void main(String[] args) {
 * // Test Case 3: Flash Deal-Induced Spike
 * 
 * // Demand history (hourly data): product with a flash deal spike
 * List<TrendPath.Product> products = Arrays.asList(
 * new TrendPath.Product("Burger", Arrays.asList(10, 12, 11, 13, 50, 100, 150,
 * 300, 250, 100), 100),
 * new TrendPath.Product("Fries", Arrays.asList(8, 9, 7, 10, 45, 48, 50, 55, 60,
 * 65), 80),
 * new TrendPath.Product("Soda", Arrays.asList(5, 5, 6, 5, 30, 32, 33, 35, 36,
 * 38), 60));
 * 
 * // Create product graph: central kitchen supplies all items
 * TrendPath.Graph productGraph = new TrendPath.Graph();
 * productGraph.addEdge("Kitchen", "Burger", 5.0);
 * productGraph.addEdge("Kitchen", "Fries", 4.0);
 * productGraph.addEdge("Kitchen", "Soda", 3.0);
 * 
 * // Optional cross-product delivery dependencies
 * productGraph.addEdge("Burger", "Fries", 2.0);
 * productGraph.addEdge("Fries", "Soda", 1.5);
 * 
 * // Run TrendPath algorithm
 * TrendPath.Result result = TrendPath.runTrendPath(products, productGraph);
 * 
 * // Print results
 * System.out.println("Depletion Warning: " + result.willDeplete);
 * System.out.println("Depletion Days: " + result.depletionDays);
 * 
 * System.out.println("\nAdjusted Forecasts:");
 * for (Map.Entry<String, Double> entry : result.adjustedForecasts.entrySet()) {
 * System.out.printf("%s: %.2f units/day\n", entry.getKey(), entry.getValue());
 * }
 * 
 * System.out.println("\nOptimized Paths:");
 * for (Map.Entry<String, List<String>> entry :
 * result.optimizedPaths.entrySet()) {
 * System.out.println(entry.getKey() + ": " + String.join(" -> ",
 * entry.getValue()));
 * }
 * }
 * }
 */
