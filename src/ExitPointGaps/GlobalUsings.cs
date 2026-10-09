// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

// System
global using System.ComponentModel;
global using System.Collections.Concurrent;
global using System.Collections.Generic;
global using System.Diagnostics;
global using System.Diagnostics.CodeAnalysis;
global using System.Globalization;
global using static System.FormattableString;
global using System.IO;
global using System.Linq;
global using System.Runtime.InteropServices;
global using System.Text;
global using System.Text.Json;
global using System.Text.Json.Serialization;
global using System.Threading;
global using System.Threading.Tasks;
global using System.Xml;
global using System.Xml.Linq;

// Microsoft
global using Microsoft.CodeAnalysis;
global using Microsoft.CodeAnalysis.CSharp;
global using Microsoft.CodeAnalysis.CSharp.Syntax;
global using Microsoft.CodeAnalysis.Text;

// Internal
global using ExitPointGaps.Analysis;
global using ExitPointGaps.Analysis.Models;
global using ExitPoints;
