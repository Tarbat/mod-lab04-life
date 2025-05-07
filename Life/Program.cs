using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Text.Json;
using System.IO;
using static System.Runtime.InteropServices.JavaScript.JSType;
using cli_life;
using System.Drawing;
using System.Drawing.Imaging;

namespace cli_life
{
    public class Cell
    {
        public bool IsAlive;
        public readonly List<Cell> neighbors = new List<Cell>();
        public bool IsAliveNext;
        public void DetermineNextLiveState()
        {
            int liveNeighbors = neighbors.Where(x => x.IsAlive).Count();
            if (IsAlive)
                IsAliveNext = liveNeighbors == 2 || liveNeighbors == 3;
            else
                IsAliveNext = liveNeighbors == 3;
        }
        public void Advance()
        {
            IsAlive = IsAliveNext;
        }
    }
    public class Board
    {
        public readonly Cell[,] Cells;
        public readonly int CellSize;

        public int Columns { get { return Cells.GetLength(0); } }
        public int Rows { get { return Cells.GetLength(1); } }
        public int Width { get { return Columns * CellSize; } }
        public int Height { get { return Rows * CellSize; } }

        public Board(int width, int height, int cellSize, double liveDensity = .1)
        {
            CellSize = cellSize;

            Cells = new Cell[width / cellSize, height / cellSize];
            for (int x = 0; x < Columns; x++)
                for (int y = 0; y < Rows; y++)
                    Cells[x, y] = new Cell();

            ConnectNeighbors();
            Randomize(liveDensity);
        }

        readonly Random rand = new Random();
        public void Randomize(double liveDensity)
        {
            foreach (var cell in Cells)
                cell.IsAlive = rand.NextDouble() < liveDensity;
        }

        public void Advance()
        {
            foreach (var cell in Cells)
                cell.DetermineNextLiveState();
            foreach (var cell in Cells)
                cell.Advance();
        }
        private void ConnectNeighbors()
        {
            for (int x = 0; x < Columns; x++)
            {
                for (int y = 0; y < Rows; y++)
                {
                    int xL = (x > 0) ? x - 1 : Columns - 1;
                    int xR = (x < Columns - 1) ? x + 1 : 0;

                    int yT = (y > 0) ? y - 1 : Rows - 1;
                    int yB = (y < Rows - 1) ? y + 1 : 0;

                    Cells[x, y].neighbors.Add(Cells[xL, yT]);
                    Cells[x, y].neighbors.Add(Cells[x, yT]);
                    Cells[x, y].neighbors.Add(Cells[xR, yT]);
                    Cells[x, y].neighbors.Add(Cells[xL, y]);
                    Cells[x, y].neighbors.Add(Cells[xR, y]);
                    Cells[x, y].neighbors.Add(Cells[xL, yB]);
                    Cells[x, y].neighbors.Add(Cells[x, yB]);
                    Cells[x, y].neighbors.Add(Cells[xR, yB]);
                }
            }
        }

