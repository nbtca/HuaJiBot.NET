﻿using System.Diagnostics.CodeAnalysis;

namespace HuaJiBot.NET.Plugin.GitHubBridge.Utils;

using RgbTuple = (byte r, byte g, byte b);

public static class LangColorsHelper
{
    static readonly Dictionary<string, RgbTuple> Colors =
        new()
        {
            ["ABAP"] = (0xe8, 0x27, 0x4b),
            ["AGS Script"] = (0xb9, 0xd9, 0xff),
            ["AMPL"] = (0xe6, 0xef, 0xbb),
            ["ANTLR"] = (0x9d, 0xc3, 0xff),
            ["API Blueprint"] = (0x2a, 0xcc, 0xa8),
            ["APL"] = (0x5a, 0x81, 0x64),
            ["ASP"] = (0x6a, 0x40, 0xfd),
            ["Ada"] = (0x02, 0xf8, 0x8c),
            ["Agda"] = (0x31, 0x56, 0x65),
            ["Java"] = (0xb0, 0x72, 0x19),
            ["Vala"] = (0xfb, 0xe5, 0xcd),
            ["Scala"] = (0xc2, 0x2d, 0x40),
            ["Pascal"] = (0xe3, 0xf1, 0x71),
            ["Rascal"] = (0xff, 0xfa, 0xa0),
            ["Ballerina"] = (0xff, 0x50, 0x00),
            ["DataWeave"] = (0x00, 0x3a, 0x52),
            ["SaltStack"] = (0x64, 0x64, 0x64),
            ["Smalltalk"] = (0x59, 0x67, 0x06),
            ["JavaScript"] = (0xf1, 0xe0, 0x5a),
            ["Standard ML"] = (0xdc, 0x56, 0x6d),
            ["Visual Basic"] = (0x94, 0x5d, 0xb7),
            ["Component Pascal"] = (0xb0, 0xce, 0x4e),
            ["Game Maker Language"] = (0x71, 0xb4, 0x17),
            ["Common Workflow Language"] = (0xb5, 0x31, 0x4c),
            ["YARA"] = (0x22, 0x00, 0x00),
            ["MATLAB"] = (0xe1, 0x67, 0x37),
            ["Assembly"] = (0x6e, 0x4c, 0x13),
            ["Isabelle"] = (0xfe, 0xfe, 0x00),
            ["Harbour"] = (0x0e, 0x60, 0xe3),
            ["WebAssembly"] = (0x04, 0x13, 0x3b),
            ["ActionScript"] = (0x88, 0x2b, 0x0f),
            ["Arc"] = (0xaa, 0x2a, 0xfe),
            ["AspectJ"] = (0xa9, 0x57, 0xb0),
            ["Hack"] = (0x87, 0x87, 0x87),
            ["Yacc"] = (0x4b, 0x6c, 0x4b),
            ["AngelScript"] = (0xc7, 0xd7, 0xdc),
            ["AppleScript"] = (0x10, 0x1f, 0x1f),
            ["Factor"] = (0x63, 0x67, 0x46),
            ["Racket"] = (0x3c, 0x5c, 0xaa),
            ["Emacs Lisp"] = (0xc0, 0x65, 0xdb),
            ["Fancy"] = (0x7b, 0x9d, 0xb4),
            ["Batchfile"] = (0xc1, 0xf1, 0x2e),
            ["PureBasic"] = (0x5a, 0x69, 0x86),
            ["UnrealScript"] = (0xa5, 0x4c, 0x4d),
            ["MAXScript"] = (0x00, 0xa6, 0xa6),
            ["Markdown"] = (0x08, 0x3f, 0xa1),
            ["Asymptote"] = (0x4a, 0x0c, 0x0c),
            ["AutoHotkey"] = (0x65, 0x94, 0xb9),
            ["Haxe"] = (0xdf, 0x79, 0x00),
            ["Quake"] = (0x88, 0x22, 0x33),
            ["Ragel"] = (0x9d, 0x52, 0x00),
            ["xBase"] = (0x40, 0x3a, 0x40),
            ["Chapel"] = (0x8d, 0xc6, 0x3f),
            ["Haskell"] = (0x5e, 0x50, 0x86),
            ["Nearley"] = (0x99, 0x00, 0x00),
            ["Makefile"] = (0x42, 0x78, 0x19),
            ["FreeMarker"] = (0x00, 0x50, 0xb2),
            ["SRecode Template"] = (0x34, 0x8a, 0x34),
            ["YASnippet"] = (0x32, 0xab, 0x90),
            ["Erlang"] = (0xb8, 0x39, 0x98),
            ["Mirah"] = (0xc7, 0xa9, 0x38),
            ["Slash"] = (0x00, 0x7e, 0xff),
            ["AutoIt"] = (0x1c, 0x35, 0x52),
            ["Clarion"] = (0xdb, 0x90, 0x1e),
            ["PigLatin"] = (0xfc, 0xd7, 0xde),
            ["Mask"] = (0xf9, 0x77, 0x32),
            ["Alloy"] = (0x64, 0xc8, 0x00),
            ["Opal"] = (0xf7, 0xed, 0xe0),
            ["Dhall"] = (0xdf, 0xaf, 0xff),
            ["Metal"] = (0x8f, 0x14, 0xe9),
            ["Crystal"] = (0x00, 0x01, 0x00),
            ["OCaml"] = (0x3b, 0xe1, 0x33),
            ["RAML"] = (0x77, 0xd9, 0xfb),
            ["TI Program"] = (0xa0, 0xaa, 0x87),
            ["Fantom"] = (0x14, 0x25, 0x3c),
            ["Pan"] = (0xcc, 0x00, 0x00),
            ["Stan"] = (0xb2, 0x01, 0x1d),
            ["Clean"] = (0x3f, 0x85, 0xaf),
            ["Dylan"] = (0x6c, 0x61, 0x6e),
            ["Fortran"] = (0x4d, 0x41, 0xb1),
            ["Pawn"] = (0xdb, 0xb2, 0x84),
            ["SourcePawn"] = (0x5c, 0x76, 0x11),
            ["Lasso"] = (0x99, 0x99, 0x99),
            ["Parrot"] = (0xf3, 0xca, 0x0a),
            ["ZAP"] = (0x0d, 0x66, 0x5e),
            ["Papyrus"] = (0x66, 0x00, 0xcc),
            ["Dart"] = (0x00, 0xb4, 0xab),
            ["ATS"] = (0x1a, 0xc6, 0x20),
            ["SAS"] = (0xb3, 0x49, 0x36),
            ["Max"] = (0xc4, 0xa7, 0x9c),
            ["BlitzMax"] = (0xcd, 0x64, 0x00),
            ["Boo"] = (0xd4, 0xbe, 0xc1),
            ["EmberScript"] = (0xff, 0xf4, 0xf3),
            ["Objective-C"] = (0x43, 0x8e, 0xff),
            ["Objective-J"] = (0xff, 0x0c, 0x5a),
            ["ObjectScript"] = (0x42, 0x48, 0x93),
            ["Objective-C++"] = (0x68, 0x66, 0xfb),
            ["PowerBuilder"] = (0x8f, 0x0f, 0x8d),
            ["Protocol Buffers"] = (0xcc, 0xcc, 0xcc),
            ["Jupyter Notebook"] = (0xda, 0x5b, 0x0b),
            ["Rebol"] = (0x35, 0x8a, 0x5b),
            ["Ruby"] = (0x70, 0x15, 0x16),
            ["C++"] = (0xf3, 0x4b, 0x7d),
            ["C"] = (0x55, 0x55, 0x55),
            ["C#"] = (0x17, 0x86, 0x00),
            ["CSS"] = (0x56, 0x3d, 0x7c),
            ["Ceylon"] = (0xdf, 0xa5, 0x35),
            ["Cirru"] = (0xcc, 0xcc, 0xff),
            ["Cuda"] = (0x3a, 0x4e, 0x3a),
            ["Click"] = (0xe4, 0xe6, 0xf3),
            ["CoffeeScript"] = (0x24, 0x47, 0x76),
            ["mcfunction"] = (0xe2, 0x28, 0x37),
            ["ColdFusion"] = (0xed, 0x2c, 0xd6),
            ["G-code"] = (0xd0, 0x8c, 0xf2),
            ["SuperCollider"] = (0x46, 0x39, 0x0b),
            ["Clojure"] = (0xdb, 0x58, 0x55),
            ["Slice"] = (0x00, 0x3f, 0xa2),
            ["Processing"] = (0x00, 0x96, 0xd8),
            ["Scheme"] = (0x1e, 0x4a, 0xec),
            ["Dockerfile"] = (0x38, 0x4d, 0x54),
            ["Common Lisp"] = (0x3f, 0xb6, 0x8b),
            ["GDScript"] = (0x35, 0x55, 0x70),
            ["ZenScript"] = (0x00, 0xbc, 0xd1),
            ["Dogescript"] = (0xcc, 0xa7, 0x60),
            ["LiveScript"] = (0x49, 0x98, 0x86),
            ["PogoScript"] = (0xd8, 0x00, 0x74),
            ["PostScript"] = (0xda, 0x29, 0x1c),
            ["PureScript"] = (0x1d, 0x22, 0x2d),
            ["TypeScript"] = (0x2b, 0x74, 0x89),
            ["Vim script"] = (0x19, 0x9f, 0x4b),
            ["Tcl"] = (0xe4, 0xcc, 0x98),
            ["ECL"] = (0x8a, 0x12, 0x67),
            ["NCL"] = (0x28, 0x43, 0x1f),
            ["VCL"] = (0x14, 0x8a, 0xa8),
            ["Mercury"] = (0xff, 0x2b, 0x2b),
            ["D"] = (0xba, 0x59, 0x5e),
            ["DM"] = (0x44, 0x72, 0x65),
            ["Modula-3"] = (0x22, 0x33, 0x88),
            ["Solidity"] = (0xaa, 0x67, 0x46),
            ["Idris"] = (0xb3, 0x00, 0x00),
            ["wdl"] = (0x42, 0xf1, 0xf4),
            ["IDL"] = (0xa3, 0x52, 0x2f),
            ["VHDL"] = (0xad, 0xb2, 0xcb),
            ["E"] = (0xcc, 0xce, 0x35),
            ["EQ"] = (0xa7, 0x86, 0x49),
            ["Eiffel"] = (0x94, 0x6d, 0x57),
            ["Elixir"] = (0x6e, 0x4a, 0x7e),
            ["Elm"] = (0x60, 0xb5, 0xcc),
            ["Terra"] = (0x00, 0x00, 0x4c),
            ["NetLinx+ERB"] = (0x74, 0x7f, 0xaa),
            ["eC"] = (0x91, 0x39, 0x60),
            ["nesC"] = (0x94, 0xb0, 0xc7),
            ["Red"] = (0xf5, 0x00, 0x00),
            ["sed"] = (0x64, 0xb9, 0x70),
            ["Frege"] = (0x00, 0xca, 0xfe),
            ["Genie"] = (0xfb, 0x85, 0x5d),
            ["Nemerle"] = (0x3d, 0x3c, 0x6e),
            ["Oxygene"] = (0xcd, 0xd0, 0xe3),
            ["PowerShell"] = (0x01, 0x24, 0x56),
            ["SystemVerilog"] = (0xda, 0xe1, 0xc2),
            ["Propeller Spin"] = (0x7f, 0xa2, 0xa7),
            ["Self"] = (0x05, 0x79, 0xaa),
            ["Nextflow"] = (0x3a, 0xc4, 0x86),
            ["NetLogo"] = (0xff, 0x63, 0x75),
            ["Verilog"] = (0xb2, 0xb7, 0xf8),
            ["Zephir"] = (0x11, 0x8f, 0x9e),
            ["Gherkin"] = (0x5b, 0x20, 0x63),
            ["NetLinx"] = (0x0a, 0xa0, 0xff),
            ["NewLisp"] = (0x87, 0xae, 0xd7),
            ["Shell"] = (0x89, 0xe0, 0x51),
            ["Squirrel"] = (0x80, 0x00, 0x00),
            ["Perl"] = (0x02, 0x98, 0xc3),
            ["Perl 6"] = (0x00, 0x00, 0xfb),
            ["HiveQL"] = (0xdc, 0xe2, 0x00),
            ["Shen"] = (0x12, 0x0f, 0x14),
            ["Ren'Py"] = (0xff, 0x7f, 0x7f),
            ["Meson"] = (0x00, 0x78, 0x00),
            ["Pep8"] = (0xc7, 0x6f, 0x5b),
            ["XQuery"] = (0x52, 0x32, 0xe7),
            ["Puppet"] = (0x30, 0x2b, 0x6d),
            ["Jsonnet"] = (0x00, 0x64, 0xbd),
            ["Lex"] = (0xdb, 0xca, 0x00),
            ["TeX"] = (0x3d, 0x61, 0x17),
            ["F#"] = (0xb8, 0x45, 0xfc),
            ["F*"] = (0x57, 0x2e, 0x30),
            ["FLUX"] = (0x88, 0xcc, 0xff),
            ["Forth"] = (0x34, 0x17, 0x08),
            ["LFE"] = (0x4c, 0x30, 0x23),
            ["Roff"] = (0xec, 0xde, 0xbe),
            ["Omgrofl"] = (0xca, 0xbb, 0xff),
            ["Swift"] = (0xff, 0xac, 0x45),
            ["Go"] = (0x00, 0xad, 0xd8),
            ["Glyph"] = (0xc1, 0xac, 0x7f),
            ["Rouge"] = (0xcc, 0x00, 0x88),
            ["Gnuplot"] = (0xf0, 0xa9, 0xf0),
            ["Groovy"] = (0xe6, 0x9f, 0x56),
            ["HTML"] = (0xe3, 0x4c, 0x26),
            ["HolyC"] = (0xff, 0xef, 0xaf),
            ["Python"] = (0x35, 0x72, 0xa5),
            ["PHP"] = (0x4f, 0x5d, 0x95),
            ["Hy"] = (0x77, 0x90, 0xb2),
            ["Io"] = (0xa9, 0x18, 0x8d),
            ["Ioke"] = (0x07, 0x81, 0x93),
            ["Julia"] = (0xa2, 0x70, 0xba),
            ["Jolie"] = (0x84, 0x31, 0x79),
            ["Pike"] = (0x00, 0x53, 0x90),
            ["Zig"] = (0xec, 0x91, 0x5c),
            ["Ring"] = (0x2d, 0x54, 0xcb),
            ["Turing"] = (0xcf, 0x14, 0x2b),
            ["ZIL"] = (0xdc, 0x75, 0xe5),
            ["Nim"] = (0x37, 0x77, 0x5b),
            ["Kotlin"] = (0xf1, 0x8e, 0x33),
            ["wisp"] = (0x75, 0x82, 0xd1),
            ["JSONiq"] = (0x40, 0xd4, 0x7e),
            ["Nit"] = (0x00, 0x99, 0x17),
            ["Nix"] = (0x7e, 0x7e, 0xff),
            ["J"] = (0x9e, 0xed, 0xff),
            ["KRL"] = (0x28, 0x43, 0x0a),
            ["LookML"] = (0x65, 0x2b, 0x81),
            ["LLVM"] = (0x18, 0x56, 0x19),
            ["LSL"] = (0x3d, 0x99, 0x70),
            ["Lua"] = (0x00, 0x00, 0x80),
            ["Prolog"] = (0x74, 0x28, 0x3c),
            ["Wollok"] = (0xa2, 0x37, 0x38),
            ["PLSQL"] = (0xda, 0xd8, 0xd8),
            ["Volt"] = (0x1f, 0x1f, 0x1f),
            ["XSLT"] = (0xeb, 0x8c, 0xeb),
            ["MQL4"] = (0x62, 0xa8, 0xd6),
            ["MQL5"] = (0x4a, 0x76, 0xb8),
            ["MTML"] = (0xb7, 0xe1, 0xf4),
            ["QML"] = (0x44, 0xa5, 0x1c),
            ["Nu"] = (0xc9, 0xdf, 0x40),
            ["ooc"] = (0xb0, 0xb7, 0x7e),
            ["Oz"] = (0xfa, 0xb7, 0x38),
            ["P4"] = (0x70, 0x55, 0xb5),
            ["SQF"] = (0x3f, 0x3f, 0x3f),
            ["SQL"] = (0xe3, 0x8c, 0x00),
            ["R"] = (0x19, 0x8c, 0xe7),
            ["Rust"] = (0xde, 0xa5, 0x84),
            ["Vue"] = (0x2c, 0x3e, 0x50),
            ["X10"] = (0x4b, 0x6b, 0xef),
            ["XC"] = (0x99, 0xda, 0x07),
        };

    public static bool GetColor(string? name, [NotNullWhen(true)] out RgbTuple? color)
    {
        if (name is not null)
        {
            if (Colors.TryGetValue(name, out var result))
            {
                color = result;
                return true;
            }
            foreach (var (k, v) in Colors) //大小写不敏感
            {
                if (k.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    color = v;
                    return true;
                }
            }
        }
        color = null;
        return false;
    }
}
