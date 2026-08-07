Lights Out — WPF Desktop Application

A .NET 8 WPF desktop application implementing the Lights Out technical challenge with support for multiple board depths, custom piece shapes, manual puzzle input, built-in sample puzzles, and an automated backtracking solver.

The application provides a visual representation of the puzzle, shows the effect of every piece placement, and displays the final solution coordinates.

📸 Application Preview
<!-- Add your main application screenshot here --> <!-- Recommended filename: Screenshots/main-screen.png -->

🎯 Challenge Overview

Lights Out is a puzzle based on a board of lights. In the original game, switching a light also switches neighboring lights.

This challenge is a variation of the original game.

Instead of using only a fixed plus-shaped pattern, the puzzle introduces different piece shapes. It also allows each board cell to have more than two states.

For example, the states can cycle:

Red → Green → Blue → Red

The objective is to place every supplied piece on the board so that, after all pieces have been placed, every board cell has a final value of 0.

✨ Features
.NET 8 WPF desktop application
C# implementation
Document-style UI based on the supplied challenge
Editable puzzle input
Built-in 10 sample puzzles
Load sample puzzles from a dropdown
Load custom .txt puzzle files
Reset puzzle
Solve puzzle automatically
Backtracking solver
Memoization of failed states
Support for depth 2, 3, and 4
Multiple board cell states
Red, Green, Blue and Yellow board visualization
Custom piece shapes
No piece rotation
No board rotation
Pieces cannot be placed outside the board
Animated/step-by-step solution visualization
Shows board state after every piece placement
Displays solution coordinates in original piece order
Final solved-state validation
🖥️ User Interface

The application is designed as a technical/document-style desktop interface.

It contains:

┌──────────────────────────────────────────────────────┐
│ Lights Out                                            │
│                                                      │
│ Problem                                              │
│ ┌──────────────────────────────────────────────────┐ │
│ │ Editable puzzle input                            │ │
│ └──────────────────────────────────────────────────┘ │
│                                                      │
│ Sample Puzzles                                       │
│ ┌────────────────────────────────┐                  │
│ │ Sample 01                    ▼ │  Load Sample     │
│ └────────────────────────────────┘                  │
│                                                      │
│ [Load Input] [Reset] [Solve Puzzle]                 │
│                                                      │
│ Initial Board                                        │
│                                                      │
│ Pieces                                               │
│                                                      │
│ Solution                                             │
│                                                      │
└──────────────────────────────────────────────────────┘

The input remains editable even after selecting one of the built-in samples.

✏️ Manual Puzzle Input

The application allows a puzzle to be entered or modified manually.

For example:

2
001,011,011
.X,XX XX .X,XX

You can edit the input directly and then solve the modified puzzle.

This makes it possible to test puzzles without changing the source code.

🧩 Built-in Sample Puzzles

The application includes all 10 supplied puzzle input files.

The sample selector provides:

Sample 01
Sample 02
Sample 03
Sample 04
Sample 05
Sample 06
Sample 07
Sample 08
Sample 09
Sample 10

Selecting a sample and choosing Load Selected Sample populates the editable problem input.

The sample files are stored in:

Samples/
├── 01.txt
├── 02.txt
├── 03.txt
├── 04.txt
├── 05.txt
├── 06.txt
├── 07.txt
├── 08.txt
├── 09.txt
└── 10.txt

The supplied challenge defines the puzzle input as three lines: depth, initial board, and pieces.