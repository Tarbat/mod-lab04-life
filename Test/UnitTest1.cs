using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Xunit;
using cli_life;
using Microsoft.VisualStudio.TestPlatform.TestHost;

namespace Test
{
    public class LifeTests
    {
        [Fact]
        public void Cell_Creation_IsInitiallyDead()
        {
            var cell = new Cell();
            Assert.False(cell.IsAlive);
        }

        [Fact]
        public void Cell_DetermineNextState_AliveWith4Neighbors_Dies()
        {
            var cell = new Cell { IsAlive = true };
            for (int i = 0; i < 4; i++)
                cell.neighbors.Add(new Cell { IsAlive = true });

            cell.DetermineNextLiveState();
            Assert.False(cell.IsAliveNext);
        }

        [Fact]
        public void Board_Creation_HasCorrectDimensions()
        {
            var board = new Board(100, 100, 10);
            Assert.Equal(10, board.Columns);
            Assert.Equal(10, board.Rows);
        }

        [Fact]
        public void Board_CountAliveCells_ReturnsCorrectCount()
        {
            var board = new Board(10, 10, 1, 0);
            board.Cells[0, 0].IsAlive = true;
            board.Cells[1, 1].IsAlive = true;
            Assert.Equal(2, board.CountAliveCells());
        }

        [Fact]
        public void Board_SaveAndLoad_StatePreserved()
        {
            var board1 = new Board(10, 10, 1);
            board1.Cells[0, 0].IsAlive = true;
            board1.SaveToFile("test_save.txt");

            var board2 = new Board(10, 10, 1);
            board2.LoadFromFile("test_save.txt");
            Assert.True(board2.Cells[0, 0].IsAlive);

            File.Delete("test_save.txt");
        }

        [Fact]
        public void Board_Randomize_HasApproximateDensity()
        {
            double density = 0.3;
            var board = new Board(100, 100, 1, density);
            int alive = board.CountAliveCells();
            double actualDensity = alive / (double)(board.Columns * board.Rows);
            Assert.InRange(actualDensity, density - 0.1, density + 0.1); // ±10%
        }

        [Fact]
        public void Board_ConnectNeighbors_CorrectCount()
        {
            var board = new Board(10, 10, 1);
            Assert.Equal(8, board.Cells[0, 0].neighbors.Count); // Угловая клетка
            Assert.Equal(8, board.Cells[5, 5].neighbors.Count); // Центральная клетка
        }

        [Fact]
        public void Board_Toroidal_WrappingWorks()
        {
            var board = new Board(10, 10, 1);
            // Левая граница должна соединяться с правой
            Assert.Contains(board.Cells[9, 0], board.Cells[0, 0].neighbors);
            // Верхняя граница должна соединяться с нижней
            Assert.Contains(board.Cells[0, 9], board.Cells[0, 0].neighbors);
        }
       
        [Fact]
        public void Cell_DetermineNextState_AliveWith2Neighbors_StaysAlive()
        {
            var cell = new Cell { IsAlive = true };
            for (int i = 0; i < 2; i++)
                cell.neighbors.Add(new Cell { IsAlive = true });

            cell.DetermineNextLiveState();
            Assert.True(cell.IsAliveNext);
        }

        [Fact]
        public void Cell_DetermineNextState_AliveWith1Neighbor_Dies()
        {
            var cell = new Cell { IsAlive = true };
            cell.neighbors.Add(new Cell { IsAlive = true });

            cell.DetermineNextLiveState();
            Assert.False(cell.IsAliveNext);
        }
       
        [Fact]
        public void Board_Empty_NoAliveCells()
        {
            var board = new Board(10, 10, 1);
            foreach (var cell in board.Cells)
                cell.IsAlive = false;

            Assert.Equal(0, board.CountAliveCells());
        }

        [Fact]
        public void Board_Full_AllCellsAlive()
        {
            var board = new Board(10, 10, 1);
            foreach (var cell in board.Cells)
                cell.IsAlive = true;

            Assert.Equal(100, board.CountAliveCells());
        }
        
        [Fact]
        public void Board_LoadNonexistentFile_HandlesGracefully()
        {
            var board = new Board(10, 10, 1);
            var exception = Record.Exception(() => board.LoadFromFile("nonexistent.txt"));
            Assert.Null(exception);
        }

        [Fact]
        public void Board_SaveToInvalidPath_HandlesGracefully()
        {
            var board = new Board(10, 10, 1);
            var exception = Record.Exception(() => board.SaveToFile("/invalid/path/save.txt"));
            Assert.Null(exception);
        }

        [Fact]
        public void Bloc_MovesCorrectly()
        {
            var board = new Board(10, 10, 1, 0);
            // Создаем блок
            board.Cells[2, 1].IsAlive = true;
            board.Cells[3, 1].IsAlive = true;
            board.Cells[2, 0].IsAlive = true;
            board.Cells[3, 0].IsAlive = true;

            int initialCount = board.CountAliveCells();
            for (int i = 0; i < 4; i++) board.Advance();
            Assert.Equal(initialCount, board.CountAliveCells()); // Количество клеток не меняется
        }
    }
}