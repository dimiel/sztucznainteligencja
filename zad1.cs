using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace TSP_FullSolver
{
    class Program
    {
        static Random rng = new Random();

        static void Main(string[] args)
        {
            string filePath = "odleglosci.txt";

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Blad: Nie znaleziono pliku {filePath} w folderze bin.");
                return;
            }

            // 1. Wczytanie danych
            var (cityNames, matrix) = ParseDistanceFile(filePath);
            if (cityNames.Count == 0) return;

            // 2. Szukanie Bielska-Białej
            int startIdx = cityNames.FindIndex(c => c.ToLower().Contains("bielsko"));
            if (startIdx == -1) startIdx = 0;

            // 3. Obliczanie tras
            // A. Najblizszy sasiad
            var routeNN = GetNearestNeighbor(matrix, startIdx);

            // B. Krzyzowanie AEX (NN + Losowa trasa jako rodzice)
            var randomRoute = Enumerable.Range(0, cityNames.Count).OrderBy(x => rng.Next()).ToList();
            var routeAEX = CrossoverAEX(routeNN, randomRoute, matrix);

            // C. Krzyzowanie HGREX
            var routeHGREX = CrossoverHGREX(routeNN, randomRoute, matrix);

            // D. Optymalizacja 2-opt (na bazie HGREX)
            var routeOptimized = Optimize2Opt(new List<int>(routeHGREX), matrix);

            // 4. Wyswietlanie wynikow
            DisplayRoute("1. METODA NAJBLIZSZEGO SASIADA", routeNN, cityNames, matrix);
            DisplayRoute("2. KRZYZOWANIE AEX", routeAEX, cityNames, matrix);
            DisplayRoute("3. KRZYZOWANIE HGREX", routeHGREX, cityNames, matrix);
            DisplayRoute("4. NAJLEPSZA TRASA (HGREX + 2-OPT)", routeOptimized, cityNames, matrix);

            // 5. Podsumowanie
            Console.WriteLine("======================================================");
            Console.WriteLine("PODSUMOWANIE DYSTANSOW:");
            Console.WriteLine($"Najblizszy Sasiad:  {CalcDist(routeNN, matrix):F2} km");
            Console.WriteLine($"AEX Crossover:      {CalcDist(routeAEX, matrix):F2} km");
            Console.WriteLine($"HGREX Crossover:    {CalcDist(routeHGREX, matrix):F2} km");
            Console.WriteLine($"Zoptymalizowana:    {CalcDist(routeOptimized, matrix):F2} km");
            Console.WriteLine("======================================================");

            Console.WriteLine("\nNacisnij dowolny klawisz, aby zakonczyc...");
            Console.ReadKey();
        }

        // --- ALGORYTMY BUDOWANIA TRAS ---

        static List<int> GetNearestNeighbor(double[][] matrix, int start)
        {
            int n = matrix.Length;
            var route = new List<int> { start };
            var visited = new bool[n];
            visited[start] = true;
            int curr = start;

            for (int i = 0; i < n - 1; i++)
            {
                int next = -1; double min = double.MaxValue;
                for (int j = 0; j < n; j++)
                    if (!visited[j] && matrix[curr][j] < min && matrix[curr][j] > 0) { min = matrix[curr][j]; next = j; }
                if (next != -1) { visited[next] = true; route.Add(next); curr = next; }
            }
            return route;
        }

        static List<int> CrossoverAEX(List<int> p1, List<int> p2, double[][] matrix)
        {
            int n = p1.Count;
            List<int> child = new List<int>();
            HashSet<int> visited = new HashSet<int>();
            int current = p1[0];
            child.Add(current);
            visited.Add(current);

            for (int i = 0; i < n - 1; i++)
            {
                List<int> activeParent = (i % 2 == 0) ? p1 : p2;
                int idx = activeParent.IndexOf(current);
                int next = activeParent[(idx + 1) % n];

                if (visited.Contains(next))
                {
                    next = -1; double min = double.MaxValue;
                    for (int j = 0; j < n; j++)
                        if (!visited.Contains(j) && matrix[current][j] < min) { min = matrix[current][j]; next = j; }
                }
                if (next != -1) { child.Add(next); visited.Add(next); current = next; }
            }
            return child;
        }

        static List<int> CrossoverHGREX(List<int> p1, List<int> p2, double[][] matrix)
        {
            int n = p1.Count;
            List<int> child = new List<int>();
            HashSet<int> visited = new HashSet<int>();
            int current = p1[0];
            child.Add(current);
            visited.Add(current);

            while (child.Count < n)
            {
                int nextP1 = p1[(p1.IndexOf(current) + 1) % n];
                int nextP2 = p2[(p2.IndexOf(current) + 1) % n];
                int selected = -1;

                bool v1 = !visited.Contains(nextP1);
                bool v2 = !visited.Contains(nextP2);

                if (v1 && v2) selected = (matrix[current][nextP1] < matrix[current][nextP2]) ? nextP1 : nextP2;
                else if (v1) selected = nextP1;
                else if (v2) selected = nextP2;
                else
                {
                    double min = double.MaxValue;
                    for (int i = 0; i < n; i++)
                        if (!visited.Contains(i) && matrix[current][i] < min) { min = matrix[current][i]; selected = i; }
                }
                if (selected != -1) { child.Add(selected); visited.Add(selected); current = selected; }
            }
            return child;
        }

        static List<int> Optimize2Opt(List<int> route, double[][] matrix)
        {
            bool improved = true;
            while (improved)
            {
                improved = false;
                for (int i = 1; i < route.Count - 1; i++)
                {
                    for (int j = i + 1; j < route.Count; j++)
                    {
                        double oldD = matrix[route[i - 1]][route[i]] + matrix[route[j]][route[(j + 1) % route.Count]];
                        double newD = matrix[route[i - 1]][route[j]] + matrix[route[i]][route[(j + 1) % route.Count]];
                        if (newD < oldD) { route.Reverse(i, j - i + 1); improved = true; }
                    }
                }
            }
            return route;
        }

        // --- FUNKCJE POMOCNICZE ---

        static double CalcDist(List<int> route, double[][] matrix)
        {
            double d = 0;
            for (int i = 0; i < route.Count; i++) d += matrix[route[i]][route[(i + 1) % route.Count]];
            return d;
        }

        static void DisplayRoute(string title, List<int> route, List<string> names, double[][] matrix)
        {
            Console.WriteLine($"\n>>> {title} <<<");
            for (int i = 0; i < route.Count; i++)
            {
                int c = route[i];
                int n = route[(i + 1) % route.Count];
                Console.WriteLine($"{i + 1}. {names[c]} -> {names[n]} ({matrix[c][n]} km)");
            }
            Console.WriteLine($"SUMA: {CalcDist(route, matrix):F2} km");
        }

        static (List<string>, double[][]) ParseDistanceFile(string path)
        {
            var names = new List<string>();
            var data = new List<List<double>>();
            var lines = File.ReadAllLines(path);
            int idx = -1;

            foreach (var l in lines)
            {
                if (string.IsNullOrWhiteSpace(l)) continue;
                if (char.IsLetter(l[0]))
                {
                    names.Add(l.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)[0]);
                    data.Add(new List<double>());
                    idx++;
                    Extract(l, data[idx]);
                }
                else if (idx >= 0) Extract(l, data[idx]);
            }
            return (names, data.Select(x => x.ToArray()).ToArray());
        }

        static void Extract(string input, List<double> list)
        {
            var ms = Regex.Matches(input, @"\b\d+\b");
            foreach (Match m in ms)
            {
                // Ignorujemy liczby w tagach 
                if (!input.Contains("[" + m.Value + "]") && !input.Contains(": " + m.Value))
                    list.Add(double.Parse(m.Value));
            }
        }
    }
}