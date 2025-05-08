using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using cli_life;
using Microsoft.VisualStudio.TestPlatform.TestHost;

namespace Test
{
    [TestClass]
    public class LifeTests
    {
        [TestMethod]
        public void Cell_Creation_IsInitiallyDead()
        {
            var cell = new Cell();
            Assert.IsFalse(cell.IsAlive);
        }

        [TestMethod]
        public void Cell_DetermineNextState_AliveWith4Neighbors_Dies()
        {
            var cell = new Cell { IsAlive = true };
            for (int i = 0; i < 4; i++)
                cell.neighbors.Add(new Cell { IsAlive = true });

            cell.DetermineNextLiveState();
            Assert.IsFalse(cell.IsAliveNext);
        }

        [TestMethod]
        public void Board_Creation_HasCorrectDimensions()
        {
            var board = new Board(100, 100, 10);
            Assert.AreEqual(10, board.Columns);
            Assert.AreEqual(10, board.Rows);
        }

        [TestMethod]
        public void Board_CountAliveCells_ReturnsCorrectCount()
        {
            var board = new Board(10, 10, 1, 0);
            board.Cells[0, 0].IsAlive = true;
            board.Cells[1, 1].IsAlive = true;
            Assert.AreEqual(2, board.CountAliveCells());
        }

        [TestMethod]
        public void Board_SaveAndLoad_StatePreserved()
        {
            var board1 = new Board(10, 10, 1);
            board1.Cells[0, 0].IsAlive = true;
            board1.SaveToFile("test_save.txt");

            var board2 = new Board(10, 10, 1);
            board2.LoadFromFile("test_save.txt");
            Assert.IsTrue(board2.Cells[0, 0].IsAlive);

            File.Delete("test_save.txt");
        }

        [TestMethod]
        public void Board_Randomize_HasApproximateDensity()
        {
            double density = 0.3;
            var board = new Board(100, 100, 1, density);
            int alive = board.CountAliveCells();
            double actualDensity = alive / (double)(board.Columns * board.Rows);
            Assert.IsTrue(actualDensity >= density - 0.1 && actualDensity <= density + 0.1);
        }

        [TestMethod]
        public void Board_ConnectNeighbors_CorrectCount()
        {
            var board = new Board(10, 10, 1);
            Assert.AreEqual(8, board.Cells[0, 0].neighbors.Count);
            Assert.AreEqual(8, board.Cells[5, 5].neighbors.Count);
        }

        [TestMethod]
        public void Board_Toroidal_WrappingWorks()
        {
            var board = new Board(10, 10, 1);
            CollectionAssert.Contains(board.Cells[0, 0].neighbors, board.Cells[9, 0]);
            CollectionAssert.Contains(board.Cells[0, 0].neighbors, board.Cells[0, 9]);
        }

        [TestMethod]
        public void Cell_DetermineNextState_AliveWith2Neighbors_StaysAlive()
        {
            var cell = new Cell { IsAlive = true };
            for (int i = 0; i < 2; i++)
                cell.neighbors.Add(new Cell { IsAlive = true });

            cell.DetermineNextLiveState();
            Assert.IsTrue(cell.IsAliveNext);
        }

        [TestMethod]
        public void Cell_DetermineNextState_AliveWith1Neighbor_Dies()
        {
            var cell = new Cell { IsAlive = true };
            cell.neighbors.Add(new Cell { IsAlive = true });

            cell.DetermineNextLiveState();
            Assert.IsFalse(cell.IsAliveNext);
        }

        [TestMethod]
        public void Board_Empty_NoAliveCells()
        {
            var board = new Board(10, 10, 1);
            foreach (var cell in board.Cells)
                cell.IsAlive = false;

            Assert.AreEqual(0, board.CountAliveCells());
        }

        [TestMethod]
        public void Board_Full_AllCellsAlive()
        {
            var board = new Board(10, 10, 1);
            foreach (var cell in board.Cells)
                cell.IsAlive = true;

            Assert.AreEqual(100, board.CountAliveCells());
        }

        [TestMethod]
        public void Board_LoadNonexistentFile_HandlesGracefully()
        {
            var board = new Board(10, 10, 1);
            try
            {
                board.LoadFromFile("nonexistent.txt");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Expected no exception, but got {ex.GetType().Name}");
            }
        }

        [TestMethod]
        public void Board_SaveToInvalidPath_HandlesGracefully()
        {
            var board = new Board(10, 10, 1);
            try
            {
                board.SaveToFile("/invalid/path/save.txt");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Expected no exception, but got {ex.GetType().Name}");
            }
        }

        [TestMethod]
        public void Bloc_MovesCorrectly()
        {
            var board = new Board(10, 10, 1, 0);
            board.Cells[2, 1].IsAlive = true;
            board.Cells[3, 1].IsAlive = true;
            board.Cells[2, 0].IsAlive = true;
            board.Cells[3, 0].IsAlive = true;

            int initialCount = board.CountAliveCells();
            for (int i = 0; i < 4; i++) board.Advance();
            Assert.AreEqual(initialCount, board.CountAliveCells());
        }

        [TestMethod]
        public void Board_DetectsStabilizationCorrectly()
        {
            // Arrange - создаем стабильный блок
            var board = new Board(10, 10, 1, 0);
            board.Cells[1, 1].IsAlive = true;
            board.Cells[1, 2].IsAlive = true;
            board.Cells[2, 1].IsAlive = true;
            board.Cells[2, 2].IsAlive = true;

            var aliveHistory = new List<int>();

            // Act
            for (int i = 0; i < 5; i++)
            {
                board.Advance();
                aliveHistory.Add(board.CountAliveCells());
            }

            // Assert - количество живых клеток не должно меняться
            Assert.IsTrue(aliveHistory.All(count => count == 4),
                "Alive cell count should remain stable at 4");
        }

        [TestMethod]
        public void CellsHistory_FileContentIsCorrect()
        {
            var board = new Board(10, 10, 1, 0);

            // Arrange
            var history = new List<int> { 10, 15, 8, 3 }; // 4 элемента
            string testFilePath = "cells_history_test.txt";

            try
            {
                // Act
                Board.CellsHistory(history, testFilePath);

                // Assert
                Assert.IsTrue(File.Exists(testFilePath), "Файл не был создан");

                var lines = File.ReadAllLines(testFilePath);

                // Проверяем, что записано на 1 элемент меньше (из-за условия i < aliveCellsHistory.Count - 1)
                Assert.AreEqual(history.Count - 1, lines.Length, "Количество строк в файле неверное");

                // Проверяем содержимое каждой строки
                for (int i = 0; i < lines.Length; i++)
                {
                    Assert.AreEqual($"{i}: {history[i]}", lines[i]);
                }
            }
            finally
            {
                // Cleanup
                if (File.Exists(testFilePath))
                {
                    File.Delete(testFilePath);
                }
            }
        }
    }
}