        public void SaveToFile(string saved_board)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(saved_board))
                {
                    for (int y = 0; y < Rows; y++)
                    {
                        for (int x = 0; x < Columns; x++)
                        {
                            writer.Write(Cells[x, y].IsAlive ? '1' : '0');
                        }
                        writer.WriteLine(); // Переход на новую строку для каждого ряда
                    }
                }
                Console.WriteLine("Состояние сохранено в файл: " + saved_board);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при сохранении: {ex.Message}");
            }
        }

        public void LoadFromFile(string saved_board)
        {
            try
            {
                if (!File.Exists(saved_board))
                {
                    Console.WriteLine("Файл не найден.");
                    return;
                }

                string[] lines = File.ReadAllLines(saved_board);
                for (int y = 0; y < Math.Min(Rows, lines.Length); y++)
                {
                    for (int x = 0; x < Math.Min(Columns, lines[y].Length); x++)
                    {
                        char cellChar = lines[y][x];
                        Cells[x, y].IsAlive = cellChar == '1';
                    }
                }
                Console.WriteLine("Состояние загружено из файла: " + saved_board);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке: {ex.Message}");
            }
        }

        public int CountAliveCells()
        {
            Console.WriteLine("");
            int AliveC = 0;
            for (int y = 0; y < Rows; y++)
            {
                for (int x = 0; x < Columns; x++)
                {
                    if (Cells[x, y].IsAlive) AliveC++;
                }
            }
            Console.WriteLine("Живых клеток: " + AliveC);
            return AliveC;
        }
    }

    public class Pattern
    {
        public string Name { get; set; }
        public bool[,] Matrix { get; set; }
        public int Width => Matrix.GetLength(0);
        public int Height => Matrix.GetLength(1);
    }

    class Program
    {
        public static List<Pattern> LoadPatterns(string patternsDirectory)
        {
            var patterns = new List<Pattern>();
            string[] patternFiles = Directory.GetFiles(patternsDirectory, "*.txt");

            foreach (string filePath in patternFiles)
            {
                string[] lines = File.ReadAllLines(filePath);
                int width = lines[0].Length;
                int height = lines.Length;
                bool[,] matrix = new bool[width, height];

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        matrix[x, y] = lines[y][x] == '1';
                    }
                }

                patterns.Add(new Pattern
                {
                    Name = Path.GetFileNameWithoutExtension(filePath),
                    Matrix = matrix
                });
            }

            return patterns;
        }

        public static Dictionary<string, int> ClassifyPatterns(Board board, List<Pattern> patterns)
        {
            var result = new Dictionary<string, int>();
            bool[,] visited = new bool[board.Columns, board.Rows];

            foreach (var pattern in patterns)
            {
                result[pattern.Name] = 0;
            }

            for (int x = 0; x < board.Columns; x++)
            {
                for (int y = 0; y < board.Rows; y++)
                {
                    if (board.Cells[x, y].IsAlive && !visited[x, y])
                    {
                        var cluster = FindCluster(board, x, y, visited);
                        foreach (var pattern in patterns)
                        {
                            if (IsPatternMatch(cluster, pattern))
                            {
                                result[pattern.Name]++;
                                break;
                            }
                        }
                    }
                }
            }

            return result;
        }

        private static List<(int x, int y)> FindCluster(Board board, int startX, int startY, bool[,] visited)
        {
            var cluster = new List<(int x, int y)>();
            var queue = new Queue<(int x, int y)>();
            queue.Enqueue((startX, startY));

            while (queue.Count > 0)
            {
                var (x, y) = queue.Dequeue();

                if (x < 0 || x >= board.Columns || y < 0 || y >= board.Rows ||
                    !board.Cells[x, y].IsAlive || visited[x, y])
                    continue;

                visited[x, y] = true;
                cluster.Add((x, y));

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        queue.Enqueue((x + dx, y + dy));
                    }
                }
            }

            return cluster;
        }

        private static bool IsPatternMatch(List<(int x, int y)> cluster, Pattern pattern)
        {
            if (cluster.Count != pattern.Matrix.Cast<bool>().Count(c => c))
                return false;

            // Нормализуем координаты кластера
            int minX = cluster.Min(p => p.x);
            int minY = cluster.Min(p => p.y);
            var normalized = cluster.Select(p => (x: p.x - minX, y: p.y - minY)).ToList();

            // Проверяем все возможные повороты и отражения
            for (int rotation = 0; rotation < 4; rotation++)
            {
                for (int flip = 0; flip < 2; flip++)
                {
                    bool match = true;
                    foreach (var (x, y) in normalized)
                    {
                        int checkX = x;
                        int checkY = y;

                        // Применяем поворот
                        for (int r = 0; r < rotation; r++)
                        {
                            (checkX, checkY) = (checkY, -checkX);
                        }

                        // Применяем отражение
                        if (flip == 1)
                        {
                            checkX = -checkX;
                        }

                        // Корректируем координаты после преобразований
                        int patternX = checkX + pattern.Width / 2;
                        int patternY = checkY + pattern.Height / 2;

                        if (patternX < 0 || patternX >= pattern.Width ||
                            patternY < 0 || patternY >= pattern.Height ||
                            !pattern.Matrix[patternX, patternY])
                        {
                            match = false;
                            break;
                        }
                    }

                    if (match)
                        return true;
                }
            }

            return false;
        }

        public class BoardSettings
        {
            public int Width { get; set; }
            public int Height { get; set; }
            public int CellSize { get; set; }
            public double LiveDensity { get; set; }
        }

        static public BoardSettings LoadSettings(string filePath)
        {
            string jsonString = File.ReadAllText(filePath);

            var options = new JsonSerializerOptions
            {
                AllowTrailingCommas = true // Разрешаем висящие запятые
            };

            return JsonSerializer.Deserialize<BoardSettings>(jsonString, options);
        }

        static Board board;

        static private void Reset()
        {
            string settingsPath = "../../../settings.json";
            var settings = LoadSettings(settingsPath);

            board = new Board(
                width: settings.Width,
                height: settings.Height,
                cellSize: settings.CellSize,
                liveDensity: settings.LiveDensity);
        }

        static void Render()
        {
            for (int row = 0; row < board.Rows; row++)
            {
                for (int col = 0; col < board.Columns; col++)   
                {
                    var cell = board.Cells[col, row];
                    if (cell.IsAlive)
                    {
                        Console.Write('*');
                    }
                    else
                    {
                        Console.Write(' ');
                    }
                }
                Console.Write('\n');
            }
        }
        static int CountCellCombinations(Board board)
        {
            bool[,] visited = new bool[board.Columns, board.Rows];
            int count = 0;

            for (int x = 0; x < board.Columns; x++)
            {
                for (int y = 0; y < board.Rows; y++)
                {
                    if (board.Cells[x, y].IsAlive && !visited[x, y])
                    {
                        count++;
                        MarkConnectedCells(board, x, y, visited);
                    }
                }
            }

            return count;
        }

        static void MarkConnectedCells(Board board, int x, int y, bool[,] visited)
        {
            if (x < 0 || x >= board.Columns || y < 0 || y >= board.Rows)
                return;

            if (!board.Cells[x, y].IsAlive || visited[x, y])
                return;

            visited[x, y] = true;

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    MarkConnectedCells(board, x + dx, y + dy, visited);
                }
            }
        }

        public class SimulationResult
        {
            public double Density { get; set; }
            public List<int> AliveCellsHistory { get; set; } = new List<int>();
            public int GenerationsToStabilize { get; set; }
        }

        private static List<Pattern> patterns;

        public class GraphBuilder
        {
            public static void PlotAliveCellsHistory(List<int> aliveCellsHistory, string filePath)
            {
                if (aliveCellsHistory == null || aliveCellsHistory.Count == 0)
                    return;

                // Параметры изображения
                int width = 800;
                int height = 400;
                int margin = 50;
                int graphWidth = width - 2 * margin;
                int graphHeight = height - 2 * margin;

                // Создаем изображение
                using (Bitmap bitmap = new Bitmap(width, height))
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    // Заливаем фон
                    g.Clear(Color.White);

                    // Находим максимальное значение для масштабирования
                    int maxValue = aliveCellsHistory.Max();
                    if (maxValue == 0) maxValue = 1;

                    // Рисуем оси
                    using (Pen axisPen = new Pen(Color.Black, 2))
                    {
                        g.DrawLine(axisPen, margin, margin, margin, height - margin); // Y ось
                        g.DrawLine(axisPen, margin, height - margin, width - margin, height - margin); // X ось
                    }

                    // Подписи осей
                    using (Font font = new Font("Arial", 10))
                    using (SolidBrush brush = new SolidBrush(Color.Black))
                    {
                        g.DrawString("Поколение", font, brush, width / 2 - 30, height - margin + 20);
                        g.DrawString("Количество клеток", font, brush, 10, margin - 20);
                    }

                    // Деления и значения на осях
                    using (Pen tickPen = new Pen(Color.Black, 1))
                    using (Font tickFont = new Font("Arial", 8))
                    using (SolidBrush brush = new SolidBrush(Color.Black)) // Добавляем создание brush
                    {
                        // Деления на оси Y
                        for (int i = 0; i <= 10; i++)
                        {
                            int yPos = height - margin - (i * graphHeight / 10);
                            g.DrawLine(tickPen, margin - 5, yPos, margin + 5, yPos);
                            g.DrawString((maxValue * i / 10).ToString(), tickFont, brush, margin - 40, yPos - 8);
                        }

                        // Деления на оси X
                        int xStep = aliveCellsHistory.Count / 10;
                        if (xStep == 0) xStep = 1;
                        for (int i = 0; i <= aliveCellsHistory.Count; i += xStep)
                        {
                            int xPos = margin + (i * graphWidth / aliveCellsHistory.Count);
                            if (xPos > width - margin) xPos = width - margin;
                            g.DrawLine(tickPen, xPos, height - margin - 5, xPos, height - margin + 5);
                            g.DrawString(i.ToString(), tickFont, brush, xPos - 10, height - margin + 10);
                        }
                    }

                    // Рисуем график
                    using (Pen graphPen = new Pen(Color.Blue, 2))
                    {
                        for (int i = 0; i < aliveCellsHistory.Count - 1; i++)
                        {
                            int x1 = margin + (i * graphWidth / aliveCellsHistory.Count);
                            int y1 = height - margin - (aliveCellsHistory[i] * graphHeight / maxValue);
                            int x2 = margin + ((i + 1) * graphWidth / aliveCellsHistory.Count);
                            int y2 = height - margin - (aliveCellsHistory[i + 1] * graphHeight / maxValue);

                            g.DrawLine(graphPen, x1, y1, x2, y2);
                        }
                    }

                    // Сохраняем изображение
                    bitmap.Save(filePath, ImageFormat.Png);
                }
            }
        }

        static void Main(string[] args)
        {
            string patternsDir = Path.Combine(AppContext.BaseDirectory, "Patterns");
            patterns = LoadPatterns(patternsDir);

            Reset();
            string savedStatePath = "../../../saved_state.txt";
            string relativePath = Path.Combine("Life", "Shapes");
            bool isPaused = true;
            int generation = 0, Cell1 = 0, Cell2 = 0, stability = 0;
            List<int> aliveCellsHistory = new List<int>();

            Console.WriteLine("Управление:");
            Console.WriteLine("Пробел - пауза/продолжить");
            Console.WriteLine("S - сохранить текущее состояние");
            Console.WriteLine("L - загрузить сохранённое состояние");
            Console.WriteLine("U - загрузить заданную фигуру");
            Console.WriteLine("G - сохранить график живых клеток");
            Console.WriteLine("R - сбросить (случайное состояние)");
            Console.WriteLine("Esc - выход");
            Console.WriteLine("\nСимуляция запущена...");

            while (true)
            {
                if (!isPaused)
                {
                    Console.Clear();
                    Console.WriteLine($"Поколение: {generation++}");

                    int aliveCells = board.CountAliveCells();
                    aliveCellsHistory.Add(aliveCells); // Запись данных

                    if ((generation & 1) == 0) {
                        Cell1 = board.CountAliveCells();
                    } else {
                        Cell2 = board.CountAliveCells();
                    }
                    if (Cell1 == Cell2) stability++;
                        else stability = 0;
                    if (stability == 4) { Console.WriteLine($"Состояние стабилизировалось за {generation++} поколений: "); isPaused = true;}

                    int combinationsCount = CountCellCombinations(board);
                    Console.WriteLine($"Найдено комбинаций: {combinationsCount}");

                    var patternCounts = ClassifyPatterns(board, patterns);
                    Console.WriteLine("Найдено фигур:");
                    foreach (var kvp in patternCounts)
                    {
                        if (kvp.Value > 0)
                            Console.WriteLine($"{kvp.Key}: {kvp.Value}");
                    }

                    Render();
                    board.Advance();
                }

                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true).Key;
                    switch (key)
                    {
                        case ConsoleKey.Spacebar:
                            isPaused = !isPaused;
                            Console.WriteLine(isPaused ? "Пауза" : "Продолжено");
                            break;

                        case ConsoleKey.S:
                            board.SaveToFile(savedStatePath);
                            Console.WriteLine($"Сохранено в {savedStatePath}");
                            Thread.Sleep(1000);
                            break;

                        case ConsoleKey.L:
                            board.LoadFromFile(savedStatePath);
                            generation = 0;
                            Console.WriteLine($"Загружено из {savedStatePath}");
                            Thread.Sleep(1000);
                            break;

                        case ConsoleKey.U:
                            Console.WriteLine("Выбор фигуры: ");
                            string? x = Console.ReadLine();
                            string ShapesFail = @"../../../Board/" + x + ".txt";
                            board.LoadFromFile(ShapesFail);
                            generation = 0;
                            Console.WriteLine($"Загружено из {ShapesFail}");
                            Thread.Sleep(1000);
                            break;

                        case ConsoleKey.R:
                            Reset();
                            generation = 0;
                            Console.WriteLine("Сброшено в случайное состояние");
                            Thread.Sleep(1000);
                            break;
                        case ConsoleKey.G: // Сохранение графика
                            if (aliveCellsHistory.Count > 0)
                            {
                                string graphPath = "../../../plot.png";
                                GraphBuilder.PlotAliveCellsHistory(aliveCellsHistory, graphPath);
                                Console.WriteLine($"График сохранен в {graphPath}");
                            }
                            else
                            {
                                Console.WriteLine("Нет данных для построения графика");
                            }
                            Thread.Sleep(1000);
                            break;

                        case ConsoleKey.Escape:
                            if (aliveCellsHistory.Count > 0)
                            {
                                string graphPath = "../../../plot.png";
                                GraphBuilder.PlotAliveCellsHistory(aliveCellsHistory, graphPath);
                                Console.WriteLine($"График сохранен в {graphPath}");
                            }
                            else
                            {
                                Console.WriteLine("Нет данных для построения графика");
                            }
                            return;
                    }
                }
                
                if (!isPaused)
                {
                    Thread.Sleep(100);
                }
            }
        }
    }
}