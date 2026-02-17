# Grafpack – Drawing Canvas Application

Module: Computer Graphics Programming (MOD006127 TRI2)

This project was developed for my Computer Graphics Programming module during my Bachelor’s degree.

## Overview

Grafpack is a Windows Forms drawing application built using C# and the .NET Framework. It allows users to create, select, transform, and delete graphical shapes on a canvas. The application demonstrates object-oriented design, mathematical transformations, and graphics rendering using native drawing APIs.

## Features

Users can create Squares, Triangles, Circles, and Polygons using mouse input. Shapes can be selected via mouse or keyboard, then translated or rotated either through drag-and-drop or a transform dialog.

The application supports stroke and fill colours, background customization, undo (last five actions), and a help dialog. Double buffering is implemented to ensure smooth rendering.

An abstract `Shape` base class defines shared properties, while derived classes implement specific drawing logic and cloning behavior. The main `Canvas` form manages rendering, event handling, shape storage, and transformation logic.

## Outcome

The project demonstrates practical use of C#, Windows Forms, matrix-based rotation, geometric calculations, and structured state management to build a lightweight interactive graphics application.
